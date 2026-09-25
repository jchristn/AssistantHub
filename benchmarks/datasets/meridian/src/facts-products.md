# Meridian corpus: products, support and sales facts cheat-sheet (PRIVATE)

This file is not ingested. It records the facts planted in the `prod-`, `sup-` and `sales-` documents so that the
question-writing pass can target them. All documents live in `src/products/`.

## 1. Canonical data (all documents must agree with this)

### 1.1 Halcyon sibling models (HX-200 / HX-210 / HX-220)

| Parameter | HX-200 | HX-210 | HX-220 |
|---|---|---|---|
| Gases | H2S only (single-gas) | O2, LEL (CH4), CO, H2S | O2, LEL (CH4), CO, H2S |
| H2S range / resolution | 0-100 ppm / 0.1 ppm | 0-200 ppm / 1 ppm | 0-100 ppm / 0.1 ppm |
| CO range | n/a | 0-500 ppm | 0-1000 ppm |
| LEL range | n/a | 0-100% LEL | 0-100% LEL |
| O2 range | n/a | 0-30% vol | 0-25% vol |
| Battery pack | HX-BAT-200, 2,600 mAh Li-ion | HX-BAT-210, 3,400 mAh Li-ion | HX-BAT-220L, 5,200 mAh Li-ion |
| Runtime (20 C) | 36 h | 14 h | 22 h |
| Charge time | 4 h | 5 h | 6.5 h |
| Calibration interval | 180 days | 120 days (v2 procedure; was 180 days in v1) | 150 days |
| Calibration gas | MI-CAL-H2S25 (25 ppm H2S in N2), 0.5 L/min via MI-REG-05 | MI-CAL-Q4-B, 1.0 L/min via MI-REG-10 (v2) | MI-CAL-Q4-B, 1.0 L/min via MI-REG-10 |
| ATEX / IECEx marking | II 2G Ex ib IIB T4 Gb (Zone 1, gas group IIB) | II 1G Ex ia IIC T4 Ga (Zone 0) | II 2G Ex ib IIC T4 Gb (Zone 1) |
| Certificates | Baseefa21ATEX0147X / IECEx BAS 21.0063X | Baseefa22ATEX0212X / IECEx BAS 22.0098X | Baseefa23ATEX0031X / IECEx BAS 23.0017X |
| Ingress protection | IP66 | IP67 | IP67 |
| Operating temperature | -20 to +50 C | -20 to +55 C | -40 to +55 C |
| Weight | 210 g | 330 g | 365 g |
| Dimensions | 98 x 55 x 32 mm | 125 x 68 x 40 mm | 131 x 70 x 44 mm |
| Datalog (1-min interval) | 60 days | 90 days | 180 days |
| Audible alarm | 95 dB at 30 cm | 95 dB at 30 cm | 103 dB at 30 cm |
| Connectivity | USB-C via HX-ADP-200 adapter in dock | USB-C / HX-DOCK-4 | USB-C / HX-DOCK-4 + Bluetooth LE 5.0, GPS, man-down alarm |
| Current firmware | 3.2.4 | 3.4.1 | 3.4.2 (HX-220-only maintenance build) |
| Starter kit SKU | HX-200-KIT-01 | HX-210-KIT-03 | HX-220-KIT-05 |

Alarm set points (factory defaults):

| Gas | HX-200 | HX-210 | HX-220 |
|---|---|---|---|
| H2S low / high / STEL / TWA | 5 / 10 / 15 / 5 ppm | 5 / 10 / 15 / 5 ppm | 5 / 15 / 15 / 5 ppm |
| CO low / high / STEL / TWA | n/a | 30 / 100 / 200 / 30 ppm | 35 / 200 / 200 / 30 ppm |
| LEL low / high | n/a | 10 / 20 % LEL | 10 / 25 % LEL |
| O2 low / high | n/a | 19.5 / 23.0 % vol | 19.5 / 23.5 % vol |

Part numbers: sensors HX-SNS-H2S-02, HX-SNS-CO-03, HX-SNS-LEL-04, HX-SNS-O2-01; docking station HX-DOCK-4
(4-bay; dock firmware D-2.1.0 required for instrument firmware 3.4.1 and later; D-2.0.x supports up to 3.4.0);
HX-200 dock adapter HX-ADP-200; optional sampling pump HX-PMP-22 (HX-220 only); regulators MI-REG-05 (0.5 L/min),
MI-REG-10 (1.0 L/min); cal gas MI-CAL-H2S25, MI-CAL-Q4-A (discontinued 2025), MI-CAL-Q4-B. PC software: Meridian Link.

### 1.2 HX-210 calibration procedure v1 -> v2 (superseded)

| Item | v1 (prod-hx210-calibration-procedure-v1, 2024-03-12) | v2 (prod-hx210-calibration-procedure-v2, 2025-05-19) |
|---|---|---|
| Interval | 180 days | 120 days |
| Gas mix | MI-CAL-Q4-A: 2.5% vol CH4 (50% LEL on 5.0% vol LEL basis), 18.0% O2, 100 ppm CO, 25 ppm H2S | MI-CAL-Q4-B: 1.1% vol CH4 (25% LEL on 4.4% vol basis, IEC 60079-20-1), 18.0% O2, 50 ppm CO, 15 ppm H2S |
| Flow / regulator | 0.5 L/min, MI-REG-05 | 1.0 L/min, MI-REG-10 |
| Span stabilisation wait | 90 s | 120 s |
| Minimum firmware | none | 3.4.1 |
| Dock firmware | any | D-2.1.0 |

### 1.3 Firmware release notes (superseded)
- prod-halcyon-firmware-release-notes-3-4-0 (v1, 2024-11-04): 3.4.0 for HX-210/HX-220.
- prod-halcyon-firmware-release-notes-3-4-1 (v2, 2025-06-02): 3.4.1 supersedes 3.4.0; fixes; notes that 3.4.2 is an HX-220-only maintenance build; HX-200 stays on 3.2.x branch (3.2.4).

