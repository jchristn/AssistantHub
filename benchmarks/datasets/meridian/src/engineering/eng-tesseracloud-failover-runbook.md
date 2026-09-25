# TesseraCloud Regional Failover Runbook

Document ID: eng-tesseracloud-failover-runbook

Owner: Platform Engineering (Head: Kofi Mensah-Bonsu)

Last reviewed: 2025-06-20

Version: 3.2

Classification: Internal. Applies to TesseraCloud production only.

## 1. Purpose and scope

This runbook describes how to move TesseraCloud production traffic from the primary region euw1 (Dublin) to the secondary disaster recovery region euc1 (Frankfurt), how to verify the result, and how to fail back once the primary region is healthy again. It covers the customer-facing services that make up the TesseraCloud SaaS platform: the telemetry ingestion tier that receives data from TS-4 gateways and TS-4e edge nodes, the public API and web application, the stream processing tier, the Kafka event backbone and the TimescaleDB time-series store.

The runbook does not cover the Atlas ERP integration (billing export), which is batch based and is re-run manually after a failover, and it does not cover the on-premises TS-4 gateway fleet itself, which requires no action from Meridian during a failover because gateways reconnect by DNS name.

The recovery objectives committed to customers in the TesseraCloud service description are:

- Recovery Time Objective (RTO): 60 minutes from the failover decision to restored ingestion and API service in euc1.
- Recovery Point Objective (RPO): 5 minutes of telemetry at most, measured at the Kafka and TimescaleDB layers.

Because TS-4 gateways buffer telemetry locally, the effective customer data loss in most failovers is zero; the RPO applies to data that had already been acknowledged by tc-ingest in euw1 but had not yet been replicated to euc1.

## 2. When to fail over

A regional failover is a significant event. It moves every customer tenant, invalidates warm caches and requires a planned failback later. Do not fail over for a single degraded component that can be repaired in place within the RTO. Use the criteria below. If in doubt, the Incident Commander decides, and the decision is recorded in the incident channel.

| Situation | Fail over? | Notes |
|---|---|---|
| Cloud provider declares a regional outage affecting euw1 compute or networking | Yes | Start at step 1 immediately; do not wait for provider ETA |
| tc-ingest unavailable in all euw1 availability zones for more than 15 minutes with no identified fix | Yes | Check the tc-ingest health endpoint first (step 4) |
| TimescaleDB primary and synchronous standby both lost | Yes | Regional failover is faster than rebuilding the primary |
| Kafka cluster in euw1 has lost quorum or more than two brokers | Yes, unless the brokers are returning | Confirm with the Kafka on-call engineer |
| Single broker, single database node or single availability zone lost | No | Handle with the component runbooks |
| Elevated API latency or error rate under 5 percent | No | Treat as SEV2 or SEV3 per the severity definitions |
| Security incident requiring isolation of euw1 | Case by case | Head of IT Security (Mei-Ling Tan) must be consulted |

A failover is always handled as a SEV1 incident as defined in it-incident-severity-definitions, regardless of how the incident was originally classified. If the incident was opened at a lower severity, the Incident Commander raises it to SEV1 at the moment the failover decision is taken.

## 3. Decision authority and roles

The decision to fail over is made by the Incident Commander, with approval from the Head of Platform Engineering or the delegated duty manager listed on the Platform on-call rotation. Approval may be given verbally on the incident bridge; the Incident Commander records the name of the approver and the time in the incident channel. If neither the Head of Platform Engineering nor the duty manager can be reached within 10 minutes, the Incident Commander may proceed alone and must note the attempts to reach them.

| Role | Filled by | Responsibilities |
|---|---|---|
| Incident Commander (IC) | Platform on-call primary, or the senior engineer who declared the incident | Owns the decision, runs the bridge, keeps the timeline |
| Approver | Head of Platform Engineering or delegated duty manager | Approves failover and failback |
| Operator | Platform on-call secondary | Runs tcctl commands and records output |
| Database lead | Data platform on-call | Promotes TimescaleDB, checks replication |
| Kafka lead | Streaming on-call | Checks MirrorMaker 2, moves consumer groups |
| Communications lead | Customer Support duty lead | Updates status page and key account contacts |
| Scribe | Any available engineer | Keeps timestamps in the incident channel |

