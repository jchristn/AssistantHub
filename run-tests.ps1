# Run AssistantHub test suites and print a summary.
# Exit code: 0 only if every project returned 0.

$ErrorActionPreference = "Continue"
$ScriptDir = $PSScriptRoot

$OverallExit = 0
$TotalStopwatch = [System.Diagnostics.Stopwatch]::StartNew()

Write-Host "=============================================================="
Write-Host "  AssistantHub Test Runner"
Write-Host "=============================================================="
if ($env:ASSISTANTHUB_TEST_SUITES) {
    Write-Host "  ASSISTANTHUB_TEST_SUITES=$($env:ASSISTANTHUB_TEST_SUITES)"
    Write-Host "  Note: suite filtering applies to Touchstone-backed .NET test projects."
}
if ($env:ASSISTANTHUB_TEST_KEEP_ARTIFACTS) {
    Write-Host "  ASSISTANTHUB_TEST_KEEP_ARTIFACTS=$($env:ASSISTANTHUB_TEST_KEEP_ARTIFACTS)"
}
Write-Host ""

# --- Test.Automated (console runner) ---
Write-Host "Running Test.Automated..."
$sw1 = [System.Diagnostics.Stopwatch]::StartNew()

& dotnet run --project (Join-Path (Join-Path $ScriptDir "src") "Test.Automated")
$AutomatedExit = $LASTEXITCODE
$sw1.Stop()
$AutomatedMs = [math]::Round($sw1.Elapsed.TotalMilliseconds)

if ($AutomatedExit -ne 0) { $OverallExit = 1 }

Write-Host ""

# --- Test.Xunit (xUnit runner) ---
Write-Host "Running Test.Xunit..."
$sw2 = [System.Diagnostics.Stopwatch]::StartNew()

& dotnet test (Join-Path (Join-Path $ScriptDir "src") "Test.Xunit") --no-build --verbosity normal
$XUnitExit = $LASTEXITCODE
$sw2.Stop()
$XUnitMs = [math]::Round($sw2.Elapsed.TotalMilliseconds)

if ($XUnitExit -ne 0) { $OverallExit = 1 }

# --- Test.Nunit (NUnit runner) ---
Write-Host "Running Test.Nunit..."
$sw3 = [System.Diagnostics.Stopwatch]::StartNew()

& dotnet test (Join-Path (Join-Path $ScriptDir "src") "Test.Nunit") --no-build --verbosity normal
$NunitExit = $LASTEXITCODE
$sw3.Stop()
$NunitMs = [math]::Round($sw3.Elapsed.TotalMilliseconds)

if ($NunitExit -ne 0) { $OverallExit = 1 }

$TotalStopwatch.Stop()
$TotalMs = [math]::Round($TotalStopwatch.Elapsed.TotalMilliseconds)

Write-Host ""
Write-Host "=============================================================="
Write-Host "  CROSS-PROJECT TEST SUMMARY"
Write-Host "=============================================================="

$automatedLabel = "Test.Automated".PadRight(20)
if ($AutomatedExit -eq 0) {
    Write-Host "  " -NoNewline; Write-Host "PASS" -ForegroundColor Green -NoNewline; Write-Host "  $automatedLabel (${AutomatedMs}ms)"
} else {
    Write-Host "  " -NoNewline; Write-Host "FAIL" -ForegroundColor Red -NoNewline; Write-Host "  $automatedLabel (${AutomatedMs}ms)"
}

$xunitLabel = "Test.Xunit".PadRight(20)
if ($XUnitExit -eq 0) {
    Write-Host "  " -NoNewline; Write-Host "PASS" -ForegroundColor Green -NoNewline; Write-Host "  $xunitLabel (${XUnitMs}ms)"
} else {
    Write-Host "  " -NoNewline; Write-Host "FAIL" -ForegroundColor Red -NoNewline; Write-Host "  $xunitLabel (${XUnitMs}ms)"
}

$nunitLabel = "Test.Nunit".PadRight(20)
if ($NunitExit -eq 0) {
    Write-Host "  " -NoNewline; Write-Host "PASS" -ForegroundColor Green -NoNewline; Write-Host "  $nunitLabel (${NunitMs}ms)"
} else {
    Write-Host "  " -NoNewline; Write-Host "FAIL" -ForegroundColor Red -NoNewline; Write-Host "  $nunitLabel (${NunitMs}ms)"
}

Write-Host "--------------------------------------------------------------"
Write-Host "  Total runtime: ${TotalMs}ms"

if ($OverallExit -eq 0) {
    Write-Host "  " -NoNewline; Write-Host "OVERALL: PASS" -ForegroundColor Green
} else {
    $failCount = @($AutomatedExit, $XUnitExit, $NunitExit) | Where-Object { $_ -ne 0 } | Measure-Object | Select-Object -ExpandProperty Count
    Write-Host "  " -NoNewline; Write-Host "OVERALL: FAIL ($failCount project(s) failed)" -ForegroundColor Red
}
Write-Host "=============================================================="

exit $OverallExit
