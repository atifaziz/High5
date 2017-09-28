namespace ParseFive.Extensions
{
    using System;
    using System.Collections.Generic;

    static class BooleanExtensions
    {
        public static bool IsTruthy(this string s) => !String.IsNullOrEmpty(s);
    }

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
