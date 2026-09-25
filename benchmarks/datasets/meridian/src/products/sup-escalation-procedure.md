# Customer Support Escalation Procedure

Document ID: sup-escalation-procedure | Version: 1 | Effective: 2025-05-01 | Owner: Customer Support (Rosa Delgado)

## Purpose

This procedure describes how customer tickets in the Meridian Support Centre (MSC) are escalated from first-line support to product engineering, and when senior management must be informed. It applies to all four regional support hubs and to all ticket priorities defined in sup-ticket-priority-codes.

## Regional support hubs

| Hub | Site | Covers | Local coverage hours (Professional contracts) |
|---|---|---|---|
| Leeds | Meridian Instruments Ltd, HQ | UK, Ireland, Middle East, Africa | 07:00-20:00 GMT/BST |
| Rotterdam | Meridian Instruments B.V. | EU and rest of Europe | 07:00-20:00 CET/CEST |
| Austin | Meridian Instruments Inc. | Americas | 07:00-20:00 US Central |
| Penang | Meridian Instruments Sdn Bhd | Asia-Pacific | 07:00-20:00 MYT |

Premier 24x7 cover is provided by a follow-the-sun rota across the four hubs. Out-of-hours P1 calls are routed to the hub whose duty engineer is on shift.

## Escalation levels

- L1 Support Engineer: triages the ticket, confirms priority, collects serial numbers, firmware versions, logs and error codes, and works the published fix (error-code tables, troubleshooting compendium sup-troubleshooting-compendium).
- L2 Senior Product Specialist: one per product line per hub (Halcyon, Tessera, Lumen). Handles faults not resolved by published procedures, reproduces issues on lab units, and decides on RMA or field visit.
- L3 Product Engineering: firmware, hardware and TesseraCloud engineering teams, reached through a Forge engineering issue linked to the MSC ticket. L3 owns root-cause analysis, hotfixes and field notices.

## Escalation triggers

| Priority | L1 to L2 | L2 to L3 |
|---|---|---|
| P1 | Immediately on confirmation | If not restored within 4 hours, or at once for any suspected firmware or safety defect |
| P2 | Within 4 hours if no fix identified | Within 2 business days |
| P3 | Within 2 business days | At L2 discretion |
| P4 | Not escalated, except for documentation errors | Enhancement requests go to Product Management |

## Duty manager rota

Each hub has a duty manager on a weekly rota, published in the MSC portal staff calendar and changing over at 09:00 local time each Monday. The duty manager is paged automatically for every new P1, approves out-of-policy advance replacements, and is the only person besides the regional support manager who may downgrade a P1. If the duty manager does not acknowledge a page within 15 minutes, the page repeats to the regional support manager.

## Management notification

- The Head of Customer Support, Rosa Delgado, is informed by the duty manager within 1 hour of any P1 ticket that involves a safety concern, and within 4 hours of any other P1 open for more than 8 hours.
- The CTO, Lars Hedegaard, is informed by Rosa Delgado within 4 hours of any P1 safety incident: a Halcyon instrument failing to alarm in the presence of gas, a suspected intrinsic safety defect, or a TesseraCloud outage affecting more than one tenant.
- The VP Product, Samuel Achterberg, is informed when L3 confirms a defect that requires a field notice or a stop-ship.
- The customer's account manager is copied on every P1 and on any P2 open for more than 5 business days.

## Customer communication

For P1 tickets, the ticket owner updates the customer at least every 2 hours during coverage hours until service is restored, then provides a written incident summary within 5 business days. For safety incidents, the summary is reviewed by L3 and approved by Rosa Delgado before release.

## Closing an escalation

An escalated ticket returns to L1 for closure once the fix is confirmed by the customer. L3 root-cause findings are recorded in the Forge issue and summarised in the MSC ticket. Recurring faults are added to the troubleshooting compendium at its next revision.
