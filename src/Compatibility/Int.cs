using System;

public struct Int : IEquatable<Int>, IComparable<Int>
{
    readonly int _value;

    public Int(int value) => _value = value;

    public override string ToString() => _value.ToString();
    public override int GetHashCode() => _value.GetHashCode();
    public override bool Equals(object other) => other is Int n && Equals(n);
    public bool Equals(Int other) => _value == other._value;
    public int CompareTo(Int other) => _value.CompareTo(other._value);

    public static implicit operator int(Int n) => n._value;
    public static implicit operator Int(int value) => new Int(value);
    public static bool operator true(Int n) => n != 0;
    public static bool operator false(Int n) => n == 0;
    public static bool operator ==(Int a, Int b) => a.Equals(b);
    public static bool operator !=(Int a, Int b) => !(a == b);
    public static bool operator >(Int a, Int b) => a.CompareTo(b) > 0;
    public static bool operator <(Int a, Int b) => a.CompareTo(b) < 0;
    public static bool operator <=(Int a, Int b) => a.CompareTo(b) <= 0;
    public static bool operator >=(Int a, Int b) => a.CompareTo(b) >= 0;
}