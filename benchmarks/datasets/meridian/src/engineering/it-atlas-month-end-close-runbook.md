# Atlas Month-End Close Support Runbook

Document ID: it-atlas-month-end-close-runbook

Owner: IT Business Applications (Atlas team lead Aoife Gallagher)

Last reviewed: 2025-05-12

Applies to: Atlas ERP production

## 1. Purpose and scope

This runbook describes how IT Business Applications supports Group Finance during the monthly close of the Atlas ERP system. It covers the technical preparation before the close window opens, the controlled release of the month-end batch jobs, the payroll export to our external payroll bureau, the period lock, and the tidy-up activities once Finance has signed off. It is written for the Atlas support engineer on duty for the close and for the Beacon service desk analysts who triage Finance tickets during the window.

The runbook applies to the Atlas production environment only. The Atlas test and pre-production environments do not run the month-end batch chain on a schedule; if Finance asks for a rehearsal close in pre-production, raise a separate Beacon request and follow the pre-production notes held by the Atlas team.

Month-end close is the single most business-critical recurring event for Atlas. The period close drives statutory reporting for the four legal entities, intercompany settlement, and the monthly payroll for UK staff. Year-end close (March, because the Meridian fiscal year starts on 1 April) follows the same procedure with the additional year-end steps owned by Group Finance; those steps are outside the scope of this document.

## 2. Overview of the close window

The close window runs from BD-2 through BD+3, where BD means business day relative to the last calendar day of the month. BD-1 is the last business day of the month and BD+1 is the first business day of the new month. Business days follow the UK (Leeds) calendar, because Group Finance is based in Leeds; local holidays in Rotterdam, Austin or Penang do not move the window.

A change freeze applies for the whole close window in accordance with the Change Management Procedure v2 (it-change-management-procedure-v2). During the freeze no normal or standard changes may be applied to Atlas, its databases, the batch scheduler, the integration middleware, or the network paths Atlas depends on. Only emergency changes approved through the Emergency Change Advisory Board (ECAB) are allowed. The day before the freeze (BD-3) is a preparation day and changes already approved by CAB may still complete on that day, but must be finished before 18:00 UK time.

The month-end batch chain runs on the MEC_BATCH queue of the Atlas batch scheduler. The jobs run in the following fixed order, and each job must complete successfully and be reviewed before the next is released:

- ACCRUE: posts standard and recurring accruals for all entities.
- FXLOAD: loads European Central Bank reference exchange rates for period-end revaluation.
- ICRECON: reconciles intercompany balances across the four legal entities.
- GLRECON: runs the general ledger reconciliation and subledger tie-out.
- PAYX-MONTHLY: exports the monthly payroll file to Calderbrook Payroll.
- CLOSE-LOCK: locks the accounting period.

The four legal entities in scope for ICRECON are Meridian Instruments Ltd (UK), Meridian Instruments B.V. (NL), Meridian Instruments Inc. (US) and Meridian Instruments Sdn. Bhd. (MY).

## 3. System landscape

The following hosts are involved in month-end close. All are in the Leeds data centre unless stated otherwise.

| Host | Role | Notes for close |
|---|---|---|
| atlas-app-prd-01.meridian.internal | Application tier node 1 | Load balanced with nodes 2 to 4 |
| atlas-app-prd-02.meridian.internal | Application tier node 2 | Load balanced |
| atlas-app-prd-03.meridian.internal | Application tier node 3 | Load balanced |
| atlas-app-prd-04.meridian.internal | Application tier node 4 | Load balanced; hosts the Finance power-user pool |
| atlas-db-prd-01.meridian.internal | Primary database | All batch writes; archive logs on /u02/arch |
| atlas-db-prd-02.meridian.internal | Standby replica | Used for reporting offload during close |
| atlas-batch-prd-01.meridian.internal | Batch scheduler | Hosts the MEC_BATCH queue |
| atlas-rpt-prd-01.meridian.internal | Reporting server | Close report archive share |

The standby replica atlas-db-prd-02.meridian.internal receives redo from the primary continuously. During the close window, Finance reporting queries are redirected to the standby so that heavy ad-hoc reporting does not compete with the batch chain on the primary. The redirection is a reporting data-source switch in the Atlas reporting configuration and does not require a change record, because it is a documented operational step of this runbook.

