# Lumen LF-60 Installation Manual

Document ID: prod-lf60-installation-manual | Version: 1 | Effective: 2024-10-07 | Owner: Lumen Engineering, Rotterdam | Applies to firmware: 1.8.2

## 1. About this manual

This manual describes how to install, wire, ground, bolt and commission the Lumen LF-60 optical flow meter. It applies to all LF-60 meters from DN25 to DN200 with PN16 flanges. It is written for qualified mechanical and instrumentation technicians.

The LF-60 shares its measuring principle and much of its installation procedure with the high-pressure LF-60P. Do not use the LF-60P Installation Manual for an LF-60: straight-run lengths, bolt torques and commissioning steps differ. Menu paths on the local display are written with the greater-than sign between levels, for example Menu > Setup > Outputs.

## 2. Safety

Installation and removal must only be carried out when the pipe section has been isolated, depressurised and drained, and, where the fluid is hot, cooled to below 40 °C. Never loosen flange bolts on a pressurised line.

- Follow the site permit-to-work system and lock-out/tag-out rules before starting.
- The meter weighs 6.2 kg at DN50 and up to 41 kg at DN200. Use lifting straps around the meter body, never around the electronics housing.
- The LF-60 is not certified for hazardous areas.
- The 24 VDC supply must be a SELV/PELV source with a current limit not exceeding 2 A.

## 3. Product identification

The nameplate on the measuring tube shows the order code, serial number, nominal size, pressure rating and maximum fluid temperature. Order codes follow the pattern LF60-{size}-PN16. The serial number format is LS followed by two digits for the year and six digits, for example LS24-011583. Check that the PN16 (16 bar) rating is equal to or greater than the design pressure of the line and that the fluid will not exceed +80 °C.

## 4. Unpacking and inspection

Each LF-60 is shipped with protective flange covers, two M20 x 1.5 cable glands, one blanking plug, a terminal compartment key (4 mm hex), a quick reference card and a calibration certificate. Keep the flange covers in place until the meter is lifted into position, and inspect the sapphire windows through the bore before installation. Clean only with isopropyl alcohol and a lint-free cloth.

## 5. Site selection

The LF-60 measures accurately only when the pipe is full and the flow profile is well developed.

- At least 10 pipe diameters (10D) of straight pipe upstream of the meter.
- At least 5 pipe diameters (5D) of straight pipe downstream.
- Where the upstream disturbance is a control valve or two elbows in different planes, increase the upstream length to 20D.
- The meter must not be at the highest point of the line.
- Ambient temperature at the electronics must stay between -20 and +60 °C.

The LF-60 housing is rated IP65. It is protected against water jets but must not be installed in pits or chambers that can flood. Use the LF-60P for flooded locations.

## 6. Pipe preparation

Flush the line to remove debris before fitting the meter. Check that the mating flanges are EN 1092-1 PN16, parallel within 0.5 mm, and that the bolt holes are aligned. The flange gap must equal the meter face-to-face length plus two gaskets: 200 mm for DN25 to DN50, 250 mm for DN65 to DN100 and 300 mm for DN125 to DN200.

## 7. Mechanical installation and orientation

The arrow on the meter body shows the forward flow direction. The preferred orientation is a horizontal pipe with the electronics housing at the side (3 o'clock or 9 o'clock). Vertical pipe with upward flow is also acceptable. Do not install with the electronics housing directly underneath the pipe.

Use gaskets with an inside diameter no smaller than the pipe bore; a protruding gasket causes L-204 low signal quality warnings. Compressed fibre gaskets are suitable across the full LF-60 temperature range.

## 8. Electrical installation

Use screened twisted-pair instrument cable of 0.5 to 1.5 mm². Close unused cable entries with the blanking plug to maintain IP65.

