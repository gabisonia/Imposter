# Clearing history and resetting an imposter

Use `ClearInvocations()` between test phases when the configured behavior should stay the same. Use `Reset()` when the same instance needs fresh imposter-owned state.

```csharp
imposter.Next().Returns(1).Then().Returns(2);
var instance = imposter.Instance();
instance.Next(); // 1

imposter.ClearInvocations();
imposter.Next().Called(Count.Never());
instance.Next(); // 2: clearing history does not rewind the outcome sequence

imposter.Reset();
imposter.Next().Returns(42);
instance.Next(); // 42: the previously obtained instance stays connected
```

| State | `ClearInvocations()` | `Reset()` |
| --- | --- | --- |
| Invocation history and verification coverage | Cleared | Cleared |
| Setups, their usage and leniency | Kept | Cleared |
| Position in sequential outcomes | Kept | Cleared with the setup |
| Stored property and indexer values | Kept | Cleared |
| Imposter-owned event subscriptions | Kept | Cleared |
| Underlying instance identity | Kept | Kept |
| Implicit or Explicit mode | Kept | Kept |
| Previously obtained setup builders | Still connected | Detached |

After `Reset()`, obtain new setup builders. Configuring a builder retained from before the reset has no effect on subsequent instance calls.

For class targets, reset does not rerun the constructor or roll back base-class state changed through `UseBaseImplementation()`. An unconfigured property reads its current base getter again on the first read after reset; that value may differ from the original initializer. Likewise, subscriptions managed by a base event accessor belong to the base instance, not to the imposter's subscription storage.

`ClearInvocations()` is safe while instance calls are running. An overlapping call may appear in the remaining history or be cleared; history traversal remains valid. Clearing several members does not promise one simultaneous cutoff across all members. Do not race `Reset()` with instance calls, setup changes or verification.

The global invocation sequence is never reset. Previously obtained invocation-list snapshots remain usable after either operation.

If the target itself declares a member called `Reset` or `ClearInvocations`, that member keeps its setup name. Access the lifecycle operation through the interface:

```csharp
((IImposter)imposter).ClearInvocations();
((IImposter)imposter).Reset();
```

Reset clears the histories observed by retained builders before detaching them. Those builders cannot configure or observe new calls on the reset imposter. Snapshot lists obtained before the reset keep their original records.
