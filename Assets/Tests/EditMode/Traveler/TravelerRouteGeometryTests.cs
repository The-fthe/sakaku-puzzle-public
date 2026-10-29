using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace SakakuPuzzle.Tests
{
    public sealed class TravelerRouteGeometryTests
    {
        [Test]
        public void TravelerRouteGeometry_RoundsAndShortensSharpCorners()
        {
            var temporaryRawPath = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(10f, 10f),
                new Vector2(0f, 20f)
            };
            var temporaryRoundedPath =
                TravelerRouteGeometry.BuildRoundedPath(
                    temporaryRawPath,
                    6f,
                    8);

            Assert.That(
                temporaryRoundedPath.Count,
                Is.GreaterThan(temporaryRawPath.Length));
            Assert.That(
                temporaryRoundedPath[0],
                Is.EqualTo(temporaryRawPath[0]));
            Assert.That(
                temporaryRoundedPath[
                    temporaryRoundedPath.Count - 1],
                Is.EqualTo(
                    temporaryRawPath[
                        temporaryRawPath.Length - 1]));

            var temporaryRawLength =
                Vector2.Distance(
                    temporaryRawPath[0],
                    temporaryRawPath[1]) +
                Vector2.Distance(
                    temporaryRawPath[1],
                    temporaryRawPath[2]);
            var temporaryRoundedLength = 0f;
            var temporarySmallestDirectionDot = 1f;
            for (var temporaryIndex = 0;
                 temporaryIndex <
                 temporaryRoundedPath.Count - 1;
                 temporaryIndex++)
            {
                temporaryRoundedLength += Vector2.Distance(
                    temporaryRoundedPath[temporaryIndex],
                    temporaryRoundedPath[
                        temporaryIndex + 1]);
                if (temporaryIndex == 0)
                {
                    continue;
                }

                var temporaryPreviousDirection =
                    (temporaryRoundedPath[temporaryIndex] -
                     temporaryRoundedPath[temporaryIndex - 1])
                    .normalized;
                var temporaryNextDirection =
                    (temporaryRoundedPath[temporaryIndex + 1] -
                     temporaryRoundedPath[temporaryIndex])
                    .normalized;
                temporarySmallestDirectionDot = Mathf.Min(
                    temporarySmallestDirectionDot,
                    Vector2.Dot(
                        temporaryPreviousDirection,
                        temporaryNextDirection));
            }

            Assert.That(
                temporaryRoundedLength,
                Is.LessThan(temporaryRawLength));
            Assert.That(
                temporarySmallestDirectionDot,
                Is.GreaterThan(0.9f));
            Assert.That(
                temporaryRoundedPath.Any(
                    temporaryPoint =>
                        Vector2.Distance(
                            temporaryPoint,
                            temporaryRawPath[1]) < 0.01f),
                Is.False);
        }

        [Test]
        public void TravelerRouteGeometry_MergesTileGapBeforeRounding()
        {
            var temporarySplitCornerPath = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(10f, 9.75f),
                new Vector2(10f, 10.25f),
                new Vector2(0f, 20f)
            };
            var temporarySourceMappings = new List<int>();
            var temporaryRoundedPath =
                TravelerRouteGeometry.BuildRoundedPath(
                    temporarySplitCornerPath,
                    6f,
                    8,
                    temporarySourceMappings);
            var temporaryMaximumX =
                temporaryRoundedPath.Max(
                    temporaryPoint => temporaryPoint.x);

            Assert.That(
                temporaryMaximumX,
                Is.LessThan(9f));
            Assert.That(
                temporarySourceMappings.Count,
                Is.EqualTo(temporarySplitCornerPath.Length));
            Assert.That(
                temporarySourceMappings[1],
                Is.EqualTo(temporarySourceMappings[2]));
        }

        [Test]
        public void TravelerRouteGeometry_KeepsCornerWhenCurveLeavesSafeRoad()
        {
            var temporaryRawPath = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(10f, 0f),
                new Vector2(10f, 10f)
            };
            var temporaryRoundedPath =
                TravelerRouteGeometry.BuildRoundedPath(
                    temporaryRawPath,
                    6f,
                    8,
                    null,
                    temporaryPoint =>
                        Mathf.Abs(temporaryPoint.y) < 0.001f ||
                        Mathf.Abs(temporaryPoint.x - 10f) < 0.001f);

            Assert.That(
                temporaryRoundedPath,
                Is.EqualTo(temporaryRawPath));
        }

        [Test]
        public void TravelerRouteGeometry_ShrinksCornerToRemainInsideSafeRoad()
        {
            var temporaryRawPath = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(10f, 0f),
                new Vector2(10f, 10f)
            };
            bool IsInsideRoad(Vector2 temporaryPoint) =>
                temporaryPoint.y <= 1.01f ||
                temporaryPoint.x >= 8.99f;

            var temporaryRoundedPath =
                TravelerRouteGeometry.BuildRoundedPath(
                    temporaryRawPath,
                    6f,
                    8,
                    null,
                    IsInsideRoad);

            Assert.That(
                temporaryRoundedPath.Count,
                Is.GreaterThan(temporaryRawPath.Length));
            Assert.That(
                temporaryRoundedPath[1].x,
                Is.GreaterThan(5.75f));
            Assert.That(
                temporaryRoundedPath[1].x,
                Is.LessThan(9.9f));
            Assert.That(
                temporaryRoundedPath.All(IsInsideRoad),
                Is.True);
            Assert.That(
                temporaryRoundedPath.Any(
                    temporaryPoint =>
                        Vector2.Distance(
                            temporaryPoint,
                            temporaryRawPath[1]) < 0.01f),
                Is.False);
        }

        [Test]
        public void TravelerRouteGeometry_FindsSafeBandAwayFromSharpCorner()
        {
            var temporaryRawPath = new[]
            {
                new Vector2(-5f, 5f),
                Vector2.zero,
                new Vector2(5f, 5f)
            };

            var temporaryRoundedPath =
                TravelerRouteGeometry.BuildRoundedPath(
                    temporaryRawPath,
                    5f,
                    8,
                    isPointSafe: temporaryPoint =>
                        temporaryPoint.y >= 4.9f ||
                        (
                            temporaryPoint.y >= 0.9f &&
                            Mathf.Abs(temporaryPoint.x) <= 2.1f
                        ));

            Assert.That(
                temporaryRoundedPath.Count,
                Is.GreaterThan(3));
            Assert.That(
                temporaryRoundedPath.All(
                    temporaryPoint =>
                        temporaryPoint.y >= 4.9f ||
                        (
                            temporaryPoint.y >= 0.9f &&
                            Mathf.Abs(temporaryPoint.x) <= 2.1f
                        )),
                Is.True);
        }

        [Test]
        public void TravelerRouteGeometry_SkipsUnsafeReversalCorner()
        {
            var temporaryRawPath = new[]
            {
                new Vector2(-1f, -1f),
                new Vector2(-2f, -2f),
                new Vector2(-0.5f, -0.5f)
            };

            var temporaryRoundedPath =
                TravelerRouteGeometry.BuildRoundedPath(
                    temporaryRawPath,
                    1f,
                    6,
                    isPointSafe: temporaryPoint =>
                        temporaryPoint.x >= -1.5f &&
                        temporaryPoint.y >= -1.5f);

            Assert.That(
                temporaryRoundedPath,
                Has.None.EqualTo(temporaryRawPath[1]));
            Assert.That(
                temporaryRoundedPath.All(
                    temporaryPoint =>
                        temporaryPoint.x >= -1.5f &&
                        temporaryPoint.y >= -1.5f),
                Is.True);
        }

        [Test]
        public void TravelerRouteGeometry_UsesDeepTrimForUnsafeBoundaryCorner()
        {
            var temporaryRawPath = new[]
            {
                new Vector2(0f, 5f),
                Vector2.zero,
                new Vector2(5f, 0f)
            };

            var temporaryRoundedPath =
                TravelerRouteGeometry.BuildRoundedPath(
                    temporaryRawPath,
                    5f,
                    8,
                    isPointSafe: temporaryPoint =>
                        temporaryPoint.x +
                        temporaryPoint.y >= 1.8f);

            Assert.That(
                temporaryRoundedPath.Count,
                Is.GreaterThan(3));
            Assert.That(
                temporaryRoundedPath.All(
                    temporaryPoint =>
                        temporaryPoint.x +
                        temporaryPoint.y >= 1.8f),
                Is.True);
        }

        [Test]
        public void TravelerRouteGeometry_ClearanceIncludesInkWallAndMargin()
        {
            var temporaryWalls =
                new[]
                {
                    new TravelerRouteGeometry.WallSegment(
                        new Vector2(0f, 0f),
                        new Vector2(0f, 10f),
                        0.5f)
                };
            var temporaryBounds =
                new Rect(-5f, -5f, 20f, 20f);

            Assert.That(
                TravelerRouteGeometry.HasRequiredClearance(
                    new Vector2(1f, 5f),
                    temporaryWalls,
                    0.4f,
                    0.2f,
                    temporaryBounds),
                Is.False);
            Assert.That(
                TravelerRouteGeometry.HasRequiredClearance(
                    new Vector2(1.2f, 5f),
                    temporaryWalls,
                    0.4f,
                    0.2f,
                    temporaryBounds),
                Is.True);
            Assert.That(
                TravelerRouteGeometry.HasRequiredClearance(
                    new Vector2(-4.5f, 5f),
                    temporaryWalls,
                    0.4f,
                    0.2f,
                    temporaryBounds),
                Is.False);
        }

        [Test]
        public void TravelerRouteGeometry_InsetsUnsafeEndpointsAlongPath()
        {
            var temporaryRawPath = new[]
            {
                new Vector2(-2f, -2f),
                new Vector2(5f, 5f),
                new Vector2(12f, 12f)
            };

            var temporaryPath =
                TravelerRouteGeometry.BuildRoundedPath(
                    temporaryRawPath,
                    0f,
                    4,
                    isPointSafe: temporaryPoint =>
                        temporaryPoint.x >= 1f &&
                        temporaryPoint.x <= 9f &&
                        temporaryPoint.y >= 1f &&
                        temporaryPoint.y <= 9f);

            Assert.That(
                Vector2.Distance(
                    temporaryPath[0],
                    new Vector2(1f, 1f)),
                Is.LessThan(0.001f));
            Assert.That(
                Vector2.Distance(
                    temporaryPath[1],
                    new Vector2(5f, 5f)),
                Is.LessThan(0.001f));
            Assert.That(
                Vector2.Distance(
                    temporaryPath[2],
                    new Vector2(9f, 9f)),
                Is.LessThan(0.001f));
        }
    }
}
