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

namespace ParseFive.Extensions
{
    using System;
    using System.Collections.Generic;

    static class StringExtensions
    {
        public static string ToLowerCase(this string str) => str.ToLower();
        public static int CharCodeAt(this string str, int index) => (int)str[index];
    }

    public static class ListExtensions
    {
        public static void Push<T>(this List<T> list, T elem)
        {
            list.Add(elem);
        }

        public static void Push<T>(this IList<T> list, T elem)
        {
            list.Add(elem);
        }

        public static T Pop<T>(this List<T> list)
        {
            if (list.Count == 0)
                throw new InvalidOperationException("Array is empty.");
            var temp = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return temp;
        }

        public static void Splice<T>(this List<T> list, int index, int count)
        {
            list.RemoveRange(index, count);
        }

        public static void Splice<T>(this List<T> list, int index, int count, T item1)
        {
            list.RemoveRange(index, count);
            list.Insert(index, item1);
        }

        public static T Shift<T> (this List<T> list)
        {
            if (list.Count == 0)
                throw new InvalidOperationException("Array is empty.");
            var temp = list[0];
            list.RemoveAt(0);
            return temp;
        }
    }
}