One person may hold several roles in a small incident, but the Incident Commander should never also be the Operator.

## 4. Architecture summary

TesseraCloud runs active-passive across two regions. Only euw1 serves customer traffic in normal operation. euc1 runs warm standby capacity: the Kubernetes services are deployed at reduced replica counts, the database replica is continuously applying changes, and MirrorMaker 2 copies Kafka topics from euw1 to euc1.

| Component | Primary (euw1) | DR (euc1) | Port | Replication |
|---|---|---|---|---|
| tc-ingest (MQTT ingestion) | tc-ingest deployment, 12 replicas | tc-ingest deployment, 3 replicas (scaled on failover) | 8883 MQTT over TLS; 8081 health | Stateless |
| tc-api (public API and web) | tc-api deployment, 8 replicas | tc-api deployment, 2 replicas | 443 HTTPS | Stateless |
| tc-stream (Kafka consumers) | consumer group tc-stream-prd | same group name, stopped until promotion | n/a | Offsets translated by MirrorMaker 2 |
| Kafka | kafka-prd-01 to kafka-prd-06 | kafka-dr-01 to kafka-dr-03 | 9093 TLS | MirrorMaker 2, asynchronous |
| TimescaleDB primary | tsdb-prd-01.tessera.meridian.internal | n/a | 5432 | Synchronous to tsdb-prd-02 |
| TimescaleDB standby | tsdb-prd-02 (synchronous) | tsdb-dr-01.euc1.tessera.meridian.internal (asynchronous) | 5432 | Streaming replication |
| Public DNS | ingest.tesseracloud.meridian-instruments.com, api.tesseracloud.meridian-instruments.com | same names, switched by tcctl | n/a | TTL 60 seconds |

TS-4 gateways connect to ingest.tesseracloud.meridian-instruments.com on port 8883 using MQTT over TLS. TS-4e edge nodes normally forward through a TS-4 gateway; the small number of TS-4e units configured for direct cloud connection use the same DNS name and port. The public API and web application are reached at api.tesseracloud.meridian-instruments.com on port 443.

The DNS records for both names have a TTL of 60 seconds. Clients that honour the TTL will reach euc1 within about one minute of the DNS switch. Some customer networks run caching resolvers that ignore short TTLs; these clients reconnect as their caches expire, which in past drills has taken up to 20 minutes for a small tail of sites.

### Gateway buffering

TS-4 gateways store readings in their local store-and-forward buffer when they cannot reach TesseraCloud, so readings are not lost during a failover. After the DNS switch they reconnect to euc1 and replay the buffered data in order. Gateways running gateway firmware 5.0.0 or later use jittered reconnect backoff and paced replay (added after INC-2024-031, action item AI-3). Gateways on older 4.x firmware reconnect immediately and replay at full speed, which can overload tc-ingest in the first 30 minutes after the switch. The firmware distribution report in the TesseraCloud admin console (Fleet, Firmware) shows how many gateways are still below 5.0.0; at the last review in June 2025 this was under 4 percent of the fleet.

## 5. Prerequisites

Before starting the procedure, the Operator confirms the following. Most of these are standing conditions that are checked monthly by the Platform team, but they must be reconfirmed during an incident.

- Access to the tcctl command line tool at version 2.14 or later, authenticated with a production break-glass role. Run tcctl version to check.
- VPN access to the Meridian management network, or access through the cloud bastion if the corporate VPN is affected.
- Access to the Forge (CI/CD) administration console to freeze deployments.
- Access to the status page administration for status.tesseracloud.meridian-instruments.com.
- The incident channel is open in the chat tool and a bridge call is running.
- A Beacon incident ticket exists in category SEC-INCIDENT or INC-PLATFORM (whichever the service desk used when opening the incident); the ticket number is posted in the incident channel.
- The euc1 capacity reservation is active. This is confirmed by the monthly DR readiness check; if the check has failed in the last 30 days, the Operator must warn the IC that scale-up may be slower.

## 6. Failover procedure

