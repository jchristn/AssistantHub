# Penang SMT Line Changeover Runbook

Document ID: mfg-penang-smt-changeover-runbook. Owner: Penang Manufacturing Engineering (Factory Manager: Nurul Aziz). Last reviewed: 2025-08-04. Form reference: MFG-F-112 (SMT Changeover Record).

This runbook is controlled. Printed copies at the line are for reference only; the controlled version is the one published in the Meridian Instruments engineering knowledge base. If a printed copy and the published version disagree, the published version wins and the line lead must replace the printed copy before the next shift.

## 1. Purpose

This runbook describes how a surface-mount technology (SMT) line at the Meridian Instruments Penang factory in Bayan Lepas is changed over from one board family to another. It exists so that every changeover is done the same way on every shift, so that the line comes back to production quickly, and so that no board is released to volume production until the setup has been proven by a first article inspection.

The runbook follows single-minute exchange of die (SMED) principles. Work that can be done while the line is still running the previous job is treated as external work and must be completed before the line is stopped. Only work that genuinely requires the line to be stopped is treated as internal work.

## 2. Scope

This runbook applies to the four SMT lines in the Penang factory, SMT-1 to SMT-4, and to every changeover between the board families listed in section 4. It applies to all shifts, including weekend overtime shifts.

It does not cover:

- New product introduction (NPI) builds, which follow the NPI build procedure and require a manufacturing engineer to be present for the whole build.
- Changes of revision within the same board family where only the placement program changes; those are handled as a program-only change and recorded on MFG-F-112 with the changeover type set to "program only".
- Through-hole, selective solder, conformal coating and final assembly operations.
- Rework of boards after AOI; rework follows the rework station instruction.

## 3. Lines and equipment

Each of the four lines has the same equipment sequence. The lines are physically identical in layout, but the placement machine feeder bank capacity differs between SMT-1/SMT-2 and SMT-3/SMT-4, which affects which board families are normally scheduled on which line.

| Line | Stencil printer | SPI | Placement machines | Reflow oven | AOI | Usual board families |
|---|---|---|---|---|---|---|
| SMT-1 | 1 | 1 | 2 | 10-zone | 1 | HX-MB, LF60-SB |
| SMT-2 | 1 | 1 | 2 | 10-zone | 1 | HX-MB, TS4E-EN |
| SMT-3 | 1 | 1 | 2 | 10-zone | 1 | TS4-GW, TS4E-EN |
| SMT-4 | 1 | 1 | 2 | 10-zone | 1 | TS4-GW, LF60-SB |

Every line runs in this order: stencil printer, solder paste inspection (SPI), placement machine 1, placement machine 2, 10-zone reflow oven, automated optical inspection (AOI). All four ovens are nitrogen-capable, but nitrogen is only required for the TS4E-EN profile.

The line PC for each line is connected to the factory OT network. Faults with the line PC, the barcode scanners, or the connection to the Atlas shop-floor module are raised in Beacon under the category MFG-OT. Do not raise these as generic hardware tickets; the MFG-OT queue is staffed around the clock for the Penang factory and the generic queue is not.

## 4. Board families and process parameters

The Penang factory builds four board families on the SMT lines.

- HX-MB is the Halcyon main board. It is shared by the HX-200, HX-210 and HX-220 gas detectors; the variant is set later by the sensor module fitted in final assembly, not on the SMT line.
- TS4-GW is the Tessera TS-4 gateway board.
- TS4E-EN is the Tessera TS-4e edge node board. It has the finest pitch components in the factory and is the most sensitive to paste and stencil condition.
- LF60-SB is the Lumen sensor board. It is used on both the LF-60 and the LF-60P optical-flow meters.

| Board family | Stencil ID format | Stencil thickness | Reflow profile | Profile peak | Atmosphere | Under-stencil wipe | Solder paste |
|---|---|---|---|---|---|---|---|
| HX-MB | STN-HX-MB-rev | 0.12 mm | RP-HX-07 | 245 °C | Air | Every 10 prints | SAC305 Type 4 |
| TS4-GW | STN-TS4-GW-rev | 0.12 mm | RP-GW-04 | 243 °C | Air | Every 10 prints | SAC305 Type 4 |
| TS4E-EN | STN-TS4E-EN-rev | 0.10 mm | RP-TS4E-03 | 240 °C | Nitrogen, O2 below 1000 ppm | Every 5 prints | SAC305 Type 4 |
| LF60-SB | STN-LF60-SB-rev | 0.13 mm | RP-LF-02 | 238 °C | Air | Every 10 prints | SAC305 Type 4 |

