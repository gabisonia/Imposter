using System.ComponentModel;

namespace Imposter.Abstractions;

/// <summary>A recorded property assignment, including a failed assignment.</summary>
/// <typeparam name="T">The stored value type.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class PropertySetterInvocation<T> : InvocationRecord
{
    /// <summary>The assigned value.</summary>
    protected internal T StoredValue { get; }

    /// <summary>Creates a completed property assignment record.</summary>
    protected PropertySetterInvocation(T value, long sequence, Exception? exception)
        : base(sequence, exception) => StoredValue = value;

    /// <summary>Formats the assignment without invoking user callbacks.</summary>
    public override string ToString() =>
        "set "
        + Member
        + " = "
        + DiagnosticValue.Format(StoredValue)
        + (Exception is null ? string.Empty : " threw " + DiagnosticValue.Format(Exception));
}

/// <summary>Tracks the use and leniency of one property setter builder chain.</summary>
/// <typeparam name="T">The assigned value type.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class PropertySetterSetup<T>
{
    internal readonly Arg<T> Criteria;
    internal int Registered;
    internal int Used;
    internal bool UsesBase;
    private bool _lenient;
    private readonly List<string> _outcomes = new();

    /// <summary>Creates a setup for assignments matching the criteria.</summary>
    public PropertySetterSetup(Arg<T> criteria) => Criteria = criteria;

    /// <summary>Excludes this entire builder chain from unused-setup verification.</summary>
    public void Lenient() => _lenient = true;

    internal bool IsUnused => !_lenient && Volatile.Read(ref Used) == 0;

    internal void DescribeOutcome(string kind)
    {
        lock (_outcomes)
            _outcomes.Add(kind);
    }

    internal string Describe(string member)
    {
        lock (_outcomes)
            return "set " + member + "(value: " + Criteria + ") -> " + string.Join(", ", _outcomes);
    }
}