## 4. Roles and responsibilities

| Role | Who | Responsibility |
|---|---|---|
| Close coordinator | Financial Controller, Group Finance | Owns the close calendar, approves job releases, signs off the period |
| Atlas support engineer | IT Business Applications on-duty engineer | Executes this runbook, monitors batch jobs and infrastructure |
| Atlas team lead | Aoife Gallagher | Escalation point for technical decisions, approves deviations from this runbook |
| Database administrator | IT Infrastructure DBA on rota | Database health, archive logs, standby lag, backup verification |
| Payroll lead | Group Payroll, HR | Confirms payroll data is final before PAYX-MONTHLY |
| Group Treasury | Treasury analyst on duty | Signs off unusual exchange-rate movements before FXLOAD is released |
| Service desk | Beacon first-line analysts | Triages ATLAS-FIN and ATLAS-PAYX tickets, routes to the Atlas support engineer |
| ECAB chair | Head of IT Operations or delegate | Approves emergency changes during the freeze |

## 5. Close calendar

| Day | Time (UK) | Activity | Owner |
|---|---|---|---|
| BD-3 | Before 18:00 | Last approved changes complete; pre-close checks | Atlas support engineer |
| BD-3 | 22:00 | Pre-close full backup, tagged MEC-YYYYMM | DBA |
| BD-2 | 09:15 | Close window opens; first MEC Bridge stand-up | Close coordinator |
| BD-2 | 10:00 | Reporting redirected to standby; ACCRUE released | Atlas support engineer |
| BD-1 | 09:15 | Stand-up | Close coordinator |
| BD-1 | 15:30 (16:30 CET) | FXLOAD rate load and variance check | Atlas support engineer, Group Treasury |
| BD-1 | After FXLOAD | ICRECON released and reviewed | Atlas support engineer, entity accountants |
| BD+1 | 09:15 | Stand-up | Close coordinator |
| BD+1 | 10:00 | GLRECON released | Atlas support engineer |
| BD+1 | 14:00 | PAYX-MONTHLY host key check and release | Atlas support engineer, Payroll lead |
| BD+2 | 09:15 | Stand-up; final adjustments | Close coordinator |
| BD+3 | 09:15 | Stand-up; sign-off | Financial Controller |
| BD+3 | After sign-off | CLOSE-LOCK; reports archived; window closes | Atlas support engineer |

A daily stand-up is held at 09:15 UK time on every day of the window in the Teams channel "MEC Bridge". The MEC Bridge channel is also the single place where job releases are announced and approved. Approvals given verbally or in private chats are not valid for job release and must be restated in the channel.

## 6. Pre-close checks

The following checks are completed on BD-3 and recorded in the close checklist attached to the monthly Beacon close ticket (category ATLAS-FIN, title "Month-end close YYYY-MM"):

- All CAB-approved changes affecting Atlas have completed and been closed in Beacon, or have been deferred until after BD+3.
- No open SEV1 or SEV2 incidents affect Atlas or its integrations.
- The batch scheduler on atlas-batch-prd-01 is healthy and the MEC_BATCH queue has no jobs left over from the previous month.
- The Calderbrook Payroll SFTP credentials in the Atlas credential vault have not expired.
- The previous month's close reports are present in the archive share on atlas-rpt-prd-01.
- The on-duty Atlas support engineer, DBA and service desk lead for each day of the window are named in the close ticket.

## 7. Procedure

Steps are numbered continuously across all phases. Do not skip a step. If a step cannot be completed, stop, record the reason in the close ticket and raise it in the MEC Bridge before continuing.

### Phase A: Preparation (BD-3)

