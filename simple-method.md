| Library | N | Mean time | Allocated |
|---------|--:|----------:|----------:|
| Moq | 1 | 233.70 μs | 12.82 KB |
| NSubstitute | 1 | 3.75 μs | 7.48 KB |
| FakeItEasy | 1 | 3.88 μs | 5.49 KB |
| Rocks | 1 | 76.72 ns | 304 B |
| **Imposter** | **1** | **175.84 ns** | **696 B** |
| Moq | 10 | 2.25 ms | 114.85 KB |
| NSubstitute | 10 | 19.42 μs | 27.24 KB |
| FakeItEasy | 10 | 18.30 μs | 34.87 KB |
| Rocks | 10 | 905.43 ns | 1.84 KB |
| **Imposter** | **10** | **1.51 μs** | **5.25 KB** |
| Moq | 100 | 23.00 ms | 1.38 MB |
| NSubstitute | 100 | 375.08 μs | 217.46 KB |
| FakeItEasy | 100 | 364.90 μs | 715.14 KB |
| Rocks | 100 | 44.68 μs | 17.31 KB |
| **Imposter** | **100** | **31.21 μs** | **50.95 KB** |
| Moq | 1,000 | 281.34 ms | 41.22 MB |
| NSubstitute | 1,000 | 29.97 ms | 2.07 MB |
| FakeItEasy | 1,000 | 27.75 ms | 44.72 MB |
| Rocks | 1,000 | 3.99 ms | 172.00 KB |
| **Imposter** | **1,000** | **3.00 ms** | **507.98 KB** |

AMD EPYC 7763, 4 logical cores, Linux Ubuntu 24.04.5 LTS (Noble Numbat), .NET 10.0.14, BenchmarkDotNet 0.15.6, commit 9f2ff3af, 2026-10-29 ([workflow run](https://github.com/gabisonia/Imposter/actions/runs/40791863337)).
