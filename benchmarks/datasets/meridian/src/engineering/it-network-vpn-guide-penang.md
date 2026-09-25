# Network and VPN Setup Guide: Penang

Document ID: it-network-vpn-guide-penang. Site: Penang, Malaysia (factory, Bayan Lepas, site code PEN). Owner: IT Operations, Network team, with Penang OT Support. Last updated: 7 April 2025.

## Who this guide is for

New starters and visitors working at the Penang factory offices, and Penang-based staff connecting from home. Other sites have their own guide; subnets, VPN endpoints and printer queues are different at each site. Penang is the only site with a separate operational technology (OT) network for production.

## Wi-Fi

- Meridian-Corp: company laptops only, 802.1X certificate authentication. Not available on the production floor.
- Meridian-Guest: visitors, sponsor approval by email, sessions last 8 hours.
- There is no Wi-Fi on the OT network. Shop-floor kiosks are wired.

## Site subnets

| Network | VLAN | Subnet |
|---|---|---|
| Corporate supernet | n/a | 10.40.0.0/16 |
| Users (wired and Meridian-Corp) | 410 | 10.40.16.0/20 |
| Voice | 420 | 10.40.32.0/23 |
| Printers | 430 | 10.40.40.0/24 |
| Site servers | 440 | 10.40.100.0/24 |
| OT network (SMT lines, test stations, calibration stations) | separate | 10.41.0.0/16 |

DNS servers: 10.40.1.10 and 10.40.1.11. Internal domain: meridian.internal. The OT network 10.41.0.0/16 has its own DNS and cannot be reached directly from the corporate network or the VPN.

## VPN (Meridian Connect)

- Client: Meridian Connect, installed on every company laptop.
- Penang endpoint: vpn-pen.meridian-instruments.com
- Protocol and port: UDP 443. Penang uses UDP 443 instead of the UDP 51820 used at other sites because several local home broadband providers filter non-standard UDP ports.
- Tunnel mode: split tunnel. Traffic to 10.0.0.0/8 and meridian.internal goes through the VPN.
- Authentication: directory account plus MFA.
- Idle timeout: 8 hours.

If vpn-pen is unavailable, use vpn-lds.meridian-instruments.com as the fallback (note it listens on UDP 51820).

## Remote access to the OT network

Access to the OT network from the VPN is only through the jump host ot-jump-pen-01.meridian.internal. It requires a FIDO2 security key, an approved Beacon ticket in category MFG-OT, and a session recorded by the privileged access vault. Sessions are limited to 4 hours.

## Printers

Print server: prn-pen-01.meridian.internal. Queues:

| Queue | Location | Type |
|---|---|---|
| PEN-ADM-MFP01 | Administration building | Colour multifunction |
| PEN-SMT-LABEL01 to PEN-SMT-LABEL04 | One per SMT line (SMT-1 to SMT-4) | Board serial label printers |
| PEN-FT-LABEL01 | Final test and calibration | Product and calibration labels |
| PEN-WH-ZEBRA01 | Warehouse | Shipping labels |

Label printers on the SMT lines are on the OT network and are managed by Penang OT Support, not the corporate print server.

## Getting help

Raise a Beacon ticket in category NET-VPN, NET-WIFI or PRN-QUEUE for corporate issues, and MFG-OT for anything on the production floor. The Penang IT desk is in the administration building, open 07:30 to 17:30 MYT.
