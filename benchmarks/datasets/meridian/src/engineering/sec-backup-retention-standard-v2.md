# Backup and Retention Standard (v2)

Document ID: sec-backup-retention-standard-v2. Owner: IT Infrastructure and IT Security. Version 2. Effective date: 1 April 2025. Supersedes: Backup and Retention Standard v1 (February 2023). Approved by: Mei-Ling Tan, Head of IT Security, and Lars Hedegaard, CTO.

## Summary of changes from v1

- Daily incremental retention reduced from 35 days to 30 days.
- Weekly full retention increased from 8 weeks to 12 weeks.
- Monthly full retention increased from 12 months to 24 months.
- Offsite replication to the Rotterdam vault changed from weekly to daily.
- New immutable copy tier: all daily backups are written to an object-lock repository and cannot be deleted or altered for 30 days (ransomware protection).
- Restore testing increased from annual to quarterly.
- TesseraCloud retention rules added for the TimescaleDB telemetry store (see ADR-023).

## Backup platform

Backups are taken with the Strata Backup platform. Primary repository STRATA-LDS-01 (Leeds data centre); immutable object-lock repository STRATA-LDS-IMM; offsite copy in the Rotterdam vault STRATA-RTM-02.

## Backup frequency and retention

| Backup type | Frequency | Retention |
|---|---|---|
| Daily incremental | Every night, 23:00 local time | 30 days |
| Immutable daily copy | Every night | 30 days (object lock, cannot be shortened) |
| Weekly full | Sunday 02:00 | 12 weeks |
| Monthly full | First Sunday of the month | 24 months |
| Yearly full (Atlas finance data) | 1 April (start of fiscal year) | 7 years (unchanged) |
| Atlas month-end pre-close backup (tag MEC-YYYYMM) | BD-3 at 22:00 | 13 months |
| TesseraCloud TimescaleDB base backup | Daily, 01:00 UTC | 30 days |
| TesseraCloud WAL archive | Continuous | 7 days |

## Data retention (TesseraCloud)

| Data set | Retention |
|---|---|
| Raw vibration telemetry (Standard plan) | 400 days (covers the 13-month contractual commitment) |
| Raw vibration telemetry (Enterprise plan) | 36 months, older chunks held in compressed archive tier |
| 1-minute aggregates | 5 years |
| Hourly aggregates | Life of customer contract plus 1 year |
| Audit log (tc-api) | 2 years |

## Offsite copies

Every daily backup is replicated to STRATA-RTM-02 in Rotterdam by 06:00 UK time. Replication failures raise a Beacon ticket in category SEC-BACKUP automatically and must be resolved within one business day.

## Restore testing

Restore tests are performed quarterly (in the first month of each fiscal quarter: April, July, October, January). Each test restores Atlas to an isolated host, one file share and one TesseraCloud tenant. Results are recorded in Beacon category SEC-BACKUP-TEST. A failed restore test is treated as a SEV2 incident (previously SEV3).

## Encryption and access

All backups are encrypted at rest with AES-256. Deletion of backup sets before expiry requires two approvers from IT Infrastructure and IT Security. Backup administrator accounts must use FIDO2 MFA per the Password and MFA Standard v2.

## Review

This standard is reviewed annually.
