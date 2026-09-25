# ADR-023: Replace InfluxDB with TimescaleDB for TesseraCloud Telemetry

Document ID: eng-adr-023-timeseries-timescaledb. Status: Accepted. Date: 4 November 2024. Supersedes: ADR-008 (InfluxDB as the TesseraCloud time-series store). Deciders: Lars Hedegaard (CTO), Kofi Mensah-Bonsu (Head of Platform Engineering), TesseraCloud tech leads, Mei-Ling Tan (Head of IT Security) consulted.

## Context

ADR-008 chose self-managed InfluxDB in 2022. Since then:

- Customer contracts now commit to 13 months of raw telemetry on the Standard plan and 36 months on the Enterprise plan, far beyond the 90 days ADR-008 assumed.
- The dual-write high-availability approach caused two data divergence incidents in 2023 and 2024, each requiring manual reconciliation.
- Every report joining telemetry to asset metadata needs two query paths, slowing feature delivery.
- ADR-019 moved ingestion to Kafka, making it straightforward to replay topics into a new store during migration.

## Decision

Replace InfluxDB with TimescaleDB (PostgreSQL with the Timescale extension) as the single store for TesseraCloud telemetry and asset metadata.

- Primary: tsdb-prd-01.tessera.meridian.internal (port 5432), synchronous standby tsdb-prd-02, and an asynchronous disaster-recovery replica tsdb-dr-01 in euc1.
- Raw telemetry retention: 400 days by default (TC_RETENTION_RAW_DAYS), enforced by a retention policy on the hypertable; Enterprise tenants keep 36 months, with chunks older than 400 days moved to a compressed archive tablespace.
- 1-minute continuous aggregates retained for 5 years.
- Native compression on chunks older than 7 days.

## Migration

- Kafka topic tessera.telemetry.raw replayed into TimescaleDB in parallel with InfluxDB from December 2024.
- Read cut-over completed 27 January 2025; InfluxDB decommissioned 24 February 2025.

## Consequences

- One SQL query path for tc-api; reporting joins become simple.
- Streaming replication gives a real standby, replacing dual-writes.
- Storage for 400 days of raw data is roughly 38 TB after compression, which is acceptable.
- Operations must now manage PostgreSQL vacuum and replication lag; replication lag is included in the TesseraCloud failover checks.
- Backup is via daily base backups and continuous WAL archiving, per the Backup and Retention Standard v2.
