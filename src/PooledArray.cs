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
    using System.Diagnostics;
    using Microsoft.CodeAnalysis.PooledObjects;

    sealed class PooledArray<T> : IDisposable
    {
        public readonly Array<T> Array = new Array<T>();

        readonly ObjectPool<PooledArray<T>> _pool;

        PooledArray(ObjectPool<PooledArray<T>> pool)
        {
            Debug.Assert(pool != null);
            _pool = pool;
        }

        public int Length => Array.Length;

        public void Free()
        {
            var array = Array;

            if (array.Length <= 1024) // do not pool arrays that are too large
            {
                array.Clear();
                _pool.Free(this);
            }
            else
            {
                _pool.ForgetTrackedObject(this);
            }
        }

        public void Dispose() => Free();

        public static implicit operator Array<T>(PooledArray<T> pooled) =>
            pooled.Array;

        static readonly ObjectPool<PooledArray<T>> PoolInstance = CreatePool();

        public static PooledArray<T> GetInstance() => PoolInstance.Allocate();

        public static ObjectPool<PooledArray<T>> CreatePool(int size = 32)
        {
            ObjectPool<PooledArray<T>> pool = null;
            pool = new ObjectPool<PooledArray<T>>(() => new PooledArray<T>(pool), size);
            return pool;
        }
    }
}