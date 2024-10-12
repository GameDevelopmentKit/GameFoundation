namespace GameFoundation.Scripts.Utilities.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class TupleExtensions
    {
        public static (TFirst, TSecond) First<TFirst, TSecond>(this IEnumerable<(TFirst, TSecond)> tuples, Func<TFirst, TSecond, bool> predicate)
        {
            return tuples.First(tuple => predicate(tuple.Item1, tuple.Item2));
        }

        public static (TFirst, TSecond) FirstOrDefault<TFirst, TSecond>(this IEnumerable<(TFirst, TSecond)> tuples, Func<TFirst, TSecond, bool> predicate)
        {
            return tuples.FirstOrDefault(tuple => predicate(tuple.Item1, tuple.Item2));
        }

        public static (TFirst, TSecond) Last<TFirst, TSecond>(this IEnumerable<(TFirst, TSecond)> tuples, Func<TFirst, TSecond, bool> predicate)
        {
            return tuples.Last(tuple => predicate(tuple.Item1, tuple.Item2));
        }

        public static (TFirst, TSecond) LastOrDefault<TFirst, TSecond>(this IEnumerable<(TFirst, TSecond)> tuples, Func<TFirst, TSecond, bool> predicate)
        {
            return tuples.LastOrDefault(tuple => predicate(tuple.Item1, tuple.Item2));
        }

        public static (TFirst, TSecond) Single<TFirst, TSecond>(this IEnumerable<(TFirst, TSecond)> tuples, Func<TFirst, TSecond, bool> predicate)
        {
            return tuples.Single(tuple => predicate(tuple.Item1, tuple.Item2));
        }

        public static (TFirst, TSecond) SingleOrDefault<TFirst, TSecond>(this IEnumerable<(TFirst, TSecond)> tuples, Func<TFirst, TSecond, bool> predicate)
        {
            return tuples.SingleOrDefault(tuple => predicate(tuple.Item1, tuple.Item2));
        }

        public static IEnumerable<(TFirst, TSecond)> Where<TFirst, TSecond>(this IEnumerable<(TFirst, TSecond)> tuples, Func<TFirst, TSecond, bool> predicate)
        {
            return tuples.Where(tuple => predicate(tuple.Item1, tuple.Item2));
        }

        public static IEnumerable<TResult> Select<TFirst, TSecond, TResult>(this IEnumerable<(TFirst, TSecond)> tuples, Func<TFirst, TSecond, TResult> selector)
        {
            return tuples.Select(tuple => selector(tuple.Item1, tuple.Item2));
        }

        public static IEnumerable<TResult> SelectMany<TFirst, TSecond, TResult>(this IEnumerable<(TFirst, TSecond)> tuples, Func<TFirst, TSecond, IEnumerable<TResult>> selector)
        {
            return tuples.SelectMany(tuple => selector(tuple.Item1, tuple.Item2));
        }

        public static IEnumerable<IGrouping<TKey, (TFirst, TSecond)>> GroupBy<TFirst, TSecond, TKey>(this IEnumerable<(TFirst, TSecond)> tuples, Func<TFirst, TSecond, TKey> keySelector)
        {
            return tuples.GroupBy(tuple => keySelector(tuple.Item1, tuple.Item2));
        }

        public static IOrderedEnumerable<(TFirst, TSecond)> OrderBy<TFirst, TSecond, TKey>(this IEnumerable<(TFirst, TSecond)> tuples, Func<TFirst, TSecond, TKey> keySelector)
        {
            return tuples.OrderBy(tuple => keySelector(tuple.Item1, tuple.Item2));
        }

        public static TResult Aggregate<TFirst, TSecond, TResult>(this IEnumerable<(TFirst, TSecond)> tuples, TResult seed, Func<TResult, TFirst, TSecond, TResult> func)
        {
            return tuples.Aggregate(seed, (current, tuple) => func(current, tuple.Item1, tuple.Item2));
        }

        public static TResult Min<TFirst, TSecond, TResult>(this IEnumerable<(TFirst, TSecond)> tuples, Func<TFirst, TSecond, TResult> selector)
        {
            return tuples.Min(tuple => selector(tuple.Item1, tuple.Item2));
        }

        public static TResult Max<TFirst, TSecond, TResult>(this IEnumerable<(TFirst, TSecond)> tuples, Func<TFirst, TSecond, TResult> selector)
        {
            return tuples.Max(tuple => selector(tuple.Item1, tuple.Item2));
        }

        public static bool Any<TFirst, TSecond>(this IEnumerable<(TFirst, TSecond)> tuples, Func<TFirst, TSecond, bool> predicate)
        {
            return tuples.Any(tuple => predicate(tuple.Item1, tuple.Item2));
        }

        public static bool All<TFirst, TSecond>(this IEnumerable<(TFirst, TSecond)> tuples, Func<TFirst, TSecond, bool> predicate)
        {
            return tuples.All(tuple => predicate(tuple.Item1, tuple.Item2));
        }

        public static void ForEach<TFirst, TSecond>(this IEnumerable<(TFirst, TSecond)> tuples, Action<TFirst, TSecond> action)
        {
            tuples.ForEach(tuple => action(tuple.Item1, tuple.Item2));
        }

        public static void SafeForEach<TFirst, TSecond>(this IEnumerable<(TFirst, TSecond)> tuples, Action<TFirst, TSecond> action)
        {
            tuples.SafeForEach(tuple => action(tuple.Item1, tuple.Item2));
        }

        public static Dictionary<TFirst, TSecond> ToDictionary<TFirst, TSecond>(this IEnumerable<(TFirst, TSecond)> tuples)
        {
            return tuples.ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
        }

        public static (List<TFirst>, List<TSecond>) Unzip<TFirst, TSecond>(this IEnumerable<(TFirst, TSecond)> tuples)
        {
            return tuples.Aggregate((new List<TFirst>(), new List<TSecond>()),
                (lists, tuple) =>
                {
                    lists.Item1.Add(tuple.Item1);
                    lists.Item2.Add(tuple.Item2);
                    return lists;
                });
        }

        public static (List<(TFirst, TSecond)> Matches, List<(TFirst, TSecond)> Mismatches) Split<TFirst, TSecond>(this IEnumerable<(TFirst, TSecond)> tuples, Func<TFirst, TSecond, bool> predicate)
        {
            return tuples.Split(tuple => predicate(tuple.Item1, tuple.Item2));
        }
    }
}