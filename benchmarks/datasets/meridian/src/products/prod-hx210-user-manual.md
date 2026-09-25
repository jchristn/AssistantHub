# Halcyon HX-210 User Manual

Document ID: prod-hx210-user-manual | Revision: F | Effective: 2025-06-16 | Owner: Halcyon Product Engineering, Leeds | Applies to firmware 3.4.1

## 1. Safety information

Read this manual in full before using the Halcyon HX-210 portable four-gas detector. The HX-210 is personal protective equipment. It warns the wearer of hazardous concentrations of oxygen deficiency or enrichment, flammable gas, carbon monoxide (CO) and hydrogen sulphide (H2S). It does not remove any hazard, and it cannot warn of gases for which it has no sensor fitted.

The HX-210 is certified intrinsically safe with the marking II 1G Ex ia IIC T4 Ga. It may be used in Zone 0, Zone 1 and Zone 2 hazardous areas for gas groups IIA, IIB and IIC with temperature class T4. The ATEX certificate is Baseefa22ATEX0212X and the IECEx certificate is IECEx BAS 22.0098X. The "X" suffix means special conditions of use apply; these are listed below.

- Charge the instrument only in a safe (non-hazardous) area, using the HX-DOCK-4 docking station or the supplied single-unit charger.
- Replace the battery pack HX-BAT-210 only in a safe area. Do not use any other battery pack; the HX-BAT-220L pack from the HX-220 is not certified for the HX-210 and will not fit the housing.
- Do not open the housing in a hazardous area.
- Do not rub or clean the front label with a dry cloth in a hazardous area; the label may accumulate an electrostatic charge.
- Substitution of components may impair intrinsic safety.
- Perform a bump test before each day's use. Calibrate at least every 120 days, or sooner if a bump test fails.
- High off-scale readings of flammable gas may indicate an explosive concentration. Leave the area immediately.
- Oxygen-deficient atmospheres (below approximately 10% vol O2) can cause the catalytic LEL sensor to read low.
- Silicones, leaded petrol vapours, sulphur compounds and halogenated hydrocarbons can poison the catalytic LEL sensor. See section 11 and error code E-117.

## 2. Product overview

The Halcyon HX-210 is a compact portable detector that continuously monitors four gases at the same time. It is the core model of the Halcyon family. The HX-200 is the single-gas H2S sibling, and the HX-220 adds Bluetooth LE, GPS, a man-down alarm and a larger battery.

The HX-210 is fitted with four sensors:

- Oxygen (O2): electrochemical, part number HX-SNS-O2-01, range 0-30% vol, resolution 0.1% vol.
- Flammable gas (LEL): catalytic bead, part number HX-SNS-LEL-04, range 0-100% LEL, resolution 1% LEL, calibrated to methane (CH4).
- Carbon monoxide (CO): electrochemical, part number HX-SNS-CO-03, range 0-500 ppm, resolution 1 ppm.
- Hydrogen sulphide (H2S): electrochemical, part number HX-SNS-H2S-02, range 0-200 ppm, resolution 1 ppm.

The instrument is a diffusion instrument. Gas reaches the sensors through the sensor grille on the front face. The HX-210 does not support the optional sampling pump HX-PMP-22; for remote sampling use the HX-220.

The housing is rated IP67 and weighs 330 g including the battery. Dimensions are 125 x 68 x 40 mm. The operating temperature range is -20 to +55 C.

## 3. Package contents

The HX-210 starter kit, part number HX-210-KIT-03, contains:

- 1 x HX-210 detector with four sensors fitted and calibrated at the factory
- 1 x HX-BAT-210 rechargeable Li-ion battery pack, 3,400 mAh (fitted)
- 1 x single-unit USB-C charger with UK, EU, US and Type G/Type M plug adapters
- 1 x calibration cap HX-CAP-210 with 1 m hose
- 1 x stainless steel belt clip with fixing screws
- 1 x printed quick-start guide (see also prod-hx210-quick-start)
- 1 x factory calibration certificate

Calibration gas and regulators are not included. The HX-210 requires calibration gas MI-CAL-Q4-B and the 1.0 L/min regulator MI-REG-10. The older mix MI-CAL-Q4-A and the 0.5 L/min regulator MI-REG-05 are no longer approved for HX-210 calibration; see section 9.

## 4. Controls and indicators

The HX-210 has one large push button on the front (the ON/OK button) and two smaller buttons below the display (UP and DOWN).

