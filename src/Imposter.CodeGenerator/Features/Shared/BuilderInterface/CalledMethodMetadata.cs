using Imposter.CodeGenerator.SyntaxHelpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Imposter.CodeGenerator.Features.Shared.BuilderInterface;

// A builder's Called(count) verification.
internal readonly struct CalledMethodMetadata
{
    internal readonly string Name = "Called";

    internal readonly TypeSyntax ReturnType;

    internal readonly ParameterMetadata CountParameter;
    internal readonly ParameterMetadata BecauseParameter;

    public CalledMethodMetadata()
    {
        ReturnType = WellKnownTypes.Void;
        CountParameter = new ParameterMetadata("count", WellKnownTypes.Imposter.Abstractions.Count);
        BecauseParameter = new ParameterMetadata("because", WellKnownTypes.String);
    }
}
