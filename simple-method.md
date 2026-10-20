| Library | N | Mean time | Allocated |
|---------|--:|----------:|----------:|
| Moq | 1 | 172.31 μs | 12.82 KB |
| NSubstitute | 1 | 2.97 μs | 7.48 KB |
| FakeItEasy | 1 | 2.85 μs | 5.49 KB |
| Rocks | 1 | 65.54 ns | 304 B |
| **Imposter** | **1** | **276.30 ns** | **2.28 KB** |
| Moq | 10 | 1.63 ms | 114.85 KB |
| NSubstitute | 10 | 14.09 μs | 26.20 KB |
| FakeItEasy | 10 | 13.45 μs | 34.87 KB |
| Rocks | 10 | 791.39 ns | 1.84 KB |
| **Imposter** | **10** | **2.56 μs** | **21.27 KB** |
| Moq | 100 | 16.68 ms | 1.38 MB |
| NSubstitute | 100 | 387.09 μs | 228.34 KB |
| FakeItEasy | 100 | 278.28 μs | 715.14 KB |
| Rocks | 100 | 37.30 μs | 17.31 KB |
| **Imposter** | **100** | **41.39 μs** | **211.11 KB** |
| Moq | 1,000 | 209.60 ms | 41.22 MB |
| NSubstitute | 1,000 | 22.98 ms | 2.07 MB |
| FakeItEasy | 1,000 | 22.05 ms | 44.72 MB |
| Rocks | 1,000 | 3.35 ms | 172.00 KB |
| **Imposter** | **1,000** | **2.94 ms** | **2.06 MB** |

AMD EPYC 9V74, 4 logical cores, Linux Ubuntu 24.04.5 LTS (Noble Numbat), .NET 10.0.13, BenchmarkDotNet 0.15.6, commit abc037f5, 2026-10-20 ([workflow run](https://github.com/gabisonia/Imposter/actions/runs/39460130331)).
