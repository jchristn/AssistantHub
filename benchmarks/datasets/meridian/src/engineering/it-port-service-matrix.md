# Port and Service Matrix

Document ID: it-port-service-matrix. Owner: IT Operations, Network team, with Platform Engineering. Last updated: 1 July 2025.

This matrix lists the network ports that Meridian firewalls permit for core internal and customer-facing services. Firewall rule requests that match a row here can use standard change SC-009. Anything not listed needs a normal change through CAB.

## Corporate and business systems

| Service | Host(s) | Port | Protocol | Allowed from |
|---|---|---|---|---|
| Atlas web application | atlas-app-prd-01 to atlas-app-prd-04 | 443 | HTTPS | All corporate user VLANs and VPN |
| Atlas database (primary) | atlas-db-prd-01.meridian.internal | 1521 | TCP (database listener) | Atlas app and batch hosts only |
| Atlas database (standby, reporting) | atlas-db-prd-02.meridian.internal | 1521 | TCP (database listener) | atlas-rpt-prd-01, finance reporting tools |
| Atlas batch to payroll bureau | atlas-batch-prd-01 to sftp.calderbrook-payroll.co.uk | 2222 | SFTP (outbound) | atlas-batch-prd-01 only |
| Beacon service desk | beacon.meridian.internal | 443 | HTTPS | All corporate networks |
| Keel HR portal SSO | keel.meridian-instruments.com | 443 | HTTPS (SAML) | Internet |
| Forge web and API | forge.meridian.internal | 443 | HTTPS | Corporate and VPN |
| Forge Git over SSH | forge.meridian.internal | 2224 | SSH | Corporate and VPN |
| Privileged access vault | pam.meridian.internal | 443 | HTTPS | Corporate and VPN |
| Print servers | prn-lds-01, prn-rtm-01, prn-aus-01, prn-pen-01 | 9100, 631 | RAW, IPP | Site user VLANs |

## Remote access

| Service | Endpoint | Port | Protocol |
|---|---|---|---|
| VPN Leeds | vpn-lds.meridian-instruments.com | 51820 | UDP |
| VPN Rotterdam | vpn-rtm.meridian-instruments.com | 51820 | UDP |
| VPN Austin | vpn-aus.meridian-instruments.com | 51820 | UDP |
| VPN Penang | vpn-pen.meridian-instruments.com | 443 | UDP |
| Penang OT jump host | ot-jump-pen-01.meridian.internal | 3389 | RDP (from VPN via vault proxy only) |

## TesseraCloud production

| Service | Host | Port | Protocol | Allowed from |
|---|---|---|---|---|
| Device ingestion (TS-4, TS-4e) | ingest.tesseracloud.meridian-instruments.com | 8883 | MQTT over TLS | Internet |
| Public API and web app | api.tesseracloud.meridian-instruments.com | 443 | HTTPS | Internet |
| tc-ingest health | tc-ingest pods | 8081 | HTTP | Internal load balancer only |
| Kafka brokers | kafka-prd-01 to kafka-prd-06 | 9093 | Kafka over TLS | TesseraCloud private network |
| TimescaleDB | tsdb-prd-01.tessera.meridian.internal | 5432 | PostgreSQL | tc-api, tc-stream |
| Redis cache | redis-prd-01.tessera.meridian.internal | 6380 | Redis over TLS | tc-api |

Non-production TesseraCloud ports differ; see the per-environment configuration pages.

## Blocked by policy

- Telnet (23), FTP (21) and SMBv1 are blocked everywhere.
- Plain MQTT on 1883 is only allowed inside the TesseraCloud development environment.
- Direct RDP (3389) from the internet is never permitted.
