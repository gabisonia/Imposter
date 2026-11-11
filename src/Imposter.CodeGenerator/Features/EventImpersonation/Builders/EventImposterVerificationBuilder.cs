using System;
using System.Collections.Generic;
using System.Linq;
using Imposter.CodeGenerator.Features.EventImpersonation.Metadata;
using Imposter.CodeGenerator.SyntaxHelpers;
using Imposter.CodeGenerator.SyntaxHelpers.Builders;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Imposter.CodeGenerator.Features.Shared.Builders.FormatValueMethodBuilder;
using static Imposter.CodeGenerator.Features.Shared.Builders.VerificationFailedBuilder;
using static Imposter.CodeGenerator.SyntaxHelpers.SyntaxFactoryHelper;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Imposter.CodeGenerator.Features.EventImpersonation.Builders;

internal static class EventImposterVerificationBuilder
{
    // A history entry, as the verification predicates and failure-only descriptions read it.
    private static readonly IdentifierNameSyntax Entry = IdentifierName("entry");

    internal static MethodDeclarationSyntax BuildSubscribedVerificationMethod(
        in ImposterEventMetadata @event
    ) =>
        BuildSubscriptionVerificationMethod(
            @event,
            @event.Builder.Methods.Subscribed,
            @event.Builder.Fields.SubscribeHistory,
            "subscribed"
        );

    internal static MethodDeclarationSyntax BuildUnsubscribedVerificationMethod(
        in ImposterEventMetadata @event
    ) =>
        BuildSubscriptionVerificationMethod(
            @event,
            @event.Builder.Methods.Unsubscribed,
            @event.Builder.Fields.UnsubscribeHistory,
            "unsubscribed"
        );

    private static MethodDeclarationSyntax BuildSubscriptionVerificationMethod(
        in ImposterEventMetadata @event,
        in CriteriaMethodMetadata method,
        in FieldMetadata history,
        string action
    )
    {
        var criteriaName = method.CriteriaParameter.Name;
        var eventName = @event.Core.Name;

        return new MethodDeclarationBuilder(
            @event.BuilderInterface.VerificationInterfaceTypeSyntax,
            method.Name
        )
            .WithExplicitInterfaceSpecifier(@event.BuilderInterface.VerificationInterfaceTypeSyntax)
            .AddParameter(ParameterSyntax(method.CriteriaParameter))
            .AddParameter(ParameterSyntax(@event.Builder.Methods.CountParameter))
            .WithBody(BuildSubscriptionVerificationBody(@event, method, history, eventName, action))
            .Build();
    }

