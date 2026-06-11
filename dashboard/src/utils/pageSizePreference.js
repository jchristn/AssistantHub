export const PAGE_SIZE_OPTIONS = [10, 25, 50, 100];

const PAGE_SIZE_STORAGE_KEY = 'assistanthub.table.pageSize';

export function normalizePageSize(value, fallback = 10) {
  const fallbackSize = PAGE_SIZE_OPTIONS.includes(Number(fallback)) ? Number(fallback) : 10;
  const parsed = Number(value);
  return PAGE_SIZE_OPTIONS.includes(parsed) ? parsed : fallbackSize;
}

export function getStoredPageSize(fallback = 10) {
  if (typeof window === 'undefined' || !window.localStorage) return normalizePageSize(fallback);

  try {
    return normalizePageSize(window.localStorage.getItem(PAGE_SIZE_STORAGE_KEY), fallback);
  } catch {
    return normalizePageSize(fallback);
  }
}

export function setStoredPageSize(size, fallback = 10) {
  const normalized = normalizePageSize(size, fallback);
  if (typeof window === 'undefined' || !window.localStorage) return normalized;

  try {
    window.localStorage.setItem(PAGE_SIZE_STORAGE_KEY, String(normalized));
  } catch {
    // Ignore storage failures; the in-memory selection still applies.
  }

  return normalized;
}
