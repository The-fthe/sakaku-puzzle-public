using System.Linq;
using NUnit.Framework;

namespace SakakuPuzzle.Tests
{
    public sealed class BoardPresentationStateTests
    {
        [Test]
        public void NewBoard_HidesEveryRoad()
        {
            var temporaryPresentation =
                new BoardPresentationState(5, 5, 0.7f);

            for (var temporaryTileIndex = 0;
                 temporaryTileIndex < 25;
                 temporaryTileIndex++)
            {
                Assert.That(
                    temporaryPresentation.GetRoadPresentation(
                        temporaryTileIndex,
                        0f),
                    Is.EqualTo(RoadPresentation.WhitePaper));
            }
        }

        [Test]
        public void ReachableRoad_FadesFromWhitePaperToInk()
        {
            var temporaryPresentation =
                new BoardPresentationState(
                    5,
                    5,
                    0.7f,
                    0.22f);

            temporaryPresentation.SetReachableTiles(
                new[] { 20 },
                1f);

            Assert.That(
                temporaryPresentation.GetRoadAlpha(20, 1f),
                Is.Zero);
            Assert.That(
                temporaryPresentation.GetRoadAlpha(20, 1.11f),
                Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(
                temporaryPresentation.GetRoadAlpha(20, 1.22f),
                Is.EqualTo(1f).Within(0.001f));
            Assert.That(
                temporaryPresentation.GetRoadPresentation(
                    20,
                    1.22f),
                Is.EqualTo(RoadPresentation.Inked));
        }

        [Test]
        public void DisconnectedRoad_FadesBackToWhitePaper()
        {
            var temporaryPresentation =
                new BoardPresentationState(
                    5,
                    5,
                    0.7f,
                    0.22f);
            temporaryPresentation.SetReachableTiles(
                new[] { 20 },
                1f);

            temporaryPresentation.SetReachableTiles(
                System.Array.Empty<int>(),
                1.22f);

            Assert.That(
                temporaryPresentation.GetRoadAlpha(20, 1.33f),
                Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(
                temporaryPresentation.GetRoadAlpha(20, 1.44f),
                Is.Zero.Within(0.001f));
            Assert.That(
                temporaryPresentation.GetRoadPresentation(
                    20,
                    1.44f),
                Is.EqualTo(RoadPresentation.WhitePaper));
        }

        [Test]
        public void InkedRoad_RemainsPermanentOutsidePeekRange()
        {
            var temporaryPresentation =
                new BoardPresentationState(
                    5,
                    5,
                    0.7f,
                    0.22f);
            temporaryPresentation.SetReachableTiles(
                new[] { 20 },
                0f);
            temporaryPresentation.Focus(12, 0.3f);

            Assert.That(
                temporaryPresentation.GetRoadPresentation(
                    20,
                    0.3f),
                Is.EqualTo(RoadPresentation.Inked));
            Assert.That(
                temporaryPresentation.GetRoadAlpha(20, 0.3f),
                Is.EqualTo(1f));

            temporaryPresentation.Focus(13, 0.4f);

            Assert.That(
                temporaryPresentation.GetRoadPresentation(
                    20,
                    0.5f),
                Is.EqualTo(RoadPresentation.Inked));
            Assert.That(
                temporaryPresentation.GetRoadAlpha(20, 0.5f),
                Is.EqualTo(1f));
        }

        [Test]
        public void FocusCenter_RevealsOnlyFocusedTile()
        {
            var temporaryPresentation =
                new BoardPresentationState(5, 5, 0.7f);

            temporaryPresentation.Focus(12, 0f);

            for (var temporaryTileIndex = 0;
                 temporaryTileIndex < 25;
                 temporaryTileIndex++)
            {
                Assert.That(
                    temporaryPresentation.GetRoadPresentation(
                        temporaryTileIndex,
                        0f),
                    temporaryTileIndex == 12
                        ? Is.EqualTo(RoadPresentation.Peeking)
                        : Is.EqualTo(RoadPresentation.WhitePaper),
                    $"tile {temporaryTileIndex}");
            }
        }

        [TestCase(0)]
        [TestCase(2)]
        public void FocusBoundary_RevealsOnlyFocusedTile(
            int focusedTileIndex)
        {
            var temporaryPresentation =
                new BoardPresentationState(5, 5, 0.7f);

            temporaryPresentation.Focus(focusedTileIndex, 0f);

            var temporaryActualPeeking =
                Enumerable.Range(0, 25)
                    .Where(
                        temporaryTileIndex =>
                            temporaryPresentation
                                .GetRoadPresentation(
                                    temporaryTileIndex,
                                    0f) ==
                            RoadPresentation.Peeking);
            Assert.That(
                temporaryActualPeeking,
                Is.EqualTo(new[] { focusedTileIndex }));
        }

        [Test]
        public void MoveFocus_LeavesPreviousTileAsTimedAfterimage()
        {
            var temporaryPresentation =
                new BoardPresentationState(5, 5, 0.7f);
            temporaryPresentation.Focus(12, 0f);

            temporaryPresentation.Focus(13, 0.1f);

            Assert.That(
                temporaryPresentation.GetRoadPresentation(13, 0.2f),
                Is.EqualTo(RoadPresentation.Peeking));
            Assert.That(
                temporaryPresentation.GetRoadPresentation(12, 0.2f),
                Is.EqualTo(RoadPresentation.Afterimage));
            Assert.That(
                temporaryPresentation.GetRoadPresentation(12, 0.81f),
                Is.EqualTo(RoadPresentation.WhitePaper));
        }

        [Test]
        public void Reset_ClearsFocusAndAfterimageImmediately()
        {
            var temporaryPresentation =
                new BoardPresentationState(5, 5, 0.7f);
            temporaryPresentation.Focus(12, 0f);
            temporaryPresentation.Focus(13, 0.1f);

            temporaryPresentation.Reset();

            for (var temporaryTileIndex = 0;
                 temporaryTileIndex < 25;
                 temporaryTileIndex++)
            {
                Assert.That(
                    temporaryPresentation.GetRoadPresentation(
                        temporaryTileIndex,
                        0.2f),
                    Is.EqualTo(RoadPresentation.WhitePaper));
            }
        }

        [Test]
        public void ResetToInitialMarker_ShowsOnlyMarkerTile()
        {
            var temporaryPresentation =
                new BoardPresentationState(5, 5, 0.7f);
            temporaryPresentation.SetReachableTiles(
                new[] { 15, 20 },
                0f);
            temporaryPresentation.Focus(12, 0.3f);

            temporaryPresentation.ResetToInitialMarker(20);

            for (var temporaryTileIndex = 0;
                 temporaryTileIndex < 25;
                 temporaryTileIndex++)
            {
                Assert.That(
                    temporaryPresentation.GetRoadAlpha(
                        temporaryTileIndex,
                        0.3f),
                    temporaryTileIndex == 20
                        ? Is.EqualTo(1f)
                        : Is.Zero,
                    $"tile {temporaryTileIndex}");
            }
        }

        [Test]
        public void ClearFocus_LeavesCurrentTileAsTimedAfterimage()
        {
            var temporaryPresentation =
                new BoardPresentationState(5, 5, 0.7f);
            temporaryPresentation.Focus(12, 0f);

            temporaryPresentation.ClearFocus(12, 0.1f);

            Assert.That(
                temporaryPresentation.GetRoadPresentation(12, 0.2f),
                Is.EqualTo(RoadPresentation.Afterimage));
            Assert.That(
                temporaryPresentation.GetRoadPresentation(12, 0.81f),
                Is.EqualTo(RoadPresentation.WhitePaper));
        }

        [Test]
        public void ExitThenEnter_PreservesExitedTileAsAfterimage()
        {
            var temporaryPresentation =
                new BoardPresentationState(5, 5, 0.7f);
            temporaryPresentation.Focus(12, 0f);
            temporaryPresentation.ClearFocus(12, 0.1f);

            temporaryPresentation.Focus(13, 0.11f);

            Assert.That(
                temporaryPresentation.GetRoadPresentation(12, 0.2f),
                Is.EqualTo(RoadPresentation.Afterimage));
            Assert.That(
                temporaryPresentation.GetRoadPresentation(13, 0.2f),
                Is.EqualTo(RoadPresentation.Peeking));
        }
    }
}
