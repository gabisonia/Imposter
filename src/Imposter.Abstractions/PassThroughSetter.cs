using System.ComponentModel;

namespace Imposter.Abstractions;

/// <summary>Tracks one setup chain for an accessor whose value cannot be retained.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class SetterSetup
{
    internal int Registered;
    internal int Used;
    internal bool UsesBase;
    internal bool IsLenient;

    /// <summary>Excludes this setup chain from unused setup verification.</summary>
    public void Lenient() => IsLenient = true;

    /// <summary>Marks this setup chain as executed.</summary>
    public void MarkUsed() => Interlocked.Exchange(ref Used, 1);
}

/// <summary>Stores verification state for setters with values that cannot be retained.</summary>
/// <typeparam name="TInvocation">The generated invocation record without a retained value.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class PassThroughSetter<TInvocation> : IImposterVerificationSource
    where TInvocation : InvocationRecord
{
    private InvocationHistory<TInvocation>? _history;
    private SetupCollection<SetterSetup>? _setups;
    private readonly ImposterMode _mode;
    private readonly string _member;
    private bool _configured;
    private bool _useBase;

    /// <summary>Creates setter state.</summary>
    protected PassThroughSetter(ImposterMode mode, string member)
    {
        _mode = mode;
        _member = member;
    }

    /// <summary>The owning imposter.</summary>
    public IImposter Owner { get; set; } = null!;

    /// <summary>Registers an outcome on a setup chain.</summary>
    public void Register(SetterSetup setup)
    {
        if (Interlocked.CompareExchange(ref setup.Registered, 1, 0) == 0)
            LazyInitializer.EnsureInitialized(ref _setups)!.Push(setup);
        _configured = true;
    }

    /// <summary>Configures unconditional base setter delegation.</summary>
    public void UseBaseImplementation() => UseBaseImplementation(new SetterSetup());

    /// <summary>Configures base setter delegation.</summary>
    public void UseBaseImplementation(SetterSetup setup)
    {
        setup.UsesBase = true;
        Register(setup);
        _useBase = true;
    }

    /// <summary>Checks explicit setup requirements.</summary>
    protected void EnsureConfigured()
    {
        if (_mode == ImposterMode.Explicit && !_configured)
            throw new MissingImposterException(
                _member,
                "set " + _member + "(value: <not captured>)",
                Array.Empty<string>()
            );
    }

    /// <summary>Marks base setup execution and returns whether to delegate.</summary>
    protected bool ShouldUseBase()
    {
        if (!_useBase)
            return false;
        if (_setups is not null)
            foreach (var setup in _setups)
                if (setup.UsesBase)
                    setup.MarkUsed();
        return true;
    }

    /// <summary>Creates the generated member-specific record.</summary>
    protected abstract TInvocation CreateInvocation(long sequence, Exception? exception);

    /// <summary>Records completion, including throwing calls.</summary>
    public void RecordInvocation(long sequence, Exception? exception) =>
        LazyInitializer
            .EnsureInitialized(ref _history)!
            .Push(CreateInvocation(sequence, exception));

    /// <summary>Clears history without modifying setups.</summary>
    public void ClearInvocations() => _history?.Clear();

    /// <summary>Returns a detached ordered snapshot.</summary>
    public IReadOnlyList<IImposterInvocation> Snapshot() =>
        MatchingInvocations<IImposterInvocation>();

    /// <summary>Returns a detached ordered snapshot in the requested public record type.</summary>
    public IReadOnlyList<TRecord> MatchingInvocations<TRecord>()
        where TRecord : IImposterInvocation
    {
        var result = new List<TRecord>();
        if (_history is not null)
            foreach (var record in _history)
                result.Add((TRecord)(object)record);
        return InvocationSnapshots.Order(result);
    }

    /// <summary>Verifies and covers the observed setter calls.</summary>
    public void Called(Count count, string? because = null)
    {
        var head = _history?.Head;
        var actual = 0;
        for (var record = head; record is not null; record = (TInvocation?)record.Next)
            actual++;
        if (!count.Matches(actual))
            throw new VerificationFailedException(
                _member + " (setter)",
                "set " + _member + "(value: <not captured>)",
                count,
                actual,
                actual == 0 ? null : actual + " interaction(s)",
                because,
                head is null && Owner is not null ? DiagnosticValue.OtherInteractions(Owner) : null
            );
        for (var record = head; record is not null; record = (TInvocation?)record.Next)
            record.MarkVerified();
    }

    /// <inheritdoc/>
    public IEnumerable<string> Interactions()
    {
        if (_history is not null)
            foreach (var record in _history)
                yield return Describe(record);
    }

    /// <inheritdoc/>
    public IEnumerable<string> UncoveredInteractions()
    {
        if (_history is not null)
            foreach (var record in _history)
                if (!record.IsVerified)
                    yield return Describe(record);
    }

    /// <inheritdoc/>
    public IEnumerable<string> UnusedSetups()
    {
        if (_setups is null)
            yield break;
        foreach (var setup in _setups)
            if (Volatile.Read(ref setup.Used) == 0 && !setup.IsLenient)
                yield return "set " + _member + "(value: <not captured>)";
    }

    private static string Describe(TInvocation record) =>
        "set "
        + record.Member
        + "(value: <not captured>)"
        + (
            record.Exception is null
                ? string.Empty
                : " threw " + DiagnosticValue.Format(record.Exception)
        );
}