### 1.4 Tessera
- TS-4 gateway: up to 64 TS-4e nodes; TesseraMesh 2.4 GHz IEEE 802.15.4; backhaul SKUs TS4-GW-LTE (LTE Cat-M1 + Ethernet) and TS4-GW-ETH (Ethernet only); PoE IEEE 802.3at; current gateway firmware 5.3.0 (2025-08-18); local UI https://192.168.50.1; factory reset: hold RESET 15 s.
- TS-4e edge node TS4E-NODE-3AX: triaxial MEMS, 2 Hz - 8 kHz, +/-50 g, battery life 5 years at 1 sample/hour, -40 to +85 C, M6 stud torque 6 N m; Zone 2 variant TS4E-NODE-3AX-Z2 requires node firmware 2.8.0; current node firmware 2.8.0.
- Compatibility: node 2.6.1 works with gateway 5.1.x/5.2.x/5.3.0 (deprecated on 5.3.0); node 2.7.0 works with 5.2.x and 5.3.0 only; node 2.8.0 works with 5.3.0 only. TesseraCloud requires gateway firmware >= 5.2.0 from 2025-10-01.
- TesseraCloud API base https://api.tesseracloud.meridian-instruments.com; endpoints /v2/devices, /v2/devices/{id}, /v2/devices/{id}/readings, /v2/devices/{id}/alarms, /v2/sites/{siteId}/assets, /v2/webhooks, /v2/exports.
- API rate limits v1 (prod-tesseracloud-api-rate-limits-v1, 2024-06-03): 600 req/min per tenant (all plans), burst 100 per 10 s, readings endpoint 120 req/min per tenant, max page size 1000.
- API rate limits v2 (prod-tesseracloud-api-rate-limits-v2, 2025-07-01): Standard 300 req/min, Enterprise 1,200 req/min, readings 30 req/min per device, max page size 500, headers X-RateLimit-Limit / X-RateLimit-Remaining / X-RateLimit-Reset, bulk via POST /v2/exports, webhook deliveries not counted.
- TesseraCloud data retention: raw readings 13 months (Standard), 36 months (Enterprise).

### 1.5 Lumen LF-60 vs LF-60P

| Parameter | LF-60 | LF-60P |
|---|---|---|
| Pipe sizes | DN25-DN200 | DN25-DN150 |
| Pressure rating | PN16 (16 bar) | PN40 (40 bar) |
| Accuracy | +/-1.0% of reading | +/-0.75% of reading |
| Velocity range | 0.1-12 m/s | 0.1-15 m/s |
| Fluid temperature | -10 to +80 C | -20 to +120 C |
| Ambient temperature | -20 to +60 C | -40 to +60 C |
| Outputs | 4-20 mA, pulse, Modbus RTU | 4-20 mA with HART 7, pulse, Modbus RTU |
| Power | 24 VDC +/-10%, 4 W | 24 VDC +/-10%, 5.5 W |
| Ingress | IP65 | IP67 |
| Housing / wetted | aluminium / 316L | 316 stainless / 316L (Hastelloy C-276 option) |
| Firmware | 1.8.2 | 1.9.0 |
| Weight DN50 | 6.2 kg | 9.8 kg |
| Straight run | 10D upstream / 5D downstream | 15D upstream / 5D downstream |
| Modbus defaults | address 1, 9600 baud 8N1 | address 1, 19200 baud 8E1 |
| Flange bolt torque DN50 | 4 x M16, 60 N m | 4 x M16, 85 N m |
| Flange bolt torque DN100 | 8 x M16, 70 N m | 8 x M20, 140 N m |
| DN50 SKU | LF60-050-PN16 | LF60P-050-PN40 |

### 1.6 Support
- Portal: Meridian Support Centre (MSC), support.meridian-instruments.com; ticket numbers MSC-YYYY-NNNNNN.
- Priority codes: P1 Critical, P2 High, P3 Medium, P4 Low (defined in sup-ticket-priority-codes).
- SLA tiers (sup-sla-tiers): Essential / Professional / Premier. Hours Mon-Fri 08:00-18:00 local / Mon-Fri 07:00-20:00 local / 24x7x365. P1 response 8 business hours / 4 hours / 1 hour. P2 2 business days / 8 business hours / 4 hours. P3 5 business days / 2 business days / 1 business day. P4 best effort / 5 business days / 3 business days. TesseraCloud uptime 99.5% / 99.7% / 99.9%. Advance replacement: no / yes / yes next business day. Named Technical Account Manager: Premier only.
- Support contract prices per site per year (sales-support-contract-price-list): Essential GBP 1,200 / EUR 1,395 / USD 1,510 / MYR 6,950; Professional GBP 3,600 / EUR 4,180 / USD 4,540 / MYR 20,850; Premier GBP 9,800 / EUR 11,380 / USD 12,350 / MYR 56,750.
- RMA v1 (sup-rma-process-v1, 2024-02-01): email rma@meridian-instruments.com; RMA-24-NNNNN; valid 30 days; all EMEA returns to Leeds, Americas to Austin, APAC to Penang; decontamination declaration recommended; advance replacement Premier only; turnaround 15 business days.
- RMA v2 (sup-rma-process-v2, 2025-04-01): raised in MSC portal, linked MSC ticket required; RMA-YYYY-NNNNNN; valid 21 days; UK returns Leeds, EU (non-UK) returns Rotterdam hub, Americas Austin, APAC Penang; decontamination declaration form MI-F-031 mandatory (units without it quarantined and returned at customer cost); photos required for physical damage claims; advance replacement Professional and Premier; turnaround 10 business days.
- Warranty 2024 (sup-warranty-terms-2024, effective 2024-04-01): Halcyon instruments 24 months; sensors 12 months; batteries 12 months; TS-4 24 months; TS-4e 24 months; Lumen 36 months; claims within 30 days of discovery.
- Warranty 2025 (sup-warranty-terms-2025, effective 2025-04-01): Halcyon instruments 36 months; sensors 24 months except O2 sensor HX-SNS-O2-01 18 months; batteries 12 months; TS-4 36 months; TS-4e 24 months; Lumen 36 months, LF-60P wetted parts 60 months; product registration within 90 days required for the 36-month terms; claims within 30 days.

### 1.7 Halcyon error codes (sup-halcyon-error-codes)
E-101 sensor not detected; E-104 span calibration failed (fix via prod-hx210-calibration-procedure-v2); E-108 calibration overdue; E-111 battery fault; E-113 pump flow fault (HX-220 with HX-PMP-22 only); E-117 LEL sensor poisoned / bridge fault (replace HX-SNS-LEL-04 per sup-hx-sensor-replacement-procedure section 4; do not attempt recalibration); E-122 firmware checksum (reflash via HX-DOCK-4); E-126 real-time clock lost; E-131 Bluetooth module fault (HX-220 only); E-140 datalog memory full.

