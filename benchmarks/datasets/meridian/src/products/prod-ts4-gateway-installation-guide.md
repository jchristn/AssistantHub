# Tessera TS-4 Gateway Installation Guide

Document ID: prod-ts4-gateway-installation-guide | Version: 4.2 | Effective: 2025-08-25 | Owner: Tessera Product Engineering, Meridian Instruments Ltd

## 1. About this guide

This guide describes how to survey, mount, power, connect and commission the Tessera TS-4 gateway, and how to pair TS-4e edge nodes to it. It applies to both gateway variants: TS4-GW-LTE (LTE Cat-M1 plus Ethernet backhaul) and TS4-GW-ETH (Ethernet-only backhaul). Where the procedure differs between the two variants, the difference is called out explicitly.

The procedures in this edition assume gateway firmware 5.3.0, released on 18 August 2025. Earlier firmware (5.1.x and 5.2.x) uses the same menu structure for most tasks, but the mesh channel planner described in section 11 and the gland kit guidance in section 12 were introduced or revised with 5.3.0. For the firmware versions supported with each TS-4e edge node release, refer to the Tessera Firmware Compatibility Matrix (prod-tessera-firmware-compatibility-matrix).

This guide is intended for Meridian field engineers, certified Tessera partners and customer maintenance technicians who have completed the Tessera Installer module. Only qualified personnel should open the gateway enclosure or work on the power supply.

## 2. Safety information

The TS-4 gateway is not certified for use in hazardous areas. It must be installed in a non-classified (safe) area. TS-4e edge nodes in the Zone 2 variant (TS4E-NODE-3AX-Z2) may be installed inside a Zone 2 area, but the gateway they report to must remain outside the classified boundary. Radio range through a hazardous-area boundary wall should be verified during the site survey.

Observe the following precautions:

- Isolate the power supply before opening the enclosure lid. Where the gateway is powered by Power over Ethernet, disconnect the Ethernet cable at the switch or injector.
- Do not install the LTE antenna within 20 cm of locations normally occupied by people, in order to comply with RF exposure limits.
- When working at height, follow the site's permit-to-work and fall-protection rules. The gateway weighs 1.9 kg with the mounting bracket fitted.
- The internal lithium coin cell (real-time clock backup) is not user-replaceable.

## 3. Site survey

A site survey is required before any gateway is mounted. The survey confirms backhaul availability, mesh radio coverage and a suitable mounting position. Meridian recommends using the TesseraSurvey mobile app with a demo gateway from a Meridian demo kit, although a manual survey is acceptable for small installations.

Check the following during the survey:

- Backhaul: for TS4-GW-LTE, confirm LTE Cat-M1 coverage with a minimum RSRP of -110 dBm at the proposed antenna position. For TS4-GW-ETH, confirm a switch port within 100 m of cable length and whether it supplies PoE (IEEE 802.3at).
- Firewall: the gateway requires outbound TCP 443 (HTTPS) and TCP 8883 (MQTT over TLS) to *.tesseracloud.meridian-instruments.com, and outbound UDP 123 for NTP. No inbound ports are required.
- Mesh coverage: TesseraMesh operates in the 2.4 GHz band using IEEE 802.15.4. Typical line-of-sight range between nodes is 80 m indoors in open plant rooms and 200 m outdoors. Dense steelwork, tank walls and concrete bunds reduce range significantly.
- Node count: one gateway supports up to 64 TS-4e edge nodes. For more nodes, plan an additional gateway per group of 64 nodes and assign each to a separate mesh network ID.
- Wi-Fi coexistence: record the channels used by existing 2.4 GHz Wi-Fi access points so that the mesh channel can be planned to avoid them (see section 11).

Record the survey results on form TS-F-012 (Tessera Site Survey Record) and attach it to the installation work order.

## 4. Mounting the gateway

The TS-4 gateway is supplied with a stainless steel wall and pole bracket (part TS4-BRK-01). The enclosure is rated IP66 and may be installed outdoors, but should be shielded from direct sunlight where ambient temperatures regularly exceed 50 C.

To mount the gateway on a wall:

1. Choose a position at least 2 m above floor level and at least 1 m away from large metal obstructions, with the antenna ports facing upwards or sideways, never downwards.
2. Mark and drill the four bracket holes using the bracket as a template. On concrete or masonry, use M8 anchors rated for at least 0.5 kN pull-out load.
3. Fix the bracket with the four M8 bolts supplied and tighten them to 12 N m. Do not exceed 15 N m, as over-tightening can distort the bracket and stress the enclosure base.
4. Hang the gateway on the bracket hooks and secure it with the two M5 locking screws, tightened to 3 N m.
5. Fit the earth bonding lead from the enclosure earth stud to a local protective earth point.

