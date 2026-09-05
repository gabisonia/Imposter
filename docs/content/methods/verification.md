# Verification

Verify that methods were invoked the expected number of times with specific arguments. Use `Called(Count.*)` on the method builder.

Target type used in examples:

!!! example
    ```csharp {data-gh-link="https://github.com/themidnightgospel/Imposter/blob/master/tests/Imposter.Tests/Features/Docs/Methods/Verification/VerificationTests.cs#L5"}
    using Imposter.Abstractions;

    [assembly: GenerateImposter(typeof(Imposter.Tests.Docs.Methods.IVerifyService))]

    public interface IVerifyService
    {
        void Increment(int v);
        int Combine(int a, int b);
    }
    ```

## Basic counts

!!! example
    ```csharp {data-gh-link="https://github.com/themidnightgospel/Imposter/blob/master/tests/Imposter.Tests/Features/Docs/Methods/Verification/VerificationTests.cs#L26"}
    service.Increment(1);
    service.Increment(2);

    imposter.Increment(Arg<int>.Any()).Called(Count.AtLeast(2));
    imposter.Increment(2).Called(Count.Once());
    ```

## Matching arguments

Verification respects the same argument matching rules used for arrangements:

!!! example
    ```csharp {data-gh-link="https://github.com/themidnightgospel/Imposter/blob/master/tests/Imposter.Tests/Features/Docs/Methods/Verification/VerificationTests.cs#L41"}
    // See more examples in repo tests
    imposter.Increment(Arg<int>.Is(x => x > 10)).Called(Count.Exactly(3));
    imposter.Combine(Arg<int>.Is(x => x > 0), Arg<int>.Is(y => y < 10)).Called(Count.Once());
    ```

## Checking for additional calls

Use `imposter.CallCount().Method(...)` to read the number of recorded calls matching its arguments and generic type arguments. It returns zero if no calls match. Reading counts does not create setups, change configured behavior, or clear invocation history. You can retain the object returned by `CallCount()` and query it again after more calls.

Each query scans the recorded calls for the selected method. The counter reads the current history on each query; retaining it does not freeze the count.

Save the count before the next action, then compare it with the new count to verify that additional calls occurred:

!!! example
    ```csharp
    service.Increment(1);
    var previousCount = imposter.CallCount().Increment(Arg<int>.Any());

    service.Increment(2);

    var additionalCalls = imposter.CallCount().Increment(Arg<int>.Any()) - previousCount;
    additionalCalls.ShouldBe(1);
    imposter.Increment(Arg<int>.Any()).Called(Count.Exactly(2));
    ```

`CallCount()` uses the same matching rules as `Called(Count.*)`. It counts calls to the selected method, not property, indexer, or event interactions. Await asynchronous work before checking its recorded calls, as you would with `Called`.

If the target already has a member or type parameter named `CallCount`, the query accessor receives a unique suffix, such as `CallCount_1()`, to preserve the existing setup API.

## Failures

When verification fails, `VerificationFailedException` is thrown. The message includes both the expected/actual counts and, when available, a textual list of performed invocations:

```
Invocation was expected to be performed {expectedCount} but instead was performed {actualCount} times.
Performed invocations:
{invocation1}
{invocation2}
...
```

Use this list to quickly see which calls were actually made and why the verification did not match your expectations.
