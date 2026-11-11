using System.Collections.Generic;
using Imposter.CodeGenerator.Features.MethodImpersonation.Metadata.ImposterTargetMethod;
using Imposter.CodeGenerator.Features.MethodImpersonation.Metadata.MethodImposter;
using Imposter.CodeGenerator.SyntaxHelpers;
using Imposter.CodeGenerator.SyntaxHelpers.Builders;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Imposter.CodeGenerator.SyntaxHelpers.SyntaxFactoryHelper;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Imposter.CodeGenerator.Features.MethodImpersonation.Builders.MethodImposter;

internal static class MethodVerificationSourceBuilder
{
    internal static MemberDeclarationSyntax[] Build(
        in ImposterTargetMethodMetadata method,
        bool collection
    )
    {
        var source = WellKnownTypes.Imposter.Abstractions.IImposterVerificationSource;
        var resultType = WellKnownTypes.System.Collections.Generic.IEnumerable(
            WellKnownTypes.String
        );
        var members = new List<MemberDeclarationSyntax>();
        foreach (var name in new[] { "Interactions", "UncoveredInteractions" })
            members.Add(
                new MethodDeclarationBuilder(resultType, name)
                    .WithExplicitInterfaceSpecifier(source)
                    .WithBody(
                        Block(
                            ReturnStatement(
                                WellKnownTypes
                                    .Imposter.Abstractions.ImposterVerification.Dot(
                                        IdentifierName("Describe")
                                    )
                                    .Call([
                                        Argument(
                                            IdentifierName(
                                                    method.InvocationHistory.Collection.AsField.Name
                                                )
                                                .Dot(IdentifierName("Snapshot"))
                                                .Call()
                                        ),
                                        Argument(name == "UncoveredInteractions" ? True : False),
                                    ])
                            )
                        )
                    )
                    .Build()
            );

        var findings = IdentifierName("findings");
        var item = IdentifierName("item");
        var listType = WellKnownTypes.System.Collections.Generic.List(WellKnownTypes.String);
        var body = new BlockBuilder().AddStatement(
            LocalVariableDeclarationSyntax(Var, "findings", listType.New())
        );
        if (collection)
        {
            body.AddStatement(
                ForEachStatement(
                    Var,
                    item.Identifier,
                    IdentifierName(MethodImposterCollectionMetadata.ImpostersFieldName),
                    findings
                        .Dot(IdentifierName("AddRange"))
                        .Call(Argument(item.Dot(IdentifierName("UnusedSetups")).Call()))
                        .ToStatementSyntax()
                )
            );
        }
        else
        {
            body.AddStatement(
                ForEachStatement(
                    Var,
                    item.Identifier,
                    IdentifierName(method.MethodImposter.InvocationImposterGroupsField.Name),
                    IfStatement(
                        item.Dot(
                                IdentifierName(
                                    method
                                        .MethodInvocationImposterGroup
                                        .HasConfiguredOutcomeFieldName
                                )
                            )
                            .And(
                                Not(
                                    item.Dot(
                                        IdentifierName(
                                            method.MethodInvocationImposterGroup.IsUsedFieldName
                                        )
                                    )
                                )
                            )
                            .And(
                                Not(
                                    item.Dot(
                                        IdentifierName(
                                            method.MethodInvocationImposterGroup.IsLenientFieldName
                                        )
                                    )
                                )
                            ),
                        findings
                            .Dot(IdentifierName("Add"))
                            .Call(Argument(item.Dot(IdentifierName("ToString")).Call()))
                            .ToStatementSyntax()
                    )
                )
            );
        }
        body.AddStatement(ReturnStatement(findings));
        members.Add(
            new MethodDeclarationBuilder(resultType, "UnusedSetups")
                .WithExplicitInterfaceSpecifier(source)
                .WithBody(body.Build())
                .Build()
        );
        return members.ToArray();
    }
}
