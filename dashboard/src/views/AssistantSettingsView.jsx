import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ApiClient } from '../utils/api';
import Tooltip from '../components/Tooltip';
import AlertModal from '../components/AlertModal';
import JsonViewModal from '../components/modals/JsonViewModal';
import PasswordInput from '../components/PasswordInput';
import { LabelConstraintInput, TagConstraintInput, getIndexId } from '../utils/artifactSearch.jsx';

const DEFAULT_TOOL_POLICY = {
  EnableToolCalls: false,
  EnableToolFeedbackEvents: true,
  ExposeToolTraceToUser: false,
  EnableCollectionSearchTool: false,
  EnableCollectionReadChunksTool: false,
  EnableCollectionEnumerateDocumentsTool: false,
  EnableVerbexFullTextSearchTool: false,
  EnableIndexEnumerateRecordsTool: false,
  EnableS3ObjectReadTool: false,
  EnableBucketEnumerateObjectsTool: false,
  EnableWebSearchTool: false,
  DocumentBackedObjectsOnly: true,
  RedactObjectKeys: true,
  RequireDocumentMapping: true,
  RequireSafeSearch: true,
  MaxToolIterations: 6,
  MaxToolCallsPerTurn: 12,
  MaxToolOutputChars: 12000,
  MaxToolOutputCharactersPerTurn: 50000,
  MaxSearchResultsPerCall: 10,
  MaxSearchTopK: 50,
  MaxSearchQueriesPerCall: 3,
  MaxDocumentsConsideredPerSearch: 1000,
  MaxResultsConsideredPerSearch: 1000,
  EnableServerGeneratedQueryVariants: false,
  ReturnFullSearchContent: false,
  MaxChunksPerRead: 20,
  MaxReadRangesPerCall: 5,
  MaxVerbexResults: 20,
  MaxObjectReadBytes: 131072,
  MaxBucketEnumerationResults: 50,
  MaxWebResults: 5,
  MaxWebSearchesPerTurn: 3,
  SearchDepth: 'basic',
  AllowedSearchModes: ['Vector', 'FullText', 'Hybrid']
};

function parseToolPolicyJson(json) {
  if (!json || !json.trim()) return { ...DEFAULT_TOOL_POLICY };

  try {
    const parsed = JSON.parse(json);
    return parsed && typeof parsed === 'object' && !Array.isArray(parsed)
      ? { ...DEFAULT_TOOL_POLICY, ...parsed }
      : { ...DEFAULT_TOOL_POLICY };
  } catch {
    return { ...DEFAULT_TOOL_POLICY };
  }
}

function formatPolicyList(value) {
  return Array.isArray(value) ? value.join(', ') : '';
}

function getEnumerationItems(result) {
  if (result && Array.isArray(result.Objects)) return result.Objects;
  if (result && Array.isArray(result.Data)) return result.Data;
  return Array.isArray(result) ? result : [];
}

function parsePolicyList(value) {
  if (!value || !value.trim()) return [];
  return value
    .split(',')
    .map(item => item.trim())
    .filter(Boolean)
    .filter((item, index, items) => items.findIndex(candidate => candidate.toLowerCase() === item.toLowerCase()) === index);
}

function parseFilterJson(json) {
  if (!json || !String(json).trim()) return {};
  try {
    const parsed = JSON.parse(json);
    return parsed && typeof parsed === 'object' && !Array.isArray(parsed) ? parsed : {};
  } catch {
    return {};
  }
}

function getFilterArray(filter, key) {
  if (!filter || typeof filter !== 'object') return [];
  const match = Object.keys(filter).find(candidate => candidate.toLowerCase() === key.toLowerCase());
  const value = match ? filter[match] : [];
  return Array.isArray(value) ? value : [];
}

function normalizeLabelRows(value) {
  const rows = Array.isArray(value) ? value.map(item => String(item ?? '')) : [];
  return rows.length > 0 ? rows : [''];
}

function normalizeTagRows(value) {
  const rows = Array.isArray(value)
    ? value.map(item => ({
      key: String(item?.key ?? item?.Key ?? ''),
      value: String(item?.value ?? item?.Value ?? ''),
      condition: String(item?.condition ?? item?.Condition ?? 'Equals')
    }))
    : [];
  return rows.length > 0 ? rows : [{ key: '', value: '', condition: 'Equals' }];
}

function parseRetrievalLabelFilterRows(json) {
  const filter = parseFilterJson(json);
  return {
    required: normalizeLabelRows(getFilterArray(filter, 'Required')),
    excluded: normalizeLabelRows(getFilterArray(filter, 'Excluded'))
  };
}

function parseRetrievalTagFilterRows(json) {
  const filter = parseFilterJson(json);
  return {
    required: normalizeTagRows(getFilterArray(filter, 'Required')),
    excluded: normalizeTagRows(getFilterArray(filter, 'Excluded'))
  };
}

function compactLabelRows(rows) {
  return normalizeLabelRows(rows)
    .map(item => item.trim())
    .filter(Boolean)
    .filter((item, index, items) => items.findIndex(candidate => candidate.toLowerCase() === item.toLowerCase()) === index);
}

function tagConditionNeedsValue(condition) {
  const normalized = String(condition || 'Equals').trim().toLowerCase();
  return normalized !== 'isnull' && normalized !== 'isnotnull';
}

function compactTagRows(rows) {
  return normalizeTagRows(rows)
    .map(item => {
      const key = item.key.trim();
      const value = item.value.trim();
      const condition = item.condition.trim() || 'Equals';
      if (!key) return null;
      if (tagConditionNeedsValue(condition) && !value) return null;
      return tagConditionNeedsValue(condition)
        ? { Key: key, Condition: condition, Value: value }
        : { Key: key, Condition: condition };
    })
    .filter(Boolean);
}

function serializeRetrievalLabelFilter(requiredRows, excludedRows) {
  const required = compactLabelRows(requiredRows);
  const excluded = compactLabelRows(excludedRows);
  const filter = {};
  if (required.length > 0) filter.Required = required;
  if (excluded.length > 0) filter.Excluded = excluded;
  return Object.keys(filter).length > 0 ? JSON.stringify(filter) : '';
}

function serializeRetrievalTagFilter(requiredRows, excludedRows) {
  const required = compactTagRows(requiredRows);
  const excluded = compactTagRows(excludedRows);
  const filter = {};
  if (required.length > 0) filter.Required = required;
  if (excluded.length > 0) filter.Excluded = excluded;
  return Object.keys(filter).length > 0 ? JSON.stringify(filter) : '';
}

function getEndpointField(endpoint, ...keys) {
  if (!endpoint) return undefined;
  for (const key of keys) {
    if (endpoint[key] !== undefined && endpoint[key] !== null) return endpoint[key];
  }
  return undefined;
}

function getEndpointId(endpoint) {
  return getEndpointField(endpoint, 'Id', 'id', 'ID');
}

function getEndpointText(endpoint, ...keys) {
  const value = getEndpointField(endpoint, ...keys);
  return value === undefined || value === null ? '' : String(value);
}

function getEndpointBoolean(endpoint, ...keys) {
  const value = getEndpointField(endpoint, ...keys);
  return coerceEndpointBoolean(value);
}

const TOOL_CALLING_LABEL = 'assistanthub:tool-calling';
const TOOL_TAG_SUPPORTS = 'AssistantHub.SupportsToolCalling';
const TOOL_TAG_FORMAT = 'AssistantHub.ToolCallingApiFormat';

function coerceEndpointBoolean(value) {
  if (typeof value === 'boolean') return value;
  if (typeof value === 'number') return value !== 0;
  if (typeof value === 'string') {
    const normalized = value.trim().toLowerCase();
    if (normalized === 'true' || normalized === 'yes' || normalized === '1') return true;
    if (normalized === 'false' || normalized === 'no' || normalized === '0' || normalized === '') return false;
  }
  return !!value;
}

function getEndpointTags(endpoint) {
  const value = getEndpointField(endpoint, 'Tags', 'tags');
  return value && typeof value === 'object' && !Array.isArray(value) ? value : {};
}

function getEndpointTag(endpoint, key) {
  const tags = getEndpointTags(endpoint);
  const match = Object.keys(tags).find(candidate => candidate.toLowerCase() === key.toLowerCase());
  return match ? tags[match] : undefined;
}

function endpointHasLabel(endpoint, label) {
  const labels = getEndpointField(endpoint, 'Labels', 'labels');
  return Array.isArray(labels) && labels.some(value => typeof value === 'string' && value.toLowerCase() === label.toLowerCase());
}

function endpointSupportsToolCalling(endpoint) {
  if (getEndpointBoolean(endpoint, 'SupportsToolCalling', 'supportsToolCalling')) return true;
  const tagValue = getEndpointTag(endpoint, TOOL_TAG_SUPPORTS);
  if (tagValue !== undefined && tagValue !== null) return coerceEndpointBoolean(tagValue);
  return endpointHasLabel(endpoint, TOOL_CALLING_LABEL);
}

function getEndpointToolCallingApiFormat(endpoint) {
  return getEndpointText(endpoint, 'ToolCallingApiFormat', 'toolCallingApiFormat') || getEndpointTag(endpoint, TOOL_TAG_FORMAT) || '';
}

function getBucketName(bucket) {
  return bucket?.Name || bucket?.name || bucket?.BucketName || bucket?.bucketName || bucket?.Id || bucket?.id || '';
}

function getIndexLabel(index) {
  const id = getIndexId(index);
  const name = index?.Name || index?.name || index?.Identifier || index?.identifier || id;
  return id && name && id !== name ? `${name} (${id})` : (name || id);
}

function buildSelectOptions(items, getValue, getLabel, currentValues = []) {
  const seen = new Set();
  const options = [];

  (items || []).forEach(item => {
    const value = getValue(item);
    if (!value || seen.has(value)) return;
    seen.add(value);
    options.push({ value, label: getLabel(item) || value });
  });

  (currentValues || []).forEach(value => {
    if (!value || seen.has(value)) return;
    seen.add(value);
    options.push({ value, label: value });
  });

  return options;
}

function getMultiSelectValues(event) {
  return Array.from(event.target.selectedOptions || [])
    .map(option => option.value)
    .filter(Boolean);
}

