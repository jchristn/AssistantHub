#!/usr/bin/env bash
# Retrieval regression gate. Runs the retrieval benchmark on the committed datasets with the configuration each
# baseline in benchmarks/baselines/ was recorded with, then compares candidate against baseline: a metric regresses
# when it drops by more than --tolerance AND a paired bootstrap test gives p < --alpha. Exits non-zero on any
# regression, so it can gate a pull request. Needs the benchmark stack and server running (benchmarks/README.md).
#
# Refresh a baseline after an intended change:
#   benchmarks/ci-gate.sh --update
set -uo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
UPDATE=0
[ "${1:-}" = "--update" ] && UPDATE=1
dotnet build src/Test.Benchmark -c Release >/dev/null || exit 1
B="dotnet run --project src/Test.Benchmark -c Release --no-build --"
OUT="$ROOT/benchmarks/.run/ci"
mkdir -p "$OUT"
STATUS=0

# dataset | retrieval options (retrieval-only, no LLM stages, so results are deterministic for fixed models)
GATES=(
  "benchmarks/datasets/assistanthub-docs.json|--modes Vector,Hybrid"
  "benchmarks/datasets/meridian.json|--modes Vector,Hybrid"
)

for gate in "${GATES[@]}"; do
  DATASET="${gate%%|*}"
  OPTIONS="${gate#*|}"
  NAME="$(basename "$DATASET" .json)"
  rm -f "$OUT"/*-retrieval-"$NAME"-ci.json
  $B retrieval --dataset "$DATASET" $OPTIONS --label ci --output-dir "$OUT" --metrics-url none --no-history >/dev/null || { STATUS=1; continue; }
  CANDIDATE="$(ls -t "$OUT"/*-retrieval-"$NAME"-ci.json | head -1)"
  BASELINE="benchmarks/baselines/retrieval-$NAME.json"
  if [ "$UPDATE" = "1" ] || [ ! -f "$BASELINE" ]; then
    cp "$CANDIDATE" "$BASELINE"
    echo "baseline written: $BASELINE"
    continue
  fi
  $B compare --baseline "$BASELINE" --candidate "$CANDIDATE" --tolerance 0.01 --alpha 0.05 || STATUS=1
done
exit $STATUS
