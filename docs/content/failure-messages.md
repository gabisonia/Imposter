# Failure messages

Describe a matcher when a predicate's meaning is important to a failing test:

```csharp
imposter.Hello(Arg<string>.Any(), Arg<int>.Is(x => x > 10).Describe("greater than 10"))
    .Called(Count.Once(), "a greeting should be repeated");
```

`Describe` returns a new matcher. It does not change matching behavior or modify a shared `Any()` matcher. `SpanArg<T>` and `ReadOnlySpanArg<T>` support the same operation.

Default descriptions are `any`, `<value>`, `<predicate>`, `not <description>`, and membership lists such as `in [<1>, <2>]`. Span equality matchers display their captured element list. Describing a matcher never evaluates its predicate or comparer.

Verification failures expose the matcher expectation in `VerificationFailedException.Expected` and retain the recorded member calls in `PerformedInvocations`. A `because` argument adds the reason to the failure message. When the member has no recorded calls, the message can include other recorded interactions in sequence order. A member that was called with different arguments still reports its own history.

Explicit-mode failures include the actual invocation and `MissingImposterException.ConfiguredSetups`. Bare verification builders are not configured setups. Outcome descriptions reflect the original configuration, including consumed sequential outcomes; they retain outcome kinds and exception type names, not return factories or captured callback objects.

Argument and result formatting calls `ToString()` only while producing diagnostics. If it throws, the value is shown as `<unprintable TypeName>` and the original verification or missing-setup exception is preserved. Formatting never invokes configured outcomes, callbacks, or matcher predicates.

## Compatibility

Generated failure messages have changed to include member names and matcher descriptions. Tests that assert exact message text should be updated deliberately. The legacy exception constructors remain available, and `PerformedInvocations` retains its existing purpose.
