| Library | N | Mean time | Allocated |
|---------|--:|----------:|----------:|
| Moq | 1 | 227.36 μs | 12.82 KB |
| NSubstitute | 1 | 3.77 μs | 7.48 KB |
| FakeItEasy | 1 | 3.60 μs | 5.49 KB |
| Rocks | 1 | 73.62 ns | 304 B |
| **Imposter** | **1** | **144.54 ns** | **624 B** |
| Moq | 10 | 2.23 ms | 114.85 KB |
| NSubstitute | 10 | 17.02 μs | 26.20 KB |
| FakeItEasy | 10 | 17.00 μs | 34.87 KB |
| Rocks | 10 | 876.88 ns | 1.84 KB |
| **Imposter** | **10** | **1.36 μs** | **4.76 KB** |
| Moq | 100 | 23.20 ms | 1.38 MB |
| NSubstitute | 100 | 370.11 μs | 217.46 KB |
| FakeItEasy | 100 | 360.92 μs | 715.14 KB |
| Rocks | 100 | 46.05 μs | 17.31 KB |
| **Imposter** | **100** | **30.28 μs** | **46.24 KB** |
| Moq | 1,000 | 284.39 ms | 41.22 MB |
| NSubstitute | 1,000 | 30.36 ms | 2.07 MB |
| FakeItEasy | 1,000 | 27.59 ms | 44.72 MB |
| Rocks | 1,000 | 3.97 ms | 172.00 KB |
| **Imposter** | **1,000** | **2.88 ms** | **461.09 KB** |

AMD EPYC 7763, 4 logical cores, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon), .NET 10.0.14, BenchmarkDotNet 0.15.6, commit 715a1a5b, 2026-11-03 ([workflow run](https://github.com/gabisonia/Imposter/actions/runs/41711800509)).
