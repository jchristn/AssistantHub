# Python SDK Testing

The Python SDK uses a standalone integration test runner in `test_sdk.py`.

## Prerequisites

- A running AssistantHub server
- Python 3.10+

## Configuration

Optional environment variables:

- `ASSISTANTHUB_URL` default: `http://localhost:6600`
- `ASSISTANTHUB_API_KEY` default: `default`

Set them in your shell if you are not using the defaults.

## Run

From `sdk/python`:

```bash
pip install -e .
python test_sdk.py
```
