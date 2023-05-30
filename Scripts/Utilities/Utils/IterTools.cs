namespace GameFoundation.Scripts.Utilities.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Sirenix.Utilities;

    public static class IterTools
    {
        public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
        {
            using var e1 = first.GetEnumerator();
            using var e2 = second.GetEnumerator();
            while (e1.MoveNext() && e2.MoveNext())
            {
                yield return resultSelector(e1.Current, e2.Current);
            }
        }

        public static IEnumerable<TResult> Zip<TFirst, TSecond, TThird, TResult>(IEnumerable<TFirst> first, IEnumerable<TSecond> second, IEnumerable<TThird> third, Func<TFirst, TSecond, TThird, TResult> resultSelector)
        {
            using var e1 = first.GetEnumerator();
            using var e2 = second.GetEnumerator();
            using var e3 = third.GetEnumerator();
            while (e1.MoveNext() && e2.MoveNext() && e3.MoveNext())
            {
                yield return resultSelector(e1.Current, e2.Current, e3.Current);
            }
        }

        public static IEnumerable<T[]> Zip<T>(params IEnumerable<T>[] enumerables)
        {
            var enumerators = enumerables.GetEnumerators();
            var hasNexts    = enumerators.MoveNexts();
            while (hasNexts.All(IsTrue))
            {
                yield return enumerators.GetCurrents();
                hasNexts = enumerators.MoveNexts();
            }

            enumerators.Dispose();
        }

        public static IEnumerable<TResult> ZipLongest<TFirst, TSecond, TResult>(IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
        {
            using var e1        = first.GetEnumerator();
            using var e2        = second.GetEnumerator();
            var       e1HasNext = e1.MoveNext();
            var       e2HasNext = e2.MoveNext();
            while (e1HasNext || e2HasNext)
            {
                yield return resultSelector(GetCurrentOrDefault(e1, e1HasNext), GetCurrentOrDefault(e2, e2HasNext));
                e1HasNext = e1.MoveNext();
                e2HasNext = e2.MoveNext();
            }
        }

        public static IEnumerable<TResult> ZipLongest<TFirst, TSecond, TThird, TResult>(IEnumerable<TFirst> first, IEnumerable<TSecond> second, IEnumerable<TThird> third, Func<TFirst, TSecond, TThird, TResult> resultSelector)
        {
            using var e1        = first.GetEnumerator();
            using var e2        = second.GetEnumerator();
            using var e3        = third.GetEnumerator();
            var       e1HasNext = e1.MoveNext();
            var       e2HasNext = e2.MoveNext();
            var       e3HasNext = e3.MoveNext();
            while (e1HasNext || e2HasNext || e3HasNext)
            {
                yield return resultSelector(GetCurrentOrDefault(e1, e1HasNext), GetCurrentOrDefault(e2, e2HasNext), GetCurrentOrDefault(e3, e3HasNext));
                e1HasNext = e1.MoveNext();
                e2HasNext = e2.MoveNext();
                e3HasNext = e3.MoveNext();
            }
        }

        public static IEnumerable<T[]> ZipLongest<T>(params IEnumerable<T>[] enumerables)
        {
            var enumerators = enumerables.GetEnumerators();
            var hasNexts    = enumerators.MoveNexts();
            while (hasNexts.Any(IsTrue))
            {
                yield return Enumerable.Zip(enumerators, hasNexts, GetCurrentOrDefault).ToArray();
                hasNexts = enumerators.MoveNexts();
            }

            enumerators.Dispose();
        }

        public static IEnumerable<T[]> Product<T>(params IEnumerable<T>[] enumerables)
        {
            var pool        = enumerables.Select(enumerable => enumerable.ToList()).ToArray();
            var length      = pool.Length;
            var enumerators = pool.GetEnumerators();
            if (!enumerators.MoveNexts().All(IsTrue))
            {
                enumerators.Dispose();
                yield break;
            }

            while (true)
            {
                yield return enumerators.GetCurrents();
                var index = length - 1;
                while (true)
                {
                    if (enumerators[index].MoveNext()) break;
                    enumerators[index].Dispose();
                    enumerators[index] = pool[index].GetEnumerator();
                    enumerators[index].MoveNext();
                    if (--index < 0)
                    {
                        enumerators.Dispose();
                        yield break;
                    }
                }
            }
        }

        public static IEnumerable<T[]> Product<T>(IEnumerable<T> enumerable, int repeat)
        {
            return Product(Enumerable.Repeat(enumerable, repeat).ToArray());
        }

        public static IEnumerable<T> Cycle<T>(this IEnumerable<T> enumerable, int count = -1)
        {
            if (count == 0) yield break;
            var cache = new List<T>();
            foreach (var item in enumerable)
            {
                yield return item;
                cache.Add(item);
            }

            while (--count != 0)
            {
                foreach (var item in cache)
                {
                    yield return item;
                }
            }
        }

        public static Stack<T> ToStack<T>(this IEnumerable<T> enumerable)
        {
            var stack = new Stack<T>();
            foreach (var item in enumerable)
            {
                stack.Push(item);
            }

            return stack;
        }

        public static Queue<T> ToQueue<T>(this IEnumerable<T> enumerable)
        {
            var queue = new Queue<T>();
            foreach (var item in enumerable)
            {
                queue.Enqueue(item);
            }

            return queue;
        }

        public static IEnumerable<T> Slice<T>(this IEnumerable<T> enumerable, int start, int stop, int step = 1)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                if (index >= stop) yield break;
                if (index >= start && (index - start) % step == 0) yield return item;
                ++index;
            }
        }

        public static T[,] To2DArray<T>(this T[][] source)
        {
            try
            {
                var dimension1 = source.Length;
                var dimension2 = source.GroupBy(row => row.Length).Single().Key;
                var result     = new T[dimension1, dimension2];
                for (var i = 0; i < dimension1; ++i)
                {
                    for (var j = 0; j < dimension2; ++j)
                    {
                        result[i, j] = source[i][j];
                    }
                }

                return result;
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("The given jagged array is not rectangular");
            }
        }

        private static IEnumerator<T>[] GetEnumerators<T>(this IEnumerable<IEnumerable<T>> enumerables) => enumerables.Select(e => e.GetEnumerator()).ToArray();
        private static bool[]           MoveNexts<T>(this IEnumerable<IEnumerator<T>> enumerators)      => enumerators.Select(e => e.MoveNext()).ToArray();
        private static T[]              GetCurrents<T>(this IEnumerable<IEnumerator<T>> enumerators)    => enumerators.Select(e => e.Current).ToArray();
        private static void             Dispose<T>(this IEnumerable<IEnumerator<T>> enumerators)        => enumerators.ForEach(enumerator => enumerator.Dispose());
        private static T                GetCurrentOrDefault<T>(IEnumerator<T> enumerator, bool hasNext) => hasNext ? enumerator.Current : default;
        private static T                Item<T>(T item)                                                 => item;
        private static bool             IsTrue(bool b)                                                  => b;
        private static bool             IsFalse(bool b)                                                 => !b;
    }
}