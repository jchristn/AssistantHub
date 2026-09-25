namespace AssistantHub.Server.OpenApi
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Text.Json.Serialization;
    using System.Xml.Linq;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// Builds OpenAPI schemas from CLR types by reflection, honoring the AssistantHub JSON conventions
    /// (JsonPropertyName, JsonIgnore, string enums) and using XML documentation summaries as descriptions.
    /// </summary>
    public static class ApiSchema
    {
        #region Private-Members

        private const int _MaxDepth = 6;
        private static readonly ConcurrentDictionary<Assembly, Dictionary<string, string>> _XmlDocs = new ConcurrentDictionary<Assembly, Dictionary<string, string>>();
        private static readonly ConcurrentDictionary<string, OpenApiSchemaMetadata> _Components = new ConcurrentDictionary<string, OpenApiSchemaMetadata>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Type> _ComponentTypes = new Dictionary<string, Type>(StringComparer.Ordinal);

        #endregion

        #region Public-Members

        /// <summary>
        /// Component schemas generated so far, keyed by component name. Every named model referenced by
        /// route metadata appears here and is emitted under components/schemas.
        /// </summary>
        public static IReadOnlyDictionary<string, OpenApiSchemaMetadata> Components => _Components;

        #endregion

        #region Public-Methods

        /// <summary>
        /// Build a schema for a CLR type.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <returns>Schema.</returns>
        public static OpenApiSchemaMetadata FromType(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return Build(type, 0, new HashSet<Type>());
        }

        /// <summary>
        /// Build a string schema.
        /// </summary>
        /// <param name="description">Description.</param>
        /// <param name="format">Optional format.</param>
        /// <returns>Schema.</returns>
        public static OpenApiSchemaMetadata String(string description = null, string format = null)
        {
            return new OpenApiSchemaMetadata { Type = "string", Description = description, Format = format };
        }

        /// <summary>
        /// Build a binary string schema.
        /// </summary>
        /// <param name="description">Description.</param>
        /// <returns>Schema.</returns>
        public static OpenApiSchemaMetadata Binary(string description = null)
        {
            return new OpenApiSchemaMetadata { Type = "string", Format = "binary", Description = description };
        }

        #endregion

        #region Private-Methods

        private static OpenApiSchemaMetadata Build(Type type, int depth, HashSet<Type> path)
        {
            Type underlying = Nullable.GetUnderlyingType(type);
            if (underlying != null)
            {
                OpenApiSchemaMetadata inner = Build(underlying, depth, path);
                inner.Nullable = true;
                return inner;
            }

            if (type == typeof(string) || type == typeof(char)) return new OpenApiSchemaMetadata { Type = "string" };
            if (type == typeof(bool)) return new OpenApiSchemaMetadata { Type = "boolean" };
            if (type == typeof(int) || type == typeof(short) || type == typeof(byte) || type == typeof(sbyte) || type == typeof(ushort))
                return new OpenApiSchemaMetadata { Type = "integer", Format = "int32" };
            if (type == typeof(long) || type == typeof(uint) || type == typeof(ulong))
                return new OpenApiSchemaMetadata { Type = "integer", Format = "int64" };
            if (type == typeof(float)) return new OpenApiSchemaMetadata { Type = "number", Format = "float" };
            if (type == typeof(double)) return new OpenApiSchemaMetadata { Type = "number", Format = "double" };
            if (type == typeof(decimal)) return new OpenApiSchemaMetadata { Type = "number" };
            if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) return new OpenApiSchemaMetadata { Type = "string", Format = "date-time" };
            if (type == typeof(Guid)) return new OpenApiSchemaMetadata { Type = "string", Format = "uuid" };
            if (type == typeof(TimeSpan)) return new OpenApiSchemaMetadata { Type = "string", Description = "Duration in hh:mm:ss format." };
            if (type == typeof(Uri)) return new OpenApiSchemaMetadata { Type = "string", Format = "uri" };
            if (type == typeof(byte[])) return new OpenApiSchemaMetadata { Type = "string", Format = "byte" };

            if (type.IsEnum)
            {
                return new OpenApiSchemaMetadata
                {
                    Type = "string",
                    Enum = Enum.GetNames(type).Cast<object>().ToList()
                };
            }

            if (type == typeof(object)
                || type == typeof(JsonElement)
                || type == typeof(JsonDocument)
                || typeof(JsonNode).IsAssignableFrom(type))
            {
                return new OpenApiSchemaMetadata { Description = "Arbitrary JSON value." };
            }

            Type dictionaryValueType = GetDictionaryValueType(type);
            if (dictionaryValueType != null)
            {
                return new OpenApiSchemaMetadata
                {
                    Type = "object",
                    Description = "Map of string keys to " + DescribeType(dictionaryValueType) + " values."
                };
            }

            Type elementType = GetEnumerableElementType(type);
            if (elementType != null)
            {
                return new OpenApiSchemaMetadata
                {
                    Type = "array",
                    Items = depth >= _MaxDepth ? new OpenApiSchemaMetadata { Type = "object" } : Build(elementType, depth + 1, path)
                };
            }

            bool anonymous = IsAnonymous(type);
            if (!anonymous)
            {
                // Named models are emitted once under components/schemas and referenced everywhere else.
                string componentName = GetComponentName(type);
                if (!_Components.ContainsKey(componentName))
                {
                    _Components[componentName] = new OpenApiSchemaMetadata { Type = "object" };
                    _Components[componentName] = BuildObject(type, 0, new HashSet<Type>());
                }

                return new OpenApiSchemaMetadata { Ref = "#/components/schemas/" + componentName };
            }

            return BuildObject(type, depth, path);
        }

        private static OpenApiSchemaMetadata BuildObject(Type type, int depth, HashSet<Type> path)
        {
            OpenApiSchemaMetadata schema = new OpenApiSchemaMetadata { Type = "object" };
            if (depth >= _MaxDepth || path.Contains(type))
            {
                schema.Description = "Nested " + DescribeType(type) + " object.";
                return schema;
            }

            path.Add(type);
            Dictionary<string, OpenApiSchemaMetadata> properties = new Dictionary<string, OpenApiSchemaMetadata>(StringComparer.Ordinal);

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetIndexParameters().Length > 0) continue;
                if (property.GetMethod == null || !property.GetMethod.IsPublic) continue;

                JsonIgnoreAttribute ignore = property.GetCustomAttribute<JsonIgnoreAttribute>();
                if (ignore != null && ignore.Condition == JsonIgnoreCondition.Always) continue;

                JsonPropertyNameAttribute nameAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
                string name = nameAttribute?.Name ?? property.Name;
                if (properties.ContainsKey(name)) continue;

                OpenApiSchemaMetadata propertySchema = Build(property.PropertyType, depth + 1, path);
                string summary = GetPropertySummary(property);
                if (!System.String.IsNullOrEmpty(summary)) propertySchema.Description = summary;
                properties[name] = propertySchema;
            }

            path.Remove(type);

            if (properties.Count > 0) schema.Properties = properties;
            string typeSummary = GetTypeSummary(type);
            if (!System.String.IsNullOrEmpty(typeSummary)) schema.Description = typeSummary;
            return schema;
        }

        private static bool IsAnonymous(Type type)
        {
            return type.Name.Contains("AnonymousType", StringComparison.Ordinal)
                || (type.Name.StartsWith("<>", StringComparison.Ordinal) && type.IsGenericType);
        }

        private static string GetComponentName(Type type)
        {
            string name = GetBaseComponentName(type);

            lock (_ComponentTypes)
            {
                if (_ComponentTypes.TryGetValue(name, out Type existing) && existing != type)
                    name = (type.FullName ?? name).Replace('.', '_').Replace('+', '_').Replace('`', '_');

                _ComponentTypes[name] = type;
            }

            return name;
        }

        private static string GetBaseComponentName(Type type)
        {
            if (!type.IsGenericType) return type.Name.Replace('+', '_');

            string name = type.Name;
            int tick = name.IndexOf('`');
            if (tick > 0) name = name.Substring(0, tick);
            return name + "Of" + System.String.Join("And", type.GetGenericArguments().Select(GetBaseComponentName));
        }

        private static Type GetDictionaryValueType(Type type)
        {
            foreach (Type candidate in new[] { type }.Concat(type.GetInterfaces()))
            {
                if (!candidate.IsGenericType) continue;
                Type definition = candidate.GetGenericTypeDefinition();
                if (definition == typeof(IDictionary<,>) || definition == typeof(IReadOnlyDictionary<,>))
                    return candidate.GetGenericArguments()[1];
            }

            if (typeof(IDictionary).IsAssignableFrom(type)) return typeof(object);
            return null;
        }

        private static Type GetEnumerableElementType(Type type)
        {
            if (type == typeof(string)) return null;
            if (type.IsArray) return type.GetElementType();

            foreach (Type candidate in new[] { type }.Concat(type.GetInterfaces()))
            {
                if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return candidate.GetGenericArguments()[0];
            }

            if (typeof(IEnumerable).IsAssignableFrom(type)) return typeof(object);
            return null;
        }

        private static string DescribeType(Type type)
        {
            Type underlying = Nullable.GetUnderlyingType(type) ?? type;
            if (!underlying.IsGenericType) return underlying.Name;
            string name = underlying.Name;
            int tick = name.IndexOf('`');
            if (tick > 0) name = name.Substring(0, tick);
            return name + "<" + System.String.Join(", ", underlying.GetGenericArguments().Select(DescribeType)) + ">";
        }

        private static string GetTypeSummary(Type type)
        {
            if (type.IsGenericType || type.Name.Contains("AnonymousType", StringComparison.Ordinal)) return null;
            return GetSummary(type.Assembly, "T:" + type.FullName?.Replace('+', '.'));
        }

        private static string GetPropertySummary(PropertyInfo property)
        {
            Type declaring = property.DeclaringType;
            if (declaring == null || declaring.Name.Contains("AnonymousType", StringComparison.Ordinal)) return null;

            string typeName = declaring.IsGenericType
                ? declaring.GetGenericTypeDefinition().FullName
                : declaring.FullName;

            if (System.String.IsNullOrEmpty(typeName)) return null;
            return GetSummary(declaring.Assembly, "P:" + typeName.Replace('+', '.') + "." + property.Name);
        }

        private static string GetSummary(Assembly assembly, string memberName)
        {
            Dictionary<string, string> docs = _XmlDocs.GetOrAdd(assembly, LoadXmlDocs);
            return docs.TryGetValue(memberName, out string summary) ? summary : null;
        }

        private static Dictionary<string, string> LoadXmlDocs(Assembly assembly)
        {
            Dictionary<string, string> docs = new Dictionary<string, string>(StringComparer.Ordinal);

            try
            {
                string assemblyName = assembly.GetName().Name;
                List<string> candidates = new List<string>
                {
                    Path.Combine(AppContext.BaseDirectory, assemblyName + ".xml")
                };

                if (!System.String.IsNullOrEmpty(assembly.Location))
                    candidates.Add(Path.ChangeExtension(assembly.Location, ".xml"));

                string file = candidates.FirstOrDefault(File.Exists);
                if (file == null) return docs;

                XDocument document = XDocument.Load(file);
                foreach (XElement member in document.Descendants("member"))
                {
                    string name = member.Attribute("name")?.Value;
                    XElement summary = member.Element("summary");
                    if (System.String.IsNullOrEmpty(name) || summary == null) continue;

                    string text = System.String.Join(" ", summary.DescendantNodes()
                        .Select(node => node is XText textNode
                            ? textNode.Value
                            : node is XElement element && element.Attribute("cref") != null
                                ? element.Attribute("cref").Value.Substring(element.Attribute("cref").Value.LastIndexOf('.') + 1)
                                : null)
                        .Where(value => value != null))
                        .Replace("\r", " ")
                        .Replace("\n", " ");

                    while (text.Contains("  ", StringComparison.Ordinal))
                        text = text.Replace("  ", " ", StringComparison.Ordinal);

                    text = text.Trim();
                    if (text.Length > 0) docs[name] = text;
                }
            }
            catch
            {
            }

            return docs;
        }

        #endregion
    }
}
