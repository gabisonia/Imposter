using System;
using System.Threading;
using System.Threading.Tasks;
using Imposter.Abstractions;
using Shouldly;
using Xunit;

[assembly: GenerateImposter(typeof(Imposter.Tests.Features.Verification.IVerificationService))]

namespace Imposter.Tests.Features.Verification
{
    public interface IVerificationService
    {
        void Save(int value);
        int Read(int value);
        T Echo<T>(T value);
        void VerifyNoOtherCalls();
    }

    public class VerificationCompletenessTests
    {
        private readonly IVerificationServiceImposter _sut = new();

        [Fact]
        public void GivenRecordedCalls_WhenOnlyOneIsCovered_ShouldReportTheOther()
        {
            _sut.Instance().Save(1);
            _sut.Instance().Save(2);
            _sut.Save(1).Called(Count.Once());
            var failure = Should.Throw<VerificationFailedException>(() =>
                ((IImposter)_sut).VerifyNoOtherCalls()
            );
            failure.Message.ShouldContain("<2>");
            _sut.Save(2).Called(Count.Once());
            ((IImposter)_sut).VerifyNoOtherCalls();
            Should.Throw<VerificationFailedException>(() => _sut.VerifyNoInteractions());
        }

        [Fact]
        public void GivenRecordedCall_WhenCountingReadingOrFailingVerification_ShouldNotCoverIt()
        {
            _sut.Instance().Save(1);
            var builder = _sut.Save(1);
            builder.CallCount().ShouldBe(1);
            builder.Invocations.Count.ShouldBe(1);
            Should.Throw<VerificationFailedException>(() => builder.Called(Count.Twice()));
            _sut.Save(2).Called(Count.Never());
            Should.Throw<VerificationFailedException>(() => ((IImposter)_sut).VerifyNoOtherCalls());
        }

        [Fact]
        public void GivenCallAfterVerification_WhenCheckingCoverage_ShouldRemainUncovered()
        {
            _sut.Instance().Save(1);
            _sut.Save(Arg<int>.Any()).Called(Count.Once());
            _sut.Instance().Save(2);
            Should.Throw<VerificationFailedException>(() => ((IImposter)_sut).VerifyNoOtherCalls());
        }

        [Fact]
        public async Task GivenConcurrentCall_WhenVerificationCapturedHistory_ShouldNotCoverNewCall()
        {
            _sut.Instance().Save(1);
            using var entered = new ManualResetEventSlim();
            using var resume = new ManualResetEventSlim();
            var builder = _sut.Save(
                Arg<int>.Is(value =>
                {
                    entered.Set();
                    resume.Wait(TimeSpan.FromSeconds(10)).ShouldBeTrue();
                    return value > 0;
                })
            );
            var verification = Task.Run(() => builder.Called(Count.Once()));
            entered.Wait(TimeSpan.FromSeconds(10)).ShouldBeTrue();
            _sut.Instance().Save(2);
            resume.Set();
            await verification;
            Should.Throw<VerificationFailedException>(() => ((IImposter)_sut).VerifyNoOtherCalls());
        }

        [Fact]
        public void GivenShadowedAndLenientSetups_WhenVerifyingUsage_ShouldIgnoreOnlyLenient()
        {
            _sut.Read(1).Returns(1);
            _sut.Read(1).Returns(2);
            _sut.Read(2).Returns(3).Lenient();
            _sut.Read(3); // A verification handle is not a configured setup.
            _sut.Instance().Read(1).ShouldBe(2);
            var failure = Should.Throw<VerificationFailedException>(() =>
                _sut.VerifyAllSetupsUsed()
            );
            failure.Message.ShouldContain("Read");
            failure.Message.ShouldNotContain("<2>");
        }

        [Fact]
        public void GivenSequentialThrowingAndGenericSetups_WhenUsed_ShouldCountTheWholeChain()
        {
            _sut.Read(1).Returns(10).Then().Returns(20);
            _sut.Save(1).Throws<InvalidOperationException>();
            _sut.Echo<int>(1).Returns(2);
            _sut.Instance().Read(1).ShouldBe(10);
            Should.Throw<InvalidOperationException>(() => _sut.Instance().Save(1));
            _sut.Instance().Echo(1).ShouldBe(2);
            _sut.VerifyAllSetupsUsed();
            _sut.Echo<int>(1).Called(Count.Once());
            _sut.Save(1).Called(Count.Once());
            _sut.Read(1).Called(Count.Once());
            ((IImposter)_sut).VerifyNoOtherCalls();
        }

        [Fact]
        public void GivenSeveralImposters_WhenVerifyingTogether_ShouldReportEveryFailure()
        {
            var second = new IVerificationServiceImposter();
            _sut.Instance().Save(1);
            second.Instance().Save(2);
            var failure = Should.Throw<VerificationFailedException>(() =>
                Imposters.VerifyNoInteractions(_sut, second)
            );
            failure.Message.ShouldContain("#1");
            failure.Message.ShouldContain("#2");
        }

        [Fact]
        public void GivenMemberNamedVerifyNoOtherCalls_WhenUsingImposterVerification_ShouldRemainReachable()
        {
            _sut.Instance().VerifyNoOtherCalls();
            _sut.VerifyNoOtherCalls().Called(Count.Once());
            ((IImposter)_sut).VerifyNoOtherCalls();
        }
    }
}
