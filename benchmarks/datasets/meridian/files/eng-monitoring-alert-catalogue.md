# Monitoring Alert Catalogue

Document ID: eng-monitoring-alert-catalogue. Owner: Platform Engineering and IT Operations. Last updated: 18 August 2025.

This catalogue lists the production alerts that page an on-call engineer or raise a Beacon ticket automatically. Each alert has a fixed severity, which sets the acknowledgement time through the Incident Severity Definitions. Alert names are exact and appear in the paging tool and in Beacon ticket titles.

## TesseraCloud

| Alert | Condition | Severity | Routed to | Origin |
|---|---|---|---|---|
| TC_INGEST_CONNECTIONS_ZERO | Active MQTT connections on tc-ingest in euw1 fall to zero for 2 minutes | SEV1 | platform-primary | Original design |
| TC_INGEST_HEALTH_FAILING | More than 50% of tc-ingest pods fail /healthz on port 8081 for 3 minutes | SEV2 | platform-primary | Original design |
| TC_CONSUMER_ACK_RATE_ZERO | Any tc-stream or tc-agg consumer acknowledges no messages for 3 minutes while its input is non-empty | SEV2 | platform-primary | INC-2024-031 action item AI-5 |
| TC_STREAM_CONSUMER_LAG_HIGH | Consumer group tc-stream-prd lag above 500,000 messages for 10 minutes | SEV2 | platform-primary | ADR-019 migration |
| TC_MM2_REPLICATION_LAG | MirrorMaker 2 replication-latency-ms-max above 30,000 ms for 15 minutes | SEV3 | platform-primary (business hours) | Failover readiness |
| TSDB_REPLICATION_LAG | tsdb-dr-01 replay lag above 60 seconds for 10 minutes | SEV3 | platform-primary (business hours) | ADR-023 |
| TSDB_DISK_USAGE | TimescaleDB data volume above 80% | SEV3 | platform-primary (business hours) | Original design |
| TC_API_5XX_RATE | tc-api 5xx responses above 2% of requests for 5 minutes | SEV2 | platform-primary | Original design |

## Forge

| Alert | Condition | Severity | Routed to | Origin |
|---|---|---|---|---|
| FORGE_RUNNER_TOKEN_EXPIRY_SOON | Any runner token expires in 10 days or less | SEV3 | platform-primary | INC-2024-052 action item AI-1 |
| FORGE_RUNNER_POOL_LOW | Fewer than 5 Leeds Linux runners online for more than 30 minutes | SEV3 | platform-primary | ADR-015 |
| FORGE_QUEUE_WAIT_HIGH | Median job queue wait above 10 minutes for 1 hour | SEV4 | Beacon FORGE-RUNNER ticket only | ADR-015 review trigger |

## Atlas

| Alert | Condition | Severity | Routed to | Origin |
|---|---|---|---|---|
| ATLAS_PAYX_FAILED | PAYX-MONTHLY ends in error | SEV2 | itops-primary and atlas-oncall@meridian-instruments.com; Beacon ATLAS-PAYX ticket | INC-2025-007 action items AI-1 and AI-5 |
| ATLAS_ARCHLOG_FS_WARN | /u02/arch on atlas-db-prd-01 above 80% | SEV3 | itops-primary | Month-end runbook |
| ATLAS_ARCHLOG_FS_CRIT | /u02/arch on atlas-db-prd-01 above 85% | SEV2 | itops-primary | Month-end runbook |
| ATLAS_STANDBY_LAG | atlas-db-prd-02 apply lag above 5 minutes | SEV3 | itops-primary | Month-end runbook |

## Infrastructure and security

| Alert | Condition | Severity | Routed to | Origin |
|---|---|---|---|---|
| VPN_ENDPOINT_DOWN | Any of vpn-lds, vpn-rtm, vpn-aus or vpn-pen fails health checks for 5 minutes | SEV2 | itops-primary | Original design |
| STRATA_OFFSITE_REPLICATION_FAILED | Daily copy to STRATA-RTM-02 not complete by 06:00 UK | SEV3 | Beacon SEC-BACKUP ticket | Backup and Retention Standard v2 |
| PAM_BREAKGLASS_USED | Any break-glass account checked out | SEV3 | security-primary | Privileged Access Management Guide |
| OT_NEW_DEVICE_DETECTED | Passive monitoring sees an unknown device on 10.41.0.0/16 | SEV3 | security-primary and Penang OT Support | Penang OT Network Security Standard |

## Rules for adding alerts

- Every paging alert must link to a runbook section in its description.
- SEV1 and SEV2 alerts must be reviewed by the Head of Platform Engineering or the Head of IT Operations before they go live.
- An alert that pages more than 5 times in a month without action is reviewed at the monthly paging review described in the On-call Policy v2.
