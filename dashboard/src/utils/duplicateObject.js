const DEFAULT_OMIT_FIELDS = new Set([
  'Id',
  'GUID',
  'CreatedUtc',
  'UpdatedUtc',
  'CreationDate',
  'LastModifiedUtc',
  'LastCrawlStartUtc',
  'LastCrawlFinishUtc',
  'LastCrawlSuccess',
  'LastCrawlError',
  'LastCrawlBytes',
  'LastCrawlDocuments',
  'NextCrawlUtc',
  'State',
]);

function cloneValue(value) {
  if (Array.isArray(value)) return value.map(cloneValue);
  if (value && typeof value === 'object') {
    return Object.fromEntries(Object.entries(value).map(([key, entry]) => [key, cloneValue(entry)]));
  }
  return value;
}

export function appendCopySuffix(value, suffix = ' (Copy)') {
  const text = value === undefined || value === null ? '' : String(value);
  return text.trim() ? `${text}${suffix}` : text;
}

export function createDuplicateInitialData(row, options = {}) {
  const {
    nameField = 'Name',
    suffix = ' (Copy)',
    omitFields = [],
    includeFields = null,
    transform = null,
  } = options;

  const omitted = new Set([...DEFAULT_OMIT_FIELDS, ...omitFields]);
  const sourceEntries = includeFields
    ? includeFields.filter(field => row?.[field] !== undefined).map(field => [field, row[field]])
    : Object.entries(row || {}).filter(([field]) => !omitted.has(field));

  const data = Object.fromEntries(sourceEntries.map(([field, value]) => [field, cloneValue(value)]));
  if (nameField && Object.prototype.hasOwnProperty.call(data, nameField)) {
    data[nameField] = appendCopySuffix(data[nameField], suffix);
  }

  return transform ? transform(data, row) : data;
}
