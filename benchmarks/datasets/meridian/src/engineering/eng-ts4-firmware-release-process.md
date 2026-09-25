# TS-4 and TS-4e Firmware Release Process

Document ID: eng-ts4-firmware-release-process. Owner: Tessera Firmware Team (release manager role rotates per release). Last updated: 9 June 2025.

## Scope

This process covers firmware for the Tessera TS-4 gateway and the TS-4e edge node, from release candidate to general availability through TesseraCloud over-the-air (OTA) updates.

## Release types

| Type | Version change | Example | Typical cadence |
|---|---|---|---|
| Major | First number | 4.x to 5.0 | Yearly |
| Minor | Second number | 5.2 to 5.3 | Quarterly |
| Patch | Third number | 5.2.0 to 5.2.1 | As needed |
| Hotfix | Patch released outside the normal cadence | 5.2.2 for a security fix | As needed |

## Stages

1. Release candidate built in Forge by a release pipeline on the release-tagged runners, including the hil-test stage on the tessera-tagged Penang runners and the sign stage.
2. The release manager declares a firmware release freeze on the release branch. During the freeze, Forge runner token rotation is also paused on the Penang runners.
3. Staging soak: the candidate runs on the 40 test TS-4 gateways and 25 TS-4e edge nodes connected to TesseraCloud staging for at least 7 days (major and minor) or 48 hours (patch).
4. Pilot ring: OTA to 2% of production devices that belong to customers enrolled in the early-access programme, for 7 days.
5. Staged rollout: 10%, then 25%, then 50%, then 100% of the fleet, with at least 48 hours between steps.
6. General availability announcement to customers.

## Rollout halt criteria

The rollout is halted automatically if, in any ring:

- more than 0.5% of updated devices fail to reconnect within 30 minutes, or
- more than 1% of updated devices report a watchdog reset within 24 hours, or
- any SEV2 or higher incident is linked to the release.

Halted rollouts can only be resumed by the release manager with approval from the Head of Platform Engineering.

## Reconnect behaviour requirement

Since gateway firmware 5.0.0 (September 2024) the TS-4 gateway uses jittered reconnect backoff and paced buffer replay, an action item from INC-2024-031. Any release that changes reconnect or replay behaviour must pass a reconnect-storm test in staging (all 40 test gateways reconnecting at once) and requires approval from the Head of Platform Engineering.

## Minimum supported versions

| Device | Minimum supported firmware | End of support for older versions |
|---|---|---|
| TS-4 gateway | 5.2.0 | TesseraCloud requires 5.2.0 or later from 1 October 2025; older gateways show as Degraded |
| TS-4e edge node | 2.6.1 | Node 2.6.1 is deprecated on gateway 5.3.0; see the product firmware compatibility matrix |

## Records

The release manager records every release in the release register with the version, Forge pipeline ID, signing timestamp, soak results and the rollout ring dates.
