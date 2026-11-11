using System.Collections.Generic;
using Imposter.CodeGenerator.Features.PropertyImpersonation.Metadata;
using Imposter.CodeGenerator.SyntaxHelpers;
using Imposter.CodeGenerator.SyntaxHelpers.Builders;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Imposter.CodeGenerator.SyntaxHelpers.SyntaxFactoryHelper;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Imposter.CodeGenerator.Features.PropertyImpersonation.Builders.PropertyImposter;

internal static partial class PropertyImposterBuilder
{
    private static List<MemberDeclarationSyntax> BuildVerificationSources(
        in ImposterPropertyMetadata property
    )
    {
        var members = new List<MemberDeclarationSyntax>();
        var source = WellKnownTypes.Imposter.Abstractions.IImposterVerificationSource;
        var resultType = WellKnownTypes.System.Collections.Generic.IEnumerable(
            WellKnownTypes.String
        );
        foreach (var method in new[] { "Interactions", "UncoveredInteractions", "UnusedSetups" })
        {
            var statements = new List<StatementSyntax>
            {
                LocalVariableDeclarationSyntax(
                    Var,
                    "findings",
                    WellKnownTypes.System.Collections.Generic.List(WellKnownTypes.String).New()
                ),
            };
            if (property.Core.HasGetter)
                statements.Add(
                    AddFindings(
                        property.ImposterBuilder.GetterImposterBuilderField.Name,
                        source,
                        method
                    )
                );
            if (property.Core.HasSetter)
                statements.Add(
                    AddFindings(property.ImposterBuilder.SetterImposterField.Name, source, method)
                );
            statements.Add(ReturnStatement(IdentifierName("findings")));
            members.Add(
                new MethodDeclarationBuilder(resultType, method)
                    .WithExplicitInterfaceSpecifier(source)
                    .WithBody(Block(statements))
                    .Build()
            );
        }
        return members;
    }

    private static ExpressionStatementSyntax AddFindings(
        string field,
        TypeSyntax source,
        string method
    ) =>
        IdentifierName("findings")
            .Dot(IdentifierName("AddRange"))
            .Call(
                Argument(
                    ParenthesizedExpression(CastExpression(source, IdentifierName(field)))
                        .Dot(IdentifierName(method))
                        .Call()
                )
            )
            .ToStatementSyntax();

    private static MethodDeclarationSyntax BuildSnapshot(in ImposterPropertyMetadata property)
    {
        var invocation = WellKnownTypes.Imposter.Abstractions.IImposterInvocation;
        ExpressionSyntax result = property.Core.HasSetter
            ? IdentifierName(property.ImposterBuilder.SetterImposterField.Name)
                .Dot(IdentifierName("Snapshot"))
                .Call()
            : WellKnownTypes.System.Collections.Generic.List(invocation).New();
        return new MethodDeclarationBuilder(
            WellKnownTypes.System.Collections.Generic.IReadOnlyList(invocation),
            "Snapshot"
        )
            .AddModifier(Token(SyntaxKind.InternalKeyword))
            .WithBody(Block(ReturnStatement(result)))
            .Build();
    }
}
