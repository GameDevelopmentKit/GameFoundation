namespace GameFoundation.Scripts.Utilities.Extension
{
    using System;
    using System.Collections.Generic;

    public sealed class PriorityQueue<TElement, TPriority>
    {
        private readonly SortedList<TPriority, TElement> items;

        public PriorityQueue() : this(Comparer<TPriority>.Default)
        {
        }

        public PriorityQueue(Comparison<TPriority> comparison) : this(Comparer<TPriority>.Create(comparison))
        {
        }

        public PriorityQueue(IComparer<TPriority> comparer)
        {
            this.items = new(Comparer<TPriority>.Create((i1, i2) =>
            {
                var result = comparer.Compare(i1, i2);
                return result != 0 ? result : 1;
            }));
        }

        public int Count => this.items.Count;

        public void Enqueue(TElement element, TPriority priority)
        {
            this.items.Add(priority, element);
        }

        public TElement Dequeue()
        {
            var result = this.items.Values[this.items.Count - 1];
            this.items.RemoveAt(this.items.Count - 1);
            return result;
        }

        public TElement Peek()
        {
            return this.items.Values[this.items.Count - 1];
        }
    }
}