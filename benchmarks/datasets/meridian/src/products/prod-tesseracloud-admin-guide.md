# TesseraCloud Administrator Guide

Document ID: prod-tesseracloud-admin-guide | Version: 2025.2 | Effective: 2025-07-14 | Owner: TesseraCloud Product Team, Meridian Instruments Ltd

## 1. Introduction

TesseraCloud is Meridian Instruments' software-as-a-service platform for the Tessera vibration-monitoring system. It receives telemetry from TS-4 gateways and their TS-4e edge nodes, stores readings, evaluates alarm policies and presents condition information to maintenance and reliability teams. This guide is written for customer administrators, that is, users holding the Tenant Owner or Site Admin role, and for Meridian partners who administer tenants on a customer's behalf.

This edition corresponds to the TesseraCloud 2025.2 release (July 2025). It introduces the per-plan API rate limits described in the TesseraCloud API Rate Limits document (prod-tesseracloud-api-rate-limits-v2), the revised data retention settings in section 8, and the certificate re-provisioning procedure in section 11.

TesseraCloud is offered in two subscription plans, priced per monitored asset per year:

- Standard (SKU TC-SUB-STD): core monitoring, alarm policies, email and Microsoft Teams notifications, 13 months of raw reading retention.
- Enterprise (SKU TC-SUB-ENT): everything in Standard plus SCIM user provisioning, SMS notifications, 36 months of raw reading retention, higher API limits and the advanced analytics workspace.

## 2. Tenancy and data residency

### 2.1 Tenants

Each customer organisation is provisioned as one tenant. A tenant is identified by a tenant ID in the format TC-T-NNNNNN (for example TC-T-004817), shown under Settings > Tenant > Overview. All users, sites, gateways, nodes, alarm policies and API keys belong to exactly one tenant. Data is never shared between tenants.

Large organisations sometimes ask for separate tenants per business unit. Meridian recommends a single tenant with multiple sites instead, because cross-site reporting, shared alarm policies and single sign-on are all tenant-scoped.

### 2.2 Hosting regions

When a tenant is created, it is assigned to one hosting region, which cannot be changed later without a data migration project:

| Region code | Hosting location | Typical customers |
|---|---|---|
| EU1 | Amsterdam, Netherlands | UK, EU, Middle East |
| US1 | Central United States | Americas |
| AP1 | Singapore | Asia-Pacific, including Malaysia |

All readings, configuration and audit data for the tenant remain in its region. Backups are replicated to a second availability zone within the same region.

### 2.3 Tenant Owner responsibilities

Every tenant must have at least two users with the Tenant Owner role. TesseraCloud prevents removing or demoting the last remaining Tenant Owner. Tenant Owners receive service notifications, including planned maintenance announcements, certificate expiry warnings and API key expiry warnings.

## 3. Signing in and single sign-on

### 3.1 Local accounts

Users can sign in with a local TesseraCloud account (email address and password). Local accounts require multi-factor authentication with an authenticator app; SMS one-time codes are not offered for sign-in. Passwords must be at least 12 characters. An idle web session expires after 30 minutes.

### 3.2 SAML 2.0 single sign-on

TesseraCloud supports SAML 2.0 single sign-on with any compliant identity provider. Meridian has tested Microsoft Entra ID, Okta and Ping Identity. To configure SSO, a Tenant Owner goes to Settings > Security > Single Sign-On and enters the identity provider metadata URL or uploads the metadata XML.

Values to give your identity provider team:

- Service provider entity ID: urn:tesseracloud:tenant:{tenantId}, for example urn:tesseracloud:tenant:TC-T-004817.
- Assertion consumer service (ACS) URL: https://login.tesseracloud.meridian-instruments.com/saml/acs
- NameID format: email address.
- Optional attribute "tc_role" to assign a role at sign-in; if absent, new users receive the Viewer role.

Once SSO is enforced (the "Require SSO" switch), local passwords stop working for all users except designated break-glass accounts. At least one Tenant Owner must remain as a break-glass local account so that the tenant can be recovered if the identity provider is unavailable.

### 3.3 SCIM provisioning (Enterprise)

Enterprise tenants can provision and deprovision users automatically using SCIM 2.0. The SCIM base URL is https://api.tesseracloud.meridian-instruments.com/scim/v2 and it authenticates with a dedicated SCIM token generated at Settings > Security > SCIM. Deprovisioned users are deactivated immediately and deleted after 30 days.

