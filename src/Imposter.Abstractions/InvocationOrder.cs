using System.Text;

namespace Imposter.Abstractions;

/// <summary>Verifies a subsequence of interactions, including interactions across imposters.</summary>
public sealed class InvocationOrder
{
    private readonly List<IInvocationStep> _steps = new();

    internal InvocationOrder() { }

    /// <summary>Adds the next expected interaction without configuring an outcome.</summary>
    /// <param name="step">The generated member verification builder.</param>
    /// <returns>This ordered verification.</returns>
    public InvocationOrder Then(IInvocationStep step)
    {
        if (step is null)
            throw new ArgumentNullException(nameof(step));
        _steps.Add(step);
        return this;
    }

    /// <summary>Verifies the configured steps and covers only the selected invocations on success.</summary>
    /// <remarks>
    /// Each step selects the earliest matching invocation after the previous step. Unrelated calls may occur
    /// between steps. A failed verification adds no coverage. Coverage from earlier verifications is retained.
    /// </remarks>
    public void Verify()
    {
        var selected = new IVerifiableInvocation[_steps.Count];
        var previous = long.MinValue;
        for (var index = 0; index < _steps.Count; index++)
        {
            IVerifiableInvocation? match = null;
            foreach (var invocation in _steps[index].MatchingInvocations())
            {
                if (
                    invocation.Sequence <= previous
                    || (match is not null && invocation.Sequence >= match.Sequence)
                )
                    continue;
                match =
                    invocation as IVerifiableInvocation
                    ?? throw new InvalidOperationException(
                        "An ordered invocation must support verification coverage."
                    );
            }

            if (match is null)
                throw Failure(index);
            selected[index] = match;
            previous = match.Sequence;
        }

        foreach (var invocation in selected)
            invocation.MarkVerified();
    }

    private VerificationFailedException Failure(int failedIndex)
    {
        var expected = new StringBuilder("Interactions in order:");
        for (var index = 0; index < _steps.Count; index++)
            expected
                .AppendLine()
                .Append(index == failedIndex ? "  -> " : "     ")
                .Append(index + 1)
                .Append(". ")
                .Append(_steps[index].Description);

        var owners = new List<IImposter>();
        var interactions = new List<(IImposterInvocation Invocation, string Owner)>();
        foreach (var step in _steps)
        {
            var owner = step.Imposter;
            if (owners.Any(existing => ReferenceEquals(existing, owner)))
                continue;
            owners.Add(owner);
            var ownerName = owner.GetType().Name + " #" + owners.Count;
            foreach (var invocation in owner.Invocations())
                interactions.Add((invocation, ownerName));
        }
        interactions.Sort(
            (left, right) => left.Invocation.Sequence.CompareTo(right.Invocation.Sequence)
        );
        var performed = string.Join(
            Environment.NewLine,
            interactions.Select(item => item.Owner + ": " + item.Invocation)
        );
        return new VerificationFailedException(
            expected.ToString(),
            string.IsNullOrEmpty(performed) ? "none" : performed
        );
    }
}
