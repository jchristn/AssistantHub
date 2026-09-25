# Password and MFA Standard (v1)

Document ID: sec-password-mfa-standard-v1. Owner: IT Security (Head of IT Security: Mei-Ling Tan). Version 1. Effective date: 1 September 2022. Approved by: Lars Hedegaard, CTO.

## Purpose

This standard sets the minimum requirements for passwords and multi-factor authentication (MFA) on all Meridian Instruments accounts, including the corporate directory, Atlas, Beacon, Keel and Forge. It applies to employees, contractors and service accounts at all sites (Leeds, Rotterdam, Austin and Penang).

## Password requirements for standard user accounts

- Minimum length: 12 characters.
- Complexity: at least three of the four character classes (upper case, lower case, digits, symbols).
- Mandatory rotation: every 90 days. Users receive a reminder 14 days before expiry.
- Password history: the last 12 passwords cannot be reused.
- Account lockout: 5 failed attempts locks the account for 15 minutes.
- Passwords must not contain the user's name, username or the word "Meridian".

## Privileged accounts

Privileged accounts (domain administrators, Atlas DBA accounts, Forge administrators, firewall administrators) must meet stricter rules:

- Minimum length: 15 characters.
- Mandatory rotation: every 60 days.
- Separate admin identity (prefix adm-) that is never used for email or browsing.

## Service accounts

Service account passwords are rotated every 12 months or when a person with knowledge of the password leaves the team. Service account credentials are stored in the IT password vault and must not be written into scripts or Forge pipeline definitions.

## Multi-factor authentication

MFA is required for remote access (VPN), webmail from outside the corporate network, Atlas finance roles and all privileged accounts. Accepted second factors in this version:

- Authenticator app push notification or time-based one-time code.
- SMS one-time code (accepted where the user does not have a company smartphone).
- Hardware token (issued on request by IT Security).

## Exceptions

Exceptions must be requested in Beacon using category SEC-EXCEPTION and approved by the Head of IT Security. Exceptions are valid for a maximum of six months.

## Review

This standard is reviewed every two years.
