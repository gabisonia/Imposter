using Imposter.CodeGenerator.Features.EventImpersonation.Metadata;
using Imposter.CodeGenerator.SyntaxHelpers;
using Imposter.CodeGenerator.SyntaxHelpers.Builders;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Imposter.CodeGenerator.SyntaxHelpers.SyntaxFactoryHelper;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Imposter.CodeGenerator.Features.EventImpersonation.Builders;

internal static class EventHistoryBuilder
{
    internal static PropertyDeclarationSyntax Build(
        in ImposterEventMetadata @event,
        bool subscription,
        bool implementation
    )
    {
        var recordType = WellKnownTypes.Imposter.Abstractions.EventSubscriptionInvocation(
            @event.Core.HandlerTypeSyntax
        );
        var resultType = WellKnownTypes.System.Collections.Generic.IReadOnlyList(recordType);
        var builder = new PropertyDeclarationBuilder(
            resultType,
            subscription ? "Subscriptions" : "Unsubscriptions"
        );
        if (!implementation)
            return builder.WithGetter().Build();
        var field = subscription
            ? @event.Builder.Fields.SubscribeHistory
            : @event.Builder.Fields.UnsubscribeHistory;
        var history = IdentifierName(field.Name);
        return builder
            .WithExplicitInterfaceSpecifier(
                ExplicitInterfaceSpecifier(@event.BuilderInterface.VerificationInterfaceTypeSyntax)
            )
            .WithGetterBody(
                Block(
                    ReturnStatement(
                        ConditionalExpression(
                            history.IsNull(),
                            WellKnownTypes
                                .Imposter.Abstractions.InvocationSnapshots.Dot(
                                    IdentifierName("Order")
                                )
                                .Call(
                                    Argument(
                                        WellKnownTypes
                                            .System.Collections.Generic.List(recordType)
                                            .New()
                                    )
                                ),
                            history.Dot(IdentifierName("Snapshot")).Call()
                        )
                    )
                )
            )
            .Build();
    }
}
