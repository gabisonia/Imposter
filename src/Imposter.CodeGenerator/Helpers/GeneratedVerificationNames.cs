using System.Collections.Generic;

namespace Imposter.CodeGenerator.Helpers;

// Lifecycle and verification helpers appear inside both target-generic and method-generic types.
// Reserve their fixed member, parameter and local names before rendering either kind of type parameter.
// TypeParameterNames preserves the target's public namespace/file identity when it performs this renaming.
internal static class GeneratedVerificationNames
{
    internal static readonly HashSet<string> Reserved =
    [
        "Invocations",
        "Owner",
        "Snapshot",
        "MatchingInvocations",
        "CreateInvocation",
        "CaptureInvocationValue",
        "Member",
        "Sequence",
        "Exception",
        "Value",
        "Result",
        "IsVerified",
        "MarkVerified",
        "Lenient",
        "ClearInvocations",
        "Reset",
        "VerifyNoOtherCalls",
        "VerifyNoInteractions",
        "VerifyAllSetupsUsed",
        "Interactions",
        "UncoveredInteractions",
        "UnusedSetups",
        "Description",
        "Imposter",
        "invocations",
        "owner",
        "source",
        "entry",
        "sequence",
        "result",
        "findings",
        "matches",
        "criteria",
        "count",
        "value",
        "arguments",
        "invocation",
        "current",
        "head",
        "failure",
        "exception",
        "setup",
        "registration",
        "actual",
        "because",
        "description",
        "assignmentValue",
        "assignmentCapturedValue",
        "assignmentSequence",
        "assignmentFailure",
        "assignmentException",
        "_runtimeCriteria",
        "_setup",
        "_verification",
    ];
}