    private static BlockSyntax BuildSubscriptionVerificationBody(
        in ImposterEventMetadata @event,
        in CriteriaMethodMetadata method,
        in FieldMetadata historyField,
        string eventName,
        string action
    )
    {
        var history = IdentifierName(historyField.Name);
        var snapshot = IdentifierName("snapshot");
        var cursor = IdentifierName("cursor");
        var actual = IdentifierName("actual");
        var count = IdentifierName(@event.Builder.Methods.CountParameter.Name);
        var predicate = Matches(
            method.CriteriaParameter.Name,
            Entry.Dot(IdentifierName("Handler"))
        );
        var loopCount = WhileStatement(
            cursor.Dot(IdentifierName("MoveNext")).Call(),
            Block(
                LocalVariableDeclarationSyntax(
                    Var,
                    Entry.Identifier.Text,
                    cursor.Dot(IdentifierName("Current"))
                ),
                IfStatement(
                    predicate,
                    PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, actual)
                        .ToStatementSyntax()
                )
            )
        );
        var loopCover = WhileStatement(
            cursor.Dot(IdentifierName("MoveNext")).Call(),
            Block(
                LocalVariableDeclarationSyntax(
                    Var,
                    Entry.Identifier.Text,
                    cursor.Dot(IdentifierName("Current"))
                ),
                IfStatement(
                    predicate,
                    Entry.Dot(IdentifierName("MarkVerified")).Call().ToStatementSyntax()
                )
            )
        );
        return Block(
            ThrowIfNull(method.CriteriaParameter.Name),
            ThrowIfNull(@event.Builder.Methods.CountParameter.Name),
            LocalVariableDeclarationSyntax(Var, "actual", NumericLiteral(0)),
            LocalVariableDeclarationSyntax(
                Var,
                "snapshot",
                ConditionalExpression(
                    history.IsNull(),
                    DefaultExpression(
                        QualifiedName((NameSyntax)historyField.Type, IdentifierName("Enumerator"))
                    ),
                    history.Dot(IdentifierName("GetEnumerator")).Call()
                )
            ),
            LocalVariableDeclarationSyntax(Var, "cursor", snapshot),
            loopCount,
            IfStatement(
                CountDoesNotMatch(count, actual),
                BuildHistoryFailureBody(
                    historyField,
                    handler =>
                        BuildSubscriptionDescription(
                            eventName,
                            action,
                            handler.Dot(IdentifierName("Handler"))
                        ),
                    @event.Builder.Methods.CountParameter.Name,
                    eventName,
                    Expected(eventName, action, [method.CriteriaParameter.Name])
                )
            ),
            cursor.Assign(snapshot).ToStatementSyntax(),
            loopCover,
            ReturnThis
        );
    }

    internal static MethodDeclarationSyntax BuildRaisedVerificationMethod(
        in ImposterEventMetadata @event
    )
    {
        var eventName = @event.Core.Name;
        var parameters = @event.Core.MatchedParameters;
        var historyEntry = @event.Builder.Fields.HistoryEntry;

        return new MethodDeclarationBuilder(
            @event.BuilderInterface.VerificationInterfaceTypeSyntax,
            @event.Builder.Methods.RaisedVerification.Name
        )
            .WithExplicitInterfaceSpecifier(@event.BuilderInterface.VerificationInterfaceTypeSyntax)
            .AddParameters(
                @event.Builder.Methods.RaisedCriteriaParameters.Select(criteria =>
                    ParameterSyntax(criteria)
                )
            )
            .AddParameter(ParameterSyntax(@event.Builder.Methods.CountParameter))
            .WithBody(
                BuildHistoryVerificationBody(
                    @event,
                    historyField: @event.Builder.Fields.History,
                    criteriaParameterNames: @event.Builder.Methods.RaisedCriteriaParameters.Select(
                        criteria => criteria.Name
                    ),
                    predicate: BuildRaisedPredicate(@event),
                    descriptionFactory: entry =>
                        BuildRaisedDescription(eventName, parameters, historyEntry, entry)
                )
            )
            .Build();
    }

    private static ExpressionSyntax BuildRaisedPredicate(in ImposterEventMetadata @event)
    {
        var historyEntry = @event.Builder.Fields.HistoryEntry;
        var criteria = @event.Builder.Methods.RaisedCriteriaParameters;
        return @event
            .Core.MatchedParameters.Select(
                (parameter, index) =>
                    (ExpressionSyntax)Matches(
                        criteria[index].Name,
                        historyEntry.ParameterValue(Entry, parameter)
                    )
            )
            .DefaultIfEmpty(True)
            .Aggregate((current, next) => current.And(next));
    }

    internal static MethodDeclarationSyntax BuildHandlerInvokedVerificationMethod(
        in ImposterEventMetadata @event
    )
    {
        var method = @event.Builder.Methods.HandlerInvoked;
        var criteriaName = method.HandlerCriteriaParameter.Name;
        var eventName = @event.Core.Name;
        var parameters = @event.Core.MatchedParameters;
        var handlerInvocationEntry = @event.Builder.Fields.HandlerInvocationEntry;

        return new MethodDeclarationBuilder(
            @event.BuilderInterface.VerificationInterfaceTypeSyntax,
            method.Name
        )
            .WithExplicitInterfaceSpecifier(@event.BuilderInterface.VerificationInterfaceTypeSyntax)
            .AddParameter(ParameterSyntax(method.HandlerCriteriaParameter))
            .AddParameter(ParameterSyntax(@event.Builder.Methods.CountParameter))
            .WithBody(
                BuildHistoryVerificationBody(
                    @event,
                    historyField: @event.Builder.Fields.HandlerInvocations,
                    criteriaParameterNames: [criteriaName],
                    predicate: Matches(criteriaName, handlerInvocationEntry.Handler(Entry)),
                    descriptionFactory: entry =>
                        BuildHandlerInvocationDescription(
                            eventName,
                            parameters,
                            handlerInvocationEntry,
                            entry
                        )
                )
            )
            .Build();
    }

    // The predicate and the description read the history entry as Entry.
    private static BlockSyntax BuildHistoryVerificationBody(
        in ImposterEventMetadata @event,
        in FieldMetadata historyField,
        IEnumerable<string> criteriaParameterNames,
        ExpressionSyntax predicate,
        Func<ExpressionSyntax, ExpressionSyntax> descriptionFactory
    )
    {
        var countParameterName = @event.Builder.Methods.CountParameter.Name;

        var blockBuilder = new BlockBuilder();
        foreach (var criteriaParameterName in criteriaParameterNames)
        {
            blockBuilder.AddStatement(ThrowIfNull(criteriaParameterName));
        }

        blockBuilder.AddStatement(ThrowIfNull(countParameterName));

        AddEnsureCountMatchesStatements(
            blockBuilder,
            countParameterName,
            IdentifierName(historyField.Name),
            predicate,
            BuildHistoryFailureBody(
                historyField,
                descriptionFactory,
                countParameterName,
                @event.Core.Name,
                Expected(
                    @event.Core.Name,
                    historyField.Name == @event.Builder.Fields.History.Name
                        ? "raised"
                        : "handler invoked",
                    criteriaParameterNames
                )
            )
        );

        blockBuilder.AddStatement(ReturnThis);

        return blockBuilder.Build();
    }

    private static InvocationExpressionSyntax Matches(string criteria, ExpressionSyntax value) =>
        IdentifierName(criteria).Dot(IdentifierName("Matches")).Call(Argument(value));

    private static void AddEnsureCountMatchesStatements(
        BlockBuilder blockBuilder,
        string countParameterName,
        ExpressionSyntax history,
        ExpressionSyntax predicate,
        BlockSyntax failureBody
    )
    {
        var actual = IdentifierName("actual");
        var expected = IdentifierName(countParameterName);
        blockBuilder.AddStatement(
            LocalVariableDeclarationSyntax(
                WellKnownTypes.Int,
                actual.Identifier.Text,
                NumericLiteral(0)
            )
        );
        blockBuilder.AddStatement(
            LazyCollectionSyntaxHelper.UnlessNull(
                ForEachStatement(
                    Var,
                    Entry.Identifier,
                    history,
                    Block(
                        IfStatement(
                            predicate,
                            Block(
                                PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, actual)
                                    .ToStatementSyntax()
                            )
                        )
                    )
                )
            )
        );

        // Build descriptions only on failure; successful verification needs no predicate or closure allocation.
        blockBuilder.AddStatement(IfStatement(CountDoesNotMatch(expected, actual), failureBody));
    }

    private static BlockSyntax BuildHistoryFailureBody(
        in FieldMetadata historyField,
        Func<ExpressionSyntax, ExpressionSyntax> descriptionFactory,
        string countParameterName,
        string member,
        ExpressionSyntax expected
    )
    {
        var stringListType = WellKnownTypes.System.Collections.Generic.List(WellKnownTypes.String);
        var performedInvocations = IdentifierName("performedInvocations");

        return Block(
            LocalVariableDeclarationSyntax(
                Var,
                performedInvocations.Identifier.Text,
                stringListType.New()
            ),
            LazyCollectionSyntaxHelper.UnlessNull(
                ForEachStatement(
                    Var,
                    Entry.Identifier,
                    IdentifierName(historyField.Name),
                    Block(
                        IfStatement(
                            True,
                            Block(
                                performedInvocations
                                    .Dot(IdentifierName("Add"))
                                    .Call(Argument(descriptionFactory(Entry)))
                                    .ToStatementSyntax()
                            )
                        )
                    )
                )
            ),
            performedInvocations.Dot(IdentifierName("Reverse")).Call().ToStatementSyntax(),
            ThrowStatement(
                WellKnownTypes.Imposter.Abstractions.VerificationFailedException.New(
                    ArgumentListSyntax([
                        Argument(member.StringLiteral()),
                        Argument(expected),
                        Argument(IdentifierName(countParameterName)),
                        Argument(IdentifierName("actual")),
                        Argument(JoinWithNewLines(performedInvocations)),
                        Argument(Null),
                        Argument(
                            ConditionalExpression(
                                BinaryExpression(
                                    SyntaxKind.EqualsExpression,
                                    performedInvocations.Dot(IdentifierName("Count")),
                                    NumericLiteral(0)
                                ),
                                WellKnownTypes
                                    .Imposter.Abstractions.DiagnosticValue.Dot(
                                        IdentifierName("OtherInteractions")
                                    )
                                    .Call(Argument(IdentifierName("Owner"))),
                                Null
                            )
                        ),
                    ])
                )
            )
        );
    }

    private static ExpressionSyntax Expected(
        string member,
        string action,
        IEnumerable<string> criteria
    )
    {
        ExpressionSyntax result = BuildActionDescription(member, action);
        foreach (var name in criteria)
            result = result
                .Add(" ".StringLiteral())
                .Add(IdentifierName(name).Dot(IdentifierName("ToString")).Call());
        return result;
    }

    internal static MethodDeclarationSyntax WithReason(MethodDeclarationSyntax method)
    {
        var result = method.AddParameterListParameters(
            ParameterSyntax(WellKnownTypes.String.ToNullableType(), "because")
        );
        var failures = result
            .DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Where(node =>
                node.Type.IsEquivalentTo(
                    WellKnownTypes.Imposter.Abstractions.VerificationFailedException
                )
            )
            .ToArray();
        return result.ReplaceNodes(
            failures,
            (original, _) =>
                original.WithArgumentList(
                    original.ArgumentList!.WithArguments(
                        original.ArgumentList.Arguments.Replace(
                            original.ArgumentList.Arguments[5],
                            Argument(IdentifierName("because"))
                        )
                    )
                )
        );
    }

    private static BinaryExpressionSyntax BuildSubscriptionDescription(
        string eventName,
        string action,
        ExpressionSyntax handlerExpression
    )
    {
        var description = BuildActionDescription(eventName, action);
        return AppendDetail(description, "handler", handlerExpression);
    }

    private static ExpressionSyntax BuildHandlerInvocationDescription(
        string eventName,
        EventParameterMetadata[] parameters,
        in EventHandlerInvocationEntryMetadata handlerInvocationEntry,
        ExpressionSyntax entry
    )
    {
        ExpressionSyntax description = BuildActionDescription(eventName, "handler invoked");
        description = AppendDetail(description, "handler", handlerInvocationEntry.Handler(entry));

        foreach (var parameter in parameters)
        {
            description = AppendDetail(
                description,
                parameter.Name,
                EventHandlerInvocationEntryMetadata.ParameterValue(entry, parameter)
            );
        }

        return description;
    }

    private static ExpressionSyntax BuildRaisedDescription(
        string eventName,
        EventParameterMetadata[] parameters,
        in EventHistoryEntryMetadata historyEntry,
        ExpressionSyntax entry
    )
    {
        ExpressionSyntax description = BuildActionDescription(eventName, "raised");

        foreach (var parameter in parameters)
        {
            description = AppendDetail(
                description,
                parameter.Name,
                historyEntry.ParameterValue(entry, parameter)
            );
        }

        return description;
    }

    private static LiteralExpressionSyntax BuildActionDescription(
        string eventName,
        string action
    ) => $"{eventName} {action}".StringLiteral();

    private static BinaryExpressionSyntax AppendDetail(
        ExpressionSyntax description,
        string label,
        ExpressionSyntax valueExpression
    ) => description.Add($" {label}: ".StringLiteral().Add(Invocation(valueExpression)));
}
