using System.ComponentModel;

namespace Imposter.Abstractions;

/// <summary>Formats values for diagnostics without allowing a failing ToString to hide the original failure.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DiagnosticValue
{
    /// <summary>Gets a cached label for a configured exception type.</summary>
    /// <typeparam name="TException">The configured exception type.</typeparam>
    public static string Throws<TException>() => ExceptionOutcome<TException>.Description;

    private static class ExceptionOutcome<TException>
    {
        internal static readonly string Description = "Throws<" + typeof(TException).Name + ">";
    }

    /// <summary>Describes other recorded interactions when a verified member has no history.</summary>
    /// <param name="imposter">The owner of the recorded history.</param>
    public static string OtherInteractions(IImposter imposter)
    {
        var invocations = imposter.Invocations();
        if (invocations.Count == 0)
            return string.Empty;
        var entries = new List<string>();
        foreach (var invocation in invocations)
            entries.Add("  " + invocation.ToString());
        return "Other interactions on "
            + imposter.GetType().Name
            + ":"
            + Environment.NewLine
            + string.Join(Environment.NewLine, entries);
    }

    /// <summary>Formats stored outcome kinds without evaluating configured behavior.</summary>
    /// <param name="descriptions">The descriptions retained when the setup was configured.</param>
    public static string Outcomes(object? descriptions)
    {
        if (descriptions is null)
            return string.Empty;
        return descriptions is string single ? single : string.Join(", ", (string[])descriptions);
    }

    /// <summary>Retains outcome labels without allocating storage for a single outcome.</summary>
    /// <param name="descriptions">The immutable label or label array.</param>
    /// <param name="description">The outcome label.</param>
    public static void AddOutcome(ref object? descriptions, string description)
    {
        object? snapshot;
        object updated;
        do
        {
            snapshot = Volatile.Read(ref descriptions);
            if (snapshot is null)
                updated = description;
            else if (snapshot is string single)
                updated = new[] { single, description };
            else
            {
                var previous = (string[])snapshot;
                var entries = new string[previous.Length + 1];
                Array.Copy(previous, entries, previous.Length);
                entries[previous.Length] = description;
                updated = entries;
            }
        } while (
            !ReferenceEquals(
                Interlocked.CompareExchange(ref descriptions, updated, snapshot),
                snapshot
            )
        );
    }

    /// <summary>Formats a value, including null, inside angle brackets.</summary>
    /// <param name="value">The value to describe.</param>
    public static string Format(object? value)
    {
        try
        {
            return "<" + (value?.ToString() ?? "null") + ">";
        }
        catch (Exception)
        {
            return "<unprintable " + value!.GetType().Name + ">";
        }
    }

    internal static string Sequence<T>(IEnumerable<T> values) =>
        "[" + string.Join(", ", values.Select(value => Format(value))) + "]";
}
