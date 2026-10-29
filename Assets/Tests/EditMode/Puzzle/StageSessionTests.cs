using NUnit.Framework;

namespace SakakuPuzzle.Tests
{
    public sealed class StageSessionTests
    {
        #region Ranking Challenge

        [SetUp]
        public void SetUp()
        {
            StageSession.AbandonToStageSelect();
        }

        [Test]
        public void CompleteFourStagesInOrder_CanSubmitRanking()
        {
            StageSession.BeginRankingChallenge();

            Assert.That(StageSession.CompleteStage(1, 2), Is.False);
            StageSession.ContinueToNextStage();
            Assert.That(StageSession.CompleteStage(2, 4), Is.False);
            StageSession.ContinueToNextStage();
            Assert.That(StageSession.CompleteStage(3, 6), Is.False);
            StageSession.ContinueToNextStage();
            Assert.That(StageSession.CompleteStage(4, 8), Is.True);

            Assert.That(StageSession.CumulativeSteps, Is.EqualTo(20));
            Assert.That(
                StageSession.StageSteps,
                Is.EqualTo(new[] { 2, 4, 6, 8 }));
            Assert.That(StageSession.CanSubmitRanking, Is.True);
        }

        [Test]
        public void SelectingLaterStage_StartsFreePlay()
        {
            StageSession.SelectStage(3);

            Assert.That(StageSession.IsRankingChallenge, Is.False);
            Assert.That(StageSession.CanSubmitRanking, Is.False);
        }

        [Test]
        public void AssistedChallenge_CannotSubmitAfterReset()
        {
            StageSession.BeginRankingChallenge();
            StageSession.MarkAssisted();
            StageSession.RecordRestart();

            StageSession.CompleteStage(1, 2);
            StageSession.ContinueToNextStage();
            StageSession.CompleteStage(2, 4);
            StageSession.ContinueToNextStage();
            StageSession.CompleteStage(3, 6);
            StageSession.ContinueToNextStage();
            StageSession.CompleteStage(4, 8);

            Assert.That(StageSession.RestartCount, Is.EqualTo(1));
            Assert.That(StageSession.CanSubmitRanking, Is.False);
        }

        #endregion
    }
}
