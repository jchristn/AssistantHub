#!/usr/bin/env bash
# Standard benchmark suite: retrieval on every dataset present (all three search modes, reachability where evidence
# labels exist), chat on the committed datasets, and the stub load test. Assumes the stack and server are running
# (see benchmarks/README.md). Extra arguments are passed to every command, e.g. --label round1.
set -uo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
dotnet build src/Test.Benchmark -c Release >/dev/null || exit 1
B="dotnet run --project src/Test.Benchmark -c Release --no-build --"

for d in benchmarks/datasets/assistanthub-docs.json benchmarks/datasets/meridian.json; do
  $B retrieval --dataset "$d" --reachability "$@"
done
for d in qasper-50 multihop-rag-300; do
  [ -f "benchmarks/data/$d.json" ] && $B retrieval --dataset "benchmarks/data/$d.json" --reachability "$@"
done
for d in scifact nfcorpus; do
  [ -f "benchmarks/data/$d.json" ] && $B retrieval --dataset "benchmarks/data/$d.json" "$@"
done

$B chat --dataset benchmarks/datasets/assistanthub-docs.json --mode Hybrid --threshold 0 "$@"
$B chat --dataset benchmarks/datasets/meridian.json --mode Hybrid --threshold 0 --limit 150 "$@"
$B load --dataset benchmarks/datasets/assistanthub-docs.json --stub --scenario retrieve --concurrency 1,4,16,32 --duration 30 "$@"
