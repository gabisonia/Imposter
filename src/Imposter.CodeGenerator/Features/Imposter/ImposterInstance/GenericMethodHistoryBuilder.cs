using System.Collections.Generic;
using System.Linq;
using Imposter.CodeGenerator.Features.MethodImpersonation.Metadata.ImposterTargetMethod;
using Imposter.CodeGenerator.Features.MethodImpersonation.Metadata.InvocationHistory;
using Imposter.CodeGenerator.Helpers;
using Imposter.CodeGenerator.SyntaxHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Imposter.CodeGenerator.SyntaxHelpers.SyntaxFactoryHelper;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Imposter.CodeGenerator.Features.Imposter.ImposterInstance;

// Own generic history at the caller boundary, before setup selection can run user predicates.
// The same boundary records calls dispatched through a compatible-type adapter without another node.
internal static class GenericMethodHistoryBuilder
{
    internal static BlockSyntax Build(
        in ImposterTargetMethodMetadata method,
        ExpressionSyntax invoke,
        string imposterField
    )
    {
        var names = new NameSet(
            method
                .Model.Parameters.Select(parameter => parameter.Name)
                .Concat(method.Model.TypeParameters.Select(parameter => parameter.Name))
        );
        var sequence = IdentifierName(names.Use("invocationSequence"));
        var failure = IdentifierName(names.Use("invocationException"));
        var caught = IdentifierName(names.Use("caughtInvocationException"));
        var result = IdentifierName(names.Use("invocationResult"));
        var returned = IdentifierName(names.Use("returnedValue"));
        var captures = method.Parameters.InputParameterMetadata.ToDictionary(
            parameter => parameter.Name,
            parameter => names.Use("captured" + parameter.Name.TrimStart('@'))
        );
        var body = new List<StatementSyntax>
        {
            LocalVariableDeclarationSyntax(
                WellKnownTypes.Long,
                sequence.Identifier.Text,
                WellKnownTypes
                    .Imposter.Abstractions.InvocationSequence.Dot(IdentifierName("Next"))
                    .Call()
            ),
            LocalVariableDeclarationSyntax(
                WellKnownTypes.System.Exception.ToNullableType(),
                failure.Identifier.Text,
                Null
            ),
        };
        foreach (var parameter in method.Parameters.InputParameterMetadata)
            body.Add(
                LocalVariableDeclarationSyntax(
                    parameter.NullableAwareKeptTypeSyntax,
                    captures[parameter.Name],
                    parameter.KeptValue
                )
            );
        foreach (var parameter in method.Parameters.OutputParameters)
            body.Add(
                IdentifierName(EscapeKeyword(parameter.Name))
                    .Assign(DefaultNonNullable)
                    .ToStatementSyntax()
            );
        if (method.KeepsResult)
            body.Add(
                LocalVariableDeclarationSyntax(
                    method.ReturnType.KeptTypeSyntax,
                    result.Identifier.Text,
                    DefaultNonNullable
                )
            );

        var invocation = new List<StatementSyntax>();
        if (!method.HasReturnValue)
            invocation.Add(invoke.ToStatementSyntax());
        else if (!method.KeepsResult)
            invocation.Add(ReturnStatement(invoke));
        else
        {
            invocation.Add(LocalVariableDeclarationSyntax(Var, returned.Identifier.Text, invoke));
            invocation.Add(
                result.Assign(method.ReturnType.KeptValue(returned)).ToStatementSyntax()
            );
            invocation.Add(ReturnStatement(returned));
        }
        var recordArguments = new List<ArgumentSyntax>();
        foreach (var parameter in method.Parameters.MatchedParameterMetadata)
            recordArguments.Add(
                Argument(
                    parameter.Model.RefKind == RefKind.Out
                        ? parameter.KeptValue
                        : IdentifierName(captures[parameter.Name])
                )
            );
        if (method.KeepsResult)
            recordArguments.Add(Argument(result));
        recordArguments.Add(Argument(failure));
        recordArguments.Add(Argument(sequence));
        body.Add(
            TryStatement(
                Block(invocation),
                SingletonList(
                    CatchClause(
                        CatchDeclaration(WellKnownTypes.System.Exception, caught.Identifier),
                        null,
                        Block(failure.Assign(caught).ToStatementSyntax(), ThrowStatement())
                    )
                ),
                FinallyClause(
                    Block(
                        IdentifierName(imposterField)
                            .Dot(IdentifierName(method.InvocationHistory.Collection.AsField.Name))
                            .Dot(IdentifierName(InvocationHistoryCollectionMetadata.AddMethodName))
                            .Call(
                                Argument(
                                    method.InvocationHistory.Syntax.New(
                                        ArgumentListSyntax(recordArguments)
                                    )
                                )
                            )
                            .ToStatementSyntax()
                    )
                )
            )
        );
        return Block(body);
    }
}
