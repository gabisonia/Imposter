using System.Collections.Generic;
using Imposter.Abstractions;
using Xunit;

[assembly: GenerateImposter(typeof(Imposter.Tests.Features.Docs.IHistoryExample))]

namespace Imposter.Tests.Features.Docs
{
    public interface IHistoryExample
    {
        string Hello(string name, int times);
    }

    public class InvocationHistoryTests
    {
        [Fact]
        public void TypedAndCombinedHistoryExamples()
        {
            var imposter = new IHistoryExampleImposter();
            var instance = imposter.Instance();
            instance.Hello("Ann", 1);
            instance.Hello("Bob", 2);

            var calls = imposter.Hello(Arg<string>.Any(), Arg<int>.Any()).Invocations;
            Assert.Equal("Ann", calls[0].Name);
            Assert.Equal(1, calls[0].Times);
            Assert.True(calls[0].Sequence < calls[1].Sequence);

            IReadOnlyList<IImposterInvocation> history = imposter.Invocations();
            Assert.Equal(2, history.Count);
            Assert.Equal("Hello", history[0].Member);
            Assert.Same(calls[0], history[0]);

            imposter.ClearInvocations();
            Assert.Empty(imposter.Invocations());
            Assert.Equal(2, calls.Count);
        }
    }
}
