# Network and VPN Setup Guide: Leeds

Document ID: it-network-vpn-guide-leeds. Site: Leeds, United Kingdom (headquarters, site code LDS). Owner: IT Operations, Network team. Last updated: 7 April 2025.

## Who this guide is for

New starters and visitors working at the Leeds headquarters, and Leeds-based staff connecting from home. Other sites have their own guide; subnets, VPN endpoints and printer queues are different at each site.

## Wi-Fi

- Meridian-Corp: company laptops only, 802.1X certificate authentication, joins automatically after laptop provisioning.
- Meridian-Guest: visitors, sponsor approval by email, sessions last 12 hours.
- Meridian-Lab: Leeds engineering lab only, for Halcyon and Tessera test devices; isolated from the corporate network.

## Site subnets

| Network | VLAN | Subnet |
|---|---|---|
| Site supernet | n/a | 10.10.0.0/16 |
| Users (wired and Meridian-Corp) | 110 | 10.10.16.0/20 |
| Voice | 120 | 10.10.32.0/22 |
| Printers | 130 | 10.10.40.0/24 |
| Engineering lab | 150 | 10.10.60.0/22 |
| Leeds data centre servers | 200 | 10.10.100.0/22 |

DNS servers: 10.10.1.10 and 10.10.1.11. Internal domain: meridian.internal.

## VPN (Meridian Connect)

- Client: Meridian Connect, installed on every company laptop.
- Leeds endpoint: vpn-lds.meridian-instruments.com
- Protocol and port: UDP 51820.
- Tunnel mode: split tunnel. Traffic to 10.0.0.0/8 and meridian.internal goes through the VPN; general internet traffic does not.
- Authentication: directory account plus MFA (authenticator push with number matching, or FIDO2 key).
- Idle timeout: 8 hours.

Leeds-based staff should always use the Leeds endpoint. If vpn-lds is unavailable, use vpn-rtm.meridian-instruments.com as the fallback.

## Printers

Print server: prn-lds-01.meridian.internal. Queues:

| Queue | Location | Type |
|---|---|---|
| LDS-FL1-MFP01 | Ground floor, reception side | Mono multifunction |
| LDS-FL2-MFP02 | First floor, finance area | Colour multifunction |
| LDS-FL3-MFP03 | Second floor, engineering | Colour multifunction |
| LDS-LAB-COLOUR | Engineering lab | A3 colour |

Print jobs are held until you badge in at the device (follow-me printing). Held jobs are deleted after 24 hours.

## Getting help

Raise a Beacon ticket in category NET-VPN for VPN problems, NET-WIFI for Wi-Fi, or PRN-QUEUE for printers. The Leeds IT desk is on the ground floor next to reception, open 08:00 to 18:00.
