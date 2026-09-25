# Postmortem: INC-2024-031 TesseraCloud Ingestion Outage

Document ID: eng-postmortem-inc-2024-031-tesseracloud-ingestion. Incident: INC-2024-031. Severity: SEV1. Date of incident: 14 May 2024. Postmortem published: 24 May 2024. Author: Hannah Osei (Platform Engineering). Review chaired by: Kofi Mensah-Bonsu (Head of Platform Engineering).

## Summary

On 14 May 2024, TesseraCloud stopped accepting telemetry from all TS-4 gateways for 3 hours 42 minutes (09:18 to 13:00 UK time). A deployment of the aggregation consumer (tc-agg) at 08:47 introduced a defect that stopped it acknowledging messages. Its queue tessera.agg grew to about 41 million messages, and RabbitMQ node rmq-prd-02 reached its memory high watermark (vm_memory_high_watermark set to 0.4). RabbitMQ then blocked all publishing connections across the cluster, so tc-ingest could not publish and began rejecting MQTT connections from gateways on port 8883.

## Customer impact

- All TesseraCloud customers (about 10,400 gateways at the time) saw no new data for up to 3 hours 42 minutes.
- TS-4 gateways stored readings in their local store-and-forward buffer, so no telemetry was permanently lost. However, when ingestion recovered, all gateways (on gateway firmware 4.x at the time) reconnected at the same moment and replayed their buffers at full speed. The reconnect storm kept tc-ingest saturated and delayed full data catch-up until 16:40, 3 hours 40 minutes after ingestion was restored.
- Alerts on vibration thresholds were not delivered during the outage. Two customers reported missed alerts on critical pumps; no equipment damage was reported.
- Service credits were issued under customer SLAs by Customer Success.

## Timeline (UK time)

| Time | Event |
|---|---|
| 08:47 | tc-agg version 2.31.0 deployed via Forge to production |
| 08:52 | tc-agg consumers stop acknowledging messages; queue tessera.agg starts growing |
| 09:18 | rmq-prd-02 memory alarm; cluster blocks publishers; tc-ingest publish calls hang |
| 09:21 | tc-ingest health checks fail; load balancer removes pods; gateways cannot connect |
| 09:24 | Pager alert "ingest connections zero"; Platform on-call primary acknowledges at 09:36 (12 minutes, within the then 15-minute SEV1 target) |
| 09:45 | SEV1 declared; Kofi Mensah-Bonsu joins as Incident Commander |
| 10:05 | Status page status.tesseracloud.meridian-instruments.com updated (first customer update) |
| 10:30 | Regional failover considered and rejected: the DR region shared the same RabbitMQ design and the fault would follow the data |
| 11:10 | Root cause identified as tc-agg not acknowledging messages |
| 11:25 | tc-agg rolled back to 2.30.4 |
| 11:40 | Queue drain begins; memory alarm still active |
| 12:35 | Memory falls below watermark; publishers unblocked |
| 13:00 | Ingestion restored; about 10,400 gateways reconnect simultaneously and start replaying buffers |
| 13:20 | tc-ingest CPU above 95%; connection attempts rejected and retried in a tight loop |
| 16:40 | Buffer replay complete for all gateways; dashboards current |
| 17:30 | Incident closed after monitoring |

## Root causes

1. A defect in tc-agg 2.31.0 caused consumers to stop acknowledging messages without crashing, so no health check failed.
2. The tessera.agg queue had no length limit or dead-letter policy, so one stalled consumer could consume broker memory without bound.
3. RabbitMQ memory alarms block publishers cluster-wide, coupling ingestion to the health of every downstream consumer.
4. Gateway firmware 4.x reconnected immediately and replayed buffers without pacing, turning recovery into a second overload.
5. The failover runbook had no check of broker queue depth, so responders spent time evaluating a failover that would not have helped.

## Action items

| ID | Action | Owner | Due | Status |
|---|---|---|---|---|
| AI-1 | Set x-max-length and a dead-letter exchange on all TesseraCloud queues | Hannah Osei | 2024-05-31 | Done |
| AI-2 | Add a broker queue-depth check to the TesseraCloud Regional Failover Runbook before any failover decision (added as step 9; converted to a Kafka consumer-lag check after ADR-019) | Piotr Zielinski | 2024-06-28 | Done |
| AI-3 | Add jittered reconnect backoff and paced buffer replay to the TS-4 gateway firmware so gateways do not all reconnect at once after an outage | Tessera firmware team | 2024-09-30 | Done (gateway firmware 5.0.0 released 9 September 2024) |
| AI-4 | Evaluate a broker that supports replay and does not block publishers when a consumer stalls | Kofi Mensah-Bonsu | 2024-09-15 | Done (resulted in ADR-019, Kafka) |
| AI-5 | Alert when a consumer's acknowledgement rate drops to zero for 3 minutes | Ravi Chandrasekar | 2024-06-14 | Done |
| AI-6 | Add a canary stage for tc-agg deployments in Forge (10% of consumers for 15 minutes) | Ellie Brannigan | 2024-07-31 | Done |

## Lessons

A consumer that stops working silently is harder to detect than one that crashes. Back-pressure design must prevent one consumer from stopping ingestion for every customer.
