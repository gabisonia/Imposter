using System.ComponentModel;

namespace Imposter.Abstractions;

/// <summary>A generated verification builder that can participate in ordered verification.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IInvocationStep
{
    /// <summary>The imposter whose interactions this step verifies.</summary>
    IImposter Imposter { get; }

    /// <summary>A description of this step and its argument matchers.</summary>
    string Description { get; }

    /// <summary>Returns a snapshot of the invocations matching this step.</summary>
    IReadOnlyList<IImposterInvocation> MatchingInvocations();
}
