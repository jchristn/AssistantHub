# Halcyon Gas Detector Calibration Procedure

Document ID: mfg-halcyon-calibration-procedure

Owner: Quality Engineering

Last reviewed: 2025-06-16

Applies to: Penang final test and Rotterdam Repair Centre

## 1. Purpose

This procedure defines how Meridian Instruments calibrates Halcyon portable gas detectors (HX-200, HX-210 and HX-220) before shipment from the Penang factory and after repair or recalibration at the Rotterdam Repair Centre.

A Halcyon detector is a life-safety device, so this procedure has no optional steps. If a step cannot be completed as written, the unit is placed on hold and the failure handling section applies.

## 2. Scope

This procedure covers factory and repair-centre calibration; customer field calibration follows the product calibration procedures, for example the HX-210 calibration procedure v2.

This procedure applies to:

- New Halcyon units at Penang final test, after PCBA assembly, sensor fitting and functional test are complete.
- Units returned under RMA to the Rotterdam Repair Centre for repair, sensor replacement or routine recalibration.
- Units reworked at Penang after a failed calibration or a failed outgoing quality audit.

This procedure does not cover:

- Bump testing and calibration performed by customers in the field. Customer guidance is summarised in section 12 for reference only.
- Calibration of Tessera vibration monitoring products or Lumen optical-flow meters, which have their own final test specifications.
- Certification of calibration gas cylinders, which is performed by the gas supplier and verified on receipt by Goods Inward.

## 3. Definitions

- Zero: the sensor reading in the absence of the target gas. For toxic and flammable sensors this is 0 ppm or 0% LEL. For the oxygen sensor the equivalent reference point is 20.9% vol in clean air.
- Span: the sensor reading when exposed to a known concentration of calibration gas.
- Pre-adjustment reading ("as found"): the span reading taken before any adjustment is made. It indicates how far the sensor has drifted.
- Post-adjustment reading ("as left"): the span reading taken after adjustment. This is the reading used for pass or fail.
- LEL: lower explosive limit. For methane, 100% LEL corresponds to 4.4% vol, so 25% LEL corresponds to 1.1% vol.
- Man-down alarm: an HX-220 feature that raises an alarm when the wearer has been motionless for a configured period.
- Drift flag: a status recorded against a sensor serial number in the Atlas quality module when its as-found reading is outside the drift limit in step 14.

## 4. Models and sensors

The Halcyon range uses the same main board (HX-MB) across all three models. The sensor fit, the radio options and the firmware configuration determine the model.

| Model | Sensors fitted | Gases measured | Additional features |
|---|---|---|---|
| HX-200 | 1 electrochemical | Hydrogen sulphide (H2S) only | None; single-gas detector |
| HX-210 | 2 electrochemical, 1 catalytic bead, 1 electrochemical O2 | O2, LEL (CH4), CO, H2S | None; standard 4-gas detector |
| HX-220 | As HX-210 | O2, LEL (CH4), CO, H2S | Bluetooth LE 5.0, GPS, man-down alarm |

The HX-200 is calibrated only for H2S. The HX-210 and HX-220 are calibrated for all four gases. The HX-220 then receives additional functional checks for Bluetooth LE, GPS and the man-down alarm.

### 4.1 Firmware

| Model | Firmware branch | Minimum for calibration |
|---|---|---|
| HX-200 | 3.2.x | Current release 3.2.4 |
| HX-210 | 3.4.x | 3.4.1 |
| HX-220 | 3.4.x (HX-220-only maintenance build) | 3.4.2 |

## 5. Calibration gases

All calibration gases are supplied in certified cylinders. Cylinders must carry a certificate of analysis stating an accuracy of ±2% of the stated concentration. Do not use cylinders past expiry, even if they still hold pressure.

| Cylinder | Used for | Contents | Flow | Regulator |
|---|---|---|---|---|
| MI-CAL-H2S25 | HX-200 | 25 ppm H2S in N2 | 0.5 L/min | MI-REG-05 |
| MI-CAL-Q4-B | HX-210 and HX-220 | 1.1% vol CH4 (25% LEL), 18.0% vol O2, 50 ppm CO, 15 ppm H2S | 1.0 L/min | MI-REG-10 |

