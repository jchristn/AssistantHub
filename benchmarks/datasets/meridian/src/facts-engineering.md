# Meridian corpus: engineering, IT, security and manufacturing facts cheat-sheet (PRIVATE)

This file is not ingested. It lists the facts planted in the `eng-`, `it-`, `sec-` and `mfg-` documents (52 documents in `src/engineering/`, manifest `manifest-engineering.json`) so the question-writing pass can target them.

People used in this part (checked against the HR and product corpora; no clashes): CTO Lars Hedegaard; Head of IT Security Mei-Ling Tan; Head of Platform Engineering Kofi Mensah-Bonsu; Head of IT Operations Siobhan Kerr; Atlas team lead Aoife Gallagher (IT Business Applications); Penang Factory Manager Nurul Aziz; Platform engineers Piotr Zielinski, Hannah Osei, Ravi Chandrasekar, Ellie Brannigan, Femke Jansen (Rotterdam); Penang follow-the-sun Lim Wei Jie and Farah Iskandar; duty manager Dominic Farrell.

## 1. Superseded pairs (old -> new; only the new doc is relevant)

| Topic | Old doc (date) | New doc (date) | What changed |
|---|---|---|---|
| Passwords and MFA | sec-password-mfa-standard-v1 (2022-09-01) | sec-password-mfa-standard-v2 (2025-06-01) | Min length 12 -> 14 (standard users); privileged 15 -> 20; 90-day rotation -> change only on compromise; privileged 60-day rotation -> automatic rotation after each vault check-out; service accounts 12 months -> 180 days; lockout 5 attempts/15 min -> 10 attempts/30 min; SMS MFA accepted -> SMS removed (enrolment closed 2025-06-01, registrations removed 2025-09-30); FIDO2 mandatory for privileged and Penang OT remote access; exception max 6 months -> 90 days; review every 2 years -> annually. Kiosk (Penang) accounts still rotate every 90 days in v2. |
| Backup and retention | sec-backup-retention-standard-v1 (2023-02-15) | sec-backup-retention-standard-v2 (2025-04-01) | Daily incremental 35 -> 30 days; weekly full 8 -> 12 weeks; monthly 12 -> 24 months; Atlas finance yearly 7 years (unchanged); offsite to Rotterdam weekly (Monday 06:00) -> daily by 06:00 UK (STRATA-RTM-02); new immutable object-lock copy 30 days (STRATA-LDS-IMM); restore tests annual -> quarterly (April/July/October/January); failed restore test SEV3 -> SEV2; TesseraCloud snapshot every 6 h/14 days -> TimescaleDB daily base backup 01:00 UTC kept 30 days + WAL 7 days; new MEC-YYYYMM pre-close backup kept 13 months. |
| On-call | it-oncall-policy-v1 (2023-05-01) | it-oncall-policy-v2 (2025-01-15) | SEV1 page ack 15 min -> 5 min; SEV2 30 -> 15 min (v2 takes values from it-incident-severity-definitions); SEV1 escalation to secondary 15 -> 5 min; Penang follow-the-sun (weekdays 00:00-08:00 UK) added; IT Security 24x7 own rota (v1: business hours only); post-page late start 4 -> 5 hours. |
| Change management | it-change-management-procedure-v1 (2023-01-10) | it-change-management-procedure-v2 (2025-03-03) | CAB Tuesday 14:00 UK -> Wednesday 10:30 UK; normal lead time 5 -> 3 business days; submission cut-off Thursday 12:00 before CAB -> Monday 16:00 before CAB; Atlas freeze last BD + first 2 BDs -> BD-2 to BD+3; FY-end freeze 25 Mar-5 Apr -> 20 Mar-10 Apr; standard change catalogue introduced (SC-004, SC-009, SC-011, SC-017, SC-021, SC-026); Penang line changes need factory manager sign-off. |
| Message broker ADR | eng-adr-012-message-broker-rabbitmq (2022-11-21) | eng-adr-019-message-broker-kafka (2024-09-12, supersedes ADR-012) | RabbitMQ 3 nodes rmq-prd-01..03 -> Kafka 6 brokers kafka-prd-01..06 port 9093, RF 3, min.insync.replicas 2; motivated by INC-2024-031 and 6,200 msg/s peak (>5,000 trigger). Dual publish from 2024-11-04; consumers moved by 2024-12-16; RabbitMQ decommissioned 2025-02-10. |
| Time-series ADR | eng-adr-008-timeseries-influxdb (2022-03-08) | eng-adr-023-timeseries-timescaledb (2024-11-04, supersedes ADR-008) | InfluxDB, raw 90 days, dual-write HA -> TimescaleDB tsdb-prd-01 port 5432, sync standby tsdb-prd-02, DR tsdb-dr-01 (euc1); raw 400 days (Enterprise 36 months via archive tier); 1-minute aggregates 5 years; compression after 7 days; read cut-over 2025-01-27; InfluxDB decommissioned 2025-02-24; ~38 TB. |

