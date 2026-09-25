# Lumen LF-60P Installation Manual

Document ID: prod-lf60p-installation-manual | Version: 1 | Effective: 2025-03-17 | Owner: Lumen Engineering, Rotterdam | Applies to firmware: 1.9.0

## 1. About this manual

This manual describes how to install, wire, ground, bolt and commission the Lumen LF-60P optical flow meter. It applies to all LF-60P meters from DN25 to DN150 with PN40 flanges, including the Hastelloy C-276 variants ordered with the -HC suffix. It is written for qualified mechanical and instrumentation technicians working on pressurised pipework.

The LF-60P shares its measuring principle and much of its installation procedure with the standard-pressure LF-60. Do not use the LF-60 Installation Manual for an LF-60P: straight-run lengths, bolt torques, cable requirements and commissioning steps differ. The installation manual revision that applies to your meter is printed on the label inside the terminal compartment cover.

Throughout this manual, menu paths on the local display are written with the greater-than sign between levels, for example Menu > Setup > Outputs. Terminal numbers refer to the terminal block inside the rear compartment of the electronics housing.

### 1.1 Related documents

- Lumen LF-60P Optical Flow Meter Datasheet (prod-lf60p-datasheet)
- Lumen Modbus Register Map (prod-lumen-modbus-register-map)
- Lumen Error Code Reference (sup-lumen-error-codes)
- Lumen Firmware 1.9.0 Release Notes (prod-lumen-firmware-1-9-0-release-notes)

## 2. Safety

The LF-60P is a pressure-retaining component. Installation and removal must only be carried out when the pipe section has been isolated, depressurised, drained and, where the fluid is hot, cooled to below 40 °C. Never loosen flange bolts or the pressure sensor fitting on a pressurised line.

- Follow the site permit-to-work system and lock-out/tag-out rules before starting.
- Wear eye protection when handling the meter; the sapphire windows are recessed but the measuring tube edges are sharp.
- The meter weighs 9.8 kg at DN50 and up to 38 kg at DN150. Use lifting straps around the meter body, never around the electronics housing or cable glands.
- The LF-60P is not certified for hazardous areas. Do not install it in a location classified as Zone 0, 1 or 2.
- The 24 VDC supply must be a SELV/PELV source with a current limit not exceeding 2 A.

## 3. Product identification

Each meter carries two labels. The nameplate on the measuring tube shows the order code, serial number, nominal size, pressure rating, maximum fluid temperature and wetted material. The electronics label on the housing shows the power requirement, firmware version at the time of shipment and HART device revision.

Order codes follow the pattern LF60P-{size}-PN40 with an optional -HC suffix. The serial number format is LP followed by two digits for the year and six digits, for example LP25-004127. Quote the serial number whenever you contact Meridian support.

Check that the pressure rating on the nameplate (PN40, 40 bar) is equal to or greater than the design pressure of the line, and that the maximum fluid temperature (+120 °C) will not be exceeded.

## 4. Unpacking and inspection

Each LF-60P is shipped in a foam-lined carton with the following items:

- LF-60P meter with protective flange covers fitted
- Two M20 x 1.5 nickel-plated brass cable glands (fitted) and one M20 blanking plug
- Terminal compartment key (4 mm hex)
- Printed quick reference card and calibration certificate
- For -HC variants only: FKM spare seal set, part LF-SEAL-P-FKM

Keep the flange covers in place until the meter is lifted into position. They protect the sapphire windows from dust and knocks. Inspect the windows through the bore before installation: they must be clear, undamaged and free from fingerprints. Clean only with isopropyl alcohol and a lint-free cloth.

Store unused meters indoors between -40 and +70 °C in their original packaging.

## 5. Site selection

The LF-60P measures accurately only when the pipe is full and the flow profile is well developed. Choose a location that meets all of the following conditions:

- At least 15 pipe diameters (15D) of straight pipe of the same nominal size upstream of the meter.
- At least 5 pipe diameters (5D) of straight pipe downstream.
- Where the upstream disturbance is a control valve, a partially open butterfly valve or two elbows in different planes, increase the upstream length to 25D or install a flow conditioner.
- The meter must not be at the highest point of the line, where air collects.
- Avoid installing immediately downstream of a pump discharge; allow at least 20D.
- Ambient temperature at the electronics must stay between -40 and +60 °C. Fit a sunshade where the housing is exposed to direct sun in hot climates.

