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

# --- Test.XUnit (xUnit runner) ---
echo "Running Test.XUnit..."
PROJ_START=$(date +%s%N 2>/dev/null || python3 -c 'import time; print(int(time.time()*1e9))')

dotnet test "${SCRIPT_DIR}/src/Test.XUnit" --no-build --verbosity normal
XUNIT_EXIT=$?

PROJ_END=$(date +%s%N 2>/dev/null || python3 -c 'import time; print(int(time.time()*1e9))')
XUNIT_MS=$(( (PROJ_END - PROJ_START) / 1000000 ))

if [ $XUNIT_EXIT -ne 0 ]; then
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
    printf "  \033[32mPASS\033[0m  %-20s (%sms)\n" "Test.XUnit" "$XUNIT_MS"
else
    printf "  \033[31mFAIL\033[0m  %-20s (%sms)\n" "Test.XUnit" "$XUNIT_MS"
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
