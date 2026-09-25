# ADR-012: Use RabbitMQ as the TesseraCloud Message Broker

Document ID: eng-adr-012-message-broker-rabbitmq. Status: Superseded by ADR-019 (Kafka). Date: 21 November 2022. Deciders: Kofi Mensah-Bonsu (Head of Platform Engineering), TesseraCloud tech leads.

## Context

TesseraCloud ingestion (tc-ingest) receives MQTT messages from TS-4 gateways and must hand them to downstream processors for alerting, aggregation and storage. In 2022 the services called each other directly over HTTP, and a slow downstream service caused ingestion to back up and drop messages. We need a broker to decouple ingestion from processing.

## Options considered

- RabbitMQ (self-managed cluster, quorum queues).
- Apache Kafka (self-managed).
- A managed cloud queue service.

## Decision

Use RabbitMQ with a three-node cluster (rmq-prd-01 to rmq-prd-03) and quorum queues. tc-ingest publishes to the exchange tessera.telemetry; consumers bind per function (alerts, aggregation, storage).

## Rationale

- The team already operated RabbitMQ for internal tooling.
- Message volume in 2022 (about 300 messages per second) is well within RabbitMQ's comfort zone.
- Per-message acknowledgement and dead-lettering suit alert processing.
- Kafka was judged operationally heavier than the team could support at the time.

## Consequences

- Messages are removed once consumed, so replaying historical telemetry into a new consumer is not possible.
- Memory pressure on brokers must be monitored; if the memory high watermark is reached, publishers are blocked.
- Revisit if message volume exceeds 5,000 messages per second or if replay becomes a requirement.