Record the start time of each step in the incident channel. Steps are numbered continuously across all phases so that they can be referenced from postmortems and other runbooks.

### Phase A: Assess and declare

1. The IC declares or upgrades the incident to SEV1 and announces on the bridge that a regional failover is being considered. The Scribe starts the timeline.
2. The Operator runs tcctl status --region euw1 and posts the output to the incident channel. Note which components report DEGRADED or DOWN.
3. The Operator runs tcctl status --region euc1 to confirm the DR region is healthy. All components should report STANDBY or READY. If any euc1 component reports DOWN, stop and escalate to the Head of Platform Engineering before continuing.
4. The Operator checks the tc-ingest health endpoint /healthz on port 8081 in both regions through the internal load balancers. In euw1 this confirms whether ingestion is truly down; in euc1 it should return status ok with mode standby.
5. The IC reviews the decision criteria in section 2 with the Approver. If the decision is to fail over, the IC states on the bridge: failover approved, approver name, time. The Scribe records this.
6. The Communications lead posts an initial notice on status.tesseracloud.meridian-instruments.com. The first status page update must be published within 30 minutes of the SEV1 declaration, and further updates every 30 minutes until the incident is resolved.

### Phase B: Precheck and freeze

7. The Operator freezes production deployments for TesseraCloud in Forge by enabling the deployment freeze flag on the tesseracloud-prod environment. This prevents pipelines from deploying to either region during the failover. Post a screenshot or the Forge audit entry in the channel.
8. The Operator runs tcctl failover precheck --target euc1. The precheck validates credentials, DNS zone access, database replica state, MirrorMaker 2 status and euc1 capacity. It prints a summary table and exits with PASS, WARN or FAIL. Continue on PASS. On WARN, read each warning aloud on the bridge; the IC decides whether to continue. On FAIL, do not continue until the failure is resolved or the IC and Approver explicitly accept the risk.
9. The Kafka lead checks the consumer lag of consumer group tc-stream-prd in euw1 (tcctl kafka lag --group tc-stream-prd --region euw1). Record the total lag and the per-partition maximum. A large lag means processed data in TimescaleDB is behind raw data in Kafka, which affects how much reprocessing euc1 will need after promotion. This check was introduced as action item AI-2 of postmortem INC-2024-031, the May 2024 ingestion outage; at that time it was a RabbitMQ queue-depth check, and it was converted to a Kafka consumer lag check after the migration to Kafka under ADR-019.
10. If euw1 is still partially reachable, the Operator stops new MQTT connections to euw1 by setting tc-ingest to drain mode (tcctl ingest drain --region euw1). Gateways that lose their connection will start buffering locally. If euw1 is unreachable, skip this step and note it in the channel.
11. The Database lead checks replication status on tsdb-dr-01.euc1.tessera.meridian.internal and records the replay lag reported by the replica. In normal operation it is under 2 seconds.
12. The Kafka lead confirms MirrorMaker 2 connectors are running for all replicated topics, including tessera.telemetry.raw, tessera.telemetry.agg and tessera.alerts. Any stopped connector must be noted; its topic may have a larger gap.
13. The Operator scales up euc1 stateless services to production capacity using tcctl scale --region euc1 --profile production. This takes 5 to 10 minutes and can run in parallel with the next steps. Do not proceed to DNS switch until the scale-up has completed.

### Phase C: Promote data tier

14. The Kafka lead confirms that MirrorMaker 2 replication lag, reported as the metric replication-latency-ms-max, is below 30 seconds (30000 ms) for all replicated topics before promotion proceeds. If the lag is above 30 seconds, wait up to 10 minutes and re-check, because lag usually falls quickly once euw1 producers have stopped. If the lag is still above 30 seconds after 10 minutes, the Incident Commander may decide to accept the resulting data loss, provided it stays within the 5-minute RPO, and must record that decision, the observed lag and the time in the incident channel. If the lag suggests loss beyond the RPO, the IC must inform the Approver and the Communications lead before continuing.
15. The Database lead confirms that the DR replica has replayed all WAL it has received and that no further WAL is arriving from euw1.
16. The Operator runs tcctl failover promote --region euc1 --confirm. This command promotes tsdb-dr-01 to primary, switches the euc1 Kafka cluster from replica mode to active mode, stops MirrorMaker 2 and translates committed offsets for consumer group tc-stream-prd. The command is idempotent; if it times out, run it again rather than performing the steps manually.
17. The Database lead verifies that tsdb-dr-01.euc1.tessera.meridian.internal accepts writes on port 5432 and that the application role can connect. Record the promotion timestamp.
18. The Kafka lead verifies that kafka-dr-01, kafka-dr-02 and kafka-dr-03 accept produce requests on port 9093 and that all partitions of the telemetry topics have an in-sync leader.

