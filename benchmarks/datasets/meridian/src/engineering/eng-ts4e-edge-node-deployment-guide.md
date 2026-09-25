# TS-4e Edge Node Field Deployment Checklist (Internal)

Document ID: eng-ts4e-edge-node-deployment-guide. Owner: Tessera Engineering, Austin. Last updated: 1 September 2025. Audience: Meridian field application engineers and support staff.

This is the internal checklist field engineers use when deploying TS-4e edge nodes at a customer site. Product specifications and customer-facing installation instructions are in the TS-4e datasheet and the TS-4 gateway installation guide; this checklist covers the internal steps around them.

## Before the site visit

1. Confirm the customer tenant exists in TesseraCloud production and that the gateways and edge nodes are claimed to it.
2. Check firmware in the Austin or Leeds lab: gateways must run gateway firmware 5.2.0 or later (TesseraCloud minimum from 1 October 2025), and edge nodes should run node firmware 2.8.0. Zone 2 variants (TS4E-NODE-3AX-Z2) require node firmware 2.8.0.
3. Check the gateway-node compatibility matrix before mixing node firmware versions on one gateway.
4. Confirm the customer firewall allows outbound MQTT over TLS from the gateway to ingest.tesseracloud.meridian-instruments.com on port 8883, or that MQTT over WebSocket on port 443 will be enabled.
5. Print the asset mapping sheet from TesseraCloud (machine, bearing position, node serial).

## On site

1. Install the TS-4 gateway first and confirm it shows Online in TesseraCloud.
2. Stud-mount each edge node as close to the bearing as possible and tighten to the torque in the datasheet. Magnetic mounts are for surveys only.
3. Wait for each node to join the gateway's TesseraMesh network and confirm first readings arrive in TesseraCloud within 10 minutes.
4. A single TS-4 gateway supports up to 64 edge nodes. Plan a second gateway before that limit is reached, not after.
5. Record install photos, torque values and node positions in the deployment record attached to the customer project.

## After the visit

- Confirm no gateway at the site shows Degraded in TesseraCloud 24 hours after installation.
- Raise internal defects found on site in Beacon category INC-PLATFORM with the tenant ID. Customer-reported faults go through the Meridian Support Centre, not Beacon.
- Do not copy customer telemetry out of TesseraCloud; it is Confidential data under the Data Classification Standard.
