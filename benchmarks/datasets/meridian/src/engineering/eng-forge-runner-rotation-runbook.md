# Forge Runner Token Rotation Runbook

Document ID: eng-forge-runner-rotation-runbook

Owner: Platform Engineering (Head: Kofi Mensah-Bonsu)

Last reviewed: 2025-02-10

Classification: Internal

## 1. Purpose

This runbook describes how Platform Engineering rotates the authentication tokens used by Forge runners. Forge is Meridian Instruments' internal CI/CD service, built on GitLab runners, and every build, test and release pipeline for Halcyon, Tessera and Lumen software and firmware depends on it. A runner whose token has expired silently stops picking up jobs, and pipelines queue until they time out. Rotating tokens on a predictable schedule, in small waves, keeps the fleet healthy and ensures that no single event can take the whole fleet offline.

The runbook is written for the Platform Engineering engineer on rotation duty. It assumes working knowledge of Linux administration, Vault and the forgectl command line tool. Anyone who has not performed a rotation before must shadow one complete wave before running one on their own.

## 2. Scope

This runbook covers all registered Forge runners in the production Forge instance: the Leeds Linux runners, the Penang hardware-in-the-loop runners and the Windows runners. It does not cover developer-owned project runners, which are not permitted on the production instance, or the Forge server itself, whose certificates are managed separately under the platform certificate process.

## 3. Background: INC-2024-052

In August 2024 every Forge runner stopped accepting jobs within a single forty-minute period. The postmortem, INC-2024-052, found that all runner tokens had been registered on the same day during the runner fleet rebuild, and that the token lifetime was set to 720h (30 days) through FORGE_RUNNER_TOKEN_TTL. When the tokens reached their expiry together, the entire fleet dropped out of the pool. Builds were blocked for most of a working day, including a Halcyon HX-210 firmware hotfix that had been promised to a customer.

The postmortem produced several action items that shape this runbook:

- The token lifetime was doubled. FORGE_RUNNER_TOKEN_TTL is now 1440h (60 days).
- Rotation is performed in waves so that token expiry dates are staggered across the fleet and can never again coincide.
- A monitoring alert, FORGE_RUNNER_TOKEN_EXPIRY_SOON, was added as action item AI-1. It fires when any runner token has 10 days or less remaining and pages the Platform on-call engineer as a SEV3.
- Rotation was formalised as a standard change so that it is visible to the Change Advisory Board and does not collide with other scheduled work.

The full timeline and root cause analysis are in the postmortem document for INC-2024-052.

## 4. Runner fleet

All runner hostnames are under the .meridian.internal domain, for example forge-runner-lds-01.meridian.internal.

| Host | Site | Executor | Tags | Concurrency |
|---|---|---|---|---|
| forge-runner-lds-01 | Leeds | Linux, docker | lds, linux, docker | 6 |
| forge-runner-lds-02 | Leeds | Linux, docker | lds, linux, docker | 6 |
| forge-runner-lds-03 | Leeds | Linux, docker | lds, linux, docker | 6 |
| forge-runner-lds-04 | Leeds | Linux, docker | lds, linux, docker | 6 |
| forge-runner-lds-05 | Leeds | Linux, docker | lds, linux, docker, large | 6 |
| forge-runner-lds-06 | Leeds | Linux, docker | lds, linux, docker, large | 6 |
| forge-runner-lds-07 | Leeds | Linux, docker | lds, linux, docker, release | 6 |
| forge-runner-lds-08 | Leeds | Linux, docker | lds, linux, docker, release | 6 |
| forge-runner-pen-01 | Penang | Linux, shell | pen, hil, halcyon | 1 |
| forge-runner-pen-02 | Penang | Linux, shell | pen, hil, halcyon | 1 |
| forge-runner-pen-03 | Penang | Linux, shell | pen, hil, tessera | 1 |
| forge-runner-pen-04 | Penang | Linux, shell | pen, hil, tessera | 1 |
| forge-runner-win-01 | Leeds | Windows, shell | win, lumen | 2 |
| forge-runner-win-02 | Leeds | Windows, shell | win, lumen | 2 |

