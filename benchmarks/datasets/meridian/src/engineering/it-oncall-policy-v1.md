# On-call Policy (v1)

Document ID: it-oncall-policy-v1. Owner: IT Operations. Version 1. Effective date: 1 May 2023. Approved by: Lars Hedegaard, CTO.

## Scope

This policy applies to engineers in IT Operations, Platform Engineering and IT Security who take part in an out-of-hours on-call rotation. It covers Atlas, Beacon, Forge, the corporate network and TesseraCloud.

## Rotations

- Platform Engineering (TesseraCloud and Forge): one primary and one secondary, weekly rotation handing over Monday 10:00 UK time.
- IT Operations (Atlas, network, directory): one primary, weekly rotation handing over Monday 09:00 UK time.
- IT Security: one primary during business hours only; out-of-hours security incidents are routed to the IT Operations primary.

## Paging service levels

Pages are sent through the paging tool to the on-call phone. Responders must acknowledge within:

| Severity | Acknowledge page within | Start working within |
|---|---|---|
| SEV1 | 15 minutes | 30 minutes |
| SEV2 | 30 minutes | 60 minutes |
| SEV3 | Next business day | Next business day |
| SEV4 | Not paged | Not paged |

If the primary does not acknowledge a SEV1 or SEV2 page, it escalates to the secondary after 15 minutes, and to the team manager after a further 15 minutes.

## Expectations

- Stay within reach of a laptop and a reliable internet connection while on call.
- Do not consume alcohol or anything that would impair judgement while on call.
- Hand over open incidents in writing at the end of the rotation.

## Rest

After being paged between 00:00 and 06:00, the responder may start work up to four hours later the following morning.

## Swaps

Rotation swaps are agreed between engineers and recorded in the rotation calendar at least 24 hours in advance.

## Severity definitions

The severity definitions are listed in Appendix A of this policy (SEV1 critical outage, SEV2 major degradation, SEV3 minor, SEV4 low).
