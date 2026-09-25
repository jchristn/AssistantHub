# Support Ticket Priority Codes

Document ID: sup-ticket-priority-codes | Version: 1 | Effective: 2025-04-01 | Owner: Customer Support (Rosa Delgado)

## Purpose

Every customer ticket in the Meridian Support Centre (MSC) portal carries one of four priority codes, P1 to P4. The priority code decides the response target that applies under the customer's support contract. This document defines the codes only. Response times for each priority under the Essential, Professional and Premier contracts are set out in Customer Support SLA Tiers (sup-sla-tiers).

## Ticket numbers

MSC tickets are numbered MSC-YYYY-NNNNNN, where YYYY is the calendar year in which the ticket was opened and NNNNNN is a six-digit sequence that restarts on 1 January, for example MSC-2025-018842. A ticket keeps its number for life, even if it is reopened in a later year. RMA requests, field service visits and escalations all refer back to the originating MSC ticket number.

## Priority definitions

| Code | Name | Definition | Typical examples |
|---|---|---|---|
| P1 | Critical | A safety risk, or a complete loss of gas detection, vibration monitoring or flow measurement across a site or a production line, with no workaround | Every HX-210 in a confined-space crew failing bump test; TesseraCloud tenant unreachable for all users; TS-4 gateway fleet offline at a site; LF-60P custody-transfer meter reading zero on a live line |
| P2 | High | A major function is degraded or several devices are affected, and no acceptable workaround exists | HX-DOCK-4 unable to calibrate any instrument; alarms not reaching one site in TesseraCloud; repeated T-320 backhaul drops on one gateway |
| P3 | Medium | A single device or non-critical function is affected, or a workaround exists | One HX-220 showing E-131; a single TS-4e node reporting T-305; Modbus polling intermittently slow on one LF-60 |
| P4 | Low | Questions, how-to requests, documentation feedback and enhancement requests | How to export a datalog; request for an API example; request for a new report layout |

## Who sets the priority

- The customer proposes a priority when logging the ticket in the MSC portal.
- The first-line (L1) support engineer confirms or adjusts it during triage, using the definitions above, and records the reason for any change in the ticket.
- Any report involving a possible gas exposure, injury or failure of a safety instrument to alarm is always P1, whatever priority was proposed.

## Upgrading and downgrading

- A customer may request an upgrade at any time by phone or by adding a comment beginning "PRIORITY UPGRADE" to the ticket. The request is reviewed within one hour during coverage hours.
- The regional duty manager may upgrade any ticket. Only the duty manager or the regional support manager may downgrade a P1.
- A ticket may be downgraded once a workaround is in place, for example from P1 to P3 after an advance replacement unit is delivered. The customer is told of every downgrade.
- If a P3 or P4 ticket is reopened more than twice for the same fault, it is automatically raised by one priority level.

## Priority and escalation

Priority determines when escalation steps are triggered. See Customer Support Escalation Procedure (sup-escalation-procedure) for the L1/L2/L3 path and management notification rules.