1. Confirm that the change freeze notice for the close window has been published to the IT change calendar and the Atlas user community. The notice must state that only ECAB-approved emergency changes are permitted between BD-2 and BD+3.
2. Check the health of the application tier. Confirm that atlas-app-prd-01 to atlas-app-prd-04 are all in service on the load balancer and that no node is showing sustained CPU above 70% or heap usage above 85% in the monitoring dashboard.
3. Check replication from atlas-db-prd-01 to the standby atlas-db-prd-02.meridian.internal. Standby apply lag must be under 5 minutes. If lag is higher, involve the DBA before continuing.
4. Check the archive log filesystem /u02/arch on atlas-db-prd-01. The warning threshold is 80% used and the action threshold is 85% used. If usage is at or above 80% on BD-3, ask the DBA to schedule an archive log purge before the backup, following the rule in step 18.
5. At 22:00 UK time on BD-3 the DBA starts the pre-close full backup of the Atlas production database. The backup is tagged MEC-YYYYMM (for example MEC-202505 for the May 2025 close) and is retained for 13 months. This retention overrides the standard 30-day daily retention in the Backup and Retention Standard v2, so the DBA must apply the MEC retention class explicitly when the job is submitted.
6. Before the window opens, confirm with the DBA that the MEC-YYYYMM backup completed successfully and that a catalogue entry exists with the 13-month retention class. Record the backup identifier in the close ticket.

### Phase B: Window opens (BD-2)

7. Join the 09:15 stand-up in the MEC Bridge Teams channel. Confirm the close calendar with the Financial Controller and post the pre-close check results.
8. Redirect Finance reporting to the standby replica. In the Atlas reporting configuration, switch the "Finance reporting" data source to atlas-db-prd-02.meridian.internal and confirm that a sample report runs. Post confirmation in the MEC Bridge.
9. Ask the service desk lead to raise the default priority of the Beacon category ATLAS-FIN to P2 for the duration of the window. ATLAS-PAYX tickets keep their normal P2 default. Confirm that the Beacon routing rule sends both categories to the Atlas support queue.
10. Check the batch queue with atlasctl batch status --queue MEC_BATCH. All six month-end jobs should show as scheduled and held. If any job is not held, hold it immediately (for example atlasctl job hold ACCRUE) and investigate why the hold was not applied by the scheduler template.
11. When the Financial Controller approves in the MEC Bridge, release ACCRUE with atlasctl job release ACCRUE.
12. Monitor ACCRUE to completion. Typical run time is 35 to 50 minutes. On completion, send the accrual summary report to the Financial Controller and wait for confirmation that accrual totals are as expected for each entity.

### Phase C: Exchange rates and intercompany (BD-1)

13. Join the 09:15 stand-up. Confirm that FXLOAD is still held with atlasctl batch status --queue MEC_BATCH and that the ECB rate feed connector reported a successful connection test in the previous 24 hours.
14. FXLOAD loads the ECB reference rates at 16:30 CET on BD-1 (15:30 UK time). Once the rates are staged, run the variance check and compare every currency against the previous business day's rate. If any currency has moved by more than 3% versus the previous business day, keep FXLOAD on hold with atlasctl job hold FXLOAD and do not post the rates until Group Treasury has signed off the movement in the MEC Bridge. Record the currency, both rates and the Treasury approver in the close ticket.
15. When the variance check passes, or Group Treasury has signed off, release FXLOAD with atlasctl job release FXLOAD and confirm that period-end revaluation postings were created for all four entities.
16. Release ICRECON. The job reconciles intercompany balances between Meridian Instruments Ltd (UK), Meridian Instruments B.V. (NL), Meridian Instruments Inc. (US) and Meridian Instruments Sdn. Bhd. (MY).
17. Distribute the ICRECON exception report to the entity accountants. Mismatches below the Finance tolerance are cleared by Finance; mismatches caused by interface failures (for example missing shipments from the Penang factory stock transfers) are logged as ATLAS-FIN tickets and investigated by the Atlas support engineer.
18. Check /u02/arch on atlas-db-prd-01 at the end of BD-1, because ACCRUE, FXLOAD and ICRECON generate heavy redo. If usage has reached 85%, the DBA runs the archive log purge job ARCH-PURGE, but only after confirming that standby apply lag on atlas-db-prd-02.meridian.internal is under 5 minutes. Purging archive logs that the standby has not yet applied will break the standby and remove the reporting offload for the rest of the close.

### Phase D: Ledger reconciliation and payroll (BD+1)

