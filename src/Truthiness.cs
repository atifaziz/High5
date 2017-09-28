namespace ParseFive
{
    static class Truthiness
    {
        public static bool IsTruthy(string s) => !string.IsNullOrEmpty(s);
    }
}