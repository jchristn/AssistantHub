# Halcyon HX-200 User Manual

Document ID: prod-hx200-user-manual | Revision: D | Effective: 2025-01-20 | Owner: Halcyon Product Engineering, Leeds | Applies to firmware 3.2.4

## 1. Safety information

Read this manual in full before using the Halcyon HX-200 portable single-gas detector. The HX-200 is personal protective equipment. It warns the wearer of hazardous concentrations of hydrogen sulphide (H2S). It does not remove any hazard, and it cannot warn of any other gas.

The HX-200 is certified intrinsically safe with the marking II 2G Ex ib IIB T4 Gb. It may be used in Zone 1 and Zone 2 hazardous areas for gas groups IIA and IIB only, with temperature class T4. It must not be used in Zone 0 or in gas group IIC atmospheres such as hydrogen or acetylene. The ATEX certificate is Baseefa21ATEX0147X and the IECEx certificate is IECEx BAS 21.0063X.

- Charge the instrument only in a safe (non-hazardous) area.
- Replace the battery pack HX-BAT-200 only in a safe area. Do not use any other battery pack.
- Do not open the housing in a hazardous area.
- Substitution of components may impair intrinsic safety.
- Perform a bump test before each day's use. Calibrate at least every 180 days, or sooner if a bump test fails.

## 2. Product overview

The Halcyon HX-200 is the single-gas member of the Halcyon family. It continuously monitors H2S with an electrochemical sensor, part number HX-SNS-H2S-02, over a range of 0-100 ppm with a resolution of 0.1 ppm. The four-gas HX-210 and the connected HX-220 share the same sensor, controls and menu structure.

The housing is rated IP66 and weighs 210 g including the battery. Dimensions are 98 x 55 x 32 mm. The operating temperature range is -20 to +50 C.

## 3. Package contents

The HX-200 starter kit, part number HX-200-KIT-01, contains the HX-200 detector with H2S sensor fitted and calibrated, the HX-BAT-200 rechargeable Li-ion battery pack (2,600 mAh, fitted), a single-unit USB-C charger, calibration cap HX-CAP-200 with hose, a belt clip, a printed quick-start guide and a factory calibration certificate.

## 4. Controls and indicators

The HX-200 has one large push button on the front (ON/OK) and two smaller buttons below the display (UP and DOWN).

- ON/OK: press and hold for 3 seconds to switch on; press and hold for 5 seconds to switch off.
- UP: press and hold in measuring mode to display the peak reading.
- DOWN: press and hold in measuring mode to display STEL and TWA values.

Two red LEDs flash during alarms, a green LED flashes every 10 seconds as the confidence signal, and a yellow LED lights when calibration or bump test is overdue. The sounder produces 95 dB at 30 cm.

## 5. Charging and power-on

A full charge takes approximately 4 hours at 20 C. A fully charged HX-BAT-200 gives a typical runtime of 36 hours at 20 C, the longest of the Halcyon family because only one sensor is powered. Charge only between 0 and +40 C.

The HX-200 does not fit the HX-DOCK-4 directly. To use the docking station, fit the HX-200 into the HX-ADP-200 adapter first.

On power-on the instrument tests the LEDs, sounder and vibration motor, displays the firmware version (3.2.4), the alarm set points and the days until calibration and bump test are due, then warms up for 30 seconds before entering measuring mode.

## 6. Alarms

Factory default set points for the HX-200:

| Alarm | Set point |
|---|---|
| Low | 5 ppm H2S |
| High | 10 ppm H2S |
| STEL (15 min) | 15 ppm H2S |
| TWA (8 h) | 5 ppm H2S |

The low alarm is non-latching by default. The high alarm latches until acknowledged with ON/OK in clean air. STEL and TWA alarms cannot be acknowledged until the instrument is switched off. Set points can be changed only in the supervisor menu (Menu > Supervisor > Alarms > Set Points) or with Meridian Link.

## 7. Bump testing and calibration

Bump test the HX-200 before each day's use. Fit the calibration cap and apply MI-CAL-H2S25 (25 ppm H2S in nitrogen) at 0.5 L/min using the MI-REG-05 regulator. The bump test passes if the reading reaches at least 20 ppm within 45 seconds and the alarms operate.

The HX-200 must be calibrated at least every 180 days. Unlike the HX-210, the HX-200 interval was not changed in 2025; it remains 180 days. Calibration uses MI-CAL-H2S25 at 0.5 L/min with a span stabilisation wait of 90 seconds. Select Menu > Calibrate, perform the fresh-air zero, then apply gas when prompted. If the span fails, the instrument shows E-104.

## 8. Datalogging

The HX-200 logs H2S readings at a 1-minute interval. At that interval the datalog holds approximately 60 days. The event log holds the last 250 events. Logs are downloaded with Meridian Link or the HX-DOCK-4 (with HX-ADP-200).

## 9. Maintenance

Clean with a damp cloth and mild detergent only. Replace the dust membrane HX-MEM-200 every 6 months. Check remaining sensor life at Menu > Instrument > Service > Sensor Life.

The rear housing of the HX-200 is held by four Torx T8 screws. Tighten them to 0.35 N m when refitting. The rear gasket is HX-GSK-200.

## 10. Supervisor settings

The factory default supervisor passcode for the HX-200 is 2007. Change it on commissioning using Menu > Supervisor > Security > Change Passcode. After five incorrect entries the supervisor menu is locked for 10 minutes. The calibration interval can be set between 30 and 180 days.

## 11. Troubleshooting

Error codes are listed in the Halcyon Error Code Reference (sup-halcyon-error-codes). Codes that can occur on the HX-200 include E-101, E-104, E-108, E-111, E-122, E-126 and E-140. Codes E-113, E-117 and E-131 do not apply to the HX-200 because it has no pump, no LEL sensor and no Bluetooth module.

## 12. Technical specifications

| Parameter | Value |
|---|---|
| Gas and range | H2S 0-100 ppm, resolution 0.1 ppm |
| Sensor | HX-SNS-H2S-02 electrochemical |
| Battery | HX-BAT-200, 2,600 mAh Li-ion |
| Runtime | 36 h typical at 20 C |
| Charge time | 4 h |
| Calibration interval | 180 days maximum |
| Calibration gas | MI-CAL-H2S25 at 0.5 L/min (MI-REG-05) |
| Certification | ATEX II 2G Ex ib IIB T4 Gb (Baseefa21ATEX0147X); IECEx BAS 21.0063X |
| Hazardous area | Zone 1 and 2, gas groups IIA and IIB |
| Ingress protection | IP66 |
| Operating temperature | -20 to +50 C |
| Audible alarm | 95 dB at 30 cm |
| Datalog | 60 days at 1-minute interval |
| Dimensions | 98 x 55 x 32 mm |
| Weight | 210 g |
| Firmware | 3.2.4 |
| Starter kit | HX-200-KIT-01 |

## 13. Storage and disposal

Store the HX-200 switched off in a clean, dry area between 0 and +30 C. For storage longer than 30 days, charge the HX-BAT-200 to between 40% and 60% and recharge it every 3 months. The calibration countdown continues during storage; an instrument stored for more than 180 days must be calibrated before use.

At the end of its life, return the HX-200 to Meridian or to an approved WEEE collection point. Remove the battery pack and dispose of it separately. The H2S sensor contains acid electrolyte and must be treated as hazardous waste.

## 14. Document history

| Revision | Date | Changes |
|---|---|---|
| C | 2024-05-06 | Updated for firmware 3.2.3 |
| D | 2025-01-20 | Updated for firmware 3.2.4: 250-event log, improved low-temperature battery gauge |