### 1.8 Tessera and Lumen error codes
- Tessera (sup-tessera-error-codes): T-301 node not joined; T-305 node battery low; T-312 clock drift; T-320 backhaul down; T-327 device certificate expired (re-provision per prod-tesseracloud-admin-guide); T-340 firmware mismatch (see prod-tessera-firmware-compatibility-matrix).
- Lumen (sup-lumen-error-codes): L-201 empty pipe; L-204 low signal quality; L-209 over-range; L-215 coil drive fault; L-222 HART communication fault (LF-60P only); L-230 temperature out of range.

### 1.9 Hardware price list FY2025/26 (sales-price-list-fy25-26, effective 2025-04-01)

| SKU | GBP | EUR | USD | MYR |
|---|---|---|---|---|
| HX-200-KIT-01 | 395 | 459 | 499 | 2,290 |
| HX-210-KIT-03 | 1,150 | 1,335 | 1,450 | 6,650 |
| HX-220-KIT-05 | 1,690 | 1,960 | 2,130 | 9,780 |
| HX-DOCK-4 | 2,450 | 2,845 | 3,090 | 14,190 |
| HX-SNS-LEL-04 | 165 | 192 | 208 | 955 |
| HX-SNS-O2-01 | 95 | 110 | 120 | 550 |
| HX-SNS-CO-03 | 120 | 139 | 151 | 695 |
| HX-SNS-H2S-02 | 130 | 151 | 164 | 755 |
| MI-CAL-Q4-B | 145 | 168 | 183 | 840 |
| TS4-GW-LTE | 1,980 | 2,300 | 2,495 | 11,460 |
| TS4-GW-ETH | 1,740 | 2,020 | 2,190 | 10,070 |
| TS4E-NODE-3AX | 640 | 743 | 806 | 3,700 |
| TC-SUB-STD (per asset per year) | 84 | 97 | 106 | 485 |
| TC-SUB-ENT (per asset per year) | 132 | 153 | 166 | 762 |
| LF60-050-PN16 | 3,250 | 3,775 | 4,095 | 18,800 |
| LF60P-050-PN40 | 4,880 | 5,665 | 6,150 | 28,230 |

### 1.10 Sales
- Discount approval (sales-discount-approval-matrix): up to 10% account manager; >10-20% regional sales manager; >20-30% VP Sales; >30% CFO plus VP Product (Samuel Achterberg) for margin review; deals above GBP 250,000 total contract value go through deal desk.
- Partner programme tiers: Registered / Silver / Gold with partner discounts 15% / 22% / 30%; deal registration protection 90 days.
- Competitors (fictional): Corvane Safety (Corvane GX4 four-gas), Brightline Detection (BL-Quad).
- Demo kits booked in Atlas using order type ZDL, maximum loan 21 days.

## 2. Planted facts by writer group

### 2.A Halcyon documents

#### Fork A (Halcyon) planted facts

### Deep-buried facts
- prod-hx210-user-manual section 11.4: rear housing held by six captive Torx T10 screws, torque 0.45 N m (do not exceed 0.5 N m); rear gasket HX-GSK-210.
- prod-hx210-user-manual section 11.3: sensor life at Menu > Instrument > Service > Sensor Life; replace below 20%; O2 sensor life 24-30 months, others 36 months.
- prod-hx210-user-manual section 11.2: dust membrane HX-MEM-210, inspect weekly, replace every 6 months.
- prod-hx210-user-manual section 12: default supervisor passcode 2107; lock-out 10 minutes after 5 wrong entries; passcode reset only via Meridian Link (Instruments > Security > Reset Passcode) by Fleet Administrator role.
- prod-hx210-user-manual section 10: datalog interval Menu > Supervisor > Datalog > Interval (1 s to 15 min); event log 500 events; Meridian Link 4.2+, Instruments > Download Logs; E-126 after battery removed >72 h.
- prod-hx210-user-manual section 6: backlight timeout default 15 s; menu auto-closes after 30 s.
- prod-hx210-user-manual section 7: low-battery warning at ~30 min remaining; LEL over-range latches LEL channel out; O2 high cannot exceed 23.5%, LEL high cannot exceed 60% LEL.
- prod-hx210-user-manual section 15.2: HX-BAT-210 ships as UN3481; spare packs max 30% state of charge for air freight.
- prod-hx210-user-manual section 14: T90 response O2 15 s, LEL 20 s, CO 25 s, H2S 30 s.
- prod-hx220-user-manual section 8: man-down timer default 60 s (range 30-180 s, Menu > Supervisor > Lone Worker > No-Motion Time), pre-alarm fixed 15 s; PANIC button hold 2 s.
- prod-hx220-user-manual section 10: HX-PMP-22 flow 0.5 L/min, max 30 m tubing, allow 2 s per metre + 60 s; E-113 below 0.3 L/min; runtime with pump ~15 h.
- prod-hx220-user-manual section 13/14: eight Torx T10 rear screws at 0.45 N m; gasket HX-GSK-220; supervisor passcode 2207; cal interval settable 30-150 days.
- prod-hx220-user-manual section 9: Meridian Link Mobile requires Android 11+/iOS 16+; pairing via Menu > Wireless > Pair six-digit code.
- prod-hx200-user-manual: four Torx T8 rear screws at 0.35 N m; supervisor passcode 2007; warm-up 30 s (HX-210/220: 45 s); bump pass >= 20 ppm within 45 s; event log 250 events.
- sup-hx-sensor-replacement-procedure section 4: LEL sensor retaining bracket two Torx T6 screws at 0.25 N m; new LEL sensor warm-up 4 hours before calibration; section 5: CO/H2S warm-up 1 h, O2 30 min; sensors not to be fitted if >6 months past date code.
- prod-halcyon-docking-station-guide: dock inlet pressure 0.5-1.0 bar, demand-flow regulator MI-REG-DF; fallback IP 169.254.10.4; leak test fails if >5 mbar drop in 60 s; inlet filter HX-FLT-D4 every 12 months; D-1.9.x end of support 31 March 2025; dock calibrates HX-210/220 in ~4 min; default policy bumps if last bump >20 h old.

