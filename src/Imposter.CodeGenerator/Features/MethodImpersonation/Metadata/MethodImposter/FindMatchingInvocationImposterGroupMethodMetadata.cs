using Imposter.CodeGenerator.Helpers;

namespace Imposter.CodeGenerator.Features.MethodImpersonation.Metadata.MethodImposter;

internal readonly struct FindMatchingInvocationImposterGroupMethodMetadata
{
    internal readonly string Name;

    internal readonly string GroupVariableName;

    internal readonly string BareMatchVariableName;

    public FindMatchingInvocationImposterGroupMethodMetadata(
        in ReservedParameterNames reservedParameterNames,
        NameSet memberNames
    )
    {
        Name = memberNames.Use("FindMatchingInvocationImposterGroup");
        var nameContext = reservedParameterNames.CreateNameSet();
        GroupVariableName = nameContext.Use("invocationImposterGroup");
        BareMatchVariableName = nameContext.Use("bareMatch");
    }
}
