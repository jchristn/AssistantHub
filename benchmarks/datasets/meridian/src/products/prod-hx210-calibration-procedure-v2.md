# HX-210 Calibration Procedure

Document ID: prod-hx210-calibration-procedure-v2 | Version: 2 | Effective: 2025-05-19 | Supersedes: prod-hx210-calibration-procedure-v1 (2024-03-12) | Owner: Halcyon Product Engineering, Leeds | Controlled document QP-HX-017

## 1. Purpose and scope

This procedure describes how to calibrate the Halcyon HX-210 four-gas detector manually, using a calibration cap and a fixed-flow regulator. It applies to HX-210 instruments running firmware 3.4.1 or later. Calibration in the HX-DOCK-4 docking station follows the same gas and flow parameters and is performed automatically by the dock, which must run dock firmware D-2.1.0 or later.

Calibration adjusts the zero and the span (sensitivity) of each of the four sensors: O2 (HX-SNS-O2-01), LEL (HX-SNS-LEL-04), CO (HX-SNS-CO-03) and H2S (HX-SNS-H2S-02).

## 2. Changes from version 1

- The calibration interval is reduced from 180 days to 120 days.
- The calibration gas changes from MI-CAL-Q4-A to MI-CAL-Q4-B. The methane concentration falls from 2.5% vol to 1.1% vol, CO from 100 ppm to 50 ppm and H2S from 25 ppm to 15 ppm. O2 is unchanged at 18.0% vol.
- The %LEL basis for methane changes from 5.0% vol to 4.4% vol, in line with IEC 60079-20-1. 1.1% vol CH4 is therefore 25% LEL.
- The flow rate increases from 0.5 L/min (MI-REG-05) to 1.0 L/min (MI-REG-10).
- The span stabilisation wait increases from 90 seconds to 120 seconds.
- Minimum instrument firmware 3.4.1 and minimum dock firmware D-2.1.0 are now required.
- A new section 5 describes how to resolve a failed span (E-104).

The changes follow a 2024 field study of 1,900 HX-210 instruments, which showed LEL sensitivity drift of up to 18% between 120 and 180 days and better linearity when spanning close to the 20% LEL high alarm. MI-CAL-Q4-A is discontinued and must not be used after 30 September 2025.

## 3. Calibration interval

The HX-210 must be calibrated at least every 120 days. It must also be calibrated after any sensor is replaced, after a failed bump test, after an over-range reading, and after a drop from more than 2 m. From firmware 3.4.1 the instrument will not accept a value above 120 days in Menu > Supervisor > Calibration > Interval.

## 4. Equipment and procedure

Equipment:

- Calibration gas MI-CAL-Q4-B, quad-gas mixture: 1.1% vol methane (CH4), equivalent to 25% LEL; 18.0% vol oxygen; 50 ppm carbon monoxide; 15 ppm hydrogen sulphide; balance nitrogen.
- Fixed-flow regulator MI-REG-10, 1.0 L/min.
- Calibration cap HX-CAP-210 with no more than 1 m of tubing.
- A clean-air area free of contaminants.

Procedure:

1. Switch on the instrument in clean air and allow it to warm up for at least 5 minutes.
2. Enter the supervisor menu and select Menu > Supervisor > Calibration > Calibrate All.
3. The instrument performs a fresh-air zero. Wait until "ZERO OK" is displayed for all four sensors.
4. When prompted "APPLY GAS", fit the calibration cap and open the MI-REG-10 regulator. Gas flows at 1.0 L/min.
5. Confirm the cylinder values shown on screen match the MI-CAL-Q4-B certificate. Firmware 3.4.1 pre-loads the MI-CAL-Q4-B values.
6. Wait for the span stabilisation period of 120 seconds.
7. When "SPAN OK" is displayed for all sensors, close the regulator and remove the cap.
8. Allow readings to return to fresh-air values before returning the instrument to service.
9. Record the calibration, or download the certificate using Meridian Link.

## 5. Failed span and error E-104

If any sensor cannot be spanned, the instrument displays E-104 (span calibration failed) with the affected channel, and the next-calibration countdown is not reset. Work through these checks in order:

1. Cylinder: confirm it is MI-CAL-Q4-B, not the discontinued MI-CAL-Q4-A. Using MI-CAL-Q4-A with firmware 3.4.1 is the most common cause of E-104 on the LEL channel.
2. Expiry and pressure: the cylinder must be in date and hold at least 10 bar.
3. Flow: confirm the regulator is MI-REG-10 (1.0 L/min). A 0.5 L/min regulator gives a low span reading.
4. Membrane: replace the dust membrane HX-MEM-210 if dirty.
5. Repeat the calibration once, allowing the full 120-second wait.
6. If E-104 persists on the same channel, check the sensor life at Menu > Instrument > Service > Sensor Life. Replace the sensor using sup-hx-sensor-replacement-procedure if remaining life is below 20% or E-104 recurs.

If the LEL channel shows E-117 rather than E-104, the sensor is poisoned. Do not recalibrate; replace it.

## 6. Records

Keep calibration records for at least 3 years, showing serial number, date, cylinder lot number, technician and the result for each sensor.
