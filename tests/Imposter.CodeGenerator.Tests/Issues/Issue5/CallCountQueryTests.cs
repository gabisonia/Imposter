using System.Threading.Tasks;
using Imposter.CodeGenerator.Tests.Helpers;
using Xunit;

namespace Imposter.CodeGenerator.Tests.Issues.Issue5;

public class CallCountQueryTests
{
    private const string Source = /*lang=csharp*/
        """
        using Imposter.Abstractions;

        [assembly: GenerateImposter(typeof(Sample.IQueryNames))]
        [assembly: GenerateImposter(typeof(Sample.IGenericQuery<>))]
        [assembly: GenerateImposter(typeof(Sample.IQueryTypeNames<,>))]
        [assembly: GenerateImposter(typeof(Sample.IHiddenQueryNames))]

        namespace Sample
        {
            public interface IBase
            {
                int Hidden();
            }

            public interface IQueryNames : IBase
            {
                int CallCount();
                int MethodCallCounter();
                int MethodCallCounter_1();
                int _imposter();
                int Echo(int _imposter);
                int Echo(string value);
                new int Hidden();
            }

            public interface IGenericQuery<T> where T : class
            {
                T Echo(T value);
                TResult Convert<TResult>(T value) where TResult : class;
            }

            public interface IQueryTypeNames<CallCount, MethodCallCounter>
            {
                void Accept(CallCount first, MethodCallCounter second);
                void Convert<MethodCallCounter_1>(MethodCallCounter_1 value);
            }

            public interface IBaseQueryNames
            {
                int CallCount { get; }
                int MethodCallCounter { get; }
            }

            public interface IHiddenQueryNames : IBaseQueryNames
            {
                new int CallCount { get; }
                new int MethodCallCounter { get; }
                void Execute();
            }
        }
        """;

    private static readonly Task<GeneratorTestContext> TestContextTask =
        GeneratorTestHelper.CreateContext(
            Source,
            baseSourceFileName: "CallCountQuery.cs",
            snippetFileName: "Snippet.cs",
            assemblyName: nameof(CallCountQueryTests)
        );

    [Fact]
    public async Task Given_ConflictingMemberNames_When_QueryingCallCounts_Should_Compile()
    {
        var context = await TestContextTask.ConfigureAwait(false);
        var diagnostics = context.CompileSnippet( /*lang=csharp*/
            """
            using Imposter.Abstractions;
            namespace Sample
            {
                public static class Scenario
                {
                    public static void Execute()
                    {
                        var imposter = new IQueryNamesImposter();
                        imposter.CallCount().Returns(42);
                        var counts = imposter.CallCount_1();
                        int calls = counts.CallCount();
                        calls += counts.MethodCallCounter();
                        calls += counts.MethodCallCounter_1();
                        calls += counts._imposter();
                        calls += counts.Echo(Arg<int>.Any());
                        calls += counts.Echo(Arg<string>.Any());
                        calls += counts.Hidden();
                        calls += counts.Hidden_1();
                    }
                }
            }
            """
        );

        GeneratorTestHelper.AssertNoDiagnostics(diagnostics);
    }

    [Fact]
    public async Task Given_ConflictingTypeParameterNames_When_QueryingCallCounts_Should_Compile()
    {
        var context = await TestContextTask.ConfigureAwait(false);
        var diagnostics = context.CompileSnippet( /*lang=csharp*/
            """
            namespace Sample
            {
                public static class Scenario
                {
                    public static void Execute()
                    {
                        var imposter = new IQueryTypeNamesImposter<int, string>();
                        var counter = imposter.CallCount_1();
                        int calls = counter.Accept(1, "one");
                        calls += counter.Convert<int>(1);
                    }
                }
            }
            """
        );

        GeneratorTestHelper.AssertNoDiagnostics(diagnostics);
    }

    [Fact]
    public async Task Given_HiddenProperties_When_QueryingCallCounts_Should_PreserveGeneratedPropertyNames()
    {
        var context = await TestContextTask.ConfigureAwait(false);
        var diagnostics = context.CompileSnippet( /*lang=csharp*/
            """
            namespace Sample
            {
                public static class Scenario
                {
                    public static void Execute()
                    {
                        var imposter = new IHiddenQueryNamesImposter();
                        imposter.CallCount.Getter().Returns(1);
                        imposter.CallCount_1.Getter().Returns(2);
                        imposter.MethodCallCounter.Getter().Returns(3);
                        imposter.MethodCallCounter_1.Getter().Returns(4);
                        int calls = imposter.CallCount_2().Execute();
                    }
                }
            }
            """
        );

        GeneratorTestHelper.AssertNoDiagnostics(diagnostics);
    }

    [Fact]
    public async Task Given_GenericTarget_When_QueryingGenericMethods_Should_Compile()
    {
        var context = await TestContextTask.ConfigureAwait(false);
        var diagnostics = context.CompileSnippet( /*lang=csharp*/
            """
            using Imposter.Abstractions;
            namespace Sample
            {
                public static class Scenario
                {
                    public static void Execute()
                    {
                        var imposter = new IGenericQueryImposter<string>();
                        int calls = imposter.CallCount().Echo(Arg<string>.Any());
                        calls += imposter.CallCount().Convert<object>(Arg<string>.Any());
                    }
                }
            }
            """
        );

        GeneratorTestHelper.AssertNoDiagnostics(diagnostics);
    }
}
