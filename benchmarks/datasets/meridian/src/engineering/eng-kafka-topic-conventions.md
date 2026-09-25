# Kafka Topic Naming Conventions

Document ID: eng-kafka-topic-conventions. Owner: Platform Engineering. Last updated: 20 January 2025. Background: ADR-019 (Kafka replaces RabbitMQ for TesseraCloud).

## Format

Topic names use lower case, dot-separated segments:

product.domain.dataset

For example tessera.telemetry.raw. A version suffix (.v2) is added only when a breaking schema change requires a new topic; the old topic runs in parallel until all consumers have moved.

## Rules

- product is one of tessera, halcyon, lumen or platform.
- domain is a business area such as telemetry, alerts, devices, billing.
- dataset describes the content (raw, agg, events, commands).
- No environment names in topic names. Each environment has its own cluster (kafka-dev, kafka-stg, kafka-prd), so the topic name is identical across environments.
- No hyphens or underscores in topic names; they are reserved for consumer group names.
- Consumer group names use the form service-env, for example tc-stream-prd, tc-stream-stg, tc-stream-dev.

## Current production topics

| Topic | Partitions | Retention | Producer | Main consumers |
|---|---|---|---|---|
| tessera.telemetry.raw | 48 | 7 days | tc-ingest | tc-stream (storage), tc-agg |
| tessera.telemetry.agg | 24 | 3 days | tc-agg | tc-stream (storage) |
| tessera.alerts | 12 | 14 days | tc-stream | tc-notify |
| tessera.devices.events | 6 | 30 days | tc-api | tc-stream, billing export |
| platform.audit.events | 6 | 90 days | all services | security log forwarder |

## Creating a topic

Topics are created only through the Forge pipeline in the repository platform/kafka-topics. A merge request must state partitions, retention, replication factor (3 in staging and production), and the owning team. Production topics require approval by a Platform Engineering maintainer. Direct topic creation with admin tools is disabled in production (auto.create.topics.enable is false).

## Replication

All production topics except platform.audit.events are mirrored to the euc1 disaster-recovery cluster by MirrorMaker 2. Mirrored topics keep the same name on the DR cluster.