The Leeds Linux runners use the default FORGE_RUNNER_CONCURRENCY of 6. The Penang runners drive hardware-in-the-loop (HIL) test rigs for Halcyon and Tessera boards and are deliberately limited to one concurrent job, because each runner owns exactly one physical rig. The Windows runners build the Lumen configuration tool and run at a concurrency of 2.

Runner configuration lives in /etc/forge-runner/config.toml on each Linux host and in C:\ProgramData\forge-runner\config.toml on the Windows hosts. Runner tokens are never stored in configuration management; they are held in Vault at secret/forge/runners/<hostname>, and the runner service reads the token from Vault on start.

## 5. Rotation schedule

Tokens are rotated every 45 days. Because the token lifetime is 60 days, this leaves a 15-day safety margin in which a missed or delayed wave can be recovered before any token expires.

Each rotation cycle is split into waves. A wave may contain at most 25% of the fleet, which with 14 runners means no more than 3 hosts, and never more than 2 Leeds runners at the same time. The Leeds limit exists because the Leeds Linux runners carry most of the everyday pipeline load; taking out three of them at once noticeably increases queue times for engineers.

| Wave | Hosts | Week of cycle |
|---|---|---|
| 1 | forge-runner-lds-01, forge-runner-lds-02, forge-runner-pen-01 | Week 1 |
| 2 | forge-runner-lds-03, forge-runner-lds-04, forge-runner-win-01 | Week 2 |
| 3 | forge-runner-lds-05, forge-runner-lds-06, forge-runner-pen-02 | Week 3 |
| 4 | forge-runner-lds-07, forge-runner-lds-08, forge-runner-pen-03 | Week 4 |
| 5 | forge-runner-pen-04, forge-runner-win-02 | Week 5 |

The rotation engineer should aim to run each wave on a Tuesday or Wednesday morning, Leeds time, when build demand is predictable and the Penang team is still on shift for the afternoon overlap. Waves 1 to 5 take roughly five weeks, which leaves around two weeks of slack before the next cycle starts.

### 5.1 Blackout periods

Rotation must not be carried out during the following periods:

- The Atlas month-end close window, from BD-2 to BD+3 (two business days before month end through the third business day of the new month). Finance integration pipelines that deploy Atlas extensions run on Forge during the close, and the close must not be put at risk.
- Any firmware release freeze declared by the TS-4 release manager. During a freeze the Penang HIL runners are reserved for release candidate testing and the Leeds release runners (forge-runner-lds-07 and forge-runner-lds-08) are reserved for signing and packaging.
- Any period during which a SEV1 or SEV2 incident involving Forge is open.

If a blackout overlaps a planned wave, move the wave to the next available working day. Do not compress two waves into one to catch up; the 25% limit still applies.

## 6. Prerequisites

Before starting a wave, confirm the following:

- You have a standard change raised in Beacon under category CHG-STD, referencing standard change catalogue item SC-017 (Forge runner token rotation). The change record lists the hosts in the wave and the planned start time.
- You hold the Vault policy forge-runner-admin, which allows writing to secret/forge/runners/<hostname>.
- You have forgectl version 2.6 or later installed and are authenticated to the production Forge instance (forgectl auth status shows the production context).
- You have SSH access to the Linux runners through the Leeds bastion and RDP access to the Windows runners through the privileged access workstation.
- For Penang hosts, you have notified the Penang test engineering lead on the Forge channel at least one working day in advance, so that long-running HIL campaigns can be planned around the rotation.
- No blackout period applies (see section 5.1).
- The FORGE_RUNNER_TOKEN_EXPIRY_SOON alert is healthy in monitoring, so that you will know if your rotation failed silently.

