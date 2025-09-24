using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shouldly;

namespace ManagedCode.Communication.Tests.TestHelpers;

public static class ShouldlyTestExtensions
{
    public static void ShouldHaveCount<T>(this IEnumerable<T> actual, int expected, string? customMessage = null)
    {
        actual.ShouldNotBeNull(customMessage);
        actual.Count().ShouldBe(expected, customMessage);
    }

    public static void ShouldHaveCount(this IEnumerable actual, int expected, string? customMessage = null)
    {
        actual.ShouldNotBeNull(customMessage);
        actual.Cast<object?>().Count().ShouldBe(expected, customMessage);
    }

    public static void ShouldBeEquivalentTo<T>(this IEnumerable<T> actual, IEnumerable<T> expected, string? customMessage = null)
    {
        actual.ShouldNotBeNull(customMessage);
        expected.ShouldNotBeNull(customMessage);

        var actualList = actual.ToList();
        var expectedList = expected.ToList();

        actualList.Count.ShouldBe(expectedList.Count, customMessage);
        for (var i = 0; i < actualList.Count; i++)
        {
            actualList[i].ShouldBe(expectedList[i], customMessage);
        }
    }

    public static void ShouldBeEquivalentTo<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> actual, IReadOnlyDictionary<TKey, TValue> expected, string? customMessage = null)
    {
        actual.ShouldNotBeNull(customMessage);
        expected.ShouldNotBeNull(customMessage);

        actual.Count.ShouldBe(expected.Count, customMessage);
        foreach (var (key, value) in expected)
        {
            actual.ContainsKey(key).ShouldBeTrue(customMessage);
            actual[key].ShouldBe(value, customMessage);
        }
    }

    public static void ShouldBeCloseTo(this DateTime actual, DateTime expected, TimeSpan tolerance, string? customMessage = null)
    {
        var delta = (actual - expected).Duration();
        (delta <= tolerance).ShouldBeTrue(customMessage ?? $"Expected |{actual - expected}| <= {tolerance} but was {delta}.");
    }
}
