# ADR-015: Forge Runners as a Fixed Fleet with Per-Host Concurrency Limits

Document ID: eng-adr-015-forge-runner-fleet. Status: Accepted. Date: 30 May 2023. Deciders: Kofi Mensah-Bonsu (Head of Platform Engineering), Forge maintainers, Penang Manufacturing Engineering consulted.

## Context

Forge, Meridian's CI/CD platform built on GitLab runners, runs three kinds of jobs: Linux container builds for TesseraCloud and internal tools, firmware builds and hardware-in-the-loop (HIL) tests for Halcyon and Tessera boards in Penang, and Windows builds for the Lumen configuration tool. We considered moving the Linux runners to cloud autoscaling.

## Options considered

- Cloud autoscaling runners that start per job.
- A fixed on-premises fleet in Leeds and Penang with per-host concurrency limits.
- A hybrid: fixed fleet with cloud burst.

## Decision

Run Forge runners as a fixed fleet with per-host concurrency limits, set by FORGE_RUNNER_CONCURRENCY:

| Runner group | Hosts | Executor | Concurrency |
|---|---|---|---|
| Leeds Linux | forge-runner-lds-01 to forge-runner-lds-08 | Docker | 6 |
| Penang HIL | forge-runner-pen-01 to forge-runner-pen-04 | Shell | 1 |
| Windows | forge-runner-win-01, forge-runner-win-02 | Shell | 2 |

Cloud burst was rejected for now.

## Rationale

- Firmware signing and HIL rigs must stay on premises; autoscaling would only help the Linux group.
- Build caches (bucket forge-cache-lds) stay close to the Leeds runners, which cut average TesseraCloud pipeline time by 35% in testing.
- Fixed hosts are simpler to certify for firmware supply-chain audits.

## Consequences

- Capacity must be planned; the FORGE_RUNNER_POOL_LOW alert fires when fewer than 5 Leeds Linux runners are online.
- Runner tokens are per host and must be rotated; see the Forge Runner Token Rotation Runbook. (INC-2024-052 later showed the risk of all tokens being registered on the same day.)
- HIL runners with concurrency 1 are the main bottleneck for firmware pipelines; a fifth Penang HIL runner is planned for FY27.

## Review trigger

Revisit if the average queue wait for Leeds Linux jobs exceeds 10 minutes for a full month.
