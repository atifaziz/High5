#region Copyright (c) 2017 Atif Aziz, Adrian Guerra
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

    /// <summary>
    /// Implementation of a dynamic array that can grow like a list and
    /// supports sparse population.
    /// </summary>
    /// <remarks>
    /// The array does not allocate sparsely when being populated sparsely.
    /// Sparse population here means that any index can be assigned to and
    /// the array grows automatically in length to accommodate the index.
    /// Unassigned indexes will throw if accessed unless
    /// <see cref="TryIndex"/> is used. During enumeration, unassigned
    /// indexes are skipped.
    /// </remarks>

    sealed class Array<T> : IEnumerable<KeyValuePair<int, T>>
    {
        T[] _array;
        BitArray _bitmap;
        int _version;

        public Array() : this(0) {}

        public Array(int capacity) =>
            Capacity = capacity;

        /// <summary>
        /// The length of the array.
        /// </summary>

        public int Length { get; private set; }

        /// <summary>
        /// Gets or sets physically allocated length of the array storage.
        /// </summary>

        public int Capacity
        {
            get => _array?.Length ?? 0;
            set => Resize(value);
        }

        /// <summary>
        /// The count of items held by the array.
        /// </summary>

        public int Cardinality
        {
            get
            {
                // very poor man's slow implementation for now.

                var count = 0;
                for (var i = 0; i < Length; i++)
                    count += _bitmap[i] ? 1 : 0;
                return count;
            }
        }

        void OnChanging() => _version++;

        public T this[int index]
        {
            get => index < 0 || index >= Length
                ? throw new ArgumentOutOfRangeException(nameof(index))
                : !_bitmap[index]
                    ? throw new InvalidOperationException()
                    : _array[index];
            set
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));

                OnChanging();
                if (index >= Capacity)
                    Resize(Math.Max(Math.Max(4, Capacity * 2), index + 1));
                _array[index] = value;
                _bitmap[index] = true;
                Length = Math.Max(index + 1, Length);
            }
        }

        public void Delete(int index)
        {
            if (index >= Length)
                return;
            OnChanging();
            _bitmap[index] = false;
            _array[index] = default(T);
        }

        public bool TryIndex(int index, out T value)
        {
            if (index >= Length || !_bitmap[index])
            {
                value = default(T);
                return false;
            }
            value = _array[index];
            return true;
        }

        public void Add(T value) => this[Length] = value;

        public void Resize(int desiredSize)
        {
            if (Capacity == desiredSize && _array != null)
                return;
            var capacity = Math.Max(desiredSize, Capacity);
            Array.Resize(ref _array, capacity);
            (_bitmap ?? (_bitmap = new BitArray(capacity))).Length = capacity;
        }

        public void Clear()
        {
            if (Capacity == 0)
                return;
            OnChanging();
            Length = 0;
            Array.Clear(_array, 0, _array.Length);
            _bitmap.SetAll(false);
        }

        Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<KeyValuePair<int, T>> IEnumerable<KeyValuePair<int, T>>.GetEnumerator() =>
                GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Init(int length)
        {
            OnChanging();
            Capacity = length;
            Array.Clear(_array, 0, _array.Length);
            for (var i = 0; i < length; i++)
                _bitmap.Set(i, true);
            Length = length;
        }

        public ArraySegment<T> ToArraySegment() =>
            new ArraySegment<T>(_array, 0, Length);

        public struct Enumerator : IEnumerator<KeyValuePair<int, T>>
        {
            Array<T> _array;
            readonly int _version;
            int _index;
            T _item;

            public Enumerator(Array<T> array)
            {
                _array = array;
                _version = array._version;
                _index = -1;
                _item = default(T);
            }

            Enumerator This =>
                _array == null
                    ? throw new ObjectDisposedException(nameof(Enumerator))
                    : this;

            public bool MoveNext() => MoveNext(This);

            static bool MoveNext(Enumerator that)
            {
                if (that._version != that._array._version)
                    throw new InvalidOperationException("Array was modified during its enumeration.");
                var length = that._array.Length;
                for (var i = that._index + 1; i < length; i++)
                {
                    if (!that._array.TryIndex(i, out var value))
                        continue;
                    that._index = i;
                    that._item = value;
                    return true;
                }
                return false;
            }

            public void Reset() => throw new NotImplementedException();

            public KeyValuePair<int, T> Current => GetCurrent(This);

            static KeyValuePair<int, T> GetCurrent(Enumerator that) =>
                new KeyValuePair<int, T>(that._index, that._item);

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                if (_array == null)
                    return;
                _array = null;
                _item = default(T);
            }
        }
    }
}