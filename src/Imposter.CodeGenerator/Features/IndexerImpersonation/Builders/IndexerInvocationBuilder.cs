using Imposter.CodeGenerator.Features.IndexerImpersonation.Metadata;
using Imposter.CodeGenerator.SyntaxHelpers;
using Imposter.CodeGenerator.SyntaxHelpers.Builders;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Imposter.CodeGenerator.SyntaxHelpers.SyntaxFactoryHelper;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Imposter.CodeGenerator.Features.IndexerImpersonation.Builders;

internal static class IndexerInvocationBuilder
{
    internal static ClassDeclarationSyntax? Build(in ImposterIndexerMetadata indexer, bool getter)
    {
        if (getter ? !indexer.Core.HasGetter : !indexer.Core.HasSetter)
            return null;
        var metadata = getter ? indexer.GetterRecord : indexer.SetterRecord;
        var builder = new ClassDeclarationBuilder(metadata.Name)
            .AddModifier(Token(SyntaxKind.PublicKeyword))
            .AddModifier(Token(SyntaxKind.SealedKeyword))
            .AddBaseType(SimpleBaseType(WellKnownTypes.Imposter.Abstractions.InvocationRecord))
            .AddMember(
                new PropertyDeclarationBuilder(WellKnownTypes.String, "Member")
                    .AddModifier(Token(SyntaxKind.PublicKeyword))
                    .AddModifier(Token(SyntaxKind.OverrideKeyword))
                    .WithGetterBody(
                        Block(
                            ReturnStatement(
                                (
                                    indexer.Core.UniqueName + (getter ? " (getter)" : " (setter)")
                                ).StringLiteral()
                            )
                        )
                    )
                    .Build()
            )
            .AddMember(
                SingleVariableField(
                    indexer.Arguments.TypeSyntax,
                    "Arguments",
                    TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.ReadOnlyKeyword))
                )
            );
        var constructor = new ConstructorBuilder(metadata.Name)
            .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword)))
            .AddParameter(ParameterSyntax(indexer.Arguments.TypeSyntax, "arguments"));
        var body = new BlockBuilder().AddStatement(
            IdentifierName("Arguments").Assign(IdentifierName("arguments")).ToStatementSyntax()
        );
        foreach (var parameter in indexer.Core.MatchedParameters)
        {
            TypeSyntax type = parameter.KeptTypeSyntax;
            if (parameter.Model.Span is { } span)
                type = WellKnownTypes.System.ReadOnlyMemory(
                    TypeSyntaxIncludingNullable(span.ElementType)
                );
            builder.AddMember(
                new PropertyDeclarationBuilder(type, metadata.Properties[parameter.Name])
                    .AddModifier(Token(SyntaxKind.PublicKeyword))
                    .WithGetterBody(
                        Block(
                            ReturnStatement(
                                IdentifierName("Arguments").Dot(IdentifierName(parameter.FieldName))
                            )
                        )
                    )
                    .Build()
            );
        }
        if (!indexer.Core.IsValuePassedThrough)
        {
            var name = getter ? "Result" : "Value";
            var storedType = indexer.Core.NullableAwareKeptTypeSyntax;
            builder.AddMember(
                SingleVariableField(
                    storedType,
                    "_value",
                    TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword))
                )
            );
            TypeSyntax exposedType = storedType;
            if (indexer.Core.HasSpanValue)
            {
                var array = storedType is NullableTypeSyntax nullable
                    ? (ArrayTypeSyntax)nullable.ElementType
                    : (ArrayTypeSyntax)storedType;
                exposedType = WellKnownTypes.System.ReadOnlyMemory(array.ElementType);
            }
            builder.AddMember(
                new PropertyDeclarationBuilder(exposedType, name)
                    .AddModifier(Token(SyntaxKind.PublicKeyword))
                    .WithGetterBody(Block(ReturnStatement(IdentifierName("_value"))))
                    .Build()
            );
            constructor.AddParameter(ParameterSyntax(storedType, "value"));
            body.AddStatement(
                IdentifierName("_value").Assign(IdentifierName("value")).ToStatementSyntax()
            );
        }
        constructor
            .AddParameter(ParameterSyntax(WellKnownTypes.Long, "sequence"))
            .AddParameter(
                ParameterSyntax(WellKnownTypes.System.Exception.ToNullableType(), "exception")
            )
            .WithInitializer(
                ConstructorInitializer(
                    SyntaxKind.BaseConstructorInitializer,
                    ArgumentListSyntax([
                        Argument(IdentifierName("sequence")),
                        Argument(IdentifierName("exception")),
                    ])
                )
            )
            .WithBody(body.Build());
        builder.AddMember(constructor.Build());
        ExpressionSyntax description = (getter ? "get " : "set ")
            .StringLiteral()
            .Add(IdentifierName("Member"))
            .Add("(".StringLiteral());
        var first = true;
        foreach (var parameter in indexer.Core.MatchedParameters)
        {
            description = description
                .Add(((first ? "" : ", ") + parameter.Model.Name + ": ").StringLiteral())
                .Add(
                    WellKnownTypes
                        .Imposter.Abstractions.DiagnosticValue.Dot(IdentifierName("Format"))
                        .Call(
                            Argument(
                                IdentifierName("Arguments").Dot(IdentifierName(parameter.FieldName))
                            )
                        )
                );
            first = false;
        }
        description = description.Add(")".StringLiteral());
        if (!indexer.Core.IsValuePassedThrough)
            description = description
                .Add((getter ? " => " : " = ").StringLiteral())
                .Add(
                    WellKnownTypes
                        .Imposter.Abstractions.DiagnosticValue.Dot(IdentifierName("Format"))
                        .Call(Argument(IdentifierName("_value")))
                );
        builder.AddMember(
            new MethodDeclarationBuilder(WellKnownTypes.String, "ToString")
                .AddModifier(Token(SyntaxKind.PublicKeyword))
                .AddModifier(Token(SyntaxKind.OverrideKeyword))
                .WithBody(Block(ReturnStatement(description)))
                .Build()
        );
        return builder.Build();
    }
}