| Gas | Concentration | Sensor | T90 limit |
|---|---|---|---|
| Hydrogen sulphide (HX-200) | 25 ppm | Electrochemical H2S | ≤ 30 s |
| Hydrogen sulphide (HX-210, HX-220) | 15 ppm | Electrochemical H2S | ≤ 30 s |
| Carbon monoxide (CO) | 50 ppm | Electrochemical CO | ≤ 35 s |
| Methane (CH4) | 25% LEL (1.1% vol) | Catalytic bead LEL | ≤ 20 s |
| Oxygen (O2) | 18.0% vol | Electrochemical O2 | ≤ 15 s |

The discontinued quad-gas cylinder MI-CAL-Q4-A (2.5% vol CH4, 100 ppm CO, 25 ppm H2S) must not be used with firmware 3.4.1 or later. Any MI-CAL-Q4-A cylinders still found at a station must be removed and returned to stores.

Each regulator is fitted only to its own cylinder type. MI-REG-05 regulators are marked with a yellow band and MI-REG-10 regulators with a blue band. A unit calibrated at the wrong flow rate gives a false span reading and must be recalibrated from step 1.

If the expiry dates on the cylinder label and the certificate of analysis differ, use the earlier one.

## 6. Equipment

Each calibration station must have the following equipment. The station ID must be recorded on every calibration certificate.

- Calibration station PC with the Halcyon Service Tool installed and logged in with an individual operator account (shared logins are not permitted).
- IR docking cradle for the Halcyon housing, connected to the station PC by USB.
- Regulator MI-REG-05 (0.5 L/min) for MI-CAL-H2S25, and regulator MI-REG-10 (1.0 L/min) for MI-CAL-Q4-B.
- Calibration cap and PTFE tubing for the Halcyon inlet, no longer than 1 metre, because H2S adsorbs onto other tubing materials.
- Certified calibration gas cylinders as listed in section 5.
- Calibrated thermometer and hygrometer displaying room conditions at the station.
- Bluetooth LE test dongle paired to the station PC, for HX-220 checks.
- Sound level meter with a 30 cm test jig, for the alarm check.
- Exhaust hood or vent line for spent gas.
- Personal H2S and CO monitor worn by the operator.

The calibration stations are:

| Station | Site | Location |
|---|---|---|
| CAL-PEN-01 | Penang | Final test bay, line A |
| CAL-PEN-02 | Penang | Final test bay, line A |
| CAL-PEN-03 | Penang | Final test bay, line B |
| CAL-PEN-04 | Penang | Final test bay, line B |
| CAL-PEN-05 | Penang | Rework area |
| CAL-PEN-06 | Penang | Outgoing quality audit |
| CAL-RTM-01 | Rotterdam | Repair Centre bench 1 |
| CAL-RTM-02 | Rotterdam | Repair Centre bench 2 |

CAL-PEN-05 is used only for reworked units. CAL-PEN-06 is reserved for outgoing quality audit re-checks and must not be used for first-pass production calibration. HX-220 GPS checks at Penang are done at CAL-PEN-01 to CAL-PEN-04, which have a roof-mounted GPS re-radiating antenna over the final test bay.

## 7. Environmental conditions

Calibration must be performed at 20 ±5 °C and 30–70% RH. The operator checks the station thermometer and hygrometer at the start of each shift and before each batch, and records both readings in the Service Tool session header.

If the room is outside these limits, stop calibrating. Raise a facilities request and do not resume until conditions have been within limits for at least 30 minutes.

Units that arrive from cold storage or from a shipping container must acclimatise at station conditions for at least 2 hours before calibration.

## 8. Preparation

Before starting a batch the operator must complete these checks. They are recorded as a checklist in the Service Tool session header.

- Confirm the station ID matches the label on the bench.
- Confirm each cylinder in use is within expiry and has at least 150 psi (about 10 bar) remaining pressure.
- Confirm no MI-CAL-Q4-A cylinder is present at the station.
- Confirm MI-REG-05 is fitted to MI-CAL-H2S25 and MI-REG-10 is fitted to MI-CAL-Q4-B.
- Confirm the tubing is PTFE, undamaged, and connected to the calibration cap.
- Record room temperature and humidity.
- Confirm the Halcyon Service Tool version shown on the start screen matches the controlled version listed in the Penang or Rotterdam work instruction index.