### Phase D: Start processing in euc1

19. The Operator starts tc-stream in euc1. The consumers join group tc-stream-prd using the translated offsets. Watch the consumer lag for the first 5 minutes; it should begin to fall.
20. The Operator confirms tc-api pods in euc1 report ready and that the API returns 200 on its internal health check.
21. The Operator confirms tc-ingest in euc1 reports mode active on /healthz port 8081.

### Phase E: Switch traffic

22. The Operator runs tcctl dns switch --target euc1. This updates ingest.tesseracloud.meridian-instruments.com and api.tesseracloud.meridian-instruments.com to point at the euc1 load balancers. Record the time; clients honouring the 60-second TTL should arrive within about one minute.
23. The Operator monitors MQTT connection count on tc-ingest in euc1. Expect a rapid rise in the first 5 minutes followed by a slower tail. The connection count should reach at least 90 percent of the pre-incident baseline within 30 minutes.
24. The Operator watches ingestion throughput. Throughput will temporarily exceed normal peak as TS-4 gateways replay their buffers. The euc1 production profile is sized for 2.5 times normal peak; if tc-ingest CPU stays above 85 percent for 10 minutes, scale further with tcctl scale --region euc1 --component tc-ingest --replicas 24.
25. The Communications lead updates status.tesseracloud.meridian-instruments.com to say that service has been restored from the secondary region and that historical data may take time to backfill.

### Phase F: Verify and stabilise

26. The IC runs through the verification checklist in section 7 with the Operator and records each result.
27. The Operator re-enables Forge deployments for tesseracloud-prod only if the IC agrees; by default the freeze stays in place until failback, and only emergency fixes are deployed, through the ECAB process in the change management procedure.
28. The Operator raises a Beacon change record in category CHG-EMERG to document the failover as an emergency change, linking the incident ticket.
29. The IC declares service restored, records the total time from the failover decision in step 5 to completion of step 26, and compares it against the 60-minute RTO.
30. The IC confirms the incident remains open as SEV1 until customer impact has ended, then downgrades per the severity definitions and schedules the post-incident review within five business days.

## 7. Verification checklist

| Check | How | Expected result |
|---|---|---|
| DNS resolves to euc1 | Resolve ingest.tesseracloud.meridian-instruments.com and api.tesseracloud.meridian-instruments.com from two external networks | euc1 load balancer addresses |
| Gateway connections | tc-ingest connection dashboard in euc1 | At least 90 percent of baseline within 30 minutes |
| Ingestion health | /healthz on port 8081 | status ok, mode active |
| API health | Synthetic check from status page provider | HTTP 200 on api.tesseracloud.meridian-instruments.com port 443 |
| Database writes | Insert and read a canary row via tcctl db canary | Success within 1 second |
| Kafka | tcctl kafka lag --group tc-stream-prd --region euc1 | Lag falling, no partitions without leader |
| Customer view | Log in to a demo tenant and open a live TS-4 dashboard | New readings within 2 minutes |
| Alerts pipeline | Trigger a test vibration threshold alert on demo tenant | Email and webhook delivered |
| Billing export | Confirm Atlas billing job paused | Job shows HELD; re-run after failback |

## 8. Failback

Failback returns traffic to euw1. It is a planned activity, not an emergency action, and it has its own risks, so it is not rushed.

- Failback is not started until TesseraCloud has run stably in euc1 for at least 24 hours, and the cause of the euw1 failure has been understood and fixed.
- Failback is performed in a change window approved through CAB, or through ECAB if a business reason requires it sooner than the next CAB meeting. The change is raised in Beacon with a link to the original incident.
- Customers are notified at least 48 hours in advance through the status page, unless the change is approved via ECAB, in which case the notice period is agreed by the Approver.

