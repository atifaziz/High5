#region Copyright (c) 2017 Atif Aziz, Adrian Guerra
//
// Portions Copyright (c) 2013 Ivan Nikulin
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
#endregion

namespace High5.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Microsoft.Extensions.Internal;

    public static class ListNode
    {
        public static ListNode<T> Create<T>(T item) => new ListNode<T>(item, null);
    }

    public class ListNode<T> : IReadOnlyCollection<T>, IEquatable<ListNode<T>>
    {
        public static readonly ListNode<T> Empty = new ListNode<T>();

        public int Count { get; internal set; }
        public bool IsEmpty => Next == null;
        public ListNode<T> Next { get; }
        public T Item { get; }

        public ListNode() { }

        public ListNode(T item, ListNode<T> next)
        {
            Next = next ?? throw new ArgumentNullException(nameof(next));
            Item = item;
            Count = Next.Count + 1;
        }

        public ListNode<T> Prepend(T item) => new ListNode<T>(item, this);

        public IEnumerator<T> GetEnumerator()
        {
            for (var node = this; node != null; node = node.Next)
                yield return node.Item;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Equals(ListNode<T> other) =>
            ReferenceEquals(this, other)
            || Count == other.Count
            && EqualityComparer<T>.Default.Equals(Item, other.Item)
            && (ReferenceEquals(Next, other.Next) || Equals(Next, other.Next));

        public override bool Equals(object obj) =>
            obj is ListNode<T> list && Equals(list);

        public override int GetHashCode()
        {
            var hash = HashCodeCombiner.Start();
            hash.Add(EqualityComparer<T>.Default.GetHashCode(Item));
            hash.Add((object) Next);
            return hash.CombinedHash;
        }

        public static bool operator ==(ListNode<T> left, ListNode<T> right) =>
            Equals(left, right);

        public static bool operator !=(ListNode<T> left, ListNode<T> right) =>
            !Equals(left, right);
    }
}
