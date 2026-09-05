using System.Linq;
using Imposter.CodeGenerator.Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Imposter.CodeGenerator.Features.MethodImpersonation.Metadata.InvocationHistory;

internal readonly struct MethodCallCounterMetadata
{
    internal readonly string AccessorName;
    internal readonly string TypeName;
    internal readonly NameSyntax TypeSyntax;
    internal readonly string ImposterFieldName;
    internal const string ImposterParameterName = "imposter";

    internal MethodCallCounterMetadata(in ImposterTargetMetadata target, NameSet memberNameSet)
    {
        var targetTypeParameterNames = target
            .TypeParameters.TypeArguments.OfType<IdentifierNameSyntax>()
            .Select(parameter => parameter.Identifier.ValueText)
            .ToArray();
        AccessorName = memberNameSet.Use("CallCount");
        while (targetTypeParameterNames.Contains(AccessorName))
        {
            AccessorName = memberNameSet.Use("CallCount");
        }

        var methodNames = target
            .Methods.Select(method =>
                method.RequiresExplicitInterfaceImplementation
                    ? method.UniqueName
                    : method.Symbol.Name
            )
            .ToArray();
        var reservedNames = methodNames
            .Concat(targetTypeParameterNames)
            .Concat(
                target.Methods.SelectMany(method =>
                    method.Symbol.TypeParameters.Select(parameter => parameter.Name)
                )
            )
            .ToArray();
        TypeName = memberNameSet.Use("MethodCallCounter");
        while (reservedNames.Contains(TypeName))
        {
            TypeName = memberNameSet.Use("MethodCallCounter");
        }
        TypeSyntax = IdentifierName(TypeName);
        ImposterFieldName = new NameSet(reservedNames).Use("_imposter");
    }
}
