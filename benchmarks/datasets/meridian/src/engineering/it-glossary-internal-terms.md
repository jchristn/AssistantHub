# Glossary of Internal Terms and Acronyms (Engineering, IT and Manufacturing)

Document ID: it-glossary-internal-terms. Owner: IT Operations, with contributions from Platform Engineering, IT Security and Penang Manufacturing Engineering. Last updated: 1 August 2025.

This glossary explains names and abbreviations used in Meridian engineering, IT, security and manufacturing documents.

## Internal systems

| Term | Meaning |
|---|---|
| Atlas | Meridian's ERP system. Covers finance, procurement, inventory, payroll export and the shop-floor (manufacturing) module used as the MES at Penang. |
| Beacon | The IT service desk and ticketing tool. Every request and incident is a Beacon ticket with a category such as NET-VPN or MFG-OT. |
| Keel | The HR portal: employee records, leave, onboarding and leaver workflows. Keel leaver events raise ACC-REVOKE tickets in Beacon. |
| Forge | The internal CI/CD platform, built on GitLab runners. Builds firmware, TesseraCloud services and internal tools. |
| Meridian Connect | The corporate VPN client. Each site has its own endpoint (vpn-lds, vpn-rtm, vpn-aus, vpn-pen). |
| Strata Backup | The backup platform. Primary repository STRATA-LDS-01 in Leeds, offsite copy STRATA-RTM-02 in Rotterdam. |
| tcctl | Command-line tool for TesseraCloud operations (status, failover, DNS switch). |
| forgectl | Command-line tool for Forge administration, including runner drain and token rotation. |
| atlasctl | Command-line tool for Atlas batch and configuration operations. |

## Products

| Term | Meaning |
|---|---|
| Halcyon | Portable gas detector family: HX-200 (single gas), HX-210 (4-gas), HX-220 (4-gas plus PID). |
| Tessera | Vibration monitoring: TS-4 gateway, TS-4e edge node and the TesseraCloud SaaS. |
| TC | Short for TesseraCloud. Service names start with tc- (tc-ingest, tc-api, tc-stream). |
| Lumen | Optical-flow meter family: LF-60 and LF-60P (the P variant is the higher-pressure model). |
| HX-MB | Halcyon main board, shared by all HX models. |

## Sites

| Code | Site |
|---|---|
| LDS | Leeds, UK (headquarters and data centre) |
| RTM | Rotterdam, Netherlands (EU logistics and Repair Centre) |
| AUS | Austin, US (Americas sales and Tessera software engineering) |
| PEN | Penang, Malaysia (factory, Bayan Lepas) |
| euw1 / euc1 | TesseraCloud primary region (Dublin) and DR region (Frankfurt) |

## Operations and process terms

| Term | Meaning |
|---|---|
| SEV1 to SEV4 | Incident severity levels, defined in the Incident Severity Definitions. |
| IC | Incident Commander: the person who leads the response to a SEV1 or SEV2. |
| PIR | Post-incident review, required within 5 business days for SEV1 and SEV2. |
| INC-YYYY-NNN | Incident number format, for example INC-2024-031. |
| ADR | Architecture Decision Record, numbered ADR-NNN. A later ADR can supersede an earlier one. |
| CAB | Change Advisory Board; meets Wednesdays at 10:30 UK time. |
| ECAB | Emergency CAB; approves emergency changes (Beacon category CHG-EMERG). |
| SC-NNN | Standard change catalogue item, for example SC-017 Forge runner token rotation. |
| MEC | Month-end close in Atlas. The MEC window runs BD-2 to BD+3. |
| BD | Business day relative to month end. BD-1 is the last business day of the month; BD+1 is the first business day of the next month. |
| PAYX | The Atlas payroll export job PAYX-MONTHLY, sent to Calderbrook Payroll. |
| MEC Bridge | The Teams channel used to coordinate month-end close. |
| Follow-the-sun | On-call arrangement where Penang covers the Platform primary overnight UK time. |
| Duty manager | Manager on call who can approve TesseraCloud failover when the Head of Platform Engineering is unavailable. |
| RTO / RPO | Recovery time objective / recovery point objective. |
| MM2 | MirrorMaker 2, used to replicate Kafka topics from euw1 to euc1. |
| HIL | Hardware-in-the-loop test rigs attached to Penang Forge runners. |

## Manufacturing terms

| Term | Meaning |
|---|---|
| SMT | Surface-mount technology; Penang has lines SMT-1 to SMT-4. |
| SMED | Single-minute exchange of dies; the method used to shorten line changeovers. |
| SPI | Solder paste inspection. |
| AOI | Automated optical inspection. |
| FAI | First article inspection, signed by QA before a line is released after changeover. |
| MSL | Moisture sensitivity level of components; MSL 3 parts have a 168-hour floor life. |
| OT network | Operational technology network for production equipment at Penang (10.41.0.0/16). |
| RMA | Return merchandise authorisation; customer returns handled by the Rotterdam Repair Centre. |
| T90 | Time for a gas sensor to reach 90% of its final reading. |
| Bump test | Short exposure to gas to confirm a detector alarms; not a calibration. |
| Traveller | Paper or electronic route card that accompanies a production batch. |
