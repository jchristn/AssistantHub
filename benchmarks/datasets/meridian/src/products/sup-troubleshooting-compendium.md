# Meridian Field Troubleshooting Compendium

Document ID: sup-troubleshooting-compendium | Version: 4 | Effective: 2025-09-15 | Owner: Customer Support (Rosa Delgado), with L2 Product Specialists

## 1. Purpose and how to use this compendium

This compendium collects the faults most often reported to the Meridian Support Centre (MSC) for the Halcyon gas detectors, the Tessera vibration-monitoring platform and the Lumen optical-flow meters, together with the causes our L2 product specialists have confirmed and the fixes that work in the field. It is written for Meridian L1 and L2 support engineers, field service engineers and certified partner technicians. Customers with Professional or Premier support contracts may also receive a copy from their account team.

Each entry follows the same pattern: the symptom as the customer usually describes it, the likely causes in order of probability, and the fix. Where a fix depends on a formal procedure, the entry names the procedure document rather than repeating it, because procedures change more often than this compendium. Always use the current version of the referenced procedure.

The compendium does not replace the error-code tables. When a device shows an error code, start with the relevant table: Halcyon Error Codes (sup-halcyon-error-codes), Tessera Error Codes (sup-tessera-error-codes) or Lumen Error Codes (sup-lumen-error-codes). Use this compendium when there is no code, when the code does not explain the behaviour, or when the published fix has not worked.

Safety first: a Halcyon instrument that has failed a bump test, or that shows any sensor fault, must be removed from service immediately. Do not allow a customer to continue using an instrument while a ticket is open. A report that an instrument failed to alarm in the presence of gas is always a P1 ticket and must be escalated under sup-escalation-procedure.

## 2. Information to collect before troubleshooting

Collecting the right information on the first contact avoids most repeat calls. For every ticket, record the following in the MSC ticket before starting diagnosis:

- Model and serial number of every affected device. Halcyon serial numbers are on the rear label and in Menu > Instrument > About. TS-4 and TS-4e serials are on the enclosure label and in TesseraCloud under Fleet. Lumen serials are on the transmitter nameplate.
- Firmware versions. Current releases are Halcyon 3.2.4 (HX-200), 3.4.1 (HX-210) and 3.4.2 (HX-220); HX-DOCK-4 D-2.1.0; TS-4 gateway 5.3.0; TS-4e node 2.8.0; Lumen 1.8.2 (LF-60) and 1.9.0 (LF-60P).
- Any error code shown on the device display, in TesseraCloud, or in the Modbus diagnostic registers.
- When the fault started, whether anything changed at that time (firmware update, relocation, new calibration gas cylinder, process change, network change), and how many devices are affected.
- For Halcyon instruments, the last calibration date and the calibration gas part number and lot number.
- For Tessera, the site name and gateway name as shown in TesseraCloud, and whether the gateway uses LTE or Ethernet backhaul.
- For Lumen, the pipe size, process fluid, process temperature and pressure, and whether the pipe runs full at all times.

## 3. General faults common to all product lines

### 3.1 Device does not power on

Likely causes: depleted or failed battery (Halcyon, TS-4e), no 24 VDC supply or reversed polarity (Lumen, TS-4 without PoE), blown supply fuse, or damaged power connector.

Fix: For Halcyon, charge for at least 30 minutes in the charger or HX-DOCK-4 before concluding the instrument is dead; a deeply discharged pack may not show the charging indicator for the first 10 minutes. For Lumen, measure the supply at the terminal block; the LF-60 and LF-60P both require 24 VDC plus or minus 10%. For TS-4 gateways powered by PoE, confirm the switch port supplies IEEE 802.3at (PoE+); a gateway on an 802.3af port may boot and then restart when the LTE modem starts transmitting.

### 3.2 Device clock is wrong

Likely causes: the real-time clock lost backup power, time zone set incorrectly, or the device has not synchronised since a long storage period.

Fix: Halcyon instruments take their time from the HX-DOCK-4 or from Meridian Link when docked; an instrument showing E-126 needs docking to reset the clock. TS-4 gateways use NTP, falling back to the LTE network time; see section 8.4 for clock drift on TS-4e nodes. Lumen meters keep time only for totaliser logs and can be set from the local display.

