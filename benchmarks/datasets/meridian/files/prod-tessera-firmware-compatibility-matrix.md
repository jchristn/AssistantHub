# Tessera Firmware Compatibility Matrix

Document ID: prod-tessera-firmware-compatibility-matrix | Version: 7 | Effective: 2025-08-18 | Owner: Tessera Product Engineering

## Purpose

This matrix states which TS-4e edge node firmware versions are supported with which TS-4 gateway firmware versions, and the minimum gateway firmware accepted by TesseraCloud. It is updated with every gateway or node firmware release. This edition reflects the release of gateway firmware 5.3.0 on 18 August 2025.

## Gateway and node compatibility

| TS-4e node firmware | Gateway 5.1.x | Gateway 5.2.x | Gateway 5.3.0 |
|---|---|---|---|
| 2.6.1 | Supported | Supported | Supported (deprecated) |
| 2.7.0 | Not supported | Supported | Supported |
| 2.8.0 | Not supported | Not supported | Supported |

Notes:

- "Supported (deprecated)" means the combination works but will be removed in the next gateway major release. Plan to update nodes on 2.6.1 to 2.8.0.
- Node firmware 2.8.0 is mandatory for the Zone 2 edge node TS4E-NODE-3AX-Z2. Because 2.8.0 is only supported by gateway 5.3.0, any gateway serving Zone 2 nodes must run 5.3.0.
- A node running firmware the gateway does not support will fail to join the mesh and the gateway will report T-340 (firmware mismatch). If the node was never paired, it may report T-301 (node not joined) instead.

## TesseraCloud requirements

| Date | Minimum gateway firmware accepted by TesseraCloud |
|---|---|
| Until 30 September 2025 | 5.1.0 |
| From 1 October 2025 | 5.2.0 |

Gateways below the minimum still connect and upload data but are shown as Degraded and cannot receive configuration changes or certificate auto-renewal.

## Feature availability by gateway firmware

| Feature | 5.1.x | 5.2.x | 5.3.0 |
|---|---|---|---|
| Automatic certificate renewal | No | Yes | Yes |
| Provisioning token re-provisioning (T-327 recovery) | No | Yes | Yes |
| Adaptive mesh channel plan | No | No | Yes |
| MQTT over WebSocket (proxy support) | No | Yes | Yes |
| Buffer near-capacity event T-348 | No | Yes | Yes |

## Recommended update order

1. Update the gateway first, to 5.3.0.
2. Confirm the gateway is Online and all nodes have rejoined.
3. Update nodes to 2.8.0 through a firmware campaign.

Never update nodes to 2.8.0 before their gateway is on 5.3.0. TesseraCloud blocks this in firmware campaigns, but locally applied node updates using the TesseraSurvey app are not checked.

## Related documents

- TS-4 Gateway Firmware 5.3.0 Release Notes (prod-tessera-gateway-firmware-5-3-0-release-notes)
- Tessera Error Code Reference (sup-tessera-error-codes)
- Tessera TS-4 Gateway Installation Guide (prod-ts4-gateway-installation-guide)
