# Postmortem: INC-2024-052 Forge Runner Token Expiry

Document ID: eng-postmortem-inc-2024-052-forge-token-expiry. Incident: INC-2024-052. Severity: SEV2. Date of incident: 5 August 2024. Postmortem published: 13 August 2024. Author: Piotr Zielinski (Platform Engineering). Review chaired by: Kofi Mensah-Bonsu (Head of Platform Engineering).

## Summary

At 07:40 UK time on Monday 5 August 2024, all 14 Forge runners stopped picking up jobs. Every runner authentication token had been registered on the same day, 6 July 2024, during the runner OS upgrade, with FORGE_RUNNER_TOKEN_TTL=720h (30 days). All tokens therefore expired within minutes of each other. No alert existed for approaching token expiry. Forge could not run any pipeline for 5 hours 10 minutes.

## Impact

- No CI/CD pipelines ran between 07:40 and 12:50 UK time.
- An HX-210 firmware hotfix due for release to Penang production that morning was delayed by one day.
- About 60 engineers in Leeds, Austin and Penang were blocked from merging.
- TesseraCloud production was not affected, but a planned tc-api deployment was postponed.

## Timeline (UK time)

| Time | Event |
|---|---|
| 6 July 2024 | All 14 runners re-registered after OS upgrade with 720h token TTL |
| 07:40 | Runner tokens begin to expire; jobs stay in pending state |
| 08:05 | First Beacon ticket raised by an engineer in category FORGE-RUNNER |
| 08:30 | Service desk links 11 duplicate tickets; SEV2 declared under INC-PLATFORM |
| 08:42 | Platform on-call identifies "token expired" errors in runner logs |
| 09:15 | Decision to re-register runners manually; Vault paths for tokens located |
| 10:20 | Leeds runners forge-runner-lds-01 to 04 back online |
| 11:45 | Remaining Leeds runners and Windows runners online |
| 12:50 | Penang HIL runners online after on-site check of rigs; incident resolved |

## Root causes

1. All runner tokens were created at the same time with the same TTL, so they expired together.
2. The 30-day TTL (720h) was shorter than the informal rotation habit, which relied on people remembering.
3. There was no monitoring of token expiry dates.
4. Two runners had a host-level override of FORGE_RUNNER_TOKEN_TTL in /etc/forge-runner/env that nobody knew about, which complicated recovery.

## Action items

| ID | Action | Owner | Due | Status |
|---|---|---|---|---|
| AI-1 | Add alert FORGE_RUNNER_TOKEN_EXPIRY_SOON, firing at 10 days remaining and paging Platform on-call as SEV3 | Ravi Chandrasekar | 2024-08-30 | Done |
| AI-2 | Increase FORGE_RUNNER_TOKEN_TTL from 720h to 1440h (60 days) | Piotr Zielinski | 2024-08-30 | Done |
| AI-3 | Rewrite the Forge Runner Token Rotation Runbook with staggered waves of at most 25% of the fleet, rotating every 45 days | Piotr Zielinski | 2024-09-06 | Done (runbook v2.0, 2 September 2024) |
| AI-4 | Register token rotation as a standard change in the catalogue (became SC-017) | Kofi Mensah-Bonsu | 2024-10-31 | Done |
| AI-5 | Remove host-level TTL overrides and add a runbook check that a rotated token expires at least 59 days out (runbook step 14) | Piotr Zielinski | 2025-02-10 | Done |

## Lessons

Anything with a fixed expiry that was created in bulk will fail in bulk. Stagger, monitor and automate.