For pole mounting, use the two stainless steel band clamps included with TS4-BRK-01. The bracket accepts poles from 40 mm to 110 mm in diameter.

## 5. Antennas

Each gateway ships with a 2.4 GHz mesh antenna (TS4-ANT-MESH, 5 dBi omnidirectional). The TS4-GW-LTE variant additionally ships with an LTE antenna (TS4-ANT-LTE, 3 dBi, 698-2690 MHz). Both antennas use N-type connectors.

- Hand-tighten each N-type connector, then tighten to 1.7 N m using a torque spanner. Over-tightening damages the connector centre pin.
- Where antennas are mounted remotely, use low-loss cable (LMR-400 or equivalent) and keep the run under 10 m. Every remote antenna run must include a coaxial lightning arrestor (TS4-LPA-01) bonded to the site earth.
- Seal outdoor connector joints with self-amalgamating tape, then a layer of UV-resistant PVC tape.
- The mesh and LTE antennas must be at least 30 cm apart to avoid desensitisation of the mesh receiver.

## 6. Power

The gateway can be powered in two ways. Do not connect both sources at the same time.

- Power over Ethernet: the Ethernet port (ETH0) accepts IEEE 802.3at (PoE+, Type 2) power. IEEE 802.3af (Type 1) is not sufficient when the LTE modem is active. Maximum cable length is 100 m.
- DC supply: the terminal block TB1 accepts 10-30 VDC. Typical consumption is 6.5 W, peak 11 W during LTE transmission. Use a supply rated for at least 15 W and a 2 A slow-blow fuse in the positive line.

The gateway includes a supercapacitor hold-up that keeps the processor running for approximately 20 seconds after power loss, which is sufficient to flush buffered readings to flash memory and log a clean shutdown event. A power loss that exceeds this period is logged as event code T-320 only if backhaul is also lost; a clean power loss is reported in TesseraCloud as "Gateway powered down".

## 7. Backhaul configuration

### 7.1 Ethernet

By default ETH0 uses DHCP. To set a static address, open the local web interface (section 8) and go to Network > Ethernet > IPv4, select Static, and enter the address, netmask, gateway and DNS servers. Proxy servers are supported for HTTPS traffic (Network > Proxy), but the MQTT connection on TCP 8883 cannot be proxied; if the site requires all traffic through a proxy, enable "MQTT over WebSocket" on the same page, which tunnels telemetry over TCP 443.

### 7.2 LTE (TS4-GW-LTE only)

The TS4-GW-LTE uses a nano-SIM (4FF) inserted in the slot under the lid, beside the terminal block. Meridian supplies a pre-activated global IoT SIM (TS4-SIM-GLB) as an option; customer-supplied SIMs must support LTE Cat-M1 on the local network.

1. Isolate power and open the lid.
2. Insert the SIM with the notched corner towards the hinge until it clicks.
3. Close the lid, restore power and open the local interface.
4. Go to Network > Cellular > APN and enter the APN, and the username and password if the operator requires them. The TS4-SIM-GLB uses the APN "meridian.iot".
5. Set the backhaul priority at Network > Backhaul Priority. The default is Ethernet first, with LTE as failover. Failover occurs after 90 seconds without an Ethernet route to TesseraCloud.

## 8. Local web interface

The local web interface is used for initial configuration and for troubleshooting when the gateway cannot reach TesseraCloud.

- Connect a laptop to the service port (the second RJ45 port, labelled SVC). The service port runs a DHCP server and assigns the laptop an address in 192.168.50.0/24.
- Browse to https://192.168.50.1. The certificate is self-signed; accept the browser warning.
- Log in as user "admin" with the unique initial password printed on the rating label inside the lid. You will be required to change it at first login. The new password must be at least 12 characters.
- The session times out after 15 minutes of inactivity.

The local interface is disabled on the ETH0 (backhaul) port by default. It can be enabled at System > Access > Local UI on ETH0, but Meridian advises against this on shared corporate networks.

## 9. Registering the gateway in TesseraCloud

Before the gateway can forward data, it must be claimed into a TesseraCloud tenant.

1. In TesseraCloud, a user with the Site Admin or Tenant Owner role opens Fleet > Gateways > Add Gateway.
2. Enter the gateway serial number (format TS4G-YYWW-NNNNN, printed on the rating label) and the 8-character claim code on the same label.
3. Select the site the gateway belongs to. Gateways can later be moved between sites using Transfer Site, described in the TesseraCloud Administrator Guide.
4. The gateway downloads its device certificate and appears as "Online" within approximately 2 minutes.

