# Lumen Error Code Reference

Document ID: sup-lumen-error-codes | Version: 1 | Effective: 2025-03-20 | Owner: Customer Support, Technical Support Engineering

## Purpose

This reference lists the error codes shown by Lumen LF-60 and LF-60P flow meters on the local display, over Modbus (input register 30018) and, on the LF-60P, over HART. Support engineers should use it for first-line diagnosis before raising a ticket with the Rotterdam Lumen engineering team.

Active errors are shown on the display under Menu > Diagnostics > Active Errors. The error history (last 50 events with timestamps) is under Menu > Diagnostics > Error Log.

## Error codes

| Code | Meaning | Models | Output behaviour | Recommended fix |
|---|---|---|---|---|
| L-201 | Empty pipe detected | LF-60, LF-60P | Flow forced to zero | Confirm the pipe is full. Check that the meter is not at a high point and that vertical installations have upward flow (installation manual section 7). |
| L-204 | Low signal quality (below 60%) | LF-60, LF-60P | Normal, warning only | Check grounding and bonding: on non-conductive or lined pipe, grounding rings are mandatory (LF-60P installation manual section 9.2). Also check for protruding gaskets and window fouling. |
| L-209 | Flow over range | LF-60, LF-60P | Output saturates at 20.5 mA | Velocity above 12 m/s (LF-60) or 15 m/s (LF-60P). Check the 4-20 mA upper range value and the meter size selection. |
| L-215 | Coil drive fault in the optical source driver | LF-60, LF-60P | Fail-safe current | Power cycle. If the error returns within 24 hours, the electronics module must be replaced under RMA. |
| L-218 | Optical window contamination | LF-60, LF-60P | Normal, warning only | Inspect and clean the sapphire windows (LF-60P installation manual section 12.1). |
| L-222 | HART communication fault | LF-60P only | Normal | Check that loop resistance is at least 250 ohm (LF-60P installation manual section 10.1). On firmware 1.9.0 the error clears automatically after 30 seconds of good communication. |
| L-230 | Fluid temperature out of range | LF-60, LF-60P | Normal, warning only | Fluid outside -10 to +80 °C (LF-60) or -20 to +120 °C (LF-60P). Check the process; sustained operation outside the range voids the warranty. |

## Escalation guidance

- L-215 is a hardware fault. Raise a P3 ticket unless the meter is used for custody transfer or billing, in which case raise a P2.
- L-204 that persists after grounding has been verified should be escalated with a screenshot or export of the signal quality trend over at least 24 hours.
- Repeated L-201 on a line that is known to be full usually indicates air entrainment; ask the customer for the installation photographs.

When raising a ticket in the Meridian Support Centre, always record the meter model, serial number, firmware version (1.8.2 for LF-60, 1.9.0 for current LF-60P) and the value of Modbus register 30017 (signal quality).
