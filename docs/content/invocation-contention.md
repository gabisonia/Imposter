# Invocation sequence contention

The process-wide sequence supports ordering across imposter instances. It adds a shared atomic increment, which can contend when tests invoke many independent imposters concurrently.

Measured on 2026-11-11 with BenchmarkDotNet 0.15.6 ShortRun, .NET 10.0.12, Windows 11 x64, and an AMD Ryzen AI 9 HX PRO 470 (12 physical cores). Each batch makes 32,768 calls. Three measurement iterations provide an initial contention check; these results are not a general throughput guarantee.

| Workers | Shared history | Local sequence (ns/call) | Global sequence (ns/call) | Generated calls (ns/call) |
| --- | --- | ---: | ---: | ---: |
| 1 | No | 18.85 | 16.19 | 22.54 |
| 1 | Yes | 15.91 | 17.26 | 22.12 |
| 4 | No | 28.38 | 31.46 | 36.87 |
| 4 | Yes | 51.64 | 60.82 | 124.58 |
| 8 | No | 26.07 | 42.86 | 51.39 |
| 8 | Yes | 64.31 | 86.24 | 137.44 |

The local/global recorder pair uses identical records, scheduling, and publication, differing only in sequence-counter ownership. Both allocate 40 bytes per call. The real generated method includes lookup, outcome execution, result/exception storage, and verification support, and allocates 56 bytes per call. Its ratio to the synthetic recorder is not a before/after implementation comparison.

The eight-worker independent-history case shows observable counter contention (approximately 65% higher mean for the synthetic global recorder). Global ordering therefore has a throughput cost under contention; it is not described as free. ShortRun confidence intervals are wide, especially for real generated calls. Use a longer run on the target hardware when this workload matters.

Reproduce with `IMPOSTER_BENCHMARK_JOB=short` and:

```text
dotnet run -c Release --project benchmarks/Imposter.Benchmarks/Imposter.Benchmarks.csproj -p:ROSLYN_VERSION=5.0 -- --filter *InvocationContentionBenchmarks*
```

The benchmark is in `benchmarks/Imposter.Benchmarks/InvocationContentionBenchmarks.cs`. See [invocation history](invocation-history.md) for allocation ceilings and snapshot semantics.
