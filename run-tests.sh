#!/usr/bin/env bash
# Run all AssistantHub test projects sequentially and print a cross-project summary.
# Exit code: 0 only if every project returned 0.

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

declare -a PROJECTS=(
    "Test.Models"
    "Test.Database"
    "Test.Services"
    "Test.Api"
    "Test.Integration"
)

declare -a PROJECT_RESULTS=()
declare -a PROJECT_TIMES=()
OVERALL_EXIT=0
TOTAL_START=$(date +%s%N 2>/dev/null || python3 -c 'import time; print(int(time.time()*1e9))')

echo "=============================================================="
echo "  CROSS-PROJECT TEST SUMMARY"
echo "=============================================================="

for proj in "${PROJECTS[@]}"; do
    proj_path="${SCRIPT_DIR}/src/${proj}"

    if [ ! -d "$proj_path" ]; then
        echo "  SKIP  ${proj}  (directory not found)"
        PROJECT_RESULTS+=("SKIP")
        PROJECT_TIMES+=("0")
        continue
    fi

    PROJ_START=$(date +%s%N 2>/dev/null || python3 -c 'import time; print(int(time.time()*1e9))')

    # Pass through any extra args (e.g. -- --type sqlite for Test.Database)
    if [ "$proj" = "Test.Database" ] && [ $# -gt 0 ]; then
        dotnet run --project "$proj_path" -- "$@"
    else
        dotnet run --project "$proj_path"
    fi
    EXIT_CODE=$?

    PROJ_END=$(date +%s%N 2>/dev/null || python3 -c 'import time; print(int(time.time()*1e9))')
    ELAPSED_MS=$(( (PROJ_END - PROJ_START) / 1000000 ))

    if [ $EXIT_CODE -eq 0 ]; then
        PROJECT_RESULTS+=("PASS")
    else
        PROJECT_RESULTS+=("FAIL")
        OVERALL_EXIT=1
    fi
    PROJECT_TIMES+=("$ELAPSED_MS")

    echo ""
done

TOTAL_END=$(date +%s%N 2>/dev/null || python3 -c 'import time; print(int(time.time()*1e9))')
TOTAL_MS=$(( (TOTAL_END - TOTAL_START) / 1000000 ))

echo "=============================================================="
echo "  CROSS-PROJECT TEST SUMMARY"
echo "=============================================================="

for i in "${!PROJECTS[@]}"; do
    RESULT="${PROJECT_RESULTS[$i]}"
    proj="${PROJECTS[$i]}"
    elapsed="${PROJECT_TIMES[$i]}"

    if [ "$RESULT" = "PASS" ]; then
        printf "  \033[32mPASS\033[0m  %-20s (%sms)\n" "$proj" "$elapsed"
    elif [ "$RESULT" = "FAIL" ]; then
        printf "  \033[31mFAIL\033[0m  %-20s (%sms)\n" "$proj" "$elapsed"
    else
        printf "  \033[33mSKIP\033[0m  %-20s\n" "$proj"
    fi
done

echo "--------------------------------------------------------------"
echo "  Total runtime: ${TOTAL_MS}ms"

FAIL_COUNT=0
for r in "${PROJECT_RESULTS[@]}"; do
    [ "$r" = "FAIL" ] && ((FAIL_COUNT++))
done

if [ $OVERALL_EXIT -eq 0 ]; then
    printf "  \033[32mOVERALL: PASS\033[0m\n"
else
    printf "  \033[31mOVERALL: FAIL (%d project(s) failed)\033[0m\n" "$FAIL_COUNT"
fi
echo "=============================================================="

exit $OVERALL_EXIT
