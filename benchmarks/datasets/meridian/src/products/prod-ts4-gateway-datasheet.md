# Tessera TS-4 Gateway Datasheet

Document ID: prod-ts4-gateway-datasheet | Version: 3.0 | Effective: 2025-08-25 | Owner: Tessera Product Engineering

## Overview

The Tessera TS-4 gateway is the site hub of the Tessera vibration-monitoring platform. It forms and manages a TesseraMesh wireless network of up to 64 TS-4e edge nodes, buffers their readings, and forwards them securely to TesseraCloud. The gateway is available in two variants that differ only in backhaul: TS4-GW-LTE, with LTE Cat-M1 cellular and Ethernet, and TS4-GW-ETH, with Ethernet only.

Typical applications include pump stations, cooling-water systems, HVAC plant rooms, conveyors and process fans, where running a cable to every sensor would be costly or impractical.

## Specifications

| Parameter | TS4-GW-LTE | TS4-GW-ETH |
|---|---|---|
| Backhaul | LTE Cat-M1 and 10/100 Ethernet | 10/100 Ethernet |
| Cellular bands | LTE B1, B2, B3, B4, B5, B8, B12, B13, B20, B25, B26, B28 | Not applicable |
| SIM | Nano-SIM (4FF), optional TS4-SIM-GLB | Not applicable |
| Mesh radio | TesseraMesh, IEEE 802.15.4, 2.4 GHz, channels 11-26 | TesseraMesh, IEEE 802.15.4, 2.4 GHz, channels 11-26 |
| Maximum nodes | 64 TS-4e | 64 TS-4e |
| Maximum mesh hops | 4 | 4 |
| Power | PoE IEEE 802.3at or 10-30 VDC | PoE IEEE 802.3af/at or 10-30 VDC |
| Typical consumption | 6.5 W (11 W peak) | 4.0 W |
| Store-and-forward buffer | 256 MB flash, approx. 30 days at 64 nodes | 256 MB flash, approx. 30 days at 64 nodes |
| Service port | RJ45 (SVC), local UI at https://192.168.50.1 | RJ45 (SVC), local UI at https://192.168.50.1 |
| Antennas | 2.4 GHz mesh (5 dBi) and LTE (3 dBi), N-type | 2.4 GHz mesh (5 dBi), N-type |
| Operating temperature | -30 to +70 C | -30 to +70 C |
| Ingress protection | IP66 | IP66 |
| Enclosure | UV-stabilised polycarbonate, 240 x 180 x 90 mm | UV-stabilised polycarbonate, 240 x 180 x 90 mm |
| Weight (with bracket) | 1.9 kg | 1.8 kg |
| Hazardous area | Safe area only | Safe area only |
| Current firmware | 5.3.0 | 5.3.0 |
| Warranty | 36 months (from 1 April 2025) | 36 months (from 1 April 2025) |

Note that the TS4-GW-ETH can run from an IEEE 802.3af (PoE Type 1) switch port because it has no cellular modem; the TS4-GW-LTE requires IEEE 802.3at (PoE+).

## Security

- Device identity: X.509 certificate issued at claim, valid 3 years, renewed automatically 60 days before expiry on firmware 5.2.0 and later.
- Transport: TLS 1.3 to TesseraCloud over TCP 8883 (MQTT) and TCP 443 (HTTPS). No inbound ports.
- Mesh: AES-128 CCM link encryption with a per-network key.
- Firmware: signed images, A/B partitions with automatic rollback after three failed boots.

## Certifications

CE (RED 2014/53/EU), UKCA, FCC Part 15 and Part 22/24/27 (LTE variant), ISED Canada, SIRIM Malaysia, IEC 62368-1.

## Ordering information

| SKU | Description |
|---|---|
| TS4-GW-LTE | TS-4 gateway, LTE Cat-M1 and Ethernet, with mesh and LTE antennas and bracket TS4-BRK-01 |
| TS4-GW-ETH | TS-4 gateway, Ethernet only, with mesh antenna and bracket TS4-BRK-01 |
| TS4-SIM-GLB | Global IoT SIM, pre-activated, 5-year data plan |
| TS4-LPA-01 | Coaxial lightning arrestor, N-type |
| TS4-GLD-KIT-02 | Cable gland kit with EMC gland (replaces TS4-GLD-KIT-01) |
| TS4-ANT-MESH | Spare 2.4 GHz mesh antenna, 5 dBi |

Installation procedures are described in the Tessera TS-4 Gateway Installation Guide (prod-ts4-gateway-installation-guide). TesseraCloud subscriptions (TC-SUB-STD and TC-SUB-ENT) are sold separately, per monitored asset per year.
