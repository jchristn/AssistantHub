# Forge Pipeline Standards

Document ID: eng-forge-pipeline-standards. Owner: Platform Engineering. Last updated: 16 April 2025.

## Purpose

These standards describe the minimum stages and settings every Forge pipeline must use. Forge is Meridian's CI/CD platform built on GitLab runners.

## Required stages

Every pipeline that produces a deployable artefact must include these stages in order:

1. build
2. unit-test
3. security-gate (dependency and container image scanning; fails on unremediated Critical findings, per the Vulnerability Management Standard)
4. package
5. deploy (only for services with a Forge environment)

Firmware pipelines for Halcyon, Tessera and Lumen add a hil-test stage on the Penang runners and a sign stage, which is the only stage allowed to use the firmware signing keys held in the hardware security module.

## Runner tags

| Tag | Runners | Use |
|---|---|---|
| lds | forge-runner-lds-01 to forge-runner-lds-08 | General Linux builds |
| large | forge-runner-lds-05, forge-runner-lds-06 | Memory-heavy builds |
| release | forge-runner-lds-07, forge-runner-lds-08 | Release and signing pipelines |
| halcyon | forge-runner-pen-01, forge-runner-pen-02 | Halcyon HIL tests |
| tessera | forge-runner-pen-03, forge-runner-pen-04 | Tessera HIL tests |
| win | forge-runner-win-01, forge-runner-win-02 | Lumen configuration tool |

## Settings

- Job timeout: 60 minutes by default; HIL jobs may set up to 180 minutes.
- Artefacts expire after 30 days unless marked as a release; release artefacts are kept for 5 years.
- Build cache uses the bucket forge-cache-lds; objects expire after 14 days.
- Secrets are injected from Vault; secrets in pipeline variables are prohibited.
- Pipelines on main must be green before merge; direct pushes to main are blocked.

## Environments

| Forge environment | Approval |
|---|---|
| tesseracloud-dev | None |
| tesseracloud-staging | Automatic on merge to main |
| tesseracloud-prod | Two maintainers, within a change window or zero-downtime |
| atlas-integrations-prod | IT Business Applications lead; not during the Atlas month-end close window |

## Getting help

Raise Beacon category FORGE-RUNNER for runner or job execution problems, and FORGE-ACCESS for project permissions.
