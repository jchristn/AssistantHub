#!/usr/bin/env bash
# Run AssistantHub test suites and print a summary.
# Exit code: 0 only if every project returned 0.

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

OVERALL_EXIT=0
TOTAL_START=$(date +%s%N 2>/dev/null || python3 -c 'import time; print(int(time.time()*1e9))')

echo "=============================================================="
echo "  AssistantHub Test Runner"
echo "=============================================================="
if [ -n "${ASSISTANTHUB_TEST_SUITES:-}" ]; then
    echo "  ASSISTANTHUB_TEST_SUITES=${ASSISTANTHUB_TEST_SUITES}"
    echo "  Note: suite filtering applies to Touchstone-backed .NET test projects."
fi
if [ -n "${ASSISTANTHUB_TEST_KEEP_ARTIFACTS:-}" ]; then
    echo "  ASSISTANTHUB_TEST_KEEP_ARTIFACTS=${ASSISTANTHUB_TEST_KEEP_ARTIFACTS}"
fi
echo ""

# --- Test.Automated (console runner) ---
echo "Running Test.Automated..."
PROJ_START=$(date +%s%N 2>/dev/null || python3 -c 'import time; print(int(time.time()*1e9))')

dotnet run --project "${SCRIPT_DIR}/src/Test.Automated"
AUTOMATED_EXIT=$?

PROJ_END=$(date +%s%N 2>/dev/null || python3 -c 'import time; print(int(time.time()*1e9))')
AUTOMATED_MS=$(( (PROJ_END - PROJ_START) / 1000000 ))

if [ $AUTOMATED_EXIT -ne 0 ]; then
    OVERALL_EXIT=1
fi

echo ""

# --- Test.Xunit (xUnit runner) ---
echo "Running Test.Xunit..."
PROJ_START=$(date +%s%N 2>/dev/null || python3 -c 'import time; print(int(time.time()*1e9))')

dotnet test "${SCRIPT_DIR}/src/Test.Xunit" --no-build --verbosity normal
XUNIT_EXIT=$?

PROJ_END=$(date +%s%N 2>/dev/null || python3 -c 'import time; print(int(time.time()*1e9))')
XUNIT_MS=$(( (PROJ_END - PROJ_START) / 1000000 ))

if [ $XUNIT_EXIT -ne 0 ]; then
    OVERALL_EXIT=1
fi

# --- Test.Nunit (NUnit runner) ---
echo "Running Test.Nunit..."
PROJ_START=$(date +%s%N 2>/dev/null || python3 -c 'import time; print(int(time.time()*1e9))')

dotnet test "${SCRIPT_DIR}/src/Test.Nunit" --no-build --verbosity normal
NUNIT_EXIT=$?

PROJ_END=$(date +%s%N 2>/dev/null || python3 -c 'import time; print(int(time.time()*1e9))')
NUNIT_MS=$(( (PROJ_END - PROJ_START) / 1000000 ))

if [ $NUNIT_EXIT -ne 0 ]; then
    OVERALL_EXIT=1
fi

TOTAL_END=$(date +%s%N 2>/dev/null || python3 -c 'import time; print(int(time.time()*1e9))')
TOTAL_MS=$(( (TOTAL_END - TOTAL_START) / 1000000 ))

echo ""
echo "=============================================================="
echo "  CROSS-PROJECT TEST SUMMARY"
echo "=============================================================="

if [ $AUTOMATED_EXIT -eq 0 ]; then
    printf "  \033[32mPASS\033[0m  %-20s (%sms)\n" "Test.Automated" "$AUTOMATED_MS"
else
    printf "  \033[31mFAIL\033[0m  %-20s (%sms)\n" "Test.Automated" "$AUTOMATED_MS"
fi

if [ $XUNIT_EXIT -eq 0 ]; then
    printf "  \033[32mPASS\033[0m  %-20s (%sms)\n" "Test.Xunit" "$XUNIT_MS"
else
    printf "  \033[31mFAIL\033[0m  %-20s (%sms)\n" "Test.Xunit" "$XUNIT_MS"
fi

if [ $NUNIT_EXIT -eq 0 ]; then
    printf "  \033[32mPASS\033[0m  %-20s (%sms)\n" "Test.Nunit" "$NUNIT_MS"
else
    printf "  \033[31mFAIL\033[0m  %-20s (%sms)\n" "Test.Nunit" "$NUNIT_MS"
fi

echo "--------------------------------------------------------------"
echo "  Total runtime: ${TOTAL_MS}ms"

if [ $OVERALL_EXIT -eq 0 ]; then
    printf "  \033[32mOVERALL: PASS\033[0m\n"
else
    printf "  \033[31mOVERALL: FAIL\033[0m\n"
fi
echo "=============================================================="

exit $OVERALL_EXIT