## 2. Confusable near-duplicates

### Site network/VPN guides

| Item | Leeds (it-network-vpn-guide-leeds) | Rotterdam (-rotterdam) | Austin (-austin, html) | Penang (-penang) |
|---|---|---|---|---|
| Supernet | 10.10.0.0/16 | 10.20.0.0/16 | 10.30.0.0/16 | 10.40.0.0/16 (+ OT 10.41.0.0/16) |
| User VLAN / subnet | 110 / 10.10.16.0/20 | 210 / 10.20.16.0/21 | 310 / 10.30.16.0/22 | 410 / 10.40.16.0/20 |
| Printer VLAN | 130 / 10.10.40.0/24 | 230 / 10.20.40.0/24 | 330 / 10.30.40.0/24 | 430 / 10.40.40.0/24 |
| DNS | 10.10.1.10/.11 | 10.20.1.10/.11 | 10.30.1.10/.11 | 10.40.1.10/.11 |
| VPN endpoint | vpn-lds.meridian-instruments.com | vpn-rtm... | vpn-aus... | vpn-pen... |
| VPN port | UDP 51820 | UDP 51820 | UDP 51820 | UDP 443 (ISPs filter non-standard UDP) |
| VPN fallback | vpn-rtm | vpn-lds | vpn-lds | vpn-lds |
| Idle timeout | 8 h | 8 h | 10 h | 8 h |
| Print server | prn-lds-01 | prn-rtm-01 | prn-aus-01 | prn-pen-01 |
| Queues | LDS-FL1-MFP01, LDS-FL2-MFP02 (finance), LDS-FL3-MFP03 (engineering), LDS-LAB-COLOUR | RTM-OFF-MFP01, RTM-REP-ZEBRA01, RTM-WH-ZEBRA02 | AUS-MFP01, AUS-ENG-PLOTTER | PEN-ADM-MFP01, PEN-SMT-LABEL01..04, PEN-FT-LABEL01, PEN-WH-ZEBRA01 |
| Special SSID | Meridian-Lab (VLAN 150) | Meridian-Repair (VLAN 250) | Meridian-Demo (VLAN 350) | none on OT; guest 8 h (others 12 h) |
| Local IT | ground floor, 08:00-18:00 | on-site Tue and Thu only | part-time Mon/Wed/Fri 09:00-17:00 CT | admin building 07:30-17:30 MYT |

### TesseraCloud environment configs

| Item | dev (html) | staging (html) | prod (html) |
|---|---|---|---|
| API host:port | api.dev.tessera.meridian.internal:8443 | api.staging.tesseracloud.meridian-instruments.com:443 | api.tesseracloud.meridian-instruments.com:443 |
| Ingest host:port | mqtt-dev.tessera.meridian.internal:1883 (no TLS) | ingest.staging.tesseracloud.meridian-instruments.com:18883 | ingest.tesseracloud.meridian-instruments.com:8883 |
| Health port | 9081 | 8081 | 8081 |
| TimescaleDB | tsdb-dev-01:5432 | tsdb-stg-01:5433 | tsdb-prd-01:5432 |
| Kafka | kafka-dev-01:9092 plaintext | kafka-stg-01..03:9094 | kafka-prd-01..06:9093 |
| Redis | redis-dev-01:6379 | redis-stg-01:6381 | redis-prd-01:6380 |
| TC_INGEST_BATCH_SIZE | 500 | 2000 | 5000 |
| TC_INGEST_MAX_CONNECTIONS | 500 | 5000 | 60000 |
| TC_RETENTION_RAW_DAYS | 7 | 30 | 400 |
| TC_KAFKA_CONSUMER_GROUP | tc-stream-dev | tc-stream-stg | tc-stream-prd |
| TC_API_RATE_LIMIT_RPM | 0 (disabled) | 150 | 300 (Enterprise override 1,200) |
| TC_LOG_LEVEL | debug | info | warn |
| TC_FEATURE_ANOMALY_DETECTION | true | true | false |
| Forge env / approval | tesseracloud-dev, none; reset Sundays 03:00 UK | tesseracloud-staging, auto on merge to main; 48 h soak | tesseracloud-prod, two maintainers, Tue/Thu 06:00-08:00 UK window |

