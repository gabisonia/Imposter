using System;
using Imposter.Abstractions;
using Shouldly;
using Xunit;

namespace Imposter.CodeGenerator.Tests;

public class DiagnosticExceptionTests
{
    [Fact]
    public void GivenSetupDescriptions_WhenCallerMutatesInput_ShouldKeepExceptionSnapshot()
    {
        var setups = new[] { "Hello(name: any) -> Returns" };
        var exception = new MissingImposterException("Hello", "Hello(name: <Ann>)", setups);
        setups[0] = "changed";
        exception.ConfiguredSetups[0].ShouldBe("Hello(name: any) -> Returns");
        exception.Message.ShouldContain("No setup matched Hello(name: <Ann>).");
        exception.MethodName.ShouldBe("Hello");
    }

    [Fact]
    public void GivenNoRecordedCalls_WhenVerificationFails_ShouldExplainExpectationAndReason()
    {
        var exception = new VerificationFailedException(
            "Hello",
            "Hello(name: any)",
            Count.Once(),
            0,
            string.Empty,
            "a greeting is required",
            "Other interactions on Greeter:\n  Goodbye()"
        );
        exception.Expected.ShouldBe("Hello(name: any)");
        exception.Message.ShouldStartWith(
            "Expected Hello(name: any) exactly 1 time(s), performed 0 times."
        );
        exception.Message.ShouldContain("Performed invocations of Hello: none.");
        exception.Message.ShouldContain("Other interactions on Greeter:");
        exception.Message.ShouldEndWith("Because: a greeting is required");
    }

    [Fact]
    public void GivenStoredOutcomeDescriptions_WhenFormatted_ShouldKeepAllConfiguredSteps()
    {
        object? descriptions = null;
        DiagnosticValue.AddOutcome(ref descriptions, "Returns");
        DiagnosticValue.AddOutcome(ref descriptions, "Throws<InvalidOperationException>");
        DiagnosticValue.AddOutcome(ref descriptions, "Callback");
        DiagnosticValue
            .Outcomes(descriptions)
            .ShouldBe("Returns, Throws<InvalidOperationException>, Callback");
        DiagnosticValue.Outcomes(null).ShouldBe(string.Empty);
    }
}
