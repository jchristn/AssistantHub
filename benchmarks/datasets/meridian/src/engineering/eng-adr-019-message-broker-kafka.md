# ADR-019: Move the TesseraCloud Message Broker from RabbitMQ to Kafka

Document ID: eng-adr-019-message-broker-kafka. Status: Accepted. Date: 12 September 2024. Supersedes: ADR-012 (RabbitMQ as the TesseraCloud message broker). Deciders: Lars Hedegaard (CTO), Kofi Mensah-Bonsu (Head of Platform Engineering), TesseraCloud tech leads.

## Context

ADR-012 selected RabbitMQ in November 2022. Since then:

- Gateway count grew from about 3,000 to more than 11,000, and peak message rate reached 6,200 messages per second in mid-2024, above the 5,000 per second review trigger in ADR-012.
- INC-2024-031 (14 May 2024) was a SEV1 ingestion outage caused by RabbitMQ node rmq-prd-02 reaching its memory high watermark, which blocked publishers and stopped ingestion for all customers.
- Product teams need to replay historical telemetry into new consumers (for example the planned anomaly-detection service), which RabbitMQ cannot do once messages are consumed.
- A regional disaster-recovery design requires cross-region replication of the event stream.

## Decision

Replace RabbitMQ with Apache Kafka for TesseraCloud.

- Production cluster: six brokers, kafka-prd-01 to kafka-prd-06, TLS listener on port 9093, replication factor 3, min.insync.replicas 2.
- Topics: tessera.telemetry.raw (48 partitions, 7-day retention), tessera.telemetry.agg (24 partitions, 3-day retention), tessera.alerts (12 partitions, 14-day retention).
- Consumers use the consumer group tc-stream-prd.
- Cross-region replication to the euc1 DR cluster (kafka-dr-01 to kafka-dr-03) with MirrorMaker 2.
- Topic naming follows the Kafka Topic Naming Conventions.

## Consequences

- Seven days of raw telemetry can be replayed into any new consumer.
- Broker back-pressure no longer blocks publishers; lag accumulates in the log instead.
- The team needs Kafka operational skills; two engineers attended training in October 2024.
- The TesseraCloud failover runbook must be rewritten: the RabbitMQ queue-depth check added after INC-2024-031 becomes a consumer-lag check, and a MirrorMaker 2 replication-lag check is added before promotion.

## Migration

Dual-publishing to RabbitMQ and Kafka began 4 November 2024. Consumers moved to Kafka by 16 December 2024. RabbitMQ was decommissioned on 10 February 2025.
