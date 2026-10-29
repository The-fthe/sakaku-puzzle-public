using System;
using System.Collections.Generic;
using UnityEngine;

namespace SakakuPuzzle
{
    public static class TravelerRouteGeometry
    {
        public readonly struct WallSegment
        {
            public WallSegment(
                Vector2 start,
                Vector2 end,
                float halfWidth)
            {
                Start = start;
                End = end;
                HalfWidth = Mathf.Max(0f, halfWidth);
            }

            public Vector2 Start { get; }
            public Vector2 End { get; }
            public float HalfWidth { get; }
        }

        #region Geometry

        public static List<Vector2> BuildRoundedPath(
            IReadOnlyList<Vector2> path,
            float cornerRadius,
            int cornerSegments,
            IList<int> sourcePointMappings = null,
            Func<Vector2, bool> isPointSafe = null)
        {
            var temporaryResult = new List<Vector2>();
            sourcePointMappings?.Clear();
            if (path == null || path.Count == 0)
            {
                return temporaryResult;
            }

            var temporaryRadius = Mathf.Max(0f, cornerRadius);
            var temporarySegments = Mathf.Clamp(
                cornerSegments,
                2,
                24);
            var temporaryCollapsedPath = new List<Vector2>
            {
                path[0]
            };
            var temporarySourceToCollapsed =
                new int[path.Count];
            var temporaryMergeDistance =
                temporaryRadius * 0.25f;
            for (var temporaryIndex = 1;
                 temporaryIndex < path.Count;
                 temporaryIndex++)
            {
                var temporaryPoint = path[temporaryIndex];
                var temporaryLastIndex =
                    temporaryCollapsedPath.Count - 1;
                if (temporaryIndex > 1 &&
                    temporaryIndex < path.Count - 1 &&
                    temporaryMergeDistance > 0.01f &&
                    Vector2.Distance(
                        temporaryCollapsedPath[
                            temporaryLastIndex],
                        temporaryPoint) <
                    temporaryMergeDistance)
                {
                    temporaryCollapsedPath[temporaryLastIndex] =
                        (
                            temporaryCollapsedPath[
                                temporaryLastIndex] +
                            temporaryPoint
                        ) * 0.5f;
                    temporarySourceToCollapsed[temporaryIndex] =
                        temporaryLastIndex;
                    continue;
                }

                temporaryCollapsedPath.Add(temporaryPoint);
                temporarySourceToCollapsed[temporaryIndex] =
                    temporaryCollapsedPath.Count - 1;
            }

            InsetUnsafeEndpoints(
                temporaryCollapsedPath,
                isPointSafe);

            var temporaryMappings =
                new int[temporaryCollapsedPath.Count];
            temporaryMappings[0] = AddDistinct(
                temporaryResult,
                temporaryCollapsedPath[0]);

            for (var temporaryIndex = 1;
                 temporaryIndex <
                 temporaryCollapsedPath.Count - 1;
                 temporaryIndex++)
            {
                var temporaryPrevious =
                    temporaryCollapsedPath[temporaryIndex - 1];
                var temporaryCorner =
                    temporaryCollapsedPath[temporaryIndex];
                var temporaryNext =
                    temporaryCollapsedPath[temporaryIndex + 1];
                var temporaryIncoming =
                    temporaryCorner - temporaryPrevious;
                var temporaryOutgoing =
                    temporaryNext - temporaryCorner;
                var temporaryIncomingLength =
                    temporaryIncoming.magnitude;
                var temporaryOutgoingLength =
                    temporaryOutgoing.magnitude;
                if (temporaryRadius <= 0.01f ||
                    temporaryIncomingLength <= 0.01f ||
                    temporaryOutgoingLength <= 0.01f)
                {
                    temporaryMappings[temporaryIndex] =
                        AddDistinct(
                            temporaryResult,
                            temporaryCorner);
                    continue;
                }

                temporaryIncoming /= temporaryIncomingLength;
                temporaryOutgoing /= temporaryOutgoingLength;
                var temporaryDirectionDot = Vector2.Dot(
                    temporaryIncoming,
                    temporaryOutgoing);
                if (temporaryDirectionDot > 0.999f)
                {
                    temporaryMappings[temporaryIndex] =
                        AddDistinct(
                            temporaryResult,
                            temporaryCorner);
                    continue;
                }

                if (temporaryDirectionDot < -0.999f)
                {
                    if (isPointSafe == null ||
                        isPointSafe(temporaryCorner))
                    {
                        temporaryMappings[temporaryIndex] =
                            AddDistinct(
                                temporaryResult,
                                temporaryCorner);
                    }
                    else
                    {
                        temporaryMappings[temporaryIndex] =
                            temporaryResult.Count - 1;
                    }

                    continue;
                }

                var temporaryCornerIsSafe =
                    isPointSafe == null ||
                    isPointSafe(temporaryCorner);
                var temporarySegmentTrimRatio =
                    temporaryCornerIsSafe
                        ? 0.45f
                        : 0.9f;
                var temporaryTrim = Mathf.Min(
                    temporaryRadius,
                    temporaryIncomingLength *
                    temporarySegmentTrimRatio,
                    temporaryOutgoingLength *
                    temporarySegmentTrimRatio);
                var temporaryCurvePoints =
                    BuildQuadraticCurve(
                        temporaryCorner,
                        temporaryIncoming,
                        temporaryOutgoing,
                        temporaryTrim,
                        temporarySegments);
                if (!IsCurveSafe(
                        temporaryCurvePoints,
                        isPointSafe))
                {
                    const int RADIUS_SEARCH_SAMPLES = 24;
                    const int RADIUS_BINARY_ATTEMPTS = 10;
                    var temporaryMaximumTrim = temporaryTrim;
                    var temporaryPreviousUnsafeTrim =
                        temporaryMaximumTrim;
                    var temporarySafeTrim = 0f;
                    for (var temporarySample = 1;
                         temporarySample <=
                         RADIUS_SEARCH_SAMPLES;
                         temporarySample++)
                    {
                        var temporaryCandidateTrim =
                            temporaryMaximumTrim *
                            (
                                1f -
                                temporarySample /
                                (float)RADIUS_SEARCH_SAMPLES
                            );
                        if (temporaryCandidateTrim <= 0.01f)
                        {
                            break;
                        }

                        var temporaryCandidatePoints =
                            BuildQuadraticCurve(
                                temporaryCorner,
                                temporaryIncoming,
                                temporaryOutgoing,
                                temporaryCandidateTrim,
                                temporarySegments);
                        if (!IsCurveSafe(
                                temporaryCandidatePoints,
                                isPointSafe))
                        {
                            temporaryPreviousUnsafeTrim =
                                temporaryCandidateTrim;
                            continue;
                        }

                        temporarySafeTrim =
                            temporaryCandidateTrim;
                        temporaryCurvePoints =
                            temporaryCandidatePoints;
                        var temporaryUnsafeTrim =
                            temporaryPreviousUnsafeTrim;
                        for (var temporaryAttempt = 0;
                             temporaryAttempt <
                             RADIUS_BINARY_ATTEMPTS;
                             temporaryAttempt++)
                        {
                            var temporaryRefinedTrim =
                                (
                                    temporarySafeTrim +
                                    temporaryUnsafeTrim
                                ) * 0.5f;
                            var temporaryRefinedPoints =
                                BuildQuadraticCurve(
                                    temporaryCorner,
                                    temporaryIncoming,
                                    temporaryOutgoing,
                                    temporaryRefinedTrim,
                                    temporarySegments);
                            if (IsCurveSafe(
                                    temporaryRefinedPoints,
                                    isPointSafe))
                            {
                                temporarySafeTrim =
                                    temporaryRefinedTrim;
                                temporaryCurvePoints =
                                    temporaryRefinedPoints;
                            }
                            else
                            {
                                temporaryUnsafeTrim =
                                    temporaryRefinedTrim;
                            }
                        }

                        break;
                    }

                    temporaryTrim = temporarySafeTrim;
                }

                if (temporaryTrim <= 0.01f)
                {
                    temporaryMappings[temporaryIndex] =
                        AddDistinct(
                            temporaryResult,
                            temporaryCorner);
                    continue;
                }

                AddDistinct(
                    temporaryResult,
                    temporaryCurvePoints[0]);

                var temporaryMappingIndex = -1;
                for (var temporarySegment = 1;
                     temporarySegment <
                     temporaryCurvePoints.Count;
                     temporarySegment++)
                {
                    var temporaryPointIndex = AddDistinct(
                        temporaryResult,
                        temporaryCurvePoints[temporarySegment]);
                    if (temporarySegment ==
                        temporarySegments / 2)
                    {
                        temporaryMappingIndex =
                            temporaryPointIndex;
                    }
                }

                temporaryMappings[temporaryIndex] =
                    temporaryMappingIndex >= 0
                        ? temporaryMappingIndex
                        : temporaryResult.Count - 1;
            }

            if (temporaryCollapsedPath.Count > 1)
            {
                temporaryMappings[
                    temporaryCollapsedPath.Count - 1] =
                    AddDistinct(
                        temporaryResult,
                        temporaryCollapsedPath[
                            temporaryCollapsedPath.Count - 1]);
            }

            if (sourcePointMappings != null)
            {
                foreach (var temporaryCollapsedIndex in
                         temporarySourceToCollapsed)
                {
                    sourcePointMappings.Add(
                        temporaryMappings[
                            temporaryCollapsedIndex]);
                }
            }

            return temporaryResult;
        }

