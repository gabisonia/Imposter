using System;
using System.Collections.Generic;
using Imposter.Abstractions;
using Shouldly;
using Xunit;

namespace Imposter.CodeGenerator.Tests;

public class MatcherDescriptionTests
{
    [Fact]
    public void GivenMatchers_WhenDescribed_ShouldUseDefaultDescriptions()
    {
        Arg<int>.Any().ToString().ShouldBe("any");
        Arg<int>.Is(42).ToString().ShouldBe("<42>");
        Arg<string>.Is((string?)null).ToString().ShouldBe("<null>");
        Arg<int>.IsNot(42).ToString().ShouldBe("not <42>");
        Arg<int>.IsIn(new[] { 1, 2 }).ToString().ShouldBe("in [<1>, <2>]");
        Arg<int>.IsNotIn(new[] { 1, 2 }).ToString().ShouldBe("not in [<1>, <2>]");
        Arg<int>.Is(_ => throw new Exception()).ToString().ShouldBe("<predicate>");
        Arg<int>.IsNot(_ => throw new Exception()).ToString().ShouldBe("not <predicate>");
    }

    [Fact]
    public void GivenCachedMatcher_WhenDescribed_ShouldKeepOriginalUnchanged()
    {
        var original = Arg<int>.Any();
        var described = original.Describe("any integer");
        described.ShouldNotBeSameAs(original);
        described.ToString().ShouldBe("any integer");
        described.Matches(42).ShouldBeTrue();
        original.ToString().ShouldBe("any");
        described.Describe("replacement").ToString().ShouldBe("replacement");
        described.ToString().ShouldBe("any integer");
        Should.Throw<ArgumentNullException>(() => original.Describe(null!));
    }

    [Fact]
    public void GivenDescribedMatcher_WhenMatching_ShouldKeepNegationAndComparer()
    {
        var matcher = Arg<string>
            .IsNot("hello", StringComparer.OrdinalIgnoreCase)
            .Describe("not hello");
        matcher.Matches("HELLO").ShouldBeFalse();
        matcher.Matches("world").ShouldBeTrue();
    }

    [Fact]
    public void GivenSpanMatchers_WhenDescribed_ShouldKeepCapturedElementsAndBehavior()
    {
        var values = new[] { 1, 2 };
        var span = SpanArg<int>.Is(values);
        var readOnly = ReadOnlySpanArg<int>.Is(values);
        values[0] = 99;
        span.ToString().ShouldBe("[<1>, <2>]");
        readOnly.ToString().ShouldBe("[<1>, <2>]");
        span.Describe("pair").Matches(new[] { 1, 2 }).ShouldBeTrue();
        readOnly.Describe("pair").Matches(new[] { 1, 2 }).ShouldBeTrue();
        SpanArg<int>.Any().Describe("custom").ToString().ShouldBe("custom");
        SpanArg<int>.Any().ToString().ShouldBe("any");
        ReadOnlySpanArg<int>.Any().Describe("custom").ToString().ShouldBe("custom");
        ReadOnlySpanArg<int>.Any().ToString().ShouldBe("any");
        SpanArg<int>.Is(_ => throw new Exception()).ToString().ShouldBe("<predicate>");
        ReadOnlySpanArg<int>.Is(_ => throw new Exception()).ToString().ShouldBe("<predicate>");
        Should.Throw<ArgumentNullException>(() => span.Describe(null!));
        Should.Throw<ArgumentNullException>(() => readOnly.Describe(null!));
    }

    [Fact]
    public void GivenThrowingFormatter_WhenDescribing_ShouldUseFallbackWithoutEvaluatingComparer()
    {
        var value = new Unprintable();
        DiagnosticValue.Format(value).ShouldBe("<unprintable Unprintable>");
        Arg<Unprintable>
            .Is(value, new ThrowingComparer())
            .ToString()
            .ShouldBe("<unprintable Unprintable>");
        SpanArg<Unprintable>
            .Is(new[] { value }, new ThrowingComparer())
            .ToString()
            .ShouldBe("[<unprintable Unprintable>]");
    }

    private sealed class Unprintable
    {
        public override string ToString() => throw new InvalidOperationException();
    }

    private sealed class ThrowingComparer : IEqualityComparer<Unprintable>
    {
        public bool Equals(Unprintable? x, Unprintable? y) => throw new InvalidOperationException();

        public int GetHashCode(Unprintable obj) => throw new InvalidOperationException();
    }
}
