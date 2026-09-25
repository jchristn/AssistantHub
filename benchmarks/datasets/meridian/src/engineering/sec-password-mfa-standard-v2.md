# Password and MFA Standard (v2)

Document ID: sec-password-mfa-standard-v2. Owner: IT Security (Head of IT Security: Mei-Ling Tan). Version 2. Effective date: 1 June 2025. Supersedes: Password and MFA Standard v1 (September 2022). Approved by: Lars Hedegaard, CTO.

## Summary of changes from v1

- Minimum length for standard users increased from 12 to 14 characters; passphrases are recommended.
- Mandatory periodic rotation (previously every 90 days) is removed for user accounts. Passwords are changed only on suspected compromise, or when the breached-password check flags them.
- Complexity rules removed in favour of length plus a breached-password check.
- Privileged account minimum length increased from 15 to 20 characters; 60-day rotation replaced by vault check-out with automatic rotation after each use.
- Service account secrets are now rotated every 180 days (previously 12 months).
- Account lockout changed from 5 attempts / 15 minutes to 10 attempts / 30 minutes.
- SMS is no longer an accepted MFA method. SMS enrolment closed on 1 June 2025 and all remaining SMS registrations were removed on 30 September 2025.
- Phishing-resistant MFA (FIDO2 security keys) is mandatory for privileged accounts and for remote access to the Penang OT network.

## Scope

All Meridian Instruments identities: employees, contractors, service accounts and shared kiosk accounts at Leeds, Rotterdam, Austin and Penang. Systems in scope include the corporate directory, Atlas, Beacon, Keel, Forge and TesseraCloud administrative consoles.

## Password requirements

| Account type | Minimum length | Rotation | Lockout |
|---|---|---|---|
| Standard user | 14 characters | On compromise only | 10 attempts, 30 minutes |
| Privileged (adm-) | 20 characters | Automatic after each vault check-out | 5 attempts, until reset by IT Security |
| Service account | 32 characters (generated) | Every 180 days | Not interactive |
| Shop-floor kiosk (Penang) | 14 characters | Every 90 days | 10 attempts, 30 minutes |

All new passwords are checked against the breached-password list at the time they are set. Passwords must not contain the user's name, username, or the words "Meridian", "Halcyon", "Tessera" or "Lumen".

## Multi-factor authentication

MFA is required for every interactive sign-in to corporate systems, not only remote access. Accepted methods:

- Authenticator app push with number matching (default for standard users).
- FIDO2 security key (mandatory for privileged accounts, Forge maintainers with production deploy rights, and remote access to the Penang OT network).
- Platform passkey on a company-managed laptop.

SMS and voice call codes are not accepted. Users who cannot use a smartphone are issued a FIDO2 key by the local IT desk; request via Beacon category ACC-MFA.

## Privileged access

Privileged credentials are held in the privileged access vault and checked out per task; see the Privileged Access Management Guide (sec-privileged-access-management-guide).

## Exceptions

Exceptions are requested in Beacon category SEC-EXCEPTION and must be approved by the Head of IT Security. Maximum exception duration is 90 days (reduced from six months in v1).

## Review

This standard is reviewed annually.
