using System.Collections.Generic;
using System.Linq;
using Imposter.CodeGenerator.Features.IndexerImpersonation.Metadata;
using Imposter.CodeGenerator.SyntaxHelpers;
using Imposter.CodeGenerator.SyntaxHelpers.Builders;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Imposter.CodeGenerator.Features.IndexerImpersonation.Builders.IndexerImposterBuilderCommon;
using static Imposter.CodeGenerator.SyntaxHelpers.SyntaxFactoryHelper;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Imposter.CodeGenerator.Features.IndexerImpersonation.Builders;

internal static class IndexerVerificationBuilder
{
    internal static MethodDeclarationSyntax Cover(
        in ImposterIndexerMetadata indexer,
        bool getter,
        MethodDeclarationSyntax method
    )
    {
        var history = getter
            ? indexer.GetterImplementation.InvocationHistoryField
            : indexer.SetterImplementation.InvocationHistoryField;
        var criteria = getter
            ? indexer.GetterImplementation.CriteriaParameterName
            : indexer.SetterImplementation.CriteriaParameterName;
        var enumeratorType = QualifiedName((NameSyntax)history.Type, IdentifierName("Enumerator"));
        var cursor = IdentifierName("cursor");
        var current = cursor.Dot(IdentifierName("Current"));
        var matches = IdentifierName(criteria)
            .Dot(IdentifierName("Matches"))
            .Call(Argument(current.Dot(IdentifierName("Arguments"))));
        var count = IdentifierName("invocationCount");
        return method.WithBody(
            Block(
                List<StatementSyntax>([
                    LocalVariableDeclarationSyntax(
                        enumeratorType,
                        "snapshot",
                        ConditionalExpression(
                            IdentifierName(history.Name).IsNull(),
                            DefaultExpression(enumeratorType),
                            IdentifierName(history.Name).Dot(IdentifierName("GetEnumerator")).Call()
                        )
                    ),
                    LocalVariableDeclarationSyntax(Var, "cursor", IdentifierName("snapshot")),
                    LocalVariableDeclarationSyntax(
                        WellKnownTypes.Int,
                        "invocationCount",
                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))
                    ),
                    WhileStatement(
                        cursor.Dot(IdentifierName("MoveNext")).Call(),
                        Block(
                            IfStatement(
                                matches,
                                PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, count)
                                    .ToStatementSyntax()
                            )
                        )
                    ),
                    .. method.Body!.Statements.Skip(1),
                    cursor.Assign(IdentifierName("snapshot")).ToStatementSyntax(),
                    WhileStatement(
                        cursor.Dot(IdentifierName("MoveNext")).Call(),
                        Block(
                            IfStatement(
                                matches,
                                current
                                    .Dot(IdentifierName("MarkVerified"))
                                    .Call()
                                    .ToStatementSyntax()
                            )
                        )
                    ),
                ])
            )
        );
    }

    internal static MemberDeclarationSyntax[] Step(in ImposterIndexerMetadata indexer, bool getter)
    {
        var builder = getter
            ? indexer.GetterImplementation.Builder
            : indexer.SetterImplementation.Builder;
        var imposter = IdentifierName(builder.ImposterFieldName);
        var criteria = IdentifierName(builder.CriteriaFieldName);
        return
        [
            PropertyDeclaration(WellKnownTypes.Imposter.Abstractions.IImposter, "Imposter")
                .WithExplicitInterfaceSpecifier(
                    ExplicitInterfaceSpecifier(WellKnownTypes.Imposter.Abstractions.IInvocationStep)
                )
                .WithExpressionBody(ArrowExpressionClause(imposter.Dot(IdentifierName("Owner"))))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
            PropertyDeclaration(WellKnownTypes.String, "Description")
                .WithExplicitInterfaceSpecifier(
                    ExplicitInterfaceSpecifier(WellKnownTypes.Imposter.Abstractions.IInvocationStep)
                )
                .WithExpressionBody(
                    ArrowExpressionClause(
                        ((getter ? "get " : "set ") + indexer.Core.DisplayName)
                            .StringLiteral()
                            .Add(ExpectedIndices(indexer, criteria))
                    )
                )
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
            new MethodDeclarationBuilder(
                WellKnownTypes.System.Collections.Generic.IReadOnlyList(
                    WellKnownTypes.Imposter.Abstractions.IImposterInvocation
                ),
                "MatchingInvocations"
            )
                .WithExplicitInterfaceSpecifier(
                    WellKnownTypes.Imposter.Abstractions.IInvocationStep
                )
                .WithBody(
                    Block(
                        ReturnStatement(
                            imposter
                                .Dot(IdentifierName("MatchingInvocations"))
                                .Call(Argument(criteria))
                        )
                    )
                )
                .Build(),
        ];
    }

    internal static MethodDeclarationSyntax Lenient(in ImposterIndexerMetadata indexer, bool getter)
    {
        var continuation = getter
            ? indexer.GetterBuilderInterface.ContinuationInterfaceTypeSyntax
            : indexer.SetterBuilderInterface.ContinuationInterfaceTypeSyntax;
        var target = getter
            ? IdentifierName(IndexerGetterImposterMetadata.InvocationImposterPropertyName)
                .Dot(IdentifierName("IsLenient"))
            : IdentifierName(indexer.SetterImplementation.Builder.CriteriaFieldName)
                .Dot(IdentifierName(indexer.ArgumentsCriteria.SetterLenientFieldName));
        return new MethodDeclarationBuilder(continuation, "Lenient")
            .WithExplicitInterfaceSpecifier(continuation)
            .WithBody(Block(target.Assign(True).ToStatementSyntax(), ReturnThis))
            .Build();
    }

    internal static MemberDeclarationSyntax[] Source(
        in ImposterIndexerMetadata indexer,
        bool? getter
    )
    {
        var members = new List<MemberDeclarationSyntax>();
        var resultType = WellKnownTypes.System.Collections.Generic.IEnumerable(
            WellKnownTypes.String
        );
        foreach (var name in new[] { "Interactions", "UncoveredInteractions" })
            members.Add(
                new MethodDeclarationBuilder(resultType, name)
                    .WithExplicitInterfaceSpecifier(
                        WellKnownTypes.Imposter.Abstractions.IImposterVerificationSource
                    )
                    .WithBody(
                        Block(
                            ReturnStatement(
                                WellKnownTypes
                                    .Imposter.Abstractions.ImposterVerification.Dot(
                                        IdentifierName("Describe")
                                    )
                                    .Call([
                                        Argument(IdentifierName("Snapshot").Call()),
                                        Argument(name == "UncoveredInteractions" ? True : False),
                                    ])
                            )
                        )
                    )
                    .Build()
            );
        members.Add(
            new MethodDeclarationBuilder(resultType, "UnusedSetups")
                .WithExplicitInterfaceSpecifier(
                    WellKnownTypes.Imposter.Abstractions.IImposterVerificationSource
                )
                .WithBody(Block(Unused(indexer, getter)))
                .Build()
        );
        return members.ToArray();
    }

    private static StatementSyntax[] Unused(in ImposterIndexerMetadata indexer, bool? getter)
    {
        if (getter is true)
            return
            [
                ForEachStatement(
                    Var,
                    Identifier("setup"),
                    IdentifierName(indexer.GetterImplementation.SetupsField.Name),
                    IfStatement(
                        Not(IdentifierName("setup").Dot(IdentifierName("IsUsed")))
                            .And(Not(IdentifierName("setup").Dot(IdentifierName("IsLenient"))))
                            .And(
                                IdentifierName("setup")
                                    .Dot(
                                        IdentifierName(
                                            IndexerGetterImposterMetadata
                                                .GetterInvocationMetadata
                                                .OutcomeDescriptionsFieldName
                                        )
                                    )
                                    .IsNotNull()
                            ),
                        YieldStatement(
                            SyntaxKind.YieldReturnStatement,
                            IdentifierName("setup").Dot(IdentifierName("ToString")).Call()
                        )
                    )
                ),
            ];
        if (getter is false)
        {
            var setter = indexer.SetterImplementation;
            var statements = new List<StatementSyntax>
            {
                UnusedSetter(indexer, setter.CallbacksField, false),
            };
            if (setter.BaseImplementationCriteriaField is { } baseField)
                statements.Add(UnusedSetter(indexer, baseField, true));
            return statements.ToArray();
        }
        var result = new List<StatementSyntax>();
        foreach (
            var field in new[]
            {
                indexer.Core.HasGetter ? indexer.Builder.GetterImposterField.Name : null,
                indexer.Core.HasSetter ? indexer.Builder.SetterImposterField.Name : null,
            }
        )
            if (field is not null)
                result.Add(
                    ForEachStatement(
                        Var,
                        Identifier("unused"),
                        ParenthesizedExpression(
                                CastExpression(
                                    WellKnownTypes
                                        .Imposter
                                        .Abstractions
                                        .IImposterVerificationSource,
                                    IdentifierName(field)
                                )
                            )
                            .Dot(IdentifierName("UnusedSetups"))
                            .Call(),
                        YieldStatement(SyntaxKind.YieldReturnStatement, IdentifierName("unused"))
                    )
                );
        return result.ToArray();
    }

    private static IfStatementSyntax UnusedSetter(
        in ImposterIndexerMetadata indexer,
        in FieldMetadata field,
        bool isBase
    )
    {
        var setup = IdentifierName("setup");
        ExpressionSyntax criteria = setup.Dot(IdentifierName("Value"));
        if (!isBase)
            criteria = criteria.Dot(IdentifierName("Criteria"));
        return IfStatement(
            IdentifierName(field.Name).IsNotNull(),
            ForEachStatement(
                Var,
                Identifier("setup"),
                IdentifierName(field.Name),
                IfStatement(
                    Not(criteria.Dot(IdentifierName(indexer.ArgumentsCriteria.SetterUsedFieldName)))
                        .And(
                            Not(
                                criteria.Dot(
                                    IdentifierName(indexer.ArgumentsCriteria.SetterLenientFieldName)
                                )
                            )
                        ),
                    YieldStatement(
                        SyntaxKind.YieldReturnStatement,
                        ("set " + indexer.Core.DisplayName)
                            .StringLiteral()
                            .Add(ExpectedIndices(indexer, criteria))
                            .Add(
                                (
                                    isBase ? " -> UseBaseImplementation" : " -> Callback"
                                ).StringLiteral()
                            )
                    )
                )
            )
        );
    }
}