If the gateway does not appear online, check the Status > Cloud page in the local interface. The most common causes are blocked outbound TCP 8883, an incorrect APN, or incorrect system time preventing TLS validation (see section 13).

## 10. Commissioning the mesh and pairing edge nodes

### 10.1 Creating the mesh network

At first boot the gateway generates a random 16-bit mesh network ID and a network key. The mesh network ID is shown at Mesh > Overview. If several gateways are installed on the same site, confirm that each has a distinct network ID; this is automatic in almost all cases, but a collision can be resolved with Mesh > Overview > Regenerate Network ID. Regenerating the ID unpairs all nodes.

### 10.2 Pairing TS-4e edge nodes

TS-4e edge nodes ship in a deep-sleep state to preserve their battery. To pair a node:

1. In TesseraCloud open the site, then Fleet > Gateways > [gateway] > Nodes > Pair Nodes. This opens a 10-minute pairing window on the gateway. Alternatively, open the window locally at Mesh > Pairing > Open Window.
2. Wake each node by holding a magnet (supplied with the node) against the marked spot on the node housing for 3 seconds. The node LED flashes green while searching.
3. The node joins, the LED shows a solid green for 5 seconds, and the node appears in the node list with its serial number (TS4E-YYWW-NNNNN).
4. Assign each node to an asset and measurement point in TesseraCloud.

A node that fails to join within the window reports T-301 (node not joined) once the gateway sees its beacon. Refer to the Tessera Error Code Reference (sup-tessera-error-codes) for causes. The most common cause is a node firmware version that the gateway does not support; for example, a node running 2.8.0 will not join a gateway on 5.2.x.

### 10.3 Mounting edge nodes

TS-4e nodes mount on an M6 stud. The stud must be tightened into the machine surface first, then the node is screwed onto the stud and tightened to 6 N m. Detailed mounting guidance, including spot-facing and adhesive mounting pads, is in the TS-4e datasheet (prod-ts4e-edge-node-datasheet).

## 11. Mesh radio planning and advanced mesh settings

The default mesh configuration suits most installations. The settings in this section should only be changed after a site survey has shown a need.

### 11.1 Channel plan

TesseraMesh uses IEEE 802.15.4 channels 11 to 26 in the 2.4 GHz band. The factory default channel is 15, chosen because it falls between Wi-Fi channels 1 and 6. If the survey shows heavy Wi-Fi use on channels 1 and 6, channel 25 or 26 usually gives the cleanest spectrum; note that channel 26 is restricted to reduced transmit power in some regions and is not available on units sold in certain markets.

To change the channel, go to Mesh > Radio > Channel Plan in the local interface, or Fleet > Gateways > [gateway] > Mesh > Radio in TesseraCloud. Select either a fixed channel or "Adaptive", which lets the gateway move between up to four channels you nominate. When the channel changes, the gateway broadcasts the change to all paired nodes over the next three beacon cycles, which takes up to 3 minutes. Nodes that are asleep or out of range during this period will reappear with T-301 and must be re-paired.

### 11.2 Hop count and topology

TesseraMesh is a self-forming mesh. Nodes may relay messages for other nodes. The maximum hop count from any node to the gateway is 4. A node that can only reach the gateway through more than 4 hops will not join, and will report T-301. Meridian recommends designing for no more than 3 hops, leaving a spare hop for route recovery when a relay node fails.

For network stability, no more than 24 nodes should connect directly to the gateway (first hop). Above that number, some nodes should be positioned so that they route through neighbours.

### 11.3 Transmit power

The mesh transmit power is set at Mesh > Radio > TX Power. The default is +8 dBm. It may be raised to a maximum of +10 dBm where regional regulations permit. Raising transmit power increases range but also reduces TS-4e battery life on nodes that relay traffic.

### 11.4 Beacon and reporting intervals

The gateway beacon interval is fixed at 60 seconds. The node reporting interval is set per asset in TesseraCloud. At the default of 1 sample per hour, the TS-4e battery lasts 5 years. Faster reporting intervals shorten battery life proportionally.

## 12. Enclosure, cable entries and environmental limits

### 12.1 Cable glands

The gateway enclosure has three cable entries on the underside. From left to right as viewed from the front:

- Entry 1: M20 x 1.5, for the Ethernet (ETH0) cable, cable diameter 6-12 mm.
- Entry 2: M20 x 1.5, for the DC power cable, cable diameter 6-12 mm.
- Entry 3: M16 x 1.5, for the service or auxiliary I/O cable, cable diameter 4-8 mm.