Other confusables: postmortems (INC-2024-031 SEV1 TesseraCloud vs INC-2024-052 SEV2 Forge vs INC-2025-007 SEV2 Atlas vs INC-2025-014 SEV2 Penang SMT-2); LF-60 vs LF-60P final test limits (mfg-lumen-lf60-final-test-spec: 24 bar/60 s vs 60 bar/120 s; +/-0.5% vs +/-0.4%; test points up to 11 vs 14 m/s; TP-LF60-07 vs TP-LF60P-04; zero stability 0.005 vs 0.003 m/s); SMT reflow profiles per board (RP-HX-07 245 C, RP-GW-04 243 C, RP-TS4E-03 240 C with N2 O2 <1000 ppm, RP-LF-02 238 C); stencil thickness (HX-MB 0.12, TS4-GW 0.12, TS4E-EN 0.10, LF60-SB 0.13 mm).

## 3. Deep-buried facts in long documents (2,500-5,000 words)

- it-atlas-month-end-close-runbook: step 14 FXLOAD loads ECB rates at 16:30 CET (15:30 UK) on BD-1, hold if any currency moves >3% vs previous business day without Group Treasury sign-off; step 22 verify Calderbrook SFTP host key (sftp.calderbrook-payroll.co.uk port 2222) against vault entry payx/calderbrook/hostkey (INC-2025-007 AI-3); step 3 standby lag < 5 min; step 4 /u02/arch warn 80%, act 85%; step 18 ARCH-PURGE only if standby lag < 5 min; step 5 pre-close backup BD-3 22:00 tagged MEC-YYYYMM kept 13 months; step 25 release PAYX, alerts to atlas-oncall@ (old atlas-ops@ decommissioned); PAYX runs 14:00 BD+1, bureau cut-off 17:00 UK BD+1; step 29/30 CLOSE-LOCK sets atlas.period.lock=true; step 31 close reports archived on atlas-rpt-prd-01 for 7 years; step 2 app CPU >70%/heap >85%; batch order ACCRUE, FXLOAD, ICRECON, GLRECON, PAYX-MONTHLY, CLOSE-LOCK on queue MEC_BATCH; stand-up 09:15 UK in Teams "MEC Bridge"; BD-3 CAB-approved changes must finish by 18:00 UK.
- eng-tesseracloud-failover-runbook: step 9 consumer lag of tc-stream-prd (origin INC-2024-031 AI-2, formerly RabbitMQ queue depth); step 14 MirrorMaker 2 replication-latency-ms-max < 30 s (30000 ms), wait up to 10 min, then IC may accept loss within 5-min RPO; step 4 /healthz on port 8081; step 7 Forge deploy freeze on tesseracloud-prod; step 16 tcctl failover promote --region euc1 --confirm; step 22 tcctl dns switch --target euc1 (TTL 60 s); step 24 scale tc-ingest if CPU >85% for 10 min (to 24 replicas); RTO 60 min, RPO 5 min; tcctl >= 2.14; approval by Head of Platform Engineering or duty manager (IC may proceed alone after 10 min without answer); failback only after 24 h stable in euc1 with 48 h customer notice; drills April and October, last 2025-04-15 took 38 min; gateways below firmware 5.0.0 (<4% of fleet in June 2025) reconnect without backoff.
- eng-forge-runner-rotation-runbook: step 14 new token expiry must be >= 59 days (about 30 days means legacy override in /etc/forge-runner/env); step 5 drain timeout 45 min; step 6 Penang /var/lib/forge/hil.lock; step 22 flag if >3 runners expire within 24 h; FORGE_RUNNER_TOKEN_TTL=1440h (was 720h); rotation every 45 days, waves <=25% and never >2 Leeds runners; FORGE_RUNNER_CONCURRENCY default 6; Vault secret/forge/runners/<hostname>; forgectl >= 2.6; SC-017 under CHG-STD; cache forge-cache-lds 14 days, artefacts 30 days; continue a wave only if >= 6 Leeds Linux runners online.
- mfg-penang-smt-changeover-runbook: step 14 FAI: SPI volume 75-150%, AOI false calls threshold 500 ppm, QA signature; step 19 two-person reflow profile verification (INC-2025-014, added revision D 2025-04-07); step 10 feeder barcode mismatch locks machine; step 12 N2 O2 <1000 ppm for RP-TS4E-03; target 25 min, escalate to shift lead >40 min (target cut from 35 min in revision C); paste SAC305 Type 4, stored 2-10 C, thaw >= 4 h, open <= 24 h; stencil tension >= 35 N/cm; wipe every 5 prints TS4E-EN else 10; FAI board kept 30 days; form MFG-F-112.
- mfg-halcyon-calibration-procedure: step 14 >10% pre-adjustment deviation -> "drift" flag in Atlas quality module, >20% -> replace sensor; step 12 120 s stabilisation; step 17 +/-5% (e.g. CO 47.5-52.5 ppm on 50 ppm); T90 CO 35 s, H2S 30 s, LEL 20 s, O2 15 s; step 22 HX-220 GPS fix within 120 s; step 26 alarm 95 dB (HX-200/210) or 103 dB (HX-220) at 30 cm; step 28 due date 180/120/150 days (HX-200/210/220); cylinders +/-2%; 20 +/-5 C, 30-70% RH; stations CAL-PEN-01..06 (05 rework, 06 outgoing audit, 1 in 50), CAL-RTM-01/02; MFG-F-207 kept 10 years; lot hold at >3% drift in a week.

