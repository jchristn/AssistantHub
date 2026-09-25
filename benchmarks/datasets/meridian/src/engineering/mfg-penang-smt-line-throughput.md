# Penang SMT Line Throughput and Capacity, FY26

Document ID: mfg-penang-smt-line-throughput. Owner: Penang Manufacturing Engineering (Factory Manager: Nurul Aziz). Issued: 2 May 2025. Applies to fiscal year FY26 (1 April 2025 to 31 March 2026).

## Operating pattern

The Penang factory runs its four SMT lines (SMT-1 to SMT-4) on three 8-hour shifts, six days a week (Monday to Saturday). Sunday 08:00 to 14:00 MYT is reserved for preventive maintenance and OT system changes. Planned available time is 144 hours per line per week.

## Line assignments

| Line | Primary board family | Secondary board family |
|---|---|---|
| SMT-1 | HX-MB (Halcyon main board) | LF60-SB (Lumen sensor board) |
| SMT-2 | HX-MB (Halcyon main board) | TS4E-EN (TS-4e edge node) |
| SMT-3 | TS4-GW (TS-4 gateway) | TS4E-EN (TS-4e edge node) |
| SMT-4 | TS4-GW (TS-4 gateway) | LF60-SB (Lumen sensor board) |

## Throughput by line and board

| Line | Board | Boards per panel | Cycle time per panel (s) | Rated boards per hour | FY25 actual average boards per hour | FY25 OEE |
|---|---|---|---|---|---|---|
| SMT-1 | HX-MB | 4 | 38 | 379 | 312 | 78% |
| SMT-1 | LF60-SB | 2 | 52 | 138 | 118 | 81% |
| SMT-2 | HX-MB | 4 | 36 | 400 | 331 | 77% |
| SMT-2 | TS4E-EN | 6 | 44 | 490 | 402 | 74% |
| SMT-3 | TS4-GW | 2 | 61 | 118 | 97 | 79% |
| SMT-3 | TS4E-EN | 6 | 42 | 514 | 436 | 80% |
| SMT-4 | TS4-GW | 2 | 64 | 112 | 90 | 76% |
| SMT-4 | LF60-SB | 2 | 50 | 144 | 121 | 80% |

SMT-2 FY25 OEE for TS4E-EN includes the line stop for INC-2025-014 in March 2025; excluding that event it was 79%.

## FY26 targets

| Measure | Target |
|---|---|
| OEE, all lines | 80% |
| Changeover time | 25 minutes median (see the Penang SMT Line Changeover Runbook) |
| First-pass yield after AOI | 98.5% |
| AOI false call rate | Below 500 ppm |
| Changeovers per line per week | No more than 6 |

## Weekly capacity (planning figures)

Planning uses 85% of rated throughput multiplied by 120 productive hours per week (144 available hours less maintenance, changeovers and breaks).

| Board family | Lines able to run it | Planning capacity per week (single line) |
|---|---|---|
| HX-MB | SMT-1, SMT-2 | about 38,700 (SMT-1) or 40,800 (SMT-2) |
| TS4E-EN | SMT-2, SMT-3 | about 50,000 (SMT-2) or 52,400 (SMT-3) |
| TS4-GW | SMT-3, SMT-4 | about 12,000 (SMT-3) or 11,400 (SMT-4) |
| LF60-SB | SMT-1, SMT-4 | about 14,100 (SMT-1) or 14,700 (SMT-4) |

## Notes

- TS-4 gateway boards (TS4-GW) are the capacity constraint for FY26. A second placement head upgrade on SMT-3 is planned for Q3 FY26.
- Halcyon demand peaks in Q4 (January to March); planners should load HX-MB on both SMT-1 and SMT-2 during that quarter.
- Throughput figures exclude boards scrapped at X-ray or functional test.
