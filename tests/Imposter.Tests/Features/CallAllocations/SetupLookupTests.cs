using System.Threading;
using System.Threading.Tasks;
using Imposter.Abstractions;
using Shouldly;
using Xunit;

namespace Imposter.Tests.Features.CallAllocations
{
    public class SetupLookupTests
    {
        [Fact]
        public void GivenOverlappingSetups_WhenCalling_ShouldUseNewestMatchingSetup()
        {
            var imposter = new ICallAllocationSutImposter();
            imposter.Square(Arg<int>.Any()).Returns(10);
            imposter.Square(Arg<int>.Is(value => value == 2)).Returns(20);
            imposter.Square(Arg<int>.Is(value => value == 3)).Returns(30);
            imposter.Echo<int>(Arg<int>.Any()).Returns(10);
            imposter.Echo<int>(Arg<int>.Is(value => value == 2)).Returns(20);
            imposter.Echo<string>(Arg<string>.Any()).Returns("different type");
            imposter[Arg<int>.Any()].Getter().Returns(10);
            imposter[Arg<int>.Is(value => value == 2)].Getter().Returns(20);
            imposter[Arg<int>.Is(value => value == 3)].Getter().Returns(30);

            var instance = imposter.Instance();
            instance.Square(1).ShouldBe(10);
            instance.Square(2).ShouldBe(20);
            instance.Square(3).ShouldBe(30);
            instance.Echo(1).ShouldBe(10);
            instance.Echo(2).ShouldBe(20);
            instance.Echo("value").ShouldBe("different type");
            instance[1].ShouldBe(10);
            instance[2].ShouldBe(20);
            instance[3].ShouldBe(30);
        }

        // The setups added while calls run match another argument than the calls use: a setup is published before its
        // outcome is configured (#381), so a matching one could still throw MissingImposterException here. What this
        // test pins is that the lookup keeps serving its snapshot while another thread prepends to the collection.
        [Fact]
        public async Task GivenConcurrentSetupAddition_WhenCallingOtherArguments_ShouldKeepServingTheOlderSetup()
        {
            var imposter = new ICallAllocationSutImposter();
            imposter.Square(Arg<int>.Any()).Returns(0);
            imposter.Echo<int>(Arg<int>.Any()).Returns(0);
            imposter[Arg<int>.Any()].Getter().Returns(0);
            var instance = imposter.Instance();
            using var start = new Barrier(2);

            var writer = Task.Run(() =>
            {
                start.SignalAndWait();
                for (var i = 0; i < 100; i++)
                {
                    // Each predicate has a distinct closure so indexer setups also create distinct lookup entries.
                    var iteration = i;
                    imposter.Square(Arg<int>.Is(value => value == 2 && iteration >= 0)).Returns(1);
                    imposter
                        .Echo<int>(Arg<int>.Is(value => value == 2 && iteration >= 0))
                        .Returns(1);
                    imposter[Arg<int>.Is(value => value == 2 && iteration >= 0)]
                        .Getter()
                        .Returns(1);
                }
            });
            var reader = Task.Run(() =>
            {
                start.SignalAndWait();
                for (var i = 0; i < 1000; i++)
                {
                    instance.Square(1).ShouldBe(0);
                    instance.Echo(1).ShouldBe(0);
                    instance[1].ShouldBe(0);
                }
            });

            await Task.WhenAll(writer, reader);
            instance.Square(2).ShouldBe(1);
            instance.Echo(2).ShouldBe(1);
            instance[2].ShouldBe(1);
        }
    }
}