### 3.3 Firmware update fails part way

Likely causes: interrupted power, insufficient battery charge, incompatible version sequence, or a USB cable that supports charging only.

Fix: Halcyon firmware updates require at least 40% battery and must be done in a HX-DOCK-4 running dock firmware D-2.1.0 for instrument firmware 3.4.1 and later. A failed update often leaves the instrument showing E-122; re-run the update from the dock. For Tessera, never skip the compatibility check in prod-tessera-firmware-compatibility-matrix: upgrading nodes to 2.8.0 before the gateway is on 5.3.0 leaves the nodes unable to join.

## Part A: Halcyon gas detectors

## 4. Halcyon power and battery faults

### 4.1 Runtime much shorter than specified

Symptom: an instrument that should run a full shift turns off early or shows a low-battery warning mid-shift.

Reference runtimes at 20 C are 36 hours for the HX-200, 14 hours for the HX-210 and 22 hours for the HX-220, with no alarms active and backlight on default settings.

Likely causes, in order: the battery pack is older than 12 months and has lost capacity; the instrument is used in the cold (see section 11); the backlight has been set to always on; frequent alarms, since the buzzer, LEDs and vibration motor all draw heavily; on the HX-220, Bluetooth and GPS left active when not needed; on the HX-220 with the HX-PMP-22 pump fitted, pump operation, which roughly halves runtime.

Fix: check battery health in Menu > Instrument > Battery > Health. A value below 70% means the pack should be replaced (HX-BAT-200, HX-BAT-210 or HX-BAT-220L as appropriate; the packs are not interchangeable). Batteries carry a 12-month warranty.

### 4.2 Instrument will not charge in the dock

Likely causes: dirty charge contacts, instrument not fully seated, dock bay fault, or an HX-200 placed in the dock without the HX-ADP-200 adapter.

Fix: clean the contacts with an isopropyl alcohol wipe and let them dry. Try a different bay. The HX-200 must always sit in the HX-ADP-200 adapter; without it the contacts do not meet and the bay LED stays amber.

### 4.3 E-111 battery fault

E-111 indicates a cell imbalance or a pack temperature fault. Remove the instrument from service and replace the battery pack. If E-111 returns with a new pack, raise an RMA; the charge controller is at fault.

## 5. Halcyon sensor and calibration faults

### 5.1 Calibration fails on one channel

Symptom: HX-DOCK-4 or manual calibration reports a failure, often with E-104 on screen.

Likely causes: wrong calibration gas; expired cylinder; wrong regulator flow; insufficient stabilisation time; a sensor at end of life.

Fix: this is the most common Halcyon ticket and, since May 2025, most cases are caused by sites still using the discontinued MI-CAL-Q4-A mix or the 0.5 L/min MI-REG-05 regulator with the HX-210 or HX-220. The HX-210 and HX-220 now require MI-CAL-Q4-B at 1.0 L/min through an MI-REG-10 regulator. Follow the current HX-210 Calibration Procedure (prod-hx210-calibration-procedure-v2) exactly; the HX-220 uses the same gas and flow. The HX-200 still uses MI-CAL-H2S25 at 0.5 L/min. Check the cylinder expiry date on the label: expired H2S cylinders lose concentration quickly and cause H2S span failures. If one channel still fails after a correct calibration, check sensor life in Menu > Instrument > Service > Sensor Life and replace the sensor following sup-hx-sensor-replacement-procedure.

### 5.2 E-117 on the LEL channel

E-117 means the catalytic LEL sensor bridge is out of range, usually because the sensor has been poisoned by silicones, lead or sulphur compounds, or inhibited by high H2S. Do not attempt to recalibrate; a poisoned sensor may appear to calibrate but then under-read methane. Replace HX-SNS-LEL-04 as described in section 4 of sup-hx-sensor-replacement-procedure, then perform a full calibration and a bump test. Ask the customer about silicone-based sprays, sealants or lubricants in the work area, since repeat E-117 faults nearly always have a local source.

