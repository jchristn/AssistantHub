# Postmortem: INC-2025-007 Atlas Payroll Export Failure

Document ID: it-postmortem-inc-2025-007-atlas-payroll-export. Incident: INC-2025-007. Severity: SEV2. Date of incident: 2 January 2025. Postmortem published: 14 January 2025. Author: Aoife Gallagher (Atlas team lead, IT Business Applications). Review chaired by: Siobhan Kerr (Head of IT Operations).

## Summary

During the December 2024 month-end close, the PAYX-MONTHLY job in Atlas failed to deliver the month-end payroll export file (payroll cost allocation, pension deductions and statutory reporting data for the closed month) to Calderbrook Payroll, Meridian's UK payroll bureau. The job failed at 14:03 UK time on BD+1 (2 January 2025) because Calderbrook had rotated the SSH host key on its SFTP server sftp.calderbrook-payroll.co.uk (port 2222), and Atlas refused the connection with a host key verification error. The failure alert was sent to the distribution list atlas-ops@meridian-instruments.com, which had been decommissioned in November 2024, so nobody saw it. The failure was only noticed at 16:40 when Calderbrook phoned the Payroll lead. The file was delivered at 17:38, after the bureau agreed to extend its 17:00 cut-off. December salaries had already been paid on 20 December 2024, so no employee pay was affected.

## Impact

- Payroll export delivered 38 minutes after the bureau's normal cut-off; bureau processing extended into the evening at Calderbrook's discretion.
- No employee payments were affected, but the monthly pension contribution submission deadline was at risk for approximately one hour.
- Around 6 hours of unplanned effort across Payroll, IT Business Applications and Finance.
- No data was exposed. The host key change was legitimate.

## Timeline (UK time, 2 January 2025)

| Time | Event |
|---|---|
| 30 Dec 2024 | Calderbrook rotates the host key on its SFTP server and emails notice to the old atlas-ops@ list |
| 13:55 | Payroll lead confirms in MEC Bridge that Keel payroll changes are complete |
| 14:00 | PAYX-MONTHLY released on queue MEC_BATCH |
| 14:03 | PAYX-MONTHLY fails: host key verification failed for sftp.calderbrook-payroll.co.uk port 2222 |
| 14:03 | Failure email sent to atlas-ops@meridian-instruments.com (decommissioned, silently dropped) |
| 16:40 | Calderbrook phones the Payroll lead asking where the file is |
| 16:52 | Beacon ticket raised in category ATLAS-PAYX; SEV2 declared; Aoife Gallagher takes Incident Commander role |
| 17:05 | Calderbrook confirms the host key rotation |
| 17:20 | New fingerprint verified by phone with Calderbrook's service desk using a pre-agreed callback number |
| 17:31 | Known hosts entry updated on atlas-batch-prd-01 under emergency change CHG-EMERG approval |
| 17:33 | atlasctl job release PAYX-MONTHLY |
| 17:38 | File delivered successfully |
| 17:55 | Calderbrook acknowledgement file received |
| 18:10 | Incident resolved |

## Root causes

1. The bureau's host key rotation was communicated only to a mailbox that no longer existed. Meridian had not updated the supplier's contact details when atlas-ops@ was replaced by atlas-oncall@ in November 2024.
2. The PAYX failure alert was still routed to the decommissioned list, so the failure was invisible for more than two and a half hours.
3. The month-end runbook had no pre-flight check of the SFTP host key before releasing PAYX-MONTHLY.

## What went well

- The callback verification process with Calderbrook worked; the new fingerprint was verified out-of-band before it was trusted.
- ECAB approved the emergency change in under 10 minutes despite the change freeze.

## Action items

| ID | Action | Owner | Due | Status |
|---|---|---|---|---|
| AI-1 | Route PAYX failure alerts to atlas-oncall@meridian-instruments.com and page the IT Operations on-call primary | Aoife Gallagher | 2025-01-10 | Done |
| AI-2 | Audit alert destinations of all Atlas batch jobs on atlas-batch-prd-01 | Aoife Gallagher | 2025-02-28 | Done |
| AI-3 | Add a step to the Atlas Month-End Close Support Runbook to verify the bureau SFTP host key fingerprint against the vault entry payx/calderbrook/hostkey before releasing PAYX-MONTHLY (now step 22) | Aoife Gallagher | 2025-02-17 | Done |
| AI-4 | Update supplier contact records so Calderbrook sends host key and certificate notices to atlas-oncall@, with 10 business days' notice | Finance Systems and Procurement | 2025-03-31 | Done |
| AI-5 | Create a Beacon ticket in category ATLAS-PAYX automatically when PAYX-MONTHLY fails | IT Service Desk | 2025-03-31 | Done |

## Lessons

Silent alert routing failures are more dangerous than the underlying fault. All distribution list decommissions must now include a search of Atlas batch job configurations and supplier contact records before the list is removed.