The IP67 rating allows installation in valve chambers and pits that may flood temporarily, up to 1 m of water for 30 minutes. Where prolonged submersion is likely, use the remote-mount electronics kit LF-RMK-10, which allows the electronics to be mounted up to 10 m away.

## 6. Pipe preparation

Before fitting the meter, flush the line to remove welding slag, scale and debris. Particles larger than 2 mm can scratch the sapphire windows.

Check that the mating flanges are EN 1092-1 PN40 (or ASME Class 300 for meters ordered with ASME flanges), that they are parallel within 0.5 mm across the flange face and that the bolt holes are aligned. Do not use the meter to pull misaligned pipework into position.

The gap between the mating flanges must equal the meter face-to-face length plus the thickness of two gaskets. Face-to-face lengths are 200 mm for DN25 to DN50, 250 mm for DN65 to DN100 and 300 mm for DN125 and DN150.

## 7. Mechanical installation and orientation

The arrow on the meter body shows the forward flow direction. The meter measures reverse flow as well, but accuracy is specified in the forward direction only.

Acceptable orientations:

- Horizontal pipe, with the electronics housing on the side (3 o'clock or 9 o'clock position) so that the optical windows are not at the top or bottom of the pipe. This is the preferred orientation.
- Vertical pipe with upward flow.

Do not install on vertical pipe with downward flow unless there is enough back pressure to keep the pipe full at all times. Do not install with the electronics housing directly underneath the pipe, where leaks from the flanges can drip onto the cable glands.

Lift the meter into position with the flange covers still fitted, remove the covers, insert the gaskets and fit the bolts finger tight. Tighten the bolts as described in section 11.

### 7.1 Gaskets

Use gaskets with an inside diameter no smaller than the pipe bore. A gasket that protrudes into the bore creates turbulence directly upstream of the optical windows and causes L-204 low signal quality warnings. For hot water applications above 80 °C, use graphite-faced spiral-wound or reinforced graphite gaskets. Compressed fibre gaskets are acceptable up to 80 °C.

## 8. Electrical installation

### 8.1 Cable and glands

Use screened twisted-pair instrument cable with a conductor cross-section of 0.5 to 1.5 mm². For the RS-485 bus, use cable with a characteristic impedance of 120 ohm. Terminate the screen at the meter end in the cable gland (360-degree termination) and at the control system end only if the site earthing philosophy requires it.

Unused cable entries must be closed with the supplied M20 blanking plug to maintain IP67.

### 8.2 Terminal assignment

| Terminal | Function | Notes |
|---|---|---|
| 1 | +24 VDC supply | 24 VDC ±10%, 5.5 W |
| 2 | 0 V supply | |
| 3 | 4-20 mA / HART + | Active or passive, selected in menu |
| 4 | 4-20 mA / HART - | |
| 5 | Pulse output + | Open collector, max 30 VDC, 100 mA, max 2 kHz |
| 6 | Pulse output - | |
| 7 | RS-485 A (D+) | Modbus RTU |
| 8 | RS-485 B (D-) | Modbus RTU |
| 9 | Functional earth / screen | |

The RS-485 termination resistor (120 ohm) is switched on with DIP switch S1 on the terminal board. Enable it only on the last device of the bus.

### 8.3 Power

The LF-60P draws 5.5 W at 24 VDC, around 230 mA, plus an inrush of up to 900 mA for 20 ms. Size the supply and fuse accordingly. The meter starts measuring about 25 seconds after power is applied; the display shows the firmware version (1.9.0) during start-up.

## 9. Grounding and bonding

Correct grounding is the single most important factor for stable optical signal processing on the LF-60P. The receivers and the high-speed correlation electronics are sensitive to potential differences between the fluid and the meter body.

### 9.1 Metallic, unlined pipe

On bare carbon steel or stainless steel pipe, connect the grounding lug on each flange of the meter to the mating pipe flange with the supplied 6 mm² braided straps, and connect terminal 9 to the plant functional earth. The resistance between the meter body and the plant earth must be less than 1 ohm.

### 9.2 Non-conductive or lined pipe

On plastic pipe (PVC, PE, PP, GRP) and on metallic pipe with an internal lining (rubber, PTFE, epoxy or cement), grounding rings are mandatory on both sides of the meter. Grounding rings are fitted between the meter flange and the gasket on each side, and each ring is bonded to the meter grounding lug.