19. Join the 09:15 stand-up. Confirm that all ICRECON exceptions are either cleared or accepted by the Financial Controller.
20. Release GLRECON with atlasctl job release GLRECON. Typical run time is 60 to 90 minutes.
21. Send the GLRECON tie-out report to the Financial Controller. Any subledger that does not tie out is recorded in the close ticket and must be resolved or explicitly accepted before CLOSE-LOCK.
22. Before releasing PAYX-MONTHLY, verify the Calderbrook Payroll SFTP host key fingerprint. Retrieve the current fingerprint presented by sftp.calderbrook-payroll.co.uk on port 2222 and compare it with the value recorded in the Atlas credential vault entry payx/calderbrook/hostkey. If they differ, do not release the job; contact Calderbrook Payroll through the bureau contact in the vault entry to confirm a legitimate key rotation, update the vault entry only after confirmation, and record the change in the ATLAS-PAYX ticket. This step was added as action item AI-3 of postmortem INC-2025-007 (Atlas payroll export failure, January 2025).
23. Confirm with the Payroll lead in the MEC Bridge that payroll data for the month is final and that no further Keel changes affecting pay will be submitted for this cycle.
24. Test connectivity from atlas-batch-prd-01 to sftp.calderbrook-payroll.co.uk on port 2222 with atlasctl net check --host sftp.calderbrook-payroll.co.uk --port 2222. A failed check usually means a firewall or proxy change and must be escalated to Network Operations immediately, because the payroll bureau cut-off is 17:00 UK time on BD+1.
25. Release PAYX-MONTHLY with atlasctl job release PAYX-MONTHLY and monitor the transfer. Failure alerts for PAYX-MONTHLY are sent to the distribution list atlas-oncall@meridian-instruments.com. The old list atlas-ops@meridian-instruments.com was decommissioned and must not be used in any alert configuration.
26. Confirm receipt with Calderbrook Payroll. The bureau returns an acknowledgement file within 30 minutes of a successful upload. Post the acknowledgement reference in the MEC Bridge and attach it to the ATLAS-PAYX ticket.

### Phase E: Adjustments, sign-off and lock (BD+2 to BD+3)

27. On BD+2, support Finance with late manual journals and reruns of individual reconciliation reports. Reruns of whole batch jobs require the Financial Controller's approval in the MEC Bridge.
28. On BD+3, obtain the Financial Controller's sign-off of the period in the MEC Bridge and record it in the close ticket.
29. Release CLOSE-LOCK with atlasctl job release CLOSE-LOCK. The job sets the configuration key atlas.period.lock=true for the closed period in all four entities.
30. Verify the lock with atlasctl config get atlas.period.lock --period YYYY-MM and confirm that a test posting into the closed period is rejected.
31. Generate the close report pack and archive it to the close archive share on atlas-rpt-prd-01. Close reports are retained for 7 years.
32. Switch the "Finance reporting" data source back from atlas-db-prd-02.meridian.internal to the standard reporting configuration and confirm that a sample report runs.
33. Ask the service desk lead to return the Beacon category ATLAS-FIN to its normal default priority.
34. Post the close summary in the MEC Bridge, close the stand-up series, and confirm to the change manager that the freeze for Atlas can be lifted at the end of BD+3.

## 8. Reopening a locked period

A period locked by CLOSE-LOCK may only be reopened with the approval of the Financial Controller, recorded in Beacon as an ATLAS-FIN request with the approval attached. The Atlas support engineer then sets atlas.period.lock=false for the named entity and period only, supports the correction, and sets the lock back to true on the same day. Reopening a period is not a change under the Change Management Procedure, but every reopen is reported in the monthly Atlas service review.

## 9. Incident handling during the close

Any issue that blocks the close calendar is declared a SEV2 incident according to the incident severity definitions (it-incident-severity-definitions). Examples include a failed batch job that cannot be rerun within two hours, loss of the standby replica, PAYX-MONTHLY failing after 14:00 on BD+1, or the ECB rate feed being unavailable on BD-1. Issues that affect a single user or a single report are handled as normal ATLAS-FIN tickets.

