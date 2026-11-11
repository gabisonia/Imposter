using System.ComponentModel;

namespace Imposter.Abstractions;

/// <summary>Orders materialized invocation snapshots for generated imposters.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class InvocationSnapshots
{
    /// <summary>Copies a captured span array without exposing the copy to callback mutation.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="values">The captured array, or null when an invocation failed before returning a span.</param>
    public static T[] CopySpan<T>(T[]? values) =>
        values is null ? Array.Empty<T>() : (T[])values.Clone();

    /// <summary>Combines two optional histories into an independent snapshot.</summary>
    /// <param name="first">The first history.</param>
    /// <param name="second">The second history.</param>
    public static IReadOnlyList<IImposterInvocation> Combine(
        IEnumerable<IImposterInvocation>? first,
        IEnumerable<IImposterInvocation>? second
    )
    {
        var entries = new List<IImposterInvocation>();
        if (first is not null)
            entries.AddRange(first);
        if (second is not null)
            entries.AddRange(second);
        return Order(entries);
    }

    /// <summary>Sorts the supplied snapshot by entry stamp and exposes it as read-only.</summary>
    /// <typeparam name="T">The invocation record type.</typeparam>
    /// <param name="invocations">A newly materialized list owned by the caller.</param>
    public static IReadOnlyList<T> Order<T>(List<T> invocations)
        where T : IImposterInvocation
    {
        invocations.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
        return invocations.AsReadOnly();
    }
}