## 9. Calibration procedure

The steps below are numbered continuously and must be performed in order. Steps that apply to only some models say so.

### 9.1 Unit set-up

1. Scan the unit serial number barcode into the Halcyon Service Tool. The tool retrieves the model, the sensor serial numbers and the build record from Atlas.
2. Confirm the model shown in the Service Tool matches the label on the unit housing. If they differ, stop and place the unit on hold.
3. Place the unit in the IR docking cradle and power it on. Wait for the self-test to complete (about 20 seconds). Any self-test error places the unit on hold.
4. Check the firmware version reported by the unit against section 4.1. The HX-210 minimum is 3.4.1. The HX-220 minimum is 3.4.2, the HX-220-only maintenance build; do not load a generic 3.4.x image onto an HX-220. HX-200 units run the 3.2.x branch and are updated to the current release, 3.2.4, if older. Update firmware through the Service Tool before continuing.
5. Confirm the unit has been powered for at least 10 minutes before zeroing if it contains a catalytic bead (HX-210 and HX-220). The Service Tool enforces this warm-up with a countdown.

### 9.2 Zero

6. Remove the calibration cap. Zero in clean air: the unit must be in the station's fresh air supply, away from exhaust vents and away from any cylinder outlet.
7. Start the zero routine in the Service Tool. The tool sets the toxic and LEL channels to 0 and sets O2 to 20.9%.
8. Check the zero results. Acceptable zero readings after the routine are 0 ±1 ppm for CO, 0 ±0.5 ppm for H2S, 0 ±1% LEL for the LEL channel, and 20.9 ±0.2% vol for O2. If any channel is outside these limits, repeat the zero once. A second failure places the unit on hold.

### 9.3 Span: as-found reading

9. Fit the calibration cap to the unit inlet. For the HX-200, connect the tubing to MI-CAL-H2S25 through regulator MI-REG-05. For the HX-210 and HX-220, connect the tubing to MI-CAL-Q4-B through regulator MI-REG-10.
10. Open the regulator. Gas flows at 0.5 L/min for the HX-200 or 1.0 L/min for the HX-210 and HX-220. Start the T90 timer in the Service Tool at the moment gas flow begins.
11. Watch the live readings. The Service Tool records the time at which each channel reaches 90% of its final stable value. These times are the T90 results.
12. Wait for the stabilisation time of 120 seconds from gas flow start before taking any reading. This applies to all models. Do not take a reading before 120 seconds even if the display looks stable.
13. Record the as-found span reading for each channel. The Service Tool captures this automatically when the operator presses "Record as found".
14. Compare each as-found reading with the applied concentration. If the pre-adjustment span reading deviates more than 10% from the applied gas, flag the sensor as "drift" in the Atlas quality module against the sensor serial number. If the deviation exceeds 20%, replace the sensor instead of adjusting it: do not continue with span adjustment for that channel, close the gas, and go to section 11 for the sensor replacement route. Deviations of 10% or less need no flag and go straight to adjustment.

### 9.4 Span adjustment and as-left reading

15. For each channel within 20% as found, press "Adjust span" in the Service Tool. The tool sets the sensor gain so the reading matches the applied concentration.
16. Keep gas flowing and wait 30 seconds after the adjustment for the reading to settle.
17. Record the as-left span reading for each channel. The pass tolerance is ±5% of applied concentration after adjustment. For the HX-200, H2S must read between 23.75 and 26.25 ppm. For the HX-210 and HX-220, CO must read between 47.5 and 52.5 ppm, H2S between 14.25 and 15.75 ppm, LEL between 23.75 and 26.25% LEL, and O2 between 17.1 and 18.9% vol.
18. Check the T90 results captured in step 11 against the limits: CO ≤ 35 s, H2S ≤ 30 s, LEL ≤ 20 s, O2 ≤ 15 s. A channel that exceeds its T90 limit fails even if its as-left reading is within tolerance, because a slow sensor delays the alarm.
19. Close the regulator and disconnect the tubing from the cylinder. Leave the calibration cap on the unit.
20. Purge the unit with clean air for at least 60 seconds, until all toxic and LEL channels read below 10% of their span value and O2 reads 20.9 ±0.2% vol.