### 5.3 Oxygen reading drifts downwards

Likely causes: the O2 sensor is approaching end of life (typical life 24 to 30 months), or the instrument is stored in a sealed case with a charging battery.

Fix: calibrate in clean air. If the O2 reading will not reach 20.9% vol, replace HX-SNS-O2-01. Note that under the 2025 warranty terms the O2 sensor carries 18 months of cover, against 24 months for the other sensors.

### 5.4 CO reading rises in the presence of hydrogen

The CO sensor has a known cross-sensitivity to hydrogen of about 40%, so 100 ppm H2 can read as roughly 40 ppm CO. This is normal. For battery rooms and hydrogen applications, advise the customer to contact their account manager, because the Halcyon range does not include a hydrogen-compensated CO sensor.

## 6. Halcyon alarm and display faults

### 6.1 Customer reports wrong alarm levels

Most "wrong alarm" tickets come from mixed fleets. The HX-210 and HX-220 have different factory defaults for H2S high (10 ppm and 15 ppm), CO low (30 ppm and 35 ppm), CO high (100 ppm and 200 ppm), LEL high (20% LEL and 25% LEL) and O2 high (23.0% vol and 23.5% vol). A site that bought HX-220s to add to an HX-210 fleet will see different alarm behaviour unless the set points are aligned in Meridian Link. Confirm which model the customer is holding before changing anything.

### 6.2 Display blank but instrument beeps

Likely causes: display ribbon damaged by a drop, or the display heater failed on an HX-220 used below -20 C.

Fix: raise an RMA. Do not open the instrument; opening voids the intrinsic safety certification.

### 6.3 Man-down alarm triggers when the wearer is stationary (HX-220 only)

The HX-220 man-down alarm triggers after 60 seconds without motion by default, with a 15-second pre-alarm. Crane operators and control-room staff often trigger it. The timer can be increased to 180 seconds in Menu > Settings > Man-Down > Timer; it cannot be disabled on instruments where the site profile marks man-down as mandatory.

## 7. HX-DOCK-4 docking station faults

### 7.1 Dock rejects instruments after a firmware update

Instruments updated to firmware 3.4.1 or later will not calibrate in a dock running D-2.0.x firmware; the dock shows "Instrument firmware unsupported". Update the dock to D-2.1.0 from Meridian Link before updating instruments.

### 7.2 Bump test fails in the dock but passes manually

Likely causes: gas inlet pressure too low, blocked inlet filter, or tubing longer than 10 m between cylinder and dock.

Fix: the dock needs an inlet supply from a demand-flow or fixed-flow regulator with tubing no longer than 10 m. Replace the inlet filter every 6 months. Check that the purge port is not blocked by a wall or cabinet.

### 7.3 HX-220 Bluetooth pairing and E-131

Symptom: the HX-220 will not pair with the supervisor's phone, or live readings stop appearing in the app.

Likely causes: phone Bluetooth permissions revoked after an operating system update, the instrument already paired with another phone (an HX-220 pairs with one device at a time), or a Bluetooth module fault shown as E-131.

Fix: on the instrument, clear the existing pairing in Menu > Settings > Bluetooth > Forget Paired Device, then pair again from the app. If E-131 is shown, restart the instrument once; if E-131 returns, the Bluetooth module has failed. The instrument remains safe to use as a gas detector, because gas detection and local alarms do not depend on Bluetooth, but GPS location and remote man-down notification are lost. Raise an RMA. Firmware 3.4.2 fixed a pairing loss that affected 3.4.1 on the HX-220 after 72 hours of continuous connection, so confirm the firmware version first.

## Part B: Tessera vibration monitoring

## 8. TS-4 gateway faults

### 8.1 Gateway shows offline in TesseraCloud

Likely causes: backhaul failure (T-320), expired device certificate (T-327), firewall blocking outbound traffic, SIM suspended, or gateway firmware below the TesseraCloud minimum.

