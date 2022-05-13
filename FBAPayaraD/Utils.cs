using System.Linq;

namespace FBAPayaraD
{
    public static class Utils
    {
        public static string Capitalized(this string word)
        {
            if (word.Length == 0) return word;
            var lowered = word.ToLower();
            return lowered[0].ToString().ToUpper()
                .Concat(lowered[1..])
                .ToString();
        }
    }
}
