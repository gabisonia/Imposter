using System.Threading.Tasks;
using Imposter.Abstractions;
using Shouldly;
using Xunit;

namespace Imposter.CodeGenerator.Tests;

public class InvocationRecordTests
{
    [Fact]
    public void GivenConcurrentCoverage_WhenMarked_ShouldPreserveEntrySequence()
    {
        var record = new TestRecord(long.MaxValue - 1);
        record.IsVerified.ShouldBeFalse();
        Parallel.For(
            0,
            64,
            _ =>
            {
                record.MarkVerified();
                record.Sequence.ShouldBe(long.MaxValue - 1);
            }
        );
        record.IsVerified.ShouldBeTrue();
        record.Sequence.ShouldBe(long.MaxValue - 1);
    }

    private sealed class TestRecord(long sequence) : InvocationRecord(sequence)
    {
        public override string Member => "Member";
    }
}
