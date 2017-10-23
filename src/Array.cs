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
    /// Implementation of a dynamic array that can grow like a list.
    /// </summary>

    sealed class Array<T> : IEnumerable<T>
    {
        T[] _array;
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

        void OnChanging() => _version++;

        public T this[int index]
        {
            get => index < 0 || index >= Length
                ? throw new ArgumentOutOfRangeException(nameof(index))
                : _array[index];

            private set
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));

                OnChanging();
                if (index >= Capacity)
                    Resize(Math.Max(Math.Max(4, Capacity * 2), index + 1));
                _array[index] = value;
                Length = Math.Max(index + 1, Length);
            }
        }

        public void Add(T value) => this[Length] = value;
        public void Push(T value) => Add(value);

        public void Resize(int desiredSize)
        {
            if (Capacity == desiredSize && _array != null)
                return;
            var capacity = Math.Max(desiredSize, Capacity);
            Array.Resize(ref _array, capacity);
        }

        public void Clear()
        {
            if (Capacity == 0)
                return;
            OnChanging();
            Length = 0;
            Array.Clear(_array, 0, _array.Length);
        }

        Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Init(int length)
        {
            OnChanging();
            Capacity = length;
            Array.Clear(_array, 0, _array.Length);
            Length = length;
        }

        public ArraySegment<T> AsArraySegment() =>
            new ArraySegment<T>(_array, 0, Length);

        public struct Enumerator : IEnumerator<T>
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

            public bool MoveNext()
            {
                if (_version != _array._version)
                    throw new InvalidOperationException("Array was modified during its enumeration.");
                var i = _index + 1;
                if (i >= _array.Length)
                    return false;
                _item = _array[i];
                _index = i;
                return true;
            }

            public void Reset() => throw new NotImplementedException();

            public T Current =>
                _array == null
                ? throw new ObjectDisposedException(nameof(Enumerator))
                : _item;

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
