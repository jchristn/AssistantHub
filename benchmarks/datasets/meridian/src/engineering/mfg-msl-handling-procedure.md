# Moisture Sensitive Device (MSL) Handling Procedure

Document ID: mfg-msl-handling-procedure. Owner: Penang Manufacturing Engineering. Effective date: 3 February 2025. Applies to: Penang SMT lines SMT-1 to SMT-4 and the Penang component store.

## Purpose

Plastic-packaged components absorb moisture from the air. If too much moisture is absorbed, the component can crack or delaminate during reflow. Each component has a moisture sensitivity level (MSL) that sets how long it may be exposed to factory air before it must be soldered or baked.

## Floor life by MSL

Floor life applies at factory conditions of 30 °C or below and 60% RH or below.

| MSL | Floor life after opening the dry pack |
|---|---|
| 1 | Unlimited |
| 2 | 1 year |
| 2a | 4 weeks |
| 3 | 168 hours |
| 4 | 72 hours |
| 5 | 48 hours |
| 5a | 24 hours |
| 6 | Must be baked before use; use within the time on the label |

Most Meridian MSL-sensitive parts are MSL 3, including the TS4E-EN radio module and the HX-MB microcontroller.

## Receiving and storage

1. On receipt, check the humidity indicator card in each dry pack. If the 10% spot shows pink, the parts must be baked before use.
2. Unopened dry packs are stored in the component store at below 40 °C and below 90% RH, within 12 months of the bag seal date.
3. Opened reels not in use are stored in dry cabinets at below 5% RH. Time in a dry cabinet does not count towards floor life.

## On the line

- When a dry pack is opened, the operator scans the reel into the Atlas shop-floor module, which starts the floor-life clock.
- The placement machine warns at 80% of floor life and blocks the reel at 100%.
- Reels returned to the dry cabinet within their floor life pause the clock.

## Baking

Parts that exceed floor life, or whose humidity indicator card fails, are baked before use. Components in trays are baked at 125 °C for 24 hours. Components on tape and reel that cannot withstand 125 °C are baked at 40 °C and 5% RH or below for 79 days, so in practice exceeded reels are usually scrapped unless the part is on allocation. Baking resets the floor-life clock. Each component may be baked no more than twice.

## Records

Bake records are kept in the Atlas shop-floor module against the reel ID for 3 years.
