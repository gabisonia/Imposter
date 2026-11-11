using System.Collections.Generic;
using System.Linq;
using Imposter.CodeGenerator.Features.IndexerImpersonation.Metadata;
using Imposter.CodeGenerator.SyntaxHelpers;
using Imposter.CodeGenerator.SyntaxHelpers.Builders;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Imposter.CodeGenerator.SyntaxHelpers.SyntaxFactoryHelper;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Imposter.CodeGenerator.Features.IndexerImpersonation.Builders;

internal static class IndexerHistoryBuilder
{
    internal static MethodDeclarationSyntax Wrap(
        in ImposterIndexerMetadata indexer,
        MethodDeclarationSyntax method,
        bool getter
    )
    {
        var record = getter ? indexer.GetterRecord : indexer.SetterRecord;
        var history = getter
            ? indexer.GetterImplementation.InvocationHistoryField
            : indexer.SetterImplementation.InvocationHistoryField;
        var arguments = IdentifierName(
            getter
                ? indexer.GetterImplementation.ArgumentsVariableName
                : indexer.SetterImplementation.ArgumentsVariableName
        );
        var sequence = IdentifierName(record.SequenceLocal);
        var failure = IdentifierName(record.ExceptionLocal);
        var result = IdentifierName(record.ResultLocal);
        var statements = new List<StatementSyntax>
        {
            LocalVariableDeclarationSyntax(
                WellKnownTypes.Long,
                record.SequenceLocal,
                WellKnownTypes
                    .Imposter.Abstractions.InvocationSequence.Dot(IdentifierName("Next"))
                    .Call()
            ),
            LocalVariableDeclarationSyntax(
                WellKnownTypes.System.Exception.ToNullableType(),
                record.ExceptionLocal,
                Null
            ),
        };
        if (getter && !indexer.Core.IsValuePassedThrough)
            statements.Add(
                LocalVariableDeclarationSyntax(
                    indexer.Core.NullableAwareValueTypeSyntax,
                    record.ResultLocal,
                    DefaultNonNullable
                )
            );
        if (!getter && indexer.Core.HasSpanValue)
        {
            var value = IdentifierName(indexer.SetterImplementation.ValueParameterName);
            statements.Add(
                LocalVariableDeclarationSyntax(
                    indexer.Core.NullableAwareKeptTypeSyntax,
                    record.ResultLocal,
                    CopyArray(value)
                )
            );
        }
        BlockSyntax invocation;
        if (getter)
        {
            statements.Add(method.Body!.Statements[0]);
            invocation = ((TryStatementSyntax)method.Body.Statements[1]).Block;
            if (!indexer.Core.IsValuePassedThrough)
                invocation = (BlockSyntax)new CaptureReturn(result).Visit(invocation)!;
        }
        else
        {
            statements.Add(method.Body!.Statements[1]);
            invocation = Block(
                method.Body.Statements.Where((_, index) => index != 1 && index != 2)
            );
        }
        ExpressionSyntax capturedArguments = arguments;
        if (indexer.Core.MatchedParameters.Any(parameter => parameter.Model.Span is not null))
        {
            capturedArguments = IdentifierName(record.ArgumentsLocal);
            statements.Add(
                LocalVariableDeclarationSyntax(
                    indexer.Arguments.TypeSyntax,
                    record.ArgumentsLocal,
                    indexer.Arguments.TypeSyntax.New(
                        ArgumentListSyntax(
                            indexer.Core.MatchedParameters.Select(parameter =>
                            {
                                ExpressionSyntax value = arguments.Dot(
                                    IdentifierName(parameter.FieldName)
                                );
                                return Argument(
                                    parameter.Model.Span is null ? value : CopyArray(value)
                                );
                            })
                        )
                    )
                )
            );
        }
        var recordArguments = new List<ArgumentSyntax> { Argument(capturedArguments) };
        if (!indexer.Core.IsValuePassedThrough)
        {
            ExpressionSyntax value =
                getter || indexer.Core.HasSpanValue
                    ? result
                    : IdentifierName(indexer.SetterImplementation.ValueParameterName);
            if (getter && indexer.Core.HasSpanValue)
                value = CopyArray(value);
            else if (!indexer.Core.HasSpanValue)
                value = indexer.Core.KeptValue(value);
            recordArguments.Add(Argument(value));
        }
        recordArguments.Add(Argument(sequence));
        recordArguments.Add(Argument(failure));
        var caught = IdentifierName(
            indexer.Core.CreateParameterNameSet().Use("caughtInvocationException")
        );
        statements.Add(
            TryStatement(
                invocation,
                SingletonList(
                    CatchClause(
                        CatchDeclaration(WellKnownTypes.System.Exception, caught.Identifier),
                        null,
                        Block(failure.Assign(caught).ToStatementSyntax(), ThrowStatement())
                    )
                ),
                FinallyClause(
                    Block(
                        LazyCollectionSyntaxHelper
                            .EnsureInitialized(history.Name)
                            .Dot(IdentifierName("Push"))
                            .Call(Argument(record.Type.New(ArgumentListSyntax(recordArguments))))
                            .ToStatementSyntax()
                    )
                )
            )
        );
        return method.WithBody(Block(statements));
    }

