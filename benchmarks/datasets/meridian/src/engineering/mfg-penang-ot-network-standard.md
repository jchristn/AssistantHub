# Penang OT Network Security Standard

Document ID: mfg-penang-ot-network-standard. Owner: IT Security (Head of IT Security: Mei-Ling Tan) and Penang OT Support. Approved by: Nurul Aziz, Penang Factory Manager. Effective date: 5 May 2025.

## Scope

This standard covers the operational technology (OT) network at the Penang factory: SMT line PCs and machine controllers, reflow ovens, SPI and AOI systems, final test and calibration stations, and line label printers. The OT network uses 10.41.0.0/16 and is separate from the Penang corporate network (10.40.0.0/16).

## Network zones

| Zone | Subnet | Contents |
|---|---|---|
| OT core services | 10.41.0.0/24 | OT DNS, time server, historian |
| SMT-1 | 10.41.1.0/24 | Line PC, printer, SPI, placement, oven, AOI |
| SMT-2 | 10.41.2.0/24 | Line PC, printer, SPI, placement, oven, AOI |
| SMT-3 | 10.41.3.0/24 | Line PC, printer, SPI, placement, oven, AOI |
| SMT-4 | 10.41.4.0/24 | Line PC, printer, SPI, placement, oven, AOI |
| Final test and calibration | 10.41.10.0/24 | FT-LUM-01, FT-LUM-02, CAL-PEN-01 to CAL-PEN-06 |
| Label printers | 10.41.20.0/24 | PEN-SMT-LABEL01 to PEN-SMT-LABEL04, PEN-FT-LABEL01 |
| HIL test rigs | 10.41.30.0/24 | Rigs attached to forge-runner-pen-01 to forge-runner-pen-04 |
| OT management | 10.41.250.0/24 | Jump host interface, monitoring sensors |

## Allowed traffic between corporate and OT

| From | To | Port | Purpose |
|---|---|---|---|
| OT line PCs | Atlas application servers (atlas-app-prd-01 to 04) | 443 | Atlas shop-floor module (setup sheets, traceability) |
| OT historian | Corporate log collector | 6514 | Syslog over TLS |
| forge-runner-pen hosts | HIL test rigs | 22 | Test orchestration |
| ot-jump-pen-01 | OT zones | 3389, 22 | Engineering access |

All other traffic between the networks is denied. The OT network has no direct internet access.

## Controls

- USB storage is disabled on all OT PCs. Recipe and program files are transferred only through the managed file transfer share on ot-jump-pen-01, where files are malware-scanned.
- OT PCs are patched quarterly in the Sunday maintenance window (08:00 to 14:00 MYT) after vendor qualification. Critical vulnerabilities with no qualified patch are covered by a SEC-EXCEPTION with compensating controls.
- Active vulnerability scanning of the OT network is forbidden; IT Security uses passive monitoring only (see the Vulnerability Management Standard).
- Any new device on 10.41.0.0/16 raises the alert OT_NEW_DEVICE_DETECTED.
- OT logs are retained for 12 months.

## Remote access

- Staff: VPN to vpn-pen.meridian-instruments.com, then ot-jump-pen-01 with a FIDO2 key and an approved Beacon MFG-OT ticket. Sessions last a maximum of 4 hours and are recorded.
- Equipment vendors: escorted sessions only, started by Penang OT Support through the privileged access vault, maximum 2 hours per session. Vendors never receive standing credentials.

## Changes

Changes to OT systems follow the Change Management Procedure v2 and require sign-off from the Penang factory manager or her delegate.
