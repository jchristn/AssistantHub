@echo off
REM Standard benchmark suite: retrieval on every dataset present (all three search modes, reachability where evidence
REM labels exist), chat on the committed datasets, and the stub load test. Assumes the stack and server are running
REM (see benchmarks\README.md). Extra arguments are passed to every command, e.g. --label round1.
setlocal
set "ROOT=%~dp0.."
pushd "%ROOT%"
dotnet build src\Test.Benchmark -c Release >nul || exit /b 1
set "B=dotnet run --project src\Test.Benchmark -c Release --no-build --"

%B% retrieval --dataset benchmarks\datasets\assistanthub-docs.json --reachability %*
%B% retrieval --dataset benchmarks\datasets\meridian.json --reachability %*
if exist benchmarks\data\qasper-50.json %B% retrieval --dataset benchmarks\data\qasper-50.json --reachability %*
if exist benchmarks\data\multihop-rag-300.json %B% retrieval --dataset benchmarks\data\multihop-rag-300.json --reachability %*
if exist benchmarks\data\scifact.json %B% retrieval --dataset benchmarks\data\scifact.json %*
if exist benchmarks\data\nfcorpus.json %B% retrieval --dataset benchmarks\data\nfcorpus.json %*

%B% chat --dataset benchmarks\datasets\assistanthub-docs.json --mode Hybrid --threshold 0 %*
%B% chat --dataset benchmarks\datasets\meridian.json --mode Hybrid --threshold 0 --limit 150 %*
%B% load --dataset benchmarks\datasets\assistanthub-docs.json --stub --scenario retrieve --concurrency 1,4,16,32 --duration 30 %*
popd
endlocal