Stencil IDs follow the format STN-<board>-<rev>, for example STN-HX-MB-D or STN-TS4E-EN-B. The revision letter on the stencil must match the stencil revision printed on the job traveller. A stencil with a different revision letter must not be used, even if the board revision appears compatible.

All board families use the same solder paste, SAC305 Type 4. Paste handling rules are in section 6.4 and apply regardless of board family.

## 5. Roles

| Role | Responsibility during changeover |
|---|---|
| Line operator | Performs the changeover steps, scans feeders, loads the stencil, completes the operator sections of MFG-F-112. |
| Line lead (setup technician) | Owns the changeover end to end, checks programs and profiles, performs the second check in step 19, calls the shift lead if the escalation threshold is reached. |
| Material handler | Kits components and feeders for the next job, delivers thawed paste, returns unused reels to stores. |
| QA inspector | Performs and signs the first article inspection (FAI), decides whether the line can be released. |
| Shift lead | Receives escalations when a changeover exceeds 40 minutes, decides on re-sequencing the schedule. |
| Process engineer (on call) | Supports AOI library tuning, profile issues and SPI failures that the line lead cannot resolve. |

Only the QA inspector can sign the FAI. The line lead cannot sign the FAI for a line they have set up, even if they hold a QA inspection qualification.

## 6. Preparation (external activities)

Everything in this section is external work under SMED. It must be completed while the line is still running the previous job. The changeover clock on MFG-F-112 does not start until the last good board of the previous job has left the AOI, so time spent in preparation does not count against the target, but poor preparation is the most common reason for missing the target.

### 6.1 Timing

The material handler starts preparation when the Atlas shop-floor module shows that the running job has 60 minutes or less of remaining build time. The line lead confirms the next job on the Atlas schedule at the same time. If the schedule has been changed by planning in the last 60 minutes, the line lead must confirm with the shift lead that the new job is correct before any material is kitted.

### 6.2 Kitting

The material handler kits all component reels, trays and tubes for the next job against the Atlas pick list. Each reel is scanned out of stores so that the Atlas inventory reflects the move. Partial reels are preferred over new reels where the partial reel quantity covers the job, to reduce the number of open reels on the floor.

### 6.3 Moisture sensitive components

Moisture sensitive devices (MSDs) must be handled according to mfg-msl-handling-procedure. For the Penang lines the most common level is MSL 3, which has a floor life of 168 hours once the dry pack is opened. Before an MSL 3 reel is kitted, the material handler checks the floor life label. If the accumulated exposure exceeds 168 hours, the reel must be baked according to mfg-msl-handling-procedure before it can be used. Do not kit an expired reel "to be baked later"; baked reels are kitted as a separate delivery.

### 6.4 Solder paste

Solder paste is SAC305 Type 4. Unopened jars are stored in the paste refrigerator at 2–10 °C. The material handler removes the paste required for the next job from the refrigerator and allows it to thaw for at least 4 hours at room temperature before opening. Opening a jar before it has thawed causes condensation in the paste and leads to solder balling.

Once a jar is opened on the line it may stay open for a maximum of 24 hours. The time the jar was opened is written on the jar label. Paste that has been open for more than 24 hours must be scrapped, not returned to the refrigerator. Paste left on the stencil at the end of a job may be reused on the next job only if the next job is on the same line, the paste is within its 24-hour open time, and it has not been contaminated.

### 6.5 Pre-staging feeders offline

Feeders for the next job are loaded offline on the feeder cart at the pre-staging bench. Each reel is loaded into a feeder, the feeder barcode and the reel barcode are scanned together at the pre-staging bench, and the pairing is recorded in the Atlas shop-floor module. Pre-staged carts are labelled with the line, the job number and the placement machine they are intended for.

### 6.6 Stencil and program readiness

The line lead retrieves the stencil for the next job from the stencil store and checks:

- The stencil ID matches the traveller (format STN-<board>-<rev>).
- The stencil tension is at least 35 N/cm at all five measurement points. A stencil below 35 N/cm at any point is quarantined and reported to the process engineer.
- The apertures are clean and undamaged when viewed on the light table.

The line lead also confirms that the placement programs for both placement machines, the SPI program, the AOI program and the reflow profile named on the traveller are available on the line PC. The profile is not loaded yet.

### 6.7 ESD readiness

All personnel involved in the changeover must pass the wrist strap check at the station tester before touching boards, feeders or stencils, per mfg-esd-control-standard. A failed wrist strap check must be resolved before the person takes part in the changeover.

## 7. Changeover procedure (internal activities)

The changeover clock starts when the last good board of the previous job leaves the AOI. The line operator writes the start time on MFG-F-112. The target for a full changeover, from the last good board of the previous job to QA release of the new job, is 25 minutes. Steps are numbered continuously and must be performed in order unless a step explicitly allows parallel work.

1. Confirm on the Atlas shop-floor module that the previous job is complete and that the board count matches the job quantity. Record any shortfall on MFG-F-112.
2. Clear the line. Remove all boards of the previous job from the conveyors, the reflow oven exit, the AOI buffer and the rework shelf. No board from the previous job may remain on the line when the first board of the new job is loaded.
3. Remove the stencil from the printer. Remove remaining paste from the stencil with the paste knife. Decide whether paste can be reused according to section 6.4; if not, scrap it and record the quantity.
4. Send the used stencil to the stencil cleaner. The stencil is logged back into the stencil store only after it has been cleaned and inspected.
5. Load the stencil for the new job. Scan the stencil ID barcode on the printer. The printer software rejects a stencil whose ID does not match the loaded print program.
6. Load the print program for the new board family. Confirm the under-stencil wipe frequency in the program: every 5 prints for TS4E-EN, every 10 prints for HX-MB, TS4-GW and LF60-SB.
7. Apply fresh or reusable solder paste to the stencil. Confirm the open time written on the jar label is within 24 hours.
8. Remove the feeder carts of the previous job from both placement machines. At the same time (parallel work), the material handler brings the pre-staged carts for the new job to the line.
9. Dock the pre-staged feeder carts to placement machine 1 and placement machine 2 according to the cart labels.
10. Perform feeder verification. The line operator scans each feeder position against the setup sheet in the Atlas shop-floor module. Any mismatch between the scanned reel and the setup sheet locks the placement machine; the lock can only be released by correcting the feeder and rescanning, not by overriding on the machine.
11. Load the placement programs for both placement machines and the SPI and AOI programs for the new board family. Confirm the program names on the line PC match the traveller.
12. Load the reflow profile named on the traveller (RP-HX-07, RP-GW-04, RP-TS4E-03 or RP-LF-02). If the new profile is RP-TS4E-03, switch the oven to nitrogen and wait until the oven O2 reading is below 1000 ppm. Wait for all 10 zones to reach their set points and for the oven to report stable. Record the oven stable time on MFG-F-112.
13. Adjust the conveyor width on printer, SPI, both placement machines, oven and AOI for the new board. Run one bare board through the whole line to confirm transfer without jamming.
14. Perform the First Article Inspection (FAI). Print, place and reflow the first board of the new job and hold it at the AOI output. The SPI result for the first board must show paste volume within 75–150% on every pad. The AOI false call rate for the new program is checked against the threshold of 500 ppm; if the false call rate is above 500 ppm, the AOI library must be re-tuned by the process engineer before release, and the FAI is repeated after re-tuning. The QA inspector performs the visual and dimensional checks listed in section 8. The FAI must be signed by QA on MFG-F-112 before the line is released. Without a QA signature the line stays in FAI hold.
15. If the SPI result is outside 75–150% on any pad, stop. Check stencil seating, paste condition and printer support pins, correct the cause, wash the board, and repeat step 14. Record each repeat on MFG-F-112.
16. If the AOI reports a real defect (not a false call) on the first article, the line lead and QA inspector decide whether the cause is setup (feeder, program, placement offset) or material. Setup causes are corrected and step 14 is repeated. Material causes are escalated to the process engineer.
17. The first article board is marked with the FAI label and retained at the line for the rest of the shift. It is sent to the FAI retention shelf at the end of the shift.
18. Load a second board and confirm SPI and AOI results remain within the step 14 criteria. This confirms the setup is stable and not only correct for one board.
19. Before releasing the line to volume production, perform the two-person reflow profile verification. The line lead and a second person (the QA inspector or another qualified setup technician) independently read the profile name loaded on the oven controller and compare it with the profile named on the job traveller. Both people initial MFG-F-112. If the names differ, the line must not be released; all boards processed since step 12 are quarantined and the line lead contacts the process engineer. This step was added as an action item of postmortem INC-2025-014, when SMT-2 ran TS4E-EN boards on the HX-MB profile (RP-HX-07 instead of RP-TS4E-03) in March 2025.
20. Release the line in the Atlas shop-floor module. The release transaction records the operator, line lead and QA inspector IDs.
21. Record the changeover end time on MFG-F-112. The changeover time is the elapsed time from the start time recorded before step 1 to the release in step 20.
22. If the changeover time exceeded 25 minutes, record the main cause of delay on MFG-F-112 using the delay codes in section 10. If it exceeded 40 minutes, confirm the shift lead was contacted (see section 9).
23. Return unused reels from the previous job to stores. Each reel is scanned back into Atlas. MSL reels are returned with an updated floor life label or placed into dry storage according to mfg-msl-handling-procedure.
24. Return the feeder carts from the previous job to the pre-staging bench. Unload the feeders and clean them before they are reused.
25. Confirm that the paste jar in use is labelled with the line number and open time, and that the next paste required for the shift has been removed from the refrigerator in time to complete its 4-hour thaw.
26. Clean the work area around the printer and placement machines. Remove any dropped components; dropped components are scrapped, never reloaded.
27. Check the SPI trend for the first 20 boards. If paste volume is trending towards the 75% or 150% limits, notify the line lead before the limit is reached.
28. Check the AOI false call trend for the first 50 boards. If the false call rate trends above 500 ppm, notify the process engineer.
29. The line lead reviews MFG-F-112 for completeness: start time, stencil ID and tension reading, feeder verification result, profile and oven stable time, FAI signature, step 19 initials, release time and delay codes.
30. The line lead submits MFG-F-112. Completed forms are scanned and attached to the job in the Atlas shop-floor module at the end of the shift.