function AssistantSettingsView({ onOpenChatDrawer }) {
  const { serverUrl, credential } = useAuth();
  const [searchParams] = useSearchParams();
  const api = new ApiClient(serverUrl, credential?.BearerToken);
  const selectedEndpointDetailLoads = useRef(new Set());
  const [assistants, setAssistants] = useState([]);
  const [collections, setCollections] = useState([]);
  const [indices, setIndices] = useState([]);
  const [buckets, setBuckets] = useState([]);
  const [inferenceEndpoints, setInferenceEndpoints] = useState([]);
  const [embeddingEndpoints, setEmbeddingEndpoints] = useState([]);
  const [selectedId, setSelectedId] = useState('');
  const [settings, setSettings] = useState(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [alert, setAlert] = useState(null);
  const [dirty, setDirty] = useState(false);
  const [showJson, setShowJson] = useState(false);
  const [verifyingSlack, setVerifyingSlack] = useState(false);
  const [toolDescriptors, setToolDescriptors] = useState([]);
  const [loadingTools, setLoadingTools] = useState(false);
  const [validatingTools, setValidatingTools] = useState(false);
  const [testingTools, setTestingTools] = useState(false);
  const [externalSearchStatus, setExternalSearchStatus] = useState(null);

  const loadCollections = useCallback(async () => {
    try {
      const result = await api.getCollections({ maxResults: 1000 });
      const items = getEnumerationItems(result);
      setCollections(items);
    } catch (err) {
      console.error('Failed to load collections:', err);
    }
  }, [serverUrl, credential]);

  const loadIndices = useCallback(async () => {
    try {
      const result = await api.getIndices({ maxResults: 1000 });
      setIndices(getEnumerationItems(result));
    } catch (err) {
      console.error('Failed to load indices:', err);
      setIndices([]);
    }
  }, [serverUrl, credential]);

  const loadBuckets = useCallback(async () => {
    try {
      const result = await api.getBuckets({ maxResults: 1000 });
      setBuckets(getEnumerationItems(result));
    } catch (err) {
      console.error('Failed to load buckets:', err);
      setBuckets([]);
    }
  }, [serverUrl, credential]);

  const loadAssistants = useCallback(async () => {
    try {
      const result = await api.getAssistants({ maxResults: 1000 });
      const items = (result && result.Objects) ? result.Objects : Array.isArray(result) ? result : [];
      setAssistants(items);
      const paramId = searchParams.get('assistantId');
      if (paramId && items.some(a => a.Id === paramId)) {
        setSelectedId(paramId);
        loadSettings(paramId);
      } else if (items.length === 1) {
        setSelectedId(items[0].Id);
        loadSettings(items[0].Id);
      }
    } catch (err) {
      console.error('Failed to load assistants:', err);
    }
  }, [serverUrl, credential, searchParams]);

  const loadEndpoints = useCallback(async () => {
    try {
      const [completionResult, embeddingResult] = await Promise.all([
        api.enumerateCompletionEndpoints({ maxResults: 1000 }),
        api.enumerateEmbeddingEndpoints({ maxResults: 1000 })
      ]);
      const completionItems = getEnumerationItems(completionResult);
      const embeddingItems = getEnumerationItems(embeddingResult);
      setInferenceEndpoints(completionItems);
      setEmbeddingEndpoints(embeddingItems);
    } catch (err) {
      console.error('Failed to load endpoints:', err);
    }
  }, [serverUrl, credential]);

  useEffect(() => {
    const endpointId = settings?.InferenceEndpointId;
    if (!endpointId || selectedEndpointDetailLoads.current.has(endpointId)) return;

    const selected = (inferenceEndpoints || []).find(ep => getEndpointId(ep) === endpointId);
    if (selected && endpointSupportsToolCalling(selected) && getEndpointToolCallingApiFormat(selected)) return;

    let cancelled = false;
    selectedEndpointDetailLoads.current.add(endpointId);

    const loadSelectedEndpointDetail = async () => {
      try {
        const endpoint = await api.getCompletionEndpoint(endpointId);
        if (cancelled || !endpoint) return;

        setInferenceEndpoints(prev => {
          const existingIndex = (prev || []).findIndex(ep => getEndpointId(ep) === endpointId);
          if (existingIndex < 0) return [...(prev || []), endpoint];

          const next = [...prev];
          next[existingIndex] = { ...next[existingIndex], ...endpoint };
          return next;
        });
      } catch (err) {
        console.warn('Failed to load selected completion endpoint details:', err);
      }
    };

    loadSelectedEndpointDetail();
    return () => { cancelled = true; };
  }, [settings?.InferenceEndpointId, inferenceEndpoints, serverUrl, credential]);

  const loadExternalSearchStatus = useCallback(async () => {
    try {
      const result = await api.getExternalSearchStatus();
      setExternalSearchStatus(result || null);
    } catch (err) {
      console.warn('Failed to load external-search status:', err);
      setExternalSearchStatus(null);
    }
  }, [serverUrl, credential]);

  useEffect(() => { loadAssistants(); loadCollections(); loadIndices(); loadBuckets(); loadEndpoints(); loadExternalSearchStatus(); }, [loadAssistants, loadCollections, loadIndices, loadBuckets, loadEndpoints, loadExternalSearchStatus]);

  const loadAssistantTools = useCallback(async (id) => {
    if (!id) { setToolDescriptors([]); return; }
    setLoadingTools(true);
    try {
      const result = await api.getAssistantTools(id);
      setToolDescriptors(Array.isArray(result) ? result : []);
    } catch (err) {
      console.error('Failed to load assistant tools:', err);
      setToolDescriptors([]);
    } finally {
      setLoadingTools(false);
    }
  }, [serverUrl, credential]);

  const loadSettings = useCallback(async (id) => {
    if (!id) {
      setSettings(null);
      setToolDescriptors([]);
      return;
    }
    setLoading(true);
    setLoadingTools(true);
    try {
      const [settingsResult, toolsResult] = await Promise.allSettled([
        api.getAssistantSettings(id),
        api.getAssistantTools(id)
      ]);
      if (settingsResult.status === 'rejected') throw settingsResult.reason;
      const result = settingsResult.value;
      setToolDescriptors(toolsResult.status === 'fulfilled' && Array.isArray(toolsResult.value) ? toolsResult.value : []);
      setSettings({
        Temperature: result?.Temperature ?? 0.7,
        TopP: result?.TopP ?? 1.0,
        SystemPrompt: result?.SystemPrompt || 'You are a helpful assistant. Use the provided context to answer questions accurately.',
        MaxTokens: result?.MaxTokens || 4096,
        ContextWindow: result?.ContextWindow || 8192,
        EnableRag: result?.EnableRag ?? false,
        EnableRetrievalGate: result?.EnableRetrievalGate ?? false,
        EnableCitations: result?.EnableCitations ?? false,
        CitationLinkMode: result?.CitationLinkMode || 'None',
        EnableDocumentAttachments: result?.EnableDocumentAttachments ?? false,
        DocumentAttachmentMaxCount: result?.DocumentAttachmentMaxCount ?? 10,
        ExposeDocumentSourceUrls: result?.ExposeDocumentSourceUrls ?? false,
        CollectionId: result?.CollectionId || '',
        RetrievalTopK: result?.RetrievalTopK || 5,
        RetrievalScoreThreshold: result?.RetrievalScoreThreshold ?? 0.7,
        RetrievalIncludeNeighbors: result?.RetrievalIncludeNeighbors ?? 0,
        SearchMode: result?.SearchMode || 'Vector',
        TextWeight: result?.TextWeight ?? 0.3,
        FullTextSearchType: result?.FullTextSearchType || 'TsRank',
        FullTextLanguage: result?.FullTextLanguage || 'english',
        FullTextNormalization: result?.FullTextNormalization ?? 32,
        FullTextMinimumScore: result?.FullTextMinimumScore ?? '',
        InferenceEndpointId: result?.InferenceEndpointId || '',
        RetrievalGateInferenceEndpointId: result?.RetrievalGateInferenceEndpointId || '',
        QueryRewriteInferenceEndpointId: result?.QueryRewriteInferenceEndpointId || '',
        RerankInferenceEndpointId: result?.RerankInferenceEndpointId || '',
        EmbeddingEndpointId: result?.EmbeddingEndpointId || '',
        LoadModelsOnChatOpen: result?.LoadModelsOnChatOpen ?? false,
        Title: result?.Title || '',
        LogoUrl: result?.LogoUrl || '',
        FaviconUrl: result?.FaviconUrl || '',
        Streaming: result?.Streaming ?? true,
        EnableQueryRewrite: result?.EnableQueryRewrite ?? false,
        QueryRewritePrompt: result?.QueryRewritePrompt || '',
        EnableReranking: result?.EnableReranking ?? false,
        RerankerTopK: result?.RerankerTopK ?? 5,
        RerankerScoreThreshold: result?.RerankerScoreThreshold ?? 3.0,
        RerankPrompt: result?.RerankPrompt || '',
        RetrievalLabelFilter: result?.RetrievalLabelFilter || '',
        RetrievalTagFilter: result?.RetrievalTagFilter || '',
        EnableSlack: result?.EnableSlack ?? false,
        SlackAppToken: result?.SlackAppToken || '',
        SlackBotToken: result?.SlackBotToken || '',
        SlackChannelId: result?.SlackChannelId || '',
        SlackMessagePrefix: result?.SlackMessagePrefix || '',
        ToolPolicyJson: result?.ToolPolicyJson || '',
      });
      setDirty(false);
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to load settings' });
      setSettings(null);
      setToolDescriptors([]);
    } finally {
      setLoading(false);
      setLoadingTools(false);
    }
  }, [serverUrl, credential]);

  const handleSelectAssistant = (e) => {
    const id = e.target.value;
    setSelectedId(id);
    loadSettings(id);
  };

  const handleChange = (field, value) => {
    setSettings(prev => ({ ...prev, [field]: value }));
    setDirty(true);
  };

  const handleRetrievalLabelFilterChange = (field, rows) => {
    setSettings(prev => {
      const current = parseRetrievalLabelFilterRows(prev?.RetrievalLabelFilter || '');
      const next = { ...current, [field]: rows };
      return {
        ...prev,
        RetrievalLabelFilter: serializeRetrievalLabelFilter(next.required, next.excluded)
      };
    });
    setDirty(true);
  };

  const handleRetrievalTagFilterChange = (field, rows) => {
    setSettings(prev => {
      const current = parseRetrievalTagFilterRows(prev?.RetrievalTagFilter || '');
      const next = { ...current, [field]: rows };
      return {
        ...prev,
        RetrievalTagFilter: serializeRetrievalTagFilter(next.required, next.excluded)
      };
    });
    setDirty(true);
  };

  const handleToolPolicyChange = (field, value) => {
    setSettings(prev => {
      const nextPolicy = parseToolPolicyJson(prev?.ToolPolicyJson || '');
      if ((typeof value === 'string' && !value.trim()) || (Array.isArray(value) && value.length === 0)) {
        delete nextPolicy[field];
      } else {
        nextPolicy[field] = value;
      }

      return {
        ...prev,
        ToolPolicyJson: JSON.stringify(nextPolicy, null, 2)
      };
    });
    setDirty(true);
  };

  const handleToolPolicyNumberChange = (field, value) => {
    if (value === '' || value === null || value === undefined) {
      handleToolPolicyChange(field, '');
      return;
    }

    const parsed = Number(value);
    if (!Number.isFinite(parsed)) return;
    handleToolPolicyChange(field, parsed);
  };

  const handleToolPolicyListChange = (field, value) => {
    handleToolPolicyChange(field, parsePolicyList(value));
  };

  const handleResetToolPolicyDisabled = () => {
    setSettings(prev => ({
      ...prev,
      ToolPolicyJson: JSON.stringify({ ...DEFAULT_TOOL_POLICY, EnableToolCalls: false }, null, 2)
    }));
    setDirty(true);
  };

  const handleSave = async () => {
    if (!selectedId || !settings) return;
    setSaving(true);
    try {
      const toolPolicyJson = settings.ToolPolicyJson?.trim() || '';
      if (toolPolicyJson) {
        JSON.parse(toolPolicyJson);
      }

      const payload = {
        ...settings,
        TextWeight: parseFloat(settings.TextWeight) || 0.3,
        FullTextNormalization: parseInt(settings.FullTextNormalization) || 32,
        FullTextMinimumScore: settings.FullTextMinimumScore === '' || settings.FullTextMinimumScore === null
          ? null
          : parseFloat(settings.FullTextMinimumScore),
        RetrievalGateInferenceEndpointId: settings.RetrievalGateInferenceEndpointId || null,
        QueryRewriteInferenceEndpointId: settings.QueryRewriteInferenceEndpointId || null,
        RerankInferenceEndpointId: settings.RerankInferenceEndpointId || null,
        EmbeddingEndpointId: settings.EmbeddingEndpointId || null,
        RerankerTopK: parseInt(settings.RerankerTopK) || 5,
        RerankerScoreThreshold: parseFloat(settings.RerankerScoreThreshold) || 3.0,
        DocumentAttachmentMaxCount: Math.min(100, Math.max(1, parseInt(settings.DocumentAttachmentMaxCount) || 10)),
        ToolPolicyJson: toolPolicyJson || null
      };
      await api.updateAssistantSettings(selectedId, payload);
      await loadAssistantTools(selectedId);
      setDirty(false);
      setAlert({ title: 'Success', message: 'Settings saved successfully.' });
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to save settings' });
    } finally {
      setSaving(false);
    }
  };

  const handleReset = () => {
    loadSettings(selectedId);
  };

  const handleValidateToolPolicy = async () => {
    if (!selectedId || !settings) return;
    setValidatingTools(true);
    try {
      const result = await api.validateAssistantToolPolicy(selectedId, {
        ToolPolicyJson: settings.ToolPolicyJson?.trim() || null
      });
      setToolDescriptors(Array.isArray(result?.Tools) ? result.Tools : []);
      const validationErrors = Array.isArray(result?.Errors) ? result.Errors : [];
      const validationCodes = Array.isArray(result?.ErrorCodes) ? result.ErrorCodes : [];
      const validationMessage = validationErrors.length > 0
        ? [
            validationErrors.join('\n'),
            validationCodes.length > 0 ? `Codes: ${validationCodes.join(', ')}` : ''
          ].filter(Boolean).join('\n')
        : (result?.Message || 'Tool policy is invalid.');
      setAlert({
        title: result?.Success ? 'Tool Policy Valid' : 'Tool Policy Invalid',
        message: result?.Success
          ? (result?.Message || 'Tool policy is valid.')
          : validationMessage
      });
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to validate tool policy' });
    } finally {
      setValidatingTools(false);
    }
  };

  const handleTestToolPolicy = async () => {
    if (!selectedId || !settings) return;
    setTestingTools(true);
    try {
      const result = await api.testAssistantToolPolicy(selectedId, {
        ToolPolicyJson: settings.ToolPolicyJson?.trim() || null
      });
      setToolDescriptors(Array.isArray(result?.Tools) ? result.Tools : []);
      const errors = Array.isArray(result?.Errors) ? result.Errors : [];
      const warnings = Array.isArray(result?.Warnings) ? result.Warnings : [];
      const codes = Array.isArray(result?.ErrorCodes) ? result.ErrorCodes : [];
      const available = Array.isArray(result?.Tools) ? result.Tools.filter(tool => tool.Available ?? tool.available).length : 0;
      const total = Array.isArray(result?.Tools) ? result.Tools.length : 0;
      const endpoint = result?.EndpointResolved
        ? `Endpoint: ${result.EndpointModel || 'configured model'} (${result.EndpointApiFormat || 'unknown format'}, tool calling ${result.EndpointSupportsToolCalling ? 'enabled' : 'disabled'})`
        : 'Endpoint: unresolved';
      const details = [
        result?.Message || (result?.Success ? 'Tool diagnostics passed.' : 'Tool diagnostics failed.'),
        endpoint,
        `Tools: ${available}/${total} available`,
        warnings.length > 0 ? `Warnings:\n${warnings.join('\n')}` : '',
        errors.length > 0 ? `Errors:\n${errors.join('\n')}` : '',
        codes.length > 0 ? `Codes: ${codes.join(', ')}` : ''
      ].filter(Boolean).join('\n');
      setAlert({
        title: result?.Success ? 'Tool Diagnostics Passed' : 'Tool Diagnostics Failed',
        message: details
      });
    } catch (err) {
      setAlert({ title: 'Error', message: err.message || 'Failed to run tool diagnostics' });
    } finally {
      setTestingTools(false);
    }
  };

  const handleVerifySlack = async () => {
    if (!selectedId || !settings) return;
    setVerifyingSlack(true);
    try {
      const result = await api.verifyAssistantSlackSettings(selectedId, {
        EnableSlack: settings.EnableSlack,
        SlackAppToken: settings.SlackAppToken,
        SlackBotToken: settings.SlackBotToken,
        SlackChannelId: settings.SlackChannelId,
        SlackMessagePrefix: settings.SlackMessagePrefix
      });
      const checks = [
        { label: 'Bot token', ...result?.BotToken },
        { label: 'Channel', ...result?.Channel },
        { label: 'Socket Mode', ...result?.SocketMode }
      ];
      setAlert({
        title: result?.Success ? 'Slack Verified' : 'Slack Verification Failed',
        extraWide: true,
        content: (
          <div style={{ display: 'grid', gap: '0.75rem', width: '100%', maxWidth: '100%', overflowX: 'hidden' }}>
            {checks.map((check) => {
              const success = !!check.Success;
              return (
                <div
                  key={check.label}
                  style={{
                    display: 'grid',
                    gridTemplateColumns: '20px 120px minmax(0, 1fr)',
                    alignItems: 'start',
                    columnGap: '0.75rem',
                    textAlign: 'left'
                  }}
                >
                  <span
                    aria-hidden="true"
                    style={{
                      color: success ? 'var(--success, #16a34a)' : 'var(--danger, #dc2626)',
                      fontSize: '1rem',
                      lineHeight: 1,
                      textAlign: 'center'
                    }}
                  >
                    {success ? '✓' : '✕'}
                  </span>
                  <span style={{ fontWeight: 600, color: 'var(--text-primary)', textAlign: 'left', justifySelf: 'start' }}>{check.label}</span>
                  <span style={{ color: 'var(--text-secondary)', textAlign: 'left', justifySelf: 'start', whiteSpace: 'normal', wordBreak: 'break-word' }}>{check.Message || (success ? 'OK' : 'Failed')}</span>
                </div>
              );
            })}
          </div>
        )
      });
    } catch (err) {
      setAlert({ title: 'Slack Verification Failed', message: err.message || 'Failed to verify Slack settings' });
    } finally {
      setVerifyingSlack(false);
    }
  };

  const selectedAssistant = assistants.find(a => a.Id === selectedId);
  const formatInferenceEndpointLabel = (endpoint) => {
    const name = getEndpointText(endpoint, 'Name', 'name') || getEndpointText(endpoint, 'Model', 'model') || getEndpointId(endpoint);
    const model = getEndpointText(endpoint, 'Model', 'model');
    const baseName = model && model !== name ? `${name} (${model})` : name;
    const supportsTools = endpointSupportsToolCalling(endpoint);
    const toolFormat = getEndpointToolCallingApiFormat(endpoint);
    if (supportsTools) return `${baseName} - tools: ${toolFormat || 'configured'}`;
    return `${baseName} - no tools`;
  };
  const renderInferenceEndpointOptions = (fallbackLabel) => (
    <>
      <option value="">{fallbackLabel}</option>
      {(inferenceEndpoints || []).map(ep => (
        <option key={getEndpointId(ep)} value={getEndpointId(ep)}>{formatInferenceEndpointLabel(ep)}</option>
      ))}
    </>
  );
  const toolPolicy = parseToolPolicyJson(settings?.ToolPolicyJson || '');
  const retrievalLabelFilterRows = parseRetrievalLabelFilterRows(settings?.RetrievalLabelFilter || '');
  const retrievalTagFilterRows = parseRetrievalTagFilterRows(settings?.RetrievalTagFilter || '');
  const selectedAllowedIndexIds = Array.isArray(toolPolicy.AllowedVerbexIndexIds) ? toolPolicy.AllowedVerbexIndexIds : [];
  const selectedAllowedBucketNames = Array.isArray(toolPolicy.AllowedBucketNames) ? toolPolicy.AllowedBucketNames : [];
  const indexOptions = buildSelectOptions(
    indices,
    getIndexId,
    getIndexLabel,
    [toolPolicy.DefaultIndexId, ...selectedAllowedIndexIds].filter(Boolean)
  );
  const bucketOptions = buildSelectOptions(
    buckets,
    getBucketName,
    bucket => getBucketName(bucket),
    selectedAllowedBucketNames
  );
  const selectedInferenceEndpoint = inferenceEndpoints.find(ep => getEndpointId(ep) === settings?.InferenceEndpointId);
  const selectedEndpointToolCapable = endpointSupportsToolCalling(selectedInferenceEndpoint);
  const selectedEndpointToolFormat = getEndpointToolCallingApiFormat(selectedInferenceEndpoint);
  const collectionToolsEnabled = !!(toolPolicy.EnableCollectionSearchTool || toolPolicy.EnableCollectionReadChunksTool || toolPolicy.EnableCollectionEnumerateDocumentsTool);
  const verbexToolsEnabled = !!(toolPolicy.EnableVerbexFullTextSearchTool || toolPolicy.EnableVerbexSearchTool || toolPolicy.EnableIndexEnumerateRecordsTool || toolPolicy.EnableIndexEnumerationTool);
  const s3ToolsEnabled = !!(toolPolicy.EnableS3ObjectReadTool || toolPolicy.EnableBucketEnumerateObjectsTool);
  const unavailableEnabledTools = (toolDescriptors || [])
    .filter(tool => {
      const enabled = !!(tool.EnabledByPolicy ?? tool.enabledByPolicy);
      const available = !!(tool.Available ?? tool.available);
      return enabled && !available;
    })
    .map(tool => ({
      name: tool.DisplayName || tool.displayName || tool.ToolName || tool.toolName || 'Tool',
      reason: tool.UnavailableReason || tool.unavailableReason || 'Prerequisites are not satisfied.'
    }));

  return (
    <div>
      <div className="content-header">
        <div>
          <h1 className="content-title">Assistant Settings</h1>
          <p className="content-subtitle">Configure retrieval, prompts, and managed endpoint settings for each assistant.</p>
        </div>
        {selectedId && (
          <div style={{ display: 'flex', gap: '8px' }}>
            {onOpenChatDrawer && (
              <button className="btn btn-secondary" onClick={() => onOpenChatDrawer(selectedId)}>
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" style={{ marginRight: 6, verticalAlign: 'text-bottom' }}>
                  <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>
                </svg>
                Test Chat
              </button>
            )}
            <a href={`/chat/${selectedId}`} target="_blank" rel="noopener noreferrer" className="btn btn-primary">Launch Chat</a>
          </div>
        )}
      </div>
      <div className="settings-view">
        <div className="form-group">
          <label className="form-label"><Tooltip text="Choose which assistant's settings to configure">Select Assistant</Tooltip></label>
          <select
            className="form-input"
            value={selectedId}
            onChange={handleSelectAssistant}
          >
            <option value="">-- Select an assistant --</option>
            {assistants.map(a => (
              <option key={a.Id} value={a.Id}>{a.Name} ({a.Id.substring(0, 8)}...)</option>
            ))}
          </select>
        </div>

        {loading && (
          <div className="loading"><div className="spinner" /></div>
        )}

        {settings && !loading && (
          <div className="settings-form">
            {selectedAssistant && (
              <div className="settings-assistant-info">
                Editing settings for <strong>{selectedAssistant.Name}</strong>
              </div>
            )}
            <div className="settings-section">
              <h3 className="settings-section-title">Appearance</h3>
              <div className="form-group">
                <label className="form-label"><Tooltip text="Heading displayed at the top of the chat window">Title</Tooltip></label>
                <input className="form-input" type="text" title="Heading displayed at the top of the chat window" value={settings.Title} onChange={(e) => handleChange('Title', e.target.value)} placeholder="Heading shown on the chat window" />
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label"><Tooltip text="URL of the image shown in the chat header (max 192x192)">Logo URL</Tooltip></label>
                  <input className="form-input" type="text" title="URL of the image shown in the chat header (max 192x192)" value={settings.LogoUrl} onChange={(e) => handleChange('LogoUrl', e.target.value)} placeholder="Image URL for chat logo (max 192x192)" />
                </div>
                <div className="form-group">
                  <label className="form-label"><Tooltip text="URL of the icon shown in the browser tab">Favicon URL</Tooltip></label>
                  <input className="form-input" type="text" title="URL of the icon shown in the browser tab" value={settings.FaviconUrl} onChange={(e) => handleChange('FaviconUrl', e.target.value)} placeholder="Image URL for browser tab favicon" />
                </div>
              </div>
            </div>

            <div className="settings-section">
              <h3 className="settings-section-title">Endpoints</h3>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label"><Tooltip text="Managed completion endpoint used for assistant responses. The model configured on this endpoint is used unless a chat request explicitly overrides it.">Response Inference Endpoint</Tooltip></label>
                  <select className="form-input" value={settings.InferenceEndpointId} onChange={(e) => handleChange('InferenceEndpointId', e.target.value)}>
                    {renderInferenceEndpointOptions('-- Select an inference endpoint --')}
                  </select>
                </div>
                {(!settings || settings.SearchMode !== 'FullText') && (
                <div className="form-group">
                  <label className="form-label"><Tooltip text="Managed embedding endpoint used for vector and hybrid retrieval queries. Leave blank to use the server default.">Embedding Endpoint</Tooltip></label>
                  <select className="form-input" value={settings.EmbeddingEndpointId} onChange={(e) => handleChange('EmbeddingEndpointId', e.target.value)}>
                    <option value="">-- Use server default --</option>
                    {(embeddingEndpoints || []).map(ep => (
                      <option key={ep.Id} value={ep.Id}>{ep.Name || ep.Model || ep.Id}</option>
                    ))}
                  </select>
                </div>
                )}
              </div>
              <div className="form-row" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))' }}>
                <div className="form-group">
                  <label className="form-label"><Tooltip text="Inference endpoint used to decide whether a follow-up message needs new retrieval. Leave blank to use the response endpoint.">Retrieval Gate Endpoint</Tooltip></label>
                  <select className="form-input" value={settings.RetrievalGateInferenceEndpointId} onChange={(e) => handleChange('RetrievalGateInferenceEndpointId', e.target.value)}>
                    {renderInferenceEndpointOptions('-- Use response endpoint --')}
                  </select>
                </div>
                <div className="form-group">
                  <label className="form-label"><Tooltip text="Inference endpoint used to rewrite a user prompt into retrieval queries. Leave blank to use the response endpoint.">Query Rewrite Endpoint</Tooltip></label>
                  <select className="form-input" value={settings.QueryRewriteInferenceEndpointId} onChange={(e) => handleChange('QueryRewriteInferenceEndpointId', e.target.value)}>
                    {renderInferenceEndpointOptions('-- Use response endpoint --')}
                  </select>
                </div>
                <div className="form-group">
                  <label className="form-label"><Tooltip text="Inference endpoint used to score and filter retrieved chunks before context injection. Leave blank to use the response endpoint.">Re-Rank Endpoint</Tooltip></label>
                  <select className="form-input" value={settings.RerankInferenceEndpointId} onChange={(e) => handleChange('RerankInferenceEndpointId', e.target.value)}>
                    {renderInferenceEndpointOptions('-- Use response endpoint --')}
                  </select>
                </div>
              </div>
              <div className="form-group form-toggle">
                <label>
                  <input type="checkbox" checked={settings.LoadModelsOnChatOpen} onChange={(e) => handleChange('LoadModelsOnChatOpen', e.target.checked)} />
                  <Tooltip text="Load or warm the configured endpoint models when a chat window is opened.">Load models on chat open</Tooltip>
                </label>
              </div>
            </div>

            <div className="settings-section">
              <h3 className="settings-section-title">Inference Configuration</h3>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label"><Tooltip text="Controls randomness: lower values are more focused, higher values are more creative (0-2)">Temperature</Tooltip> <span className="range-value">{settings.Temperature}</span></label>
                  <input type="range" min="0" max="2" step="0.1" value={settings.Temperature} onChange={(e) => handleChange('Temperature', parseFloat(e.target.value))} />
                </div>
                <div className="form-group">
                  <label className="form-label"><Tooltip text="Nucleus sampling: limits token selection to a cumulative probability threshold (0-1)">Top P</Tooltip> <span className="range-value">{settings.TopP}</span></label>
                  <input type="range" min="0" max="1" step="0.05" value={settings.TopP} onChange={(e) => handleChange('TopP', parseFloat(e.target.value))} />
                </div>
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label"><Tooltip text="Maximum number of tokens the model can generate per response">Max Tokens</Tooltip></label>
                  <input className="form-input" type="number" title="Maximum number of tokens the model can generate per response" value={settings.MaxTokens} onChange={(e) => handleChange('MaxTokens', parseInt(e.target.value) || 0)} min="1" />
                </div>
                <div className="form-group">
                  <label className="form-label"><Tooltip text="Maximum number of tokens available for input context and conversation history">Context Window</Tooltip></label>
                  <input className="form-input" type="number" title="Maximum number of tokens available for input context and conversation history" value={settings.ContextWindow} onChange={(e) => handleChange('ContextWindow', parseInt(e.target.value) || 0)} min="1" />
                </div>
              </div>
              <div className="form-group form-toggle">
                <label>
                  <input type="checkbox" checked={settings.Streaming} onChange={(e) => handleChange('Streaming', e.target.checked)} />
                  <Tooltip text="Enable real-time token-by-token response streaming">Streaming</Tooltip>
                </label>
              </div>
            </div>

            <div className="settings-section">
              <h3 className="settings-section-title">System Prompt</h3>
              <div className="form-group">
                <label className="form-label"><Tooltip text="Instructions that define the assistant's behavior and personality">System Prompt</Tooltip></label>
                <textarea className="form-input" title="Instructions that define the assistant's behavior and personality" value={settings.SystemPrompt} onChange={(e) => handleChange('SystemPrompt', e.target.value)} rows={6} />
              </div>
            </div>

            <div className="settings-section">
              <h3 className="settings-section-title">Retrieval (RAG)</h3>
              <div className="form-group form-toggle">
                <label>
                  <input type="checkbox" checked={settings.EnableRag} onChange={(e) => handleChange('EnableRag', e.target.checked)} />
                  <Tooltip text="Enable Retrieval-Augmented Generation to use documents as context">Enable RAG</Tooltip>
                </label>
              </div>
              {settings.EnableRag && (
                <>
                  <div className="form-group form-toggle">
                    <label>
                      <input type="checkbox" checked={settings.EnableRetrievalGate} onChange={(e) => handleChange('EnableRetrievalGate', e.target.checked)} />
                      <Tooltip text="Use an LLM call to classify whether each message requires new retrieval or can be answered from existing conversation context. Skips retrieval for follow-up questions about already-retrieved data.">Enable Retrieval Gate</Tooltip>
                    </label>
                  </div>
                  <div className="form-group form-toggle">
                    <label>
                      <input type="checkbox" checked={settings.EnableCitations} onChange={(e) => handleChange('EnableCitations', e.target.checked)} />
                      <Tooltip text="Include citation metadata in chat responses. When enabled, the model is instructed to cite source documents using bracket notation [1], [2], and the response includes a citations object mapping references to source documents.">Include Citations</Tooltip>
                    </label>
                  </div>
                  {settings.EnableCitations && (
                    <div className="form-group">
                      <label className="form-label"><Tooltip text="Controls document download linking in citation cards. None: display-only. Authenticated: requires bearer token. Public: unauthenticated server-proxied download.">Citation Link Mode</Tooltip></label>
                      <select className="form-input" value={settings.CitationLinkMode} onChange={(e) => handleChange('CitationLinkMode', e.target.value)}>
                        <option value="None">None (display only)</option>
                        <option value="Authenticated">Authenticated (bearer token required)</option>
                        <option value="Public">Public (no authentication required)</option>
                      </select>
                    </div>
                  )}
                  <div className="form-group form-toggle">
                    <label>
                      <input type="checkbox" checked={settings.EnableQueryRewrite} onChange={(e) => handleChange('EnableQueryRewrite', e.target.checked)} />
                      <Tooltip text="When enabled, the user's prompt is rewritten into multiple semantically varied queries before retrieval, improving recall by capturing synonyms and alternate phrasing">Enable Query Rewrite</Tooltip>
                    </label>
                  </div>
                  {settings.EnableQueryRewrite && (
                    <div className="form-group">
                      <label className="form-label"><Tooltip text="The prompt sent to the LLM to rewrite the user's query. Must contain the {prompt} placeholder, which is replaced with the user's message at runtime. The LLM should return a newline-separated list of prompts including the original.">Query Rewrite Prompt</Tooltip></label>
                      <textarea
                        className="form-input"
                        title="The prompt sent to the LLM to rewrite the user's query. Must contain the {prompt} placeholder."
                        value={settings.QueryRewritePrompt}
                        onChange={(e) => handleChange('QueryRewritePrompt', e.target.value)}
                        rows={6}
                        placeholder="Leave empty to use the built-in default prompt. Custom prompts must include {prompt} as a placeholder for the user's message."
                      />
                    </div>
                  )}
                  <div className="form-group form-toggle">
                    <label>
                      <input type="checkbox" checked={settings.EnableReranking} onChange={(e) => handleChange('EnableReranking', e.target.checked)} />
                      <Tooltip text="When enabled, retrieved chunks are scored by an LLM for relevance to the query. Low-relevance chunks are filtered out before context injection, improving answer precision.">Enable Re-Ranking</Tooltip>
                    </label>
                  </div>
                  {settings.EnableReranking && (
                    <>
                      <div className="form-group">
                        <label className="form-label"><Tooltip text="Maximum number of chunks to keep after re-ranking. Should be less than or equal to Retrieval Top K.">Re-Ranker Top K</Tooltip></label>
                        <input className="form-input" type="number" title="Maximum number of chunks to keep after re-ranking. Should be less than or equal to Retrieval Top K." min="1" value={settings.RerankerTopK} onChange={(e) => handleChange('RerankerTopK', e.target.value)} />
                      </div>
                      <div className="form-group">
                        <label className="form-label"><Tooltip text="Minimum re-rank score (0-10) for a chunk to be included. Higher values mean stricter filtering.">Re-Ranker Score Threshold</Tooltip> <span className="range-value">{settings.RerankerScoreThreshold}</span></label>
                        <input type="range" min="0" max="10" step="0.5" value={settings.RerankerScoreThreshold} onChange={(e) => handleChange('RerankerScoreThreshold', parseFloat(e.target.value))} />
                      </div>
                      <div className="form-group">
                        <label className="form-label"><Tooltip text="The prompt sent to the LLM to score each chunk's relevance. Must contain {query} and {chunks} placeholders. Leave blank to use the built-in default.">Re-Rank Prompt</Tooltip></label>
                        <textarea
                          className="form-input"
                          title="The prompt sent to the LLM to score each chunk's relevance. Must contain {query} and {chunks} placeholders."
                          value={settings.RerankPrompt}
                          onChange={(e) => handleChange('RerankPrompt', e.target.value)}
                          rows={5}
                          placeholder="Leave blank to use built-in default re-rank prompt"
                        />
                      </div>
                    </>
                  )}
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Vector collection to search for relevant document chunks">Collection ID</Tooltip></label>
                    <select className="form-input" value={settings.CollectionId} onChange={(e) => handleChange('CollectionId', e.target.value)}>
                      <option value="">-- Select a collection --</option>
                      {collections.map(c => (
                        <option key={c.Id} value={c.Id}>{c.Name || c.Id}</option>
                      ))}
                    </select>
                  </div>
                  <div className="form-group form-toggle">
                    <label>
                      <input type="checkbox" checked={settings.EnableDocumentAttachments} onChange={(e) => handleChange('EnableDocumentAttachments', e.target.checked)} />
                      <Tooltip text="Allow public chat users to select completed documents from this assistant's collection and constrain retrieval to those documents.">Enable Document Attachments</Tooltip>
                    </label>
                  </div>
                  {settings.EnableDocumentAttachments && (
                    <div className="form-row">
                      <div className="form-group">
                        <label className="form-label"><Tooltip text="Maximum number of documents a chat user may attach to one request.">Attachment Limit</Tooltip></label>
                        <input className="form-input" type="number" title="Maximum number of documents a chat user may attach to one request." min="1" max="100" value={settings.DocumentAttachmentMaxCount} onChange={(e) => handleChange('DocumentAttachmentMaxCount', parseInt(e.target.value) || 10)} />
                      </div>
                      <div className="form-group form-toggle">
                        <label>
                          <input type="checkbox" checked={settings.ExposeDocumentSourceUrls} onChange={(e) => handleChange('ExposeDocumentSourceUrls', e.target.checked)} />
                          <Tooltip text="Include source URLs in public document-selection results. Leave off when source URLs may reveal internal paths or private crawl origins.">Expose Source URLs</Tooltip>
                        </label>
                      </div>
                    </div>
                  )}
                  <div className="form-row">
                    <div className="form-group">
                      <label className="form-label"><Tooltip text="Number of most relevant document chunks to retrieve per query">Retrieval Top K</Tooltip></label>
                      <input className="form-input" type="number" title="Number of most relevant document chunks to retrieve per query" value={settings.RetrievalTopK} onChange={(e) => handleChange('RetrievalTopK', parseInt(e.target.value) || 1)} min="1" />
                    </div>
                    <div className="form-group">
                      <label className="form-label"><Tooltip text="Minimum similarity score for retrieved chunks to be included (0-1)">Score Threshold</Tooltip> <span className="range-value">{settings.RetrievalScoreThreshold}</span></label>
                      <input type="range" min="0" max="1" step="0.05" value={settings.RetrievalScoreThreshold} onChange={(e) => handleChange('RetrievalScoreThreshold', parseFloat(e.target.value))} />
                    </div>
                    <div className="form-group">
                      <label className="form-label"><Tooltip text="Number of neighboring chunks to retrieve before and after each matched chunk (0-10). Provides surrounding context for each match. 0 means no neighbors.">Include Neighbors</Tooltip></label>
                      <input className="form-input" type="number" title="Number of neighboring chunks to retrieve before and after each matched chunk (0-10)." min="0" max="10" value={settings.RetrievalIncludeNeighbors} onChange={(e) => handleChange('RetrievalIncludeNeighbors', parseInt(e.target.value) || 0)} placeholder="0" />
                    </div>
                  </div>
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="How documents are retrieved: Vector (semantic similarity), FullText (keyword matching), or Hybrid (both combined)">Search Mode</Tooltip></label>
                    <select className="form-input" value={settings.SearchMode} onChange={(e) => handleChange('SearchMode', e.target.value)}>
                      <option value="Vector">Vector</option>
                      <option value="FullText">FullText</option>
                      <option value="Hybrid">Hybrid</option>
                    </select>
                  </div>
                  {settings.SearchMode === 'Hybrid' && (
                    <div className="form-group">
                      <label className="form-label"><Tooltip text="Balance between vector and text scoring in hybrid mode. 0.0 = pure vector, 1.0 = pure text. Recommended: 0.3 for quality embeddings">Text Weight</Tooltip> <span className="range-value">{settings.TextWeight}</span></label>
                      <input type="range" min="0" max="1" step="0.05" value={settings.TextWeight} onChange={(e) => handleChange('TextWeight', parseFloat(e.target.value))} />
                    </div>
                  )}
                  {(settings.SearchMode === 'FullText' || settings.SearchMode === 'Hybrid') && (
                    <>
                      <div className="form-row">
                        <div className="form-group">
                          <label className="form-label"><Tooltip text="TsRank: standard term frequency scoring. TsRankCd: cover density, rewards terms appearing close together">Full-Text Ranking</Tooltip></label>
                          <select className="form-input" value={settings.FullTextSearchType} onChange={(e) => handleChange('FullTextSearchType', e.target.value)}>
                            <option value="TsRank">TsRank</option>
                            <option value="TsRankCd">TsRankCd</option>
                          </select>
                        </div>
                        <div className="form-group">
                          <label className="form-label"><Tooltip text="Text search language for stemming and stop words. Use 'simple' to disable stemming">Language</Tooltip></label>
                          <select className="form-input" value={settings.FullTextLanguage} onChange={(e) => handleChange('FullTextLanguage', e.target.value)}>
                            <option value="english">english</option>
                            <option value="simple">simple</option>
                            <option value="spanish">spanish</option>
                            <option value="french">french</option>
                            <option value="german">german</option>
                          </select>
                        </div>
                      </div>
                      <div className="form-group">
                        <label className="form-label"><Tooltip text="Documents with text relevance below this threshold are excluded. Leave empty for no threshold">Minimum Text Score</Tooltip></label>
                        <input className="form-input" type="number" title="Documents with text relevance below this threshold are excluded. Leave empty for no threshold" min="0" max="1" step="0.05" value={settings.FullTextMinimumScore} onChange={(e) => handleChange('FullTextMinimumScore', e.target.value)} placeholder="Optional (0.0-1.0)" />
                      </div>
                    </>
                  )}
                  {/* Retrieval Filters */}
                  <div className="form-section-header" style={{ marginTop: '1.5rem' }}>
                    <Tooltip text="Filter retrieval to only return documents matching required labels and tags, and to exclude documents carrying specific labels or tags.">Retrieval Filters</Tooltip>
                  </div>
                  <div className="form-row">
                    <div className="form-group">
                      <label className="form-label">
                        <Tooltip text="Every retrieved document must contain each required label. Empty rows are ignored.">Required Labels</Tooltip>
                      </label>
                      <LabelConstraintInput
                        value={retrievalLabelFilterRows.required}
                        onChange={(rows) => handleRetrievalLabelFilterChange('required', rows)}
                        inputTitle="Required label value."
                        addTitle="Add required label"
                        deleteTitle="Delete required label"
                      />
                    </div>
                    <div className="form-group">
                      <label className="form-label">
                        <Tooltip text="Retrieved documents carrying any excluded label are filtered out. Empty rows are ignored.">Excluded Labels</Tooltip>
                      </label>
                      <LabelConstraintInput
                        value={retrievalLabelFilterRows.excluded}
                        onChange={(rows) => handleRetrievalLabelFilterChange('excluded', rows)}
                        inputTitle="Excluded label value."
                        addTitle="Add excluded label"
                        deleteTitle="Delete excluded label"
                      />
                    </div>
                  </div>
                  <div className="form-row">
                    <div className="form-group">
                      <label className="form-label">
                        <Tooltip text="Every retrieved document must match each required tag key/value pair. Empty rows are ignored.">Required Tags</Tooltip>
                      </label>
                      <TagConstraintInput
                        value={retrievalTagFilterRows.required}
                        onChange={(rows) => handleRetrievalTagFilterChange('required', rows)}
                        keyTitle="Required tag key."
                        valueTitle="Required tag value."
                        addTitle="Add required tag"
                        deleteTitle="Delete required tag"
                      />
                    </div>
                    <div className="form-group">
                      <label className="form-label">
                        <Tooltip text="Retrieved documents matching any excluded tag key/value pair are filtered out. Empty rows are ignored.">Excluded Tags</Tooltip>
                      </label>
                      <TagConstraintInput
                        value={retrievalTagFilterRows.excluded}
                        onChange={(rows) => handleRetrievalTagFilterChange('excluded', rows)}
                        keyTitle="Excluded tag key."
                        valueTitle="Excluded tag value."
                        addTitle="Add excluded tag"
                        deleteTitle="Delete excluded tag"
                      />
                    </div>
                  </div>
                </>
              )}
            </div>

            <div className="settings-section">
              <h3 className="settings-section-title">Tool Calls</h3>
              {toolPolicy.EnableToolCalls && (
                <div className="tool-policy-warning">
                  Public assistant chat can execute enabled read-only server tools. Keep scopes narrow and validate the effective tool list before launch.
                </div>
              )}
              {toolPolicy.EnableToolCalls && selectedInferenceEndpoint && !selectedEndpointToolCapable && (
                <div className="tool-policy-warning danger">
                  Selected completion endpoint is not marked tool-capable. Tool-call chat will fail until the endpoint advertises support.
                </div>
              )}
              {toolPolicy.EnableToolCalls && selectedEndpointToolCapable && !selectedEndpointToolFormat && (
                <div className="tool-policy-warning danger">
                  Selected completion endpoint is tool-capable but has no tool-call API format configured.
                </div>
              )}
              {collectionToolsEnabled && !settings.CollectionId && (
                <div className="tool-policy-warning">
                  Collection tools require an assistant collection. Select one in Retrieval before saving this policy.
                </div>
              )}
              {unavailableEnabledTools.length > 0 && (
                <div className="tool-policy-warning danger">
                  {unavailableEnabledTools.slice(0, 4).map(tool => `${tool.name}: ${tool.reason}`).join(' ')}
                  {unavailableEnabledTools.length > 4 ? ` ${unavailableEnabledTools.length - 4} more tool prerequisites are missing.` : ''}
                </div>
              )}

              <div className="tool-policy-subsection">
                <div className="tool-policy-subsection-header">
                  <span>Runtime</span>
                  <button className="btn btn-secondary btn-sm" type="button" onClick={handleResetToolPolicyDisabled}>Reset Disabled</button>
                </div>
                <div className="tool-policy-grid">
                  <label className="form-toggle">
                    <input
                      type="checkbox"
                      checked={!!toolPolicy.EnableToolCalls}
                      onChange={(e) => handleToolPolicyChange('EnableToolCalls', e.target.checked)}
                    />
                    <Tooltip text="Master switch allowing the model to request server-side tools exposed by this assistant policy. Existing assistants default to disabled.">Enable Tool Calls</Tooltip>
                  </label>
                  <label className="form-toggle">
                    <input
                      type="checkbox"
                      checked={toolPolicy.EnableToolFeedbackEvents !== false}
                      onChange={(e) => handleToolPolicyChange('EnableToolFeedbackEvents', e.target.checked)}
                    />
                    <Tooltip text="Emit safe browser/SSE status events while tools are running. Raw arguments and outputs are not exposed.">Tool Progress Events</Tooltip>
                  </label>
                  <label className="form-toggle">
                    <input
                      type="checkbox"
                      checked={!!toolPolicy.ExposeToolTraceToUser}
                      onChange={(e) => handleToolPolicyChange('ExposeToolTraceToUser', e.target.checked)}
                    />
                    <Tooltip text="Include safe tool_calls metadata in chat responses. Leave disabled for public assistants unless users need trace summaries.">Expose Safe Trace Metadata</Tooltip>
                  </label>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Maximum model/tool loop iterations for one chat turn.">Max Iterations</Tooltip></label>
                    <input className="form-input" type="number" title="Maximum model/tool loop iterations for one chat turn." min="1" max="20" value={toolPolicy.MaxToolIterations ?? 6} onChange={(e) => handleToolPolicyNumberChange('MaxToolIterations', e.target.value)} />
                  </div>
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Maximum individual tool calls for one chat turn.">Max Calls Per Turn</Tooltip></label>
                    <input className="form-input" type="number" title="Maximum individual tool calls for one chat turn." min="1" max="50" value={toolPolicy.MaxToolCallsPerTurn ?? 12} onChange={(e) => handleToolPolicyNumberChange('MaxToolCallsPerTurn', e.target.value)} />
                  </div>
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Maximum model-visible characters from one tool call.">Max Output Chars</Tooltip></label>
                    <input className="form-input" type="number" title="Maximum model-visible characters from one tool call." min="1024" max="200000" value={toolPolicy.MaxToolOutputChars ?? 12000} onChange={(e) => handleToolPolicyNumberChange('MaxToolOutputChars', e.target.value)} />
                  </div>
                </div>
              </div>

              <div className="tool-policy-subsection">
                <div className="tool-policy-subsection-header"><span>Collection</span></div>
                <div className="tool-policy-grid">
                  <label className="form-toggle">
                    <input type="checkbox" checked={!!toolPolicy.EnableCollectionSearchTool} onChange={(e) => handleToolPolicyChange('EnableCollectionSearchTool', e.target.checked)} />
                    <Tooltip text="Expose collection_search for bounded or exhaustive search of the assistant collection.">Search Collection</Tooltip>
                  </label>
                  <label className="form-toggle">
                    <input type="checkbox" checked={!!toolPolicy.EnableCollectionReadChunksTool} onChange={(e) => handleToolPolicyChange('EnableCollectionReadChunksTool', e.target.checked)} />
                    <Tooltip text="Expose collection_read_chunks for exact chunk reads by validated assistant document and position.">Read Chunks</Tooltip>
                  </label>
                  <label className="form-toggle">
                    <input type="checkbox" checked={!!toolPolicy.EnableCollectionEnumerateDocumentsTool} onChange={(e) => handleToolPolicyChange('EnableCollectionEnumerateDocumentsTool', e.target.checked)} />
                    <Tooltip text="Expose collection_enumerate_documents for safe document discovery in the assistant collection.">List Documents</Tooltip>
                  </label>
                  <label className="form-toggle">
                    <input type="checkbox" checked={!!toolPolicy.EnableServerGeneratedQueryVariants} onChange={(e) => handleToolPolicyChange('EnableServerGeneratedQueryVariants', e.target.checked)} />
                    <Tooltip text="Allow AssistantHub to add deterministic punctuation and quote-normalized query variants within Max Queries.">Server Query Variants</Tooltip>
                  </label>
                  <label className="form-toggle">
                    <input type="checkbox" checked={!!toolPolicy.ReturnFullSearchContent} onChange={(e) => handleToolPolicyChange('ReturnFullSearchContent', e.target.checked)} />
                    <Tooltip text="Return full chunk content directly from collection_search. Leave disabled so search returns excerpts and the model uses collection_read_chunks for exact text.">Full Search Content</Tooltip>
                  </label>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Allowed collection search modes, comma-separated.">Search Modes</Tooltip></label>
                    <input className="form-input" type="text" title="Allowed collection search modes, comma-separated." value={formatPolicyList(toolPolicy.AllowedSearchModes)} onChange={(e) => handleToolPolicyListChange('AllowedSearchModes', e.target.value)} placeholder="Vector, FullText, Hybrid" />
                  </div>
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Maximum collection search results per tool call.">Max Results</Tooltip></label>
                    <input className="form-input" type="number" title="Maximum collection search results per tool call." min="1" max="100" value={toolPolicy.MaxSearchResultsPerCall ?? 10} onChange={(e) => handleToolPolicyNumberChange('MaxSearchResultsPerCall', e.target.value)} />
                  </div>
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Maximum collection top-k a model may request.">Max Top K</Tooltip></label>
                    <input className="form-input" type="number" title="Maximum collection top-k a model may request." min="1" max="100" value={toolPolicy.MaxSearchTopK ?? 50} onChange={(e) => handleToolPolicyNumberChange('MaxSearchTopK', e.target.value)} />
                  </div>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Maximum query variants in one collection search call.">Max Queries</Tooltip></label>
                    <input className="form-input" type="number" title="Maximum query variants in one collection search call." min="1" max="20" value={toolPolicy.MaxSearchQueriesPerCall ?? 3} onChange={(e) => handleToolPolicyNumberChange('MaxSearchQueriesPerCall', e.target.value)} />
                  </div>
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Maximum assistant-visible documents a collection search may consider before it narrows the search and marks exhaustive results incomplete.">Max Docs Considered</Tooltip></label>
                    <input className="form-input" type="number" title="Maximum assistant-visible documents a collection search may consider before it narrows the search and marks exhaustive results incomplete." min="1" max="10000" value={toolPolicy.MaxDocumentsConsideredPerSearch ?? 1000} onChange={(e) => handleToolPolicyNumberChange('MaxDocumentsConsideredPerSearch', e.target.value)} />
                  </div>
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Maximum raw retrieval results collection_search may consider across all search passes.">Max Results Considered</Tooltip></label>
                    <input className="form-input" type="number" title="Maximum raw retrieval results collection_search may consider across all search passes." min="1" max="10000" value={toolPolicy.MaxResultsConsideredPerSearch ?? 1000} onChange={(e) => handleToolPolicyNumberChange('MaxResultsConsideredPerSearch', e.target.value)} />
                  </div>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Maximum exact chunks returned by one read call.">Max Chunks Read</Tooltip></label>
                    <input className="form-input" type="number" title="Maximum exact chunks returned by one read call." min="1" max="100" value={toolPolicy.MaxChunksPerRead ?? 20} onChange={(e) => handleToolPolicyNumberChange('MaxChunksPerRead', e.target.value)} />
                  </div>
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Maximum range entries accepted by one chunk read call.">Max Read Ranges</Tooltip></label>
                    <input className="form-input" type="number" title="Maximum range entries accepted by one chunk read call." min="1" max="50" value={toolPolicy.MaxReadRangesPerCall ?? 5} onChange={(e) => handleToolPolicyNumberChange('MaxReadRangesPerCall', e.target.value)} />
                  </div>
                </div>
              </div>

              <div className="tool-policy-subsection">
                <div className="tool-policy-subsection-header"><span>Verbex</span></div>
                <div className="tool-policy-grid">
                  <label className="form-toggle">
                    <input type="checkbox" checked={!!toolPolicy.EnableVerbexFullTextSearchTool} onChange={(e) => handleToolPolicyChange('EnableVerbexFullTextSearchTool', e.target.checked)} />
                    <Tooltip text="Expose verbex_full_text_search for lexical, exact phrase, and identifier-oriented search.">Full-Text Search</Tooltip>
                  </label>
                  <label className="form-toggle">
                    <input type="checkbox" checked={!!toolPolicy.EnableIndexEnumerateRecordsTool} onChange={(e) => handleToolPolicyChange('EnableIndexEnumerateRecordsTool', e.target.checked)} />
                    <Tooltip text="Expose index_enumerate_records for safe Verbex record discovery mapped to assistant documents.">List Records</Tooltip>
                  </label>
                  <label className="form-toggle">
                    <input type="checkbox" checked={toolPolicy.RequireDocumentMapping !== false} onChange={(e) => handleToolPolicyChange('RequireDocumentMapping', e.target.checked)} />
                    <Tooltip text="Require Verbex records to map back to assistant documents before the model can see them.">Require Document Mapping</Tooltip>
                  </label>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Optional default Verbex index ID override.">Default Index ID</Tooltip></label>
                    <select
                      className="form-input"
                      value={toolPolicy.DefaultIndexId || ''}
                      onChange={(e) => handleToolPolicyChange('DefaultIndexId', e.target.value)}
                      title="Optional default Verbex index ID override."
                    >
                      <option value="">Use assistant or tenant default</option>
                      {indexOptions.map(option => (
                        <option key={option.value} value={option.value}>{option.label}</option>
                      ))}
                    </select>
                  </div>
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Allowed Verbex index IDs. Empty uses assistant/tenant mapping.">Allowed Index IDs</Tooltip></label>
                    <select
                      className="form-input"
                      multiple
                      size={Math.min(Math.max(indexOptions.length, 3), 6)}
                      value={selectedAllowedIndexIds}
                      onChange={(e) => handleToolPolicyChange('AllowedVerbexIndexIds', getMultiSelectValues(e))}
                      title="Allowed Verbex index IDs. Empty uses assistant/tenant mapping."
                    >
                      {indexOptions.map(option => (
                        <option key={option.value} value={option.value}>{option.label}</option>
                      ))}
                    </select>
                  </div>
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Maximum Verbex result count.">Max Verbex Results</Tooltip></label>
                    <input className="form-input" type="number" title="Maximum Verbex result count." min="1" max="100" value={toolPolicy.MaxVerbexResults ?? 20} onChange={(e) => handleToolPolicyNumberChange('MaxVerbexResults', e.target.value)} />
                  </div>
                </div>
              </div>

              <div className="tool-policy-subsection">
                <div className="tool-policy-subsection-header"><span>S3 Objects</span></div>
                <div className="tool-policy-grid">
                  <label className="form-toggle">
                    <input type="checkbox" checked={!!toolPolicy.EnableS3ObjectReadTool} onChange={(e) => handleToolPolicyChange('EnableS3ObjectReadTool', e.target.checked)} />
                    <Tooltip text="Expose s3_object_read for bounded reads of document-backed or explicitly allowed bucket objects.">Read Objects</Tooltip>
                  </label>
                  <label className="form-toggle">
                    <input type="checkbox" checked={!!toolPolicy.EnableBucketEnumerateObjectsTool} onChange={(e) => handleToolPolicyChange('EnableBucketEnumerateObjectsTool', e.target.checked)} />
                    <Tooltip text="Expose bucket_enumerate_objects for explicitly allowed bucket/prefix enumeration.">List Bucket Objects</Tooltip>
                  </label>
                  <label className="form-toggle">
                    <input type="checkbox" checked={toolPolicy.DocumentBackedObjectsOnly !== false} onChange={(e) => handleToolPolicyChange('DocumentBackedObjectsOnly', e.target.checked)} />
                    <Tooltip text="Keep reads limited to objects backing assistant documents. Disable only with explicit bucket-wide opt-in and prefixes.">Document-Backed Only</Tooltip>
                  </label>
                  <label className="form-toggle">
                    <input type="checkbox" checked={!!toolPolicy.AllowBucketWideObjectRead} onChange={(e) => handleToolPolicyChange('AllowBucketWideObjectRead', e.target.checked)} />
                    <Tooltip text="Allow object_key reads outside known documents, still constrained by bucket and prefix allow-lists.">Bucket-Wide Reads</Tooltip>
                  </label>
                  <label className="form-toggle">
                    <input type="checkbox" checked={toolPolicy.RedactObjectKeys !== false} onChange={(e) => handleToolPolicyChange('RedactObjectKeys', e.target.checked)} />
                    <Tooltip text="Redact S3 object keys in model-visible output.">Redact Object Keys</Tooltip>
                  </label>
                  <label className="form-toggle">
                    <input type="checkbox" checked={!!toolPolicy.AllowBinaryObjectOutput} onChange={(e) => handleToolPolicyChange('AllowBinaryObjectOutput', e.target.checked)} />
                    <Tooltip text="Allow base64 output for binary object reads. Leave disabled for public assistants.">Allow Binary Output</Tooltip>
                  </label>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Allowed bucket names. Empty allows only the default storage bucket.">Allowed Buckets</Tooltip></label>
                    <select
                      className="form-input"
                      multiple
                      size={Math.min(Math.max(bucketOptions.length, 3), 6)}
                      value={selectedAllowedBucketNames}
                      onChange={(e) => handleToolPolicyChange('AllowedBucketNames', getMultiSelectValues(e))}
                      title="Allowed bucket names. Empty allows only the default storage bucket."
                    >
                      {bucketOptions.map(option => (
                        <option key={option.value} value={option.value}>{option.label}</option>
                      ))}
                    </select>
                  </div>
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Allowed object key prefixes, comma-separated. Required for bucket-wide reads and enumeration.">Allowed Prefixes</Tooltip></label>
                    <input className="form-input" type="text" title="Allowed object key prefixes, comma-separated. Required for bucket-wide reads and enumeration." value={formatPolicyList(toolPolicy.AllowedBucketPrefixes)} onChange={(e) => handleToolPolicyListChange('AllowedBucketPrefixes', e.target.value)} placeholder="documents/, public/" />
                  </div>
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Allowed object suffixes, comma-separated.">Allowed Suffixes</Tooltip></label>
                    <input className="form-input" type="text" title="Allowed object suffixes, comma-separated." value={formatPolicyList(toolPolicy.AllowedObjectSuffixes)} onChange={(e) => handleToolPolicyListChange('AllowedObjectSuffixes', e.target.value)} placeholder=".txt, .md, .pdf" />
                  </div>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Allowed content types, comma-separated.">Allowed Content Types</Tooltip></label>
                    <input className="form-input" type="text" title="Allowed content types, comma-separated." value={formatPolicyList(toolPolicy.AllowedContentTypes)} onChange={(e) => handleToolPolicyListChange('AllowedContentTypes', e.target.value)} placeholder="text/plain, application/pdf" />
                  </div>
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Maximum bytes returned by one object read.">Max Read Bytes</Tooltip></label>
                    <input className="form-input" type="number" title="Maximum bytes returned by one object read." min="1" max="10485760" value={toolPolicy.MaxObjectReadBytes ?? 131072} onChange={(e) => handleToolPolicyNumberChange('MaxObjectReadBytes', e.target.value)} />
                  </div>
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Maximum bucket enumeration results.">Max Enumeration Results</Tooltip></label>
                    <input className="form-input" type="number" title="Maximum bucket enumeration results." min="1" max="1000" value={toolPolicy.MaxBucketEnumerationResults ?? 50} onChange={(e) => handleToolPolicyNumberChange('MaxBucketEnumerationResults', e.target.value)} />
                  </div>
                </div>
              </div>

              <div className="tool-policy-subsection">
                <div className="tool-policy-subsection-header"><span>Web Search</span></div>
                <div className="tool-policy-grid">
                  <label className="form-toggle">
                    <input type="checkbox" checked={!!toolPolicy.EnableWebSearchTool} onChange={(e) => handleToolPolicyChange('EnableWebSearchTool', e.target.checked)} />
                    <Tooltip text="Expose Tavily-backed public web search to the model when tool calls are enabled and Tavily credentials are configured.">Tavily Web Search</Tooltip>
                  </label>
                  <label className="form-toggle">
                    <input type="checkbox" checked={toolPolicy.RequireSafeSearch !== false} onChange={(e) => handleToolPolicyChange('RequireSafeSearch', e.target.checked)} />
                    <Tooltip text="Force safe-search behavior on Tavily requests.">Require Safe Search</Tooltip>
                  </label>
                  <label className="form-toggle">
                    <input type="checkbox" checked={!!toolPolicy.AllowNewsTopic} onChange={(e) => handleToolPolicyChange('AllowNewsTopic', e.target.checked)} />
                    <Tooltip text="Allow Tavily news topic requests.">Allow News Topic</Tooltip>
                  </label>
                  <label className="form-toggle">
                    <input type="checkbox" checked={!!toolPolicy.AllowAdvancedSearchDepth} onChange={(e) => handleToolPolicyChange('AllowAdvancedSearchDepth', e.target.checked)} />
                    <Tooltip text="Allow Tavily advanced search depth.">Allow Advanced Depth</Tooltip>
                  </label>
                  <label className="form-toggle">
                    <input type="checkbox" checked={!!toolPolicy.AllowRawWebContent} onChange={(e) => handleToolPolicyChange('AllowRawWebContent', e.target.checked)} />
                    <Tooltip text="Allow raw Tavily result content when provider returns it.">Allow Raw Content</Tooltip>
                  </label>
                  <label className="form-toggle">
                    <input type="checkbox" checked={!!toolPolicy.AllowWebImages} onChange={(e) => handleToolPolicyChange('AllowWebImages', e.target.checked)} />
                    <Tooltip text="Allow Tavily image result URLs when returned.">Allow Images</Tooltip>
                  </label>
                </div>
                <div className="form-row">
                <div className="form-group">
                  <label className="form-label"><Tooltip text="Optional assistant-level Tavily search endpoint override. Leave blank to use the system-wide Tavily endpoint.">Tavily Endpoint</Tooltip></label>
                  <input
                    className="form-input"
                    type="text"
                    title="Optional assistant-level Tavily search endpoint override. Leave blank to use the system-wide Tavily endpoint."
                    value={toolPolicy.TavilyEndpoint || ''}
                    onChange={(e) => handleToolPolicyChange('TavilyEndpoint', e.target.value)}
                    placeholder="Use system-wide configuration"
                  />
                </div>
                <div className="form-group">
                  <label className="form-label"><Tooltip text="Optional assistant-level Tavily API key override. Leave blank to use the system-wide Tavily API key.">Tavily API Key</Tooltip></label>
                  <PasswordInput
                    value={toolPolicy.TavilyApiKey || ''}
                    onChange={(e) => handleToolPolicyChange('TavilyApiKey', e.target.value)}
                    placeholder="Use system-wide configuration"
                    title="Optional assistant-level Tavily API key override. Leave blank to use the system-wide Tavily API key."
                    autoComplete="new-password"
                    className="form-input"
                  />
                </div>
              </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Allowed web domains, comma-separated. Empty means no assistant-level allow-list.">Allowed Domains</Tooltip></label>
                    <input className="form-input" type="text" title="Allowed web domains, comma-separated. Empty means no assistant-level allow-list." value={formatPolicyList(toolPolicy.AllowedWebDomains)} onChange={(e) => handleToolPolicyListChange('AllowedWebDomains', e.target.value)} placeholder="example.com, docs.example.com" />
                  </div>
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Blocked web domains, comma-separated.">Blocked Domains</Tooltip></label>
                    <input className="form-input" type="text" title="Blocked web domains, comma-separated." value={formatPolicyList(toolPolicy.BlockedWebDomains)} onChange={(e) => handleToolPolicyListChange('BlockedWebDomains', e.target.value)} placeholder="blocked.example" />
                  </div>
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Allowed search providers, comma-separated. Use Tavily for first release.">Allowed Providers</Tooltip></label>
                    <input className="form-input" type="text" title="Allowed search providers, comma-separated. Use Tavily for first release." value={formatPolicyList(toolPolicy.AllowedProviders)} onChange={(e) => handleToolPolicyListChange('AllowedProviders', e.target.value)} placeholder="Tavily" />
                  </div>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Maximum Tavily results per call.">Max Web Results</Tooltip></label>
                    <input className="form-input" type="number" title="Maximum Tavily results per call." min="1" max="20" value={toolPolicy.MaxWebResults ?? 5} onChange={(e) => handleToolPolicyNumberChange('MaxWebResults', e.target.value)} />
                  </div>
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Maximum web_search calls in one chat turn.">Max Web Searches Per Turn</Tooltip></label>
                    <input className="form-input" type="number" title="Maximum web_search calls in one chat turn." min="1" max="50" value={toolPolicy.MaxWebSearchesPerTurn ?? 3} onChange={(e) => handleToolPolicyNumberChange('MaxWebSearchesPerTurn', e.target.value)} />
                  </div>
                  <div className="form-group">
                    <label className="form-label"><Tooltip text="Default Tavily search depth. Advanced is enforced only when allowed.">Search Depth</Tooltip></label>
                    <select className="form-input" value={toolPolicy.SearchDepth || 'basic'} onChange={(e) => handleToolPolicyChange('SearchDepth', e.target.value)}>
                      <option value="basic">basic</option>
                      <option value="advanced">advanced</option>
                    </select>
                  </div>
                </div>
                <div className="tool-policy-empty" style={{ marginBottom: '1rem' }}>
                  Blank Tavily endpoint or API key fields use the system-wide Tavily configuration.
                  {externalSearchStatus ? (
                    <span className={`tool-policy-status-inline ${(externalSearchStatus.ConfiguredProviders || externalSearchStatus.configuredProviders || 0) > 0 ? 'active' : 'warning'}`}>
                      System Tavily: {(externalSearchStatus.ConfiguredProviders || externalSearchStatus.configuredProviders || 0) > 0
                        ? 'configured'
                        : (externalSearchStatus.Enabled || externalSearchStatus.enabled) ? 'enabled but incomplete' : 'disabled'}
                    </span>
                  ) : (
                    <span className="tool-policy-status-inline warning">System Tavily status unavailable</span>
                  )}
                  If no complete system-wide or assistant-level configuration exists, Tavily web search will be unavailable and the server logs a warning.
                </div>
              </div>

              <div className="form-group">
                <label className="form-label"><Tooltip text="JSON policy controlling which server-side tools the model may request, including collection, Verbex, S3, and Tavily web-search tools.">Policy JSON</Tooltip></label>
                <textarea
                  className="form-input"
                  title="JSON policy controlling which server-side tools the model may request, including collection, Verbex, S3, and Tavily web-search tools."
                  value={settings.ToolPolicyJson}
                  onChange={(e) => handleChange('ToolPolicyJson', e.target.value)}
                  rows={10}
                  placeholder='{"EnableToolCalls":false,"EnableCollectionSearchTool":false,"EnableWebSearchTool":false}'
                />
              </div>
              <div className="form-group tool-policy-actions">
                <button className="btn btn-secondary" type="button" onClick={handleValidateToolPolicy} disabled={validatingTools || testingTools}>
                  {validatingTools ? 'Validating...' : 'Validate Policy'}
                </button>
                <button className="btn btn-secondary" type="button" onClick={handleTestToolPolicy} disabled={validatingTools || testingTools}>
                  {testingTools ? 'Running...' : 'Run Diagnostics'}
                </button>
              </div>
              <div className="tool-policy-preview">
                <div className="tool-policy-preview-header">
                  <span>Effective Tools</span>
                  <span className="status-badge info">{loadingTools ? 'Loading' : `${toolDescriptors.filter(tool => tool.Available).length}/${toolDescriptors.length} available`}</span>
                </div>
                {loadingTools ? (
                  <div className="tool-policy-empty">Loading tool availability...</div>
                ) : toolDescriptors.length === 0 ? (
                  <div className="tool-policy-empty">No tool descriptors available.</div>
                ) : (
                  <div className="tool-policy-list">
                    {toolDescriptors.map((tool) => {
                      const name = tool.ToolName || tool.toolName || 'unknown_tool';
                      const available = !!(tool.Available ?? tool.available);
                      const enabled = !!(tool.EnabledByPolicy ?? tool.enabledByPolicy);
                      const reason = tool.UnavailableReason || tool.unavailableReason;
                      return (
                        <div className="tool-policy-row" key={name}>
                          <div className="tool-policy-row-main">
                            <span className="tool-policy-name">{tool.DisplayName || tool.displayName || name}</span>
                            <span className="tool-policy-id">{name}</span>
                          </div>
                          <span className="tool-policy-category">{tool.Category || tool.category || 'Tool'}</span>
                          <span className={`status-badge ${available ? 'active' : enabled ? 'pending' : 'inactive'}`}>
                            {available ? 'Available' : enabled ? 'Unavailable' : 'Disabled'}
                          </span>
                          {reason && <span className="tool-policy-reason">{reason}</span>}
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>
            </div>

            <div className="settings-section">
              <h3 className="settings-section-title">Slack</h3>
              <div className="form-group form-toggle">
                <label>
                  <input type="checkbox" checked={settings.EnableSlack} onChange={(e) => handleChange('EnableSlack', e.target.checked)} />
                  <Tooltip text="Enable a per-assistant Slack Socket Mode connection. Messages in the configured channel trigger on the configured indicator or an @bot mention. Direct messages to the bot are also supported.">Enable Slack</Tooltip>
                </label>
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label"><Tooltip text="Slack app-level token used for Socket Mode. Must start with xapp-.">App Token</Tooltip></label>
                  <PasswordInput value={settings.SlackAppToken} onChange={(e) => handleChange('SlackAppToken', e.target.value)} placeholder="xapp-..." title="Slack app-level token used for Socket Mode. Must start with xapp-." autoComplete="new-password" className="form-input" />
                </div>
                <div className="form-group">
                  <label className="form-label"><Tooltip text="Slack bot token used for API calls and message delivery. Must start with xoxb-.">Bot Token</Tooltip></label>
                  <PasswordInput value={settings.SlackBotToken} onChange={(e) => handleChange('SlackBotToken', e.target.value)} placeholder="xoxb-..." title="Slack bot token used for API calls and message delivery. Must start with xoxb-." autoComplete="new-password" className="form-input" />
                </div>
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label"><Tooltip text="Slack channel ID for channel traffic. Direct messages are also supported when Slack is enabled.">Channel ID</Tooltip></label>
                  <input className="form-input" type="text" title="Slack channel ID for channel traffic. Direct messages are also supported when Slack is enabled." value={settings.SlackChannelId} onChange={(e) => handleChange('SlackChannelId', e.target.value)} placeholder="C..., G..., or similar" />
                </div>
                <div className="form-group">
                  <label className="form-label"><Tooltip text="Configured-channel messages trigger when they start with this indicator after leading whitespace normalization. An @bot mention also triggers the assistant.">Start-of-Message Indicator</Tooltip></label>
                  <input className="form-input" type="text" title="Configured-channel messages trigger when they start with this indicator after leading whitespace normalization. An @bot mention also triggers the assistant." value={settings.SlackMessagePrefix} onChange={(e) => handleChange('SlackMessagePrefix', e.target.value)} placeholder="Hey bot," />
                </div>
              </div>
              <div className="form-group">
                <button className="btn btn-secondary" type="button" onClick={handleVerifySlack} disabled={verifyingSlack}>
                  {verifyingSlack ? 'Verifying...' : 'Verify Connectivity'}
                </button>
              </div>
            </div>

            <div className="settings-actions">
              <button className="btn btn-secondary" onClick={() => setShowJson(true)}>View JSON</button>
              <button className="btn btn-secondary" onClick={handleReset} disabled={!dirty || saving}>Reset</button>
              <button className="btn btn-primary" onClick={handleSave} disabled={!dirty || saving || !settings.InferenceEndpointId}>
                {saving ? 'Saving...' : 'Save Settings'}
              </button>
            </div>
          </div>
        )}

        {!settings && !loading && selectedId && (
          <div className="empty-state"><p>No settings found for this assistant.</p></div>
        )}
      </div>
      {showJson && settings && <JsonViewModal title="Assistant Settings JSON" data={settings} onClose={() => setShowJson(false)} />}
      {alert && <AlertModal title={alert.title} message={alert.message} content={alert.content} wide={alert.wide} extraWide={alert.extraWide} onClose={() => setAlert(null)} />}
    </div>
  );
}

export default AssistantSettingsView;
