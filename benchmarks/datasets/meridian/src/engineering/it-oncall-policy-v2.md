# On-call Policy (v2)

Document ID: it-oncall-policy-v2. Owner: IT Operations (Head of IT Operations: Siobhan Kerr). Version 2. Effective date: 15 January 2025. Supersedes: On-call Policy v1 (May 2023). Approved by: Lars Hedegaard, CTO.

## What changed from v1

- Paging acknowledgement times are no longer defined in this policy. They are taken from the Acknowledge column of the Incident Severity Definitions (it-incident-severity-definitions). At the time of publication that means SEV1 pages must be acknowledged within 5 minutes (was 15 minutes in v1) and SEV2 pages within 15 minutes (was 30 minutes).
- Escalation to the secondary now happens automatically after 5 minutes without acknowledgement for SEV1 (was 15 minutes).
- A follow-the-sun rotation was added: Penang covers the Platform Engineering primary from 00:00 to 08:00 UK time on weekdays.
- IT Security now runs its own 24x7 on-call rotation.
- Post-page rest increased: after being paged between 00:00 and 06:00, the responder may start up to 5 hours later.

## Scope

This policy applies to engineers in IT Operations, Platform Engineering, IT Security and Penang OT Support who take part in an on-call rotation. It covers Atlas, Beacon, Keel integrations, Forge, TesseraCloud, the corporate and OT networks.

## Rotations

| Rotation | Coverage | Handover | Roles |
|---|---|---|---|
| Platform Engineering (TesseraCloud, Forge) | 24x7 | Monday 10:00 UK | Primary, secondary |
| Platform Engineering Penang follow-the-sun | Weekdays 00:00 to 08:00 UK | Monday 10:00 UK | Primary only |
| IT Operations (Atlas, network, directory, Beacon) | 24x7 | Monday 09:00 UK | Primary, secondary |
| IT Security | 24x7 | Wednesday 09:00 UK | Primary |
| Penang OT Support (SMT lines, OT network) | 24x7, aligned to the three factory shifts | Monday 08:00 MYT | Primary |

The current Platform Engineering schedule is published in the on-call rotation table for the quarter.

## Paging service levels

Paged responders must acknowledge within the Acknowledge time for the incident's severity in the Incident Severity Definitions, and must join the incident channel within 10 minutes of acknowledging a SEV1 or SEV2. SEV3 alerts are paged only during business hours; SEV4 is never paged.

## Escalation

| Severity | Escalate to secondary after | Escalate to manager after |
|---|---|---|
| SEV1 | 5 minutes without acknowledgement | 10 minutes |
| SEV2 | 15 minutes without acknowledgement | 30 minutes |
| SEV3 | Not escalated automatically | Not escalated automatically |

For TesseraCloud SEV1 incidents that may require regional failover, the on-call primary pages the Head of Platform Engineering (Kofi Mensah-Bonsu) or the delegated duty manager, as described in the TesseraCloud Regional Failover Runbook.

## Expectations

- Stay within 15 minutes of a laptop and a reliable internet connection.
- Do not consume alcohol or anything that would impair judgement while on call.
- Record all actions in the Beacon incident ticket.
- Hand over open incidents in writing at handover.

## Swaps

Swaps are recorded in the paging tool at least 24 hours in advance. A person may not be primary for more than two consecutive weeks.

## Review

Paging statistics (time to acknowledge, pages per week, out-of-hours pages) are reviewed monthly by the Head of IT Operations and the Head of Platform Engineering.
