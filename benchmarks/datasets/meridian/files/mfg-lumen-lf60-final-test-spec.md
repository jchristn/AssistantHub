# Lumen LF-60 and LF-60P Final Test Specification

Document ID: mfg-lumen-lf60-final-test-spec. Owner: Quality Engineering, with Lumen Engineering (Rotterdam). Revision: F. Effective date: 16 June 2025. Applies to: final test of LF-60 and LF-60P optical-flow meters at the Penang factory, test stations FT-LUM-01 and FT-LUM-02.

## Purpose

This specification defines the final tests every Lumen meter must pass before it is packed. The LF-60 and LF-60P look similar and share the LF60-SB sensor board, but their test limits differ. Always check the model on the traveller before selecting a test program.

## Test programs

| Model | Test program | Firmware loaded at test |
|---|---|---|
| LF-60 | TP-LF60-07 | 1.8.2 |
| LF-60P | TP-LF60P-04 | 1.9.0 |

The test station refuses to start if the firmware reported by the meter does not match the program.

## Test sequence and limits

| Test | LF-60 | LF-60P |
|---|---|---|
| Hydrostatic pressure test | 24 bar (1.5 x PN16) held 60 seconds | 60 bar (1.5 x PN40) held 120 seconds |
| Flow accuracy test points | 0.5, 2, 6 and 11 m/s | 0.5, 2, 6, 11 and 14 m/s |
| Accuracy acceptance at test | Within 0.5% of reference at every point | Within 0.4% of reference at every point |
| Published accuracy (datasheet) | 1.0% of reading | 0.75% of reading |
| Zero stability | Below 0.005 m/s over 60 seconds | Below 0.003 m/s over 60 seconds |
| 4-20 mA output check | 4.00 and 20.00 mA within 0.02 mA | 4.00 and 20.00 mA within 0.02 mA |
| HART communication | Not applicable | HART 7 device identification read |
| Modbus defaults set | Address 1, 9600 baud, 8N1 | Address 1, 19200 baud, 8E1 |
| Insulation resistance | Above 100 MOhm at 500 V DC | Above 100 MOhm at 500 V DC |
| Ingress seal check | IP65 gasket visual check | IP67 gasket visual check and pressure decay test |

The acceptance limits at test are deliberately tighter than the published datasheet accuracy to leave margin for installation effects.

## Reference rig

Flow accuracy is measured against the gravimetric reference rig on station FT-LUM-01 (DN25 to DN100) or FT-LUM-02 (DN125 to DN200). LF-60P meters above DN150 do not exist; DN200 is LF-60 only. Reference rigs are calibrated every 12 months by an accredited laboratory.

## Failures

- A meter that fails any accuracy point may be retested once after re-zeroing. A second failure sends the meter to engineering analysis with reason code ACC.
- A hydrostatic failure is never retested; the meter body is quarantined with reason code HYD.
- Failures are recorded in the Atlas quality module against the serial number.

## Records

The test certificate is generated automatically and shipped with the meter. Test data is retained for 10 years in the Atlas quality module.
