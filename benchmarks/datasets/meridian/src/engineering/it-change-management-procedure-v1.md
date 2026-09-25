# Change Management Procedure (v1)

Document ID: it-change-management-procedure-v1. Owner: IT Operations. Version 1. Effective date: 10 January 2023. Approved by: Lars Hedegaard, CTO.

## Purpose

This procedure controls changes to production IT systems at Meridian Instruments so that changes are assessed, approved, scheduled and recorded. It applies to Atlas, Beacon, Keel integrations, Forge, the corporate network, the Leeds data centre and TesseraCloud production.

## Change types

| Type | Description | Approval | Beacon category |
|---|---|---|---|
| Standard | Low-risk, repeatable change from the pre-approved catalogue | Pre-approved | CHG-STD |
| Normal | Any change not in the catalogue | Change Advisory Board (CAB) | CHG-NORMAL |
| Emergency | Change needed to restore service or fix a critical vulnerability | Emergency CAB (ECAB) | CHG-EMERG |

## Change Advisory Board

The CAB meets every Tuesday at 14:00 UK time in the Leeds boardroom and on Teams. Members: Head of IT Operations (chair), Head of Platform Engineering, Head of IT Security, IT Business Applications lead, and a representative of each affected site.

Normal changes must be submitted in Beacon at least 5 business days before the planned implementation date and no later than 12:00 UK time on the Thursday before the CAB meeting at which they will be reviewed.

## Emergency changes

Emergency changes are approved by at least two ECAB members, one of whom must be the Head of IT Operations or the CTO. The change record must be completed within 2 business days after implementation.

## Change windows

- Standard maintenance window: Saturday 20:00 to Sunday 06:00 UK time.
- Penang factory systems: Sunday 08:00 to 14:00 MYT when lines are stopped for preventive maintenance.

## Change freezes

- Atlas month-end: the last business day of the month and the first 2 business days of the next month.
- Fiscal year end: 25 March to 5 April.

## Post-implementation review

Failed changes, and any change that caused an incident, receive a post-implementation review at the next CAB.

## Records

All changes are recorded in Beacon with a rollback plan, test evidence and the name of the implementer.
