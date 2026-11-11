namespace Imposter.Abstractions;

/// <summary>A recorded event subscription or unsubscription.</summary>
/// <typeparam name="THandler">The event delegate type.</typeparam>
public sealed class EventSubscriptionInvocation<THandler> : InvocationRecord
{
    /// <summary>Creates a completed event interaction.</summary>
    /// <param name="member">The event name.</param>
    /// <param name="handler">The handler supplied to the event accessor.</param>
    /// <param name="isSubscription">Whether the handler was added.</param>
    /// <param name="sequence">The entry stamp.</param>
    /// <param name="exception">The exception thrown by the accessor.</param>
    public EventSubscriptionInvocation(
        string member,
        THandler handler,
        bool isSubscription,
        long sequence,
        Exception? exception
    )
        : base(sequence, exception)
    {
        Member = member;
        Handler = handler;
        IsSubscription = isSubscription;
    }

    /// <inheritdoc />
    public override string Member { get; }

    /// <summary>Gets the supplied handler.</summary>
    public THandler Handler { get; }

    /// <summary>Gets whether this invocation added the handler.</summary>
    public bool IsSubscription { get; }

    /// <inheritdoc />
    public override string ToString() =>
        Member
        + (IsSubscription ? " subscribed " : " unsubscribed ")
        + DiagnosticValue.Format(Handler)
        + (Exception is null ? string.Empty : " threw " + DiagnosticValue.Format(Exception));
}