### Sibling differences beyond canonical table
- Warm-up: HX-200 30 s; HX-210/HX-220 45 s.
- Event log: HX-200 250, HX-210 500, HX-220 1,000.
- Rear screws: HX-200 4 x T8 at 0.35 N m; HX-210 6 x T10 at 0.45 N m; HX-220 8 x T10 at 0.45 N m.
- Supervisor passcodes: HX-200 2007, HX-210 2107, HX-220 2207.
- Membranes HX-MEM-200/210/220, gaskets HX-GSK-200/210/220, cal caps HX-CAP-200/210/220 are model-specific.
- HX-220 has blue Bluetooth LED and PANIC button; HX-220 Zone 0 prohibited.

### Superseded pairs
- Calibration procedure v1 -> v2 (see canonical 1.2); v2 also sets MI-CAL-Q4-A must not be used after 30 September 2025; field study of 1,900 instruments showed up to 18% LEL drift between 120 and 180 days.
- Firmware notes 3.4.0 -> 3.4.1: 3.4.0 known issue (E-104 on LEL below 30% LEL, keep MI-CAL-Q4-A) fixed in 3.4.1; minimum Meridian Link 4.1 -> 4.2; dock D-2.0.0 -> D-2.1.0; Meridian Link Mobile 2.3 -> 2.5; 3.4.1 %LEL readings ~13.6% higher for same methane; 3.4.2 HX-220-only build fixes GPS below -30 C; man-down timer was fixed 90 s before 3.4.0.

### Cross-document chains
- sup-halcyon-error-codes E-117 -> sup-hx-sensor-replacement-procedure section 4 (warm-up 4 h, T6 0.25 N m) -> prod-hx210-calibration-procedure-v2.
- sup-halcyon-error-codes E-104 -> prod-hx210-calibration-procedure-v2 section 5 (MI-CAL-Q4-A is most common cause).
- prod-halcyon-accessories-catalogue -> sales-price-list-fy25-26 for prices.
- prod-hx210-user-manual section 13 troubleshooting row -> prod-halcyon-docking-station-guide (dock firmware D-2.1.0).
- sup-halcyon-error-codes escalation: >3 E-117 at one site in 30 days -> P2 (priority codes in sup-ticket-priority-codes).

### 2.B Tessera documents

#### Tessera planted facts (fork B)

### Deep-buried facts
- prod-ts4-gateway-installation-guide s4: bracket TS4-BRK-01 fixed with four M8 bolts at 12 N m (max 15 N m); M5 locking screws 3 N m; bracket fits poles 40-110 mm.
- s5: N-type antenna connectors 1.7 N m; remote antenna runs under 10 m with lightning arrestor TS4-LPA-01; mesh and LTE antennas at least 30 cm apart.
- s6: TS4-GW-LTE needs IEEE 802.3at PoE+ (802.3af insufficient); DC 10-30 VDC, 6.5 W typical / 11 W peak; 2 A slow-blow fuse; supercapacitor hold-up ~20 s.
- s7.2: nano-SIM (4FF); APN at Network > Cellular > APN; TS4-SIM-GLB APN "meridian.iot"; Ethernet-to-LTE failover after 90 s.
- s8: local UI https://192.168.50.1 via SVC port, user "admin", password min 12 chars, 15-min session timeout.
- s11.1: mesh channels 11-26, factory default channel 15; path Mesh > Radio > Channel Plan; Adaptive mode up to four channels; channel change propagates in up to 3 minutes.
- s11.2: maximum hop count 4 (design for 3); max 24 first-hop nodes.
- s11.3: TX power default +8 dBm, max +10 dBm (Mesh > Radio > TX Power).
- s12.1: cable entries M20 x 1.5 (Ethernet), M20 x 1.5 (DC power), M16 x 1.5 (service/IO); gland nuts 4 N m (M20), 2.5 N m (M16); gland kit TS4-GLD-KIT-02 replaces TS4-GLD-KIT-01.
- s12.2: operating -30 to +70 C, IP66, 240 x 180 x 90 mm.
- s13: clock drift >2 s raises T-312; TLS fails when clock off >24 h; heartbeat 60 s, Offline after 10 missed heartbeats (10 min); buffer 256 MB ~30 days at 64 nodes, T-348 at 90%, upload 2,000 readings/min.
- s14: firmware update ~6 min; A/B rollback after 3 failed boots; node updates ~40 min per node, 8 in parallel.
- s15: factory reset = hold RESET 15 s (rapid amber flash); 2-5 s press = reboot only; certificate retained.
- prod-tesseracloud-admin-guide: tenant ID format TC-T-NNNNNN; regions EU1 Amsterdam / US1 Central US / AP1 Singapore (s2.2); min two Tenant Owners; web session idle 30 min; SAML ACS URL https://login.tesseracloud.meridian-instruments.com/saml/acs, entity ID urn:tesseracloud:tenant:{tenantId} (s3.2); SCIM Enterprise only, deprovisioned deleted after 30 days; five roles (s4); Standard max 50 sites (s5.1); Transfer Site path Fleet > Gateways > [gateway] > Actions > Transfer Site, historical readings stay with original site (s6.3); firmware campaign up to 200 gateways; alarm persistence default 3, ack timeout 15 min (5-240), 3 escalation levels (s7); SMS Enterprise only 1,000/month; webhook retries 6 (s8); retention path Settings > Sites > Data Retention > Downsampling, options 6/3 months, hourly aggregates kept 7 years, nightly job 02:00 UTC (s9); API keys max 20, lifetime 365 days, warnings 30 and 7 days, revoked within 60 s (s10); gateway certs valid 3 years, auto-renew 60 days before on fw >=5.2.0; T-327 re-provisioning: Fleet > Gateways > [gateway] > Security > Re-provision Certificate, 12-char token XXXX-XXXX-XXXX valid 24 h, entered at local UI System > Cloud > Provisioning Token (s11.2); maintenance mode max 72 hours per activation (s12); audit log retained 400 days, CSV export up to 100,000 rows (s13); exports max 31 days raw, files kept 7 days, 3 concurrent jobs (s14); support access 7 days at Settings > Security > Support Access (s15); tenant deletion 30-day grace (s16); dashboards max 24 widgets, scheduled reports max 50 and 25 recipients, default 07:00 (s18); Integrator accounts max 10 per tenant, node export up to 5,000 nodes (s19).

