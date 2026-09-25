# Backup and Retention Standard (v1)

Document ID: sec-backup-retention-standard-v1. Owner: IT Infrastructure and IT Security. Version 1. Effective date: 15 February 2023. Approved by: Mei-Ling Tan, Head of IT Security.

## Purpose

This standard defines how often Meridian Instruments systems are backed up, how long backups are retained, where copies are stored and how restores are tested. It covers on-premises systems in the Leeds data centre, site servers in Rotterdam, Austin and Penang, and the TesseraCloud production environment.

## Backup platform

Backups are taken with the Strata Backup platform. The primary backup repository is in the Leeds data centre (repository name STRATA-LDS-01). An offsite copy is written to the Rotterdam site vault.

## Backup frequency and retention

| Backup type | Frequency | Retention |
|---|---|---|
| Daily incremental | Every night, 23:00 local time | 35 days |
| Weekly full | Sunday 02:00 | 8 weeks |
| Monthly full | First Sunday of the month | 12 months |
| Yearly full (Atlas finance data) | 1 April (start of fiscal year) | 7 years |
| TesseraCloud telemetry database snapshot | Every 6 hours | 14 days |

## Offsite copies

A copy of the most recent weekly full backup is replicated to the Rotterdam vault every week (Monday 06:00). Offsite copies are retained for the same period as the source backup.

## System-specific rules

- Atlas production database (atlas-db-prd-01): archive logs backed up every 30 minutes, retained 35 days.
- Keel HR portal: data is held by the HR platform vendor; Meridian keeps a monthly export for 12 months.
- Forge (CI/CD): repositories backed up nightly; build artefacts are not backed up.
- File shares: nightly incremental, 35 days.

## Restore testing

A full restore test of Atlas and one file share is performed once per year and recorded in Beacon (category SEC-BACKUP-TEST). Failed restore tests are treated as SEV3 incidents.

## Encryption

All backups are encrypted at rest using AES-256. Encryption keys are held by IT Infrastructure and escrowed with IT Security.

## Review

This standard is reviewed every two years.