## 4. Other exact identifiers and table facts

- Severity table (it-incident-severity-definitions): SEV1 ack 5 min, IC 15 min, updates 30 min, restore 4 h; SEV2 15 min / 30 min / 60 min / 8 h; SEV3 4 business hours / none / daily / 3 business days; SEV4 2 business days / none / on resolution / 20 business days. Business hours 08:00-18:00 local. PIR within 5 business days for SEV1/2. P1-P4 map to SEV1-4.
- On-call v2 escalation: SEV1 secondary after 5 min, manager after 10 min; SEV2 15/30 min; join channel within 10 min of ack; max two consecutive weeks as primary; stay within 15 minutes of a laptop.
- Rotation Q3 FY26 (html): week of 2025-10-13 primary Ravi Chandrasekar, secondary Ellie Brannigan, Penang Lim Wei Jie, duty manager Dominic Farrell; drill 14 October 2025; Femke Jansen hands over 11:00 CET.
- Beacon categories: ACC-NEW, ACC-REVOKE, ACC-MFA (P2), HW-LAPTOP, SW-INSTALL (P4), NET-VPN, NET-WIFI, PRN-QUEUE, ATLAS-FIN (P3, P2 in close), ATLAS-PAYX (P2), ATLAS-MFG, KEEL-HR, FORGE-RUNNER, FORGE-ACCESS, INC-PLATFORM, MFG-OT (P2, 24x7), CHG-STD/NORMAL/EMERG, SEC-INCIDENT (P1), SEC-INCIDENT-PD ("Security Incident – Personal Data", 2 hours; aligns with HR data protection policy), SEC-PHISH, SEC-VULN, SEC-EXCEPTION, SEC-ACCESS-REVIEW, SEC-BACKUP, SEC-BACKUP-TEST.
- Port matrix: Atlas DB 1521; payroll SFTP 2222; Forge Git SSH 2224; print 9100/631; OT jump RDP 3389; ingest 8883; Kafka 9093; TimescaleDB 5432; Redis 6380; plain MQTT 1883 dev only.
- Vulnerability SLAs: Critical 72 h internet-facing / 7 days internal; High 7/30 days; Medium 30/90; Low 90 days/next maintenance; Critical exceptions max 30 days (CTO + Head of IT Security); scans Sunday 04:00 UK; Forge stage security-gate; patching SC-004 second week of month.
- Access reviews: quarterly in May, August, November, February; 10 business days to respond; removals within 5; 10% sample; evidence 7 years; movers keep old access max 30 days.
- PAM: vault pam.meridian.internal; check-out 4 h default, 8 h max; recordings 1 year; break-glass use = SEV3; pamctl elevate up to 12 h.
- DR overview: TesseraCloud Tier 0 RTO 60 min/RPO 5 min; Atlas Tier 1 RTO 8 h/RPO 15 min; Penang OT RTO 4 h/RPO 24 h; Forge 24 h/24 h; Beacon 24 h/4 h; Keel 48 h/24 h; file shares 72 h/24 h; Atlas restore test 2025-07-08 took 6 h 20 min.
- Kafka topics: tessera.telemetry.raw 48 partitions 7 days; tessera.telemetry.agg 24/3 days; tessera.alerts 12/14 days; tessera.devices.events 6/30 days; platform.audit.events 6/90 days (not mirrored); repo platform/kafka-topics; auto.create.topics.enable false.
- Forge pipeline: stages build, unit-test, security-gate, package, deploy (+ hil-test, sign for firmware); job timeout 60 min (HIL 180); release artefacts 5 years; runner tags lds, large (lds-05/06), release (lds-07/08), halcyon (pen-01/02), tessera (pen-03/04), win.
- ADR-015: concurrency Leeds Linux 6, Penang HIL 1, Windows 2; FORGE_RUNNER_POOL_LOW < 5 Leeds runners; cache cut pipeline time 35%; review if queue wait > 10 min for a month.
- Alert catalogue: TC_INGEST_CONNECTIONS_ZERO SEV1 (2 min); TC_CONSUMER_ACK_RATE_ZERO SEV2 3 min (INC-2024-031 AI-5); TC_STREAM_CONSUMER_LAG_HIGH > 500,000 msgs 10 min; TSDB_REPLICATION_LAG > 60 s; ATLAS_PAYX_FAILED SEV2; OT_NEW_DEVICE_DETECTED SEV3.
- TS-4 firmware release: soak 7 days (major/minor) or 48 h (patch) on 40 gateways + 25 nodes; pilot 2% for 7 days; rings 10/25/50/100% with >= 48 h gaps; halt if >0.5% fail to reconnect in 30 min, >1% watchdog resets in 24 h, or linked SEV2+; minimum gateway 5.2.0 (from 1 Oct 2025), node 2.6.1.
- SMT throughput table (docx): e.g. SMT-3 TS4E-EN rated 514 boards/h, FY25 actual 436, OEE 80%; SMT-4 TS4-GW rated 112, actual 90; SMT-2 TS4E-EN OEE 74% (79% excluding INC-2025-014); planning 85% of rated x 120 h; TS4-GW is FY26 constraint; FY26 OEE target 80%, FPY 98.5%.
- MSL: MSL 3 168 h, MSL 4 72 h, MSL 5 48 h, 5a 24 h; dry cabinet < 5% RH; warn at 80% floor life; trays baked 125 C 24 h; max 2 bakes; records 3 years.
- ESD: tester pass 750 kOhm-35 MOhm; worksurface < 1x10^9 ohm monthly, floor quarterly; ionisers +/-35 V quarterly; RH 40-60%; training every 12 months.
- OT network: zones 10.41.1-4.0/24 for SMT-1..4, 10.41.10.0/24 test/calibration, 10.41.20.0/24 label printers, 10.41.30.0/24 HIL rigs; syslog 6514; vendor sessions max 2 h; staff 4 h; OT logs 12 months.
- Rotterdam Repair Centre: triage within 2 business days; turnaround 10 business days; Lumen sent to Penang (no reference rig in Rotterdam), FY25 average 14.6 days; missing MI-F-031 -> quarantine; flag to Support by day 7.
- Laptop provisioning: build VLAN 199; enrolment profiles MER-LDS/RTM/AUS/PEN; build ~40 min; leaver laptops quarantined 30 days.
- Phishing: service desk ext 4357 / +44 113 496 0400; phish@meridian-instruments.com; refresher module within 14 days.

