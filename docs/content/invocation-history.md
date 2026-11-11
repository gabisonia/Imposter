# Invocation history

Generated imposters expose completed interactions as read-only snapshots. A method verifier's
`Invocations` property applies the same argument matchers as `Called`:

```csharp
instance.Hello("Ann", 1);
instance.Hello("Bob", 2);

var calls = imposter.Hello(Arg<string>.Any(), Arg<int>.Any()).Invocations;
Assert.Equal("Ann", calls[0].Name);
Assert.Equal(1, calls[0].Times);
Assert.True(calls[0].Sequence < calls[1].Sequence);
```

Each generated method record has properties for captured arguments, `Result` when the return
value can be stored, and `Member`, `Sequence`, and `Exception`. Generic methods expose records
for the caller's type arguments. `Called`, `CallCount`, and typed snapshots consistently use
those actual type arguments, including when a compatible setup with other type arguments
provides the outcome. Earlier versions could count an adapted call under its setup's type
arguments instead; verification should now name the type arguments used by the caller.
`VerifyAllSetupsUsed()` still tracks the compatible setup that supplied the outcome.
`ref` and `in` arguments retain their entry values; `out`
arguments retain their values when the call returns or throws. If an exception occurs before
an `out` argument is assigned, its recorded value is the type's default.

Property setters expose `imposter.Property.Setter(matcher).Invocations`. Indexer getters and
setters expose `imposter[keyMatcher].Getter().Invocations` and
`imposter[keyMatcher].Setter().Invocations`, including captured keys and a `Result` or `Value`
when the value can be stored. Custom ref structs cannot be retained in a heap record; their
values are omitted. Span arguments and values are copied and exposed as `ReadOnlyMemory<T>`.
Span setter records also isolate their entry values from setter callbacks and subsequent writes through a property getter.
Ordinary reference arguments and return values retain their identity; arbitrary objects are
not deep-cloned. Read-only memory does not provide isolation from deliberate low-level access
to its underlying array.

For a combined view, use the `Invocations()` extension from `Imposter.Abstractions`:

```csharp
IReadOnlyList<IImposterInvocation> history = imposter.Invocations();
foreach (var call in history)
    Console.WriteLine($"{call.Sequence}: {call.Member}");
```

The combined view includes methods, property setters, indexer getters and setters, and event
subscriptions and unsubscriptions. Event records are `EventSubscriptionInvocation<THandler>`
and expose `Handler` and `IsSubscription`. `imposter.Event.Subscriptions` and
`imposter.Event.Unsubscriptions` provide typed snapshots for the two event accessors.
Property getters use counters and do not appear in
history or participate in ordering. Raising events from test code and invoking their handlers
do not add history records.

Sequence numbers are assigned when an interaction enters and are shared across imposter
instances. Nested calls therefore sort by entry, even when the inner call completes first.
Only completed interactions appear in snapshots. Calls returning `Task` or `ValueTask` complete
for recording purposes when the return value is obtained: the record keeps that value and
does not await it or capture exceptions from its later asynchronous completion.

A throwing interaction is recorded after the callback's `finally` blocks finish, so its
record includes final `out` values. Verification from inside that callback's `finally` block
does not yet see the current call. Earlier versions could expose a failed call before those
blocks ran; code relying on that timing should verify after the exception reaches the caller.

Snapshots are ordered by sequence and keep their membership after later calls,
`ClearInvocations()`, or `Reset()`. Reading history does not mark calls verified or consume
configured outcomes. Concurrent calls safely publish complete records. A snapshot taken
while calls are running is a view of the records observed during collection, rather than an
atomic transaction across all members; take it after the calls finish when asserting a full
history. Snapshot creation allocates its list on demand.

## Performance costs

History and verification metadata have a measurable cost. Each recorded interaction acquires a global sequence number with `Interlocked.Increment`. Its sequence word also stores the verification flag, so coverage does not require another object or a separate field. Property getters remain counter-only and allocate no history records.

The following figures describe the specific x64 Release fixtures in [CallAllocationTests](https://github.com/themidnightgospel/Imposter/blob/master/tests/Imposter.Tests/Features/CallAllocations/CallAllocationTests.cs), after warm-up. The previous column lists the earlier regression limit; the current column is the measured allocation used as the new limit. These are representative fixtures, not universal costs for every argument or return type.

| Fixture | Previous limit | Current bytes | Retained information |
| --- | ---: | ---: | --- |
| Method returning `int`, generic reference result, or cached `Task<int>` | 48 | 56 per call | Entry sequence in the existing argument/result/exception record |
| Void method with one reference argument | 40 | 48 per call | Entry sequence in the existing record |
| Cached `ValueTask<int>` method | 56 | 64 per call | Entry sequence in the existing value-task result record |
| `ref`/`out`/`in` method returning a cached task | 48 | 64 per call | Entry sequence and the final `out` value, including object alignment |
| `int` property setter | 32 | 48 per assignment | Value, synchronous exception, sequence and next-record link |
| `int` indexer getter with one `int` key | 56 | 80 per read | Captured keys plus result, exception and sequence |
| `int` indexer setter with one `int` key | 64 | 80 per assignment | Captured keys plus value, exception and sequence |
| Generic span method with four `int` elements | 136 | 176 per call | Three copied arrays and the record; the public entry snapshot is isolated from arrays exposed to matching predicates |
| Imposter containing one `int` property | 280 | 304 per construction | Owner, diagnostic and coverage fields; history and callback storage remain lazy |
| Method setup containing three sequential returns | 736 | 816 per setup | Steps, closures and immutable diagnostic-label arrays retained during setup |

The generic span case retains the existing lookup and invocation-matching copies and adds a separate caller-entry snapshot. A matching predicate can mutate its array, so sharing that array with public history would change the recorded arguments. Each four-element array occupies 40 bytes in this fixture. Callback execution does not allocate additional per-call wrapper objects.

The same tests pin property getter access to **0 bytes**, and successful `Called`, `Subscribed` and `Unsubscribed` checks with cached handles, matchers and `Count` instances to **0 bytes after warm-up**. A getter's first verification initializes its lazy synchronization object. Reading invocation snapshots, producing failure messages, and configuring new setups can allocate. Successful count verification traverses its captured history twice to count and cover matches, so zero allocation does not mean zero additional CPU work.

The global sequence can introduce contention across independent imposters. [InvocationContentionBenchmarks](https://github.com/themidnightgospel/Imposter/blob/master/benchmarks/Imposter.Benchmarks/InvocationContentionBenchmarks.cs) compares local and global sequence counters with identical record publication, and separately measures generated calls with 1, 4 and 8 workers sharing one imposter or using independent imposters. Each batch retains a bounded number of records. Run the benchmark on representative hardware when concurrent-call throughput matters; the generated-call comparison includes setup and lookup work in addition to sequence publication.

See the [measured contention results](invocation-contention.md) for the initial ShortRun results and their limitations.