## 7. Procedure

Steps are numbered continuously across this section. Perform the procedure for one host at a time within a wave. Record the start and end time of each host in the Beacon change record.

### 7.1 Preparation

1. Open the Beacon change record for the wave and move it to Implementing.
2. Post a short notice in the Forge users channel: which runners are being rotated, the expected duration (normally under one hour per host) and a reminder that pipelines will continue on the remaining runners.
3. List the current state of the runners in the wave. For Leeds runners run forgectl runner list --tag lds; for Penang runners run forgectl runner list --tag pen; for Windows runners run forgectl runner list --tag win. Confirm that every host in the wave shows status online and that the rest of the fleet is healthy. If any runner outside the wave is already offline, stop and investigate before continuing, because taking more runners out may breach capacity.
4. For each host in the wave, run forgectl runner token-info --host <host> and record the current token expiry date in the change record. This gives you a baseline and confirms which token you are replacing.

### 7.2 Drain

5. Drain the first host. For example, forgectl runner drain --host forge-runner-lds-03 --timeout 45m. The drain stops new jobs being scheduled on the runner while allowing running jobs to finish. The drain timeout is 45 minutes; this was chosen to cover the longest normal Leeds pipeline stage, the TesseraCloud integration test suite.
6. For Penang HIL runners only, before draining, SSH to the host and check for the lock file /var/lib/forge/hil.lock. If the file exists, a HIL job is currently holding a physical rig. Never interrupt a job holding the HIL lock: a killed HIL job can leave a Halcyon or Tessera board in a partially flashed state that requires manual recovery by the Penang test team. Either wait for the job to finish and the lock to be released, or reschedule the host to later in the wave or to the next working day.
7. Watch the drain. forgectl runner list --tag <tag> shows the host as draining with a count of remaining jobs. When the count reaches zero the host shows drained.
8. If the drain timeout of 45 minutes expires with jobs still running on a Leeds or Windows runner, do not force the drain. Identify the job owners from the Forge UI, contact them, and agree whether to cancel. If the owner cannot be reached within 15 minutes, resume the runner with forgectl runner resume --host <host> and reschedule it.

### 7.3 Rotate

9. With the host drained, stop the runner service. On Linux hosts use systemctl stop forge-runner. On Windows hosts stop the Forge Runner service from the services console or with the PowerShell Stop-Service command.
10. Confirm that the host does not carry a legacy TTL override. On Linux, inspect /etc/forge-runner/env; on Windows, inspect C:\ProgramData\forge-runner\env. The file must either not set FORGE_RUNNER_TOKEN_TTL at all, in which case the fleet default of 1440h applies, or set it explicitly to FORGE_RUNNER_TOKEN_TTL=1440h. Any value of 720h is a leftover from before INC-2024-052 and must be removed.
11. Rotate the token with forgectl runner rotate-token --host <host>. This revokes the existing token on the Forge server, registers a new one with the lifetime set by FORGE_RUNNER_TOKEN_TTL, and writes the new token to Vault at secret/forge/runners/<hostname>. The command prints the new token ID but never the token value.
12. Confirm in Vault that the secret at secret/forge/runners/<hostname> has a new version with a creation timestamp matching the rotation. Do not copy or print the token value.
13. Start the runner service again (systemctl start forge-runner on Linux, or Start-Service on Windows). The runner reads the new token from Vault on start. Check the service log for the line confirming a successful registration check against the Forge server.
14. Verify the new token lifetime with forgectl runner token-info --host <host>. The new token expiry must be at least 59 days out from today. If token-info shows an expiry of around 30 days, the host still has the legacy FORGE_RUNNER_TOKEN_TTL override in /etc/forge-runner/env; correct the file as described in step 10, then repeat steps 11 to 14. Do not resume a host whose token expires in less than 59 days.

### 7.4 Verify and resume