When a SEV2 is declared, the Atlas support engineer raises the incident in Beacon, informs the Financial Controller in the MEC Bridge, and pages the Atlas on-call through the standard paging route. Fixes that require a change to production during the freeze must go through ECAB. The ECAB chair can be reached through the IT Operations on-call.

## 10. Troubleshooting

| Symptom | Likely cause | Action |
|---|---|---|
| Job stuck in RUNNING with no progress for 30 minutes | Database lock contention with ad-hoc reporting | Confirm reporting is redirected to the standby (step 8); ask DBA to identify blocking sessions |
| ACCRUE fails with period validation error | Period not opened in one entity | Ask Finance to open the period for the entity, then rerun ACCRUE |
| FXLOAD stages no rates | ECB feed connector down or proxy change | Check connector log on atlas-batch-prd-01; fall back to manual rate file supplied by Group Treasury |
| FXLOAD variance check flags a currency | Genuine market movement above 3% | Hold FXLOAD; obtain Group Treasury sign-off (step 14) |
| ICRECON shows large MY to UK mismatch | Penang stock transfer interface backlog | Check interface queue; reprocess failed messages; rerun ICRECON |
| /u02/arch above 85% | Heavy redo from batch jobs | DBA runs ARCH-PURGE after confirming standby lag under 5 minutes (step 18) |
| Standby apply lag above 5 minutes | Network or redo transport issue | Do not purge archive logs; DBA investigates; consider moving reporting back to primary |
| PAYX-MONTHLY fails with host key verification error | Bureau rotated SFTP host key | Follow step 22; confirm rotation with Calderbrook before updating vault |
| PAYX-MONTHLY fails with connection timeout | Firewall or proxy change on port 2222 | Escalate to Network Operations; run atlasctl net check (step 24) |
| No PAYX failure alert received although job failed | Alert routed to an old distribution list | Check alert configuration uses atlas-oncall@meridian-instruments.com |
| Test posting accepted after CLOSE-LOCK | Lock not applied to an entity | Rerun CLOSE-LOCK for the entity; verify atlas.period.lock=true |

## 11. Escalation contacts

| Level | Contact | When |
|---|---|---|
| 1 | Atlas support engineer on duty | All close issues |
| 2 | Aoife Gallagher, Atlas team lead | Deviation from runbook, job failure not resolved within 1 hour |
| 3 | Head of IT Operations | SEV2 declared, ECAB needed |
| 3 | Financial Controller | Any change to the close calendar |
| 4 | CTO, Lars Hedegaard | Close calendar at risk of missing statutory deadlines |
| Security | Mei-Ling Tan, Head of IT Security | Suspected credential compromise, unexpected host key change not confirmed by the bureau |

An unexpected change in the Calderbrook host key that the bureau does not confirm is treated as a potential security incident and reported to IT Security through the Beacon category SEC-INCIDENT before any further payroll transfer is attempted.

## 12. Post-close tasks

After the window closes, the Atlas support engineer completes the following within five business days:

- Close the monthly close ticket in Beacon with a summary of issues, reruns and deviations.
- Confirm with the DBA that the MEC-YYYYMM backup is still listed in the catalogue with its 13-month retention class.
- Review any ATLAS-FIN and ATLAS-PAYX tickets raised during the window and link recurring problems to a problem record.
- Update this runbook if any step was found to be wrong or incomplete, and record the change in the revision history.
- Provide close metrics (job run times, number of reruns, tickets raised) for the monthly Atlas service review.

## 13. Revision history

| Version | Date | Author | Change |
|---|---|---|---|
| 1.0 | 2023-06-05 | Aoife Gallagher | First version, replacing the Finance close checklist spreadsheet |
| 1.1 | 2024-04-22 | Aoife Gallagher | Added reporting offload to standby atlas-db-prd-02 |
| 1.2 | 2025-02-17 | Aoife Gallagher | Added step 22 host key verification (INC-2025-007 AI-3); alert list changed to atlas-oncall@ |
| 1.3 | 2025-05-12 | Aoife Gallagher | Aligned freeze wording with Change Management Procedure v2; pre-close backup retention set to 13 months |
