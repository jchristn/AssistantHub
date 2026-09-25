# Benchmark dataset format

Every dataset the harness (`src/Test.Benchmark`) reads is one JSON file in this format. Converted public datasets
(`prepare`) and the committed datasets in this directory use the same schema.

```json
{
  "name": "meridian",
  "description": "One or two sentences.",
  "corpora": [
    {
      "id": "meridian",
      "documents": [ { ...document... } ],
      "queries": [ { ...query... } ]
    }
  ]
}
```

Each corpus becomes its own AssistantHub collection. Document ids are unique within a corpus, and query relevance labels
refer to them.

## Document

| Field | Required | Meaning |
|---|---|---|
| `id` | yes | Stable id, lowercase kebab-case (for example `hr-travel-policy-2025`). Matched against query `relevant` lists. |
| `title` | yes | Human title. Becomes the AssistantHub document name. |
| `body` | one of `body`/`file` | Plain text or Markdown content, uploaded as `text/plain` or `text/markdown` per `contentType`. |
| `file` | one of `body`/`file` | Path relative to the dataset JSON file, uploaded in its native format (PDF, DOCX, HTML, MD, TXT). |
| `contentType` | no | MIME type. Inferred from the file extension when omitted, `text/plain` for `body`. |
| `summary` | no | One-line summary (informational). |
| `labels` | no | String list, attached as AssistantHub document labels (used by metadata-filter questions). |
| `tags` | no | String-to-string map, attached as AssistantHub document tags (used by metadata-filter questions). |
| `date` | no | ISO date (`2025-03-14`). Dated documents are ingested in date order. |
| `version` | no | Version string of a versioned document (`"1"`, `"2"`). |
| `supersedes` | no | Id of the document this one replaces. |

## Query

| Field | Required | Meaning |
|---|---|---|
| `id` | yes | Unique within the corpus. |
| `text` | yes | The question as a user would type it. |
| `type` | yes | Retrieval difficulty: `paraphrase`, `lexical`, `multi`, `detail`, `superseded`, `confusable`, `filter`, `table`, `followup`, `negative`. |
| `category` | yes | What the question asks (aligned with `EvalFact.RecommendedCategories`): `factual_lookup`, `multi_hop`, `aggregation`, `temporal`, `ambiguous_query`, `unanswerable`, `citation_required`, `structured_data`. |
| `relevant` | yes | Ids of documents containing the answer, most important first. **Empty for unanswerable questions.** |
| `grades` | no | Graded relevance, document id to gain (2 = primary evidence, 1 = supporting). Documents in `relevant` but not here have gain 1. |
| `evidence` | answerable: yes | Verbatim passages from the relevant documents that together answer the question. Each is copied exactly from the document text (a sentence, clause, or table cell run), 3–40 words, with no Markdown syntax (`**`, `#`, `|`, backticks, link brackets). Matching is case-, whitespace- and punctuation-insensitive. |
| `answer` | yes | Short gold answer. `NOT_IN_CORPUS` for unanswerable questions. |
| `metadataFilter` | no | `{ "required_labels": [], "excluded_labels": [], "required_tags": [ { "key": "", "condition": "Equals", "value": "" } ] }`, passed as the chat `metadata_filter`. |
| `attachedDocuments` | no | Document ids to pass as `attached_document_ids`. |
| `conversation` | no | Prior turns, `[ { "role": "user", "content": "" }, { "role": "assistant", "content": "" } ]`. The query `text` is the next user turn. |
| `date` | no | The date the question is asked on (temporal questions). |

## Type guidance

- `paraphrase`: shares few words with the evidence; the meaning must be matched.
- `lexical`: hinges on an exact identifier, code, product number, setting name or route.
- `multi`: needs two or three documents; `relevant` lists all of them.
- `detail`: the answer sits deep inside a long document, far from its title or opening.
- `superseded`: an older and a newer document disagree; only the newer one is `relevant` (the older can be listed in `grades` with gain 0 — do not put it in `relevant`).
- `confusable`: near-duplicate documents exist (sibling product models, regional variants); only one is right.
- `filter`: carries `metadataFilter` or `attachedDocuments`, and the answer depends on respecting it.
- `table`: the answer is in a table row or cell.
- `followup`: carries `conversation`; the query text is elliptical ("what about for contractors?").
- `negative`: the corpus does not answer it, although related material exists. `relevant` is empty, `answer` is `NOT_IN_CORPUS`, no `evidence`.
