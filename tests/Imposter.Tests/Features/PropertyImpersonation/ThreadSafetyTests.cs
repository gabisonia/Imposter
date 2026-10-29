using System;
using Imposter.Abstractions;
using Imposter.Tests.Shared;
using Shouldly;
using Xunit;

namespace Imposter.Tests.Features.PropertyImpersonation
{
    public class ThreadSafetyTests
    {
        private const int ThreadCount = 200;
        private readonly IPropertySetupSutImposter _sut =
#if USE_CSHARP14
        IPropertySetupSut.Imposter();
#else
        new IPropertySetupSutImposter();
#endif

        [Fact]
        public void GivenConcurrentOperations_WhenMixingGettersAndSetters_ShouldNotThrowExceptions()
        {
            DedicatedThreads.RunTogether(
                ThreadCount,
                index =>
                {
                    if (index % 2 == 0)
                    {
                        _sut.Instance().Age = index;
                    }
                    else
                    {
                        _ = _sut.Instance().Age;
                    }
                }
            );

            // Should not throw - just testing for thread safety
            Should.NotThrow(() => _sut.Age.Getter().Called(Count.Exactly(ThreadCount / 2)));
            Should.NotThrow(() =>
                _sut.Age.Setter(Arg<int>.Any()).Called(Count.Exactly(ThreadCount / 2))
            );
        }

        [Fact]
        public void GivenSequentialReturnsSetup_WhenAccessedConcurrently_ShouldWorkCorrectly()
        {
            // Setup many sequential returns
            for (int i = 0; i < ThreadCount; i++)
            {
                _sut.Age.Getter().Returns(i);
            }

            var results = new int[ThreadCount];
            DedicatedThreads.RunTogether(
                ThreadCount,
                index => results[index] = _sut.Instance().Age
            );

            results.ShouldBeUnique();
            results.ShouldAllBe(x => x >= 0 && x < ThreadCount);
        }

        [Fact]
        public void GivenGetterReturnsSetup_WhenFirstReadsAreConcurrent_ShouldReturnConfiguredValueEveryTime()
        {
            var outcomes = ConcurrentFirstCalls.CountOutcomes(
                () => ConfiguredInstance(it => it.Age.Getter().Returns(42)),
                it => it.Age
            );

            outcomes.ShouldBe(ConcurrentFirstCalls.EveryCall("42"));
        }

        [Fact]
        public void GivenGetterReturnsSequence_WhenFirstReadsAreConcurrent_ShouldRepeatLastValueAfterSequence()
        {
            var outcomes = ConcurrentFirstCalls.CountOutcomes(
                () => ConfiguredInstance(it => it.Age.Getter().Returns(1).Then().Returns(2)),
                it => it.Age
            );

            outcomes.ShouldBe(
                ConcurrentFirstCalls.FirstCallThenEveryOtherCall("1", "2"),
                ignoreOrder: true
            );
        }

        private static IPropertySetupSut ConfiguredInstance(Action<IPropertySetupSutImposter> setup)
        {
            var imposter = new IPropertySetupSutImposter();
            setup(imposter);
            return imposter.Instance();
        }
    }
}