- ON/OK: press and hold for 3 seconds to switch on. Press and hold for 5 seconds to switch off (a countdown is shown). Short press to confirm a menu selection or to acknowledge a latched alarm.
- UP: short press to scroll up or increase a value. Press and hold in measuring mode to display peak readings.
- DOWN: short press to scroll down or decrease a value. Press and hold in measuring mode to display STEL and TWA values.

Indicators:

- Two red high-intensity LEDs on the top edge flash during alarms.
- A green LED flashes once every 10 seconds to confirm correct operation (the "heartbeat" or confidence signal). The interval can be changed between 5, 10 and 30 seconds in the supervisor menu.
- A yellow LED lights when the calibration or bump test is overdue.
- The sounder produces 95 dB at 30 cm.
- A vibration motor operates in parallel with the sounder.

## 5. Charging and power-on

Charge the HX-210 fully before first use. A full charge from empty takes approximately 5 hours at 20 C. A fully charged HX-BAT-210 pack gives a typical runtime of 14 hours at 20 C with no alarms. Runtime is reduced at low temperature and during alarms; at -20 C expect approximately 9 hours.

To charge, place the instrument in the HX-DOCK-4 docking station or connect the single-unit charger to the USB-C port on the base. The battery icon animates while charging and shows a solid full symbol when charging is complete. Charge only between 0 and +40 C.

Power-on sequence:

1. Press and hold ON/OK for 3 seconds. All LEDs light, the sounder beeps and the motor vibrates. Confirm that you saw, heard and felt each alarm.
2. The display shows the firmware version (3.4.1 for instruments shipped from June 2025), the serial number and the instrument name.
3. The display shows the alarm set points for each gas in turn.
4. The display shows the days remaining until the next calibration and the next bump test. If either is overdue, the yellow LED lights and "CAL DUE" or "BUMP DUE" is shown.
5. The sensors warm up for 45 seconds. The LEL sensor may read slightly negative during warm-up; this is normal.
6. The instrument enters measuring mode. Perform a fresh-air zero (section 8) if the readings are not zero in clean air.

## 6. Display and navigation

In measuring mode the display shows four readings in a two-by-two grid: O2 (top left), LEL (top right), CO (bottom left) and H2S (bottom right). The status bar at the top shows battery level, time, and icons for datalogging and docking.

To enter the user menu, press UP and DOWN together for 2 seconds. The user menu contains:

- Menu > Peak Readings: shows and clears peak readings since power-on.
- Menu > Bump Test: starts a manual bump test.
- Menu > Zero: performs a fresh-air zero of O2, LEL, CO and H2S.
- Menu > Instrument: shows serial number, firmware version, battery health and the Service submenu.
- Menu > Settings: display contrast, language, time format and backlight timeout (default 15 seconds).
- Menu > Supervisor: protected by the supervisor passcode (see section 12).

The menu closes automatically after 30 seconds without a button press and returns to measuring mode. The instrument continues to monitor gas and will alarm while any menu is open.

## 7. Alarms

The HX-210 has five types of gas alarm: low, high, STEL (short-term exposure limit, 15-minute rolling average), TWA (time-weighted average, 8-hour) and over-range. O2 has low and high alarms only, because oxygen is hazardous both when deficient and when enriched.

Factory default set points for the HX-210:

| Gas | Low alarm | High alarm | STEL | TWA |
|---|---|---|---|---|
| H2S | 5 ppm | 10 ppm | 15 ppm | 5 ppm |
| CO | 30 ppm | 100 ppm | 200 ppm | 30 ppm |
| LEL (CH4) | 10% LEL | 20% LEL | n/a | n/a |
| O2 | 19.5% vol (falling) | 23.0% vol (rising) | n/a | n/a |

Alarm behaviour:

- Low alarm: slow two-tone sound, slow red flash, pulsed vibration. Non-latching by default; the alarm clears when the reading falls below the set point.
- High alarm: fast sound, fast red flash, continuous vibration. Latching; the alarm continues after the gas clears until the user presses ON/OK in clean air.
- STEL and TWA alarms: latching, and cannot be acknowledged until the instrument is switched off. Leave the area and inform your supervisor.
- Over-range: the display shows "OR" and the high alarm operates. For LEL, an over-range reading latches the LEL channel out of service until the instrument is switched off and on in clean air, to protect the catalytic sensor.

Set points can be changed only in the supervisor menu (Menu > Supervisor > Alarms > Set Points) or with Meridian Link. Do not set the O2 high alarm above 23.5% vol or the LEL high alarm above 60% LEL; the instrument will reject values outside the certified limits.

