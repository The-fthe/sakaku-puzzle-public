using NUnit.Framework;
using System.Collections.Generic;

namespace SakakuPuzzle.Tests
{
    public sealed class PuzzleStateTests
    {
        static readonly int[] Seeds = { 1201, 2302, 3403, 4504 };

        #region Generation

        [TestCase(1, 2)]
        [TestCase(2, 4)]
        [TestCase(3, 6)]
        [TestCase(4, 8)]
        public void CreateStage_UsesGoalCountTimesTwoAsMinimum(
            int stageNumber,
            int expectedMinimum)
        {
            var temporaryDefinition = PuzzleDefinition.CreateStage(
                stageNumber,
                Seeds[stageNumber - 1]);

            Assert.That(
                temporaryDefinition.Goals.Count,
                Is.EqualTo(stageNumber));
            Assert.That(
                temporaryDefinition.MinimumSteps,
                Is.EqualTo(expectedMinimum));
            Assert.That(
                new PuzzleState(temporaryDefinition).IsSolved,
                Is.False);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void FindShortestSolution_ReachesEveryGoal(
            int stageNumber)
        {
            var temporaryDefinition = PuzzleDefinition.CreateStage(
                stageNumber,
                Seeds[stageNumber - 1]);
            var temporaryState = new PuzzleState(temporaryDefinition);
            var temporarySolution = temporaryState.FindShortestSolution();

            Assert.That(
                temporarySolution.Count,
                Is.EqualTo(stageNumber * 2));

            foreach (var temporaryTileIndex in temporarySolution)
            {
                Assert.That(
                    temporaryState.Rotate(temporaryTileIndex),
                    Is.True);
            }

            Assert.That(temporaryState.IsSolved, Is.True);
            Assert.That(
                temporaryState.TraceCurrentRoute().ReachedGoalCount,
                Is.EqualTo(stageNumber));
            Assert.That(
                temporaryState
                    .FindSolvedJourneysByNearestGoal().Count,
                Is.EqualTo(stageNumber));
            Assert.That(
                temporaryState.CountRecordedRotations(),
                Is.EqualTo(temporaryState.Steps));
        }

        [Test]
        public void SolvedRoutes_VisitNearestRemainingGoal()
        {
            var temporaryDefinition =
                PuzzleDefinition.CreateStage(4, Seeds[3]);
            var temporaryState =
                new PuzzleState(temporaryDefinition);

            foreach (var temporaryTileIndex in
                     temporaryState.FindShortestSolution())
            {
                temporaryState.Rotate(temporaryTileIndex);
            }

            var temporaryGoalOrder = new List<int>();
            foreach (var temporaryJourney in
                     temporaryState.FindSolvedJourneysByNearestGoal())
            {
                Assert.That(temporaryJourney.Steps, Is.Not.Empty);
                temporaryGoalOrder.Add(
                    temporaryJourney.GoalIndex);
            }

            Assert.That(
                temporaryGoalOrder,
                Is.EqualTo(new[] { 0, 1, 3, 2 }));
        }

        [Test]
        public void SolvedRoutes_PreferStraightContinuationAtEqualDistance()
        {
            var temporaryDefinition =
                PuzzleDefinition.CreateStage(4, Seeds[3]);
            var temporaryState =
                new PuzzleState(temporaryDefinition);

            foreach (var temporaryTileIndex in
                     temporaryState.FindShortestSolution())
            {
                temporaryState.Rotate(temporaryTileIndex);
            }

            temporaryState.Rotate(16);
            Assert.That(temporaryState.IsSolved, Is.True);

            var temporaryJourney =
                temporaryState.FindSolvedJourneysByNearestGoal()[0];
            Assert.That(temporaryJourney.GoalIndex, Is.Zero);
            Assert.That(temporaryJourney.Steps.Count, Is.EqualTo(4));
            Assert.That(
                new[]
                {
                    temporaryJourney.Steps[0].TileIndex,
                    temporaryJourney.Steps[1].TileIndex,
                    temporaryJourney.Steps[2].TileIndex,
                    temporaryJourney.Steps[3].TileIndex
                },
                Is.EqualTo(new[] { 20, 16, 11, 5 }));

            var temporaryFirstStep = temporaryJourney.Steps[0];
            var temporarySecondStep = temporaryJourney.Steps[1];
            var temporaryFirstFrom = PuzzleDefinition.GetVertexId(
                temporaryDefinition.Width,
                temporaryFirstStep.TileIndex,
                temporaryFirstStep.EntryCorner);
            var temporaryFirstTo = PuzzleDefinition.GetVertexId(
                temporaryDefinition.Width,
                temporaryFirstStep.TileIndex,
                temporaryFirstStep.ExitCorner);
            var temporarySecondFrom = PuzzleDefinition.GetVertexId(
                temporaryDefinition.Width,
                temporarySecondStep.TileIndex,
                temporarySecondStep.EntryCorner);
            var temporarySecondTo = PuzzleDefinition.GetVertexId(
                temporaryDefinition.Width,
                temporarySecondStep.TileIndex,
                temporarySecondStep.ExitCorner);
            var temporaryRowWidth =
                temporaryDefinition.Width + 1;
            var temporaryFirstDirection = new[]
            {
                temporaryFirstTo % temporaryRowWidth -
                temporaryFirstFrom % temporaryRowWidth,
                temporaryFirstTo / temporaryRowWidth -
                temporaryFirstFrom / temporaryRowWidth
            };
            var temporarySecondDirection = new[]
            {
                temporarySecondTo % temporaryRowWidth -
                temporarySecondFrom % temporaryRowWidth,
                temporarySecondTo / temporaryRowWidth -
                temporarySecondFrom / temporaryRowWidth
            };

            Assert.That(
                temporarySecondFrom,
                Is.EqualTo(temporaryFirstTo));
            Assert.That(
                temporarySecondDirection,
                Is.EqualTo(temporaryFirstDirection));
        }

        [Test]
        public void SolvedRouteSteps_MatchTravelerJourneyOrder()
        {
            var temporaryDefinition =
                PuzzleDefinition.CreateStage(4, Seeds[3]);
            var temporaryState =
                new PuzzleState(temporaryDefinition);

            foreach (var temporaryTileIndex in
                     temporaryState.FindShortestSolution())
            {
                temporaryState.Rotate(temporaryTileIndex);
            }

            var temporaryTravelerSteps = new List<RouteStep>();
            foreach (var temporaryJourney in
                     temporaryState.FindSolvedJourneysByNearestGoal())
            {
                temporaryTravelerSteps.AddRange(
                    temporaryJourney.Steps);
            }

            Assert.That(
                temporaryState.FindSolvedRouteSteps(),
                Is.EqualTo(temporaryTravelerSteps));
        }

        #endregion
    }
}
