namespace High5
{
    static class Truthiness
    {
        public static bool IsTruthy(string s) => !string.IsNullOrEmpty(s);
    }
}