Low battery warning: at approximately 30 minutes of runtime remaining, the instrument beeps once a minute and shows "BATT LOW". At approximately 5 minutes remaining it shows "BATT OFF" and switches itself off after a 60-second countdown.

## 8. Fresh-air zero and bump testing

### 8.1 Fresh-air zero

Perform a fresh-air zero whenever the instrument is switched on in clean air and readings are not zero (or 20.9% for O2). Select Menu > Zero and confirm with ON/OK. The zero takes 30 seconds. Do not zero in an area where gas may be present.

### 8.2 Bump test

A bump test confirms that each sensor responds to gas and each alarm operates. Bump test the HX-210 before each day's use.

The bump test can be performed in the HX-DOCK-4 (automatic, recommended) or manually:

1. Fit the calibration cap HX-CAP-210 and connect the hose to an MI-REG-10 regulator on a cylinder of MI-CAL-Q4-B.
2. Select Menu > Bump Test and confirm.
3. Open the regulator. Gas flows at 1.0 L/min.
4. The bump test passes if each sensor reaches at least 80% of the cylinder concentration within 45 seconds and the alarms operate. O2 must fall to 18.0% vol plus or minus 0.5.
5. Close the regulator, remove the cap and allow the readings to return to normal before use.

If any sensor fails the bump test, calibrate the instrument (section 9). If the sensor still fails, replace it following sup-hx-sensor-replacement-procedure.

## 9. Calibration

The HX-210 must be calibrated at least every 120 days. This interval was reduced from 180 days in May 2025 following analysis of field data showing LEL sensitivity drift. The instrument counts down the days to the next calibration and displays "CAL DUE" when the interval expires. The interval is set to 120 days at the factory from firmware 3.4.1.

Calibration uses MI-CAL-Q4-B (1.1% vol CH4 equivalent to 25% LEL, 18.0% O2, 50 ppm CO and 15 ppm H2S, balance nitrogen) at 1.0 L/min through the MI-REG-10 regulator. The full step-by-step method, including the span stabilisation wait of 120 seconds, is in the controlled document HX-210 Calibration Procedure, document ID prod-hx210-calibration-procedure-v2. Always follow the current version of that procedure. Version 1 of the procedure (180-day interval, MI-CAL-Q4-A) is withdrawn.

Calibration in the HX-DOCK-4 is recommended because it records the result automatically and prints or exports a certificate. The dock must be running dock firmware D-2.1.0 or later to calibrate instruments on firmware 3.4.1.

If calibration fails, the instrument shows E-104 (span calibration failed). See section 13 and the Halcyon Error Code Reference.

## 10. Datalogging and Meridian Link

The HX-210 logs readings from all four sensors at a 1-minute interval by default. At a 1-minute interval the datalog holds approximately 90 days of continuous operation, after which the oldest data is overwritten. The interval can be set between 1 second and 15 minutes in the supervisor menu (Menu > Supervisor > Datalog > Interval). If overwriting is disabled, the instrument shows E-140 (datalog memory full) when the log is full.

The event log records the last 500 events, including alarms, bump tests, calibrations, faults and power cycles, with a time stamp.

To download the datalog, dock the instrument in the HX-DOCK-4 or connect it by USB-C to a PC running Meridian Link version 4.2 or later. In Meridian Link, select Instruments > Download Logs. Data can be exported as CSV or as a PDF exposure report.

The real-time clock is set automatically when the instrument is docked. If the clock is lost (for example after the battery has been removed for more than 72 hours), the instrument shows E-126 and must be docked or connected to Meridian Link to set the time.

## 11. Maintenance

### 11.1 Cleaning

Clean the housing with a damp cloth and mild detergent only. Do not use solvents, silicone-based polishes or alcohol wipes near the sensor grille; these can poison the LEL sensor or damage the dust membrane. Do not immerse the instrument deliberately, although it is rated IP67.

### 11.2 Dust membrane

The sensor grille is protected by a hydrophobic dust membrane, part number HX-MEM-210. Inspect the membrane weekly. Replace it every 6 months, or immediately if it is visibly dirty, torn or wet. A blocked membrane causes slow response and may cause a bump test failure.

### 11.3 Checking sensor life

The instrument estimates the remaining life of each sensor from its sensitivity at the last calibration. To view it, select Menu > Instrument > Service > Sensor Life. Each sensor is shown with a percentage. Plan to replace a sensor when its remaining life falls below 20%. The O2 sensor typically lasts 24 to 30 months; the LEL, CO and H2S sensors typically last 36 months in normal use.

