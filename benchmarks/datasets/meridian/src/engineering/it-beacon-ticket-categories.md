# Beacon Ticket Categories

Document ID: it-beacon-ticket-categories. Owner: IT Service Desk. Last updated: 30 June 2025.

Beacon is the Meridian IT service desk. Every ticket must have a category. The category decides the default priority, the resolver group and whether the ticket needs approval. Choosing the right category is the fastest way to get help.

## Category list

| Category | Use for | Default priority | Resolver group | Approval needed |
|---|---|---|---|---|
| ACC-NEW | New access to a system or shared folder | P3 | IT Service Desk | Line manager |
| ACC-REVOKE | Remove access (raised automatically for Keel leavers) | P3 | IT Service Desk | None |
| ACC-MFA | MFA enrolment, lost authenticator, FIDO2 key request | P2 | IT Service Desk | None |
| HW-LAPTOP | Laptop fault, replacement or new-starter build | P3 | Site IT desk | None |
| SW-INSTALL | Software installation from the approved catalogue | P4 | IT Service Desk | Line manager for paid licences |
| NET-VPN | Meridian Connect VPN problems | P3 | Network team | None |
| NET-WIFI | Wi-Fi problems at any site | P3 | Network team | None |
| PRN-QUEUE | Printer faults and new print queues | P4 | Site IT desk | None |
| ATLAS-FIN | Atlas finance modules (GL, AP, AR, close) | P3 (P2 during month-end close) | IT Business Applications | None |
| ATLAS-PAYX | Atlas payroll export (raised automatically when PAYX-MONTHLY fails) | P2 | IT Business Applications | None |
| ATLAS-MFG | Atlas manufacturing and shop-floor module | P3 | IT Business Applications | None |
| KEEL-HR | Keel HR portal technical problems | P3 | IT Business Applications | None |
| FORGE-RUNNER | Forge runner failures, token problems, stuck jobs | P3 | Platform Engineering | None |
| FORGE-ACCESS | Forge project access and role changes | P3 | Platform Engineering | Project owner |
| INC-PLATFORM | Incidents on TesseraCloud or Forge (severity set on the ticket) | Set by severity | Platform Engineering | None |
| MFG-OT | Penang production floor: line PCs, OT network, label printers, calibration stations | P2 | Penang OT Support | None |
| CHG-STD | Standard change from the catalogue | P4 | Change implementer | Pre-approved |
| CHG-NORMAL | Normal change for CAB | P3 | Change Manager | CAB |
| CHG-EMERG | Emergency change | P1 | Change Manager | ECAB |
| SEC-INCIDENT | Suspected security incident (shown in the portal as "Security Incident") | P1 | IT Security | None |
| SEC-INCIDENT-PD | Suspected personal data breach (shown in the portal as "Security Incident – Personal Data"); must be raised within 2 hours of discovery | P1 | IT Security and Data Protection Officer | None |
| SEC-PHISH | Suspicious email reported with the Report Phish button | P3 | IT Security | None |
| SEC-VULN | Vulnerability findings from scanners | By CVSS rating | Owning team | None |
| SEC-EXCEPTION | Exception to a security standard | P3 | IT Security | Head of IT Security |
| SEC-ACCESS-REVIEW | Quarterly and semi-annual access review tasks | P3 | IT Security | None |
| SEC-BACKUP | Backup and offsite replication failures | P2 | IT Infrastructure | None |
| SEC-BACKUP-TEST | Restore test records | P4 | IT Infrastructure | None |

## Priorities and response targets

Beacon priorities map to incident severities: P1 is SEV1, P2 is SEV2, P3 is SEV3 and P4 is SEV4. Response targets for each severity are defined in the Incident Severity Definitions.

## Tips

- If in doubt, pick the closest category; the service desk will re-categorise.
- Do not raise SEC-INCIDENT for spam; use SEC-PHISH.
- Tickets in MFG-OT are worked 24x7 because they can stop production.
