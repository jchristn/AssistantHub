# HX-210 Calibration Procedure

Document ID: prod-hx210-calibration-procedure-v1 | Version: 1 | Effective: 2024-03-12 | Owner: Halcyon Product Engineering, Leeds | Controlled document QP-HX-017

## 1. Purpose and scope

This procedure describes how to calibrate the Halcyon HX-210 four-gas detector manually, using a calibration cap and a fixed-flow regulator. It applies to all HX-210 instruments regardless of firmware version. Calibration in the HX-DOCK-4 docking station follows the same gas and flow parameters and is performed automatically by the dock.

Calibration adjusts the zero and the span (sensitivity) of each of the four sensors: O2 (HX-SNS-O2-01), LEL (HX-SNS-LEL-04), CO (HX-SNS-CO-03) and H2S (HX-SNS-H2S-02).

## 2. Calibration interval

The HX-210 must be calibrated at least every 180 days. It must also be calibrated:

- after any sensor is replaced;
- after a failed bump test;
- after exposure to a high concentration of gas that caused an over-range reading;
- after the instrument has been dropped from a height of more than 2 m.

The calibration interval is set in Menu > Supervisor > Calibration > Interval. The default and maximum permitted value is 180 days.

## 3. Equipment

- Calibration gas MI-CAL-Q4-A, quad-gas mixture: 2.5% vol methane (CH4), equivalent to 50% LEL on the basis of a methane LEL of 5.0% vol; 18.0% vol oxygen; 100 ppm carbon monoxide; 25 ppm hydrogen sulphide; balance nitrogen.
- Fixed-flow regulator MI-REG-05, 0.5 L/min.
- Calibration cap HX-CAP-210 with 1 m of tubing.
- Clean air supply, or a clean-air area free of contaminants.

Check that the cylinder is within its expiry date and that its certificate matches the values above. Do not use cylinders with less than 10 bar remaining.

## 4. Procedure

1. Switch on the instrument in clean air and allow it to warm up for at least 5 minutes.
2. Enter the supervisor menu and select Menu > Supervisor > Calibration > Calibrate All.
3. The instrument performs a fresh-air zero. Wait until "ZERO OK" is displayed for all four sensors.
4. When prompted "APPLY GAS", fit the calibration cap and open the MI-REG-05 regulator. Gas flows at 0.5 L/min.
5. Confirm the cylinder values shown on screen match the MI-CAL-Q4-A certificate. Adjust them with UP and DOWN if necessary.
6. Wait for the span stabilisation period of 90 seconds. The instrument then adjusts the span of each sensor.
7. When "SPAN OK" is displayed for all sensors, close the regulator and remove the cap.
8. Allow readings to return to fresh-air values before returning the instrument to service.
9. Record the calibration in the site calibration log, or download the calibration certificate using Meridian Link.

## 5. Failed calibration

If any sensor cannot be spanned, the instrument displays E-104 (span calibration failed) and the next-calibration countdown is not reset. Check the cylinder concentration, expiry date and regulator flow, then repeat the calibration once. If E-104 persists, replace the affected sensor.

## 6. Records

Keep calibration records for at least 3 years. Records must show the instrument serial number, date, cylinder lot number, the person performing the calibration and the result for each sensor.

## 7. Calibration in the HX-DOCK-4

When calibrating in the HX-DOCK-4, connect the MI-CAL-Q4-A cylinder to the dock GAS IN port with the demand-flow regulator MI-REG-DF. The dock controls the flow internally at 0.5 L/min and applies the same 90-second stabilisation period. The dock records the result and the cylinder lot number automatically and synchronises them to Meridian Link. Any dock firmware version can be used with this procedure.

## 8. Related documents

- Halcyon HX-210 User Manual (prod-hx210-user-manual)
- Halcyon Error Code Reference (sup-halcyon-error-codes)
- Halcyon Sensor Replacement Procedure (sup-hx-sensor-replacement-procedure)
