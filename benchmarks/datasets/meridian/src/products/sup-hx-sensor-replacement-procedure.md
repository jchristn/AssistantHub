# Halcyon Sensor Replacement Procedure

Document ID: sup-hx-sensor-replacement-procedure | Revision: 3 | Effective: 2025-03-10 | Owner: Customer Support Technical Services, Leeds

## 1. Scope

This procedure describes how to replace the gas sensors in Halcyon HX-200, HX-210 and HX-220 portable gas detectors. It is intended for customer maintenance technicians who have completed Meridian Halcyon service training, and for Meridian field service engineers. Replacing sensors does not require the instrument to be returned under RMA.

Replacement sensors:

- HX-SNS-H2S-02: H2S sensor (HX-200, HX-210, HX-220)
- HX-SNS-CO-03: CO sensor (HX-210, HX-220)
- HX-SNS-LEL-04: catalytic LEL sensor (HX-210, HX-220)
- HX-SNS-O2-01: O2 sensor (HX-210, HX-220)

## 2. Safety and preparation

- Work only in a safe (non-hazardous) area, on an antistatic mat, with the instrument switched off.
- Wear an antistatic wrist strap.
- Have a new rear housing gasket (HX-GSK-200, HX-GSK-210 or HX-GSK-220) ready. The gasket must be replaced every time the housing is opened for sensor replacement.
- Tools: Torx T10 driver (T8 for the HX-200), torque screwdriver adjustable from 0.2 to 1.0 N m, plastic spudger.
- Check the new sensor's date code. Do not fit a sensor that has been stored for more than 6 months from its date code.

## 3. Removing the rear housing

1. Remove the belt clip.
2. Undo the rear housing screws: four Torx T8 screws on the HX-200, six Torx T10 screws on the HX-210, eight Torx T10 screws on the HX-220.
3. Lift the rear housing and disconnect the battery connector.
4. Remove and discard the old gasket.

## 4. Replacing the LEL sensor (HX-SNS-LEL-04)

The LEL sensor is fitted only in the HX-210 and HX-220. Replace it when the instrument shows E-117 (LEL sensor poisoned or bridge fault), when the sensor life shown at Menu > Instrument > Service > Sensor Life is below 20%, or when E-104 recurs on the LEL channel after a correct calibration.

1. Locate the LEL sensor in the top-right position of the sensor board, marked "LEL".
2. Remove the two Torx T6 screws holding the sensor retaining bracket.
3. Lift the sensor straight out of its socket using the spudger under the flange. Do not rock it; the pins bend easily.
4. Inspect the socket for corrosion. Clean with a dry brush only; do not use solvent.
5. Insert the new HX-SNS-LEL-04 with the locating tab facing the display.
6. Refit the retaining bracket and tighten the two Torx T6 screws to 0.25 N m.
7. Reconnect the battery and refit the rear housing with a new gasket. Tighten the rear housing screws to 0.45 N m in a diagonal pattern.
8. Switch on the instrument. It detects the new sensor and shows "NEW SENSOR". Confirm with ON/OK.
9. Leave the instrument switched on in clean air for a warm-up period of 4 hours before calibrating. The catalytic bead needs this time to stabilise; calibrating earlier gives a span that drifts by up to 10% LEL in the first day.
10. Perform a full calibration (all channels), following prod-hx210-calibration-procedure-v2 for the HX-210 or the HX-220 user manual for the HX-220.
11. Perform a bump test before returning the instrument to service.

A poisoned LEL sensor must not be recalibrated. If E-117 recurs within 30 days of replacement, investigate the site for poisons such as silicone sealants, silicone-based lubricants or high H2S concentrations.

## 5. Replacing electrochemical sensors (O2, CO, H2S)

1. Locate the sensor (O2 top left, CO bottom left, H2S bottom right on the HX-210 and HX-220; the HX-200 has a single central socket).
2. Pull the sensor straight out of its socket.
3. Remove the shorting spring from the new sensor, if fitted. CO and H2S sensors are shipped with a shorting spring; O2 sensors are not.
4. Insert the new sensor with the locating tab facing the display.
5. Refit the rear housing with a new gasket. Tighten the screws to 0.45 N m (HX-210 and HX-220) or 0.35 N m (HX-200).
6. Switch on and confirm "NEW SENSOR".
7. Allow a warm-up period of 1 hour for CO and H2S sensors, or 30 minutes for the O2 sensor, before calibrating.
8. Calibrate all channels and perform a bump test.

## 6. Records and disposal

Record the old and new sensor serial numbers in Meridian Link (Instruments > [serial] > Service History). Electrochemical sensors contain small amounts of acid electrolyte and lead (O2). Dispose of them as hazardous waste in accordance with local regulations. Sensors that fail within warranty should be kept for 30 days in case the warranty claim requires return.
