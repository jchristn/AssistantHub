import { useState, useRef, useEffect, useCallback } from 'react';

const STATUS_PERCENT = {
  uploading: 10,
  uploaded: 15,
  typedetecting: 25,
  processing: 40,
  summarizing: 50,
  processingchunks: 60,
  storingembeddings: 80,
  completed: 100,
  indexed: 100,
  active: 100,
  failed: 100,
  error: 100,
};

const STATUS_LABEL = {
  queued: 'Queued',
  uploading: 'Uploading',
  uploaded: 'Uploaded',
  typedetecting: 'Detecting type',
  processing: 'Processing',
  summarizing: 'Summarizing',
  processingchunks: 'Chunking',
  storingembeddings: 'Storing embeddings',
  completed: 'Completed',
  indexed: 'Completed',
  active: 'Completed',
  failed: 'Failed',
  error: 'Error',
};

function statusToPercent(status) {
  if (!status) return 0;
  return STATUS_PERCENT[status.toLowerCase()] ?? 10;
}

function statusToLabel(status) {
  if (!status) return '';
  return STATUS_LABEL[status.toLowerCase()] ?? status;
}

function isFinalStatus(status) {
  if (!status) return false;
  const s = status.toLowerCase();
  return s === 'completed' || s === 'indexed' || s === 'active' || s === 'failed' || s === 'error';
}

function isErrorStatus(status) {
  if (!status) return false;
  const s = status.toLowerCase();
  return s === 'failed' || s === 'error';
}

let nextId = 1;

export function useUploadQueue(api) {
  const [records, setRecords] = useState([]);
  const pollTimers = useRef({});
  const activeCount = useRef(0);
  const queue = useRef([]);
  const unmounted = useRef(false);
  const CONCURRENCY = 3;

  const updateRecord = useCallback((id, patch) => {
    setRecords(prev => prev.map(r => r.id === id ? { ...r, ...patch } : r));
  }, []);

  const pollDocument = useCallback((id, serverDocId) => {
    const timer = setInterval(async () => {
      if (unmounted.current) return;
      try {
        const doc = await api.getDocument(serverDocId);
        if (!doc) return;
        const status = doc.Status || '';
        const percent = statusToPercent(status);
        const stepLabel = statusToLabel(status);
        const patch = { status, percentage: percent, stepLabel };
        if (isFinalStatus(status)) {
          clearInterval(timer);
          delete pollTimers.current[id];
          activeCount.current = Math.max(0, activeCount.current - 1);
          patch.completedAt = Date.now();
          if (isErrorStatus(status)) {
            patch.error = doc.ErrorMessage || doc.FailureReason || 'Processing failed';
          }
          processQueue();
        }
        updateRecord(id, patch);
      } catch {
        // polling error, will retry next interval
      }
    }, 3000);
    pollTimers.current[id] = timer;
  }, [api, updateRecord]);

  const processOne = useCallback(async (item) => {
    const { id, file, ruleId, labels, tags } = item;
    updateRecord(id, { status: 'Uploading', percentage: 10, stepLabel: 'Uploading' });
    try {
      const base64 = await new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result.split(',')[1]);
        reader.onerror = reject;
        reader.readAsDataURL(file);
      });

      const payload = {
        IngestionRuleId: ruleId,
        Name: file.name,
        OriginalFilename: file.name,
        ContentType: file.type || 'application/octet-stream',
        Base64Content: base64,
      };
      if (labels && labels.length > 0) payload.Labels = labels;
      if (tags && Object.keys(tags).length > 0) payload.Tags = tags;

      const result = await api.uploadDocument(payload);
      if (result && result.statusCode && result.statusCode >= 400) {
        throw new Error(result.ErrorMessage || 'Upload failed');
      }
      const serverDocId = result?.Id || result?.GUID || result?.id;
      if (serverDocId) {
        updateRecord(id, { serverDocId, status: 'Uploaded', percentage: 15, stepLabel: 'Uploaded' });
        pollDocument(id, serverDocId);
      } else {
        updateRecord(id, { status: 'Completed', percentage: 100, stepLabel: 'Completed', completedAt: Date.now() });
        activeCount.current = Math.max(0, activeCount.current - 1);
        processQueue();
      }
    } catch (err) {
      updateRecord(id, { status: 'Failed', percentage: 100, stepLabel: 'Failed', error: err.message || 'Upload failed', completedAt: Date.now() });
      activeCount.current = Math.max(0, activeCount.current - 1);
      processQueue();
    }
  }, [api, updateRecord, pollDocument]);

  const processQueue = useCallback(() => {
    while (activeCount.current < CONCURRENCY && queue.current.length > 0) {
      const item = queue.current.shift();
      activeCount.current++;
      processOne(item);
    }
  }, [processOne]);

  const enqueueFiles = useCallback((files, ruleId, labels, tags) => {
    const newRecords = files.map(file => {
      const id = nextId++;
      return { id, fileName: file.name, status: 'Queued', stepLabel: 'Queued', serverDocId: null, percentage: 0, error: null, completedAt: null, _file: file, _ruleId: ruleId, _labels: labels, _tags: tags };
    });

    setRecords(prev => [...prev, ...newRecords.map(({ _file, _ruleId, _labels, _tags, ...r }) => r)]);

    for (const rec of newRecords) {
      queue.current.push({ id: rec.id, file: rec._file, ruleId: rec._ruleId, labels: rec._labels, tags: rec._tags });
    }
    processQueue();
  }, [processQueue]);

  const dismissRecord = useCallback((id) => {
    if (pollTimers.current[id]) {
      clearInterval(pollTimers.current[id]);
      delete pollTimers.current[id];
    }
    setRecords(prev => prev.filter(r => r.id !== id));
  }, []);

  // Auto-cleanup completed records after 180s
  useEffect(() => {
    const timer = setInterval(() => {
      const cutoff = Date.now() - 180000;
      setRecords(prev => {
        const toRemove = prev.filter(r => r.completedAt && r.completedAt < cutoff);
        if (toRemove.length === 0) return prev;
        for (const r of toRemove) {
          if (pollTimers.current[r.id]) {
            clearInterval(pollTimers.current[r.id]);
            delete pollTimers.current[r.id];
          }
        }
        return prev.filter(r => !r.completedAt || r.completedAt >= cutoff);
      });
    }, 10000);
    return () => clearInterval(timer);
  }, []);

  // Cleanup on unmount
  useEffect(() => {
    unmounted.current = false;
    return () => {
      unmounted.current = true;
      for (const timer of Object.values(pollTimers.current)) {
        clearInterval(timer);
      }
      pollTimers.current = {};
    };
  }, []);

  return { records, enqueueFiles, dismissRecord };
}
