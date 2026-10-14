using Imposter.CodeGenerator.Features.MethodImpersonation.Metadata.ImposterTargetMethod;
using Imposter.CodeGenerator.Features.MethodImpersonation.Metadata.MethodImposter;
using Imposter.CodeGenerator.SyntaxHelpers;
using Imposter.CodeGenerator.SyntaxHelpers.Builders;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Imposter.CodeGenerator.Features.MethodImpersonation.Builders.MethodImposter;

internal static partial class MethodImposterBuilder
{
    private static MethodDeclarationSyntax BuildHasMatchingInvocationImposterGroupMethod(
        in ImposterTargetMethodMetadata method
    )
    {
        var hasMatchingMethod = method.MethodImposter.HasMatchingInvocationImposterGroupMethod;

        var group = IdentifierName(
            method.MethodImposter.FindMatchingInvocationImposterGroupMethod.GroupVariableName
        );
        return new MethodDeclarationBuilder(hasMatchingMethod.ReturnType, hasMatchingMethod.Name)
            .AddModifier(Token(SyntaxKind.PublicKeyword))
            .AddParameter(GetHasMatchingInvocationImposterGroupParameter(method, hasMatchingMethod))
            .AddParameter(
                SyntaxFactoryHelper
                    .ParameterSyntax(
                        WellKnownTypes.Bool,
                        hasMatchingMethod.HasConfiguredOutcomeParameterName
                    )
                    .WithModifiers(TokenList(Token(SyntaxKind.OutKeyword)))
            )
            .WithBody(
                Block(
                    SyntaxFactoryHelper.LocalVariableDeclarationSyntax(
                        SyntaxFactoryHelper.Var,
                        group.Identifier.Text,
                        IdentifierName(
                                method.MethodImposter.FindMatchingInvocationImposterGroupMethod.Name
                            )
                            .Call(
                                GetFindMatchingInvocationImposterGroupArguments(
                                    method,
                                    hasMatchingMethod
                                )
                            )
                    ),
                    IdentifierName(hasMatchingMethod.HasConfiguredOutcomeParameterName)
                        .Assign(
                            group
                                .IsNotNull()
                                .And(
                                    group.Dot(
                                        IdentifierName(
                                            method
                                                .MethodInvocationImposterGroup
                                                .HasConfiguredOutcomeFieldName
                                        )
                                    )
                                )
                        )
                        .ToStatementSyntax(),
                    ReturnStatement(group.IsNotNull())
                )
            )
            .Build();

        static ParameterSyntax? GetHasMatchingInvocationImposterGroupParameter(
            in ImposterTargetMethodMetadata method,
            in HasMatchingInvocationImposterGroupMethodMetadata hasMatchingMethod
        )
        {
            return method.Parameters.HasInputParameters
                ? SyntaxFactoryHelper.ParameterSyntax(
                    method.Arguments.Syntax,
                    hasMatchingMethod.ArgumentsParameterName
                )
                : null;
        }

        static ArgumentListSyntax GetFindMatchingInvocationImposterGroupArguments(
            in ImposterTargetMethodMetadata method,
            in HasMatchingInvocationImposterGroupMethodMetadata hasMatchingMethod
        )
        {
            return method.Parameters.HasInputParameters
                ? IdentifierName(hasMatchingMethod.ArgumentsParameterName).ToSingleArgumentList()
                : SyntaxFactoryHelper.EmptyArgumentListSyntax;
        }
    }
}