## 5. Cross-document chains

1. Severity -> on-call: it-oncall-policy-v2 says pages are acknowledged within the time in it-incident-severity-definitions (SEV2 = 15 minutes; SEV1 = 5 minutes).
2. INC-2025-007 -> Atlas runbook: AI-3 added step 22 host-key check (it-postmortem-inc-2025-007 + it-atlas-month-end-close-runbook); alert routing AI-1/AI-5 -> eng-monitoring-alert-catalogue ATLAS_PAYX_FAILED.
3. INC-2024-031 -> failover runbook step 9 (AI-2) -> ADR-019 (AI-4) -> Kafka topic conventions. Also AI-5 -> TC_CONSUMER_ACK_RATE_ZERO alert; AI-6 -> canary on tesseracloud-prod (config prod page); AI-3 -> gateway firmware 5.0.0 backoff (release process doc and failover runbook).
4. INC-2024-052 -> Forge runbook: TTL 720h -> 1440h, step 14 59-day check, alert FORGE_RUNNER_TOKEN_EXPIRY_SOON at 10 days (SEV3) -> alert catalogue; SC-017 appears in it-change-management-procedure-v2 catalogue.
5. INC-2025-014 -> SMT changeover runbook step 19 (revision D, 2025-04-07); nitrogen interlock AI-3; throughput doc notes SMT-2 OEE impact.
6. Forge rotation blackout = Atlas close window BD-2 to BD+3 (eng-forge-runner-rotation-runbook + it-atlas-month-end-close-runbook + it-change-management-procedure-v2).
7. Pre-close backup MEC-YYYYMM 13 months overrides 30-day daily retention (it-atlas runbook step 5 + sec-backup-retention-standard-v2).
8. Failover is always SEV1 -> status page within 30 min (failover runbook + severity table); ECAB second approver for failover/failback is Head of Platform Engineering (change v2).
9. Penang VPN (UDP 443) -> OT jump host ot-jump-pen-01 needs FIDO2 (penang guide + password v2 + OT standard).
10. Halcyon recalibration in Rotterdam uses CAL-RTM-01/02 per calibration procedure (repair centre html + calibration procedure).

