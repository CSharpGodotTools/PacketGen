using NUnit.Framework;
using System;
using System.Collections;

namespace PacketGen.Tests;

internal static class DeepAssert
{
    public static void AreEqual(object? expected, object? actual, string path)
    {
        if (ReferenceEquals(expected, actual))
            return;

        if (expected is null || actual is null)
        {
            Assert.That(actual, Is.EqualTo(expected), $"{path} expected <{Format(expected)}> but was <{Format(actual)}>.");
            return;
        }

        if (expected is string || expected.GetType().IsPrimitive || expected is decimal || expected is Enum)
        {
            Assert.That(actual, Is.EqualTo(expected), $"{path} expected <{Format(expected)}> but was <{Format(actual)}>.");
            return;
        }

        if (expected is Array expArray && actual is Array actArray)
        {
            Assert.That(actArray.Length, Is.EqualTo(expArray.Length), $"{path} length mismatch. Expected {expArray.Length}, got {actArray.Length}.");

            for (int i = 0; i < expArray.Length; i++)
            {
                object? exp = expArray.GetValue(i);
                object? act = actArray.GetValue(i);
                AreEqual(exp, act, $"{path}[{i}]");
            }

            return;
        }

        if (expected is IDictionary expDict && actual is IDictionary actDict)
        {
            Assert.That(actDict.Count, Is.EqualTo(expDict.Count), $"{path} count mismatch. Expected {expDict.Count}, got {actDict.Count}.");

            foreach (DictionaryEntry entry in expDict)
            {
                if (!actDict.Contains(entry.Key))
                    Assert.Fail($"{path} missing key <{Format(entry.Key)}>.");

                AreEqual(entry.Value, actDict[entry.Key], $"{path}[{Format(entry.Key)}]");
            }

            foreach (DictionaryEntry entry in actDict)
            {
                if (!expDict.Contains(entry.Key))
                    Assert.Fail($"{path} had unexpected key <{Format(entry.Key)}>.");
            }

            return;
        }

        if (expected is IList expList && actual is IList actList)
        {
            Assert.That(actList.Count, Is.EqualTo(expList.Count), $"{path} count mismatch. Expected {expList.Count}, got {actList.Count}.");

            for (int i = 0; i < expList.Count; i++)
            {
                AreEqual(expList[i], actList[i], $"{path}[{i}]");
            }

            return;
        }

        Assert.That(actual, Is.EqualTo(expected), $"{path} expected <{Format(expected)}> but was <{Format(actual)}>.");
    }

    private static string Format(object? value)
    {
        return value is null ? "null" : value.ToString() ?? "(null)";
    }
}
