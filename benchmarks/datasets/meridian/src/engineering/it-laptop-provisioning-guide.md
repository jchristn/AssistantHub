# Laptop Provisioning Guide for Site IT Desks

Document ID: it-laptop-provisioning-guide. Owner: IT Service Desk. Last updated: 2 July 2025.

This guide is for site IT technicians preparing laptops for new starters. New-starter requests arrive from Keel as a Beacon ticket in category HW-LAPTOP at least 5 business days before the start date.

## Standard models

- Standard user: 14-inch business laptop, 16 GB RAM, 512 GB SSD.
- Engineering: 16-inch workstation laptop, 32 GB RAM, 1 TB SSD. Approved for firmware, Forge and CAD users.
- Shop-floor supervisor (Penang): rugged 14-inch laptop, 16 GB RAM.

## Steps

1. Take a laptop from site stock and record the asset tag and serial number against the Beacon ticket.
2. Connect the laptop to the build network port at the IT desk (VLAN 199 at every site) and power on.
3. Enrol the device with the device management service using the site enrolment profile: MER-LDS, MER-RTM, MER-AUS or MER-PEN.
4. Wait for the base build to complete. This takes about 40 minutes and installs disk encryption, endpoint protection, the Meridian Connect VPN client and office applications.
5. Check that disk encryption shows as enabled and that the recovery key has escrowed to the directory.
6. Assign the laptop to the user in the device management service. Engineering builds then receive the developer tools bundle automatically.
7. Apply the site Wi-Fi certificate so the laptop joins Meridian-Corp.
8. Print the handover sheet and put it in the box with the laptop, charger and a FIDO2 key if the user has a privileged role.

## On the first day

The user signs in on site, enrols MFA (authenticator app or FIDO2 key; SMS is not accepted) and sets a password of at least 14 characters. The technician confirms the VPN connects to the local site endpoint before closing the ticket.

## Returns

Laptops returned by leavers are wiped using the certified wipe tool and kept in quarantine for 30 days before reissue, in case data needs to be recovered for HR or legal reasons.