| Meter size | Grounding ring part number (316L) | Grounding ring part number (Hastelloy C-276) |
|---|---|---|
| DN25 | LF-GR-025-P | LF-GR-025-HC |
| DN40 | LF-GR-040-P | LF-GR-040-HC |
| DN50 | LF-GR-050-P | LF-GR-050-HC |
| DN65 | LF-GR-065-P | LF-GR-065-HC |
| DN80 | LF-GR-080-P | LF-GR-080-HC |
| DN100 | LF-GR-100-P | LF-GR-100-HC |
| DN125 | LF-GR-125-P | LF-GR-125-HC |
| DN150 | LF-GR-150-P | LF-GR-150-HC |

Grounding rings add 3 mm to the installed length per side; allow for a total of 6 mm when setting the flange gap. Use grounding ring material that matches or exceeds the corrosion resistance of the meter wetted parts.

### 9.3 Cathodically protected pipelines

On pipelines with impressed-current cathodic protection, the meter must be electrically isolated from the pipe with an insulating flange kit, and the grounding rings are bonded to each other but not to the pipe. Consult Meridian application engineering before installing on a cathodically protected line.

A missing or broken ground connection typically shows up as intermittent L-204 low signal quality warnings and a noisy flow reading at low velocities.

## 10. HART commissioning

The LF-60P supports HART 7 on the 4-20 mA output (terminals 3 and 4). The HART device revision with firmware 1.9.0 is device revision 3. Use the DD or DTM for device revision 3; older DD files for device revision 2 will connect but will not show the pressure variables.

### 10.1 Loop requirements

- A loop resistance of at least 250 ohm is required for HART communication. With a lower resistance, the handheld or asset management system will report no response and the meter may raise L-222.
- The maximum loop resistance in passive mode is (supply voltage - 12 V) / 0.022 A; with a 24 V loop supply this is 545 ohm.
- In active mode, the meter sources the loop current and the maximum load is 600 ohm.

### 10.2 Setting the HART parameters on the local display

1. Enter the service passcode (see section 13).
2. Go to Menu > Setup > Outputs > HART > Poll Address and set the polling address. The default is 0, which keeps the loop current active. Addresses 1 to 63 put the output into multidrop mode with a fixed 4 mA current.
3. Go to Menu > Setup > Outputs > HART > Tag and enter the long tag (up to 32 characters). The short tag is limited to 8 characters and is set from the host system.
4. Go to Menu > Setup > Outputs > HART > Variable Map to assign the dynamic variables. The factory mapping is PV volume flow, SV forward totaliser, TV process pressure, QV fluid temperature.
5. If burst mode is required, enable it under Menu > Setup > Outputs > HART > Burst. Burst mode is disabled by default.

### 10.3 Loop test

Use Menu > Diagnostics > Loop Test to force the output to 4, 12 and 20 mA and confirm the reading at the control system. The loop test times out after 10 minutes and returns the output to normal operation.

## 11. Flange bolting

Flange joints on the LF-60P must be tightened in a controlled sequence to the values below. Under-tightening causes leaks at operating pressure. Over-tightening distorts the measuring tube flanges and can crack the window seats, which is not covered by warranty.

### 11.1 Torque values

The values apply to EN 1092-1 PN40 flanges, lightly oiled carbon steel or stainless steel bolts (property class 8.8 or A4-80), and graphite-faced or compressed fibre gaskets. For PTFE gaskets, reduce the values by 20%.

| Meter size | Bolts | Torque |
|---|---|---|
| DN25 | 4 x M12 | 45 N m |
| DN40 | 4 x M16 | 75 N m |
| DN50 | 4 x M16 | 85 N m |
| DN65 | 8 x M16 | 80 N m |
| DN80 | 8 x M16 | 95 N m |
| DN100 | 8 x M20 | 140 N m |
| DN125 | 8 x M24 | 210 N m |
| DN150 | 8 x M24 | 250 N m |

For meters with ASME Class 300 flanges, use the torque values in the ASME supplement LF-ASME-300, which is supplied with the meter.

### 11.2 Tightening sequence

1. Fit all bolts finger tight and check that the gaskets are centred.
2. Tighten in a cross (star) pattern to 30% of the final torque.
3. Repeat the cross pattern to 60% of the final torque.
4. Repeat the cross pattern to 100% of the final torque.
5. Make one final circular pass at 100% to check that no bolt turns further.
6. Pressure test the line to 1.5 times the design pressure, but never above 60 bar.
7. For lines operating above 80 °C, re-check the torque after the first heat-up and cool-down cycle.

Use a calibrated torque wrench. Do not use impact tools.

## 12. Zero-point adjustment

