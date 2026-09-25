# Postmortem: INC-2025-014 Wrong Reflow Profile on Penang SMT-2

Document ID: mfg-postmortem-inc-2025-014-penang-smt2-reflow. Incident: INC-2025-014. Severity: SEV2. Date of incident: 11 March 2025. Postmortem published: 21 March 2025. Author: Penang Manufacturing Engineering. Review chaired by: Nurul Aziz (Penang Factory Manager).

## Summary

On 11 March 2025, SMT-2 at the Penang factory ran 612 TS4E-EN boards (Tessera TS-4e edge node) through the reflow oven using profile RP-HX-07, the profile for the Halcyon main board HX-MB, instead of the correct profile RP-TS4E-03. RP-HX-07 peaks at 245 °C, and runs in air, while RP-TS4E-03 peaks at 240 °C and requires nitrogen with oxygen below 1000 ppm. The boards passed AOI but X-ray sampling at end of shift found excessive voiding under the TS4E-EN radio module and discoloured connector housings.

## Impact

- 612 boards affected: 540 scrapped, 72 reworked and re-inspected.
- SMT-2 stopped for 6 hours 15 minutes for investigation and containment.
- TS-4e edge node shipments to two customers delayed by 4 days.
- Estimated cost of scrap and overtime: MYR 186,000.

## Timeline (MYT, 11 March 2025)

| Time | Event |
|---|---|
| 13:40 | Changeover on SMT-2 from HX-MB to TS4E-EN begins |
| 13:58 | Operator selects oven recipe from the "recent recipes" list; RP-HX-07 remains loaded |
| 14:05 | Stencil STN-TS4E-EN-C loaded, feeders verified, SPI and first article inspection passed |
| 14:12 | Line released; production starts |
| 21:30 | End-of-shift X-ray sample shows voiding above limits |
| 21:45 | Line stopped; Beacon ticket in category MFG-OT raised; SEV2 declared |
| 22:20 | Oven log confirms RP-HX-07 was active since 13:58 |
| 23:10 | All boards since 14:12 quarantined |
| 04:00 (12 March) | Correct profile loaded, verified by two people, line restarted |

## Root causes

1. The oven recipe was selected manually and was not linked to the Atlas shop-floor work order.
2. The changeover runbook had no independent verification of the loaded reflow profile.
3. First article inspection checks paste and placement, but reflow defects under shielded modules are not visible to AOI.
4. The nitrogen supply was not required by an interlock, so running in air produced no alarm.

## Action items

| ID | Action | Owner | Due | Status |
|---|---|---|---|---|
| AI-1 | Add a two-person verification that the loaded reflow profile matches the traveller to the Penang SMT Line Changeover Runbook (added as step 19 in revision D, 7 April 2025) | Penang Manufacturing Engineering | 2025-04-07 | Done |
| AI-2 | Link oven recipe selection to the Atlas shop-floor work order barcode so the correct profile loads automatically | IT Business Applications and Penang OT Support | 2025-09-30 | In progress |
| AI-3 | Add a nitrogen interlock so RP-TS4E-03 cannot run with oxygen above 1000 ppm | Penang OT Support | 2025-05-31 | Done |
| AI-4 | Add X-ray sampling of the first 5 boards after any changeover to TS4E-EN | Quality Engineering | 2025-04-15 | Done |
| AI-5 | Retrain all SMT line leads and operators on changeover verification | Nurul Aziz | 2025-04-30 | Done |

## Lessons

A human-selected recipe with no system check will eventually be wrong. Verification must be independent of the person who made the selection.