### 11.4 Opening the housing

The rear housing is held by six captive Torx T10 screws. Open the housing only in a safe area, with the instrument switched off, to replace the battery pack or sensors.

When refitting the rear housing, tighten the six Torx T10 screws in a diagonal pattern to 0.45 N m. Do not exceed 0.5 N m; over-tightening can crack the screw bosses and invalidate the IP67 rating. Check that the rear gasket, part number HX-GSK-210, is seated in its groove and is not twisted. Replace the gasket every time a sensor is replaced or if it shows any cut or compression set.

### 11.5 Replacing the battery pack

1. Switch off the instrument and move to a safe area.
2. Remove the six rear housing screws and lift off the rear housing.
3. Disconnect the HX-BAT-210 connector by pulling the connector body, not the wires.
4. Fit the new HX-BAT-210, route the wires in the channel and refit the rear housing as described in 11.4.
5. Charge the new pack fully before use. The real-time clock may need to be reset by docking.

### 11.6 Replacing sensors

Sensor replacement is described in the Halcyon Sensor Replacement Procedure (sup-hx-sensor-replacement-procedure). A full calibration is required after any sensor is replaced.

## 12. Supervisor settings

The supervisor menu is protected by a four-digit passcode. The factory default supervisor passcode for the HX-210 is 2107. Change it on commissioning using Menu > Supervisor > Security > Change Passcode. After five incorrect passcode entries the supervisor menu is locked for 10 minutes.

Supervisor settings include:

- Alarm set points and latching (Menu > Supervisor > Alarms)
- Calibration interval, 30 to 120 days (Menu > Supervisor > Calibration > Interval). From firmware 3.4.1 the HX-210 will not accept a calibration interval above 120 days.
- Bump test interval and "bump before use" enforcement (Menu > Supervisor > Bump)
- Datalog interval and overwrite (Menu > Supervisor > Datalog)
- Confidence beep and heartbeat LED interval (Menu > Supervisor > Indicators)
- Instrument name and user name, up to 16 characters (Menu > Supervisor > Identity)
- Stealth mode, which disables the confidence beep and heartbeat LED but never the alarm sounder

If the supervisor passcode is lost, it can be reset from Meridian Link (Instruments > Security > Reset Passcode) by a user with the Fleet Administrator role. It cannot be reset from the instrument itself.

## 13. Troubleshooting

When the HX-210 detects a fault it shows an error code in the form E-1nn and sounds a fault tone (three short beeps every 30 seconds). The full list of codes, their meaning, the models they apply to and the corrective action is in the Halcyon Error Code Reference (document ID sup-halcyon-error-codes). The most common codes on the HX-210 are:

- E-101: sensor not detected. Reseat the sensor.
- E-104: span calibration failed. Check the cylinder and repeat calibration.
- E-108: calibration overdue.
- E-117: LEL sensor poisoned or bridge fault. Replace the LEL sensor; do not attempt to recalibrate.
- E-126: real-time clock lost.

Other symptoms:

| Symptom | Likely cause | Action |
|---|---|---|
| Instrument will not switch on | Battery fully discharged | Charge for at least 30 minutes, then try again |
| Slow response to bump gas | Blocked dust membrane | Replace HX-MEM-210 (section 11.2) |
| O2 reads below 20.9% in fresh air | O2 sensor ageing | Zero in fresh air; check sensor life (section 11.3) |
| LEL reads negative in clean air | Temperature change since zero | Allow 10 minutes to stabilise, then zero |
| Instrument does not communicate with dock | Dock firmware older than D-2.1.0 | Update dock firmware (see prod-halcyon-docking-station-guide) |

If a fault cannot be resolved, raise a ticket in the Meridian Support Centre (support.meridian-instruments.com). Instruments returned for repair follow the current RMA process.

## 14. Technical specifications

