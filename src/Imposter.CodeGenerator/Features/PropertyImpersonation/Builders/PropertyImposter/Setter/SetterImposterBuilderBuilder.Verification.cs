using Imposter.CodeGenerator.Features.PropertyImpersonation.Metadata;
using Imposter.CodeGenerator.SyntaxHelpers;
using Imposter.CodeGenerator.SyntaxHelpers.Builders;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Imposter.CodeGenerator.SyntaxHelpers.SyntaxFactoryHelper;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Imposter.CodeGenerator.Features.PropertyImpersonation.Builders.PropertyImposter.Setter;

internal static partial class SetterImposterBuilderBuilder
{
    private static MemberDeclarationSyntax[] BuildSharedVerificationMembers(
        in ImposterPropertyMetadata property
    )
    {
        var step = WellKnownTypes.Imposter.Abstractions.IInvocationStep;
        var invocation = WellKnownTypes.Imposter.Abstractions.IImposterInvocation;
        var setter = IdentifierName(property.SetterImposter.Builder.SetterImposterField.Name);
        var criteria = IdentifierName(property.SetterImposter.Builder.RuntimeCriteriaField.Name);
        var record = property.SetterImposter.InvocationTypeSyntax;
        var arguments = property.Core.IsPassedThrough
            ? ArgumentList()
            : criteria.ToSingleArgumentList();
        ExpressionSyntax description = property.Core.IsPassedThrough
            ? ("set " + property.Core.Name + "(value: <not captured>)").StringLiteral()
            : ("set " + property.Core.Name + "(value: ")
                .StringLiteral()
                .Add(criteria.Dot(IdentifierName("ToString")).Call())
                .Add(")".StringLiteral());
        return
        [
            new PropertyDeclarationBuilder(
                WellKnownTypes.System.Collections.Generic.IReadOnlyList(record),
                "Invocations"
            )
                .WithExplicitInterfaceSpecifier(
                    ExplicitInterfaceSpecifier(
                        property.SetterImposterBuilderInterface.VerificationInterfaceTypeSyntax
                    )
                )
                .WithGetterBody(
                    Block(
                        ReturnStatement(
                            setter
                                .Dot(
                                    GenericName("MatchingInvocations")
                                        .WithTypeArgumentList(
                                            TypeArgumentList(
                                                SingletonSeparatedList<TypeSyntax>(record)
                                            )
                                        )
                                )
                                .Call(arguments)
                        )
                    )
                )
                .Build(),
            new PropertyDeclarationBuilder(
                WellKnownTypes.Imposter.Abstractions.IImposter,
                "Imposter"
            )
                .WithExplicitInterfaceSpecifier(ExplicitInterfaceSpecifier(step))
                .WithGetterBody(Block(ReturnStatement(setter.Dot(IdentifierName("Owner")))))
                .Build(),
            new PropertyDeclarationBuilder(WellKnownTypes.String, "Description")
                .WithExplicitInterfaceSpecifier(ExplicitInterfaceSpecifier(step))
                .WithGetterBody(Block(ReturnStatement(description)))
                .Build(),
            new MethodDeclarationBuilder(
                WellKnownTypes.System.Collections.Generic.IReadOnlyList(invocation),
                "MatchingInvocations"
            )
                .WithExplicitInterfaceSpecifier(step)
                .WithBody(
                    Block(
                        ReturnStatement(
                            setter
                                .Dot(
                                    GenericName("MatchingInvocations")
                                        .WithTypeArgumentList(
                                            TypeArgumentList(
                                                SingletonSeparatedList<TypeSyntax>(invocation)
                                            )
                                        )
                                )
                                .Call(arguments)
                        )
                    )
                )
                .Build(),
            new MethodDeclarationBuilder(
                property.SetterImposterBuilderInterface.ContinuationInterfaceTypeSyntax,
                "Lenient"
            )
                .WithExplicitInterfaceSpecifier(
                    property.SetterImposterBuilderInterface.ContinuationInterfaceTypeSyntax
                )
                .WithBody(
                    Block(
                        IdentifierName(property.SetterImposter.Builder.SetupField.Name)
                            .Dot(IdentifierName("Lenient"))
                            .Call()
                            .ToStatementSyntax(),
                        ReturnThis
                    )
                )
                .Build(),
        ];
    }
}
