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

namespace High5
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public struct ReadOnlyCollection<T> : IList<T>, IEquatable<ReadOnlyCollection<T>>
    {
        IList<T> _list;

        IList<T> List => _list ?? EmptyArray<T>.Value;

        public T this[int index] => List[index];
        public IEnumerator<T> GetEnumerator() => List.GetEnumerator();
        public bool Contains(T item) => List.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => List.CopyTo(array, arrayIndex);
        public int Count => List.Count;
        public int IndexOf(T item) => List.IndexOf(item);

        T IList<T>.this[int index]
        {
            get => List[index];
            set => throw ReadOnlyError();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Internal members for initialization

        void OnWriting()
        {
            if (_list == null)
                _list = new List<T>();
        }

        internal void Add(T item)
        {
            OnWriting();
            _list.Add(item);
        }

        internal void Insert(int index, T item)
        {
            OnWriting();
            _list.Insert(index, item);
        }

        internal void RemoveAt(int index)
        {
            OnWriting();
            _list.RemoveAt(index);
        }

        // Unsupported publicly accessible mutating members

        static NotSupportedException ReadOnlyError() =>
            new NotSupportedException("Collection is read-only.");

        void ICollection<T>.Add(T item)         => throw ReadOnlyError();
        void ICollection<T>.Clear()             => throw ReadOnlyError();
        bool ICollection<T>.Remove(T item)      => throw ReadOnlyError();
        bool ICollection<T>.IsReadOnly          => throw ReadOnlyError();
        void IList<T>.Insert(int index, T item) => throw ReadOnlyError();
        void IList<T>.RemoveAt(int index)       => throw ReadOnlyError();

        // Equality

        public override bool Equals(object obj) =>
            obj is ReadOnlyCollection<T> collection && Equals(collection);

        public bool Equals(ReadOnlyCollection<T> other) =>
            _list == other._list ;

        public override int GetHashCode() => List.GetHashCode();
    }
}