## 4. Roles and permissions

TesseraCloud uses five fixed roles. Roles can be granted tenant-wide or restricted to specific sites.

| Role | Typical user | Key permissions |
|---|---|---|
| Tenant Owner | Customer IT or reliability lead | All permissions, including SSO, billing contacts, API keys, support access and tenant deletion |
| Site Admin | Site maintenance manager | Manage sites, assets, gateways, nodes, alarm policies and site users within assigned sites |
| Analyst | Reliability engineer | View all data, create dashboards and reports, acknowledge alarms, run exports |
| Viewer | Operator or manager | View dashboards, assets and alarms; cannot acknowledge alarms |
| Integrator | Service account for integrations | API access only through API keys; no web sign-in |

Custom roles are not supported. A user may hold different roles on different sites; the most permissive applicable role applies to tenant-wide objects.

## 5. Sites and assets

### 5.1 Sites

A site represents a physical location, such as a plant or a pumping station. Create sites at Sites > Add Site. Each site has a name, a time zone, an address and an optional site code used in reports. Standard tenants can create up to 50 sites; Enterprise tenants have no site limit.

The site time zone controls how daily reports and business-hours notification rules are evaluated. Readings are always stored in UTC.

### 5.2 Assets and measurement points

An asset is a monitored machine, such as a pump, fan, motor or gearbox. Each asset contains one or more measurement points, and each measurement point is bound to exactly one TS-4e edge node. Assets are created at Sites > [site] > Assets > Add Asset.

For each asset, record:

- Machine type and machine class (used for ISO 10816-3 alarm defaults; see the Tessera Alarm Configuration Guide, prod-tessera-alarm-configuration-guide).
- Rated power and running speed.
- Foundation type (rigid or flexible).
- Criticality (A, B or C), used to prioritise alarm routing.

Asset subscriptions are counted per asset, not per node. An asset with four measurement points consumes one TC-SUB-STD or TC-SUB-ENT subscription.

## 6. Gateway fleet management

### 6.1 Fleet view

Fleet > Gateways lists all gateways in the tenant with their status (Online, Offline, Degraded), firmware version, backhaul type, last heartbeat and number of paired nodes. A gateway is marked Offline after 10 minutes without a heartbeat. Degraded means the gateway is online but reporting at least one active T-code event.

### 6.2 Adding gateways

Gateways are claimed using the serial number and claim code on the rating label (Fleet > Gateways > Add Gateway). Full installation instructions are in the Tessera TS-4 Gateway Installation Guide (prod-ts4-gateway-installation-guide).

### 6.3 Transferring a gateway to another site

A gateway and all of its paired nodes can be moved from one site to another within the same tenant, for example when a skid is relocated. Go to Fleet > Gateways > [gateway] > Actions > Transfer Site, choose the destination site and confirm.

Behaviour on transfer:

- The paired nodes move with the gateway. Their asset and measurement point bindings are cleared and must be re-created in the destination site.
- Historical readings remain associated with the original site and assets. They are not moved.
- Active alarms on the original assets are closed with the reason "Gateway transferred".
- A transfer can only be performed by a user who is Site Admin on both sites, or a Tenant Owner.

Transfers between tenants are not possible through the portal. A gateway must be released (Actions > Release Gateway) by the current tenant and claimed by the new tenant using the original claim code.

### 6.4 Firmware management

Firmware updates are scheduled at Fleet > Gateways > [gateway] > Firmware > Schedule Update, or in bulk from Fleet > Firmware Campaigns. A campaign can target up to 200 gateways and can be limited to a maintenance window. TesseraCloud checks the firmware compatibility rules before scheduling: it will refuse to schedule node firmware 2.8.0 onto nodes whose gateway is not already on 5.3.0.

From 1 October 2025, TesseraCloud requires gateway firmware 5.2.0 or later. Gateways on 5.1.x will still connect after that date but will be shown as Degraded and cannot receive configuration changes until updated.

## 7. Alarm policies

### 7.1 Policy structure

An alarm policy defines when an alarm is raised and who is notified. A policy has:

