using NUnit.Framework;
using UnityEditor;

namespace SakakuPuzzle.Tests
{
    public sealed class PuzzleUiCatalogTests
    {
        [Test]
        public void PuzzleUiCatalog_HasEveryRequiredPrefabReference()
        {
            var temporaryCatalog =
                AssetDatabase.LoadAssetAtPath<PuzzleUiCatalog>(
                    "Assets/GameAssets/Settings/PuzzleUiCatalog.asset");

            Assert.That(temporaryCatalog, Is.Not.Null);
            Assert.That(
                temporaryCatalog.IsValid(out var temporaryError),
                Is.True,
                temporaryError);
        }
    }
}
