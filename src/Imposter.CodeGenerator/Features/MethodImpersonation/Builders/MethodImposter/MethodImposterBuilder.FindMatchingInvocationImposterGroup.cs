using Imposter.CodeGenerator.Features.MethodImpersonation.Metadata.ImposterTargetMethod;
using Imposter.CodeGenerator.Features.MethodImpersonation.Metadata.InvocationSetup;
using Imposter.CodeGenerator.SyntaxHelpers;
using Imposter.CodeGenerator.SyntaxHelpers.Builders;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Imposter.CodeGenerator.SyntaxHelpers.SyntaxFactoryHelper;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Imposter.CodeGenerator.Features.MethodImpersonation.Builders.MethodImposter;

internal partial class MethodImposterBuilder
{
    internal static MemberDeclarationSyntax BuildFindMatchingInvocationImposterGroupMethod(
        in ImposterTargetMethodMetadata method
    )
    {
        var lookup = method.MethodImposter.FindMatchingInvocationImposterGroupMethod;
        var group = IdentifierName(lookup.GroupVariableName);
        var bareMatch = IdentifierName(lookup.BareMatchVariableName);
        var matchingBody = Block(
            IfStatement(
                group.Dot(
                    IdentifierName(
                        method.MethodInvocationImposterGroup.HasConfiguredOutcomeFieldName
                    )
                ),
                ReturnStatement(group)
            ),
            IfStatement(bareMatch.IsNull(), bareMatch.Assign(group).ToStatementSyntax())
        );
        StatementSyntax candidateBody = matchingBody;
        if (method.Parameters.HasInputParameters)
        {
            candidateBody = IfStatement(
                group
                    .Dot(
                        IdentifierName(
                            MethodInvocationImposterGroupMetadata.ArgumentsCriteriaPropertyName
                        )
                    )
                    .Dot(IdentifierName(method.ArgumentsCriteria.MatchesMethod.Name))
                    .Call(Argument(IdentifierName("arguments"))),
                matchingBody
            );
        }

        return new MethodDeclarationBuilder(
            NullableType(method.MethodInvocationImposterGroup.Syntax),
            lookup.Name
        )
            .AddModifier(Token(SyntaxKind.PrivateKeyword))
            .AddParameter(
                method.Parameters.HasInputParameters
                    ? ParameterSyntax(method.Arguments.Syntax, "arguments")
                    : null
            )
            .WithBody(
                Block(
                    LocalVariableDeclarationSyntax(
                        NullableType(method.MethodInvocationImposterGroup.Syntax),
                        bareMatch.Identifier.Text,
                        Null
                    ),
                    ForEachStatement(
                        Var,
                        group.Identifier,
                        IdentifierName(method.MethodImposter.InvocationImposterGroupsField.Name),
                        candidateBody is BlockSyntax body ? body : Block(candidateBody)
                    ),
                    ReturnStatement(bareMatch)
                )
            )
            .Build();
    }
}
