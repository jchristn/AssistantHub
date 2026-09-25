# Network and VPN Setup Guide: Rotterdam

Document ID: it-network-vpn-guide-rotterdam. Site: Rotterdam, Netherlands (EU sales, logistics and Repair Centre, site code RTM). Owner: IT Operations, Network team. Last updated: 7 April 2025.

## Who this guide is for

New starters and visitors working at the Rotterdam site, and Rotterdam-based staff connecting from home. Other sites have their own guide; subnets, VPN endpoints and printer queues are different at each site.

## Wi-Fi

- Meridian-Corp: company laptops only, 802.1X certificate authentication.
- Meridian-Guest: visitors, sponsor approval by email, sessions last 12 hours.
- Meridian-Repair: Repair Centre benches only, for customer devices under RMA; isolated from the corporate network.

## Site subnets

| Network | VLAN | Subnet |
|---|---|---|
| Site supernet | n/a | 10.20.0.0/16 |
| Users (wired and Meridian-Corp) | 210 | 10.20.16.0/21 |
| Voice | 220 | 10.20.32.0/23 |
| Printers | 230 | 10.20.40.0/24 |
| Repair Centre benches | 250 | 10.20.60.0/24 |
| Site servers and backup vault | 300 | 10.20.100.0/24 |

DNS servers: 10.20.1.10 and 10.20.1.11. Internal domain: meridian.internal.

## VPN (Meridian Connect)

- Client: Meridian Connect, installed on every company laptop.
- Rotterdam endpoint: vpn-rtm.meridian-instruments.com
- Protocol and port: UDP 51820.
- Tunnel mode: split tunnel. Traffic to 10.0.0.0/8 and meridian.internal goes through the VPN.
- Authentication: directory account plus MFA (authenticator push with number matching, or FIDO2 key).
- Idle timeout: 8 hours.

If vpn-rtm is unavailable, use vpn-lds.meridian-instruments.com as the fallback.

## Printers

Print server: prn-rtm-01.meridian.internal. Queues:

| Queue | Location | Type |
|---|---|---|
| RTM-OFF-MFP01 | Office floor | Colour multifunction |
| RTM-REP-ZEBRA01 | Repair Centre | Label printer for RMA and calibration labels |
| RTM-WH-ZEBRA02 | Warehouse dispatch | Shipping label printer |

Office jobs are held until you badge in at the device. Label printer queues print immediately.

## Getting help

Raise a Beacon ticket in category NET-VPN, NET-WIFI or PRN-QUEUE. Rotterdam does not have a full-time IT desk; on-site support is available Tuesday and Thursday, and remote support is provided from Leeds at other times.
