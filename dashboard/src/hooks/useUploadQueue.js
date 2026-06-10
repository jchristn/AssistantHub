import { useState, useEffect, useCallback } from 'react';

const INGESTION_STEP_TOTAL = 9;

const STATUS_STEP = {
  queued: 0,
  uploading: 1,
  uploaded: 2,
  typedetecting: 3,
  typedetectionsuccess: 4,
  processing: 4,
  storingtext: 5,
  processingchunks: 6,
  summarizing: 7,
  storingembeddings: 8,
  completed: 9,
  indexed: 9,
  active: 9,
  typedetectionfailed: 9,
  failed: 9,
  error: 9,
};

const STATUS_LABEL = {
  queued: 'Queued',
  uploading: 'Uploading',
  uploaded: 'Uploaded',
  typedetecting: 'Detecting type',
  typedetectionsuccess: 'Type detected',
  typedetectionfailed: 'Failed',
  processing: 'Processing',
  storingtext: 'Storing text',
  summarizing: 'Summarizing',
  processingchunks: 'Chunking',
  storingembeddings: 'Storing embeddings',
  completed: 'Completed',
  indexed: 'Completed',
  active: 'Completed',
  failed: 'Failed',
  error: 'Error',
};

const uploadQueueStore = {
  api: null,
  listeners: new Set(),
  records: [],
  pollTimers: {},
  activeCount: 0,
  queue: [],
  cleanupTimer: null,
};

let nextId = 1;

function statusToStep(status) {
  if (!status) return 0;
  return STATUS_STEP[status.toLowerCase()] ?? 1;
}

function statusToLabel(status) {
  if (!status) return '';
  return STATUS_LABEL[status.toLowerCase()] ?? status;
}

function isFinalStatus(status) {
  if (!status) return false;
  const s = status.toLowerCase();
  return s === 'completed' || s === 'indexed' || s === 'active' || s === 'typedetectionfailed' || s === 'failed' || s === 'error';
}

function isErrorStatus(status) {
  if (!status) return false;
  const s = status.toLowerCase();
  return s === 'typedetectionfailed' || s === 'failed' || s === 'error';
}

function isDismissibleStatus(status) {
  return isFinalStatus(status) || isErrorStatus(status);
}

function getConcurrency() {
  return Number(window.DASHBOARD_INGEST_MAX_PARALLEL_INGESTIONS) || 5;
}

function emit() {
  const snapshot = uploadQueueStore.records;
  for (const listener of uploadQueueStore.listeners) {
    listener(snapshot);
  }
}

function updateRecords(updater) {
  uploadQueueStore.records = updater(uploadQueueStore.records);
  emit();
}

function updateRecord(id, patch) {
  updateRecords((records) => records.map((record) => (record.id === id ? { ...record, ...patch } : record)));
}

function removePollTimer(id) {
  if (uploadQueueStore.pollTimers[id]) {
    clearInterval(uploadQueueStore.pollTimers[id]);
    delete uploadQueueStore.pollTimers[id];
  }
}

function ensureCleanupTimer() {
  if (uploadQueueStore.cleanupTimer) return;

  uploadQueueStore.cleanupTimer = setInterval(() => {
    const cutoff = Date.now() - 180000;
    const expiredIds = uploadQueueStore.records
      .filter((record) => record.completedAt && record.completedAt < cutoff)
      .map((record) => record.id);

    if (expiredIds.length < 1) return;

    for (const id of expiredIds) {
      removePollTimer(id);
    }

    const expiredSet = new Set(expiredIds);
    updateRecords((records) => records.filter((record) => !expiredSet.has(record.id)));
  }, 10000);
}

function ensureApi() {
  if (!uploadQueueStore.api) {
    throw new Error('Upload queue API client is not initialized.');
  }

  return uploadQueueStore.api;
}

function pollDocument(id, serverDocId) {
  removePollTimer(id);

  const timer = setInterval(async () => {
    try {
      const doc = await ensureApi().getDocument(serverDocId);
      if (!doc) return;

      const status = doc.Status || '';
      const patch = {
        status,
        stepCount: statusToStep(status),
        stepTotal: INGESTION_STEP_TOTAL,
        stepLabel: statusToLabel(status),
      };

      if (isFinalStatus(status)) {
        removePollTimer(id);
        uploadQueueStore.activeCount = Math.max(0, uploadQueueStore.activeCount - 1);
        patch.completedAt = Date.now();
        if (isErrorStatus(status)) {
          patch.error = doc.ErrorMessage || doc.FailureReason || 'Processing failed';
        }
        updateRecord(id, patch);
        processQueue();
        return;
      }

      updateRecord(id, patch);
    } catch {
      // polling error, will retry next interval
    }
  }, 3000);

  uploadQueueStore.pollTimers[id] = timer;
}

