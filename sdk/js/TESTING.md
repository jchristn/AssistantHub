# JS SDK Testing

The JS SDK uses a standalone integration test runner in `test_sdk.mjs`.

## Prerequisites

- A running AssistantHub server
- Node.js

## Configuration

Optional environment variables:

- `ASSISTANTHUB_URL` default: `http://localhost:6600`
- `ASSISTANTHUB_API_KEY` default: `default`

Set them in your shell if you are not using the defaults.

## Run

From `sdk/js`:

```bash
npm install
npm run build
node test_sdk.mjs
```
