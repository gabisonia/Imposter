| Library | N | Mean time | Allocated |
|---------|--:|----------:|----------:|
| Moq | 1 | 54.91 μs | 12.84 KB |
| NSubstitute | 1 | 2.22 μs | 7.48 KB |
| FakeItEasy | 1 | 2.01 μs | 5.49 KB |
| Rocks | 1 | 48.13 ns | 304 B |
| **Imposter** | **1** | **106.48 ns** | **656 B** |
| Moq | 10 | 532.69 μs | 115.04 KB |
| NSubstitute | 10 | 9.50 μs | 26.20 KB |
| FakeItEasy | 10 | 9.97 μs | 34.87 KB |
| Rocks | 10 | 570.36 ns | 1.84 KB |
| **Imposter** | **10** | **940.50 ns** | **4.93 KB** |
| Moq | 100 | 5.26 ms | 1.38 MB |
| NSubstitute | 100 | 208.76 μs | 217.46 KB |
| FakeItEasy | 100 | 198.50 μs | 715.14 KB |
| Rocks | 100 | 26.83 μs | 17.31 KB |
| **Imposter** | **100** | **17.60 μs** | **47.82 KB** |
| Moq | 1,000 | 82.97 ms | 41.23 MB |
| NSubstitute | 1,000 | 13.85 ms | 2.07 MB |
| FakeItEasy | 1,000 | 14.61 ms | 44.72 MB |
| Rocks | 1,000 | 2.19 ms | 172.00 KB |
| **Imposter** | **1,000** | **1.94 ms** | **476.73 KB** |

AMD EPYC 9V45, 4 logical cores, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon), .NET 10.0.15, BenchmarkDotNet 0.15.6, commit b6afbd3f, 2026-11-13 ([workflow run](https://github.com/gabisonia/Imposter/actions/runs/43441591276)).