### Superseded: TesseraCloud API rate limits v1 -> v2
- v1 (2024-06-03): 600 req/min per tenant all plans; burst 100/10 s; readings 120 req/min per tenant; max page 1000; temporary increases up to 30 days.
- v2 (2025-07-01): Standard 300 / Enterprise 1,200 req/min; burst 50 / 200 per 10 s; readings 30 req/min per device; max page 500; X-RateLimit-* headers; POST /v2/exports 10 jobs/hour; no temporary increases; key suspended 10 min after 15 min of continuous 429s; error code TC-ERR-429.

### Other facts
- Compatibility matrix: node 2.6.1 on 5.1.x/5.2.x/5.3.0 (deprecated on 5.3.0); 2.7.0 on 5.2.x/5.3.0; 2.8.0 on 5.3.0 only; TesseraCloud minimum 5.1.0 until 2025-09-30, 5.2.0 from 2025-10-01.
- 5.3.0 release notes: image ts4gw-5.3.0.tfw 38.4 MB; direct upgrade from 5.1.2+ (5.1.0/5.1.1 go to 5.1.2 first); LTE attach 75 s -> 30 s; failover 180 s -> 90 s.
- TS-4e datasheet: Z2 variant marking II 3G Ex ec IIC T4 Gc, operating -40 to +70 C, sealed battery; standard node battery TS4E-BAT-01; IP68 (2 m, 24 h); weight 165 g / 180 g; battery life 5 y @1/h, 3 y @1/30 min, 14 months @1/10 min; magnetic mount limits range to ~2 kHz; epoxy pad cure 24 h.
- Gateway datasheet: TS4-GW-ETH works on 802.3af, 4.0 W; LTE variant 6.5 W; weights 1.9 kg / 1.8 kg.
- API reference: /v1 API retired 2024-03-31; readings max range 7 days per request; cursors expire after 10 min; key prefixes tc_live_/tc_test_; sandbox URL; webhook must respond within 10 s.
- Alarm guide: Group 2 rigid 1.4 / 2.8 / 4.5 mm/s; Group 1 rigid 2.3 / 4.5 / 7.1; Group 1 flexible 3.5 / 7.1 / 11.0; Group 2 flexible 2.3 / 4.5 / 7.1; Warning at B/C, Alarm at C/D; baseline after 14 days, 2.5x / 4x baseline; hysteresis 10%; example 90 kW pump -> 2.8 / 4.5 mm/s.
- KB-TS-0142: firewall blocking TCP 8883 is the top cause (34%).

### Cross-document chains
- sup-tessera-error-codes T-327 -> prod-tesseracloud-admin-guide s11.2 (provisioning token procedure).
- sup-tessera-error-codes T-340 -> prod-tessera-firmware-compatibility-matrix (node 2.8.0 needs gateway 5.3.0).
- sup-kb-ts4-gateway-offline -> admin guide s11.2; priority P1/P2 -> sup-ticket-priority-codes.
- prod-tesseracloud-api-reference s8 -> prod-tesseracloud-api-rate-limits-v2.
- prod-ts4e-edge-node-datasheet: Z2 node requires fw 2.8.0 -> compatibility matrix -> gateway 5.3.0.

### 2.C Lumen and portfolio documents

#### Fork C planted facts (Lumen, portfolio, Lumen sales)

### Deep-buried facts (prod-lf60p-installation-manual, ~3,300 words)
- Section 5: flooded pits OK up to 1 m for 30 min (IP67); prolonged submersion needs remote-mount kit LF-RMK-10 (electronics up to 10 m away); upstream 25D after control valve / out-of-plane elbows; 20D after pump discharge.
- Section 6: face-to-face lengths 200 mm (DN25-DN50), 250 mm (DN65-DN100), 300 mm (DN125-DN150).
- Section 8.2/8.3: terminal map (1/2 power, 3/4 4-20 mA HART, 5/6 pulse max 2 kHz, 7/8 RS-485, 9 earth); RS-485 termination DIP switch S1; inrush 900 mA for 20 ms; start-up ~25 s.
- Section 9.2: grounding rings mandatory on plastic or lined pipe; part numbers LF-GR-<size>-P (316L) and LF-GR-<size>-HC (Hastelloy), e.g. LF-GR-050-P; each ring adds 3 mm (6 mm total). Ground resistance < 1 ohm (9.1).
- Section 10: HART device revision 3; loop resistance >= 250 ohm; max passive loop 545 ohm at 24 V; menu path Menu > Setup > Outputs > HART > Poll Address (default 0; 1-63 multidrop); long tag 32 chars; burst mode disabled by default; loop test times out after 10 minutes.
- Section 11: LF-60P PN40 torque table: DN25 4xM12 45; DN40 4xM16 75; DN50 4xM16 85; DN65 8xM16 80; DN80 8xM16 95; DN100 8xM20 140; DN125 8xM24 210; DN150 8xM24 250 N m. PTFE gaskets -20%. Sequence 30/60/100% cross pattern + circular check; pressure test 1.5x design, never above 60 bar; re-torque after first heat cycle for lines above 80 C. ASME supplement LF-ASME-300.
- Section 12: zero adjust Menu > Service > Calibration > Zero Adjust; 60 s averaging; rejected if offset > +/-2.0 cm/s; Restore Factory Zero path. 12.1: verification Menu > Diagnostics > Verification (HART command 140); L-204 below 60% signal quality, hold/fail-safe below 40%; fail-safe 3.6 mA default (21.0 mA option); window inspection every 12 months; 5% citric acid for scale. 12.2: damping default 2.0 s (0.2-60 s); low-flow cut-off 0.02 m/s.
- Section 13: LF-60P default service passcode 4060; change via Menu > Service > Security > Change Passcode; lost passcode reset only via Meridian Link with reset token from support; DIP switch S3 write protection; 5-minute timeout.

