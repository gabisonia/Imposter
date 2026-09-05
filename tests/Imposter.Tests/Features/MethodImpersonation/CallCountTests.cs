using System;
using System.Threading.Tasks;
using Imposter.Abstractions;
using Shouldly;
using Xunit;

namespace Imposter.Tests.Features.MethodImpersonation
{
    public class CallCountTests
    {
        private readonly IMethodSetupFeatureSutImposter _sut = new IMethodSetupFeatureSutImposter();

        [Fact]
        public void Given_NoCalls_When_ReadingCallCount_Should_ReturnZero()
        {
            _sut.CallCount().VoidNoParams().ShouldBe(0);
            _sut.CallCount().IntSingleParam(Arg<int>.Any()).ShouldBe(0);
        }

        [Fact]
        public void Given_PreviousCalls_When_CallingAgain_Should_ExposeAdditionalCallCount()
        {
            var counter = _sut.CallCount();
            _sut.Instance().VoidNoParams();
            var previousCount = counter.VoidNoParams();

            _sut.Instance().VoidNoParams();
            _sut.Instance().VoidNoParams();

            previousCount.ShouldBe(1);
            (counter.VoidNoParams() - previousCount).ShouldBe(2);
            counter.VoidNoParams().ShouldBe(3);
            _sut.VoidNoParams().Called(Count.Exactly(3));
        }

        [Fact]
        public void Given_DifferentArguments_When_ReadingCallCount_Should_CountOnlyMatchingCalls()
        {
            _sut.Instance().IntSingleParam(1);
            _sut.Instance().IntSingleParam(2);
            _sut.Instance().IntSingleParam(2);

            _sut.CallCount().IntSingleParam(Arg<int>.Any()).ShouldBe(3);
            _sut.CallCount().IntSingleParam(2).ShouldBe(2);
            _sut.CallCount().IntSingleParam(Arg<int>.Is(value => value > 1)).ShouldBe(2);
            _sut.CallCount().IntSingleParam(3).ShouldBe(0);
        }

        [Fact]
        public void Given_ExplicitMode_When_ReadingCallCount_Should_NotCreateASetup()
        {
            var imposter = new IMethodSetupFeatureSutImposter(ImposterMode.Explicit);

            imposter.CallCount().VoidNoParams().ShouldBe(0);

            Should.Throw<MissingImposterException>(() => imposter.Instance().VoidNoParams());
        }

        [Fact]
        public void Given_ParallelCalls_When_WorkCompletes_Should_CountAllInvocations()
        {
            var counter = _sut.CallCount();
            Parallel.For(0, 100, _ => _sut.Instance().VoidNoParams());

            counter.VoidNoParams().ShouldBe(100);
        }

        [Fact]
        public void Given_ClassWithBaseImplementation_When_ReadingCallCount_Should_PreserveBehavior()
        {
            var imposter = new MethodSetupFeatureClassSutImposter();
            imposter.IntSingleParam(Arg<int>.Any()).UseBaseImplementation();
            var counter = imposter.CallCount();

            imposter.Instance().IntSingleParam(5).ShouldBe(10);
            counter.IntSingleParam(5).ShouldBe(1);
            imposter.Instance().IntSingleParam(5).ShouldBe(10);
            counter.IntSingleParam(5).ShouldBe(2);
        }

        [Fact]
        public void Given_ReturnSetup_When_ReadingCallCount_Should_PreserveConfiguredBehavior()
        {
            _sut.IntNoParams().Returns(42);

            _sut.Instance().IntNoParams().ShouldBe(42);
            _sut.CallCount().IntNoParams().ShouldBe(1);
            _sut.Instance().IntNoParams().ShouldBe(42);
            _sut.CallCount().IntNoParams().ShouldBe(2);
        }

        [Fact]
        public void Given_GenericCalls_When_ReadingCallCount_Should_MatchTypeAndArguments()
        {
            _sut.Instance().GenericSingleParam(42);
            _sut.Instance().GenericSingleParam("42");
            _sut.Instance().GenericSingleParam(43);
            _sut.Instance().GenericReturnType<int>();
            _sut.Instance().GenericReturnType<string>();
            _sut.Instance().GenericReturnType<string>();

            _sut.CallCount().GenericSingleParam(Arg<int>.Any()).ShouldBe(2);
            _sut.CallCount().GenericSingleParam<int>(42).ShouldBe(1);
            _sut.CallCount().GenericSingleParam(Arg<string>.Any()).ShouldBe(1);
            _sut.CallCount().GenericReturnType<int>().ShouldBe(1);
            _sut.CallCount().GenericReturnType<string>().ShouldBe(2);
            _sut.CallCount().GenericReturnType<bool>().ShouldBe(0);
        }

        [Fact]
        public void Given_RefAndOutParameters_When_ReadingCallCount_Should_MatchInvocationHistory()
        {
            var value = 10;
            _sut.Instance().IntRefParam(ref value);
            _sut.Instance().IntOutParam(out _);

            _sut.CallCount().IntRefParam(10).ShouldBe(1);
            _sut.CallCount().IntRefParam(20).ShouldBe(0);
            _sut.CallCount().IntOutParam(OutArg<int>.Any()).ShouldBe(1);
        }

        [Fact]
        public async Task Given_AsyncCalls_When_ReadingCallCount_Should_CountRecordedInvocations()
        {
            await _sut.Instance().AsyncTaskIntNoParams();
            await _sut.Instance().AsyncValueTaskIntNoParams();

            _sut.CallCount().AsyncTaskIntNoParams().ShouldBe(1);
            _sut.CallCount().AsyncValueTaskIntNoParams().ShouldBe(1);
        }

        [Fact]
        public void Given_ThrowingMethod_When_ReadingCallCount_Should_CountFailedInvocation()
        {
            _sut.VoidNoParams().Throws<InvalidOperationException>();

            Should.Throw<InvalidOperationException>(() => _sut.Instance().VoidNoParams());

            _sut.CallCount().VoidNoParams().ShouldBe(1);
        }
    }
}
