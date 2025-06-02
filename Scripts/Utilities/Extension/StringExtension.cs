namespace Utilities.Extension
{
    using System.Linq;
    using Newtonsoft.Json;

    /// <summary>
    /// series of helper methods for common string operations
    /// </summary>
    public static class StringExtension
    {
        /// <summary>
        /// convert a string to snake_case
        /// </summary>
        /// <param name="str"></param>
        /// <returns>the string in snake case</returns>
        /// <remarks>from https://www.30secondsofcode.org/c-sharp/s/to-snake-case</remarks>
        public static string ToSnakeCase(this string str)
        {
            return string.Concat(str.Select((x, i) =>
                                                i > 0 && (
                                                    char.IsUpper(x) ||
                                                    (char.IsDigit(x) && !char.IsDigit(str[i - 1]))
                                                )
                                                    ? "_" + x
                                                    : x.ToString()))
                         .ToLower();
        }
    }
}