### LF-60 vs LF-60P differences beyond canonical table
- Service passcode: LF-60 1060 vs LF-60P 4060.
- Serial prefix: LS (LF-60, e.g. LS24-011583) vs LP (LF-60P, e.g. LP25-004127).
- Grounding ring parts: LF-GR-050-S (LF-60) vs LF-GR-050-P / -HC (LF-60P).
- Damping default: 3.0 s (LF-60) vs 2.0 s (LF-60P). Zero-offset rejection limit +/-2.5 cm/s (LF-60) vs +/-2.0 cm/s (LF-60P).
- Pulse max 1 kHz (LF-60) vs 2 kHz (LF-60P). Repeatability +/-0.2% vs +/-0.15%. Seals EPDM vs FKM.
- Pressure test cap: never above 24 bar (LF-60) vs 60 bar (LF-60P). Start-up 20 s vs 25 s. Supply current ~170 mA vs ~230 mA.
- Upstream after control valve: 20D (LF-60) vs 25D (LF-60P). LF-60 max weight 41 kg at DN200; LF-60P 38 kg at DN150.
- LF-60 torque table (prod-lf60-installation-manual section 11): DN25 35, DN40 50, DN50 60, DN65 55, DN80 65, DN100 70 (8xM16), DN125 85, DN150 120 (8xM20), DN200 130 (12xM20) N m.
- PED category: LF-60 SEP DN25-50, cat I DN65-200; LF-60P cat I DN25-50, cat II DN65-150. ASME option Class 150 vs Class 300.
- LF-60 has no HART; menu Menu > Setup > Outputs > Current > Range.

### Modbus register map (prod-lumen-modbus-register-map)
- LF-60P-only registers: 30013 process pressure, 30019 HART status, 30025 verification code, 40020 HART poll address, 40021 pressure unit (LF-60 returns exception 02).
- 30018 active error code (lowest-numbered when several); 40013 totaliser reset write 0xA5A5; 40030 apply comm settings; word order ABCD from fw 1.8.0 (CDAB before); max 32 meters per RS-485 segment; poll no faster than 200 ms; S3 write protection gives exception 04.

### Firmware 1.9.0 (prod-lumen-firmware-1-9-0-release-notes)
- LF-60P only; requires 1.8.2 first; Meridian Link 4.2+; image LF60P_1.9.0.mfw; update ~6 min; fixes false L-204 in aerated flow above 10 m/s; L-222 auto-clears after 30 s; known issue with -HC verification code.

### Error codes (sup-lumen-error-codes)
- Added L-218 optical window contamination (not in canonical list). Cross-doc chains: L-204 -> LF-60P manual section 9.2 grounding rings; L-222 -> section 10.1 loop resistance 250 ohm; L-218 -> section 12.1 window cleaning. L-215 escalate P3 (P2 if billing/custody transfer). L-209 output saturates at 20.5 mA.

### Sales playbook (sales-lumen-water-utilities-playbook)
- Water utilities ~45% of Lumen unit volume FY2024/25; default to LF-60; LF-60P when >16 bar, flooded chambers, HART, ±0.75% needed, ambient below -20 C; DN200 must be LF-60; not for sludge/grit. Prices deferred to sales-price-list-fy25-26 and discounts to sales-discount-approval-matrix (chain); position Professional support tier for utilities (chain to sup-sla-tiers). Price holds beyond current FY need regional sales manager approval.

### Portfolio (prod-portfolio-overview-fy26)
- Product line managers: Halcyon Aisha Rahman, Tessera Tom Brennan, Lumen Femke de Vries; all report to Samuel Achterberg.
- Manufacturing: Halcyon Penang (sensor qualification Leeds); Tessera hardware Leeds; TesseraCloud Austin; Lumen Rotterdam.

### 2.D Support documents

#### Facts planted by fork D (support documents)

#### Deep-buried facts in sup-troubleshooting-compendium (about 4,200 words)
- Section 11.1: cold-weather runtime derating is 25% at -10 C and 40% at -20 C. An HX-210 (14 h) should be planned for about 8.4 h at -20 C. HX-220 should be planned for no more than 11 h at -40 C because the display heater runs below -20 C.
- Section 11.2: at -20 C the H2S/CO T90 response times roughly double. Bump test at room temperature after 30 min acclimatisation.
- Section 11.3: moving from cold to warm causes an O2 dip of up to 0.5% vol, which recovers within 5 min.
- Section 12.1: TesseraMesh RSSI table. -70 dBm or stronger is good; -71 to -82 acceptable; -83 to -88 marginal; weaker than -88 dBm unreliable. Packet loss above 5% over 24 h means the link is marginal. The path is Fleet > Nodes > [node] > Radio.
- Section 12.2: maximum 4 mesh hops. Split a site across two gateways when it has more than 40 nodes in a dense metal environment.
- Section 12.3: TesseraMesh defaults to 802.15.4 channel 25. Move it to channel 15 when site Wi-Fi uses channel 13, via Network > Mesh > Channel in the gateway local UI.
- Section 13.1: Lumen empty-pipe detection delay is set at Menu > Diagnostics > Empty Pipe > Detection Delay. The default is 5 s, 30 s is recommended for gravity/pump-cycled lines, and the maximum is 120 s. It can also be set over HART on the LF-60P.
- Section 13.3: flange bolts are torqued in a star pattern in three passes (30%, 70%, 100%). DN50 is 60 N m on the LF-60 and 85 N m on the LF-60P.
- Section 13.4: HART needs at least 250 ohm loop resistance. Section 13.5: RS-485 uses 120 ohm termination at both ends only.
- Section 6.3: the HX-220 man-down alarm defaults to 60 s with a 15 s pre-alarm. It can be raised to 180 s at Menu > Settings > Man-Down > Timer.
- Section 7.2: the dock inlet tubing must be no longer than 10 m. Replace the dock inlet filter every 6 months.
- Section 7.3: 3.4.2 fixed an HX-220 Bluetooth pairing loss after 72 h of continuous connection on 3.4.1. Forget the pairing at Menu > Settings > Bluetooth > Forget Paired Device.
- Section 5.4: the CO sensor's hydrogen cross-sensitivity is about 40%.
- Section 4.1: battery health is at Menu > Instrument > Battery > Health. Replace the pack when health is below 70%.
- Section 8.2: TS4-GW-LTE signal is marginal when RSRP is below -110 dBm. Section 8.1: the gateway needs outbound TCP 443 and 8883.
- Section 10.4: TesseraCloud learns a baseline over 14 days, and adaptive alarms are suppressed during that time. Section 10.5: the gateway buffers 7 days of readings.
- Section 15: the Halcyon service bundle is exported with Instrument > Export > Service Bundle as a .mlz file. The TS-4 support bundle is at System > Diagnostics > Download Support Bundle and covers 7 days.
- Section 16: revision history, compendium version 4 (2025-09-15).

