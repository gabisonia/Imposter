using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Imposter.Abstractions;
using Imposter.Tests.Features.MethodImpersonation;
using Shouldly;
using Xunit;

namespace Imposter.Tests.Features.CallAllocations
{
    public class PendingOutcomeTests
    {
        [Theory]
        [InlineData(ImposterMode.Implicit, false)]
        [InlineData(ImposterMode.Implicit, true)]
        [InlineData(ImposterMode.Explicit, false)]
        [InlineData(ImposterMode.Explicit, true)]
        public void Given_FirstPendingOutcome_When_CalledBeforeConfiguration_Should_RemainConfigurable(
            ImposterMode mode,
            bool generic
        )
        {
            var imposter = new ICallAllocationSutImposter(mode);
            Func<int> call;
            Action configure;
            if (generic)
            {
                var pending = imposter.Echo<int>(Arg<int>.Any());
                call = () => imposter.Instance().Echo(5);
                configure = () => pending.Returns(40);
            }
            else
            {
                var pending = imposter.Square(Arg<int>.Any());
                call = () => imposter.Instance().Square(5);
                configure = () => pending.Returns(40);
            }

            for (var i = 0; i < 3; i++)
            {
                if (mode == ImposterMode.Explicit)
                    Should.Throw<MissingImposterException>(() => call());
                else
                    call().ShouldBe(0);
            }
            configure();
            for (var i = 0; i < 3; i++)
                call().ShouldBe(40);
        }

        [Theory]
        [InlineData(ImposterMode.Implicit, false)]
        [InlineData(ImposterMode.Implicit, true)]
        [InlineData(ImposterMode.Explicit, false)]
        [InlineData(ImposterMode.Explicit, true)]
        public void Given_PendingThen_When_CalledBeforeConfiguration_Should_RepeatPreviousOutcomeAndKeepNext(
            ImposterMode mode,
            bool consumeFirst
        )
        {
            var imposter = new IMethodSetupFeatureSutImposter(mode);
            var instance = imposter.Instance();
            var method = imposter.IntNoParams().Returns(20);
            var generic = imposter.GenericReturnType<int>().Returns(20);
            if (consumeFirst)
            {
                instance.IntNoParams().ShouldBe(20);
                instance.GenericReturnType<int>().ShouldBe(20);
            }
            var nextMethod = method.Then();
            var nextGeneric = generic.Then();
            for (var i = 0; i < 3; i++)
            {
                instance.IntNoParams().ShouldBe(20);
                instance.GenericReturnType<int>().ShouldBe(20);
            }
            nextMethod.Returns(30).Then().Returns(40);
            nextGeneric.Returns(30).Then().Returns(40);
            instance.IntNoParams().ShouldBe(30);
            instance.GenericReturnType<int>().ShouldBe(30);
            for (var i = 0; i < 3; i++)
            {
                instance.IntNoParams().ShouldBe(40);
                instance.GenericReturnType<int>().ShouldBe(40);
            }
        }

        [Theory]
        [InlineData(ImposterMode.Implicit)]
        [InlineData(ImposterMode.Explicit)]
        public void Given_VoidSequence_When_NextOutcomeIsPending_Should_PreservePreviousThrow(
            ImposterMode mode
        )
        {
            var imposter = new IMethodSetupFeatureSutImposter(mode);
            var first = imposter.VoidNoParams().Throws<InvalidOperationException>();
            var generic = imposter
                .GenericSingleParam<int>(Arg<int>.Any())
                .Throws<InvalidOperationException>();
            Should.Throw<InvalidOperationException>(() => imposter.Instance().VoidNoParams());
            Should.Throw<InvalidOperationException>(() =>
                imposter.Instance().GenericSingleParam(5)
            );
            var next = first.Then();
            var nextGeneric = generic.Then();
            for (var i = 0; i < 3; i++)
            {
                Should.Throw<InvalidOperationException>(() => imposter.Instance().VoidNoParams());
                Should.Throw<InvalidOperationException>(() =>
                    imposter.Instance().GenericSingleParam(5)
                );
            }
            next.Callback(() => { });
            nextGeneric.Callback(_ => { });
            Should.NotThrow(() => imposter.Instance().VoidNoParams());
            Should.NotThrow(() => imposter.Instance().GenericSingleParam(5));
        }

        [Theory]
        [InlineData(ImposterMode.Implicit)]
        [InlineData(ImposterMode.Explicit)]
        public async Task Given_SequenceBeingExtended_When_ReadersConsumeOutcomes_Should_NotLoseAnyConfiguredStep(
            ImposterMode mode
        )
        {
            const int steps = 100;
            var imposter = new IMethodSetupFeatureSutImposter(mode);
            var instance = imposter.Instance();
            var continuation = imposter.IntNoParams().Returns(1);
            var observed = new ConcurrentDictionary<int, byte>();
            observed.TryAdd(instance.IntNoParams(), 0);
            using var start = new ManualResetEventSlim();
            var writer = Task.Run(async () =>
            {
                start.Wait(TimeSpan.FromSeconds(10)).ShouldBeTrue();
                for (var i = 2; i <= steps; i++)
                {
                    var pending = continuation.Then();
                    await Task.Yield();
                    continuation = pending.Returns(i);
                }
            });
            var readers = Enumerable
                .Range(0, 4)
                .Select(_ =>
                    Task.Run(() =>
                    {
                        start.Wait(TimeSpan.FromSeconds(10)).ShouldBeTrue();
                        for (var i = 0; i < 1000; i++)
                            observed.TryAdd(instance.IntNoParams(), 0);
                    })
                )
                .ToArray();
            start.Set();
            await Task.WhenAll(readers.Append(writer));
            // Readers may finish before the writer, so consume any remaining queued outcomes.
            for (var i = 0; i < steps; i++)
                observed.TryAdd(instance.IntNoParams(), 0);
            observed.Keys.OrderBy(value => value).ShouldBe(Enumerable.Range(1, steps));
            instance.IntNoParams().ShouldBe(steps);
        }

        [Theory]
        [InlineData(ImposterMode.Implicit)]
        [InlineData(ImposterMode.Explicit)]
        public async Task Given_PendingSequence_When_CallsRunOnAnotherThread_Should_PreserveAndPublishOutcomes(
            ImposterMode mode
        )
        {
            var imposter = new IMethodSetupFeatureSutImposter(mode);
            var sequence = imposter.IntNoParams().Returns(10);
            imposter.Instance().IntNoParams().ShouldBe(10);
            var pending = sequence.Then();
            using var pendingCallsCompleted = new ManualResetEventSlim();
            using var configurationCompleted = new ManualResetEventSlim();
            var reader = Task.Run(() =>
            {
                try
                {
                    for (var i = 0; i < 100; i++)
                        imposter.Instance().IntNoParams().ShouldBe(10);
                }
                finally
                {
                    pendingCallsCompleted.Set();
                }
                configurationCompleted.Wait(TimeSpan.FromSeconds(10)).ShouldBeTrue();
                for (var i = 0; i < 100; i++)
                    imposter.Instance().IntNoParams().ShouldBe(20);
            });
            try
            {
                pendingCallsCompleted.Wait(TimeSpan.FromSeconds(10)).ShouldBeTrue();
                pending.Returns(20);
            }
            finally
            {
                configurationCompleted.Set();
            }
            await reader;
        }
    }
}