- Scope: tenant-wide, site or individual asset.
- Conditions: metric (velocity RMS, acceleration peak, crest factor, temperature, node battery), comparison and threshold.
- Persistence: the number of consecutive readings that must breach the threshold before the alarm is raised. The default is 3.
- Severity: Warning or Alarm.
- Escalation plan: up to 3 escalation levels.

Asset-level policies override site-level policies, which override tenant-wide policies, for the same metric.

### 7.2 Acknowledgement and escalation

When an alarm is raised, level 1 recipients are notified. If the alarm is not acknowledged within the acknowledgement timeout (default 15 minutes, configurable from 5 to 240 minutes), level 2 recipients are notified, and so on to level 3. Acknowledging an alarm stops escalation but does not clear the alarm; an alarm clears automatically when readings fall below the threshold minus the hysteresis band for the persistence count.

### 7.3 Suppression

Alarm suppression rules can silence notifications during known events such as start-up transients. Suppression is set per asset at Sites > [site] > Assets > [asset] > Suppression. Readings continue to be recorded during suppression.

## 8. Notification channels

TesseraCloud supports the following channels:

| Channel | Plans | Notes |
|---|---|---|
| Email | Standard, Enterprise | Sent from alerts@tesseracloud.meridian-instruments.com |
| Microsoft Teams | Standard, Enterprise | Incoming webhook per channel |
| Generic webhook | Standard, Enterprise | HTTPS POST, JSON payload, HMAC-SHA256 signature header X-Tessera-Signature |
| SMS | Enterprise only | 1,000 messages per month included; additional messages billed quarterly |
| Mobile push | Standard, Enterprise | Tessera mobile app, iOS and Android |

Channels are configured at Settings > Notifications > Channels. Webhook deliveries are retried up to 6 times with exponential back-off over approximately 1 hour. Webhook deliveries do not count towards API rate limits.

## 9. Data retention and downsampling

### 9.1 Retention periods

Raw readings are retained according to the subscription plan:

- Standard: 13 months of raw readings.
- Enterprise: 36 months of raw readings.

After the raw retention period, readings are not simply deleted. They are downsampled into hourly aggregates (minimum, maximum, mean and 95th percentile for each metric) which are retained for 7 years. Alarm history and acknowledgement records are retained for 7 years on both plans.

### 9.2 Configuring downsampling

Some customers prefer to downsample earlier, for example to reduce the volume of data exposed to integrations. The setting is at Settings > Sites > Data Retention > Downsampling. The options are:

- Plan default (13 or 36 months, as above).
- Downsample after 6 months.
- Downsample after 3 months.

The raw retention period can be shortened but never extended beyond the plan maximum. Changes take effect at the next nightly retention job, which runs at 02:00 UTC in the tenant's hosting region. Shortening retention is irreversible for data already downsampled.

## 10. API keys and integrations

### 10.1 Creating API keys

API keys are created at Settings > Integrations > API Keys by a Tenant Owner. Each key is bound to either a named user or an Integrator service account and has one or more scopes:

| Scope | Allows |
|---|---|
| devices:read | List and read gateways and nodes |
| devices:write | Rename devices, change reporting intervals |
| readings:read | Read readings and aggregates |
| alarms:read | Read alarms and alarm history |
| alarms:write | Acknowledge alarms |
| exports:write | Create bulk export jobs |
| webhooks:write | Manage webhook subscriptions |

The key secret is shown once at creation. A tenant can have at most 20 active API keys.

### 10.2 Rotation

API keys have a mandatory maximum lifetime of 365 days. Tenant Owners receive an email warning 30 days and again 7 days before a key expires. To rotate a key without downtime, create a new key with the same scopes, deploy it to the integration, then revoke the old key. Revoked keys stop working within 60 seconds.

### 10.3 Rate limits

API requests are rate-limited per tenant according to plan. Current limits, headers and back-off guidance are in the TesseraCloud API Rate Limits document (prod-tesseracloud-api-rate-limits-v2). Endpoint details are in the TesseraCloud REST API Reference (prod-tesseracloud-api-reference).

## 11. Device certificates and re-provisioning

### 11.1 How devices authenticate

Each TS-4 gateway authenticates to TesseraCloud with an X.509 device certificate issued by the Tessera device certificate authority when the gateway is claimed. Gateway certificates are valid for 3 years. Nodes do not hold their own certificates; they are authenticated to the gateway by the mesh network key.