The failback procedure in outline:

1. Rebuild or resynchronise TimescaleDB in euw1 as a replica of tsdb-dr-01, then re-establish tsdb-prd-02 as a synchronous standby of tsdb-prd-01 once tsdb-prd-01 becomes primary again.
2. Reconfigure MirrorMaker 2 in the reverse direction, from euc1 to euw1, and wait for replication lag below 30 seconds.
3. Run tcctl failback plan to generate the ordered step list for the current state; the plan output is attached to the change record.
4. Execute the plan during the change window: drain euc1 ingestion, promote euw1, switch DNS back, start tc-stream in euw1.
5. Restore normal MirrorMaker 2 direction from euw1 to euc1 and return euc1 to standby capacity.
6. Lift the Forge deployment freeze and resume the Atlas billing export.

## 9. Known issues and troubleshooting

| Symptom | Likely cause | Action |
|---|---|---|
| tcctl failover precheck --target euc1 reports WARN on capacity | euc1 capacity reservation not fully available | Continue; scale-up may take up to 20 minutes. Inform IC |
| Promote command times out | Control plane API throttling in euc1 | Re-run tcctl failover promote --region euc1 --confirm; it is idempotent |
| Consumer lag in euc1 not falling | Offset translation incomplete for some partitions | Kafka lead resets affected partitions to translated checkpoint; see Kafka runbook |
| Some gateways never reconnect | Customer DNS caching or firewall allowing only euw1 addresses | Communications lead contacts customer; customers should allow the documented TesseraCloud address ranges for both regions |
| Duplicate readings in dashboards | Gateway replay overlapping data already replicated | Expected; tc-stream deduplicates on gateway sequence number within 24 hours |
| Reconnect surge from some sites | Gateways on firmware older than 5.0.0 reconnect without backoff | Scale tc-ingest (step 24); record affected tenants and recommend a firmware upgrade |
| API 401 errors after failover | Token signing key cache not warm in euc1 | Restart tc-api pods in euc1 one availability zone at a time |

## 10. Drills and testing

Failover drills are run twice a year, in April and October, against production with customer notice, following this runbook exactly. Each drill produces a short report with timings per phase and any runbook corrections. The most recent drill, on 2025-04-15, completed in 38 minutes from the failover decision to verification, within the 60-minute RTO. The drill found that step 13 scale-up was the longest single step and that the status page template needed a German translation for DACH customers; both were addressed in version 3.2.

In addition, the monthly DR readiness check runs tcctl failover precheck --target euc1 in dry-run mode and reports the result to the Platform Engineering channel.

## 11. Related documents

- it-incident-severity-definitions: SEV1 definition and update cadence
- it-oncall-policy-v2: paging and acknowledgement rules for the Platform rotation
- it-change-management-procedure-v2: CAB and ECAB process for failback and emergency changes
- eng-postmortem-inc-2024-031-tesseracloud-ingestion: origin of the consumer lag check in step 9
- eng-adr-019-message-broker-kafka and eng-adr-023-timeseries-timescaledb: architecture decisions behind the current data tier
- eng-tesseracloud-config-prod: production hosts, ports and configuration keys

## 12. Revision history

| Version | Date | Author | Change |
|---|---|---|---|
| 1.0 | 2023-03-02 | Platform Engineering | First version for RabbitMQ and InfluxDB architecture |
| 2.0 | 2024-06-27 | Platform Engineering | Added queue-depth check (INC-2024-031 AI-2) and gateway buffering notes |
| 3.0 | 2025-02-24 | Platform Engineering | Rewritten for Kafka (ADR-019) and TimescaleDB (ADR-023); queue-depth check converted to consumer lag; added MirrorMaker 2 lag check |
| 3.1 | 2025-04-22 | Platform Engineering | Updates after April 2025 drill: scale-up moved earlier, promote command made idempotent |
| 3.2 | 2025-06-20 | Kofi Mensah-Bonsu | Review; status page cadence aligned with SEV1 definition; added troubleshooting rows |