Fix: from 1 October 2025, TesseraCloud accepts connections only from gateways running firmware 5.2.0 or later; older gateways appear offline without an error. Check the local UI at https://192.168.50.1 for the connection status page. The gateway needs outbound TCP 443 and 8883 to TesseraCloud. If the status page shows a certificate error, follow the certificate re-provisioning section of the TesseraCloud Administrator Guide (prod-tesseracloud-admin-guide).

### 8.2 LTE gateway drops connection every few hours

Likely causes: weak LTE signal, carrier moving the SIM between Cat-M1 and fallback networks, or the antenna connector not tightened.

Fix: check signal quality on the local UI; RSRP below -110 dBm is marginal for the TS4-GW-LTE. Tighten the N-type antenna connector by hand plus a quarter turn and check the antenna is vertical. Where possible move to Ethernet backhaul or fit the optional external antenna on a mast.

### 8.3 Gateway restarts repeatedly

Likely causes: PoE budget (see 3.1), overheating in a sealed cabinet above 60 C, or a corrupted configuration after a power loss during update. If restarts continue after power and temperature are ruled out, perform a factory reset by holding RESET for 15 seconds and re-provision the gateway in TesseraCloud.

### 8.4 Node clock drift (T-312)

TS-4e nodes take their time from the gateway at each sync. T-312 appears when a node has not synchronised for more than 24 hours, usually because it has lost its mesh route. Treat it as a mesh coverage problem (section 12).

## 9. TS-4e edge node faults

### 9.1 Node will not join the gateway (T-301)

Likely causes: node firmware incompatible with gateway firmware, node commissioned to a different gateway, node out of range, or node not woken with the commissioning magnet.

Fix: check versions against prod-tessera-firmware-compatibility-matrix. Node 2.8.0 joins only gateways on 5.3.0; node 2.7.0 does not join gateways on 5.1.x. Zone 2 nodes (TS4E-NODE-3AX-Z2) require node firmware 2.8.0. If versions are compatible, hold the commissioning magnet against the node's marked target for 3 seconds until the LED flashes blue.

### 9.2 Short node battery life (T-305)

The specified battery life is 5 years at 1 sample per hour. Customers who switch nodes to high-rate acquisition (for example a 1-minute interval or continuous waveform capture) can drain a battery in months. Review the acquisition schedule in TesseraCloud. Nodes in poor mesh positions also use more energy because of retries.

### 9.3 Readings look noisy or show a resonance

Likely causes: the node is loose on its stud, is mounted on a thin cover rather than the bearing housing, or has been fitted with a magnetic base on a curved surface.

Fix: remount on a flat, machined spot-face. The M6 mounting stud is tightened to 6 N m. Do not use thread-locking compound on the stud; it dampens high-frequency response.

## 10. TesseraCloud and API faults

### 10.1 Integrations returning HTTP 429

HTTP 429 means the tenant or device has exceeded the API rate limit. The limits changed in July 2025; see the current TesseraCloud API rate limits document (prod-tesseracloud-api-rate-limits-v2). The most common cause is a script polling /v2/devices/{id}/readings for every device every minute. Advise customers to honour the Retry-After header, to read X-RateLimit-Remaining, and to use POST /v2/exports for bulk history.

### 10.2 Alarms not received by email

Check the user's notification preferences, then the site alarm routing, then whether the recipient's mail system is filtering messages from the TesseraCloud sender domain. Webhook deliveries can be checked in the webhook delivery log.

### 10.3 Users cannot sign in after SSO change

Where the tenant uses SAML single sign-on, a certificate rollover at the identity provider stops all sign-ins. The Tenant Owner must upload the new identity provider certificate; see prod-tesseracloud-admin-guide.

### 10.4 Too many vibration alarms after commissioning

Symptom: a newly commissioned site generates dozens of alert and danger alarms in the first week, and the customer suspects faulty nodes.

Likely causes: alarm thresholds left at the generic defaults rather than set for the machine class, nodes mounted on non-rigid covers (section 9.3), or baselines not yet learned.

Fix: TesseraCloud learns an operating baseline over the first 14 days of data for each asset. During this period, adaptive alarms are suppressed and only absolute ISO 10816 zone alarms are active. Check that each asset has the correct machine class (for example Class II for medium machines on rigid foundations) in the asset settings, because a Class I threshold applied to a large pump produces constant alarms. Where a machine runs at several speeds, create an operating state for each speed so that thresholds follow the load. See the Tessera Alarm Configuration Guide (prod-tessera-alarm-configuration-guide) for the full method.

