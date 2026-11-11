using System.ComponentModel;

namespace Imposter.Abstractions;

/// <summary>Shared storage for generated accessor and event invocation records.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class InvocationRecord : IVerifiableInvocation
{
    internal InvocationRecord? Next;

    // Entry stamps are positive; the sign bit tracks coverage without growing every record.
    private long _sequenceAndCoverage;

    /// <summary>Initializes an invocation record.</summary>
    /// <param name="sequence">The invocation entry stamp.</param>
    /// <param name="exception">The synchronous exception, if any.</param>
    protected InvocationRecord(long sequence, Exception? exception = null)
    {
        _sequenceAndCoverage = sequence;
        Exception = exception;
    }

    /// <inheritdoc />
    public abstract string Member { get; }

    /// <inheritdoc />
    public long Sequence => Volatile.Read(ref _sequenceAndCoverage) & long.MaxValue;

    /// <inheritdoc />
    public Exception? Exception { get; private set; }

    /// <inheritdoc />
    public bool IsVerified => Volatile.Read(ref _sequenceAndCoverage) < 0;

    /// <inheritdoc />
    public void MarkVerified() =>
        Interlocked.Exchange(
            ref _sequenceAndCoverage,
            Volatile.Read(ref _sequenceAndCoverage) | long.MinValue
        );

    /// <summary>Records an exception before this invocation is published to history.</summary>
    /// <param name="exception">The exception thrown by the interaction.</param>
    protected internal void RecordException(Exception exception) => Exception = exception;
}
