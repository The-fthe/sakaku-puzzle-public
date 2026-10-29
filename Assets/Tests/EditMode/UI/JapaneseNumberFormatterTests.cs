using NUnit.Framework;

namespace SakakuPuzzle.Tests
{
    public sealed class JapaneseNumberFormatterTests
    {
        #region Format

        [Test]
        public void Format_Twenty_UsesJapaneseNumerals()
        {
            Assert.That(
                JapaneseNumberFormatter.Format(20),
                Is.EqualTo("二十"));
        }

        [Test]
        public void Format_TwentyOne_UsesJapaneseNumerals()
        {
            Assert.That(
                JapaneseNumberFormatter.Format(21),
                Is.EqualTo("二十一"));
        }

        [Test]
        public void Format_NinetyNine_UsesJapaneseNumerals()
        {
            Assert.That(
                JapaneseNumberFormatter.Format(99),
                Is.EqualTo("九十九"));
        }

        [Test]
        public void Format_OneHundred_OmitsLeadingOne()
        {
            Assert.That(
                JapaneseNumberFormatter.Format(100),
                Is.EqualTo("百"));
        }

        [Test]
        public void Format_NineHundredNinetyNine_UsesJapaneseNumerals()
        {
            Assert.That(
                JapaneseNumberFormatter.Format(999),
                Is.EqualTo("九百九十九"));
        }

        [TestCase(1000)]
        [TestCase(1001)]
        [TestCase(int.MaxValue)]
        public void Format_AtOrAboveOneThousand_UsesUpperBoundLabel(
            int value)
        {
            Assert.That(
                JapaneseNumberFormatter.Format(value),
                Is.EqualTo("一千以上"));
        }

        [TestCase(101, "百一")]
        [TestCase(110, "百十")]
        [TestCase(909, "九百九")]
        public void Format_HundredsContainingZero_OmitsZeroDigits(
            int value,
            string expected)
        {
            Assert.That(
                JapaneseNumberFormatter.Format(value),
                Is.EqualTo(expected));
        }

        #endregion
    }
}
