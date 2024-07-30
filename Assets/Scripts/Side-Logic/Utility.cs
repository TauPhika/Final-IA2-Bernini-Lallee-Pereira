using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public static class Utility
{
    public static T Log<T>(T value, string prefix = "")
    {
        //Debug.Log(prefix + value);
        return value;
    }

    public static IEnumerable<T> Generate<T>(T seed, Func<T, T> mutate)
    {
        var accum = seed;
        while (true)
        {
            yield return accum;
            accum = mutate(accum);
        }
    }

    public static bool In<T>(this T x, HashSet<T> set)
    {
        return set.Contains(x);
    }

    public static bool In<K, V>(this KeyValuePair<K, V> x, Dictionary<K, V> dict)
    {
        return dict.Contains(x);
    }

    public static void UpdateWith<K, V>(this Dictionary<K, V> a, Dictionary<K, V> b)
    {
        foreach (var kvp in b)
        {
            a[kvp.Key] = kvp.Value;
        }
    }

    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> list)
    {
        return new HashSet<T>(list);
    }

    public static V DefaultGet<K, V>(
        this Dictionary<K, V> dict,
        K key,
        Func<V> defaultFactory
    )
    {
        V v;
        if (!dict.TryGetValue(key, out v))
            dict[key] = v = defaultFactory();
        return v;
    }
}
