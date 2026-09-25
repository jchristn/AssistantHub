# IT Disaster Recovery Overview

Document ID: it-disaster-recovery-overview. Owner: IT Operations and Platform Engineering. Approved by: Lars Hedegaard, CTO. Effective date: 1 July 2025.

## Purpose

This document summarises how Meridian Instruments recovers its critical IT services after a major disruption, and the recovery objectives the business has agreed for each service. Detailed technical procedures are held in the system runbooks referenced below.

## Service tiers and recovery objectives

| Service | Tier | Recovery time objective (RTO) | Recovery point objective (RPO) | Recovery site | Detailed procedure |
|---|---|---|---|---|---|
| TesseraCloud (customer SaaS) | Tier 0 | 60 minutes | 5 minutes | euc1 (Frankfurt) | TesseraCloud Regional Failover Runbook |
| Atlas ERP | Tier 1 | 8 hours | 15 minutes | Rotterdam server room using standby atlas-db-prd-02 replica copy | Atlas DR procedure (IT Business Applications) |
| Penang OT systems (SMT line control, calibration stations) | Tier 1 | 4 hours | 24 hours | Local spares at Penang | Penang OT recovery plan |
| Forge CI/CD | Tier 2 | 24 hours | 24 hours | Rebuild from backup in Leeds | Forge rebuild procedure |
| Beacon service desk | Tier 2 | 24 hours | 4 hours | Vendor-hosted | Vendor contract |
| Keel HR portal | Tier 2 | 48 hours | 24 hours | Vendor-hosted | Vendor contract |
| File shares | Tier 3 | 72 hours | 24 hours | Restore from Strata Backup | Backup and Retention Standard v2 |

## Invocation

A disaster is declared by the CTO, or in the CTO's absence by the Head of IT Operations or the Head of Platform Engineering. For TesseraCloud, a regional failover can be invoked by the Incident Commander with approval of the Head of Platform Engineering or the delegated duty manager, without waiting for a full disaster declaration.

## Dependencies

- Directory services are replicated to Rotterdam and Penang; sign-in continues if Leeds is lost.
- MFA is cloud-hosted and independent of the Leeds data centre.
- The privileged access vault has a warm standby in Rotterdam; break-glass credentials are also sealed in the Rotterdam site safe.

## Testing

| Test | Frequency | Last performed | Result |
|---|---|---|---|
| TesseraCloud regional failover drill | Twice a year (April and October) | 15 April 2025 | Pass, 38 minutes |
| Atlas restore to isolated host | Quarterly (with restore testing) | 8 July 2025 | Pass, 6 hours 20 minutes |
| Penang OT line PC rebuild from image | Annually | 11 February 2025 | Pass |
| Forge rebuild tabletop exercise | Annually | 3 December 2024 | Pass with actions |

## Communication

During a declared disaster, updates are issued on the SEV1 cadence in the Incident Severity Definitions (every 30 minutes). Customer communication for TesseraCloud uses status.tesseracloud.meridian-instruments.com.
