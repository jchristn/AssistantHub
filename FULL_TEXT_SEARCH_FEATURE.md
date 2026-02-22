# Full-Text Search and Hybrid Search — Developer Integration Guide

This guide is for developers building or maintaining applications that use RecallDB. It covers everything you need to upgrade an existing vector-only search integration to also use full-text search, hybrid search (vector + full-text combined), and the full range of filter composition.

---

## Table of Contents

1. [What Changed](#1-what-changed)
2. [Upgrade Steps](#2-upgrade-steps)
3. [Search Modes Overview](#3-search-modes-overview)
4. [Full-Text Search (Standalone)](#4-full-text-search-standalone)
5. [Hybrid Search (Vector + Full-Text)](#5-hybrid-search-vector--full-text)
6. [Combining Search with Filters](#6-combining-search-with-filters)
7. [Migrating from Vector-Only to Hybrid](#7-migrating-from-vector-only-to-hybrid)
8. [API Reference](#8-api-reference)
9. [SDK Examples](#9-sdk-examples)
10. [Scoring, Ranking, and Tuning](#10-scoring-ranking-and-tuning)
11. [Pagination](#11-pagination)
12. [Edge Cases and Gotchas](#12-edge-cases-and-gotchas)
13. [Recipes: Common Search Patterns](#13-recipes-common-search-patterns)
14. [Frequently Asked Questions](#14-frequently-asked-questions)

---

## 1. What Changed

RecallDB now supports three search modes through the **same endpoint**:

| Mode | When | What You Get |
|------|------|--------------|
| **Vector-only** | You send `Vector` without `FullText` | Semantic similarity ranking (existing behavior, unchanged) |
| **Full-text-only** | You send `FullText` without `Vector` | Lexical relevance ranking using PostgreSQL `ts_rank` |
| **Hybrid** | You send both `Vector` and `FullText` | A single blended score combining semantic + lexical relevance |

No new endpoints were added. The existing search endpoint now accepts an optional `FullText` object alongside the existing `Vector` object. All existing filters (`LabelFilter`, `TagFilter`, `Terms`, date ranges, `DocumentIds`) work with all three modes.

**Backward compatibility is complete.** If you send the same requests you send today (vector-only), you get the same results. The `FullText` property defaults to `null` and is ignored when absent.

---

## 2. Upgrade Steps

### 2.1 Server-Side

1. **Update your RecallDB server** to the version that includes full-text search support.
2. **No migration commands are needed.** The FTS index (`GIN tsvector`) is created automatically on server startup for all collections, including existing ones. The index is built from the existing `content` column — no data changes required.
3. **No configuration changes are needed.** FTS is available immediately on all collections.

### 2.2 Client-Side (SDKs)

- **C# SDK** — Update to the latest version. New model classes `FullTextQuery` and updated `SearchQuery`/`DocumentRecord` are included.
- **Python SDK** — Update to the latest version. The `search()` method accepts the new `FullText` dict parameter. No code changes required for existing vector-only calls.
- **JavaScript SDK** — Update to the latest version. The `search()` method accepts the new `FullText` object parameter. No code changes required for existing vector-only calls.

### 2.3 Verification

After upgrading, verify full-text search is working:

```bash
curl -X POST http://localhost:8600/v1.0/tenants/default/collections/default/search \
  -H "Authorization: Bearer default" \
  -H "Content-Type: application/json" \
  -d '{
    "FullText": {
      "Query": "test"
    },
    "MaxResults": 5
  }'
```

If you get a successful response (even with zero results), FTS is active. If the collection has documents containing the word "test", you should see results with `Score > 0` and `TextScore > 0`.

---

## 3. Search Modes Overview

### 3.1 How the Mode Is Determined

The search mode is determined entirely by which query objects you include in the request body:

```
┌─────────────────────────────────────────────────────┐
│  Request includes...     │  Search Mode              │
├──────────────────────────┼───────────────────────────┤
│  Vector only             │  Vector-only (unchanged)  │
│  FullText only           │  Full-text-only (new)     │
│  Vector + FullText       │  Hybrid (new)             │
│  Neither                 │  Unscored (returns all)   │
└─────────────────────────────────────────────────────┘
```

### 3.2 Score Semantics by Mode

| Field | Vector-Only | Full-Text-Only | Hybrid |
|-------|-------------|----------------|--------|
| `Score` | Vector similarity (0-1 for cosine) | Text relevance (0-1 with default normalization) | Weighted blend of both |
| `TextScore` | `null` / absent | Same as `Score` | Text relevance component only |
| `Distance` | Vector distance | `0.0` (placeholder) | Vector distance |

### 3.3 When to Use Each Mode

- **Vector-only**: Your application generates embeddings for the query and you want pure semantic search. Best for finding conceptually similar content even when wording differs.
- **Full-text-only**: You want to find documents containing specific terms, ranked by how relevant those terms are. No embeddings needed. Best for keyword-based retrieval, exact terminology matching, or when you don't have an embedding model available.
- **Hybrid**: You want the best of both — semantic understanding from vectors plus term-matching precision from full-text. Best for RAG pipelines where you need to catch both semantically similar and lexically relevant documents.

---

## 4. Full-Text Search (Standalone)

### 4.1 Minimal Example

```json
POST /v1.0/tenants/{tid}/collections/{cid}/search

{
  "FullText": {
    "Query": "OAuth2 PKCE configuration"
  },
  "MaxResults": 10
}
```

That's it. All other `FullText` properties have sensible defaults:
- `SearchType`: `"TsRank"` (standard relevance scoring)
- `Language`: `"english"` (stemming + stop word removal)
- `Normalization`: `32` (scores normalized to 0-1 range)
- `TextWeight`: `0.5` (only matters in hybrid mode)
- `MinimumScore`: `null` (no threshold)

### 4.2 How It Works

The query text is processed by PostgreSQL's `plainto_tsquery()` function:

1. Text is split on whitespace and punctuation.
2. Stop words are removed (e.g., "the", "is", "how", "to").
3. Remaining words are stemmed (e.g., "running" becomes "run", "configurations" becomes "configur").
4. Stemmed terms are joined with AND logic — all terms must be present in a document for it to match.

Examples of query processing:

| Input Query | After Processing | Matches Documents Containing |
|-------------|-----------------|------------------------------|
| `"OAuth2 PKCE configuration"` | `'oauth2' & 'pkce' & 'configur'` | All three stems |
| `"how to set up authentication"` | `'set' & 'authent'` | Both stems ("how", "to", "up" are stop words) |
| `"running tests"` | `'run' & 'test'` | Both stems |
| `"the and is"` | *(empty — all stop words)* | Nothing (zero results) |

### 4.3 Choosing a Ranking Function

| SearchType | Best For | Description |
|------------|----------|-------------|
| `"TsRank"` (default) | General-purpose retrieval | Scores based on term frequency with length normalization. A document mentioning "OAuth2" five times scores higher than one mentioning it once. |
| `"TsRankCd"` | Multi-term queries where proximity matters | Cover density ranking — additionally rewards documents where the query terms appear close together. If searching for "machine learning pipeline", a document with all three words in the same paragraph scores higher than one with the words scattered across sections. |

### 4.4 Response

```json
{
  "Success": true,
  "Documents": [
    {
      "DocumentKey": "doc_01JABC123",
      "DocumentId": "oauth2-reference",
      "Content": "OAuth2 PKCE Configuration Reference: This document covers...",
      "Score": 0.62,
      "TextScore": 0.62,
      "Distance": 0.0,
      "Labels": ["documentation"],
      "Tags": { "category": "auth" }
    }
  ],
  "TotalRecords": 1,
  "MaxResults": 10,
  "EndOfResults": true,
  "TotalMs": 8.12
}
```

In full-text-only mode, `Score` and `TextScore` are identical. `Distance` is `0.0` (not meaningful without a vector query).

---

## 5. Hybrid Search (Vector + Full-Text)

### 5.1 Basic Example

```json
{
  "Vector": {
    "SearchType": "CosineSimilarity",
    "Embeddings": [0.0123, -0.0456, 0.0789, ...]
  },
  "FullText": {
    "Query": "OAuth2 PKCE flow configuration"
  },
  "MaxResults": 10
}
```

With default `TextWeight` of `0.5`, the blended score is:

```
Score = 0.5 * cosine_similarity + 0.5 * ts_rank
```

### 5.2 How Hybrid Scoring Works

The hybrid score formula is:

```
Score = (1.0 - TextWeight) * vectorScore + TextWeight * textScore
```

Where:
- `vectorScore` = vector similarity score (e.g., cosine similarity, range 0-1)
- `textScore` = `ts_rank` or `ts_rank_cd` score (range 0-1 with default normalization of 32)
- `TextWeight` = the `FullText.TextWeight` value (default 0.5, clamped to 0.0-1.0)

| TextWeight | Effect |
|------------|--------|
| `0.0` | Pure vector scoring (text match still acts as a filter, but doesn't affect score) |
| `0.3` | 70% vector, 30% text — good starting point for RAG with quality embeddings |
| `0.5` | Equal weighting (default) |
| `0.7` | 30% vector, 70% text — use when exact terminology matters more than semantic meaning |
| `1.0` | Pure text scoring (vector distance still computed, but doesn't affect score) |

### 5.3 Critical Behavior: AND Semantics

**In hybrid mode, documents MUST match the full-text query to be returned.** The text query acts as both a filter and a scorer. Documents that are semantically similar (high vector score) but don't contain the search terms are excluded.

This means:
- Hybrid search returns a **subset** of what vector-only search would return (only the documents that also match the text query).
- If you need documents that match *either* vector similarity *or* text relevance (union behavior), issue two separate searches and merge client-side.

### 5.4 Client-Side Fusion Alternative

If AND semantics don't work for your use case, you can issue two separate searches and merge results yourself:

```python
# Search 1: Vector similarity
vector_results = client.search(tid, cid, {
    "Vector": {
        "SearchType": "CosineSimilarity",
        "Embeddings": embeddings
    },
    "MaxResults": 20
})

# Search 2: Full-text relevance
text_results = client.search(tid, cid, {
    "FullText": {
        "Query": "OAuth2 PKCE configuration"
    },
    "MaxResults": 20
})

# Merge: Reciprocal Rank Fusion (RRF)
def rrf_merge(vector_docs, text_docs, k=60):
    scores = {}
    for rank, doc in enumerate(vector_docs):
        key = doc["DocumentKey"]
        scores[key] = scores.get(key, 0) + 1.0 / (k + rank + 1)
    for rank, doc in enumerate(text_docs):
        key = doc["DocumentKey"]
        scores[key] = scores.get(key, 0) + 1.0 / (k + rank + 1)

    # Deduplicate and sort by RRF score
    all_docs = {d["DocumentKey"]: d for d in vector_docs + text_docs}
    return sorted(all_docs.values(), key=lambda d: scores[d["DocumentKey"]], reverse=True)

merged = rrf_merge(
    vector_results["Documents"],
    text_results["Documents"]
)
```

This gives you union behavior (documents matching either search appear in results) at the cost of two API calls.

### 5.5 Hybrid Response

```json
{
  "Success": true,
  "Documents": [
    {
      "DocumentKey": "doc_01JABC123",
      "DocumentId": "oauth2-reference",
      "Content": "OAuth2 PKCE Configuration Reference...",
      "Score": 0.75,
      "TextScore": 0.62,
      "Distance": 0.12,
      "Labels": ["documentation"],
      "Tags": { "category": "auth" }
    }
  ],
  "TotalRecords": 5,
  "MaxResults": 10,
  "EndOfResults": true,
  "TotalMs": 15.7
}
```

Here `Score` (0.75) is the blended score, `TextScore` (0.62) is the text-only component, and `Distance` (0.12) is the raw vector distance. You can use `TextScore` and `Distance` to understand how each component contributed.

---

## 6. Combining Search with Filters

All filters work with all three search modes. Filters are combined with AND logic — a document must satisfy the search query AND every filter to be returned.

### 6.1 Available Filters

| Filter | Purpose | Works With |
|--------|---------|------------|
| `LabelFilter` | Include/exclude documents by labels | All modes |
| `TagFilter` | Include/exclude documents by key-value tags | All modes |
| `Terms` | Binary substring matching (case-insensitive ILIKE) | All modes |
| `CreatedAfter` / `CreatedBefore` | Date range on document creation time | All modes |
| `DocumentIds` | Restrict to specific document IDs | All modes |
| `MinimumScore` / `MaximumScore` | Threshold on final blended `Score` | All modes |
| `MinimumDistance` / `MaximumDistance` | Threshold on vector `Distance` | Vector and Hybrid |

### 6.2 Understanding Terms vs. FullText

These serve different purposes and can be used together:

| Feature | `Terms` (existing) | `FullText` (new) |
|---------|---------------------|-------------------|
| **Purpose** | Binary yes/no filter | Ranked relevance search |
| **Matching** | Exact substring, case-insensitive (`ILIKE '%term%'`) | Stemmed token match with stop word removal |
| **Scoring** | None — just includes/excludes | `ts_rank` or `ts_rank_cd` score |
| **Example: query "running"** | Matches only documents containing the literal substring "running" | Matches documents containing "running", "runs", "ran", "runner" (stemmed to "run") |
| **Example: query "OAuth2"** | Matches documents containing exactly "OAuth2" (case-insensitive) | Matches documents containing "oauth2" (after stemming/normalization) |
| **Use when** | You need exact substring presence/absence | You want relevance-ranked results based on natural language |

**Using both together:** `FullText` provides ranking, `Terms` provides hard constraints. For example, search for documents relevant to "authentication setup" but that must contain the exact string "OAuth2" and must not contain "deprecated":

```json
{
  "FullText": {
    "Query": "authentication setup flow"
  },
  "Terms": {
    "Required": ["OAuth2"],
    "Excluded": ["deprecated"]
  },
  "MaxResults": 10
}
```

### 6.3 LabelFilter

```json
{
  "LabelFilter": {
    "Required": ["documentation", "v2"],
    "Excluded": ["draft", "archived"]
  }
}
```

- `Required`: Document must have ALL listed labels.
- `Excluded`: Document must have NONE of the listed labels.

### 6.4 TagFilter

Tags are key-value pairs with rich comparison operators.

```json
{
  "TagFilter": {
    "Required": [
      { "Key": "category", "Condition": "Equals", "Value": "auth" },
      { "Key": "version", "Condition": "GreaterThan", "Value": "2.0" },
      { "Key": "author", "Condition": "Contains", "Value": "smith" }
    ],
    "Excluded": [
      { "Key": "status", "Condition": "Equals", "Value": "deprecated" }
    ]
  }
}
```

Available tag conditions:

| Condition | Description |
|-----------|-------------|
| `Equals` | Exact match |
| `NotEquals` | Not equal |
| `GreaterThan` | String comparison: tag value > provided value |
| `LessThan` | String comparison: tag value < provided value |
| `Contains` | Tag value contains substring |
| `ContainsNot` | Tag value does not contain substring |
| `StartsWith` | Tag value starts with prefix |
| `EndsWith` | Tag value ends with suffix |
| `IsNull` | Tag value is null (Value field is ignored) |
| `IsNotNull` | Tag value is not null (Value field is ignored) |

- `Required`: ALL conditions must be satisfied.
- `Excluded`: NONE of the conditions may be satisfied.

### 6.5 Complete Filter Composition Example

Hybrid search with every filter type:

```json
{
  "Vector": {
    "SearchType": "CosineSimilarity",
    "Embeddings": [0.0123, -0.0456, 0.0789]
  },
  "FullText": {
    "Query": "authentication setup OAuth2",
    "SearchType": "TsRankCd",
    "TextWeight": 0.3,
    "MinimumScore": 0.01
  },
  "LabelFilter": {
    "Required": ["documentation"],
    "Excluded": ["draft"]
  },
  "TagFilter": {
    "Required": [
      { "Key": "category", "Condition": "Equals", "Value": "auth" },
      { "Key": "version", "Condition": "GreaterThan", "Value": "1.0" }
    ]
  },
  "Terms": {
    "Required": ["PKCE"],
    "Excluded": ["deprecated"]
  },
  "CreatedAfter": "2024-01-01T00:00:00Z",
  "CreatedBefore": "2025-12-31T23:59:59Z",
  "MaxResults": 20,
  "SortOrder": "ScoreDescending"
}
```

This query:
1. Finds documents matching the text query "authentication setup OAuth2" (stemmed, AND logic).
2. Ranks them using cover density ranking (TsRankCd) with 30% text weight and 70% vector weight.
3. Filters to documents labeled "documentation" but not "draft".
4. Filters to documents tagged `category=auth` and `version > 1.0`.
5. Requires the exact substring "PKCE" to be present, excludes documents containing "deprecated".
6. Limits to documents created in 2024-2025.
7. Returns up to 20 results, sorted by the blended hybrid score.

---

## 7. Migrating from Vector-Only to Hybrid

This section walks through converting an existing vector-only integration to hybrid search step by step.

### 7.1 Before: Vector-Only Search

```python
def search_documents(query_text, embeddings):
    """Existing vector-only search."""
    results = client.search(tenant_id, collection_id, {
        "Vector": {
            "SearchType": "CosineSimilarity",
            "Embeddings": embeddings
        },
        "MaxResults": 10
    })
    return results["Documents"]
```

### 7.2 Step 1: Add Full-Text as a Complement

The simplest change — add `FullText` alongside your existing `Vector`:

```python
def search_documents(query_text, embeddings):
    """Hybrid search — semantic + lexical."""
    results = client.search(tenant_id, collection_id, {
        "Vector": {
            "SearchType": "CosineSimilarity",
            "Embeddings": embeddings
        },
        "FullText": {
            "Query": query_text
        },
        "MaxResults": 10
    })
    return results["Documents"]
```

With this change:
- Results are ranked by a 50/50 blend of vector similarity and text relevance.
- Only documents matching the text query are returned (AND semantics).
- Each result has a `TextScore` field you can inspect.

### 7.3 Step 2: Tune the Text Weight

Start with `0.3` (70% vector / 30% text) if you have a good embedding model. Increase `TextWeight` if you find that exact keyword matches are being missed:

```python
"FullText": {
    "Query": query_text,
    "TextWeight": 0.3
}
```

### 7.4 Step 3: Handle the AND Semantics

Remember: hybrid mode only returns documents that match the text query. If your users enter short, specific queries (e.g., "OAuth2 PKCE"), this is usually what you want. But if queries are long and conversational (e.g., "How do I set up authentication with OAuth2 and PKCE in my application?"), many stop words get removed and the remaining terms all need to match, which can be overly restrictive.

Options:
- **Extract key terms** from the user's query before passing to `FullText.Query`. For example, use your LLM to extract 2-4 key terms from the user's question.
- **Fall back to vector-only** if hybrid returns zero results:

```python
def search_documents(query_text, embeddings, key_terms=None):
    """Hybrid search with vector-only fallback."""
    query = {
        "Vector": {
            "SearchType": "CosineSimilarity",
            "Embeddings": embeddings
        },
        "MaxResults": 10
    }

    # Try hybrid first with extracted key terms
    search_text = key_terms if key_terms else query_text
    query["FullText"] = {
        "Query": search_text,
        "TextWeight": 0.3
    }

    results = client.search(tenant_id, collection_id, query)

    # Fall back to vector-only if no hybrid results
    if results["TotalRecords"] == 0:
        del query["FullText"]
        results = client.search(tenant_id, collection_id, query)

    return results["Documents"]
```

### 7.5 Step 4: Use Client-Side Fusion for Union Behavior

If you need documents matching *either* vector OR text (not both), issue two searches:

```python
def search_documents_union(query_text, embeddings):
    """Two-search fusion — catches both semantic and lexical matches."""

    # Semantic search
    vector_results = client.search(tenant_id, collection_id, {
        "Vector": {
            "SearchType": "CosineSimilarity",
            "Embeddings": embeddings
        },
        "MaxResults": 15
    })

    # Lexical search
    text_results = client.search(tenant_id, collection_id, {
        "FullText": {
            "Query": query_text
        },
        "MaxResults": 15
    })

    # Deduplicate by DocumentKey, prefer higher score
    seen = {}
    for doc in vector_results["Documents"] + text_results["Documents"]:
        key = doc["DocumentKey"]
        if key not in seen or doc["Score"] > seen[key]["Score"]:
            seen[key] = doc

    # Sort by score descending, take top 10
    merged = sorted(seen.values(), key=lambda d: d["Score"], reverse=True)
    return merged[:10]
```

### 7.6 Step 5: Add Labels and Tags for Precision

If your documents have labels and tags, add them to narrow results:

```python
def search_documents(query_text, embeddings, category=None, labels=None):
    """Full hybrid search with filter composition."""
    query = {
        "Vector": {
            "SearchType": "CosineSimilarity",
            "Embeddings": embeddings
        },
        "FullText": {
            "Query": query_text,
            "TextWeight": 0.3
        },
        "MaxResults": 10
    }

    if labels:
        query["LabelFilter"] = {"Required": labels}

    if category:
        query["TagFilter"] = {
            "Required": [
                {"Key": "category", "Condition": "Equals", "Value": category}
            ]
        }

    results = client.search(tenant_id, collection_id, query)
    return results["Documents"]
```

### 7.7 Step 6: Handle the Response

Update your response handling to use the new `TextScore` field:

```python
for doc in results["Documents"]:
    print(f"Key: {doc['DocumentKey']}")
    print(f"  Hybrid Score: {doc['Score']:.4f}")
    print(f"  Text Score:   {doc.get('TextScore', 'N/A')}")
    print(f"  Distance:     {doc['Distance']:.4f}")
    print(f"  Content:      {doc['Content'][:100]}...")
```

In hybrid mode:
- `Score` is the blended score you should use for ranking.
- `TextScore` tells you how relevant the document is purely based on text matching.
- `Distance` tells you how far the document is in vector space.

You can use `TextScore` and `Distance` independently for debugging, re-ranking, or displaying relevance breakdowns to users.

---

## 8. API Reference

### 8.1 Endpoint

```
POST /v1.0/tenants/{tid}/collections/{cid}/search
```

Authentication: Bearer token or admin API key in the `Authorization` header.

### 8.2 SearchQuery Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Vector` | VectorQuery | `null` | Vector search parameters. When present (with embeddings), enables vector search. |
| `FullText` | FullTextQuery | `null` | Full-text search parameters. When present (with query text), enables text search. |
| `LabelFilter` | LabelFilter | `null` | Label-based inclusion/exclusion. |
| `TagFilter` | TagFilterSet | `null` | Tag-based inclusion/exclusion with rich comparison operators. |
| `Terms` | TermsFilter | `null` | Binary substring matching (case-insensitive). Not scored — only includes/excludes. |
| `CreatedAfter` | datetime | `null` | Only include documents created after this UTC timestamp. |
| `CreatedBefore` | datetime | `null` | Only include documents created before this UTC timestamp. |
| `DocumentIds` | string[] | `[]` | Restrict search to these document IDs only. |
| `MinimumScore` | double | `null` | Exclude results with final `Score` below this value. |
| `MaximumScore` | double | `null` | Exclude results with final `Score` above this value. |
| `MinimumDistance` | double | `null` | Exclude results with `Distance` below this value (vector modes only). |
| `MaximumDistance` | double | `null` | Exclude results with `Distance` above this value (vector modes only). |
| `MaxResults` | int | `10` | Results per page. Range: 1-1000. |
| `ContinuationToken` | string | `null` | Pagination token from a previous response. |
| `SortOrder` | string | `"ScoreDescending"` | Result ordering. See sort orders below. |

### 8.3 FullTextQuery Fields

| Field | Type | Default | Required | Description |
|-------|------|---------|----------|-------------|
| `Query` | string | — | Yes | The search text. Processed with stemming and stop word removal. Terms are AND-joined. |
| `SearchType` | string | `"TsRank"` | No | Ranking function: `"TsRank"` (term frequency) or `"TsRankCd"` (cover density, rewards proximity). |
| `Language` | string | `"english"` | No | PostgreSQL text search configuration. Controls stemming and stop words. Common values: `"english"`, `"simple"` (no stemming), `"spanish"`, `"french"`, `"german"`. |
| `Normalization` | int | `32` | No | Score normalization bitmask. `32` normalizes to 0-1 range (recommended). `0` = no normalization. `1` = divide by log(length). See PostgreSQL `ts_rank` docs for all values. |
| `MinimumScore` | double | `null` | No | Minimum text relevance threshold. Documents with `TextScore` below this are excluded. Applied post-query. |
| `TextWeight` | double | `0.5` | No | Weight for text score in hybrid mode (0.0 to 1.0). The vector weight is `1.0 - TextWeight`. Only affects scoring when both `Vector` and `FullText` are present. |

### 8.4 VectorQuery Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `SearchType` | string | `"CosineSimilarity"` | Distance metric. Options: `"CosineSimilarity"`, `"InnerProduct"`, `"L2"`, `"L1"`, `"Hamming"`. |
| `Embeddings` | float[] | `null` | Query vector. Must match the collection's embedding dimensionality. |
| `MinimumScore` | double | `null` | Minimum vector similarity score threshold. |
| `MaximumScore` | double | `null` | Maximum vector similarity score threshold. |
| `MinimumDistance` | double | `null` | Minimum vector distance threshold. |
| `MaximumDistance` | double | `null` | Maximum vector distance threshold. |

### 8.5 Sort Orders

| Value | Description |
|-------|-------------|
| `"ScoreDescending"` | Highest score first (default). Recommended for all search modes. |
| `"ScoreAscending"` | Lowest score first. |
| `"TextScoreDescending"` | Highest text relevance first. Useful when you want to sort by text score independent of the blended hybrid score. |
| `"TextScoreAscending"` | Lowest text relevance first. |
| `"DistanceAscending"` | Closest vector distance first. |
| `"DistanceDescending"` | Farthest vector distance first. |
| `"CreatedDescending"` | Newest documents first. |
| `"CreatedAscending"` | Oldest documents first. |

### 8.6 Response: SearchResult

| Field | Type | Description |
|-------|------|-------------|
| `Success` | bool | Whether the search completed successfully. |
| `Documents` | DocumentRecord[] | Array of matching documents. |
| `TotalRecords` | int | Total number of matching documents (across all pages). |
| `MaxResults` | int | The `MaxResults` value that was used. |
| `ContinuationToken` | string | Token for fetching the next page. `null` if no more pages. |
| `EndOfResults` | bool | `true` if this is the last page. |
| `RecordsRemaining` | int | Number of records remaining after this page. |
| `TotalMs` | double | Query execution time in milliseconds. |

### 8.7 Response: DocumentRecord

| Field | Type | Description |
|-------|------|-------------|
| `Id` | long | Internal database ID. |
| `DocumentKey` | string | Unique identifier within the collection. |
| `DocumentId` | string | Groups chunks of the same logical document. |
| `ContentLength` | long | Content length in bytes. |
| `Etag` | string | Entity tag for cache validation. |
| `Sha256` | string | SHA256 hash of content. |
| `Position` | int | Chunk position/index within the document. |
| `ContentType` | string | Content type: `"Text"`, `"List"`, `"Table"`, `"Binary"`, `"Image"`, `"Code"`, `"Hyperlink"`, `"Meta"`, `"Unknown"`. |
| `Content` | string | The text content. |
| `BinaryData` | byte[] | Binary data (if applicable). |
| `Embeddings` | float[] | The document's embedding vector. |
| `CreatedUtc` | datetime | Creation timestamp (UTC). |
| `Score` | double | Final relevance score. Meaning depends on search mode (vector similarity, text relevance, or hybrid blend). |
| `TextScore` | double or null | Full-text relevance score. Only populated when `FullText` is used. `null` in vector-only mode. |
| `Distance` | double | Vector distance. `0.0` in full-text-only mode. |
| `Labels` | string[] | Document labels. |
| `Tags` | object | Document tags as key-value pairs. |

---

## 9. SDK Examples

### 9.1 Python SDK

```python
from recalldb_sdk import RecallDbClient

client = RecallDbClient("http://localhost:8600", "your-api-key")
tid = "ten_default"
cid = "col_default"

# --- Full-text search only ---
results = client.search(tid, cid, {
    "FullText": {
        "Query": "OAuth2 PKCE flow configuration",
        "SearchType": "TsRank",
        "MinimumScore": 0.01
    },
    "MaxResults": 10
})

for doc in results["Documents"]:
    print(f"{doc['Score']:.4f}  {doc['DocumentKey']}  {doc['Content'][:80]}")

# --- Hybrid search ---
results = client.search(tid, cid, {
    "Vector": {
        "SearchType": "CosineSimilarity",
        "Embeddings": get_embeddings("OAuth2 PKCE flow configuration")
    },
    "FullText": {
        "Query": "OAuth2 PKCE flow configuration",
        "TextWeight": 0.3
    },
    "MaxResults": 10
})

for doc in results["Documents"]:
    print(f"Score={doc['Score']:.4f}  Text={doc.get('TextScore', 'N/A')}  {doc['DocumentKey']}")

# --- Hybrid with all filters ---
results = client.search(tid, cid, {
    "Vector": {
        "SearchType": "CosineSimilarity",
        "Embeddings": get_embeddings("database migration strategies")
    },
    "FullText": {
        "Query": "database migration",
        "SearchType": "TsRankCd",
        "TextWeight": 0.4,
        "MinimumScore": 0.01
    },
    "LabelFilter": {
        "Required": ["documentation"],
        "Excluded": ["archived"]
    },
    "TagFilter": {
        "Required": [
            {"Key": "category", "Condition": "Equals", "Value": "infrastructure"},
            {"Key": "version", "Condition": "GreaterThan", "Value": "2.0"}
        ]
    },
    "Terms": {
        "Required": ["PostgreSQL"],
        "Excluded": ["deprecated"]
    },
    "CreatedAfter": "2024-01-01T00:00:00Z",
    "MaxResults": 20,
    "SortOrder": "ScoreDescending"
})
```

### 9.2 JavaScript SDK

```javascript
import { RecallDbClient } from 'recalldb-sdk';

const client = new RecallDbClient('http://localhost:8600', 'your-api-key');
const tid = 'ten_default';
const cid = 'col_default';

// --- Full-text search only ---
const results = await client.search(tid, cid, {
  FullText: {
    Query: 'OAuth2 PKCE flow configuration',
    SearchType: 'TsRank',
    MinimumScore: 0.01
  },
  MaxResults: 10
});

for (const doc of results.Documents) {
  console.log(`${doc.Score.toFixed(4)}  ${doc.DocumentKey}  ${doc.Content.slice(0, 80)}`);
}

// --- Hybrid search ---
const hybridResults = await client.search(tid, cid, {
  Vector: {
    SearchType: 'CosineSimilarity',
    Embeddings: await getEmbeddings('OAuth2 PKCE flow configuration')
  },
  FullText: {
    Query: 'OAuth2 PKCE flow configuration',
    TextWeight: 0.3
  },
  MaxResults: 10
});

for (const doc of hybridResults.Documents) {
  console.log(`Score=${doc.Score.toFixed(4)}  Text=${doc.TextScore ?? 'N/A'}  ${doc.DocumentKey}`);
}

// --- Hybrid with all filters ---
const filtered = await client.search(tid, cid, {
  Vector: {
    SearchType: 'CosineSimilarity',
    Embeddings: await getEmbeddings('database migration strategies')
  },
  FullText: {
    Query: 'database migration',
    SearchType: 'TsRankCd',
    TextWeight: 0.4,
    MinimumScore: 0.01
  },
  LabelFilter: {
    Required: ['documentation'],
    Excluded: ['archived']
  },
  TagFilter: {
    Required: [
      { Key: 'category', Condition: 'Equals', Value: 'infrastructure' },
      { Key: 'version', Condition: 'GreaterThan', Value: '2.0' }
    ]
  },
  Terms: {
    Required: ['PostgreSQL'],
    Excluded: ['deprecated']
  },
  CreatedAfter: '2024-01-01T00:00:00Z',
  MaxResults: 20,
  SortOrder: 'ScoreDescending'
});
```

### 9.3 C# SDK

```csharp
using RecallDb.Sdk;

var client = new RecallDbClient("http://localhost:8600", "your-api-key");
var tid = "ten_default";
var cid = "col_default";

// --- Full-text search only ---
var results = await client.SearchAsync(tid, cid, new SearchQuery
{
    FullText = new FullTextQuery
    {
        Query = "OAuth2 PKCE flow configuration",
        SearchType = "TsRank",
        MinimumScore = 0.01
    },
    MaxResults = 10
});

foreach (var doc in results.Documents)
{
    Console.WriteLine($"{doc.Score:F4}  {doc.DocumentKey}  {doc.Content?[..Math.Min(80, doc.Content.Length)]}");
}

// --- Hybrid search ---
var hybridResults = await client.SearchAsync(tid, cid, new SearchQuery
{
    Vector = new VectorQuery
    {
        SearchType = "CosineSimilarity",
        Embeddings = GetEmbeddings("OAuth2 PKCE flow configuration")
    },
    FullText = new FullTextQuery
    {
        Query = "OAuth2 PKCE flow configuration",
        TextWeight = 0.3
    },
    MaxResults = 10
});

foreach (var doc in hybridResults.Documents)
{
    Console.WriteLine($"Score={doc.Score:F4}  Text={doc.TextScore?.ToString("F4") ?? "N/A"}  {doc.DocumentKey}");
}

// --- Hybrid with all filters ---
var filtered = await client.SearchAsync(tid, cid, new SearchQuery
{
    Vector = new VectorQuery
    {
        SearchType = "CosineSimilarity",
        Embeddings = GetEmbeddings("database migration strategies")
    },
    FullText = new FullTextQuery
    {
        Query = "database migration",
        SearchType = "TsRankCd",
        TextWeight = 0.4,
        MinimumScore = 0.01
    },
    LabelFilter = new LabelFilter
    {
        Required = new List<string> { "documentation" },
        Excluded = new List<string> { "archived" }
    },
    TagFilter = new TagFilterSet
    {
        Required = new List<TagFilter>
        {
            new TagFilter { Key = "category", Condition = "Equals", Value = "infrastructure" },
            new TagFilter { Key = "version", Condition = "GreaterThan", Value = "2.0" }
        }
    },
    Terms = new TermsFilter
    {
        Required = new List<string> { "PostgreSQL" },
        Excluded = new List<string> { "deprecated" }
    },
    CreatedAfter = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    MaxResults = 20,
    SortOrder = "ScoreDescending"
});
```

### 9.4 curl

```bash
# Full-text search only
curl -X POST http://localhost:8600/v1.0/tenants/default/collections/default/search \
  -H "Authorization: Bearer your-api-key" \
  -H "Content-Type: application/json" \
  -d '{
    "FullText": {
      "Query": "OAuth2 PKCE flow configuration",
      "SearchType": "TsRank",
      "MinimumScore": 0.01
    },
    "MaxResults": 10
  }'

# Hybrid search
curl -X POST http://localhost:8600/v1.0/tenants/default/collections/default/search \
  -H "Authorization: Bearer your-api-key" \
  -H "Content-Type: application/json" \
  -d '{
    "Vector": {
      "SearchType": "CosineSimilarity",
      "Embeddings": [0.0123, -0.0456, 0.0789]
    },
    "FullText": {
      "Query": "OAuth2 PKCE flow",
      "TextWeight": 0.3
    },
    "MaxResults": 10
  }'

# Hybrid with labels, tags, and terms
curl -X POST http://localhost:8600/v1.0/tenants/default/collections/default/search \
  -H "Authorization: Bearer your-api-key" \
  -H "Content-Type: application/json" \
  -d '{
    "Vector": {
      "SearchType": "CosineSimilarity",
      "Embeddings": [0.0123, -0.0456, 0.0789]
    },
    "FullText": {
      "Query": "database migration",
      "SearchType": "TsRankCd",
      "TextWeight": 0.4
    },
    "LabelFilter": {
      "Required": ["documentation"],
      "Excluded": ["archived"]
    },
    "TagFilter": {
      "Required": [
        { "Key": "category", "Condition": "Equals", "Value": "infrastructure" }
      ]
    },
    "Terms": {
      "Required": ["PostgreSQL"],
      "Excluded": ["deprecated"]
    },
    "CreatedAfter": "2024-01-01T00:00:00Z",
    "MaxResults": 20,
    "SortOrder": "ScoreDescending"
  }'
```

---

## 10. Scoring, Ranking, and Tuning

### 10.1 Understanding ts_rank Scores

PostgreSQL `ts_rank` with normalization `32` (the default) produces scores in the range **0.0 to just under 1.0**. The formula is `rank / (rank + 1)`, so:

- A score of `0.0` means no relevance (but the document matched — it passed the `@@` filter).
- Scores are relative, not absolute. A score of `0.5` doesn't mean "50% relevant" — it means this document is more relevant than ones scoring `0.3` for the same query.
- Scores are comparable within the same query but not across different queries.

### 10.2 Normalization Options

| Value | Effect | When to Use |
|-------|--------|-------------|
| `32` (default) | Normalizes to 0-1 range. Fair blending with vector scores. | Almost always. Required for meaningful hybrid scores. |
| `0` | No normalization. Raw term frequency. Unbounded scores. | Only use for full-text-only mode when you want raw TF scores. Will dominate hybrid scores. |
| `1` | Divide by `1 + log(document_length)`. | When you want to penalize long documents less aggressively than normalization `2`. |
| `2` | Divide by document length. | When short documents with high term density should score highest. |

**Recommendation:** Leave at `32` unless you have a specific reason to change it. It's the only normalization that produces scores on the same scale as vector similarity (0-1), which makes hybrid blending work correctly.

### 10.3 Tuning TextWeight for Hybrid Search

| Scenario | Recommended TextWeight | Rationale |
|----------|----------------------|-----------|
| High-quality embeddings (e.g., OpenAI `text-embedding-3-large`) | `0.2` - `0.3` | Trust the embeddings; use text as a supplemental signal. |
| General-purpose embeddings | `0.3` - `0.5` | Balanced blend. |
| Domain-specific terminology (medical, legal, code) | `0.5` - `0.7` | Exact term matches matter more than semantic similarity in specialized domains. |
| Keyword-heavy queries (model names, error codes, IDs) | `0.7` - `0.9` | These queries depend on exact matches, not semantics. |
| Debugging / exploring | `0.5` | Start here and adjust. |

### 10.4 Using MinimumScore Thresholds

There are two levels of score thresholds:

1. **`FullText.MinimumScore`** — Filters on the `TextScore` component only. Applied after scoring. In hybrid mode, this can exclude documents that have a good blended score but weak text relevance.

2. **`SearchQuery.MinimumScore`** — Filters on the final `Score` (blended in hybrid mode, text-only in full-text mode, vector in vector mode).

Example: You want documents with at least some text relevance AND a decent overall score:

```json
{
  "Vector": { "SearchType": "CosineSimilarity", "Embeddings": [...] },
  "FullText": {
    "Query": "machine learning",
    "TextWeight": 0.3,
    "MinimumScore": 0.01
  },
  "MinimumScore": 0.5,
  "MaxResults": 10
}
```

### 10.5 Choosing Sort Order in Hybrid Mode

- `"ScoreDescending"` (default) — Sorts by the blended hybrid score. Usually what you want.
- `"TextScoreDescending"` — Sorts by text relevance only, ignoring the vector component. Useful if you want to see which results are most lexically relevant regardless of semantic similarity.
- `"DistanceAscending"` — Sorts by vector distance, ignoring text relevance. Useful if you want the semantically closest results that also happen to match the text query (since the text filter still applies in hybrid mode).

---

## 11. Pagination

Pagination works identically across all search modes using `ContinuationToken`:

```python
all_documents = []
continuation_token = None

while True:
    query = {
        "FullText": {
            "Query": "machine learning"
        },
        "MaxResults": 50,
    }
    if continuation_token:
        query["ContinuationToken"] = continuation_token

    results = client.search(tid, cid, query)
    all_documents.extend(results["Documents"])

    if results["EndOfResults"]:
        break

    continuation_token = results["ContinuationToken"]

print(f"Total documents retrieved: {len(all_documents)}")
```

The same pattern works for hybrid search — just include both `Vector` and `FullText` in the query alongside `ContinuationToken`.

---

## 12. Edge Cases and Gotchas

### 12.1 All Query Terms Are Stop Words

If every word in `FullText.Query` is a stop word (e.g., "the and is but"), PostgreSQL produces an empty query and **zero results are returned**. The response will be `Success: true` with `TotalRecords: 0`.

**Mitigation:** If your application generates queries programmatically, ensure the query contains at least one meaningful content word.

### 12.2 Documents with NULL Content

Documents stored with `content = NULL` (e.g., binary-only documents) will **never** match a full-text query. This is correct behavior. If you have binary documents that should be text-searchable, store searchable metadata in the `content` field.

### 12.3 Language Mismatch

The FTS index is built with the `english` text search configuration. If you specify a different `Language` in your query (e.g., `"spanish"`), the query still works but **will not use the index** — PostgreSQL falls back to a sequential scan. This is fine for small collections but slow for large ones.

If your content is primarily in a non-English language, contact your RecallDB administrator about creating language-specific indexes.

### 12.4 TextWeight Is Clamped

`TextWeight` values outside `[0.0, 1.0]` are silently clamped. A `TextWeight` of `1.5` becomes `1.0`; a `TextWeight` of `-0.3` becomes `0.0`. No error is returned.

### 12.5 Neither Vector Nor FullText Provided

If you send a search request with no `Vector` and no `FullText`, you get back **all documents** (subject to any other filters), sorted by the specified `SortOrder`. The `Score` values will not be meaningful. This can be useful for browsing/filtering without ranking.

### 12.6 Hybrid Mode Returns Fewer Results Than Vector-Only

This is expected. Hybrid mode requires documents to match the text query (AND semantics). If a document is semantically similar but doesn't contain the query terms, it won't be returned in hybrid mode. If this is a problem:
- Use shorter, more targeted text queries.
- Use client-side fusion (Section 5.4) for union behavior.
- Fall back to vector-only when hybrid returns too few results (Section 7.4).

### 12.7 Score Values Differ Between Modes

The same document will have different `Score` values in vector-only mode vs. full-text-only mode vs. hybrid mode. This is expected — each mode computes `Score` differently. Don't compare `Score` values across different search modes.

### 12.8 TextScore Is Null in Vector-Only Mode

When you don't include `FullText` in the request, `TextScore` will be `null` (or absent from the JSON). Always check for `null`/`undefined` before using `TextScore` if your code can issue both vector-only and hybrid queries.

---

## 13. Recipes: Common Search Patterns

### 13.1 RAG Pipeline — Hybrid Search with Fallback

Best for: Retrieval-augmented generation where you want to maximize recall.

```python
def rag_search(user_question, embeddings):
    """
    Hybrid search with fallback to vector-only.
    Extracts key terms from the question for the text query.
    """
    # Extract key terms (use your LLM or a simple heuristic)
    key_terms = extract_key_terms(user_question)  # e.g., "OAuth2 PKCE setup"

    # Try hybrid first
    results = client.search(tid, cid, {
        "Vector": {
            "SearchType": "CosineSimilarity",
            "Embeddings": embeddings
        },
        "FullText": {
            "Query": key_terms,
            "TextWeight": 0.3
        },
        "MaxResults": 10
    })

    if results["TotalRecords"] >= 3:
        return results["Documents"]

    # Fall back to vector-only for broader recall
    return client.search(tid, cid, {
        "Vector": {
            "SearchType": "CosineSimilarity",
            "Embeddings": embeddings
        },
        "MaxResults": 10
    })["Documents"]
```

### 13.2 Knowledge Base Search — Full-Text with Label Scoping

Best for: Searching within a specific category of documents.

```python
def knowledge_base_search(query_text, category_label):
    """Search within a specific documentation category."""
    return client.search(tid, cid, {
        "FullText": {
            "Query": query_text,
            "SearchType": "TsRankCd"  # Proximity matters for multi-term queries
        },
        "LabelFilter": {
            "Required": [category_label]
        },
        "MaxResults": 20
    })["Documents"]

# Usage
docs = knowledge_base_search("database connection pooling", "infrastructure-docs")
```

### 13.3 Time-Scoped Hybrid Search

Best for: Finding recent documents about a topic.

```python
from datetime import datetime, timedelta

def recent_hybrid_search(query_text, embeddings, days_back=30):
    """Hybrid search limited to recent documents."""
    cutoff = (datetime.utcnow() - timedelta(days=days_back)).strftime("%Y-%m-%dT%H:%M:%SZ")

    return client.search(tid, cid, {
        "Vector": {
            "SearchType": "CosineSimilarity",
            "Embeddings": embeddings
        },
        "FullText": {
            "Query": query_text,
            "TextWeight": 0.4
        },
        "CreatedAfter": cutoff,
        "MaxResults": 10,
        "SortOrder": "ScoreDescending"
    })["Documents"]
```

### 13.4 Multi-Tenant Tag-Filtered Search

Best for: SaaS applications where documents are tagged by customer/project.

```python
def tenant_scoped_search(query_text, embeddings, customer_id, project_id=None):
    """Hybrid search scoped to a specific customer and optional project."""
    tag_conditions = [
        {"Key": "customer_id", "Condition": "Equals", "Value": customer_id}
    ]
    if project_id:
        tag_conditions.append(
            {"Key": "project_id", "Condition": "Equals", "Value": project_id}
        )

    return client.search(tid, cid, {
        "Vector": {
            "SearchType": "CosineSimilarity",
            "Embeddings": embeddings
        },
        "FullText": {
            "Query": query_text,
            "TextWeight": 0.3
        },
        "TagFilter": {
            "Required": tag_conditions
        },
        "MaxResults": 15
    })["Documents"]
```

### 13.5 Exact Term Required + Relevance Ranked

Best for: When a specific term must be present, but you want results ranked by overall relevance.

```python
def search_with_required_term(query_text, required_term, embeddings):
    """
    Hybrid search where a specific term MUST appear (exact substring),
    but results are ranked by hybrid relevance.
    """
    return client.search(tid, cid, {
        "Vector": {
            "SearchType": "CosineSimilarity",
            "Embeddings": embeddings
        },
        "FullText": {
            "Query": query_text,
            "TextWeight": 0.3
        },
        "Terms": {
            "Required": [required_term]
        },
        "MaxResults": 10
    })["Documents"]

# Usage: Find documents about authentication that MUST mention "PKCE"
docs = search_with_required_term(
    "authentication flow setup",
    "PKCE",
    get_embeddings("authentication flow setup")
)
```

### 13.6 Full-Text Browse with Pagination

Best for: Browsing all documents matching a text query across the entire collection.

```javascript
async function browseAllMatches(queryText) {
  const allDocs = [];
  let continuationToken = null;

  do {
    const query = {
      FullText: { Query: queryText },
      MaxResults: 100,
    };
    if (continuationToken) {
      query.ContinuationToken = continuationToken;
    }

    const results = await client.search(tid, cid, query);
    allDocs.push(...results.Documents);
    continuationToken = results.EndOfResults ? null : results.ContinuationToken;
  } while (continuationToken);

  console.log(`Found ${allDocs.length} total documents matching "${queryText}"`);
  return allDocs;
}
```

### 13.7 Debug/Diagnostic: Compare Vector vs. Text Relevance

Best for: Understanding why certain documents rank the way they do.

```python
def diagnostic_search(query_text, embeddings):
    """Run all three search modes and compare results."""

    # Vector only
    v = client.search(tid, cid, {
        "Vector": {"SearchType": "CosineSimilarity", "Embeddings": embeddings},
        "MaxResults": 5
    })

    # Full-text only
    t = client.search(tid, cid, {
        "FullText": {"Query": query_text},
        "MaxResults": 5
    })

    # Hybrid
    h = client.search(tid, cid, {
        "Vector": {"SearchType": "CosineSimilarity", "Embeddings": embeddings},
        "FullText": {"Query": query_text, "TextWeight": 0.5},
        "MaxResults": 5
    })

    print("=== VECTOR ONLY ===")
    for d in v["Documents"]:
        print(f"  Score={d['Score']:.4f}  Dist={d['Distance']:.4f}  {d['DocumentKey']}")

    print("\n=== FULL-TEXT ONLY ===")
    for d in t["Documents"]:
        print(f"  Score={d['Score']:.4f}  TextScore={d.get('TextScore', 0):.4f}  {d['DocumentKey']}")

    print("\n=== HYBRID (50/50) ===")
    for d in h["Documents"]:
        print(f"  Score={d['Score']:.4f}  TextScore={d.get('TextScore', 0):.4f}  Dist={d['Distance']:.4f}  {d['DocumentKey']}")
```

---

## 14. Frequently Asked Questions

**Q: Do I need to re-index my documents to use full-text search?**
No. The FTS index is built from the existing `content` column automatically on server startup. No data changes or re-ingestion required.

**Q: Does full-text search work on binary documents?**
No. Documents with `content = NULL` (binary-only) are never returned by full-text search. Only documents with text content are searchable.

**Q: Can I use full-text search without generating embeddings?**
Yes. Send only the `FullText` object without `Vector`. No embeddings needed.

**Q: What happens if the text query matches zero documents in hybrid mode?**
You get zero results, even if there are vector-similar documents. The text query acts as a filter in hybrid mode. Consider using the fallback pattern (Section 7.4) or client-side fusion (Section 5.4).

**Q: Is the text query case-sensitive?**
No. PostgreSQL's text search normalizes terms to lowercase during processing.

**Q: Can I search in languages other than English?**
Yes. Set `FullText.Language` to the PostgreSQL text search configuration for your language (e.g., `"spanish"`, `"french"`, `"german"`, `"simple"`). Note that non-English queries won't use the pre-built index and will be slower on large collections.

**Q: What's the difference between `FullText.MinimumScore` and `SearchQuery.MinimumScore`?**
`FullText.MinimumScore` filters on the text relevance component (`TextScore`). `SearchQuery.MinimumScore` filters on the final blended `Score`. In hybrid mode, a document can have a good blended score but a low text score, or vice versa.

**Q: Can I sort by text score in hybrid mode?**
Yes. Use `SortOrder: "TextScoreDescending"` to sort by text relevance regardless of the blended hybrid score.

**Q: How do I get union behavior (documents matching either vector OR text)?**
Issue two separate searches and merge client-side. See Section 5.4 for a complete example with Reciprocal Rank Fusion.

**Q: What's the maximum length of the `FullText.Query` string?**
There's no hard limit beyond PostgreSQL's general query size constraints. Keep queries reasonable (under 10,000 characters). Longer queries work but produce more AND-joined terms, making matches increasingly restrictive.

**Q: Does `TextWeight: 0.0` in hybrid mode disable text scoring?**
No. The text query still acts as a **filter** — only matching documents are returned. The text score is computed but given zero weight in the blended score. If you want pure vector search without text filtering, don't include `FullText` at all.

**Q: Are existing vector-only searches affected by this upgrade?**
No. If you don't include `FullText` in your request, behavior is identical to before. The `TextScore` field will be `null`/absent in responses. No breaking changes.