### 9.5 HX-220 functional checks (HX-220 only)

21. Bluetooth LE pairing check. Put the unit in pairing mode from the Service Tool and pair it with the station's Bluetooth LE test dongle. The Service Tool confirms a Bluetooth LE 5.0 connection, reads the unit serial number over the link and compares it with the scanned barcode. A failed pairing is retried once; a second failure fails the unit.
22. GPS fix. Place the unit under the GPS re-radiating antenna and start the GPS test. The unit must report a valid position fix within 120 seconds of the test starting.
23. Man-down alarm test. Start the man-down test in the Service Tool, which temporarily shortens the no-motion timer. Leave the unit still on the bench. Confirm the pre-alarm and the full man-down alarm both activate, and that the unit sends the man-down event over the Bluetooth LE link to the test dongle.
24. Record the results of steps 21 to 23 in the Service Tool. All three must pass.
25. Restore the man-down timer to the value in the Atlas build record and unpair the test dongle, so the unit ships with no stored pairing.

### 9.6 Alarm and final checks

26. Run the alarm check in the Service Tool. The tool simulates each alarm threshold and confirms the audible alarm, the visual LEDs and the vibration motor all activate. Measure the audible alarm with the sound level meter at 30 cm: at least 95 dB for the HX-200 and HX-210, and at least 103 dB for the HX-220. Any alarm output failure fails the unit.
27. Confirm the unit's configured alarm set points match the customer order or the default set points in the Atlas build record.
28. Set the unit's "last calibrated" date to the current date and the "calibration due" date to the model's calibration interval: 180 days for the HX-200, 120 days for the HX-210, and 150 days for the HX-220. The Service Tool writes both dates into the unit's memory.
29. Remove the unit from the cradle, remove the calibration cap, and check the display shows the calibration due date correctly.
30. Print the calibration certificate on form MFG-F-207 and place it in the unit packaging (Penang) or the RMA return pack (Rotterdam). The electronic copy is saved automatically to the Atlas quality module.
31. Apply the "Calibrated" label showing the calibration date, the due date, the station ID and the operator initials.
32. Move the unit to the passed tray. At Penang one unit in every 50 is taken by outgoing quality audit for a re-check on CAL-PEN-06.

## 10. Acceptance criteria

A unit passes calibration only if every applicable criterion below is met.

| Check | Criterion | Applies to |
|---|---|---|
| Self-test | No errors | All models |
| Firmware | 3.2.4 / 3.4.1 / 3.4.2 or later | HX-200 / HX-210 / HX-220 |
| Zero | Within limits in step 8 | All models |
| As-found span | 20% or less deviation from applied (above 10% is recorded as drift) | All channels |
| As-left span | Within ±5% of applied concentration | All channels |
| T90 CO | ≤ 35 s | HX-210, HX-220 |
| T90 H2S | ≤ 30 s | All models |
| T90 LEL | ≤ 20 s | HX-210, HX-220 |
| T90 O2 | ≤ 15 s | HX-210, HX-220 |
| Bluetooth LE, GPS, man-down | All pass | HX-220 |
| Audible alarm at 30 cm | ≥ 95 dB | HX-200, HX-210 |
| Audible alarm at 30 cm | ≥ 103 dB | HX-220 |
| Calibration due date | 180 / 120 / 150 days | HX-200 / HX-210 / HX-220 |
| Environment | 20 ±5 °C and 30–70% RH during calibration | All stations |

## 11. Failure handling

### 11.1 Sensor replacement

When step 14 finds a deviation above 20%, or when a channel fails the as-left tolerance or T90 limit after one repeat, the sensor is replaced.

