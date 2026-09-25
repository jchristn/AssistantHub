# HX-DOCK-4 Docking Station Guide

Document ID: prod-halcyon-docking-station-guide | Revision: B | Effective: 2025-06-20 | Owner: Halcyon Product Engineering, Leeds

## 1. Overview

The HX-DOCK-4 is a four-bay docking station for the Halcyon family of portable gas detectors. It automatically charges, bump tests and calibrates instruments, sets their real-time clock, updates their firmware and downloads their datalogs. Results are stored on the dock and synchronised to Meridian Link on a connected PC.

The HX-DOCK-4 accepts the HX-210 and HX-220 directly. The HX-200 must first be fitted into the HX-ADP-200 adapter, which positions its smaller housing over the gas ports and charging contacts. Up to four instruments of any mix can be docked at the same time.

The docking station is not certified for use in hazardous areas. Install it in a safe area only.

## 2. Installation

1. Place the dock on a level bench or mount it on a wall using the four keyhole slots (M5 screws, not supplied).
2. Connect the 24 V DC power supply (supplied, part number HX-PSU-24).
3. Connect the calibration gas cylinder to the GAS IN port using the demand-flow regulator MI-REG-DF. The dock controls flow internally; do not use a fixed-flow regulator on the dock. Inlet pressure must be between 0.5 and 1.0 bar.
4. Connect a vent hose (maximum 10 m) from the EXHAUST port to a safe area.
5. Connect the dock to a PC with the USB-C cable, or to the site network using the Ethernet port. The Ethernet port uses DHCP by default.
6. Install Meridian Link 4.2 or later on the PC and add the dock with Docks > Add Dock.

## 3. Gas configuration

Configure the cylinder in Meridian Link under Docks > [dock] > Cylinders. For HX-210 and HX-220 instruments on firmware 3.4.1 or later, the cylinder must be MI-CAL-Q4-B (1.1% vol CH4, 18.0% O2, 50 ppm CO, 15 ppm H2S). For HX-200 instruments, a second gas inlet (GAS IN 2) can be connected to MI-CAL-H2S25. The dock selects the correct inlet automatically based on the instrument model.

The dock warns when a cylinder is within 30 days of expiry and refuses to use an expired cylinder.

## 4. Operation

When an instrument is docked, the bay LED shows its status:

- Blue: charging
- Flashing yellow: bump test or calibration in progress
- Green: test passed, instrument charged
- Red: test failed or instrument fault

The dock performs whichever action is due according to the policy set in Meridian Link (Docks > [dock] > Policy). The default policy is: bump test on every docking if the last bump test is more than 20 hours old; calibrate when the calibration is due within 7 days; download datalogs; synchronise the clock.

A bump test in the dock takes about 60 seconds. A calibration takes about 4 minutes for an HX-210 or HX-220 and about 3 minutes for an HX-200.

## 5. Firmware compatibility

The dock firmware must be compatible with the instrument firmware. Update the dock before updating instruments.

| Dock firmware | HX-200 firmware supported | HX-210 / HX-220 firmware supported | Notes |
|---|---|---|---|
| D-1.9.x | 3.2.0 to 3.2.2 | up to 3.3.x | End of support 31 March 2025 |
| D-2.0.x | 3.2.0 to 3.2.4 | up to 3.4.0 | Cannot calibrate instruments on 3.4.1 or later |
| D-2.1.0 | 3.2.0 to 3.2.4 | 3.3.0 to 3.4.2 | Required for MI-CAL-Q4-B presets and 120-day HX-210 interval |

To update the dock, select Docks > [dock] > Firmware > Check for Updates in Meridian Link. The update takes about 8 minutes, during which the bays are disabled. Do not disconnect power.

## 6. Maintenance

- Replace the inlet filter HX-FLT-D4 every 12 months.
- Check the bay gaskets monthly for cracks.
- Leak-test the gas path every 6 months using Docks > [dock] > Service > Leak Test. The test fails if pressure drops by more than 5 mbar in 60 seconds.

## 7. Troubleshooting

| Symptom | Action |
|---|---|
| Bay LED red, instrument shows E-104 | Check that the cylinder is MI-CAL-Q4-B and in date; see sup-halcyon-error-codes |
| Instrument not recognised | Clean the charging contacts; for HX-200 check the HX-ADP-200 adapter is seated |
| "FW INCOMPATIBLE" on dock display | Update dock firmware to D-2.1.0 |
| Dock not found in Meridian Link | Check USB-C cable or DHCP lease; the dock also answers on 169.254.10.4 if no DHCP server is found |

## 8. Specifications

| Parameter | Value |
|---|---|
| Bays | 4 |
| Compatible instruments | HX-210, HX-220; HX-200 with HX-ADP-200 |
| Power | 24 V DC, 60 W (HX-PSU-24) |
| Gas inlets | 2, demand flow, 0.5 to 1.0 bar |
| Connectivity | USB-C, Ethernet 10/100 |
| Storage | 10,000 test records |
| Operating temperature | 0 to +40 C |
| Dimensions | 420 x 190 x 150 mm |
| Weight | 3.4 kg |
| Current dock firmware | D-2.1.0 |
