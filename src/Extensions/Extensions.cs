using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParseFive.Extensions
{
    public interface ISomeList<T>
    {
        Int length { get; }
        T this[int index] { get; }
    }

    public class List<T> : System.Collections.Generic.List<T>, ISomeList<T>
    {
        public Int length => Count;
        
    }

    public class Array<T> : ISomeList<T>
    {
        readonly T[] _array;

        public Array(T[] array) => _array = array;
        public T this[int index] => _array[index];
        public Int length => _array.Length;
    }

    public static class Extensions
    {
        public static bool IsTruthy(this object o) => o != null;
        public static bool IsTruthy(this string s) => !string.IsNullOrEmpty(s);
        public static bool IsTruthy(this int n) => n != 0;
        public static bool IsTruthy(this Int n) => n != 0;

        public static int indexOf(this string str, char ch) =>
            str.IndexOf(ch);

        public static int indexOf(this string str, string search) =>
            str.IndexOf(search, StringComparison.Ordinal);

        public static string toLowerCase(this String str)
        {
            return str.ToLower();
        }

        public static T[] concat<T>(this T[] first, T[] second)
        {
            return first.Concat(second).ToArray();
        }

        public static int length<T>(this T[] array) => array.Length;

        public static int indexOf<T> (this T[] first, T e)
        {
            return Array.IndexOf(first, e);
        }

        public static void push<T>(this List<T> list, T elem)
        {
            list.Insert(0, elem);
        }

        public static T pop<T>(this List<T> list)
        {
            if (list.Count == 0)
                throw new IndexOutOfRangeException("Array is empty");
            var temp = list[list.length - 1];
            list.RemoveAt(list.length - 1);
            return temp;
        }

        public static void splice<T>(this List<T> list, int index, int count)
        {
            list.RemoveRange(index, count);
        }

        public static void splice<T>(this List<T> list, int index, int count, T item1)
        {
            if (index != 0)
            {
                throw new NotImplementedException("Removal of elements not implemented");
            }
            else
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

        //public static int length(this string s) => s.Length;

        public static char toChar(int n) => (char)n;

        public static Int length<T>(this Queue<T> q) => q.Count;

        public static int length<T>(this List<T> l) => l.Count;

        public static int charCodeAt(this string str, int index) => (int)str[index];

        public static int parseInt(string n, int b)
        {
            return b == 16
                  ? int.Parse(n, System.Globalization.NumberStyles.HexNumber)
                  : b == 10
                  ? int.Parse(n)
                  : throw new ArgumentException("Unsupported base");
        }
    }
}
