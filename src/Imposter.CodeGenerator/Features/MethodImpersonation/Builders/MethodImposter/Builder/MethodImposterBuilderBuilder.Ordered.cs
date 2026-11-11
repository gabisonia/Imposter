using Imposter.CodeGenerator.Features.MethodImpersonation.Metadata.ImposterTargetMethod;
using Imposter.CodeGenerator.Features.MethodImpersonation.Metadata.InvocationHistory;
using Imposter.CodeGenerator.SyntaxHelpers;
using Imposter.CodeGenerator.SyntaxHelpers.Builders;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Imposter.CodeGenerator.SyntaxHelpers.SyntaxFactoryHelper;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Imposter.CodeGenerator.Features.MethodImpersonation.Builders.MethodImposter.Builder;

internal static partial class MethodImposterBuilderBuilder
{
    private static MemberDeclarationSyntax[] BuildOrderedStepMembers(
        in ImposterTargetMethodMetadata method
    )
    {
        var history = IdentifierName(method.InvocationHistory.Collection.AsField.Name);
        var step = WellKnownTypes.Imposter.Abstractions.IInvocationStep;
        return new MemberDeclarationSyntax[]
        {
            PropertyDeclaration(WellKnownTypes.Imposter.Abstractions.IImposter, "Imposter")
                .WithExplicitInterfaceSpecifier(ExplicitInterfaceSpecifier(step))
                .WithExpressionBody(
                    ArrowExpressionClause(
                        history.Dot(
                            IdentifierName(InvocationHistoryCollectionMetadata.OwnerPropertyName)
                        )
                    )
                )
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
            PropertyDeclaration(WellKnownTypes.String, "Description")
                .WithExplicitInterfaceSpecifier(ExplicitInterfaceSpecifier(step))
                .WithExpressionBody(ArrowExpressionClause(BuildExpectedInvocation(method)))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
            new MethodDeclarationBuilder(
                WellKnownTypes.System.Collections.Generic.IReadOnlyList(
                    WellKnownTypes.Imposter.Abstractions.IImposterInvocation
                ),
                "MatchingInvocations"
            )
                .WithExplicitInterfaceSpecifier(step)
                .WithBody(
                    Block(
                        ReturnStatement(
                            history
                                .Dot(method.WithGenericArguments("MatchingInvocations"))
                                .Call(
                                    method.Parameters.HasInputParameters
                                        ? Argument(
                                                IdentifierName(
                                                    method
                                                        .MethodImposter
                                                        .Builder
                                                        .ArgumentsCriteriaField
                                                        .Name
                                                )
                                            )
                                            .ToSingleArgumentList()
                                        : EmptyArgumentListSyntax
                                )
                        )
                    )
                )
                .Build(),
        };
    }
}