        public static bool HasRequiredClearance(
            Vector2 point,
            IReadOnlyList<WallSegment> walls,
            float visibleInkHalfWidth,
            float margin,
            Rect bounds)
        {
            var temporaryInkHalfWidth =
                Mathf.Max(0f, visibleInkHalfWidth);
            var temporaryMargin = Mathf.Max(0f, margin);
            var temporaryBoundaryInset =
                temporaryInkHalfWidth + temporaryMargin;
            if (point.x < bounds.xMin + temporaryBoundaryInset ||
                point.x > bounds.xMax - temporaryBoundaryInset ||
                point.y < bounds.yMin + temporaryBoundaryInset ||
                point.y > bounds.yMax - temporaryBoundaryInset)
            {
                return false;
            }

            if (walls == null)
            {
                return true;
            }

            foreach (var temporaryWall in walls)
            {
                var temporaryRequiredDistance =
                    temporaryWall.HalfWidth +
                    temporaryInkHalfWidth +
                    temporaryMargin;
                if (DistanceToSegment(
                        point,
                        temporaryWall.Start,
                        temporaryWall.End) <
                    temporaryRequiredDistance)
                {
                    return false;
                }
            }

            return true;
        }

        static float DistanceToSegment(
            Vector2 point,
            Vector2 start,
            Vector2 end)
        {
            var temporarySegment = end - start;
            var temporaryLengthSquared =
                temporarySegment.sqrMagnitude;
            if (temporaryLengthSquared <= 0.0001f)
            {
                return Vector2.Distance(point, start);
            }

            var temporaryProgress = Mathf.Clamp01(
                Vector2.Dot(
                    point - start,
                    temporarySegment) /
                temporaryLengthSquared);
            return Vector2.Distance(
                point,
                start +
                temporarySegment * temporaryProgress);
        }

