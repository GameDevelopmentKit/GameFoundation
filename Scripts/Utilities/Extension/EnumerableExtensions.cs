namespace GameFoundation.Scripts.Utilities.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Random = UnityEngine.Random;

    public static class EnumerableExtension
    {
        #if !NET_STANDARD_2_1
        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> enumerable, int count)
        {
            enumerable = enumerable as ICollection<T> ?? enumerable.ToArray();
            return enumerable.Skip(enumerable.Count() - count);
        }
        #endif

        public static ReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.ToList().AsReadOnly();
        }

        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable) action(item);
        }

        public static void SafeForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            enumerable.ToArray().ForEach(action);
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.OrderBy(_ => Guid.NewGuid());
        }

        public static IEnumerable<T> Sample<T>(this IEnumerable<T> enumerable, int count)
        {
            return enumerable.Shuffle().Take(count);
        }

        public static T Choice<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.Shuffle().First();
        }

        public static T Choice<T>(this IEnumerable<T> enumerable, IEnumerable<int> weights)
        {
            weights = weights as ICollection<int> ?? weights.ToArray();
            var sumWeight = Random.Range(0, weights.Sum());
            return IterTools.StrictZip(enumerable, weights)
                .First((_, weight) => (sumWeight -= weight) < 0)
                .Item1;
        }

        public static T Choice<T>(this IEnumerable<T> enumerable, IEnumerable<float> weights)
        {
            weights = weights as ICollection<float> ?? weights.ToArray();
            var sumWeight = Random.Range(0, weights.Sum());
            return IterTools.StrictZip(enumerable, weights)
                .First((_, weight) => (sumWeight -= weight) < 0)
                .Item1;
        }

        public static IEnumerable<(int Index, T Value)> Enumerate<T>(this IEnumerable<T> enumerable, int start = 0)
        {
            return enumerable.Select(item => (start++, item));
        }

        public static IEnumerable<T> ToEnumerable<T>(this T item)
        {
            yield return item;
        }

        public static IEnumerable<object> ToEnumerable(this ITuple tuple)
        {
            for (var i = 0; i < tuple.Length; ++i) yield return tuple[i];
        }

        public static IEnumerable<T> ToEnumerable<T>(this ITuple tuple)
        {
            for (var i = 0; i < tuple.Length; ++i) yield return (T)tuple[i];
        }

        public static IEnumerable<T> Repeat<T>(this T item, int count)
        {
            while (count-- > 0) yield return item;
        }

        public static IEnumerable<T> Cycle<T>(this IEnumerable<T> enumerable)
        {
            var cache = new List<T>();
            foreach (var item in enumerable)
            {
                yield return item;
                cache.Add(item);
            }
            while (cache.Count > 0)
                foreach (var item in cache)
                    yield return item;
        }

        public static (List<T> Matches, List<T> Mismatches) Split<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
        {
            return enumerable.Aggregate((Matches: new List<T>(), Mismatches: new List<T>()),
                (lists, item) =>
                {
                    if (predicate(item))
                        lists.Matches.Add(item);
                    else
                        lists.Mismatches.Add(item);
                    return lists;
                });
        }
    }
}