### 10.5 Readings missing for a period

Gaps in trend charts usually follow a gateway backhaul loss. The TS-4 gateway buffers up to 7 days of node readings locally and uploads them when the connection returns, oldest first. If gaps remain after the gateway has been back online for more than 6 hours, the buffer overflowed or the gateway was restarted during the outage; buffered data is lost if the gateway is factory reset.

## 11. Environmental effects on Halcyon instruments

This section brings together field experience from cold-store, offshore and desert sites. Most "sensor fault" tickets from these environments are actually environmental effects.

### 11.1 Cold-weather runtime derating

Lithium-ion packs deliver less energy in the cold. For planning, apply a runtime derating of 25% at -10 C and 40% at -20 C relative to the 20 C runtime. An HX-210 rated at 14 hours at 20 C should therefore be planned for about 8.4 hours at -20 C, which is not a full 12-hour shift. The HX-220, the only Halcyon model rated down to -40 C, should be planned for no more than 11 hours at -40 C because the display heater operates below -20 C. The HX-200 and HX-210 are rated only down to -20 C and must not be used below it. Advise cold-store customers to keep instruments in a warm locker between shifts and to swap to a charged spare at break times rather than relying on nominal runtime.

### 11.2 Sensor response time in the cold

Electrochemical sensors slow down in the cold. At -20 C the H2S and CO T90 response times roughly double. Bump tests performed in a cold store may fail on response time even though the sensor is healthy. Perform bump tests at room temperature after at least 30 minutes of acclimatisation.

### 11.3 Condensation after moving from cold to warm

Moving an instrument from a cold store to a warm, humid area causes condensation on the sensor membranes and a temporary O2 dip of up to 0.5% vol. The reading recovers within 5 minutes. This is not a fault, but it can trigger a low O2 alarm on HX-210 instruments set at 19.5% vol if the ambient reading is already low.

### 11.4 High temperature and sun exposure

Instruments left on vehicle dashboards can exceed the maximum operating temperature (+50 C for the HX-200, +55 C for the HX-210 and HX-220). Electrolyte loss after repeated heat exposure shortens sensor life and is excluded from warranty.

## 12. TesseraMesh radio planning and coverage faults

The TS-4 gateway and TS-4e nodes use TesseraMesh, a 2.4 GHz IEEE 802.15.4 mesh network. Most "node offline", T-301 and T-312 tickets on established sites are coverage problems caused by new obstructions: stacked materials, new machinery or metal cladding.

### 12.1 Received signal thresholds

Each node reports the RSSI of its parent link in TesseraCloud under Fleet > Nodes > [node] > Radio. Use these thresholds:

| Parent link RSSI | Assessment | Action |
|---|---|---|
| -70 dBm or stronger | Good | None |
| -71 to -82 dBm | Acceptable | Monitor packet loss |
| -83 to -88 dBm | Marginal | Add a repeater node or reposition |
| Weaker than -88 dBm | Unreliable | Must be fixed; expect T-301 and T-312 |

A link weaker than -88 dBm is the single most common cause of intermittent node loss. Packet loss above 5% over 24 hours also indicates a marginal link even when RSSI looks acceptable, usually because of Wi-Fi interference on overlapping channels.

### 12.2 Hop count and topology

A node can reach the gateway through at most 4 hops. Nodes beyond 4 hops are refused a route and show T-301 even when their local signal is good. A single gateway supports up to 64 nodes, but sites with more than 40 nodes in a dense metal environment should be split across two gateways.

### 12.3 Wi-Fi coexistence

TesseraMesh defaults to 802.15.4 channel 25, which sits between Wi-Fi channels 11 and 13 in most regions. If the site Wi-Fi uses channel 13, move TesseraMesh to channel 15 in the gateway local UI under Network > Mesh > Channel. All nodes follow the gateway channel change automatically within 10 minutes.

