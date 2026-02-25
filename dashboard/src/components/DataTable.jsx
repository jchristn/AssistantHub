import React, { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import Pagination from './Pagination';
import ActionMenu from './ActionMenu';
import Tooltip from './Tooltip';

function DataTable({ columns, fetchData, getRowActions, refreshTrigger, initialFilters, onBulkDelete }) {
  const [allData, setAllData] = useState([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [loading, setLoading] = useState(false);
  const [refreshState, setRefreshState] = useState('idle');
  const [filters, setFilters] = useState(initialFilters || {});
  const [sortKey, setSortKey] = useState(null);
  const [sortDirection, setSortDirection] = useState(null);
  const [selectedIds, setSelectedIds] = useState(new Set());
  const [bulkDeleting, setBulkDeleting] = useState(false);
  const refreshTimerRef = useRef(null);

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      const result = await fetchData({ maxResults: 1000 });
      if (result && result.Objects) {
        setAllData(result.Objects);
      } else if (result && Array.isArray(result)) {
        setAllData(result);
      }
    } catch (err) {
      console.error('Failed to load data:', err);
    } finally {
      setLoading(false);
    }
  }, [fetchData]);

  useEffect(() => { loadData(); }, [loadData, refreshTrigger]);

  useEffect(() => {
    return () => { if (refreshTimerRef.current) clearTimeout(refreshTimerRef.current); };
  }, []);

  // Reset to page 1 when filters change
  useEffect(() => {
    setCurrentPage(1);
  }, [filters]);

  const filteredData = useMemo(() => {
    return allData.filter(row => {
      return Object.entries(filters).every(([key, value]) => {
        if (!value) return true;
        const cellValue = row[key];
        if (cellValue == null) return false;
        return String(cellValue).toLowerCase().includes(value.toLowerCase());
      });
    });
  }, [allData, filters]);

  const sortedData = useMemo(() => {
    if (!sortKey || !sortDirection) return filteredData;
    return [...filteredData].sort((a, b) => {
      const aVal = a[sortKey];
      const bVal = b[sortKey];
      if (aVal == null && bVal == null) return 0;
      if (aVal == null) return 1;
      if (bVal == null) return -1;
      const cmp = String(aVal).localeCompare(String(bVal), undefined, { numeric: true, sensitivity: 'base' });
      return sortDirection === 'asc' ? cmp : -cmp;
    });
  }, [filteredData, sortKey, sortDirection]);

  const totalRecords = filteredData.length;

  const paginatedData = useMemo(() => {
    const start = (currentPage - 1) * pageSize;
    return sortedData.slice(start, start + pageSize);
  }, [sortedData, currentPage, pageSize]);

  const handleRefresh = async () => {
    if (refreshState === 'spinning') return;
    setRefreshState('spinning');
    await loadData();
    setRefreshState('done');
    refreshTimerRef.current = setTimeout(() => setRefreshState('idle'), 1500);
  };

  const handlePageChange = (page) => setCurrentPage(page);
  const handlePageSizeChange = (size) => { setPageSize(size); setCurrentPage(1); };

  const handleFilterChange = (key, value) => {
    setFilters(prev => ({ ...prev, [key]: value }));
  };

  const handleSort = (key) => {
    if (sortKey === key) {
      if (sortDirection === 'asc') {
        setSortDirection('desc');
      } else {
        setSortKey(null);
        setSortDirection(null);
      }
    } else {
      setSortKey(key);
      setSortDirection('asc');
    }
  };

  // Clear selection when data or page changes
  useEffect(() => { setSelectedIds(new Set()); }, [paginatedData, currentPage]);

  const pageIds = useMemo(() => paginatedData.map(row => row.Id || row.GUID), [paginatedData]);
  const allPageSelected = pageIds.length > 0 && pageIds.every(id => selectedIds.has(id));

  const toggleSelectAll = () => {
    if (allPageSelected) {
      setSelectedIds(new Set());
    } else {
      setSelectedIds(new Set(pageIds));
    }
  };

  const toggleSelectRow = (id) => {
    setSelectedIds(prev => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const handleBulkDelete = async () => {
    if (selectedIds.size === 0 || !onBulkDelete) return;
    setBulkDeleting(true);
    await onBulkDelete(Array.from(selectedIds));
    setSelectedIds(new Set());
    setBulkDeleting(false);
  };

  const hasFilterableColumns = columns.some(col => col.filterable);

  return (
    <div className="data-table-container">
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
        <Pagination
          totalRecords={totalRecords}
          maxResults={pageSize}
          currentPage={currentPage}
          onPageChange={handlePageChange}
          onPageSizeChange={handlePageSizeChange}
          onRefresh={handleRefresh}
          refreshState={refreshState}
        />
        {onBulkDelete && selectedIds.size > 0 && (
          <button
            className="btn btn-danger"
            style={{ whiteSpace: 'nowrap', fontSize: '0.8125rem' }}
            onClick={handleBulkDelete}
            disabled={bulkDeleting}
          >
            {bulkDeleting ? 'Deleting...' : `Delete Selected (${selectedIds.size})`}
          </button>
        )}
      </div>
      {loading ? (
        <div className="loading"><div className="spinner" /></div>
      ) : allData.length === 0 ? (
        <div className="empty-state"><p>No records found.</p></div>
      ) : (
        <table className="data-table">
          <thead>
            <tr>
              {onBulkDelete && (
                <th style={{ width: '2.5rem', textAlign: 'center' }}>
                  <input type="checkbox" checked={allPageSelected} onChange={toggleSelectAll} />
                </th>
              )}
              {columns.map((col) => (
                <th key={col.key} onClick={() => handleSort(col.key)}>
                  <span className="th-content">
                    <Tooltip text={col.tooltip}>{col.label}</Tooltip>
                    {sortKey === col.key && (
                      <span className="sort-indicator">
                        {sortDirection === 'asc' ? (
                          <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M12 19V5M5 12l7-7 7 7"/></svg>
                        ) : (
                          <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M12 5v14M19 12l-7 7-7-7"/></svg>
                        )}
                      </span>
                    )}
                  </span>
                </th>
              ))}
              {getRowActions && <th className="actions-cell"></th>}
            </tr>
            {hasFilterableColumns && (
              <tr className="data-table-filter-row">
                {onBulkDelete && <td></td>}
                {columns.map((col) => (
                  <td key={col.key}>
                    {col.filterable && (
                      <input
                        type="text"
                        placeholder="Filter..."
                        value={filters[col.key] || ''}
                        onChange={(e) => handleFilterChange(col.key, e.target.value)}
                        onClick={(e) => e.stopPropagation()}
                      />
                    )}
                  </td>
                ))}
                {getRowActions && <td></td>}
              </tr>
            )}
          </thead>
          <tbody>
            {paginatedData.length === 0 ? (
              <tr>
                <td colSpan={columns.length + (getRowActions ? 1 : 0) + (onBulkDelete ? 1 : 0)} style={{ textAlign: 'center', padding: '2rem', color: 'var(--text-secondary)' }}>
                  No matching records.
                </td>
              </tr>
            ) : paginatedData.map((row, idx) => {
              const rowId = row.Id || row.GUID;
              return (
                <tr key={rowId || idx}>
                  {onBulkDelete && (
                    <td style={{ width: '2.5rem', textAlign: 'center' }}>
                      <input type="checkbox" checked={selectedIds.has(rowId)} onChange={() => toggleSelectRow(rowId)} />
                    </td>
                  )}
                  {columns.map((col) => (
                    <td key={col.key}>{col.render ? col.render(row) : row[col.key]}</td>
                  ))}
                  {getRowActions && (
                    <td className="actions-cell">
                      <ActionMenu items={getRowActions(row)} />
                    </td>
                  )}
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </div>
  );
}

export default DataTable;
