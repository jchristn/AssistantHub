# Privileged Access Management Guide

Document ID: sec-privileged-access-management-guide. Owner: IT Security. Last updated: 12 June 2025.

## What counts as privileged access

Privileged access is any account or role that can change system configuration, read other users' data, or bypass normal controls. At Meridian this includes directory administrators, Atlas DBA and period-close administrator roles, Forge instance administrators, TesseraCloud operator roles, firewall and VPN administrators, backup administrators, and Penang OT engineering workstations.

## Separate admin identities

Every privileged user has a separate identity with the prefix adm- (for example adm-kmensah). Admin identities have no mailbox and must never be used for browsing or email. Admin sign-in requires a FIDO2 security key, as required by the Password and MFA Standard v2.

## Vault check-out

Shared and break-glass credentials are stored in the privileged access vault (pam.meridian.internal). To use a credential:

1. Open the vault and search for the target system.
2. Request check-out and enter the Beacon ticket or change number that justifies access.
3. For production systems, a second person from the owning team approves the request.
4. Check-out lasts 4 hours by default and a maximum of 8 hours.
5. On check-in, the vault rotates the password automatically.

Sessions to Atlas database servers (atlas-db-prd-01 and atlas-db-prd-02) and to TesseraCloud production are proxied through the vault and recorded. Recordings are retained for 1 year.

## Break-glass accounts

Each critical system has one break-glass account, sealed in the vault with two-person release. Use of a break-glass account automatically raises a SEV3 security incident for review, even if the use was legitimate.

## Just-in-time roles

Forge production deploy rights and TesseraCloud operator roles are granted just-in-time for up to 12 hours via the command pamctl elevate --role tc-operator --ticket <ticket>. Standing access to these roles is not permitted.

## Review

Privileged group membership is reviewed quarterly as part of the User Access Review Procedure.
