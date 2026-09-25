# Halcyon HX-220 User Manual

Document ID: prod-hx220-user-manual | Revision: C | Effective: 2025-09-01 | Owner: Halcyon Product Engineering, Leeds | Applies to firmware 3.4.2

## 1. Safety information

Read this manual in full before using the Halcyon HX-220 portable four-gas detector. The HX-220 is personal protective equipment. It warns the wearer of hazardous concentrations of oxygen deficiency or enrichment, flammable gas, carbon monoxide (CO) and hydrogen sulphide (H2S). It does not remove any hazard, and it cannot warn of gases for which it has no sensor fitted.

The HX-220 is certified intrinsically safe with the marking II 2G Ex ib IIC T4 Gb. It may be used in Zone 1 and Zone 2 hazardous areas for gas groups IIA, IIB and IIC with temperature class T4. It must not be used in Zone 0. The ATEX certificate is Baseefa23ATEX0031X and the IECEx certificate is IECEx BAS 23.0017X. The lower protection level (ib rather than the ia of the HX-210) is a consequence of the larger 5,200 mAh battery and the radio module.

- Charge the instrument only in a safe (non-hazardous) area, using the HX-DOCK-4 docking station or the supplied single-unit charger.
- Replace the battery pack HX-BAT-220L only in a safe area. Do not use any other battery pack.
- Do not open the housing in a hazardous area.
- Do not rub or clean the front label with a dry cloth in a hazardous area.
- Substitution of components may impair intrinsic safety.
- Perform a bump test before each day's use. Calibrate at least every 150 days, or sooner if a bump test fails.
- High off-scale readings of flammable gas may indicate an explosive concentration. Leave the area immediately.
- Silicones, leaded petrol vapours, sulphur compounds and halogenated hydrocarbons can poison the catalytic LEL sensor.
- The man-down and GPS functions are aids to lone-worker safety. They do not replace a site lone-working procedure.

## 2. Product overview

The Halcyon HX-220 is the connected member of the Halcyon family. It continuously monitors four gases and adds Bluetooth LE 5.0, GPS location, a man-down (no-motion) alarm and support for the optional motorised sampling pump HX-PMP-22. It shares its sensor platform with the HX-210, but the H2S, CO and O2 ranges differ.

The HX-220 is fitted with four sensors:

- Oxygen (O2): electrochemical, part number HX-SNS-O2-01, range 0-25% vol, resolution 0.1% vol.
- Flammable gas (LEL): catalytic bead, part number HX-SNS-LEL-04, range 0-100% LEL, resolution 1% LEL, calibrated to methane (CH4).
- Carbon monoxide (CO): electrochemical, part number HX-SNS-CO-03, range 0-1000 ppm, resolution 1 ppm.
- Hydrogen sulphide (H2S): electrochemical, part number HX-SNS-H2S-02, range 0-100 ppm, resolution 0.1 ppm.

The housing is rated IP67 and weighs 365 g including the battery (without pump). Dimensions are 131 x 70 x 44 mm. The operating temperature range is -40 to +55 C, the widest of the Halcyon family, thanks to the low-temperature cell chemistry of the HX-BAT-220L.

## 3. Package contents

The HX-220 starter kit, part number HX-220-KIT-05, contains:

- 1 x HX-220 detector with four sensors fitted and calibrated at the factory
- 1 x HX-BAT-220L rechargeable Li-ion battery pack, 5,200 mAh (fitted)
- 1 x single-unit USB-C charger with plug adapters
- 1 x calibration cap HX-CAP-220 with 1 m hose
- 1 x stainless steel belt clip and 1 x chest harness clip
- 1 x hard carry case
- 1 x Meridian Link Mobile licence card (Bluetooth app activation)
- 1 x printed quick-start guide (see also prod-hx220-quick-start)
- 1 x factory calibration certificate

## 4. Controls and indicators

The HX-220 has one large push button on the front (ON/OK) and two smaller buttons below the display (UP and DOWN), identical in function to the HX-210. In addition, a recessed red PANIC button on the top edge triggers a manual man-down alarm when held for 2 seconds.

