# C# SDK Testing

The C# SDK uses a standalone integration test runner in `Test.Sdk/`.

## Prerequisites

- A running AssistantHub server
- `.NET 10 SDK`

## Configuration

Optional environment variables:

- `ASSISTANTHUB_BASE_URL` default: `http://localhost:6600`
- `ASSISTANTHUB_API_KEY` default: `default`
- `ASSISTANTHUB_TENANT_ID` default: `default`

Set them in your shell if you are not using the defaults.

You can also pass `baseurl=...`, `apikey=...`, and `tenantid=...` on the command line.

## Run

From `sdk/csharp`:

```bash
dotnet run --project Test.Sdk/Test.Sdk.csproj
```
