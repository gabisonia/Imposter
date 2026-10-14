| Library | N | Mean time | Allocated |
|---------|--:|----------:|----------:|
| Moq | 1 | 215.24 μs | 12.82 KB |
| NSubstitute | 1 | 3.44 μs | 7.48 KB |
| FakeItEasy | 1 | 3.15 μs | 5.49 KB |
| Rocks | 1 | 79.77 ns | 304 B |
| **Imposter** | **1** | **325.95 ns** | **2.27 KB** |
| Moq | 10 | 2.06 ms | 114.85 KB |
| NSubstitute | 10 | 16.53 μs | 26.21 KB |
| FakeItEasy | 10 | 16.01 μs | 34.87 KB |
| Rocks | 10 | 923.71 ns | 1.84 KB |
| **Imposter** | **10** | **3.07 μs** | **21.11 KB** |
| Moq | 100 | 20.76 ms | 1.38 MB |
| NSubstitute | 100 | 412.21 μs | 217.46 KB |
| FakeItEasy | 100 | 327.45 μs | 715.14 KB |
| Rocks | 100 | 49.32 μs | 17.31 KB |
| **Imposter** | **100** | **54.88 μs** | **209.55 KB** |
| Moq | 1,000 | 259.71 ms | 41.22 MB |
| NSubstitute | 1,000 | 27.02 ms | 2.07 MB |
| FakeItEasy | 1,000 | 26.15 ms | 44.72 MB |
| Rocks | 1,000 | 4.29 ms | 172.00 KB |
| **Imposter** | **1,000** | **3.86 ms** | **2.04 MB** |

AMD EPYC 9V74, 4 logical cores, Linux Ubuntu 24.04.5 LTS (Noble Numbat), .NET 10.0.13, BenchmarkDotNet 0.15.6, commit bd3b7a9e, 2026-10-14 ([workflow run](https://github.com/gabisonia/Imposter/actions/runs/38524111822)).
