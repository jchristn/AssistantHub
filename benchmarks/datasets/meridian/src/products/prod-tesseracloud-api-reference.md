# TesseraCloud REST API Reference

Document ID: prod-tesseracloud-api-reference | Version: 2025.2 | Effective: 2025-07-01 | Owner: TesseraCloud Platform Team

## 1. Introduction

The TesseraCloud REST API gives programmatic access to devices, readings, alarms, sites and export jobs in a TesseraCloud tenant. It is used by customers to integrate vibration data with maintenance management systems, historians and data warehouses. All endpoints are versioned under the /v2 path prefix; the earlier /v1 API was retired on 31 March 2024.

Base URL: https://api.tesseracloud.meridian-instruments.com

All requests and responses use JSON (UTF-8) unless stated otherwise. Timestamps are ISO 8601 in UTC, for example 2025-07-01T09:30:00Z.

## 2. Authentication

Requests are authenticated with an API key sent as a bearer token:

Authorization: Bearer tc_live_{key-secret}

API keys are created by a Tenant Owner at Settings > Integrations > API Keys (see the TesseraCloud Administrator Guide, prod-tesseracloud-admin-guide, section 10). Each key carries scopes, such as readings:read or alarms:write, and a request that needs a scope the key does not hold returns 403 with error code TC-ERR-403-SCOPE. Keys expire after at most 365 days. Test keys, prefixed tc_test_, only work against the sandbox at https://sandbox.api.tesseracloud.meridian-instruments.com.

## 3. Endpoints

| Method | Path | Scope | Description |
|---|---|---|---|
| GET | /v2/devices | devices:read | List gateways and nodes in the tenant |
| GET | /v2/devices/{id} | devices:read | Get one device, including firmware version and status |
| PATCH | /v2/devices/{id} | devices:write | Rename a device or change its reporting interval |
| GET | /v2/devices/{id}/readings | readings:read | Readings for a node, filtered by time range and metric |
| GET | /v2/devices/{id}/alarms | alarms:read | Active and historical alarms for a device |
| POST | /v2/alarms/{alarmId}/acknowledge | alarms:write | Acknowledge an alarm |
| GET | /v2/sites | devices:read | List sites |
| GET | /v2/sites/{siteId}/assets | devices:read | List assets and measurement points at a site |
| POST | /v2/exports | exports:write | Create a bulk export job |
| GET | /v2/exports/{exportId} | exports:write | Get export job status and download URL |
| GET | /v2/webhooks | webhooks:write | List webhook subscriptions |
| POST | /v2/webhooks | webhooks:write | Create a webhook subscription |
| DELETE | /v2/webhooks/{webhookId} | webhooks:write | Delete a webhook subscription |

Device IDs are opaque strings beginning with dev_. Serial numbers (for example TS4E-2519-00412) can be used to look up a device with GET /v2/devices?serial=TS4E-2519-00412.

## 4. Readings

GET /v2/devices/{id}/readings accepts the following query parameters:

| Parameter | Required | Description |
|---|---|---|
| from | Yes | Start of range, ISO 8601 |
| to | Yes | End of range, ISO 8601; maximum range 7 days per request |
| metric | No | velocity_rms, accel_peak, accel_rms, crest_factor, kurtosis, temperature, battery; default all |
| axis | No | x, y, z; default all |
| resolution | No | raw, 1m, 1h; default raw |
| limit | No | Page size, default 100, maximum 500 |
| cursor | No | Cursor from the previous page |

Readings older than the tenant's raw retention period (13 months Standard, 36 months Enterprise) are only available with resolution=1h.

Example response body (abbreviated): an object with a "data" array of readings, each containing "ts", "metric", "axis", "value" and "unit", and a "next_cursor" string that is null on the last page.

## 5. Pagination

List endpoints use cursor pagination. Pass the next_cursor value from a response as the cursor parameter in the next request. Cursors expire after 10 minutes. Offset pagination is not supported.

## 6. Exports

POST /v2/exports creates an asynchronous export. The request body specifies site or device IDs, a time range of at most 31 days for raw readings (or 12 months at 1h resolution), metrics, and format (csv or parquet). The response returns an exportId with status "queued". Poll GET /v2/exports/{exportId} no more than once every 30 seconds; when status is "complete" the response includes a signed download URL valid for 7 days.

## 7. Webhooks

Webhook subscriptions deliver events to your HTTPS endpoint. Supported event types include alarm.raised, alarm.acknowledged, alarm.cleared, device.offline, device.online and audit.* (Enterprise only). Each delivery is signed with HMAC-SHA256 in the X-Tessera-Signature header using the subscription secret. Your endpoint must respond with a 2xx status within 10 seconds, otherwise the delivery is retried up to 6 times.

## 8. Rate limits

Rate limits depend on the subscription plan. The current values are 300 requests per minute for Standard tenants and 1,200 requests per minute for Enterprise tenants, with the readings endpoint limited to 30 requests per minute per device. The authoritative figures, headers and back-off rules are in TesseraCloud API Rate Limits (prod-tesseracloud-api-rate-limits-v2).

## 9. Errors

Errors return a JSON body with "code", "message" and "request_id". Quote the request_id when contacting Meridian Support.

| HTTP status | Error code | Meaning |
|---|---|---|
| 400 | TC-ERR-400 | Malformed request or invalid parameter |
| 401 | TC-ERR-401 | Missing, invalid, expired or revoked API key |
| 403 | TC-ERR-403-SCOPE | API key lacks the required scope |
| 403 | TC-ERR-403-SITE | API key's user has no access to the site |
| 404 | TC-ERR-404 | Device, site, alarm or export not found |
| 409 | TC-ERR-409 | Conflict, for example acknowledging an already cleared alarm |
| 422 | TC-ERR-422-RANGE | Time range exceeds 7 days (readings) or 31 days (exports) |
| 429 | TC-ERR-429 | Rate limit exceeded; honour Retry-After |
| 500 | TC-ERR-500 | Internal error; retry with back-off |
| 503 | TC-ERR-503 | Service in maintenance; retry after the Retry-After interval |

## 10. Versioning and deprecation

Breaking changes are introduced only in a new major path version. Non-breaking additions, such as new fields or new metric names, can appear at any time; clients must ignore unknown fields. Deprecations are announced to Tenant Owners at least 6 months in advance and are listed on the TesseraCloud status page.
