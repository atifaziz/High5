namespace ParseFive.Compatibility
{
    static class String
    {
        public static char fromCharCode(int cp)
        {
            return (char)cp;
        }

        public static bool isTruthy(this string str) => !string.IsNullOrEmpty(str);
    }
}