| Terminal | Function | Notes |
|---|---|---|
| 1 | +24 VDC supply | 24 VDC ±10%, 4 W |
| 2 | 0 V supply | |
| 3 | 4-20 mA + | Active or passive, selected in menu |
| 4 | 4-20 mA - | No HART on the LF-60 |
| 5 | Pulse output + | Open collector, max 30 VDC, 100 mA, max 1 kHz |
| 6 | Pulse output - | |
| 7 | RS-485 A (D+) | Modbus RTU |
| 8 | RS-485 B (D-) | Modbus RTU |
| 9 | Functional earth / screen | |

The RS-485 termination resistor is switched on with DIP switch S1. The LF-60 draws 4 W, around 170 mA at 24 VDC, and starts measuring about 20 seconds after power is applied.

## 9. Grounding and bonding

On bare metallic pipe, connect the grounding lug on each meter flange to the mating pipe flange with the supplied 6 mm² braided straps and connect terminal 9 to plant functional earth. The resistance between the meter body and plant earth must be less than 1 ohm.

On plastic or internally lined pipe, grounding rings are mandatory on both sides of the meter. Use the 316L rings LF-GR-{size}-S (for example LF-GR-050-S for DN50). Grounding rings add 3 mm per side to the installed length.

## 10. Output configuration

The LF-60 has no HART interface. Configure the 4-20 mA range under Menu > Setup > Outputs > Current > Range, where 20 mA corresponds to the upper range value in the selected flow unit. The pulse output value (litres per pulse) is set under Menu > Setup > Outputs > Pulse > Pulse Value. Modbus settings are under Menu > Setup > Outputs > Modbus; the defaults are address 1, 9600 baud, 8N1. Use Menu > Diagnostics > Loop Test to force the current output to 4, 12 and 20 mA.

## 11. Flange bolting

Tighten flange joints in a cross pattern in three passes (30%, 60%, 100% of final torque), followed by a circular check pass at 100%. The values apply to EN 1092-1 PN16 flanges, lightly oiled bolts of property class 8.8 or A4-70 and compressed fibre gaskets. For PTFE gaskets, reduce the values by 20%.

| Meter size | Bolts | Torque |
|---|---|---|
| DN25 | 4 x M12 | 35 N m |
| DN40 | 4 x M16 | 50 N m |
| DN50 | 4 x M16 | 60 N m |
| DN65 | 8 x M16 | 55 N m |
| DN80 | 8 x M16 | 65 N m |
| DN100 | 8 x M16 | 70 N m |
| DN125 | 8 x M16 | 85 N m |
| DN150 | 8 x M20 | 120 N m |
| DN200 | 12 x M20 | 130 N m |

Pressure test the line to 1.5 times the design pressure, but never above 24 bar. Use a calibrated torque wrench and do not use impact tools.

## 12. Zero-point adjustment

A zero-point adjustment is required only if the meter shows a flow reading above 0.5 cm/s with a full, completely stationary pipe. Close valves upstream and downstream, wait 5 minutes, enter the service passcode and go to Menu > Service > Calibration > Zero Adjust. The meter averages the signal for 60 seconds. Offsets larger than ±2.5 cm/s are rejected. The damping time constant defaults to 3.0 seconds and the low-flow cut-off to 0.02 m/s.

## 13. Local display and security

The two-line display shows the flow rate and the forward totaliser. Configuration menus are protected by a four-digit service passcode. The factory default passcode for the LF-60 is 1060. Change it at commissioning under Menu > Service > Security > Change Passcode. Write protection is enforced with DIP switch S3.

## 14. Commissioning checklist and support

- Order code, serial number and PN16 rating recorded
- Straight run confirmed: at least 10D upstream and 5D downstream
- Flow arrow in the correct direction
- Bolts tightened to the section 11 values
- Grounding straps or rings fitted, resistance below 1 ohm
- Supply voltage between 21.6 and 26.4 VDC
- 4-20 mA loop test completed
- Modbus communication confirmed (address 1, 9600 baud, 8N1 by default)
- Service passcode changed from the default

For support, raise a ticket in the Meridian Support Centre at support.meridian-instruments.com and quote the serial number and firmware version (1.8.2 at the time of this manual). Error codes are described in the Lumen Error Code Reference.
