using NUnit.Framework;

namespace SakakuPuzzle.Tests
{
    public sealed class HowToStateTests
    {
        #region Lifecycle

        [SetUp]
        public void SetUp()
        {
            HowToState.ResetForDebug();
        }

        [TearDown]
        public void TearDown()
        {
            HowToState.ResetForDebug();
        }

        #endregion

        #region Visibility

        [Test]
        public void FreshInstall_ShowsHowToUntilGameStarts()
        {
            Assert.That(
                HowToState.ShouldShowAutomatically,
                Is.True);

            HowToState.MarkSeen();

            Assert.That(
                HowToState.ShouldShowAutomatically,
                Is.False);
        }

        #endregion
    }
}