- Two red high-intensity LEDs on the top edge flash during alarms.
- A green LED flashes once every 10 seconds as the confidence signal.
- A yellow LED lights when the calibration or bump test is overdue.
- A blue LED flashes when Bluetooth is connected to a paired phone or gateway.
- The sounder produces 103 dB at 30 cm, louder than the 95 dB of the HX-200 and HX-210, for use in noisy plant.
- A vibration motor operates in parallel with the sounder.

## 5. Charging and power-on

Charge the HX-220 fully before first use. A full charge from empty takes approximately 6.5 hours at 20 C. A fully charged HX-BAT-220L pack gives a typical runtime of 22 hours at 20 C with Bluetooth and GPS enabled and no alarms. With the HX-PMP-22 pump fitted, runtime falls to approximately 15 hours. Charge only between 0 and +40 C.

Power-on sequence:

1. Press and hold ON/OK for 3 seconds. All LEDs light, the sounder beeps and the motor vibrates.
2. The display shows the firmware version (3.4.2 for instruments shipped from September 2025), the serial number and the instrument name.
3. The display shows the alarm set points for each gas in turn.
4. The display shows the days remaining until the next calibration and the next bump test.
5. The GPS icon flashes until a position fix is obtained (typically 30 to 90 seconds outdoors).
6. The sensors warm up for 45 seconds, then the instrument enters measuring mode.

## 6. Display and navigation

The measuring screen layout and user menu are the same as on the HX-210, with two additional items:

- Menu > Wireless: switch Bluetooth on or off, pair a phone, and view the paired device.
- Menu > Location: show the current GPS position and fix quality.

The menu closes automatically after 30 seconds without a button press. The instrument continues to monitor gas and will alarm while any menu is open.

## 7. Alarms

Factory default set points for the HX-220:

| Gas | Low alarm | High alarm | STEL | TWA |
|---|---|---|---|---|
| H2S | 5 ppm | 15 ppm | 15 ppm | 5 ppm |
| CO | 35 ppm | 200 ppm | 200 ppm | 30 ppm |
| LEL (CH4) | 10% LEL | 25% LEL | n/a | n/a |
| O2 | 19.5% vol (falling) | 23.5% vol (rising) | n/a | n/a |

Note that the HX-220 defaults differ from the HX-210 (H2S high 10 ppm, CO low 30 ppm and high 100 ppm, LEL high 20% LEL, O2 high 23.0% vol). Sites running mixed fleets should align set points in Meridian Link to avoid confusion.

Low, high, STEL, TWA and over-range alarms behave as described for the HX-210: low alarms are non-latching by default, high alarms latch until acknowledged in clean air, STEL and TWA alarms cannot be acknowledged.

When Bluetooth is connected, every gas alarm, man-down alarm and panic alarm is sent to the paired Meridian Link Mobile app, together with the GPS position, within 5 seconds.

## 8. Man-down and panic alarms

The man-down function detects that the wearer has stopped moving.

1. If the instrument detects no motion for the man-down timer (default 60 seconds), it enters a pre-alarm: a slow beep and "MOVE?" on the display.
2. The pre-alarm lasts 15 seconds. Any movement or a press of ON/OK cancels it.
3. If not cancelled, a full man-down alarm sounds and is transmitted with the GPS position.

The man-down timer can be set between 30 and 180 seconds in Menu > Supervisor > Lone Worker > No-Motion Time. The pre-alarm period is fixed at 15 seconds. Man-down can be disabled only by the supervisor.

The panic alarm is triggered by holding the red PANIC button for 2 seconds. It is always latching.

## 9. Bluetooth and GPS

The HX-220 pairs with the Meridian Link Mobile app on Android 11 or later and iOS 16 or later. To pair, select Menu > Wireless > Pair, then scan the six-digit code shown on the display from the app. The instrument remembers one paired phone at a time.

GPS positions are logged every 60 seconds in the datalog. Indoors, where no fix is available, the last known position is sent with an alarm and marked as stale.

If the Bluetooth module fails its self-test the instrument shows E-131 (Bluetooth module fault). Gas detection continues normally. Error E-131 applies to the HX-220 only.

## 10. Optional sampling pump HX-PMP-22

