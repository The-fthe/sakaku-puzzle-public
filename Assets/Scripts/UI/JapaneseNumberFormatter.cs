namespace SakakuPuzzle
{
    public static class JapaneseNumberFormatter
    {
        const string JAPANESE_DIGITS = "零一二三四五六七八九";

        #region Formatting

        public static string Format(int value)
        {
            if (value == 0)
            {
                return JAPANESE_DIGITS[0].ToString();
            }

            if (value >= 1000)
            {
                return "一千以上";
            }

            if (value < 10)
            {
                return JAPANESE_DIGITS[value].ToString();
            }

            if (value < 20)
            {
                return value == 10
                    ? "十"
                    : $"十{JAPANESE_DIGITS[value - 10]}";
            }

            if (value < 100)
            {
                var temporaryTens = value / 10;
                var temporaryOnes = value % 10;
                var temporaryResult =
                    $"{JAPANESE_DIGITS[temporaryTens]}十";
                return temporaryOnes == 0
                    ? temporaryResult
                    : $"{temporaryResult}{JAPANESE_DIGITS[temporaryOnes]}";
            }

            if (value < 1000)
            {
                var temporaryHundreds = value / 100;
                var temporaryRemainder = value % 100;
                var temporaryResult = temporaryHundreds == 1
                    ? "百"
                    : $"{JAPANESE_DIGITS[temporaryHundreds]}百";
                return temporaryRemainder == 0
                    ? temporaryResult
                    : $"{temporaryResult}{Format(temporaryRemainder)}";
            }

            return value.ToString();
        }

        #endregion
    }
}
