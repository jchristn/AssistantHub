# Running Tests

Five test projects, all console applications. Exit code `0` = all passed, `1` = any failed.

## Test.Models — unit tests, no dependencies

```bash
cd src/Test.Models
dotnet run
```

## Test.Database — SQLite by default

```bash
cd src/Test.Database
dotnet run -- --type sqlite
```

Creates a temporary `test_database_<guid>.db` file, deleted automatically on exit.
Use `--no-cleanup` to preserve test data for inspection.

Other databases (require running instances):

```bash
dotnet run -- --type postgres --host 127.0.0.1 --user postgres --pass <pw> --name testdb
dotnet run -- --type mysql --host 127.0.0.1 --user root --pass <pw> --name testdb
dotnet run -- --type sqlserver --host 127.0.0.1 --user sa --pass <pw> --name testdb
```

## Test.Services — unit tests, mocked dependencies

```bash
cd src/Test.Services
dotnet run
```

## Test.Api — unit tests, mocked dependencies

```bash
cd src/Test.Api
dotnet run
```

## Test.Integration — in-process HTTP server with SQLite

```bash
cd src/Test.Integration
dotnet run
```

Starts a Watson Webserver on a random port with a temporary SQLite database. Both are cleaned up on exit.

## Run all tests

```bash
./run-tests.sh    # Linux/macOS/Git Bash
run-tests.bat     # Windows cmd
./run-tests.ps1   # PowerShell
```
