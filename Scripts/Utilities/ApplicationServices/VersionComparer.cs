namespace GameFoundation.Scripts.Utilities.ApplicationServices
{
    using System;

    public static class VersionComparer
    {
        private static readonly char[] VersionSeparators = { '.', '-', '_', '+', ' ' };

        public static bool IsBelow(string currentValue, string targetValue)
        {
            if (string.IsNullOrWhiteSpace(targetValue)) return false;
            if (string.IsNullOrWhiteSpace(currentValue)) return false;

            return Compare(currentValue, targetValue) < 0;
        }

        public static int Compare(string left, string right)
        {
            left  = left?.Trim() ?? string.Empty;
            right = right?.Trim() ?? string.Empty;

            if (long.TryParse(left, out var leftNumber) && long.TryParse(right, out var rightNumber))
            {
                return leftNumber.CompareTo(rightNumber);
            }

            var leftTokens  = Tokenize(left);
            var rightTokens = Tokenize(right);
            var maxLength   = Math.Max(leftTokens.Length, rightTokens.Length);

            for (var index = 0; index < maxLength; index++)
            {
                var leftToken  = index < leftTokens.Length ? leftTokens[index] : "0";
                var rightToken = index < rightTokens.Length ? rightTokens[index] : "0";

                if (long.TryParse(leftToken, out leftNumber) && long.TryParse(rightToken, out rightNumber))
                {
                    var numberCompare = leftNumber.CompareTo(rightNumber);
                    if (numberCompare != 0) return numberCompare;
                    continue;
                }

                var textCompare = string.Compare(leftToken, rightToken, StringComparison.OrdinalIgnoreCase);
                if (textCompare != 0) return textCompare;
            }

            return 0;
        }

        private static string[] Tokenize(string value)
        {
            return value.Split(VersionSeparators, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
