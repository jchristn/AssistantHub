namespace AssistantHub.Core.Helpers
{
    using System;
    using System.Data;
    using System.Globalization;
    using System.Text.Json;

    /// <summary>
    /// Helper class for extracting typed values from DataRow objects.
    /// </summary>
    public static class DataTableHelper
    {
        #region Public-Methods

        /// <summary>
        /// Get a string value from a data row.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <param name="columnName">Column name.</param>
        /// <returns>String value or null.</returns>
        public static string GetStringValue(DataRow row, string columnName)
        {
            if (row == null) return null;
            if (!row.Table.Columns.Contains(columnName)) return null;
            if (row[columnName] == null || row[columnName] == DBNull.Value) return null;
            return row[columnName].ToString();
        }

        /// <summary>
        /// Get a boolean value from a data row.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <param name="columnName">Column name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>Boolean value.</returns>
        public static bool GetBooleanValue(DataRow row, string columnName, bool defaultValue = false)
        {
            string val = GetStringValue(row, columnName);
            if (String.IsNullOrEmpty(val)) return defaultValue;

            if (Boolean.TryParse(val, out bool result)) return result;
            if (val == "1") return true;
            if (val == "0") return false;
            return defaultValue;
        }

        /// <summary>
        /// Get an integer value from a data row.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <param name="columnName">Column name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>Integer value.</returns>
        public static int GetIntValue(DataRow row, string columnName, int defaultValue = 0)
        {
            string val = GetStringValue(row, columnName);
            if (String.IsNullOrEmpty(val)) return defaultValue;
            if (Int32.TryParse(val, out int result)) return result;
            return defaultValue;
        }

        /// <summary>
        /// Get a long value from a data row.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <param name="columnName">Column name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>Long value.</returns>
        public static long GetLongValue(DataRow row, string columnName, long defaultValue = 0)
        {
            string val = GetStringValue(row, columnName);
            if (String.IsNullOrEmpty(val)) return defaultValue;
            if (Int64.TryParse(val, out long result)) return result;
            return defaultValue;
        }

        /// <summary>
        /// Get a nullable long value from a data row.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <param name="columnName">Column name.</param>
        /// <returns>Nullable long value.</returns>
        public static long? GetNullableLongValue(DataRow row, string columnName)
        {
            string val = GetStringValue(row, columnName);
            if (String.IsNullOrEmpty(val)) return null;
            if (Int64.TryParse(val, out long result)) return result;
            return null;
        }

        /// <summary>
        /// Get a double value from a data row.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <param name="columnName">Column name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>Double value.</returns>
        public static double GetDoubleValue(DataRow row, string columnName, double defaultValue = 0.0)
        {
            string val = GetStringValue(row, columnName);
            if (String.IsNullOrEmpty(val)) return defaultValue;
            if (Double.TryParse(val, CultureInfo.InvariantCulture, out double result)) return result;
            return defaultValue;
        }

        /// <summary>
        /// Get a DateTime value from a data row.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <param name="columnName">Column name.</param>
        /// <returns>DateTime value.</returns>
        public static DateTime GetDateTimeValue(DataRow row, string columnName)
        {
            string val = GetStringValue(row, columnName);
            if (String.IsNullOrEmpty(val)) return DateTime.UtcNow;
            if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime result)) return result;
            return DateTime.UtcNow;
        }

        /// <summary>
        /// Get a nullable DateTime value from a data row.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <param name="columnName">Column name.</param>
        /// <returns>Nullable DateTime value.</returns>
        public static DateTime? GetNullableDateTimeValue(DataRow row, string columnName)
        {
            string val = GetStringValue(row, columnName);
            if (String.IsNullOrEmpty(val)) return null;
            if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime result)) return result;
            return null;
        }

        /// <summary>
        /// Get an enum value from a data row.
        /// </summary>
        /// <typeparam name="T">Enum type.</typeparam>
        /// <param name="row">Data row.</param>
        /// <param name="columnName">Column name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>Enum value.</returns>
        public static T GetEnumValue<T>(DataRow row, string columnName, T defaultValue = default) where T : struct, Enum
        {
            string val = GetStringValue(row, columnName);
            if (String.IsNullOrEmpty(val)) return defaultValue;
            if (Enum.TryParse<T>(val, true, out T result)) return result;
            return defaultValue;
        }

        /// <summary>
        /// Get a float array value from a data row (stored as JSON).
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <param name="columnName">Column name.</param>
        /// <returns>Float array or null.</returns>
        public static float[] GetFloatArrayValue(DataRow row, string columnName)
        {
            string val = GetStringValue(row, columnName);
            if (String.IsNullOrEmpty(val)) return null;
            try
            {
                return JsonSerializer.Deserialize<float[]>(val);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get a byte array value from a data row (stored as base64).
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <param name="columnName">Column name.</param>
        /// <returns>Byte array or null.</returns>
        public static byte[] GetByteArrayValue(DataRow row, string columnName)
        {
            string val = GetStringValue(row, columnName);
            if (String.IsNullOrEmpty(val)) return null;
            try
            {
                return Convert.FromBase64String(val);
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
