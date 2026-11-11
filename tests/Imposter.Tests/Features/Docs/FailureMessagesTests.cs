using Imposter.Abstractions;
using Imposter.Tests.Features.Verification;
using Shouldly;
using Xunit;

namespace Imposter.Tests.Features.Docs
{
    public class FailureMessagesTests
    {
        [Fact]
        public void GivenDocumentedMatcher_WhenVerificationFails_ShouldIncludeDescriptionAndReason()
        {
            var imposter = new IDiagnosticMessagesImposter();
            var failure = Should.Throw<VerificationFailedException>(() =>
                imposter
                    .Hello(Arg<string>.Any(), Arg<int>.Is(x => x > 10).Describe("greater than 10"))
                    .Called(Count.Once(), "a greeting should be repeated")
            );
            failure.Expected.ShouldContain("greater than 10");
            failure.Message.ShouldContain("a greeting should be repeated");
        }
    }
}
