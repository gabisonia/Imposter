using Imposter.Abstractions;
using Imposter.Tests.Features.Verification;
using Shouldly;
using Xunit;

namespace Imposter.Tests.Docs
{
    public class ClearAndResetDocTests
    {
        [Fact]
        public void GivenSequentialSetup_WhenClearedAndReset_ShouldFollowDocumentedLifecycle()
        {
            var imposter = new ILifecycleTargetImposter();
            imposter.Next().Returns(1).Then().Returns(2);
            var instance = imposter.Instance();
            instance.Next().ShouldBe(1);

            ((IImposter)imposter).ClearInvocations();
            imposter.Next().Called(Count.Never());
            instance.Next().ShouldBe(2);

            ((IImposter)imposter).Reset();
            imposter.Next().Returns(42);
            instance.Next().ShouldBe(42);
        }
    }
}
