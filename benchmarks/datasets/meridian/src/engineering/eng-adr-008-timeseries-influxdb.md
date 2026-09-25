# ADR-008: Use InfluxDB as the TesseraCloud Time-Series Store

Document ID: eng-adr-008-timeseries-influxdb. Status: Superseded by ADR-023 (TimescaleDB). Date: 8 March 2022. Deciders: Lars Hedegaard (CTO), Kofi Mensah-Bonsu (Head of Platform Engineering), TesseraCloud tech leads.

## Context

TesseraCloud stores vibration telemetry from TS-4 gateways: spectral bands, RMS velocity and temperature per sensor channel, typically every 10 seconds. In early 2022 the fleet was about 3,000 gateways and was forecast to grow to 15,000 by 2025. The original prototype stored telemetry in a general-purpose relational database, which could not keep up with write volume beyond about 1,000 gateways.

## Options considered

- InfluxDB (open-source edition, self-managed).
- A managed cloud time-series database.
- Staying on the relational database with partitioning.

## Decision

Use InfluxDB, self-managed on TesseraCloud infrastructure, as the telemetry store. Raw data is kept for 90 days with continuous queries producing 1-minute and hourly downsampled series.

## Consequences

- Write throughput comfortably exceeds the 2025 forecast.
- The query language differs from SQL, so the tc-api reporting layer needs a separate query path from the relational metadata database.
- Clustering is not available in the open-source edition; high availability relies on dual-writing to two instances.
- Joins between telemetry and asset metadata must be done in the application.

## Review trigger

Revisit if customers require raw retention beyond 1 year, or if the dual-write approach causes data divergence.