15. Run forgectl runner verify --host <host>. The verify command checks that the runner is registered, that it can reach the Forge server and the cache bucket, that it reports the expected tags and concurrency from /etc/forge-runner/config.toml, and that it can pull the standard base image.
16. Check that the concurrency reported by verify matches the fleet table in section 4: 6 for Leeds Linux runners (the FORGE_RUNNER_CONCURRENCY default), 1 for Penang HIL runners and 2 for Windows runners. A mismatch usually means config.toml has been edited by hand and must be corrected through configuration management rather than on the host.
17. Resume the runner with forgectl runner resume --host <host>. The host returns to the pool and starts accepting jobs.
18. Watch the first job the runner picks up through to completion. For Leeds runners, any pipeline will do; for Penang runners, ask the Penang test team to trigger the short HIL smoke pipeline, which flashes and reads back a reference board; for Windows runners, trigger the Lumen configuration tool nightly build manually.
19. Record in the Beacon change record the old token expiry, the new token expiry, and the time the host was resumed.

### 7.5 Next host and close-out

20. Repeat steps 5 to 19 for the next host in the wave. Do not start draining the next host until the previous host has been resumed and has completed at least one job successfully. This keeps the number of runners out of the pool to one at a time within the wave.
21. When all hosts in the wave are complete, run forgectl runner list for each affected tag and confirm that every host shows online.
22. Run forgectl runner token-info against the whole fleet and update the rotation tracker sheet with the new expiry dates. The expiry dates across the fleet should now be spread over roughly five weeks. If more than 3 runners share an expiry date within 24 hours of each other, raise it with the Platform Engineering lead, because staggering has drifted and the next cycle should be rebalanced.
23. Confirm the FORGE_RUNNER_TOKEN_EXPIRY_SOON alert is not firing for any host in the wave. If it is still firing for a rotated host, the alert may be caching the old token; wait for the next scrape (5 minutes) before investigating.
24. Post a completion notice in the Forge users channel.
25. Move the Beacon change record to Completed and attach the token-info output.
26. If the wave was partly rescheduled because of a HIL lock or an unreachable job owner, create a follow-up task for the remaining hosts with a target date no later than 7 days later.
27. At the end of wave 5, check the date of the next cycle's wave 1 (45 days after this cycle's wave 1) against the Atlas month-end close calendar and the TS-4 release calendar, and move it if it falls in a blackout.
28. Update the revision history of this runbook if anything in the procedure was found to be wrong or unclear.

## 8. Verification checklist

At the end of a wave, all of the following must be true:

- Every host in the wave shows online in forgectl runner list.
- forgectl runner token-info shows an expiry at least 59 days out for every rotated host.
- forgectl runner verify passed for every rotated host.
- Each rotated host has completed at least one real job successfully after resume.
- No host in the fleet has FORGE_RUNNER_TOKEN_TTL set to anything other than 1440h.
- The FORGE_RUNNER_TOKEN_EXPIRY_SOON alert is clear for the rotated hosts.
- The Beacon CHG-STD record under SC-017 is closed with evidence attached.

## 9. Rollback

Token rotation revokes the previous token on the Forge server as part of forgectl runner rotate-token, so there is no way to go back to the old token. Rollback therefore means restoring a working runner, not restoring the old credential.

If a runner fails verification after rotation:

- Rotate the token again with forgectl runner rotate-token --host <host>. A second rotation fixes most problems caused by a Vault write that did not complete.
- If the second rotation also fails, leave the runner drained, raise a Beacon ticket under category FORGE-RUNNER with the verify output attached, and continue with the rest of the wave only if at least 6 Leeds Linux runners remain online.
- If a Penang HIL runner cannot be restored the same day, notify the Penang test engineering lead so that HIL jobs are routed to the remaining runner with the same product tag (halcyon or tessera).
- If a problem affects more than one host in the wave, stop the wave, resume any host that is still healthy, and escalate to the Platform Engineering lead. Do not continue to other waves until the cause is understood.

