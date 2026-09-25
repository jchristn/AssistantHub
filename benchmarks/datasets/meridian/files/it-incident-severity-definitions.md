# Incident Severity Definitions

Document ID: it-incident-severity-definitions. Owner: IT Operations (Head of IT Operations: Siobhan Kerr). Effective date: 15 January 2025. Applies to: all IT, platform, security and OT incidents at Meridian Instruments, including TesseraCloud.

## Purpose

This table is the single source of truth for incident severity at Meridian. The acknowledgement and update times below are the service levels that the On-call Policy (it-oncall-policy-v2) enforces for paged responders. If another document states a different time, this table wins.

## Severity table

| Severity | Definition | Examples | Acknowledge within | Incident Commander assigned | Stakeholder update cadence | Target restoration |
|---|---|---|---|---|---|---|
| SEV1 | Critical: complete loss of a customer-facing service, a production line stop at a factory, or a confirmed security breach | TesseraCloud ingestion down for all customers; regional failover; Penang SMT lines all stopped; ransomware detected | 5 minutes | 15 minutes | Every 30 minutes | 4 hours |
| SEV2 | Major: significant degradation, a single site or business process blocked, or a deadline-critical failure | Atlas month-end close blocked; payroll export failed; one SMT line down more than 2 hours; Forge unable to run any jobs | 15 minutes | 30 minutes | Every 60 minutes | 8 hours |
| SEV3 | Minor: limited impact with a workaround available | One Forge runner pool degraded; single-user VPN issue at scale of one team; runner token expiring soon alert; break-glass account used | 4 business hours | Not required | Daily | 3 business days |
| SEV4 | Low: cosmetic issue, question or scheduled request | Dashboard label wrong; documentation error | 2 business days | Not required | On resolution | 20 business days |

Business hours are 08:00 to 18:00 local site time, Monday to Friday, excluding site public holidays.

## Declaring and changing severity

- Anyone may declare an incident by raising a Beacon ticket in the appropriate category (for platform issues, INC-PLATFORM) and selecting a severity.
- The Incident Commander may raise or lower severity at any time and must record the reason in the incident channel.
- A SEV1 or SEV2 always requires a post-incident review (PIR) within 5 business days, with a written postmortem stored in the engineering knowledge base.
- SEV1 incidents affecting TesseraCloud customers require a status page update at status.tesseracloud.meridian-instruments.com within 30 minutes.

## Relationship to Beacon priorities

| Severity | Beacon priority |
|---|---|
| SEV1 | P1 |
| SEV2 | P2 |
| SEV3 | P3 |
| SEV4 | P4 |

## History

This table replaced the severity appendix of the On-call Policy v1. The main change is that SEV1 acknowledgement tightened from 15 minutes to 5 minutes and SEV2 from 30 minutes to 15 minutes.