    private static InvocationExpressionSyntax CopyArray(ExpressionSyntax value) =>
        WellKnownTypes
            .Imposter.Abstractions.InvocationSnapshots.Dot(IdentifierName("CopySpan"))
            .Call(Argument(value));

    internal static MethodDeclarationSyntax Snapshot(
        in ImposterIndexerMetadata indexer,
        bool getter
    )
    {
        var history = getter
            ? indexer.GetterImplementation.InvocationHistoryField
            : indexer.SetterImplementation.InvocationHistoryField;
        return new MethodDeclarationBuilder(
            WellKnownTypes.System.Collections.Generic.IReadOnlyList(
                WellKnownTypes.Imposter.Abstractions.IImposterInvocation
            ),
            "Snapshot"
        )
            .AddModifier(Token(SyntaxKind.InternalKeyword))
            .WithBody(
                Block(
                    ReturnStatement(
                        WellKnownTypes
                            .Imposter.Abstractions.InvocationSnapshots.Dot(
                                IdentifierName("Combine")
                            )
                            .Call([Argument(IdentifierName(history.Name)), Argument(Null)])
                    )
                )
            )
            .Build();
    }

    internal static MethodDeclarationSyntax Matching(
        in ImposterIndexerMetadata indexer,
        bool getter
    )
    {
        var record = getter ? indexer.GetterRecord : indexer.SetterRecord;
        var history = IdentifierName(
            (
                getter
                    ? indexer.GetterImplementation.InvocationHistoryField
                    : indexer.SetterImplementation.InvocationHistoryField
            ).Name
        );
        var result = IdentifierName("matches");
        var entry = IdentifierName("entry");
        return new MethodDeclarationBuilder(
            WellKnownTypes.System.Collections.Generic.IReadOnlyList(record.Type),
            "MatchingInvocations"
        )
            .AddModifier(Token(SyntaxKind.InternalKeyword))
            .AddParameter(ParameterSyntax(indexer.ArgumentsCriteria.TypeSyntax, "criteria"))
            .WithBody(
                Block(
                    LocalVariableDeclarationSyntax(
                        Var,
                        "matches",
                        WellKnownTypes.System.Collections.Generic.List(record.Type).New()
                    ),
                    IfStatement(
                        history.IsNotNull(),
                        ForEachStatement(
                            Var,
                            entry.Identifier,
                            history,
                            IfStatement(
                                IdentifierName("criteria")
                                    .Dot(IdentifierName("Matches"))
                                    .Call(Argument(entry.Dot(IdentifierName("Arguments")))),
                                result
                                    .Dot(IdentifierName("Add"))
                                    .Call(Argument(entry))
                                    .ToStatementSyntax()
                            )
                        )
                    ),
                    ReturnStatement(
                        WellKnownTypes
                            .Imposter.Abstractions.InvocationSnapshots.Dot(IdentifierName("Order"))
                            .Call(Argument(result))
                    )
                )
            )
            .Build();
    }

    internal static PropertyDeclarationSyntax Invocations(
        in ImposterIndexerMetadata indexer,
        bool getter,
        bool implementation
    )
    {
        var record = getter ? indexer.GetterRecord : indexer.SetterRecord;
        var builder = new PropertyDeclarationBuilder(
            WellKnownTypes.System.Collections.Generic.IReadOnlyList(record.Type),
            "Invocations"
        );
        if (!implementation)
            return builder.WithGetter().Build();
        var accessor = getter
            ? indexer.GetterImplementation.Builder
            : indexer.SetterImplementation.Builder;
        var matching = IdentifierName(accessor.ImposterFieldName)
            .Dot(IdentifierName("MatchingInvocations"))
            .Call(Argument(IdentifierName(accessor.CriteriaFieldName)));
        return builder
            .WithExplicitInterfaceSpecifier(
                ExplicitInterfaceSpecifier(
                    getter
                        ? indexer.GetterBuilderInterface.VerificationInterfaceTypeSyntax
                        : indexer.SetterBuilderInterface.VerificationInterfaceTypeSyntax
                )
            )
            .WithGetterBody(Block(ReturnStatement(matching)))
            .Build();
    }

    private sealed class CaptureReturn(IdentifierNameSyntax result) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node) =>
            node.Expression is null ? node : node.WithExpression(result.Assign(node.Expression));

        public override SyntaxNode? VisitSimpleLambdaExpression(
            SimpleLambdaExpressionSyntax node
        ) => node;

        public override SyntaxNode? VisitParenthesizedLambdaExpression(
            ParenthesizedLambdaExpressionSyntax node
        ) => node;
    }
}
