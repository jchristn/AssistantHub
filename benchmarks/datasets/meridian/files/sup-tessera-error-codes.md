# Tessera Error Code Reference

Document ID: sup-tessera-error-codes | Version: 5 | Effective: 2025-08-25 | Owner: Meridian Customer Support (Tessera)

## Purpose

This reference lists the T-codes reported by Tessera TS-4 gateways and TS-4e edge nodes, as shown in the gateway local interface (Status > Events), in TesseraCloud on the device page, and in device.* webhook payloads. Support agents should record the T-code in every Tessera ticket raised in the Meridian Support Centre.

## Error codes

| Code | Source | Meaning | Typical cause | Resolution |
|---|---|---|---|---|
| T-301 | Node | Node not joined | Node woken outside a pairing window, beyond 4 hops, on an unsupported mesh channel, or asleep during a channel change | Open a pairing window (Fleet > Gateways > [gateway] > Nodes > Pair Nodes) and wake the node with the magnet for 3 seconds; check hop count and channel plan per the TS-4 Gateway Installation Guide section 11 |
| T-305 | Node | Node battery low | Estimated remaining capacity below 10% | Replace battery TS4E-BAT-01 (standard node) or return TS4E-NODE-3AX-Z2 for factory battery replacement |
| T-312 | Gateway | Clock drift | Gateway clock more than 2 seconds from NTP; NTP UDP 123 blocked | Allow outbound UDP 123 or configure a site NTP server at System > Time > NTP Servers |
| T-320 | Gateway | Backhaul down | No route to TesseraCloud over Ethernet or LTE | Check switch port, firewall rules for TCP 443 and 8883, APN settings, LTE signal; readings are buffered |
| T-327 | Gateway | Device certificate expired | Gateway offline or on firmware 5.1.x during the automatic renewal window | Re-provision the certificate with a one-time provisioning token, following the procedure in the TesseraCloud Administrator Guide (prod-tesseracloud-admin-guide), section 11.2 |
| T-333 | Gateway | Mesh interference | Sustained packet loss above 20% on the mesh channel | Change channel or enable the adaptive channel plan (gateway firmware 5.3.0) |
| T-340 | Gateway | Firmware mismatch | Node firmware not supported by the gateway firmware, for example node 2.8.0 on gateway 5.2.x | Check the Tessera Firmware Compatibility Matrix (prod-tessera-firmware-compatibility-matrix); update the gateway first, then the node |
| T-348 | Gateway | Buffer near capacity | Store-and-forward buffer above 90% after a long backhaul outage | Restore backhaul; oldest readings are overwritten once the buffer is full |
| T-355 | Node | Sensor self-test failed | Accelerometer self-test failure at wake-up | Power-cycle by removing the battery for 30 seconds; if repeated, raise an RMA |

## Priority guidance

- T-320 affecting a whole site, or T-327 on a gateway monitoring Criticality A assets, should normally be logged as P2, or P1 if the customer has lost all monitoring of safety-relevant machines. Priority definitions are in the Ticket Priority Codes document (sup-ticket-priority-codes).
- T-305 and T-312 are normally P4.

## Escalation to engineering

Escalate to Tessera second-line support with a diagnostics bundle (local UI System > Diagnostics > Bundle, gateway firmware 5.3.0) when T-340 persists after the compatibility matrix has been followed, or when T-355 appears on more than two nodes at the same site.