## 13. Lumen optical-flow meter faults

### 13.1 Reading zero with flow present (L-201 empty pipe)

The meter declares an empty pipe when the optical path sees no fluid for longer than the detection delay. Likely causes: the pipe genuinely runs partly full (gravity lines, pump stops), entrained air at the top of a horizontal pipe, or the meter installed at a high point.

Fix: first confirm the installation. Lumen meters must not be installed at a high point in the pipe or in a downward vertical run. Where a pipe briefly runs partly full during normal operation, lengthen the empty-pipe detection delay. On the local display this is set in Menu > Diagnostics > Empty Pipe > Detection Delay. The factory default is 5 seconds; for gravity-fed lines and pump-cycled lines, 30 seconds is recommended, and the maximum is 120 seconds. On the LF-60P the same parameter can also be set over HART. Do not simply disable empty-pipe detection; the totaliser will then accumulate false flow from reflections.

### 13.2 Unstable reading (L-204 low signal quality)

Likely causes: dirty optical windows, high solids content, entrained gas bubbles, or insufficient straight pipe run.

Fix: inspect and clean the windows with the supplied cleaning kit. Confirm the straight run: the LF-60 needs 10 pipe diameters upstream and 5 downstream; the LF-60P needs 15 upstream and 5 downstream. Many LF-60P complaints come from sites that installed to the LF-60 straight-run rule.

### 13.3 Leak at the flange after installation

Likely cause: flange bolts under-torqued, or torqued in the wrong sequence. Lumen meters must be torqued in a star pattern in three passes (30%, 70%, 100%). Correct final torque for DN50 is 60 N m for the LF-60 (PN16, 4 x M16) and 85 N m for the LF-60P (PN40, 4 x M16). Full torque tables are in the installation manuals, including prod-lf60p-installation-manual. Leaks caused by incorrect torque are excluded from warranty.

### 13.4 No HART communication (LF-60P only, L-222)

HART needs a loop resistance of at least 250 ohms. Check that the HART modem or handheld is connected across the resistor, not across the supply. The LF-60 does not support HART, so an L-222 report on an LF-60 means the model has been recorded incorrectly in the ticket.

### 13.5 Modbus master sees no response

Check the serial settings. The LF-60 defaults to address 1, 9600 baud, 8N1; the LF-60P defaults to address 1, 19200 baud, 8E1. Mixed LF-60 and LF-60P networks on one RS-485 segment must be set to common settings. Termination resistors of 120 ohms are needed at both ends of the bus only.

## 14. Escalation criteria from this compendium

Escalate to L2 immediately when:

- The same fault recurs on more than three devices at one site within 30 days.
- A published fix has been followed exactly and has not worked.
- The fault appeared immediately after a firmware update.
- A Halcyon instrument may have failed to alarm (P1; also follow sup-escalation-procedure notification rules).

Raise an RMA under sup-rma-process-v2 when the fault is confirmed to be a hardware failure. Remember that every return needs decontamination declaration MI-F-031.

## 15. Collecting diagnostic logs

- Halcyon: dock the instrument and export the event log and calibration history from Meridian Link using Instrument > Export > Service Bundle. The bundle is a single .mlz file; attach it to the MSC ticket.
- TS-4 gateway: from the local UI, select System > Diagnostics > Download Support Bundle. The bundle covers the last 7 days of logs.
- TS-4e node: node logs are pulled through the gateway; in TesseraCloud, select Fleet > Nodes > [node] > Actions > Request Diagnostics. Allow up to one hour for the upload.
- Lumen: read the diagnostic registers over Modbus (see prod-lumen-modbus-register-map) or, on the LF-60P, save a HART device report.

## 16. Revision history

| Version | Date | Changes |
|---|---|---|
| 1 | 2024-01-22 | First issue, Halcyon and Lumen only |
| 2 | 2024-07-08 | Tessera part added |
| 3 | 2025-02-17 | Cold-weather guidance and TesseraMesh RSSI table added |
| 4 | 2025-09-15 | Updated for HX-210 calibration procedure v2, API rate limits v2, RMA process v2 and the TesseraCloud minimum gateway firmware |
