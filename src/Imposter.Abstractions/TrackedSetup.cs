using System.ComponentModel;

namespace Imposter.Abstractions;

/// <summary>Retains an accessor setup and whether it executed.</summary>
/// <typeparam name="T">The setup registration.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class TrackedSetup<T>
{
    private volatile bool _isUsed;

    /// <summary>Creates a tracked registration.</summary>
    /// <param name="value">The setup registration.</param>
    public TrackedSetup(T value) => Value = value;

    /// <summary>Gets the registration.</summary>
    public T Value { get; }

    /// <summary>Gets whether this setup was executed.</summary>
    public bool IsUsed => _isUsed;

    /// <summary>Marks this setup as executed, including when it throws.</summary>
    public void MarkUsed() => _isUsed = true;
}
