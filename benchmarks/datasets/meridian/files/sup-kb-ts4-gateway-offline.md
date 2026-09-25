# KB: TS-4 Gateway Shows Offline in TesseraCloud

Document ID: sup-kb-ts4-gateway-offline | Article: KB-TS-0142 | Published: 2025-09-08 | Owner: Meridian Customer Support (Tessera)

## Symptom

A TS-4 gateway appears as Offline on the Fleet > Gateways page in TesseraCloud, and assets at the site stop updating. The customer may also receive a device.offline webhook or email notification.

## Background

The gateway sends a heartbeat every 60 seconds. TesseraCloud marks it Offline after 10 consecutive missed heartbeats, so a gateway is shown Offline roughly 10 minutes after it loses contact. While offline, the gateway keeps collecting readings from its TS-4e nodes and stores them in its 256 MB buffer (about 30 days at 64 nodes). No data is lost unless the outage exceeds the buffer capacity.

## Diagnosis

Check the status LED on the gateway first:

- Slow amber flash: the gateway is running but cannot reach TesseraCloud. Go to "Backhaul problems" below.
- Red flash: certificate or authentication failure, usually T-327. Go to "Certificate problems".
- No LED: no power. Check the PoE switch port (the TS4-GW-LTE needs IEEE 802.3at PoE+) or the 10-30 VDC supply and fuse.
- Solid red: hardware fault. Raise an RMA.

### Backhaul problems

1. Connect a laptop to the SVC port and open https://192.168.50.1.
2. Open Status > Cloud and Status > Events. T-320 confirms backhaul loss.
3. For Ethernet, confirm the switch port is up and outbound TCP 443 and TCP 8883 to *.tesseracloud.meridian-instruments.com are allowed. If the site recently introduced a proxy, enable MQTT over WebSocket at Network > Proxy.
4. For LTE, check the APN at Network > Cellular > APN (meridian.iot for the TS4-SIM-GLB) and the signal strength; RSRP should be better than -110 dBm.
5. If the event log also shows T-312, fix NTP access (UDP 123), since a large clock error prevents TLS connections.

### Certificate problems

A red flashing LED with T-327 means the device certificate has expired. Follow the re-provisioning procedure in the TesseraCloud Administrator Guide (prod-tesseracloud-admin-guide), section 11.2, using a one-time provisioning token valid for 24 hours.

## Common causes seen by Support in 2025

| Cause | Share of cases |
|---|---|
| Firewall change blocking TCP 8883 | 34% |
| Power interruption or PoE budget exceeded | 22% |
| LTE coverage or SIM deactivated | 18% |
| Expired certificate (T-327) | 11% |
| Other | 15% |

## Escalation

If the gateway still shows Offline after these checks, raise a ticket in the Meridian Support Centre with the gateway serial number (TS4G-YYWW-NNNNN), the T-codes from Status > Events and a diagnostics bundle (System > Diagnostics > Bundle, firmware 5.3.0). A site-wide loss of monitoring on safety-relevant machines qualifies as P1; otherwise log as P2.