## 8. First article inspection acceptance criteria

The QA inspector uses the following criteria for the FAI in step 14. All criteria must be met for the FAI to pass.

| Check | Method | Acceptance criterion |
|---|---|---|
| Solder paste volume | SPI | 75–150% of nominal on every pad |
| Paste offset | SPI | Within program tolerance, no bridging between pads |
| Component presence and polarity | AOI and visual | 100% correct against the placement program |
| AOI false call rate | AOI program statistics | At or below 500 ppm; above 500 ppm requires AOI library re-tuning before release |
| Solder joint quality | AOI and visual under magnification | Meets IPC-A-610 Class 2 as a minimum; Class 3 for TS4E-EN connector joints |
| Reflow profile | Oven controller | Profile named on the traveller, oven stable, nitrogen O2 below 1000 ppm for RP-TS4E-03 |
| Board identification | Visual and scan | Serial label present and readable, matches Atlas job |
| Stencil | Printer record | Stencil ID matches traveller, tension at least 35 N/cm |

If any criterion is not met, the line remains in FAI hold. The QA inspector records the failed criterion on MFG-F-112 and the corrective action taken before the FAI is repeated.

## 9. Escalation

The changeover target is 25 minutes. The line lead is expected to monitor the clock and act before the escalation threshold is reached.

- At 25 minutes, the changeover is over target. The line lead continues but must record a delay code on MFG-F-112.
- If the changeover exceeds 40 minutes, the line lead must escalate to the shift lead. The shift lead decides whether to continue, to bring in the on-call process engineer, or to re-sequence the schedule so that another line picks up the job.
- If the line is still in FAI hold 60 minutes after the start time, the shift lead informs the production planner so that the day's output plan can be adjusted in Atlas.
- Line PC, scanner or OT network faults are raised in Beacon under MFG-OT at any time; the line lead does not wait for the 40-minute threshold before raising them.
- Any suspected mix of boards between jobs, or any case where the reflow profile check in step 19 finds a mismatch, is escalated immediately to the shift lead and QA, regardless of elapsed time.

