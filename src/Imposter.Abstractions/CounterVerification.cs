using System.ComponentModel;

namespace Imposter.Abstractions;

/// <summary>Counter-only verification for accessors whose values cannot be retained.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class CounterVerification
{
    private int _count;
    private int _covered;
    private object? _gate;
    private volatile bool _configured;
    private volatile bool _used;
    private volatile bool _lenient;

    /// <summary>Records an interaction without allocating.</summary>
    public void Record() => Interlocked.Increment(ref _count);

    /// <summary>Marks the accessor setup as configured.</summary>
    public void Configure() => _configured = true;

    /// <summary>Marks the accessor setup as used.</summary>
    public void Use()
    {
        if (_configured)
            _used = true;
    }

    /// <summary>Excludes the accessor setup from unused-setup verification.</summary>
    public void Lenient() => _lenient = true;

    private object Gate => LazyInitializer.EnsureInitialized(ref _gate)!;

    /// <summary>Verifies and covers the number of interactions observed by this check.</summary>
    /// <param name="expected">The expected count.</param>
    /// <param name="member">The accessor description.</param>
    /// <param name="because">An optional reason for the expectation.</param>
    /// <param name="owner">The owning imposter, used to describe other interactions on failure.</param>
    public void Verify(
        Count expected,
        string member,
        string? because = null,
        IImposter? owner = null
    )
    {
        lock (Gate)
        {
            var count = Volatile.Read(ref _count);
            if (!expected.Matches(count))
                throw new VerificationFailedException(
                    member,
                    member,
                    expected,
                    count,
                    count == 0 ? null : count + " interaction(s)",
                    because,
                    count == 0 && owner is not null
                        ? DiagnosticValue.OtherInteractions(owner)
                        : null
                );
            _covered = Math.Max(_covered, count);
        }
    }

    /// <summary>Clears history and coverage without affecting configured setup usage.</summary>
    public void Clear()
    {
        lock (Gate)
        {
            Interlocked.Exchange(ref _count, 0);
            _covered = 0;
        }
    }

    /// <summary>Describes recorded interactions.</summary>
    /// <param name="member">The accessor description.</param>
    public IEnumerable<string> Interactions(string member)
    {
        var count = Volatile.Read(ref _count);
        if (count > 0)
            yield return member + ": " + count + " interaction(s)";
    }

    /// <summary>Describes interactions not covered by a successful check.</summary>
    /// <param name="member">The accessor description.</param>
    public IEnumerable<string> UncoveredInteractions(string member)
    {
        int count;
        lock (Gate)
            count = Volatile.Read(ref _count) - _covered;
        if (count > 0)
            yield return member + ": " + count + " uncovered interaction(s)";
    }

    /// <summary>Describes an unused configured setup unless it is lenient.</summary>
    /// <param name="member">The accessor description.</param>
    public IEnumerable<string> UnusedSetups(string member)
    {
        if (_configured && !_used && !_lenient)
            yield return member;
    }
}
