# User Access Review Procedure

Document ID: sec-access-review-procedure. Owner: IT Security. Effective date: 3 March 2025.

## Purpose

Periodic access reviews confirm that people only hold the system access they need for their current role. This procedure covers the quarterly and semi-annual reviews of access to Meridian business systems.

## Review frequency

| System | Scope reviewed | Frequency | Reviewer |
|---|---|---|---|
| Atlas (finance roles) | All users with GL, AP, AR, payroll or period-close roles | Quarterly | Financial Controller |
| Atlas (manufacturing module) | Shop-floor and planning roles | Semi-annual | Site operations managers |
| Keel HR portal | HR administrator and manager self-service elevated roles | Quarterly | HR Operations lead |
| Forge | Maintainer and owner roles; production deploy rights | Quarterly | Head of Platform Engineering |
| TesseraCloud admin console | Operator and support roles | Quarterly | Head of Platform Engineering |
| Beacon | Agent and administrator roles | Semi-annual | IT Service Desk manager |
| Privileged directory groups | Domain admins, backup admins, firewall admins | Quarterly | Head of IT Security |

Quarterly reviews run in the second month of each fiscal quarter (May, August, November and February), so they do not overlap with quarter-end close.

## Procedure

1. IT Security extracts the entitlement list for each system on the first business day of the review month.
2. Extracts are loaded into a Beacon review task (category SEC-ACCESS-REVIEW), one task per system and reviewer.
3. Reviewers mark each entitlement as Keep, Remove or Modify within 10 business days.
4. Removals are actioned by the system administrators within 5 business days of the reviewer's decision.
5. IT Security samples 10% of Keep decisions and challenges any that conflict with segregation-of-duties rules (for example, a user who can both create suppliers and approve payments in Atlas).
6. Unreturned reviews are escalated to the reviewer's director after 10 business days and to the CTO after 15.
7. The completed review evidence is retained for 7 years for audit.

## Leavers and movers

Leaver access is removed on the last working day through the Keel leaver workflow, which raises a Beacon ticket in category ACC-REVOKE. Movers keep old access for no more than 30 days after a role change.

## Segregation of duties

The segregation-of-duties matrix for Atlas is maintained by Finance and IT Business Applications. Conflicts found during review must be removed or covered by a documented compensating control approved by the Financial Controller.