The HX-PMP-22 clips to the front of the HX-220 over the sensor grille and draws a sample at 0.5 L/min. It allows pre-entry testing of confined spaces with sample tubing up to 30 m long. Allow 2 seconds per metre of tubing plus 60 seconds for the reading to stabilise.

The pump is detected automatically. If flow falls below 0.3 L/min (for example if the inlet is blocked or the tube is kinked) the instrument shows E-113 (pump flow fault) and sounds the fault tone. Clear the blockage and press ON/OK. Test the pump block alarm daily by covering the inlet with a finger. The pump is not compatible with the HX-200 or HX-210.

## 11. Bump testing and calibration

Bump test the HX-220 before each day's use, using MI-CAL-Q4-B at 1.0 L/min through the MI-REG-10 regulator, or automatically in the HX-DOCK-4. The bump test passes if each sensor reaches at least 80% of the cylinder concentration within 45 seconds and the alarms operate.

The HX-220 must be calibrated at least every 150 days. It uses the same MI-CAL-Q4-B gas (1.1% vol CH4, 18.0% O2, 50 ppm CO, 15 ppm H2S), flow rate and 120-second span stabilisation wait as the HX-210 calibration procedure (prod-hx210-calibration-procedure-v2), except that the calibration interval is 150 days rather than 120 days. Calibrate with the pump removed. Docking-station calibration requires dock firmware D-2.1.0 or later.

## 12. Datalogging

The HX-220 logs readings from all four sensors and the GPS position at a 1-minute interval by default. At a 1-minute interval the datalog holds approximately 180 days, twice the HX-210 capacity. The event log records the last 1,000 events. Download logs with the HX-DOCK-4 or Meridian Link 4.2 or later.

## 13. Maintenance

Clean with a damp cloth and mild detergent only. Replace the dust membrane HX-MEM-220 every 6 months. Check remaining sensor life at Menu > Instrument > Service > Sensor Life, as on the HX-210.

The rear housing of the HX-220 is held by eight captive Torx T10 screws, tightened in a diagonal pattern to 0.45 N m. The rear gasket is HX-GSK-220 and is not interchangeable with the HX-210 gasket.

## 14. Supervisor settings

The factory default supervisor passcode for the HX-220 is 2207. Change it on commissioning using Menu > Supervisor > Security > Change Passcode. After five incorrect entries the supervisor menu is locked for 10 minutes. The calibration interval can be set between 30 and 150 days.

## 15. Troubleshooting

Error codes are listed in the Halcyon Error Code Reference (sup-halcyon-error-codes). Codes specific to the HX-220 are E-113 (pump flow fault) and E-131 (Bluetooth module fault). Firmware 3.4.2 is an HX-220-only maintenance build; do not attempt to load it on an HX-210.

## 16. Technical specifications

| Parameter | Value |
|---|---|
| Model | Halcyon HX-220 connected four-gas detector |
| Gases and ranges | O2 0-25% vol; LEL 0-100% LEL (CH4); CO 0-1000 ppm; H2S 0-100 ppm |
| Resolution | O2 0.1% vol; LEL 1% LEL; CO 1 ppm; H2S 0.1 ppm |
| Sampling | Diffusion; optional HX-PMP-22 pump, 0.5 L/min, up to 30 m tubing |
| Battery | HX-BAT-220L, 5,200 mAh Li-ion |
| Runtime | 22 h typical at 20 C (approx. 15 h with pump) |
| Charge time | 6.5 h |
| Calibration interval | 150 days maximum |
| Certification | ATEX II 2G Ex ib IIC T4 Gb (Baseefa23ATEX0031X); IECEx BAS 23.0017X |
| Hazardous area | Zone 1 and 2 (not Zone 0) |
| Ingress protection | IP67 |
| Operating temperature | -40 to +55 C |
| Audible alarm | 103 dB at 30 cm |
| Wireless | Bluetooth LE 5.0; GPS |
| Man-down | No-motion timer 60 s default (30-180 s), 15 s pre-alarm |
| Datalog | 180 days at 1-minute interval; 1,000-event log |
| Dimensions | 131 x 70 x 44 mm |
| Weight | 365 g |
| Firmware | 3.4.2 |
| Starter kit | HX-220-KIT-05 |
