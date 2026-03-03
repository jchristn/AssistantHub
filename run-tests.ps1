# Run all AssistantHub test projects sequentially and print a cross-project summary.
# Exit code: 0 only if every project returned 0.

param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ExtraArgs
)

$ErrorActionPreference = "Continue"
$ScriptDir = $PSScriptRoot

$Projects = @(
    "Test.Models",
    "Test.Database",
    "Test.Services",
    "Test.Api",
    "Test.Integration"
)

$ProjectResults = @()
$ProjectTimes = @()
$OverallExit = 0
$TotalStopwatch = [System.Diagnostics.Stopwatch]::StartNew()

Write-Host "=============================================================="
Write-Host "  CROSS-PROJECT TEST SUMMARY"
Write-Host "=============================================================="

foreach ($proj in $Projects) {
    $projPath = Join-Path $ScriptDir "src" $proj

    if (-not (Test-Path $projPath)) {
        Write-Host "  SKIP  $proj  (directory not found)"
        $ProjectResults += "SKIP"
        $ProjectTimes += 0
        continue
    }

    $sw = [System.Diagnostics.Stopwatch]::StartNew()

    if ($proj -eq "Test.Database" -and $ExtraArgs.Count -gt 0) {
        & dotnet run --project $projPath -- @ExtraArgs
    } else {
        & dotnet run --project $projPath
    }
    $exitCode = $LASTEXITCODE
    $sw.Stop()

    if ($exitCode -eq 0) {
        $ProjectResults += "PASS"
    } else {
        $ProjectResults += "FAIL"
        $OverallExit = 1
    }
    $ProjectTimes += [math]::Round($sw.Elapsed.TotalMilliseconds)

    Write-Host ""
}

$TotalStopwatch.Stop()
$TotalMs = [math]::Round($TotalStopwatch.Elapsed.TotalMilliseconds)

Write-Host "=============================================================="
Write-Host "  CROSS-PROJECT TEST SUMMARY"
Write-Host "=============================================================="

for ($i = 0; $i -lt $Projects.Count; $i++) {
    $result = $ProjectResults[$i]
    $projName = $Projects[$i].PadRight(20)
    $elapsed = $ProjectTimes[$i]

    switch ($result) {
        "PASS" { Write-Host "  " -NoNewline; Write-Host "PASS" -ForegroundColor Green -NoNewline; Write-Host "  $projName (${elapsed}ms)" }
        "FAIL" { Write-Host "  " -NoNewline; Write-Host "FAIL" -ForegroundColor Red -NoNewline; Write-Host "  $projName (${elapsed}ms)" }
        "SKIP" { Write-Host "  " -NoNewline; Write-Host "SKIP" -ForegroundColor Yellow -NoNewline; Write-Host "  $projName" }
    }
}

Write-Host "--------------------------------------------------------------"
Write-Host "  Total runtime: ${TotalMs}ms"

$failCount = ($ProjectResults | Where-Object { $_ -eq "FAIL" }).Count

if ($OverallExit -eq 0) {
    Write-Host "  " -NoNewline; Write-Host "OVERALL: PASS" -ForegroundColor Green
} else {
    Write-Host "  " -NoNewline; Write-Host "OVERALL: FAIL ($failCount project(s) failed)" -ForegroundColor Red
}
Write-Host "=============================================================="

exit $OverallExit