## 6. Deliberately NOT covered (negative questions)

1. Kubernetes cluster upgrade procedure or Kubernetes version for TesseraCloud.
2. SOC 2 or ISO 27001 certification audit results or dates.
3. Mobile phone MDM / BYOD enrolment steps for personal phones.
4. Findings or dates of an external penetration test.
5. A US region for TesseraCloud (only euw1 Dublin and euc1 Frankfurt exist) and its failover.
6. Any SMT line other than SMT-1 to SMT-4 (no SMT-5), or SMT manufacturing in Austin or Leeds.
7. Leeds data centre UPS/generator runtime or power runbook.
8. Wi-Fi 7 or any wireless upgrade roll-out plan.
9. CAD/EDA licence server hosts or ports.
10. Atlas ERP version upgrade schedule or vendor name.
11. The specific compensation for on-call (that is in the HR corpus, hr-overtime-on-call-policy, not in engineering docs).

## 7. Alignment notes with other writers

- TesseraCloud hostnames use the products corpus domain tesseracloud.meridian-instruments.com (api., ingest., status.).
- Gateway firmware numbering follows products (5.x; TesseraCloud minimum 5.2.0 from 2025-10-01; node 2.8.0 current, 2.6.1 minimum).
- Halcyon calibration uses product gases MI-CAL-H2S25 (HX-200, 0.5 L/min, MI-REG-05) and MI-CAL-Q4-B (HX-210/220, 1.0 L/min, MI-REG-10), 120 s wait, firmware 3.2.4/3.4.1/3.4.2, intervals 180/120/150 days.
- Rate limits are owned by products (prod-tesseracloud-api-rate-limits-v2: Standard 300, Enterprise 1,200); the prod config page matches (TC_API_RATE_LIMIT_RPM 300).
- Raw telemetry retention 400 days (Standard, covers 13 months) and 36 months Enterprise matches the products retention statement.