- Place the unit on hold in the Service Tool, which updates the unit status in Atlas.
- At Penang, route the unit to the rework area. At Rotterdam, the repair technician replaces the sensor on the bench.
- Record the removed sensor serial number and the reason code in the Atlas quality module. Reason codes are DRIFT (above 20% as found), SLOW (T90 exceeded), SPAN (as-left out of tolerance) and DAMAGE (physical damage or contamination).
- Fit the replacement sensor and let it stabilise. Electrochemical sensors need at least 1 hour powered before calibration. Catalytic bead sensors need the 10-minute warm-up in step 5.
- Recalibrate the whole unit from step 1. At Penang, reworked units are calibrated on CAL-PEN-05.

### 11.2 Repeat attempts

A channel may be recalibrated once without replacing the sensor if the first failure was an as-left tolerance failure of less than 2% outside the limit, which is often caused by a loose calibration cap, a tubing leak or the wrong regulator. Check the cap, tubing and regulator before repeating. Any second failure means sensor replacement.

### 11.3 HX-220 functional failures

A unit that fails the Bluetooth LE, GPS or man-down checks in steps 21 to 23 is placed on hold with reason code FUNC and routed to rework for radio module or accelerometer diagnosis. After repair the unit is recalibrated from step 1.

### 11.4 Drift trends

Quality Engineering reviews drift flags from the Atlas quality module every week. If more than 3% of sensors from a single supplier lot are flagged as drift in one week, Quality Engineering places that lot on hold and raises a supplier corrective action request.

### 11.5 Equipment failures

If a regulator, cylinder, cradle or station PC fault is suspected, stop using the station, tag it "OUT OF SERVICE", and raise a Beacon ticket under the category MFG-OT for station PC and Service Tool faults. Stations are verified every day at the start of the first shift by calibrating a reference unit and comparing the result with its previous record.

## 12. Customer guidance (reference only)

The following guidance is printed in the Halcyon user manuals and on the calibration certificate. It is repeated here so that repair technicians give consistent answers to customers. Customer field calibration follows the product calibration procedures, not this document.

- The calibration interval is 180 days for the HX-200, 120 days for the HX-210 and 150 days for the HX-220. The unit displays a warning when calibration is due and can be configured to lock out when overdue.
- A bump test should be done before each day's use. A bump test exposes the sensors to gas above the alarm thresholds and confirms the alarms activate. It is not a calibration and does not adjust the sensors.
- Customers calibrating their own HX-210 or HX-220 units must use MI-CAL-Q4-B, not the discontinued MI-CAL-Q4-A. Units returned to Meridian for calibration are handled under the Rotterdam RMA process.

## 13. Records

- Calibration certificate form MFG-F-207 is generated for every unit that passes. The electronic certificate is held in the Atlas quality module and retained 10 years from the calibration date.
- The Service Tool session log (session header, room conditions, as-found and as-left readings, T90 results, HX-220 functional check results, operator ID and station ID) is attached to the certificate record in Atlas.
- Drift flags, sensor replacements and reason codes are recorded against the sensor serial number in the Atlas quality module.
- Cylinder certificates of analysis are held by Goods Inward and linked to the cylinder lot number, which the Service Tool records for each calibration.

## 14. Training

Only operators trained and signed off on this procedure may calibrate Halcyon units. Sign-off is recorded in the training matrix and must be renewed every 2 years, or sooner if this procedure changes materially.

## 15. Revision history

| Revision | Date | Change | Author |
|---|---|---|---|
| A | 2022-06-14 | First release for HX-200 and HX-210 | Quality Engineering |
| B | 2023-04-03 | Added HX-220 with Bluetooth LE, GPS and man-down functional checks | Quality Engineering |
| C | 2024-02-19 | Added Rotterdam stations CAL-RTM-01 and CAL-RTM-02; certificate retention set to 10 years | Quality Engineering |
| D | 2025-06-16 | Moved HX-210 and HX-220 to MI-CAL-Q4-B at 1.0 L/min and withdrew MI-CAL-Q4-A; stabilisation raised to 120 seconds; firmware minimums 3.4.1 (HX-210) and 3.4.2 (HX-220); per-model calibration due dates; drift flag rule in step 14; CAL-PEN-06 reserved for outgoing audit | Quality Engineering |