#### Cross-document chains
- sales-support-contract-price-list gives the tier prices. sup-sla-tiers gives the response times. Example: Professional costs GBP 3,600, and its P1 response is 4 hours.
- sup-ticket-priority-codes defines P1-P4 but defers response times to sup-sla-tiers.
- sup-field-service-rates says included visits depend on the tier. sup-sla-tiers says Professional includes 2 preventive visits a year and Premier includes 4. The standard extra day rate is GBP 1,150 / EUR 1,335 / USD 1,450 / MYR 6,650.
- sup-sla-tiers section 5 points to sup-field-service-rates for extra visits.
- The compendium points to sup-halcyon-error-codes, sup-hx-sensor-replacement-procedure (section 4 for E-117), prod-hx210-calibration-procedure-v2, prod-tessera-firmware-compatibility-matrix, prod-tesseracloud-admin-guide (T-327 certificate), prod-tesseracloud-api-rate-limits-v2 and prod-lf60p-installation-manual.
- sup-warranty-terms-2025 section 7 says the HX-210 warranty needs calibration every 120 days under prod-hx210-calibration-procedure-v2.
- sup-rma-process-v2 says advance replacement is available to Professional and Premier customers (see sup-sla-tiers).

#### Other planted facts
- sup-sla-tiers: target P1 restoration is 2 business days (Essential), 1 business day (Professional) and 8 h (Premier).
- sup-sla-tiers service credits are 5/10/15% (Essential), 10/15/25% (Professional) and 10/20/30% (Premier), capped at 30%. They must be claimed within 30 days. Scheduled maintenance is capped at 6 h/month with 5 business days' notice.
- sup-sla-tiers: the Premier P1 on-site commitment is 48 h in the UK, NL, BE, DE, the continental US and Peninsular Malaysia.
- sup-ticket-priority-codes: a comment starting "PRIORITY UPGRADE" is reviewed within 1 h. A P3/P4 ticket reopened more than twice rises one level. Only the duty manager or the regional support manager may downgrade a P1.
- sup-escalation-procedure: a P1 goes from L2 to L3 if not restored in 4 h. The duty manager rota changes at 09:00 Monday, and an unacknowledged page repeats after 15 min. Rosa Delgado is informed within 1 h of a P1 safety ticket. Lars Hedegaard is informed within 4 h of a P1 safety incident. Samuel Achterberg is informed for field notices and stop-ships. P1 customer updates go out every 2 h, and a written summary follows within 5 business days.
- sup-field-service-rates: the half day is GBP 650. Saturday is GBP 1,610 and Sunday is GBP 2,300. The emergency surcharge is GBP 450. Mileage beyond 100 km is GBP 0.65/km. An overnight stay applies beyond 250 km. Late cancellation (under 3 business days) costs 50%.
- RMA: the no-fault-found fee is GBP 95 in both versions. v2 adds EUR 110 / USD 120 / MYR 550. A chargeable quote must be approved within 15 business days.
- Warranty: spare parts are covered for 6 months, and the HX-DOCK-4 for 24 months, in both years. Replacement parts are covered for the remaining period or 90 days, whichever is longer. Under the 2025 terms, an unregistered product falls back to 24 months.

#### v1 -> v2 changes
- RMA: email -> MSC portal with linked ticket. RMA-24-NNNNN -> RMA-YYYY-NNNNNN. Validity 30 -> 21 days. EU returns Leeds -> Rotterdam. Decontamination form recommended -> mandatory MI-F-031. Photos now required. Advance replacement Premier -> Professional+Premier. Turnaround 15 -> 10 business days.
- Warranty: Halcyon 24 -> 36 months (registration within 90 days). Sensors 12 -> 24 months. O2 12 -> 18 months. TS-4 24 -> 36 months. LF-60P wetted parts 60 months. Batteries (12), TS-4e (24) and Lumen (36) unchanged.

### 2.E Sales documents

#### Sales documents (fork E) planted facts

- sales-price-list-fy25-26: prices valid 1 April 2025 - 31 March 2026; old FY2024/25 quotes honoured until expiry but not after 30 June 2025. Volume breaks per order line: 1-24 0%, 25-99 5%, 100-249 8%, 250+ deal desk; not applicable to TC-SUB-STD/ENT or calibration gas. Incoterms FCA invoicing site; DAP freight at cost plus 10%. Minimum order GBP 250 / EUR 290 / USD 315 / MYR 1,450. Lumen lead time 4-6 weeks. TesseraCloud billed per asset, not per node. MI-CAL-Q4-A must not be quoted. Z2 node quoted via deal desk only.
- sales-support-contract-price-list: SKUs SUP-ESS-SITE / SUP-PRO-SITE / SUP-PRM-SITE; prices per canonical table; multi-site discount 1-2 sites 0%, 3-5 7.5%, 6-10 12.5%, 11+ deal desk; response times NOT restated (chain -> sup-sla-tiers). Entitlement appears in MSC within 1 working day; until then tickets default to Essential handling. Upgrades effective first day of next month; downgrades only at renewal.
- Chain: "P1 response time for the GBP 3,600 tier" = sales-support-contract-price-list (Professional) + sup-sla-tiers (4 hours).
- sales-discount-approval-matrix: 0-10% account manager; >10-20% regional sales manager; >20-30% VP Sales; >30% CFO + margin review by VP Product Samuel Achterberg. Deal desk > GBP 250,000 TCV, meets Tuesdays and Thursdays, submissions by 12:00 UK previous working day; quote validity 60 days; approvals on Atlas "Discount Approval" tab. Example: 5% volume + 7% negotiated = 12% -> regional sales manager (in price list).
- sales-competitive-battlecard-halcyon: Corvane GX4 (Zone 1, 16 h, IP65, 30-day datalog, 180-day cal, ~GBP 990); Brightline BL-Quad (Zone 0, 12 h, IP67, 60-day datalog, 90-day cal, ~GBP 1,390). Recommend HX-210 for Zone 0; HX-200 is IIB only (not hydrogen/acetylene).
- sales-tessera-roi-guide: default assumptions downtime -35%, reactive maintenance -20%, route collection eliminated 80%; install 1.5 engineer-hours per node; plan one gateway per 40 nodes in dense steel plant (hard limit 64). Worked example: 40 assets, 80 nodes, 2 TS4-GW-LTE, capex GBP 60,400, opex GBP 6,960/yr, benefit GBP 87,784/yr, payback ~9 months (0.75 yr), 3-year net return GBP 182,072; half-benefit sensitivity ~20 months. Calculator lives on SharePoint under Tessera > Tools.
- sales-partner-program-guide: discounts 15/22/30%; MDF none/2%/3%; Silver min GBP 60,000, Gold GBP 200,000 net purchases; certified service engineers 0/1/3; deal registration 90 days + one 30-day extension, extra 5% project discount, approval within 2 working days; TesseraCloud partner margin 15%, support contracts 10%; Halcyon Service Technician certificate valid 2 years; MDF claims within 60 days, pre-approval 30 days before; tier demotion effective 1 July.
- sales-demo-kit-booking: Atlas order type ZDL; max 21 days, extensions to 35 days with RSM approval; book 5 working days ahead (10 cross-region); kits DK-HX-01..06, DK-HX2-01..03, DK-TS-01..04, DK-LF-01/02; NO LF-60P demo kit (use Leeds flow rig witnessed test); cylinders UN1956 road only; return inspection within 3 working days; >7 days late flagged. Atlas menu Sales > Demo Equipment > Calendar.
- sales-case-study-tank-terminal: fictional Oostkade Tank Storage BV; 86 tanks, 1.2 million m3; 118 TS-4e nodes, 3 TS4-GW-ETH gateways, 52 assets, Enterprise subscription; 60 HX-210 + 45 HX-220 + 4 HX-DOCK-4; Premier support; 2023 bearing failure cost ~EUR 410,000; results: 0 shutdowns, 5 faults detected with 23-day average lead, overdue calibration 14% -> 0%, bump compliance 99.6%, route hours 48 -> 6 per month, 3 man-down events with 4-minute average response; payback ~11 months.

