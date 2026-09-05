using System.Collections.Generic;
using Imposter.CodeGenerator.Features.MethodImpersonation.Metadata.ImposterTargetMethod;
using Imposter.CodeGenerator.Features.MethodImpersonation.Metadata.InvocationHistory;
using Imposter.CodeGenerator.SyntaxHelpers;
using Imposter.CodeGenerator.SyntaxHelpers.Builders;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Imposter.CodeGenerator.SyntaxHelpers.SyntaxFactoryHelper;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Imposter.CodeGenerator.Features.MethodImpersonation.Builders.InvocationHistory;

internal static class MethodCallCounterBuilder
{
    internal static ClassDeclarationSyntax Build(
        in ImposterTargetMetadata target,
        in MethodCallCounterMetadata counter
    )
    {
        var builder = new ClassDeclarationBuilder(counter.TypeName)
            .AddModifier(Token(SyntaxKind.PublicKeyword))
            .AddModifier(Token(SyntaxKind.SealedKeyword))
            .AddMember(
                SinglePrivateReadonlyVariableField(
                    target.ImposterTypeSyntax,
                    counter.ImposterFieldName
                )
            )
            .AddMember(
                new ConstructorBuilder(counter.TypeName)
                    .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword)))
                    .AddParameter(
                        ParameterSyntax(
                            target.ImposterTypeSyntax,
                            MethodCallCounterMetadata.ImposterParameterName
                        )
                    )
                    .WithBody(
                        Block(
                            ThisExpression()
                                .Dot(IdentifierName(counter.ImposterFieldName))
                                .Assign(
                                    IdentifierName(MethodCallCounterMetadata.ImposterParameterName)
                                )
                                .ToStatementSyntax()
                        )
                    )
                    .Build()
            );

        foreach (var method in target.Methods)
        {
            builder.AddMember(BuildCountMethod(method, counter));
        }

        return builder.Build();
    }

    internal static MethodDeclarationSyntax BuildAccessor(in MethodCallCounterMetadata counter) =>
        new MethodDeclarationBuilder(counter.TypeSyntax, counter.AccessorName)
            .AddModifier(Token(SyntaxKind.PublicKeyword))
            .WithBody(
                Block(
                    ReturnStatement(
                        counter.TypeSyntax.New(Argument(ThisExpression()).ToSingleArgumentList())
                    )
                )
            )
            .Build();

    private static MethodDeclarationSyntax BuildCountMethod(
        in ImposterTargetMethodMetadata method,
        in MethodCallCounterMetadata counter
    )
    {
        var arguments = new List<ArgumentSyntax>();
        if (method.Parameters.HasInputParameters)
        {
            arguments.Add(Argument(NewArgumentsCriteria(method)));
        }

        var count = ThisExpression()
            .Dot(IdentifierName(counter.ImposterFieldName))
            .Dot(IdentifierName(method.InvocationHistory.Collection.AsField.Name))
            .Dot(
                WithMethodGenericArguments(
                    InvocationHistoryCollectionCountMethodMetadata.Name,
                    method
                )
            )
            .Call(ArgumentList(SeparatedList(arguments)));

        return new MethodDeclarationBuilder(
            WellKnownTypes.Int,
            method.RequiresExplicitInterfaceImplementation ? method.UniqueName : method.Symbol.Name
        )
            .AddModifier(Token(SyntaxKind.PublicKeyword))
            .WithTypeParameters(TypeParameterListSyntax(method.Symbol))
            .AddConstraintClauses(method.GenericTypeConstraintClauses)
            .WithParameterList(ArgParameters(method.Symbol.Parameters))
            .WithBody(Block(ReturnStatement(count)))
            .Build();
    }
}