TesseraCloud renews gateway certificates automatically 60 days before expiry, provided the gateway is online and running firmware 5.2.0 or later. Gateways on 5.1.x cannot auto-renew and must be updated first.

### 11.2 Expired or invalid certificates (T-327)

If a gateway certificate expires, for example because the gateway was offline or in storage during the renewal window, the gateway can no longer connect. It reports T-327 (device certificate expired) in its local event log, its status LED flashes red, and TesseraCloud shows it as Offline. The gateway continues to buffer readings.

To re-provision the certificate:

1. In TesseraCloud, go to Fleet > Gateways > [gateway] > Security > Re-provision Certificate. You need the Site Admin role for the gateway's site, or Tenant Owner.
2. TesseraCloud generates a one-time provisioning token (12 characters, grouped as XXXX-XXXX-XXXX). The token is valid for 24 hours and can be used once.
3. Connect a laptop to the gateway's SVC port and open the local interface at https://192.168.50.1.
4. Go to System > Cloud > Provisioning Token, enter the token and select Apply.
5. The gateway requests a new certificate, reconnects, and uploads its buffered readings. The LED returns to solid green within about 3 minutes.

If the token is rejected, check that the gateway clock is correct (System > Time); a clock error of more than 24 hours causes certificate validation to fail. If the gateway is on firmware 5.1.x, update it locally to 5.3.0 before re-provisioning, because the provisioning token page does not exist in 5.1.x.

### 11.3 Revoking a certificate

If a gateway is lost or stolen, revoke its certificate immediately at Fleet > Gateways > [gateway] > Security > Revoke. A revoked gateway cannot reconnect until it is re-provisioned by a Tenant Owner.

## 12. Maintenance mode

Maintenance mode is used during planned work, such as a machine overhaul, when readings are expected to be abnormal. It can be applied to an asset, a gateway or a whole site at Sites > [site] > Maintenance Mode or from the asset page.

While maintenance mode is active:

- Readings continue to be recorded and are flagged "maintenance" in exports.
- Alarm policies are not evaluated and no notifications are sent.
- Gateway offline notifications are suppressed.

Maintenance mode has a maximum duration of 72 hours per activation. When it expires, alarm evaluation resumes automatically; a new activation must be started if the work continues. This limit is deliberate, to avoid assets being left unmonitored indefinitely. Each activation and expiry is recorded in the audit log with the user who started it and the reason entered.

## 13. Audit log

The audit log records security-relevant and configuration events: sign-ins, failed sign-ins, role changes, API key creation and revocation, SSO changes, alarm policy changes, gateway transfers, certificate operations, maintenance mode activations and support access grants.

- View the log at Settings > Security > Audit Log.
- Filter by user, event type, site and date range.
- Export to CSV (up to 100,000 rows per export).
- Audit events are retained for 400 days on both plans.

Enterprise tenants can additionally stream audit events to a SIEM through a webhook subscription with the event type "audit.*".

## 14. Data exports

Bulk historical data should be extracted using export jobs rather than repeated calls to the readings endpoint. Exports can be created in the portal at Reports > Exports > New Export or through the API with POST /v2/exports.

- An export job can cover at most 31 days of raw readings, or 12 months of hourly aggregates.
- Output formats: CSV (gzip compressed) or Parquet.
- Completed export files are available for download for 7 days, after which they are deleted.
- A tenant can run at most 3 export jobs concurrently.

## 15. Support access

Meridian Support engineers cannot see tenant data by default. When a support case requires it, a Tenant Owner grants time-boxed access at Settings > Security > Support Access, entering the Meridian Support Centre ticket number (format MSC-YYYY-NNNNNN). Access is read-only unless "Allow configuration changes" is also selected, and it expires automatically after 7 days. Every action taken by a support engineer is recorded in the audit log under the engineer's named account.

## 16. Tenant offboarding

A Tenant Owner can request tenant deletion at Settings > Tenant > Close Tenant. Deletion is subject to a 30-day grace period during which the tenant is read-only and can be restored by Meridian Support. After the grace period, all data, including downsampled aggregates and audit logs, is permanently deleted within a further 30 days. Customers who need to retain data should run exports before requesting deletion.

## 17. Service limits summary

