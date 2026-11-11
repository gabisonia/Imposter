# Verification completeness and order

Use `VerifyNoOtherCalls()` after your member assertions to check that every recorded interaction was covered by a successful verification. `VerifyNoInteractions()` checks that no interactions occurred, including interactions you already verified. `VerifyAllSetupsUsed()` checks that configured setups executed.

```csharp
imposter.Save(42).Called(Count.Once());
imposter.VerifyNoOtherCalls();
imposter.VerifyAllSetupsUsed();
```

The same checks can aggregate several imposters and report all their failures:

```csharp
Imposters.VerifyNoOtherCalls(repository, notifications);
Imposters.VerifyNoInteractions(repository, notifications);
Imposters.VerifyAllSetupsUsed(repository, notifications);
```

All generated imposters implement `IImposter`. If a target member has the same name as one of these helpers, use `((IImposter)imposter).VerifyNoOtherCalls()` or the `Imposters` helper to disambiguate it.

## Coverage

A passing `Called(count)` covers the matching calls in the history snapshot captured by that check. A failing check covers nothing. `Called(Count.Never())` can pass without covering any calls. Reading `Invocations` or calling `CallCount()` does not cover calls. Calls arriving after a snapshot are left uncovered.

The successful path counts and marks matches in two passes over the same captured history. Matchers must be deterministic: the same recorded input must return the same result when evaluated again. Successful verification may evaluate your matcher twice per record.

Property getters use counters and retain no typed history; their successful count assertions cover the captured count. Property setters, methods, and indexer getters and setters retain invocation records. Event subscriptions and unsubscriptions are interactions and are covered by successful `Subscribed` and `Unsubscribed` assertions. Test-driven event raises and event handler executions are excluded from these completeness checks.

Coverage is cumulative. `ClearInvocations()` clears history and coverage, while retaining setup usage and leniency. `Reset()` also removes setups and their usage state.

## Unused setups and leniency

Creating a verification handle alone does not configure a setup. Method and accessor setup chains are considered used when any configured outcome or callback executes, including one that throws; remaining `Then()` outcomes need not all execute. If several accessor setup chains match an access, each chain whose callback executes is marked used. A separate chain that never executes remains unused, even if its matcher values equal those of a used chain.

Mark an optional setup with `Lenient()` to exclude it from the unused-setup check:

```csharp
imposter.Load(42).Returns("optional").Then().Returns("later").Lenient();
imposter.Value.Setter(Arg<int>.Any()).Callback(value => Log(value)).Lenient();
```

Leniency does not hide interactions or change matching, outcomes, or count verification. Event leniency applies to the event's whole setup builder, including its subscription interceptors, unsubscription interceptors, callbacks, and base-implementation setup. Event registrations are tracked individually. Event callbacks become used when a raise actually executes them; a callback registered after the last raise remains unused. The raise itself is not a setup.

## Ordered verification

Pass method handles, property setter handles, or indexer accessor handles to `Imposters.InOrder()`:

```csharp
Imposters.InOrder()
    .Then(repository.Save(42))
    .Then(notifications.Notify(42))
    .Verify();
```

Each step selects the earliest matching invocation after the previous selected invocation. Repeated steps require distinct invocations. Unrelated calls between steps are allowed. The check uses one global invocation-entry sequence across imposters, so an outer call precedes a nested callback call even if the outer call completes later.

A passing order check covers exactly its selected invocations. A failing order check covers none. Follow it with `Imposters.VerifyNoOtherCalls(...)` when you also want to find uncovered calls. Earlier count or order checks still contribute coverage; this combination does not enforce an exclusive sequence across the entire test.

Property getters are counter-only and cannot be order steps. Event builders have no per-action order handle; event subscription ordering is visible in invocation history but is not exposed through `Then()`.
