namespace Imposter.Abstractions;

/// <summary>
/// Exception thrown when verification of an expected invocation count fails.
/// </summary>
/// <remarks>
/// Typically thrown by verification helpers such as <c>.Called(Count.Exactly(n))</c> when the actual invocation
/// count does not satisfy the specified <see cref="Count"/> constraint.
/// </remarks>
public class VerificationFailedException : Exception
{
    /// <summary>Gets the expectation described by this failure.</summary>
    public string Expected { get; }

    /// <summary>Creates a failure for an aggregate or ordered verification.</summary>
    /// <param name="expected">The expectation that failed.</param>
    /// <param name="performedInvocations">The interactions or setup findings explaining the failure.</param>
    public VerificationFailedException(string expected, string performedInvocations)
        : base(
            "Expected "
                + expected
                + Environment.NewLine
                + "Performed invocations:"
                + Environment.NewLine
                + performedInvocations
        )
    {
        Expected = expected;
        PerformedInvocations = performedInvocations;
    }

    /// <summary>
    /// Gets the textual representation of the invocations that were performed prior to the verification failure.
    /// </summary>
    public string? PerformedInvocations { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="expectedCount">The expected number of invocations.</param>
    /// <param name="actualCount">The actual number of invocations that occurred.</param>
    public VerificationFailedException(Count expectedCount, int actualCount)
        : this(expectedCount, actualCount, null) { }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="expectedCount">The expected number of invocations.</param>
    /// <param name="actualCount">The actual number of invocations that occurred.</param>
    /// <param name="performedInvocations">The textual representation of the invocations that were performed.</param>
    public VerificationFailedException(
        Count expectedCount,
        int actualCount,
        string? performedInvocations
    )
        : base(BuildMessage(expectedCount, actualCount, performedInvocations))
    {
        PerformedInvocations = performedInvocations;
        Expected = expectedCount.ToString();
    }

    /// <summary>Creates a failure describing a member, its matchers and an optional reason.</summary>
    /// <param name="member">The member name.</param>
    /// <param name="expected">The member and matcher description.</param>
    /// <param name="expectedCount">The expected invocation count.</param>
    /// <param name="actualCount">The matching invocation count.</param>
    /// <param name="performedInvocations">Recorded invocations of this member.</param>
    /// <param name="because">An optional reason for the expectation.</param>
    /// <param name="otherInteractions">Other recorded interactions when this member has no history.</param>
    public VerificationFailedException(
        string member,
        string expected,
        Count expectedCount,
        int actualCount,
        string? performedInvocations,
        string? because = null,
        string? otherInteractions = null
    )
        : base(
            $"Expected {expected} {expectedCount}, performed {actualCount} times."
                + Environment.NewLine
                + $"Performed invocations of {member}:"
                + (
                    string.IsNullOrEmpty(performedInvocations)
                        ? " none."
                        : Environment.NewLine + performedInvocations
                )
                + (
                    string.IsNullOrEmpty(otherInteractions)
                        ? string.Empty
                        : Environment.NewLine + otherInteractions
                )
                + (
                    string.IsNullOrEmpty(because)
                        ? string.Empty
                        : Environment.NewLine + "Because: " + because
                )
        )
    {
        Expected = expected;
        PerformedInvocations = performedInvocations;
    }

    private static string BuildMessage(
        Count expectedCount,
        int actualCount,
        string? performedInvocations
    )
    {
        var message =
            $"Invocation was expected to be performed {expectedCount} but instead was performed {actualCount} times.";

        if (!string.IsNullOrWhiteSpace(performedInvocations))
        {
            return $"{message}{Environment.NewLine}Performed invocations:{Environment.NewLine}{performedInvocations}";
        }

        return message;
    }
}
