# Change Management Procedure (v2)

Document ID: it-change-management-procedure-v2. Owner: IT Operations (Head of IT Operations: Siobhan Kerr). Version 2. Effective date: 3 March 2025. Supersedes: Change Management Procedure v1 (January 2023). Approved by: Lars Hedegaard, CTO.

## What changed from v1

- The CAB meeting moved from Tuesday 14:00 UK to Wednesday 10:30 UK so that Penang (17:30 MYT) and Austin (04:30 CT, asynchronous review) can take part in the same cycle.
- Lead time for normal changes reduced from 5 business days to 3 business days. The submission cut-off is now 16:00 UK time on the Monday before the CAB.
- The Atlas month-end freeze was extended to the full close window BD-2 to BD+3.
- The fiscal year-end freeze changed from 25 March to 5 April to 20 March to 10 April.
- A published standard change catalogue replaces ad hoc pre-approvals.
- Changes that affect Penang production lines require sign-off from the Penang factory manager (Nurul Aziz) or her delegate.

## Change types

| Type | Description | Approval | Beacon category |
|---|---|---|---|
| Standard | Listed in the standard change catalogue | Pre-approved | CHG-STD |
| Normal | Any change not in the catalogue | CAB | CHG-NORMAL |
| Emergency | Restore service, or fix a Critical vulnerability | ECAB | CHG-EMERG |

## Change Advisory Board

The CAB meets every Wednesday at 10:30 UK time on Teams. Members: Head of IT Operations (chair), Head of Platform Engineering, Head of IT Security, IT Business Applications lead (Atlas), Penang OT lead, and a representative of any affected site.

Normal changes must be submitted in Beacon (category CHG-NORMAL) at least 3 business days before implementation and before 16:00 UK time on the Monday preceding the CAB meeting.

## Emergency changes (ECAB)

ECAB approval requires two members, one of whom must be the Head of IT Operations or the CTO. For TesseraCloud regional failover and failback, the Head of Platform Engineering may act as the second approver. The Beacon record must be completed within 2 business days after implementation, and every emergency change is reviewed at the next CAB.

## Standard change catalogue (extract)

| ID | Standard change | Owner |
|---|---|---|
| SC-004 | Monthly OS patching of non-production and production Linux servers | IT Operations |
| SC-009 | Firewall rule add for an approved Beacon access request | Network team |
| SC-011 | TesseraCloud tc-api horizontal scale-out within approved limits | Platform Engineering |
| SC-017 | Forge runner token rotation (per the Forge Runner Token Rotation Runbook) | Platform Engineering |
| SC-021 | Printer queue creation at any site | IT Service Desk |
| SC-026 | Atlas user role assignment approved through access request | IT Business Applications |

## Change windows

- Standard maintenance window: Saturday 20:00 to Sunday 06:00 UK time.
- Penang factory systems: Sunday 08:00 to 14:00 MYT.
- TesseraCloud production: Tuesday and Thursday 06:00 to 08:00 UK time, or any time for zero-downtime deployments via Forge.

## Change freezes

| Freeze | Window | Allowed changes |
|---|---|---|
| Atlas month-end close | BD-2 to BD+3 each month | Emergency only (ECAB) for Atlas and its integrations |
| Fiscal year end | 20 March to 10 April | Emergency only across all systems |
| Leeds office closure | 23 December to 2 January | Emergency and standard only |

## Records

Every change in Beacon includes a risk assessment, a rollback plan, test evidence and the implementer. Changes without a rollback plan are rejected at CAB.
