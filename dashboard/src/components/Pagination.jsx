import React from 'react';

function RefreshButton({ onClick, state }) {
  return (
    <button
      className={`refresh-btn ${state}`}
      onClick={onClick}
      disabled={state === 'spinning'}
      title="Refresh"
    >
      {state === 'done' ? (
        <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <polyline points="3,8 7,12 13,4" />
        </svg>
      ) : (
        <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <path d="M14 8A6 6 0 1 1 10 2.5" />
          <polyline points="14,2 14,6 10,6" />
        </svg>
      )}
    </button>
  );
}

function Pagination({ totalRecords, maxResults, currentPage, onPageChange, onPageSizeChange, onRefresh, refreshState }) {
  const totalPages = Math.max(1, Math.ceil(totalRecords / maxResults));

  return (
    <div className="data-table-toolbar">
      <div className="data-table-toolbar-left">
        {onRefresh && <RefreshButton onClick={onRefresh} state={refreshState || 'idle'} />}
        <span>{totalRecords} record{totalRecords !== 1 ? 's' : ''}</span>
        <select className="page-size-select" value={maxResults} onChange={(e) => onPageSizeChange(Number(e.target.value))}>
          <option value={10}>10</option>
          <option value={25}>25</option>
          <option value={50}>50</option>
          <option value={100}>100</option>
        </select>
        <span>per page</span>
      </div>
      <div className="data-table-toolbar-right">
        <div className="pagination">
          <button onClick={() => onPageChange(1)} disabled={currentPage <= 1} title="First">&laquo;</button>
          <button onClick={() => onPageChange(currentPage - 1)} disabled={currentPage <= 1} title="Previous">&lsaquo;</button>
          <span style={{ fontSize: '0.8125rem', padding: '0 0.5rem' }}>
            Page {currentPage} of {totalPages}
          </span>
          <button onClick={() => onPageChange(currentPage + 1)} disabled={currentPage >= totalPages} title="Next">&rsaquo;</button>
          <button onClick={() => onPageChange(totalPages)} disabled={currentPage >= totalPages} title="Last">&raquo;</button>
        </div>
      </div>
    </div>
  );
}

export default Pagination;
