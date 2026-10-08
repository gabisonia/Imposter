using System.Threading.Tasks;
using Imposter.CodeGenerator.Tests.Helpers;
using Xunit;

namespace Imposter.CodeGenerator.Tests.Issues.Issue106;

public class InIndexerParameterTests
{
    [Theory]
    [InlineData("public interface IService { int this[in int key] { get; set; } }")]
    [InlineData("public interface IService { int this[in int key] { get; } }")]
    [InlineData("public interface IService { int this[in int key] { set; } }")]
    [InlineData("public interface IService { int this[in int key, string name] { get; set; } }")]
    [InlineData(
        "public abstract class IService { public abstract int this[in int key] { get; set; } }"
    )]
    public async Task Given_IndexerWithInParameter_When_ImposterIsGenerated_Should_Compile(
        string declaration
    )
    {
        var source = $$"""
            using Imposter.Abstractions;

            [assembly: GenerateImposter(typeof(Sample.IService))]

            namespace Sample
            {
                {{declaration}}
            }
            """;

        var context = await GeneratorTestHelper.CreateContext(
            source,
            baseSourceFileName: "InIndexerParameter.Source.cs",
            snippetFileName: "Snippet.cs",
            assemblyName: nameof(InIndexerParameterTests)
        );

        GeneratorTestHelper.AssertNoDiagnostics(context.CompileSnippet(""));
    }
}