async function processOne(item) {
  const { id, file, ruleId, labels, tags } = item;
  updateRecord(id, { status: 'Uploading', stepCount: 1, stepTotal: INGESTION_STEP_TOTAL, stepLabel: 'Uploading' });

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

    const result = await ensureApi().uploadDocument(payload);
    if (result && result.statusCode && result.statusCode >= 400) {
      throw new Error(result.ErrorMessage || 'Upload failed');
    }

    const serverDocId = result?.Id || result?.GUID || result?.id;
    if (serverDocId) {
      updateRecord(id, { serverDocId, status: 'Uploaded', stepCount: 2, stepTotal: INGESTION_STEP_TOTAL, stepLabel: 'Uploaded' });
      pollDocument(id, serverDocId);
    } else {
      updateRecord(id, { status: 'Completed', stepCount: INGESTION_STEP_TOTAL, stepTotal: INGESTION_STEP_TOTAL, stepLabel: 'Completed', completedAt: Date.now() });
      uploadQueueStore.activeCount = Math.max(0, uploadQueueStore.activeCount - 1);
      processQueue();
    }
  } catch (err) {
    updateRecord(id, {
      status: 'Failed',
      stepCount: INGESTION_STEP_TOTAL,
      stepTotal: INGESTION_STEP_TOTAL,
      stepLabel: 'Failed',
      error: err.message || 'Upload failed',
      completedAt: Date.now(),
    });
    uploadQueueStore.activeCount = Math.max(0, uploadQueueStore.activeCount - 1);
    processQueue();
  }
}

function processQueue() {
  while (uploadQueueStore.activeCount < getConcurrency() && uploadQueueStore.queue.length > 0) {
    const item = uploadQueueStore.queue.shift();
    uploadQueueStore.activeCount++;
    processOne(item);
  }
}

function enqueueFilesInternal(files, ruleId, labels, tags) {
  const newRecords = files.map((file) => {
    const id = nextId++;
    return {
      id,
      fileName: file.name,
      status: 'Queued',
      stepLabel: 'Queued',
      serverDocId: null,
      stepCount: 0,
      stepTotal: INGESTION_STEP_TOTAL,
      error: null,
      completedAt: null,
      _file: file,
      _ruleId: ruleId,
      _labels: labels,
      _tags: tags,
    };
  });

  updateRecords((records) => [
    ...records,
    ...newRecords.map(({ _file, _ruleId, _labels, _tags, ...record }) => record),
  ]);

  for (const record of newRecords) {
    uploadQueueStore.queue.push({
      id: record.id,
      file: record._file,
      ruleId: record._ruleId,
      labels: record._labels,
      tags: record._tags,
    });
  }

  processQueue();
}

function dismissRecordInternal(id) {
  const record = uploadQueueStore.records.find((item) => item.id === id);
  if (!record || !isDismissibleStatus(record.status)) return;

  removePollTimer(id);
  updateRecords((records) => records.filter((item) => item.id !== id));
}

function clearFinishedRecordsInternal() {
  const removableIds = uploadQueueStore.records
    .filter((record) => isDismissibleStatus(record.status))
    .map((record) => record.id);

  if (removableIds.length < 1) return;

  for (const id of removableIds) {
    removePollTimer(id);
  }

  const removableSet = new Set(removableIds);
  updateRecords((records) => records.filter((item) => !removableSet.has(item.id)));
}

export function useUploadQueue(api) {
  if (api) {
    uploadQueueStore.api = api;
  }

  const [records, setRecords] = useState(uploadQueueStore.records);

  useEffect(() => {
    ensureCleanupTimer();

    const listener = (nextRecords) => setRecords(nextRecords);
    uploadQueueStore.listeners.add(listener);
    listener(uploadQueueStore.records);

    return () => {
      uploadQueueStore.listeners.delete(listener);
    };
  }, []);

  const enqueueFiles = useCallback((files, ruleId, labels, tags) => {
    enqueueFilesInternal(files, ruleId, labels, tags);
  }, []);

  const dismissRecord = useCallback((id) => {
    dismissRecordInternal(id);
  }, []);

  const clearFinishedRecords = useCallback(() => {
    clearFinishedRecordsInternal();
  }, []);

  return { records, enqueueFiles, dismissRecord, clearFinishedRecords };
}
