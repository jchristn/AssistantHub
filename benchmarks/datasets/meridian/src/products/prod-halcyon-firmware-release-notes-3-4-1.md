# Halcyon Firmware 3.4.1 Release Notes

Document ID: prod-halcyon-firmware-release-notes-3-4-1 | Version: 2 | Released: 2025-06-02 | Supersedes: prod-halcyon-firmware-release-notes-3-4-0 | Owner: Halcyon Firmware Team, Leeds

## Applies to

- Halcyon HX-210 (all serial numbers)
- Halcyon HX-220 (all serial numbers)

Firmware 3.4.1 supersedes 3.4.0. All HX-210 and HX-220 instruments should be updated. The HX-200 remains on the 3.2.x branch; its current release is 3.2.4.

A separate maintenance build, 3.4.2, is available for the HX-220 only. It contains all 3.4.1 changes plus a fix for GPS time-to-first-fix at temperatures below -30 C. Do not load 3.4.2 on an HX-210; Meridian Link will reject it.

## Installation

Install with Meridian Link 4.2 or later, or through an HX-DOCK-4 running dock firmware D-2.1.0. Docks on D-2.0.x cannot install 3.4.1 and cannot calibrate instruments that run it; update the dock first. Installation takes approximately 5 minutes per instrument. Configuration and datalogs are preserved.

## Changes

- HX-210 calibration interval: maximum and default reduced from 180 days to 120 days, to align with HX-210 Calibration Procedure version 2. Instruments updated from 3.4.0 with an interval above 120 days are set to 120 days automatically, and the next-calibration countdown is recalculated.
- HX-220 calibration interval: maximum set to 150 days.
- Calibration gas presets for MI-CAL-Q4-B (1.1% vol CH4, 18.0% O2, 50 ppm CO, 15 ppm H2S) are loaded by default. The MI-CAL-Q4-A preset is removed.
- The %LEL scale for methane now uses an LEL of 4.4% vol (IEC 60079-20-1) instead of 5.0% vol. Readings in %LEL will be approximately 13.6% higher for the same methane concentration than under 3.4.0.
- Span stabilisation time increased to 120 seconds.

## Fixes

- Resolved the 3.4.0 known issue where the LEL channel failed span below 30% LEL.
- Fixed the missing Bluetooth pairing code after factory reset on the HX-220.
- Fixed an occasional false E-111 (battery fault) on HX-BAT-210 packs when charging below 5 C.
- Fixed the clock drifting by up to 40 seconds per week when the instrument is stored switched off.

## Known issues

- E-126 (real-time clock lost) may be shown after the battery has been disconnected for more than 72 hours. Dock the instrument to set the time.

## Compatibility

| Component | Minimum version |
|---|---|
| Meridian Link (PC) | 4.2 |
| HX-DOCK-4 dock firmware | D-2.1.0 |
| Meridian Link Mobile (HX-220) | 2.5 |
| Calibration gas | MI-CAL-Q4-B |
