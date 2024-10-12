namespace GameFoundation.Scripts.Utilities.Extension
{
    using System;
    using System.Buffers;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Utilities.Utils;

    public static class ListExtensions
    {
        /// <summary>Add to the hashset if it's not null.</summary>
        public static void AddNonNull<T>(this ISet<T> set, T val)
        {
            if (val != null) set.Add(val);
        }

        /// <summary>Add to the dictionary if the key is not null.</summary>
        public static void AddNonNullKey<TK, TV>(this IDictionary<TK, TV> dict, TK key, TV val)
        {
            if (key != null) dict.Add(key, val);
        }

        public static T GetOrDefault<TK, T>(this IDictionary<TK, T> dictionary, TK key)
        {
            if (key == null) return default;
            return dictionary.ContainsKey(key) ? dictionary[key] : default;
        }

        public static (List<T> passes, List<T> fails) SplitOnPredicate<T>(
            this IEnumerable<T> list,
            Func<T, bool>       predicate
        )
        {
            var passes = new List<T>();
            var fails  = new List<T>();
            foreach (var entry in list)
            {
                if (predicate(entry))
                    passes.Add(entry);
                else
                    fails.Add(entry);
            }
            return (passes, fails);
        }

        public static IEnumerable<T> EveryNthElement<T>(this IEnumerable<T> list, int n)
        {
            return list.Where((x, i) => i % n == 0);
        }

        public static IEnumerable<Type> Types<T>(this IEnumerable<T> set) where T : notnull
        {
            return set.Select(x => x.GetType());
        }

        public static T GetAtIndexOrDefault<T>(this IList<T> list, int index)
        {
            if (index < 0 || index >= list.Count) return default!;
            return list[index];
        }

        public static T GetAtIndexOrDefault<T>(this T[] array, int index)
        {
            if (index < 0 || index >= array.Length) return default!;
            return array[index];
        }

        public static int IndexOf<T>(this IEnumerable<T> list, Predicate<T> test)
        {
            var index = 0;
            foreach (var entry in list)
            {
                if (test(entry)) return index;
                index++;
            }

            return -1;
        }

#nullable disable
        public static (bool, T) AllSame<T>(this IEnumerable<T> set)
        {
            var enumerable = set as T[] ?? set.ToArray();
            if (!enumerable.Any()) return (false, default);
            var val = enumerable.First();
            if (enumerable.All(x => EqualityComparer<T>.Default.Equals(x, val))) return (true, val);
            return (false, default);
        }
#nullable enable

        public static IList<T> Clone<T>(this IList<T> listToClone)
            where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
        }

        //If the enumerable is less than the given length, add default entries at the end to make it reach that length.
        public static IEnumerable<T> EnsureMinLength<T>(this IEnumerable<T> list, int len) where T : struct
        {
            var count = 0;
            foreach (var entry in list)
            {
                yield return entry;
                count++;
            }

            while (count < len)
            {
                yield return default;
                count++;
            }
        }

        public static int Count(this IEnumerable source)
        {
            var col = source as ICollection;
            if (col != null) return col.Count;

            var c = 0;
            var e = source.GetEnumerator();
            while (e.MoveNext()) c++;
            return c;
        }

        public static int FirstIndex<T>(this IList<T> list, Predicate<T> func)
        {
            var index = 0;
            foreach (var item in list)
            {
                if (func(item)) return index;
                index++;
            }

            return -1;
        }

        public static T GetOrAdd<T>(this IList<T> list, Func<T, bool> match) where T : new()
        {
            return GetOrAdd(list, match, () => new());
        }

        public static T GetOrAdd<T>(this IList<T> list, Func<T, bool> match, Func<T> create)
        {
            var entry = list.FirstOrDefault(match);
            if (entry != null) return entry;

            var item = create();
            list.Add(item);
            return item;
        }

        public static T Random<T>(this IList<T> list, Random random)
        {
            return list[random.Next(0, list.Count)];
        }

        public static T MinObj<T>(this IList<T> list, Func<T, float> distanceFunc)
        {
            var min     = float.MaxValue;
            var closest = default(T);
            foreach (var item in list)
            {
                var dist = distanceFunc(item);
                if (dist < min || closest == null)
                {
                    closest = item;
                    min     = dist;
                }
            }

            return closest!;
        }

        public static T MaxObj<T>(this IList<T> list, Func<T, double> distanceFunc)
        {
            var max     = double.MinValue;
            var closest = default(T);
            foreach (var item in list)
            {
                var dist = distanceFunc(item);
                if (dist > max || closest == null)
                {
                    closest = item;
                    max     = dist;
                }
            }

            return closest!;
        }

        public static T MaxObj<T>(this IList<T> list, Func<T, float> distanceFunc)
        {
            var max     = float.MinValue;
            var closest = default(T)!;
            foreach (var item in list)
            {
                var dist = distanceFunc(item);
                if (dist > max || closest == null)
                {
                    closest = item;
                    max     = dist;
                }
            }

            return closest;
        }

        public static T MinIndex<T>(this IList<T> list)
            where T : IComparable
        {
            var lowest      = default(T);
            var lowestIndex = -1;
            var index       = 0;
            foreach (var item in list)
            {
                if (lowestIndex < 0 || item.CompareTo(lowest) < 0)
                {
                    lowestIndex = index;
                    lowest      = item;
                }

                index++;
            }

            return lowest!;
        }

        public static int MinIndex<T>(this IList<T> list, IComparer<T> comparer)
        {
            if (list.Count == 0) return -1;

            var lowestIndex = 0;
            var lowest      = list[0];
            for (var i = 1; i < list.Count; i++)
            {
                var item = list[i];
                if (comparer.Compare(item, lowest) < 0)
                {
                    lowestIndex = i;
                    lowest      = item;
                }
            }

            return lowestIndex;
        }

        public static void ForEachIndex<T>(this IList<T> list, Action<T, int> handler)
        {
            var idx = 0;
            foreach (var item in list) handler(item, idx++);
        }

        public static void ForEach<T>(this IList<T> list, Action<T> action)
        {
            foreach (var item in list) action(item);
        }

        public static bool AddOnce<T>(this IList<T> list, T entry)
        {
            if (list.Contains(entry)) return false;
            list.Add(entry);
            return true;
        }

        /// <summary>Removes first item matching the <paramref name="matcher" />. Returns <c>true</c> if item was removed.</summary>
        public static bool RemoveFirst<T>(this IList<T> list, Predicate<T> matcher)
        {
            var index = list.FirstIndex(matcher);
            if (index < 0) return false;
            list.RemoveAt(index);
            return true;
        }

        /// <summary>
        ///     Replaces first element matching the <paramref name="matcher" /> with <paramref name="item" /> or adds new
        ///     <paramref name="item" /> if no one matched.
        /// </summary>
        public static void AddOrReplace<T>(this IList<T> list, T item, Predicate<T> matcher)
        {
            var index = list.FirstIndex(matcher);
            if (index >= 0)
                list[index] = item;
            else
                list.Add(item);
        }

        //Takes two lists and calls a function passing the entry at the same index in each. If a list has more entries than the other, the extras are ignored.
        public static void ZipApply<T>(this IEnumerable<T> left, IEnumerable<T> right, Action<T, T> zipFunc)
        {
            using var leftE  = left.GetEnumerator();
            using var rightE = right.GetEnumerator();
            while (leftE.MoveNext() && rightE.MoveNext()) zipFunc.Invoke(leftE.Current, rightE.Current);
        }

        /// <summary>
        ///     Returns first item of type <typeparamref name="T" /> or default value if not found. It is a list specific
        ///     implementation to avoid GC-allocations.
        /// </summary>
        public static T FirstOfTypeOrDefault<TElement, T>(this List<TElement> list) where T : TElement
        {
            foreach (var item in list)
                if (item is T ofType)
                    return ofType;

            return default!;
        }

        /// <summary>
        ///     Returns median value for <paramref name="list" />. If <paramref name="sorted" /> is <c>false</c> then it
        ///     makes sorted copy of list to find the median otherwise it just returns medium element.
        /// </summary>
        public static T Median<T>(this IList<T> list, bool sorted = false) where T : IComparable<T>
        {
            if (list.Count == 0) return default!;

            var mid = list.Count / 2;
            T   result;
            if (!sorted)
            {
                var pool = ArrayPool<T>.Shared;
                var tmp  = pool.Rent(list.Count);
                Array.Sort(tmp, 0, list.Count);
                Array.Sort(tmp);
                result = tmp[mid];
                pool.Return(tmp);
            }
            else
            {
                result = list[mid];
            }

            return result;
        }

        /// <summary>Returns an enumerable of each index that has a null value in the passed list.</summary>
        public static IEnumerable<int> NullIndices<T>(this IEnumerable<T> list)
        {
            var i = 0;
            foreach (var item in list)
            {
                if (item == null) yield return i;
                i++;
            }
        }
    }

    public static class StackExtensions
    {
        public static void PushRange<T>(this Stack<T> source, IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable) source.Push(item);
        }

        public static void PushRange<T>(this Stack<T> source, List<T> list)
        {
            foreach (var item in list) source.Push(item);
        }

        public static void PushRange<T>(this Stack<T> source, T[] array)
        {
            foreach (var item in array) source.Push(item);
        }
    }

    public static class ArrayExtensions
    {
        public static IEnumerable<T> XYIter<T>(this T[,] array)
        {
            for (var row = 0; row < array.GetLength(0); row++)
            for (var col = 0; col < array.GetLength(1); col++)
                yield return array[row, col];
        }

        /// <summary>
        ///     Pass forward sorted array for large middle numbers for example {1,2,3,4,5} would be {1,3,5,4,2}, or reverse
        ///     sorted array for large side numbers for example {5,4,3,2,1} would be {5,3,1,2,4}, this is useful for aligning trees
        ///     based on depth, this is a special iterator that jumps through enumerable check this explanation:
        ///     https://stackoverflow.com/a/3796619
        /// </summary>
        /// <example>
        ///     One of the cases this is used on is the tech tree visualizer where nodes with higher total rank go in the top and
        ///     bottom for a better aethestic and line drawing:
        ///     <code>listToOrder.OrderByDescending(tree => GetTotalRank(tree)).ToArray().CurveOrder();</code>
        /// </example>
        public static IEnumerable<T> CurveOrder<T>(this T[] listToOrder)
        {
            if (listToOrder.Length == 0) yield break; // Nothing to do.

            // Move forward every two.
            for (var i = 0; i < listToOrder.Length; i += 2) yield return listToOrder[i];

            // Move backward every other two. Note: Length%2 makes sure we're on the correct offset.
            for (var i = listToOrder.Length - 1 - listToOrder.Length % 2; i >= 0; i -= 2) yield return listToOrder[i];
        }

        public static T MinObj<T>(this T[] enumeration, Func<T, float> distanceFunc)
        {
            var min     = float.MaxValue;
            var closest = default(T)!;
            foreach (var item in enumeration)
            {
                var dist = distanceFunc(item);
                if (dist < min || closest == null)
                {
                    closest = item;
                    min     = dist;
                }
            }

            return closest;
        }

        public static T MaxObj<T>(this T[] array, Func<T, double> distanceFunc)
        {
            var max     = double.MinValue;
            var closest = default(T)!;
            foreach (var item in array)
            {
                var dist = distanceFunc(item);
                if (dist > max || closest == null)
                {
                    closest = item;
                    max     = dist;
                }
            }

            return closest;
        }

        public static T MaxObj<T>(this T[] array, Func<T, float> distanceFunc)
        {
            var max     = float.MinValue;
            var closest = default(T)!;
            foreach (var item in array)
            {
                var dist = distanceFunc(item);
                if (dist > max || closest == null)
                {
                    closest = item;
                    max     = dist;
                }
            }

            return closest;
        }

        public static T MinIndex<T>(this T[] array)
            where T : IComparable
        {
            var lowest      = default(T)!;
            var lowestIndex = -1;
            var index       = 0;
            foreach (var item in array)
            {
                if (lowestIndex < 0 || item.CompareTo(lowest) < 0)
                {
                    lowestIndex = index;
                    lowest      = item;
                }

                index++;
            }

            return lowest;
        }

        public static void ForEachIndex<T>(this T[] array, Action<T, int> handler)
        {
            var idx = 0;
            foreach (var item in array) handler(item, idx++);
        }

        public static int GetArrayHashCode<T>(this T[] array) where T : notnull
        {
            unchecked
            {
                var code                                    = array.Length;
                var equalityComparer                        = EqualityComparer<T>.Default;
                for (var i = 0; i < array.Length; i++) code = code * 314159 + equalityComparer.GetHashCode(array[i]);
                return code;
            }
        }

        public static bool ArrayEquals<T>(this T[] array, T[] otherArray)
        {
            if (array.Length != otherArray.Length) return false;

            var equalityComparer = EqualityComparer<T>.Default;
            for (var i = 0; i < array.Length; i++)
                if (!equalityComparer.Equals(array[i], otherArray[i]))
                    return false;

            return true;
        }
    }

    public static class QueueExtensions
    {
        /// <summary>Deque a given number of elements into a list.</summary>
        public static List<T> DequeueIntoList<T>(this Queue<T> queue, int count)
        {
            var list = new List<T>();
            for (var i = 0; i < count; i++) list.Add(queue.Dequeue());
            return list;
        }

        public static void AddRange<T>(this Queue<T> queue, IEnumerable<T> enu)
        {
            foreach (var obj in enu) queue.Enqueue(obj);
        }

        /// <summary>
        ///     Optimized non-alloc version of
        ///     <see cref="AddRange{T}(System.Collections.Generic.Queue{T},System.Collections.Generic.IEnumerable{T})" />.
        ///     Because <see cref="List{T}.GetEnumerator" /> returns struct <see cref="List{T}.Enumerator" />, but
        ///     <see cref="IEnumerable{T}.GetEnumerator" /> returns <see cref="IEnumerator{T}" /> as boxed version of
        ///     <see cref="List{T}.Enumerator" />.
        /// </summary>
        /// <param name="queue">destination queue.</param>
        /// <param name="list">list to add.</param>
        /// <typeparam name="T">item type.</typeparam>
        public static void AddRange<T>(this Queue<T> queue, List<T> list)
        {
            foreach (var obj in list) queue.Enqueue(obj);
        }

        #if NETSTANDARD2_0
        /// <summary> Make Queue.TryDequeue .NET Core 2.1 available in .NET Core 2.0. </summary>
        [PublicAPI] public static bool TryDequeue<T>(this Queue<T> queue, [MaybeNullWhen(false)] out T value)
        {
            if (queue.Count == 0)
            {
                value = default;
                return false;
            }

            value = queue.Dequeue();
            return true;
        }
        #endif
    }

    public static class CollectionExtension
    {
        public static void InsertAtIndex<T>(this IList<T> list, int index, T val)
        {
            while (index >= ListExtensions.Count(list)) list.Add(default!);
            list[index] = val;
        }

        public static bool AddUnique<T>(this ICollection<T> col, T item)
        {
            if (col.Contains(item)) return false;
            col.Add(item);
            return true;
        }

        public static bool ContainsAll<T>(this ICollection<T> source, IEnumerable<T> values)
        {
            return values.All(source.Contains);
        }

        /// <summary> Checks if <paramref name="source" /> collection contains any value from <paramref name="values" />. </summary>
        public static bool ContainsAny<T>(this ICollection<T> source, IEnumerable<T> values)
        {
            return values.Any(source.Contains);
        }

        public static T IndexOrDefault<T>(this ICollection<T> list, int index)
        {
            return (list.HasIndex(index) ? list.ElementAt(index) : default)!;
        }

        public static T IndexOrDefault<T>(this ICollection<T> list, int index, T defaultVal)
        {
            return list.HasIndex(index) ? list.ElementAt(index) : defaultVal;
        }

        public static bool HasIndex<T>(this ICollection<T> list, int index)
        {
            return !(index < 0 || index >= list.Count);
        }

        /// <summary>
        ///     Adds element to <paramref name="collection" /> only if it isn't null. Returns <c>true</c> if element was
        ///     added.
        /// </summary>
        public static bool AddNotNull<T>(this ICollection<T> collection, T? element)
        {
            if (element == null) return false;
            collection.Add(element);
            return true;
        }
    }

    public static class ArrayUtil
    {
        /// <summary>Returns the columns of the given row index</summary>
        public static IEnumerable<T> ValuesAtX<T>(this T[,] array, int x)
        {
            for (var i = 0; i < array.GetLength(1); i++) yield return array[x, i];
        }

        /// <summary>Returns the rows of the given column index</summary>
        public static IEnumerable<T> ValuesAtY<T>(this T[,] array, int y)
        {
            for (var i = 0; i < array.GetLength(0); i++) yield return array[i, y];
        }

        /// <summary>Assigns the given value to the whole row</summary>
        public static void SetValuesAtX<T>(this T[,] array, int x, T val)
        {
            for (var i = 0; i < array.GetLength(1); i++) array[x, i] = val;
        }

        /// <summary>Assigns the given value to the whole column</summary>
        public static void SetValuesAtY<T>(this T[,] array, int y, T val)
        {
            for (var i = 0; i < array.GetLength(0); i++) array[i, y] = val;
        }

        public static T SecondLast<T>(this T[] array)
        {
            return array[array.Length - 1];
        }

        /// <summary> Make an array and fill it with 'new'd entries. </summary>
        public static T[] MakeNew<T>(int count) where T : new()
        {
            var array                                = new T[count];
            for (var i = 0; i < count; i++) array[i] = new();
            return array;
        }
    }

    public static class ListUtil
    {
        //Return entries until finding one that has a different testsame value.
        public static IEnumerable<TEntry> TakeSame<TEntry, TVal>(
            this IEnumerable<TEntry> list,
            Func<TEntry, TVal>       testSame
        ) where TVal : IComparable
        {
            var firstVal = default(TVal);
            var set      = false;
            foreach (var entry in list)
            {
                var entryVal = testSame(entry);
                if (!set)
                    firstVal = entryVal;
                else if (firstVal?.CompareTo(entryVal) != 0) yield break;

                yield return entry;
            }
        }

        public static bool IsSortedAscending<T>(this IEnumerable<T> list) where T : IComparable
        {
            var last = default(T);
            foreach (var entry in list)
            {
                if (last != null)
                    if (entry.CompareTo(last) > 0)
                        return false;
                last = entry;
            }

            return true;
        }

        public static IEnumerable<(T Prev, T Current)> GetPrevAndCurrentPairs<T>(this IEnumerable<T> list)
        {
            T prev = default!;
            foreach (var entry in list)
            {
                yield return (prev, entry);
                prev = entry;
            }
        }

        //Return each node and the level, with root = 0
        public static IEnumerable<(T Node, int Level)> DepthFirstTraversal<T>(
            this T                  root,
            Func<T, IEnumerable<T>> getChildren
        )
        {
            var s = new Stack<(T Node, int Level)>();
            s.Push((root, 0));

            while (s.Count > 0)
            {
                var entry = s.Pop();
                yield return entry;

                var children = getChildren(entry.Node);
                if (children == null) continue;
                foreach (var child in children.Reverse()) s.Push((child, entry.Level + 1));
            }
        }

        //Syntactic sugary deliciousness
        public static IEnumerable<T> List<T>(params T[] list)
        {
            foreach (var entry in list) yield return entry;
        }

        public static IEnumerable<T> NonNull<T>(params T[] list)
        {
            foreach (var entry in list)
                if (entry != null)
                    yield return entry;
        }
    }

    public static class SetExtensions
    {
        public static void AddRange<T>(this ISet<T> set, IEnumerable<T> items)
        {
            foreach (var item in items) set.Add(item);
        }

        public static void RemoveRange<T>(this ISet<T> set, IEnumerable<T> items)
        {
            foreach (var item in items) set.Remove(item);
        }

        public static HashSet<T> MakeHashSet<T>(params IEnumerable<T>[] lists)
        {
            var h = new HashSet<T>();
            foreach (var entry in lists) h.UnionWith(entry);
            return h;
        }

        /// <summary> No alloc version of Enumerable.First() for HashSet. </summary>
        /// <exception cref="InvalidOperationException">If no elements in HashSet.</exception>
        public static T First<T>(this HashSet<T> set)
        {
            using (var enumerator = set.GetEnumerator())
            {
                if (!enumerator.MoveNext()) throw new InvalidOperationException("No elements.");
                return enumerator.Current;
            }
        }

        /// <summary>
        ///     Returns the passed enumerable omitting any entries repeated in the sequence. Only checks sequential repeats,
        ///     not list-wide repeats (like 'Distinct()' would).
        /// </summary>
        public static IEnumerable<T> IgnoreRepeats<T>(this IEnumerable<T> list) where T : IEquatable<T>
        {
            var last = default(T);
            foreach (var entry in list)
            {
                if (last == null || !entry.Equals(last)) yield return entry;
                last = entry;
            }
        }

        /// <summary>
        ///     Get the element at the given index, or null. Note that this is O(N), and should only be used when generic
        ///     version of IEnumerable isnt available.
        /// </summary>
        public static object? EnumerableElementAt(this IEnumerable list, int index)
        {
            var i = 0;
            foreach (var entry in list)
            {
                if (index == i) return entry;
                i++;
            }

            return null;
        }

        /// <summary>
        ///     Return the index of the given value in a non-generic enumerable. Note that this is O(N), and should only be
        ///     used when generic version of IEnumerable isnt available.
        /// </summary>
        public static int EnumerableIndexOf(this IEnumerable list, object obj)
        {
            var i = 0;
            foreach (var entry in list)
            {
                if (entry == obj) return i;
                i++;
            }

            return -1;
        }

        //Go through the groups and take up to 'getCountAllowed' entries from each, running the function on the first value.
        public static IEnumerable<TValue> LimitGroups<TKey, TValue>(
            this IEnumerable<IGrouping<TKey, TValue>> groups,
            Func<TValue, int>                         getCountAllowed
        )
        {
            foreach (var group in groups)
            {
                var limit = getCountAllowed(group.First());
                foreach (var entry in group.Take(limit)) yield return entry;
            }
        }
    }

    public static class DictionaryExtensions
    {
        public static T GetOrAdd<T, TK>(this IDictionary<TK, T> dictionary, TK key, Func<T> valueFunc)
        {
            if (dictionary.ContainsKey(key)) return dictionary[key];
            dictionary.Add(key, valueFunc());
            return dictionary[key];
        }

        public static async UniTask<TValue> GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<UniTask<TValue>> valueFunc)
        {
            if (!dictionary.ContainsKey(key)) dictionary.Add(key, await valueFunc());
            return dictionary[key];
        }
    }

    public static class EnumEnumerableExtensions
    {
        public static void ForEachWithoutExclusions<TEnum>(
            this IEnumerable<TEnum> enums,
            Action<TEnum>           executionPerItem
        )
            where TEnum : Enum
        {
            var excludeAttribute = (EnumEnumerableExcludeAttribute)(Attribute.GetCustomAttribute(typeof(TEnum),
                    typeof(EnumEnumerableExcludeAttribute))
                ?? new EnumEnumerableExcludeAttribute());

            foreach (var enumValue in enums)
            {
                if (excludeAttribute.IsExcluded(enumValue)) continue;

                executionPerItem.Invoke(enumValue);
            }
        }

        public static IEnumerable<TEnum> WithoutExclusions<TEnum, TResult>(
            this IEnumerable<TEnum> enums,
            Func<TEnum, TResult>    selector
        )
            where TEnum : Enum
        {
            var excludeAttribute = (EnumEnumerableExcludeAttribute)(Attribute.GetCustomAttribute(typeof(TEnum),
                    typeof(EnumEnumerableExcludeAttribute))
                ?? new EnumEnumerableExcludeAttribute());

            return enums.Where(enumValue => !excludeAttribute.IsExcluded(enumValue));
        }
    }
}