## 3. Superseded pairs (older -> newer; only newer is relevant)

| Older id | Newer id | Key changes |
|---|---|---|
| prod-hx210-calibration-procedure-v1 | prod-hx210-calibration-procedure-v2 | interval 180 -> 120 days; MI-CAL-Q4-A -> MI-CAL-Q4-B (2.5% vol CH4 -> 1.1% vol CH4, 100 -> 50 ppm CO, 25 -> 15 ppm H2S); 0.5 -> 1.0 L/min; 90 -> 120 s |
| prod-halcyon-firmware-release-notes-3-4-0 | prod-halcyon-firmware-release-notes-3-4-1 | current HX-210/HX-220 firmware 3.4.0 -> 3.4.1; dock D-2.1.0 required |
| prod-tesseracloud-api-rate-limits-v1 | prod-tesseracloud-api-rate-limits-v2 | 600 req/min flat -> 300 Standard / 1,200 Enterprise; readings 120/min per tenant -> 30/min per device; page size 1000 -> 500 |
| sup-rma-process-v1 | sup-rma-process-v2 | email -> MSC portal; valid 30 -> 21 days; EU returns Leeds -> Rotterdam; MI-F-031 mandatory; advance replacement Premier -> Professional+Premier; 15 -> 10 business days |
| sup-warranty-terms-2024 | sup-warranty-terms-2025 | Halcyon 24 -> 36 months; sensors 12 -> 24 months (O2 18); TS-4 24 -> 36 months; LF-60P wetted parts 60 months; 90-day registration |

## 4. Confusable sibling sets
- HX-200 / HX-210 / HX-220: prod-hx2x0-user-manual, prod-hx2x0-quick-start, prod-hx2x0-spec-sheet (see 1.1 tables; also passcodes and rear-screw torques in 2.A).
- LF-60 / LF-60P: prod-lf60(-p)-datasheet, prod-lf60(-p)-installation-manual (see 1.5; bolt torque DN50 60 vs 85 N m).
- TS4-GW-LTE vs TS4-GW-ETH in prod-ts4-gateway-datasheet.

## 5. Cross-document chains
- Support price -> tier -> response time: sales-support-contract-price-list (Professional = GBP 3,600) + sup-sla-tiers (Professional P1 response 4 hours).
- Priority definition -> response: sup-ticket-priority-codes (what counts as P1) + sup-sla-tiers (response times).
- E-117 -> sup-halcyon-error-codes -> sup-hx-sensor-replacement-procedure section 4 (HX-SNS-LEL-04, 4-hour warm-up).
- E-104 -> sup-halcyon-error-codes -> prod-hx210-calibration-procedure-v2 section 5 (MI-CAL-Q4-B, 1.0 L/min).
- T-327 -> sup-tessera-error-codes -> prod-tesseracloud-admin-guide (certificate re-provisioning section).
- T-340 -> sup-tessera-error-codes -> prod-tessera-firmware-compatibility-matrix (node 2.8.0 requires gateway 5.3.0).
- Accessory part number -> prod-halcyon-accessories-catalogue -> sales-price-list-fy25-26 (price by currency).
- API rate limit -> prod-tesseracloud-api-reference -> prod-tesseracloud-api-rate-limits-v2.
- Field visits included -> sup-field-service-rates -> sup-sla-tiers (Professional 2 visits/yr, Premier 4).

## 6. Topics deliberately NOT covered (for negative questions)
1. Any Halcyon HX-230 model or a PID/VOC sensor for Halcyon.
2. North American hazardous-location certification (UL/CSA Class I Division 1) for Halcyon.
3. Prices in AUD, JPY or any currency other than GBP/EUR/USD/MYR.
4. TesseraCloud on-premises or private-cloud deployment option.
5. Integration of Tessera with SAP PM / Maximo work orders.
6. A Lumen LF-80 model or clamp-on ultrasonic flow meters.
7. A 5G / NB-IoT backhaul module for the TS-4 gateway.
8. Customer training course prices or certification exam fees for end customers.
9. Equipment leasing or customer financing options (note: WEEE disposal and competitor-dock trade-in ARE mentioned, so avoid those).
10. Support ticket handling in languages other than English, or weekend coverage for Essential beyond what the SLA states.
