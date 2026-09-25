# Tessera Alarm Configuration Guide

Document ID: prod-tessera-alarm-configuration-guide | Version: 2.3 | Effective: 2025-05-12 | Owner: Tessera Applications Engineering

## 1. Purpose

This guide explains how TesseraCloud sets default vibration alarm thresholds for an asset, based on ISO 10816-3, and how to tune them. It is intended for reliability engineers and Site Admins configuring new assets, and for Meridian applications engineers supporting commissioning.

## 2. ISO 10816-3 evaluation zones

ISO 10816-3 evaluates machine vibration using broadband velocity RMS, measured on the bearing housings, and classifies the result into four zones:

- Zone A: vibration typical of newly commissioned machines.
- Zone B: acceptable for unrestricted long-term operation.
- Zone C: unsatisfactory for long-term continuous operation; the machine may run for a limited period until a suitable opportunity for remedial action.
- Zone D: severe enough to cause damage to the machine.

The standard distinguishes machine groups by size and foundation type. TesseraCloud uses two groups:

- Group 1: large machines with rated power above 300 kW, and electrical machines with shaft height of 315 mm or more.
- Group 2: medium machines with rated power from 15 kW to 300 kW, and electrical machines with shaft height from 160 mm to 315 mm.

Machines below 15 kW are not covered by ISO 10816-3. For such machines TesseraCloud uses the Group 2 rigid values by default and flags the asset as "Outside ISO 10816-3 scope".

## 3. Default velocity RMS thresholds

When an asset is created with its rated power and foundation type, TesseraCloud applies the zone boundaries below (velocity RMS, 10 Hz to 1 kHz, in mm/s):

| Machine group | Foundation | A/B boundary | B/C boundary | C/D boundary |
|---|---|---|---|---|
| Group 1 (above 300 kW) | Rigid | 2.3 | 4.5 | 7.1 |
| Group 1 (above 300 kW) | Flexible | 3.5 | 7.1 | 11.0 |
| Group 2 (15-300 kW) | Rigid | 1.4 | 2.8 | 4.5 |
| Group 2 (15-300 kW) | Flexible | 2.3 | 4.5 | 7.1 |

TesseraCloud maps these boundaries to alarm severities as follows:

- Warning is raised at the B/C boundary.
- Alarm is raised at the C/D boundary.
- The A/B boundary is shown on trend charts as a reference line but does not raise an alarm.

Example: a 90 kW centrifugal pump on a rigid baseplate is Group 2 rigid, so it receives a Warning at 2.8 mm/s and an Alarm at 4.5 mm/s.

## 4. Baseline-relative thresholds

ISO limits are absolute and may be too loose for smooth-running machines. After 14 days of data, TesseraCloud computes a baseline for each measurement point and offers baseline-relative thresholds:

- Warning at 2.5 times the baseline velocity RMS.
- Alarm at 4 times the baseline velocity RMS.

Baseline-relative thresholds are never set higher than the ISO values; TesseraCloud uses whichever threshold is lower. Enable them at Sites > [site] > Assets > [asset] > Alarm Profile > Use Baseline.

## 5. Other indicators

| Indicator | Default Warning | Default Alarm | Notes |
|---|---|---|---|
| Acceleration peak | 10 g | 20 g | Useful for gearboxes and rolling element bearings |
| Crest factor | 5 | 7 | Rising crest factor indicates early bearing damage |
| Surface temperature | 80 C | 95 C | Measured at the node base |
| Node battery | 10% | 5% | Low battery also raises event T-305 |

## 6. Persistence and hysteresis

To avoid nuisance alarms, an alarm is raised only after the threshold is exceeded for 3 consecutive readings (persistence), and cleared only after readings fall below the threshold minus a 10% hysteresis band for 3 consecutive readings. At the default reporting interval of 1 sample per hour, a Warning therefore takes about 3 hours to appear. For critical assets, reduce the reporting interval rather than the persistence count; persistence below 2 is not permitted.

## 7. Editing an alarm profile

1. Open Sites > [site] > Assets > [asset] > Alarm Profile.
2. Confirm the machine group and foundation type. Changing either recalculates the defaults.
3. Override individual thresholds if needed. Overrides are shown with a pencil marker and are retained if the machine data changes.
4. Choose the escalation plan (see the TesseraCloud Administrator Guide, prod-tesseracloud-admin-guide, section 7).
5. Save. The profile applies from the next reading received.

During planned work, use maintenance mode (maximum 72 hours per activation) rather than disabling the alarm profile.