The Penang factory manager, Nurul Aziz, receives a weekly summary of changeover times by line and delay code. Lines that miss the 25-minute target on more than a quarter of changeovers in a week are reviewed in the weekly manufacturing engineering meeting.

## 10. Delay codes

| Code | Meaning |
|---|---|
| D01 | Material not kitted or kitted incorrectly |
| D02 | Paste not thawed in time |
| D03 | Stencil not available, damaged or below tension |
| D04 | Feeder verification mismatch |
| D05 | Program not available on line PC |
| D06 | Oven not stable, nitrogen not within limit |
| D07 | FAI failure (SPI) |
| D08 | FAI failure (AOI false calls or defects) |
| D09 | QA inspector not available |
| D10 | Line PC, scanner or OT network fault (Beacon MFG-OT) |
| D11 | Other, describe on form |

## 11. Common problems

| Symptom | Likely cause | Action |
|---|---|---|
| Printer rejects stencil scan | Stencil ID does not match print program, or wrong revision letter | Check traveller, retrieve correct STN-<board>-<rev> stencil; do not override |
| Placement machine locked after feeder scan | Reel in feeder does not match Atlas setup sheet | Correct the feeder, rescan; do not bypass the lock |
| SPI shows low volume on fine-pitch pads | Stencil apertures blocked, wipe frequency wrong | Clean stencil, confirm wipe every 5 prints for TS4E-EN |
| SPI shows high volume or bridging | Stencil not seated, worn gasket, low tension | Reseat stencil, check tension at least 35 N/cm |
| Solder balling after reflow | Paste opened before 4-hour thaw, or paste open more than 24 hours | Scrap paste, use correctly thawed paste, repeat FAI |
| Oven does not reach stable on RP-TS4E-03 | Nitrogen supply, O2 not below 1000 ppm | Check nitrogen supply valve and flow, wait for O2 reading |
| AOI false call rate above 500 ppm | New component supplier, library not tuned | Call process engineer to re-tune AOI library, repeat FAI |
| Dull or grainy joints on HX-MB | Wrong profile loaded (lower peak than 245 °C) | Stop line, perform step 19 check, quarantine boards |
| Voiding on TS4E-EN ground pads | Air atmosphere instead of nitrogen | Confirm RP-TS4E-03 and nitrogen, quarantine affected boards |
| MSL reel floor life label expired | MSL 3 exposure over 168 hours | Remove reel, bake per mfg-msl-handling-procedure |
| Line PC cannot reach Atlas | OT network or line PC fault | Raise Beacon ticket in category MFG-OT |

## 12. Records

The following records are produced by each changeover:

- MFG-F-112 SMT Changeover Record, scanned and attached to the job in the Atlas shop-floor module.
- Feeder verification log, stored automatically by the Atlas shop-floor module.
- SPI and AOI results for the first article and first production boards, stored on the line PC and archived nightly.
- FAI board, kept on the FAI retention shelf for 30 days.

## 13. Related documents

- mfg-msl-handling-procedure (moisture sensitive device handling and baking).
- mfg-esd-control-standard (ESD controls, wrist strap checks).
- mfg-penang-smt-line-throughput (planned throughput by line and board family).
- mfg-postmortem-inc-2025-014-penang-smt2-reflow (postmortem that introduced step 19).
- it-beacon-ticket-categories (Beacon category MFG-OT).

## 14. Revision history

| Revision | Date | Author | Change |
|---|---|---|---|
| A | 2023-06-12 | Penang Manufacturing Engineering | First release for SMT-1 to SMT-3 |
| B | 2024-02-19 | Penang Manufacturing Engineering | Added SMT-4; added TS4E-EN parameters and nitrogen requirement |
| C | 2024-09-02 | Penang Manufacturing Engineering | Changeover target reduced from 35 to 25 minutes following SMED project; external and internal activities separated |
| D | 2025-04-07 | Penang Manufacturing Engineering | Added step 19 two-person reflow profile verification (INC-2025-014 action item) |
| E | 2025-08-04 | Penang Manufacturing Engineering | Annual review; AOI false call threshold set at 500 ppm; LF60-SB stencil thickness changed to 0.13 mm |