Unused entries must be closed with the blanking plugs provided. Tighten the gland cap nuts to 4 N m for M20 glands and 2.5 N m for M16 glands to maintain the IP66 rating. From firmware 5.3.0 onwards, the gland kit TS4-GLD-KIT-02 replaces the older TS4-GLD-KIT-01; the new kit adds an EMC gland for M20 entry 1, which is recommended where the Ethernet cable runs alongside variable-speed drive cabling.

### 12.2 Environmental limits

- Operating temperature: -30 to +70 C.
- Storage temperature: -40 to +85 C.
- Relative humidity: 5-95%, non-condensing.
- Ingress protection: IP66 with lid closed and all entries sealed.
- Enclosure: UV-stabilised polycarbonate, 240 x 180 x 90 mm excluding antennas.

A breather valve in the enclosure base equalises pressure. Do not paint over or block it.

## 13. Time, buffering and connectivity behaviour

### 13.1 Time synchronisation

The gateway synchronises its clock using NTP, by default from pool servers operated for TesseraCloud (time.tesseracloud.meridian-instruments.com). A custom NTP server can be entered at System > Time > NTP Servers. If the clock drifts by more than 2 seconds relative to NTP, the gateway logs T-312 (clock drift). TLS connections to TesseraCloud fail when the gateway clock is wrong by more than 24 hours, which typically happens only after a long power outage combined with blocked NTP.

### 13.2 Heartbeats and offline status

The gateway sends a heartbeat to TesseraCloud every 60 seconds. TesseraCloud marks the gateway Offline after 10 consecutive missed heartbeats, that is, after 10 minutes without contact. A knowledge-base article covering offline gateways is available (sup-kb-ts4-gateway-offline).

### 13.3 Store-and-forward buffer

When backhaul is unavailable, the gateway stores readings in a 256 MB flash buffer. At the default reporting interval with 64 nodes, the buffer holds approximately 30 days of readings. When the buffer reaches 90% the gateway raises event T-348 (buffer near capacity); once full, the oldest readings are overwritten first. Buffered readings are uploaded automatically, oldest first, when the connection is restored, at a rate of up to 2,000 readings per minute so that live data is not delayed.

## 14. Firmware updates

Gateway firmware is normally updated over the air from TesseraCloud (Fleet > Gateways > [gateway] > Firmware > Schedule Update). Updates can also be applied locally from System > Firmware > Upload in the local interface using a signed image file (.tfw) downloaded from the Meridian partner portal.

- An update takes approximately 6 minutes, including a reboot. Mesh traffic is buffered by the nodes during the reboot.
- The gateway uses A/B firmware partitions. If the new image fails to boot three times in succession, the gateway rolls back automatically to the previous version and logs the rollback.
- Always check the compatibility matrix before updating. Node firmware 2.8.0 requires gateway firmware 5.3.0. Updating nodes before their gateway will make them unable to rejoin.
- Update the gateway first, then the nodes. Node updates are distributed over the mesh and take approximately 40 minutes per node at 1-hop, longer for deeper nodes; up to 8 nodes are updated in parallel.

## 15. Factory reset

A factory reset erases all configuration, including the mesh network ID, the paired node list, network settings and the admin password. The device certificate is retained, so the gateway can reconnect to TesseraCloud once backhaul is configured again, but all nodes must be re-paired.

To reset: with the gateway powered, press and hold the recessed RESET button beside the SVC port for 15 seconds, until the status LED flashes amber rapidly. Release the button. The gateway reboots with factory defaults, which takes about 90 seconds. A shorter press (between 2 and 5 seconds) only reboots the gateway without resetting it.

## 16. Status LED reference

| LED pattern | Meaning |
|---|---|
| Solid green | Connected to TesseraCloud, mesh running |
| Slow green flash | Connected to TesseraCloud, pairing window open |
| Solid amber | Booting or applying firmware update |
| Slow amber flash | No TesseraCloud connection; buffering readings |
| Rapid amber flash | Factory reset in progress |
| Solid red | Hardware fault; contact Meridian Support |
| Red flash | Certificate or authentication failure (see T-327) |

## 17. Installation checklist

1. Site survey completed and TS-F-012 attached to the work order.
2. Bracket fixed with M8 bolts at 12 N m and earth bond fitted.
3. Antenna N-type connectors tightened to 1.7 N m and weatherproofed.
4. Power connected (PoE+ or 10-30 VDC), not both.
5. Backhaul configured and gateway Online in TesseraCloud.
6. Mesh channel and network ID recorded.
7. All nodes paired, mounted at 6 N m and assigned to assets.
8. Unused cable entries blanked; gland nuts tightened.
9. Firmware verified against the compatibility matrix.
10. Customer handover completed, including the admin password in the customer's password vault.