The LF-60P is factory calibrated on a water flow rig traceable to national standards, and a zero-point adjustment is not normally required. Carry out a zero-point adjustment after installation only if the meter shows a flow reading above 0.5 cm/s when the line is known to be full and completely stationary.

1. Make sure the measuring tube is completely full and that there is no flow. Close valves both upstream and downstream of the meter; a closed downstream valve alone is not enough.
2. Wait at least 5 minutes for the fluid to settle and, for hot lines, for the temperature to stabilise.
3. Enter the service passcode.
4. Go to Menu > Service > Calibration > Zero Adjust and confirm.
5. The meter averages the signal for 60 seconds and stores the new zero offset. The display shows the offset in cm/s.
6. If the offset is larger than ±2.0 cm/s, the adjustment is rejected. Check for leaking valves, air in the line and grounding (section 9) before trying again.

The previous zero offset can be restored under Menu > Service > Calibration > Restore Factory Zero.

### 12.1 In-situ verification and window inspection

The LF-60P runs a continuous self-check of the LED emitter current, receiver gain and correlation quality. The results can be read under Menu > Diagnostics > Verification, which produces a pass or fail report with a four-digit verification code. Record the code at commissioning; comparing later codes against the commissioning value shows whether the optical path has degraded. Asset management systems can read the same report over HART (device-specific command 140) or Modbus.

Window fouling shows as a gradual fall in signal quality. Under normal conditions the signal quality indicator reads between 85% and 100%. Below 60%, the meter raises L-204; below 40% the flow reading is held at its last good value and the output goes to the configured fail-safe current (3.6 mA by default, adjustable to 21.0 mA under Menu > Setup > Outputs > Current > Fail-safe).

On fluids that contain oils, biofilm or scale-forming minerals, plan an inspection of the sapphire windows every 12 months. The windows can be inspected with a borescope through a flange after isolating and draining the line, without removing the meter. Clean with isopropyl alcohol; for mineral scale, use a 5% citric acid solution followed by a clean water rinse. Do not use abrasive pads, wire brushes or hydrochloric acid, which will damage the window seals.

### 12.2 Low-flow cut-off and damping

The low-flow cut-off suppresses readings close to zero. The default is 0.02 m/s and can be changed under Menu > Setup > Measurement > Low Flow Cutoff. The damping time constant defaults to 2.0 seconds on the LF-60P and can be set between 0.2 and 60 seconds under Menu > Setup > Measurement > Damping. Increase damping where the flow reading must be steady for control, and decrease it for fast batching.

## 13. Local display and security

The two-line display shows the flow rate on the first line and, by default, the forward totaliser on the second line. Press the right-hand key to scroll through velocity, reverse totaliser, process pressure, fluid temperature and signal quality.

Configuration menus are protected by a four-digit service passcode. The factory default passcode for the LF-60P is 4060. Change it at commissioning under Menu > Service > Security > Change Passcode. If the passcode is lost, it can be reset only through Meridian Link over the RS-485 service connection, using a site-specific reset token obtained from Meridian support.

After 5 minutes without a key press, the display returns to the measuring screen and the passcode must be entered again. Write protection can additionally be enforced by DIP switch S3 on the terminal board; when S3 is ON, no parameter can be changed from the display, HART or Modbus.

The display backlight can be set to Always On, Auto (on for 60 seconds after a key press) or Off under Menu > Setup > Display > Backlight. The default is Auto.

## 14. Commissioning checklist and support

Complete this checklist and keep a copy with the plant instrument records.

- Nameplate order code, serial number and pressure rating recorded
- Straight run confirmed: at least 15D upstream and 5D downstream
- Flow arrow in the correct direction
- Gaskets do not protrude into the bore
- Bolts tightened to the section 11 values in a cross pattern
- Grounding straps or grounding rings fitted and resistance below 1 ohm
- Supply voltage at terminals 1 and 2 between 21.6 and 26.4 VDC
- 4-20 mA loop test completed at 4, 12 and 20 mA
- HART communication confirmed (if used), device revision 3
- Modbus communication confirmed (if used); default settings are address 1, 19200 baud, 8E1
- Zero-point checked with stationary full pipe
- Service passcode changed from the default
- No active error codes shown under Menu > Diagnostics > Active Errors

For technical support, raise a ticket in the Meridian Support Centre at support.meridian-instruments.com and quote the meter serial number and firmware version. Error code meanings and fixes are listed in the Lumen Error Code Reference (sup-lumen-error-codes).