| Limit | Standard | Enterprise |
|---|---|---|
| Sites per tenant | 50 | Unlimited |
| Raw reading retention | 13 months | 36 months |
| Aggregate retention | 7 years | 7 years |
| Active API keys | 20 | 20 |
| API key maximum lifetime | 365 days | 365 days |
| API rate limit | 300 requests per minute | 1,200 requests per minute |
| Concurrent export jobs | 3 | 3 |
| SMS notifications | Not available | 1,000 per month included |
| Maintenance mode maximum duration | 72 hours | 72 hours |
| Audit log retention | 400 days | 400 days |
| Support access grant | 7 days | 7 days |

## 18. Dashboards and scheduled reports

### 18.1 Dashboards

Every site has a default dashboard showing asset health by ISO 10816-3 zone, active alarms, gateway status and nodes with low battery. Users with the Analyst role or higher can create additional dashboards at Dashboards > New Dashboard. A dashboard can contain up to 24 widgets. Available widget types are trend chart, zone heat map, alarm list, spectrum viewer (Enterprise only), fleet status and KPI tile.

Dashboards can be private, shared with a site, or shared tenant-wide. Tenant-wide dashboards can only be published by a Tenant Owner. Dashboards shared with a site are visible to Viewers of that site, which makes them the preferred way to give operators a read-only overview.

### 18.2 Scheduled reports

Scheduled reports deliver a PDF summary by email. They are configured at Reports > Scheduled Reports > New Report. Options include:

- Frequency: daily, weekly (choose the weekday) or monthly (first working day).
- Delivery time: in the site's time zone; the default is 07:00.
- Content: asset health summary, alarms opened and closed in the period, top 10 assets by velocity RMS change, and node battery forecast.
- Recipients: up to 25 email addresses, which do not need to be TesseraCloud users.

A tenant can have at most 50 active scheduled reports. Reports that fail to generate for three consecutive runs are paused automatically and the report owner is notified.

### 18.3 Mobile app

The Tessera mobile app (iOS and Android) supports sign-in with local accounts and SSO. It provides alarm notifications by push, alarm acknowledgement, asset trends and the node pairing assistant used during installation. The app caches the last 7 days of trend data for offline viewing in plant areas without coverage. Mobile sessions are refreshed silently for up to 14 days; after that the user must sign in again.

## 19. Administrator FAQ

### Can we change the hosting region after the tenant is created?

Not through the portal. A region move is a chargeable data migration project arranged through your Meridian account manager. The tenant is read-only for up to 48 hours during the migration.

### Why does a new user see nothing after signing in with SSO?

New SSO users receive the Viewer role with no site assignments unless the identity provider sends the tc_role attribute. A Tenant Owner or Site Admin must assign sites at Settings > Users > [user] > Site Access.

### A reliability engineer left the company. What happens to the API keys they created?

API keys bound to a named user stop working when that user is deactivated, which is why Meridian recommends binding integration keys to an Integrator service account instead. Keys bound to an Integrator account are unaffected by staff changes.

### How do we find which user acknowledged an alarm?

Open the alarm in the alarm history; the acknowledging user and time are shown. The same information is in the audit log, event type "alarm.acknowledged", for 400 days.

### Can we export the node list with battery status?

Yes. Fleet > Nodes > Export produces a CSV with node serial number, gateway, firmware version, hop count, last reading time and estimated battery percentage. The export includes at most 5,000 nodes; larger tenants should use GET /v2/devices with the readings:read and devices:read scopes.

### Why does a gateway show Degraded when everything seems to work?

Degraded means at least one active T-code event, or firmware below the TesseraCloud minimum (5.2.0 from 1 October 2025). Open the gateway page and check the Events tab. The most common causes are T-312 (clock drift) and T-348 (buffer near capacity after an outage).

### Is there a limit on how many users a tenant can have?

There is no user limit on either plan. Integrator service accounts are limited to 10 per tenant.

## 20. Getting help

For TesseraCloud questions, raise a ticket in the Meridian Support Centre at support.meridian-instruments.com. Include your tenant ID and, for device problems, the gateway serial number and any T-codes displayed. Error codes are explained in the Tessera Error Code Reference (sup-tessera-error-codes). Response targets depend on your support tier as defined in the Support SLA Tiers document (sup-sla-tiers).