/// <summary>Implements callbacks, history and invocation for generated property setters.</summary>
/// <typeparam name="T">The property's value type.</typeparam>
/// <remarks>Public for generated code only and may change with the generator in the same package.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public class PropertySetter<T> : IImposterVerificationSource
{
    private CallbackCollection<(PropertySetterSetup<T> Setup, Action<T> Callback)>? _callbacks;
    private InvocationHistory<PropertySetterInvocation<T>>? _invocationHistory;
    private SetupCollection<PropertySetterSetup<T>>? _setups;
    private readonly PropertyValue<T>? _defaultValue;
    private readonly ImposterMode _mode;
    private readonly string _displayName;
    private bool _hasConfiguredSetter;
    private bool _useBaseImplementation;

    /// <summary>The owning imposter, assigned by generated initialization.</summary>
    public IImposter Owner { get; set; } = null!;

    /// <summary>Creates the setter state.</summary>
    protected PropertySetter(PropertyValue<T>? defaultValue, ImposterMode mode, string displayName)
    {
        _defaultValue = defaultValue;
        _mode = mode;
        _displayName = displayName;
    }

    /// <summary>Registers a callback for matching assignments.</summary>
    public void Callback(Arg<T> criteria, Action<T> callback) =>
        Callback(new PropertySetterSetup<T>(criteria), callback);

    /// <summary>Registers a callback belonging to one builder chain.</summary>
    public void Callback(PropertySetterSetup<T> setup, Action<T> callback)
    {
        Register(setup, "Callback");
        LazyInitializer.EnsureInitialized(ref _callbacks)!.Add((setup, callback));
        _hasConfiguredSetter = true;
    }

    private void Register(PropertySetterSetup<T> setup, string kind)
    {
        setup.DescribeOutcome(kind);
        if (Interlocked.CompareExchange(ref setup.Registered, 1, 0) == 0)
            LazyInitializer.EnsureInitialized(ref _setups)!.Push(setup);
    }

    /// <summary>Verifies matching assignments.</summary>
    public void Called(Arg<T> criteria, Count count) => Called(criteria, count, null);

    /// <summary>Verifies matching assignments with a reason.</summary>
    public void Called(Arg<T> criteria, Count count, string? because)
    {
        var head = _invocationHistory?.Head;
        var actual = 0;
        for (
            var current = head;
            current is not null;
            current = (PropertySetterInvocation<T>?)current.Next
        )
            if (criteria.Matches(current.StoredValue))
                actual++;
        if (count.Matches(actual))
        {
            for (
                var current = head;
                current is not null;
                current = (PropertySetterInvocation<T>?)current.Next
            )
                if (criteria.Matches(current.StoredValue))
                    current.MarkVerified();
            return;
        }
        var performed = new List<string>();
        for (
            var current = head;
            current is not null;
            current = (PropertySetterInvocation<T>?)current.Next
        )
            performed.Add(Describe(current));
        throw new VerificationFailedException(
            _displayName + " (setter)",
            "set " + _displayName + "(value: " + criteria + ")",
            count,
            actual,
            string.Join(Environment.NewLine, performed),
            because,
            head is null && Owner is not null ? DiagnosticValue.OtherInteractions(Owner) : null
        );
    }

    /// <summary>Configures assignments to call the base setter.</summary>
    public void UseBaseImplementation() =>
        UseBaseImplementation(new PropertySetterSetup<T>(Arg<T>.Any()));

    /// <summary>Configures base invocation belonging to the supplied builder chain.</summary>
    public void UseBaseImplementation(PropertySetterSetup<T> setup)
    {
        Register(setup, "UseBaseImplementation");
        setup.UsesBase = true;
        _hasConfiguredSetter = true;
        _useBaseImplementation = true;
    }

    /// <summary>Performs an assignment and records its outcome.</summary>
    public void Set(T value, Action<T>? baseImplementation = null)
    {
        var sequence = InvocationSequence.Next();
        var capturedValue = CaptureInvocationValue(value);
        Exception? failure = null;
        try
        {
            if (!RecordAndShouldCallBase(value))
                return;
            if (baseImplementation is null)
                throw MissingImposter(value);
            baseImplementation(value);
        }
        catch (Exception exception)
        {
            failure = exception;
            throw;
        }
        finally
        {
            RecordInvocation(capturedValue, sequence, failure);
        }
    }

    /// <summary>Runs the assignment behavior; generated init accessors record its complete outcome themselves.</summary>
    protected bool RecordAndShouldCallBase(T value)
    {
        if (_mode == ImposterMode.Explicit && !_hasConfiguredSetter)
            throw MissingImposter(value);
        if (_callbacks is not null)
            foreach (var registration in _callbacks)
                if (registration.Setup.Criteria.Matches(value))
                {
                    Interlocked.Exchange(ref registration.Setup.Used, 1);
                    registration.Callback(value);
                }
        if (_useBaseImplementation)
        {
            if (_setups is not null)
                foreach (var setup in _setups)
                    if (setup.UsesBase)
                        Interlocked.Exchange(ref setup.Used, 1);
            return true;
        }
        if (_defaultValue is { IsOn: true })
            _defaultValue.Write(value);
        return false;
    }

    /// <summary>Records the result of an assignment, including generated direct base init assignments.</summary>
    public void RecordInvocation(T value, long sequence, Exception? exception) =>
        LazyInitializer
            .EnsureInitialized(ref _invocationHistory)!
            .Push(CreateInvocation(value, sequence, exception));

    /// <summary>Captures an entry value; generated span setters override this to isolate mutable storage.</summary>
    public virtual T CaptureInvocationValue(T value) => value;

    /// <summary>Creates the generated property's concrete invocation record.</summary>
    protected virtual PropertySetterInvocation<T> CreateInvocation(
        T value,
        long sequence,
        Exception? exception
    ) => new RuntimeInvocation(_displayName, value, sequence, exception);

    /// <summary>Clears history without changing setups or their usage.</summary>
    public void ClearInvocations() => _invocationHistory?.Clear();

    /// <summary>Returns an entry-ordered snapshot of assignments.</summary>
    public IReadOnlyList<IImposterInvocation> Snapshot() =>
        MatchingInvocations<IImposterInvocation>(Arg<T>.Any());

    /// <summary>Returns matching assignments as the generated record type.</summary>
    public IReadOnlyList<TInvocation> MatchingInvocations<TInvocation>(Arg<T> criteria)
        where TInvocation : IImposterInvocation
    {
        var result = new List<TInvocation>();
        var history = _invocationHistory;
        if (history is not null)
            foreach (var invocation in history)
                if (criteria.Matches(invocation.StoredValue))
                    result.Add((TInvocation)(object)invocation);
        return InvocationSnapshots.Order(result);
    }

    /// <inheritdoc />
    public IEnumerable<string> UncoveredInteractions()
    {
        var history = _invocationHistory;
        if (history is not null)
            foreach (var invocation in history)
                if (!invocation.IsVerified)
                    yield return Describe(invocation);
    }

    /// <inheritdoc />
    public IEnumerable<string> Interactions()
    {
        var history = _invocationHistory;
        if (history is not null)
            foreach (var invocation in history)
                yield return Describe(invocation);
    }

    /// <inheritdoc />
    public IEnumerable<string> UnusedSetups()
    {
        if (_setups is not null)
            foreach (var setup in _setups)
                if (setup.IsUnused)
                    yield return setup.Describe(_displayName);
    }

    private sealed class RuntimeInvocation(
        string member,
        T value,
        long sequence,
        Exception? exception
    ) : PropertySetterInvocation<T>(value, sequence, exception)
    {
        public override string Member => member;
    }

    private string Describe(PropertySetterInvocation<T> invocation) =>
        "set "
        + _displayName
        + " = "
        + DiagnosticValue.Format(invocation.StoredValue)
        + (
            invocation.Exception is null
                ? string.Empty
                : " threw " + DiagnosticValue.Format(invocation.Exception)
        );

    private MissingImposterException MissingImposter(T value)
    {
        var descriptions = new List<string>();
        if (_setups is not null)
            foreach (var setup in _setups)
                descriptions.Add(setup.Describe(_displayName));
        return new MissingImposterException(
            _displayName + " (setter)",
            "set " + _displayName + "(value: " + DiagnosticValue.Format(value) + ")",
            descriptions
        );
    }
}