| Parameter | Value |
|---|---|
| Model | Halcyon HX-210 portable four-gas detector |
| Gases and ranges | O2 0-30% vol; LEL 0-100% LEL (CH4); CO 0-500 ppm; H2S 0-200 ppm |
| Resolution | O2 0.1% vol; LEL 1% LEL; CO 1 ppm; H2S 1 ppm |
| Sensor part numbers | HX-SNS-O2-01, HX-SNS-LEL-04, HX-SNS-CO-03, HX-SNS-H2S-02 |
| Sampling | Diffusion |
| Response time (T90) | O2 15 s; LEL 20 s; CO 25 s; H2S 30 s |
| Battery | HX-BAT-210, 3,400 mAh Li-ion, rechargeable |
| Runtime | 14 h typical at 20 C |
| Charge time | 5 h |
| Calibration interval | 120 days maximum |
| Calibration gas | MI-CAL-Q4-B at 1.0 L/min (MI-REG-10) |
| Certification | ATEX II 1G Ex ia IIC T4 Ga (Baseefa22ATEX0212X); IECEx Ex ia IIC T4 Ga (IECEx BAS 22.0098X) |
| Hazardous area | Zone 0, 1 and 2 |
| Ingress protection | IP67 |
| Operating temperature | -20 to +55 C |
| Humidity | 10-95% RH non-condensing |
| Audible alarm | 95 dB at 30 cm |
| Datalog | 90 days at 1-minute interval; 500-event log |
| Dimensions | 125 x 68 x 40 mm |
| Weight | 330 g |
| Firmware | 3.4.1 |
| Starter kit | HX-210-KIT-03 |

Warranty terms for the instrument, sensors and battery are set out in the current Meridian warranty terms. Dispose of the instrument and battery in accordance with local WEEE and battery regulations.

## 15. Storage, transport and disposal

### 15.1 Storage

Store the HX-210 switched off, in a clean, dry area between 0 and +30 C, away from solvents, silicones and fuel vapours. For storage longer than 30 days, charge the HX-BAT-210 to between 40% and 60% and recharge it every 3 months. A pack stored fully discharged for more than 6 months may be permanently damaged and will show E-111 when next charged.

Electrochemical sensors continue to age while the instrument is switched off. An instrument that has been stored for more than 120 days will show "CAL DUE" on power-on and must be calibrated before use, because the calibration countdown continues during storage.

### 15.2 Transport

The HX-BAT-210 is a lithium-ion battery with a rating below 100 Wh. When the pack is installed in the instrument it can be shipped as UN3481 (lithium-ion batteries contained in equipment) under the applicable IATA packing instruction. Spare packs shipped separately must be protected against short circuit and must not exceed 30% state of charge for air freight. Instruments returned to Meridian must be accompanied by a completed decontamination declaration as described in the current RMA process.

Calibration gas cylinders MI-CAL-Q4-B are classified as compressed gas, toxic, and must be transported in accordance with ADR/DOT regulations. Do not send partly used cylinders with a returned instrument.

### 15.3 Disposal

At the end of its life, the HX-210 must not be disposed of with general waste. Return it to Meridian or to an approved WEEE collection point. Remove the battery pack and dispose of it separately. Electrochemical sensors contain acid electrolyte and, in the case of the O2 sensor, lead; treat them as hazardous waste.

## 16. Glossary

- Bump test: a brief exposure to a known gas to confirm that sensors respond and alarms operate. It does not adjust the instrument.
- Calibration: adjustment of the zero and span of each sensor using a certified gas mixture.
- %LEL: percentage of the lower explosive limit. For methane, 100% LEL corresponds to 4.4% vol under IEC 60079-20-1, which is the basis used by firmware 3.4.1 and later.
- STEL: short-term exposure limit, a 15-minute rolling average exposure.
- TWA: time-weighted average exposure over an 8-hour shift.
- Latching alarm: an alarm that continues after the gas clears until the user acknowledges it.
- Span: the sensitivity adjustment applied during calibration.
- Zone 0: an area in which an explosive gas atmosphere is present continuously or for long periods. The HX-210 is the only Halcyon model approved for Zone 0.
- Intrinsic safety (Ex ia, Ex ib): a protection concept that limits electrical and thermal energy below the level that could ignite a gas atmosphere. Ex ia allows use in Zone 0; Ex ib is limited to Zone 1 and Zone 2.
- Meridian Link: the Windows PC software used to configure Halcyon instruments and docking stations, download datalogs and manage instrument fleets.

## 17. Document history

| Revision | Date | Changes |
|---|---|---|
| D | 2024-03-12 | Aligned with HX-210 Calibration Procedure version 1 (180-day interval) |
| E | 2024-11-04 | Updated for firmware 3.4.0: 500-event log, configurable confidence beep |
| F | 2025-06-16 | Updated for firmware 3.4.1: 120-day calibration interval, MI-CAL-Q4-B, MI-REG-10, 4.4% vol LEL basis, dock firmware D-2.1.0 |