        static void InsetUnsafeEndpoints(
            IList<Vector2> path,
            Func<Vector2, bool> isPointSafe)
        {
            if (path.Count < 2 || isPointSafe == null)
            {
                return;
            }

            path[0] = MoveToSafetyAlongSegment(
                path[0],
                path[1],
                isPointSafe);
            path[path.Count - 1] = MoveToSafetyAlongSegment(
                path[path.Count - 1],
                path[path.Count - 2],
                isPointSafe);
        }

        static Vector2 MoveToSafetyAlongSegment(
            Vector2 point,
            Vector2 inwardPoint,
            Func<Vector2, bool> isPointSafe)
        {
            const int SEARCH_SAMPLES = 32;
            const int BINARY_SEARCH_ATTEMPTS = 12;
            if (isPointSafe(point))
            {
                return point;
            }

            var temporaryUnsafeProgress = 0f;
            for (var temporarySample = 1;
                 temporarySample <= SEARCH_SAMPLES;
                 temporarySample++)
            {
                var temporarySafeProgress =
                    temporarySample /
                    (float)SEARCH_SAMPLES;
                if (!isPointSafe(
                        Vector2.Lerp(
                            point,
                            inwardPoint,
                            temporarySafeProgress)))
                {
                    temporaryUnsafeProgress =
                        temporarySafeProgress;
                    continue;
                }

                for (var temporaryAttempt = 0;
                     temporaryAttempt < BINARY_SEARCH_ATTEMPTS;
                     temporaryAttempt++)
                {
                    var temporaryProgress =
                        (
                            temporaryUnsafeProgress +
                            temporarySafeProgress
                        ) * 0.5f;
                    if (isPointSafe(
                            Vector2.Lerp(
                                point,
                                inwardPoint,
                                temporaryProgress)))
                    {
                        temporarySafeProgress =
                            temporaryProgress;
                    }
                    else
                    {
                        temporaryUnsafeProgress =
                            temporaryProgress;
                    }
                }

                return Vector2.Lerp(
                    point,
                    inwardPoint,
                    temporarySafeProgress);
            }

            return point;
        }

        static List<Vector2> BuildQuadraticCurve(
            Vector2 corner,
            Vector2 incoming,
            Vector2 outgoing,
            float trim,
            int segments)
        {
            var temporaryCurveStart =
                corner - incoming * trim;
            var temporaryCurveEnd =
                corner + outgoing * trim;
            var temporaryPoints =
                new List<Vector2>(segments + 1)
                {
                    temporaryCurveStart
                };
            for (var temporarySegment = 1;
                 temporarySegment <= segments;
                 temporarySegment++)
            {
                var temporaryProgress =
                    temporarySegment / (float)segments;
                var temporaryInverse = 1f - temporaryProgress;
                temporaryPoints.Add(
                    temporaryInverse *
                    temporaryInverse *
                    temporaryCurveStart +
                    2f *
                    temporaryInverse *
                    temporaryProgress *
                    corner +
                    temporaryProgress *
                    temporaryProgress *
                    temporaryCurveEnd);
            }

            return temporaryPoints;
        }

        static bool IsCurveSafe(
            List<Vector2> curvePoints,
            Func<Vector2, bool> isPointSafe)
        {
            return isPointSafe == null ||
                   curvePoints.TrueForAll(isPointSafe.Invoke);
        }

        static int AddDistinct(
            List<Vector2> path,
            Vector2 point)
        {
            if (path.Count > 0 &&
                Vector2.SqrMagnitude(
                    path[path.Count - 1] - point) <
                0.0001f)
            {
                return path.Count - 1;
            }

            path.Add(point);
            return path.Count - 1;
        }

        #endregion
    }
}
