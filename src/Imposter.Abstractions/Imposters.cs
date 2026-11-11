namespace Imposter.Abstractions;

/// <summary>Verification helpers that operate across several imposters.</summary>
public static class Imposters
{
    /// <summary>Starts an ordered verification of member interactions.</summary>
    public static InvocationOrder InOrder() => new();

    /// <summary>Verifies that every recorded interaction has been covered by a successful check.</summary>
    /// <param name="imposters">The imposters to verify.</param>
    public static void VerifyNoOtherCalls(params IImposter[] imposters) =>
        Verify(imposters, imposter => imposter.VerifyNoOtherCalls(), "No uncovered interactions");

    /// <summary>Verifies that no interactions have been recorded.</summary>
    /// <param name="imposters">The imposters to verify.</param>
    public static void VerifyNoInteractions(params IImposter[] imposters) =>
        Verify(imposters, imposter => imposter.VerifyNoInteractions(), "No interactions");

    /// <summary>Verifies that each non-lenient configured setup has answered a call.</summary>
    /// <param name="imposters">The imposters to verify.</param>
    public static void VerifyAllSetupsUsed(params IImposter[] imposters) =>
        Verify(imposters, imposter => imposter.VerifyAllSetupsUsed(), "All configured setups used");

    private static void Verify(IImposter[] imposters, Action<IImposter> verify, string expected)
    {
        if (imposters is null)
            throw new ArgumentNullException(nameof(imposters));
        if (imposters.Any(imposter => imposter is null))
            throw new ArgumentException("An imposter cannot be null.", nameof(imposters));

        var failures = new List<string>();
        for (var index = 0; index < imposters.Length; index++)
        {
            try
            {
                verify(imposters[index]);
            }
            catch (VerificationFailedException exception)
            {
                failures.Add(
                    imposters[index].GetType().Name + " #" + (index + 1) + ": " + exception.Message
                );
            }
        }
        if (failures.Count != 0)
            throw new VerificationFailedException(
                expected,
                string.Join(Environment.NewLine, failures)
            );
    }
}
