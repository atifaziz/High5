using System;
using System.Collections.Generic;
using System.Linq;

namespace ParseFive.Extensions
{
    public static class Extensions
    {
        public static bool IsTruthy(this object o) => o != null;
        public static bool IsTruthy(this string s) => !string.IsNullOrEmpty(s);
        public static bool IsTruthy(this int n) => n != 0;

        public static string toLowerCase(this String str)
        {
            return str.ToLower();
        }

        public static void push<T>(this List<T> list, T elem)
        {
            list.Add(elem);
        }

        public static T pop<T>(this List<T> list)
        {
            if (list.Count == 0)
                throw new IndexOutOfRangeException("Array is empty");
            var temp = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return temp;
        }

        public static void splice<T>(this List<T> list, int index, int count)
        {
            list.RemoveRange(index, count);
        }

        public static void splice<T>(this List<T> list, int index, int count, T item1)
        {
            list.RemoveRange(index, count);
            list.Insert(index, item1);
        }

        public static T shift<T> (this List<T> list)
        {
            if (list.Count == 0)
                throw new IndexOutOfRangeException("Array is empty");
            var temp = list[0];
            list.RemoveAt(0);
            return temp;
        }

        public static string substring(this string s, int startIndex) => s.Substring(startIndex);
        public static string substr(this string s, int startIndex) => s.Substring(startIndex);

        public static string substring(this string s, int startIndex, int length) => s.Substring(startIndex, length);

        public static int charCodeAt(this string str, int index) => (int)str[index];
    }
}
