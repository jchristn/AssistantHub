# Halcyon Firmware 3.4.0 Release Notes

Document ID: prod-halcyon-firmware-release-notes-3-4-0 | Version: 1 | Released: 2024-11-04 | Owner: Halcyon Firmware Team, Leeds

## Applies to

- Halcyon HX-210 (all serial numbers)
- Halcyon HX-220 (all serial numbers)

Firmware 3.4.0 does not apply to the HX-200, which remains on the 3.2.x branch (current release 3.2.3 at the time of writing).

## Installation

Install with Meridian Link 4.1 or later, or through an HX-DOCK-4 docking station running dock firmware D-2.0.x or later. Installation takes approximately 4 minutes per instrument. Configuration, calibration data and datalogs are preserved. Do not remove the instrument from the dock during the update; an interrupted update causes E-122 (firmware checksum) and the instrument must be reflashed.

## New features

- Configurable confidence-beep interval (5, 10 or 30 seconds) in Menu > Supervisor > Indicators.
- Event log extended to 500 events on the HX-210 and 1,000 events on the HX-220.
- HX-220: man-down timer now configurable between 30 and 180 seconds (previously fixed at 90 seconds).
- HX-220: GPS position included in the datalog every 60 seconds.

## Fixes

- Fixed an issue where the TWA calculation for H2S reset to zero after a low-battery warning.
- Fixed incorrect display of the CO STEL value after acknowledging a high alarm.
- Fixed a rare lock-up when the backlight timeout was set to "always on" and a bump test was started from the menu.
- HX-220: fixed Bluetooth reconnection failures with Android 14 phones.

## Known issues

- On some HX-210 instruments the LEL channel may fail span with E-104 when calibrated with gas concentrations below 30% LEL. Continue to use MI-CAL-Q4-A (2.5% vol CH4) with this release.
- The calibration interval maximum remains 180 days for the HX-210 and HX-220.
- HX-220: after a factory reset the Bluetooth pairing code is not displayed until the instrument is power-cycled.

## Compatibility

| Component | Minimum version |
|---|---|
| Meridian Link (PC) | 4.1 |
| HX-DOCK-4 dock firmware | D-2.0.0 |
| Meridian Link Mobile (HX-220) | 2.3 |
