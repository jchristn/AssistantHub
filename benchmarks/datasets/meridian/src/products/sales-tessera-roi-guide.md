# Tessera ROI Calculator Guide

Document ID: sales-tessera-roi-guide | Version: 1 | Effective: 2025-06-05 | Owner: Sales Enablement, Tessera

## 1. Purpose

The Tessera ROI calculator is a spreadsheet model on the Sales Enablement SharePoint site under Tessera > Tools. It estimates the payback period and three-year return of a Tessera vibration-monitoring deployment against the customer's current practice, which is usually monthly route-based data collection with a handheld analyser. This guide explains the inputs, the default assumptions and how to present results. It ends with a worked example.

Always run the calculator with the customer's own figures where they have them. Defaults are for a first conversation only.

## 2. Inputs

### Deployment inputs
- Number of monitored assets (motors, pumps, fans, gearboxes)
- Nodes per asset: default 2. Use 3 for gearbox trains.
- Number of TS-4 gateways: one per 64 nodes as a hard limit. For radio coverage, plan one per 40 nodes in dense steel plant.
- Gateway backhaul: TS4-GW-LTE or TS4-GW-ETH
- Subscription plan: TC-SUB-STD or TC-SUB-ENT
- Installation labour: default 1.5 engineer-hours per node at the customer's labour rate

### Current-state inputs
- Unplanned downtime hours per year on the monitored assets
- Cost of one hour of downtime
- Route-based collection cost: technician hours per month, times labour rate
- Annual reactive maintenance spend on the monitored assets

### Improvement assumptions (defaults)
- Unplanned downtime reduction: 35%
- Reactive maintenance spend reduction: 20%
- Route-based collection eliminated: 80% (some manual rounds normally remain)

These defaults are deliberately conservative. They come from the median of eleven Tessera reference sites measured in FY2024/25. Do not raise the downtime reduction above 50% without a documented customer baseline.

## 3. Cost model

The calculator uses FY2025/26 list prices from the Hardware and Subscription Price List (sales-price-list-fy25-26) and applies any approved discount you enter. Hardware is treated as a year-zero capital cost. Subscriptions are an annual operating cost billed per asset, not per node. TS-4e node batteries last about 5 years at 1 sample per hour, so no battery replacement falls within the three-year horizon. If the customer configures faster sampling, enter a battery replacement cost in year 3.

Support contracts are optional in the model. If you include one, take the per-site price from the Support Contract Price List FY2025/26.

## 4. Worked example (UK, GBP list prices)

A water company pumping station site with 40 critical pumps and motors.

### Deployment
- 40 assets, 2 nodes each = 80 TS4E-NODE-3AX
- 2 x TS4-GW-LTE gateways (80 nodes exceeds one gateway's 64-node limit)
- TC-SUB-STD for 40 assets
- Professional support for 1 site

### Costs
| Item | Calculation | GBP |
|---|---|---|
| Edge nodes | 80 x 640 | 51,200 |
| Volume break on nodes (25-99 units, 5%) | 51,200 x 5% | -2,560 |
| Gateways | 2 x 1,980 | 3,960 |
| Installation | 80 x 1.5 h x GBP 65 | 7,800 |
| Year-zero capital total | | 60,400 |
| TesseraCloud Standard | 40 x 84 per year | 3,360 per year |
| Professional support | 1 site | 3,600 per year |
| Annual operating total | | 6,960 per year |

### Benefits (customer figures)
- Unplanned downtime: 120 hours per year at GBP 1,400 per hour = GBP 168,000. A 35% reduction saves GBP 58,800 per year.
- Route-based collection: 16 hours per month at GBP 65 = GBP 12,480 per year. Eliminating 80% saves GBP 9,984 per year.
- Reactive maintenance: GBP 95,000 per year. A 20% reduction saves GBP 19,000 per year.
- Total annual benefit: GBP 87,784

### Results
- Net annual benefit: 87,784 - 6,960 = GBP 80,824
- Simple payback: 60,400 / 80,824 = 0.75 years, about 9 months
- Three-year net return: (3 x 80,824) - 60,400 = GBP 182,072

## 5. Presenting results

- Show the payback period first. Finance teams respond to months to payback more than to percentages.
- Always show the sensitivity tab. It halves each improvement assumption. For the example above, payback at half benefits is about 20 months.
- If the customer chooses TC-SUB-ENT, point out the longer raw data retention: 36 months against 13 months on Standard. Reliability engineers often need it for seasonal trend analysis.
- Do not present ROI figures as a guarantee. The calculator footer carries the standard disclaimer. Do not remove it.

## 6. Common mistakes

- Billing subscriptions per node instead of per asset. This overstates operating cost by the node-per-asset ratio.
- Forgetting the second gateway once node count exceeds 64.
- Quoting TS4E-NODE-3AX for hazardous areas. Zone 2 needs the TS4E-NODE-3AX-Z2 variant, priced through the deal desk.
- Applying a partner discount and a volume break without checking the total against the Sales Discount Approval Matrix.
