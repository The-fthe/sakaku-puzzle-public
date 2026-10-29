using NUnit.Framework;

namespace SakakuPuzzle.Tests
{
    public sealed class BoardRoadRevealTests
    {
        [Test]
        public void GetRoadAlpha_RevealsOutwardFromEveryDestination()
        {
            var temporaryReveal = new BoardRoadReveal(
                5,
                5,
                new[] { 0, 24 },
                0.1f,
                0.4f);

            Assert.That(
                temporaryReveal.GetRoadAlpha(0, 0.2f),
                Is.GreaterThan(0f));
            Assert.That(
                temporaryReveal.GetRoadAlpha(24, 0.2f),
                Is.GreaterThan(0f));
            Assert.That(
                temporaryReveal.GetRoadAlpha(1, 0.05f),
                Is.Zero);
            Assert.That(
                temporaryReveal.GetRoadAlpha(12, 0.2f),
                Is.Zero);
        }

        [Test]
        public void GetRoadAlpha_IsFullyVisibleWhenRevealDurationEnds()
        {
            var temporaryReveal = new BoardRoadReveal(
                5,
                5,
                new[] { 0, 24 },
                0.1f,
                0.4f);

            for (var temporaryTileIndex = 0;
                 temporaryTileIndex < 25;
                 temporaryTileIndex++)
            {
                Assert.That(
                    temporaryReveal.GetRoadAlpha(
                        temporaryTileIndex,
                        temporaryReveal.Duration),
                    Is.EqualTo(1f).Within(0.001f));
            }
        }
    }
}
