# Lumen Modbus Register Map (LF-60 and LF-60P)

Document ID: prod-lumen-modbus-register-map | Version: 1 | Effective: 2025-03-17 | Owner: Lumen Engineering, Rotterdam | Applies to: LF-60 firmware 1.8.2, LF-60P firmware 1.9.0

## 1. Scope

This document lists the Modbus RTU registers of the Lumen LF-60 and LF-60P flow meters. Both meters use the same register layout; registers marked LF-60P only return exception code 02 (illegal data address) when read from an LF-60.

## 2. Communication settings

| Setting | LF-60 default | LF-60P default | Range |
|---|---|---|---|
| Slave address | 1 | 1 | 1 to 247 |
| Baud rate | 9600 | 19200 | 9600, 19200, 38400 |
| Framing | 8N1 | 8E1 | 8N1, 8E1, 8O1, 8N2 |
| Response delay | 5 ms | 5 ms | 0 to 100 ms |

Settings can be changed on the local display under Menu > Setup > Outputs > Modbus or by writing holding registers 40001 to 40003. New communication settings take effect after the meter is power cycled or after 1 is written to register 40030.

## 3. Data formats

Floating-point values are IEEE 754 single precision (float32) occupying two consecutive registers. Totalisers are float64 occupying four registers. Word order is big-endian (ABCD) on firmware 1.8.0 and later. Meters on firmware earlier than 1.8.0 used swapped word order (CDAB); upgrade these meters or configure the master accordingly.

Register numbers below use the conventional 3xxxx (input registers, function code 04) and 4xxxx (holding registers, function codes 03, 06 and 16) notation. The protocol address is the register number minus 30001 or 40001.

## 4. Input registers (function code 04)

| Register | Name | Type | Unit | Availability |
|---|---|---|---|---|
| 30001 | Volume flow rate | float32 | selected flow unit (default m3/h) | Both |
| 30003 | Flow velocity | float32 | m/s | Both |
| 30005 | Forward totaliser | float64 | selected volume unit (default m3) | Both |
| 30009 | Reverse totaliser | float64 | selected volume unit | Both |
| 30013 | Process pressure | float32 | bar(g) | LF-60P only |
| 30015 | Fluid temperature | float32 | °C | Both |
| 30017 | Signal quality | uint16 | % | Both |
| 30018 | Active error code | uint16 | numeric part of L-code, for example 204 | Both |
| 30019 | HART status byte | uint16 | bit field | LF-60P only |
| 30020 | Electronics temperature | float32 | °C | Both |
| 30022 | Firmware version | uint16 x 3 | major, minor, patch | Both |
| 30025 | Verification code | uint16 | four-digit code | LF-60P only |

When more than one error is active, register 30018 returns the lowest-numbered code. A value of 0 means no active error. Error code meanings are listed in the Lumen Error Code Reference (sup-lumen-error-codes).

## 5. Holding registers (function codes 03, 06, 16)

| Register | Name | Type | Default LF-60 | Default LF-60P | Availability |
|---|---|---|---|---|---|
| 40001 | Slave address | uint16 | 1 | 1 | Both |
| 40002 | Baud rate code (0 = 9600, 1 = 19200, 2 = 38400) | uint16 | 0 | 1 | Both |
| 40003 | Framing code (0 = 8N1, 1 = 8E1, 2 = 8O1, 3 = 8N2) | uint16 | 0 | 1 | Both |
| 40004 | Flow unit code (0 = m3/h, 1 = l/s, 2 = l/min, 3 = US gpm) | uint16 | 0 | 0 | Both |
| 40005 | Damping time constant | float32 | 3.0 s | 2.0 s | Both |
| 40007 | Low-flow cut-off | float32 | 0.02 m/s | 0.02 m/s | Both |
| 40009 | 4-20 mA upper range value | float32 | size dependent | size dependent | Both |
| 40011 | Fail-safe current | float32 | 3.6 mA | 3.6 mA | Both |
| 40013 | Totaliser reset (write 0xA5A5) | uint16 | - | - | Both |
| 40020 | HART poll address | uint16 | n/a | 0 | LF-60P only |
| 40021 | Pressure unit (0 = bar, 1 = kPa, 2 = psi) | uint16 | n/a | 0 | LF-60P only |
| 40030 | Apply communication settings (write 1) | uint16 | - | - | Both |

Holding registers are write-protected when DIP switch S3 on the terminal board is ON; writes then return exception code 04.

## 6. Polling recommendations

Poll process values no faster than once every 200 ms. For SCADA systems polling many meters on one RS-485 segment, read registers 30001 to 30018 in a single request of 18 registers rather than reading each value separately. A maximum of 32 meters can share one RS-485 segment without a repeater.
