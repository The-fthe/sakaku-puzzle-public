using NUnit.Framework;

namespace SakakuPuzzle.Tests
{
    public sealed class ReachableRoadTests
    {
        [Test]
        public void ReachableTiles_IncludeConnectedBranchAndDeadEnd()
        {
            var temporaryState = new PuzzleState(
                PuzzleDefinition.CreateStage(1, 208015862));

            temporaryState.Rotate(10);

            Assert.That(
                temporaryState.GetReachableTileIndices(),
                Is.EqualTo(
                    new[]
                    {
                        6, 7, 8, 10, 11, 12, 13,
                        14, 15, 17, 18, 20, 22, 23
                    }));
        }

        [Test]
        public void ReachableTiles_ShrinkWhenConnectionIsRemoved()
        {
            var temporaryState = new PuzzleState(
                PuzzleDefinition.CreateStage(1, 208015862));

            Assert.That(
                temporaryState.GetReachableTileIndices(),
                Is.EqualTo(new[] { 15, 20 }));

            temporaryState.Rotate(15);

            Assert.That(
                temporaryState.GetReachableTileIndices(),
                Is.EqualTo(new[] { 20 }));
        }
    }
}