## 10. Caches and artefacts

Rotation does not affect the build cache or stored artefacts, but engineers sometimes ask about them when a runner comes back with a cold cache. For reference:

- The Leeds runners share the cache bucket forge-cache-lds. Objects in the cache bucket are retained for 14 days and then expire automatically. A rotated runner reconnects to the same bucket and does not lose cache.
- Job artefacts are retained for 30 days on the Forge server unless a pipeline marks them to keep. Release artefacts are copied to the release repository and are not subject to the 30-day limit.
- The Penang runners do not use the shared cache because of the latency to Leeds; they keep a local cache under /var/cache/forge, which is cleared at each rotation.

## 11. Troubleshooting

| Symptom | Likely cause | Action |
|---|---|---|
| token-info shows expiry of about 30 days after rotation | Legacy FORGE_RUNNER_TOKEN_TTL=720h override in /etc/forge-runner/env | Remove the override or set 1440h, then repeat steps 11 to 14 |
| Runner service starts but shows offline | Service could not read the token from Vault | Check the host's Vault AppRole is valid; restart the service; rotate again if needed |
| verify fails on cache access | Network path to forge-cache-lds blocked or credentials expired | Check the bucket credentials in config.toml; raise FORGE-RUNNER ticket if the bucket itself is unhealthy |
| verify reports wrong concurrency | config.toml edited on the host | Revert through configuration management; do not hand-edit |
| Drain does not complete within 45 minutes | Long-running job | Contact job owner; do not force; resume and reschedule if owner unreachable |
| hil.lock present on a Penang runner | HIL job in progress | Wait for the job to finish or reschedule; never interrupt |
| FORGE_RUNNER_TOKEN_EXPIRY_SOON still firing after rotation | Monitoring scrape not yet updated | Wait one scrape interval; if still firing, check token-info on the host |
| rotate-token returns permission denied | Missing Vault policy forge-runner-admin | Request the policy through Beacon; do not borrow another engineer's session |

## 12. Alerting and on-call

The FORGE_RUNNER_TOKEN_EXPIRY_SOON alert fires when any runner token has 10 days or less remaining. It pages the Platform on-call engineer as a SEV3 and should never fire during normal operation, because a 45-day rotation cadence against a 60-day lifetime leaves 15 days of margin. If it fires, the on-call engineer should treat it as a missed rotation: check the rotation tracker, identify which wave was skipped, and rotate the affected hosts using this runbook on the next working day, or immediately if fewer than 3 days remain.

A second alert, FORGE_RUNNER_POOL_LOW, fires when fewer than 5 Leeds Linux runners are online for more than 30 minutes. If it fires during a rotation wave, pause the wave and resume any drained runner that has already been verified.

## 13. Related documents

- Postmortem INC-2024-052: Forge runner token expiry
- ADR-015 (eng-adr-015-forge-runner-fleet): Forge runners as a fixed fleet with per-host concurrency limits
- Change Management Procedure (current version), including the standard change catalogue
- Atlas Month-End Close Support Runbook (for the close calendar)
- Incident Severity Definitions

## 14. Revision history

| Version | Date | Author | Change |
|---|---|---|---|
| 1.0 | 2023-06-12 | Platform Engineering | Initial runbook; single rotation of all runners every 30 days |
| 2.0 | 2024-09-02 | Platform Engineering | Rewritten after INC-2024-052: FORGE_RUNNER_TOKEN_TTL raised to 1440h, wave-based rotation introduced, FORGE_RUNNER_TOKEN_EXPIRY_SOON alert added |
| 2.1 | 2024-11-18 | Platform Engineering | Added HIL lock check for Penang runners and Windows runner steps |
| 2.2 | 2025-02-10 | Platform Engineering | Rotation logged as standard change SC-017; added blackout for TS-4 firmware release freeze; added legacy TTL override check at step 14 |
