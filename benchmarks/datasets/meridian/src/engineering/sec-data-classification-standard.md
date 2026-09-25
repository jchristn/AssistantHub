# Data Classification Standard

Document ID: sec-data-classification-standard. Owner: IT Security. Effective date: 10 January 2024.

## Classification levels

Meridian Instruments uses four classification levels. Every document, data set and system must be assigned the highest level of data it holds.

| Level | Label | Examples | Handling |
|---|---|---|---|
| 1 | Public | Product datasheets, published press releases | No restrictions |
| 2 | Internal | Runbooks, org charts, internal wiki pages | Meridian staff and approved contractors only |
| 3 | Confidential | Customer telemetry, supplier pricing, firmware source code, Atlas financial data before publication | Need-to-know; encrypted in transit and at rest; no personal cloud storage |
| 4 | Restricted | Payroll data, employee health data, cryptographic signing keys, security incident evidence | Named individuals only; FIDO2 MFA; access logged and reviewed quarterly |

## Labelling

Office documents are labelled using the sensitivity label in the document header. Default label for new documents is Internal. Email containing Confidential or Restricted data sent outside Meridian must be encrypted.

## Customer telemetry

TesseraCloud customer telemetry is Confidential by default. Customer data must not be copied from production TesseraCloud to dev or staging environments; synthetic data sets are used instead.

## Product signing keys

Firmware signing keys for Halcyon, Tessera and Lumen products are Restricted and are held in hardware security modules. Signing is performed only by the Forge signing stage; no engineer has direct access to the private keys.

## Disposal

Confidential and Restricted data on retired hardware is destroyed using certified wiping or physical destruction. Certificates of destruction are kept for 3 years.
