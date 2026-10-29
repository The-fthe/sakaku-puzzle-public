using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SakakuPuzzle
{
    /// <summary>
    /// タイル内だけに描画される、道路を挟む2本の壁。
    /// 黒線そのものは通行不可で、2本の壁の間にある対角線方向の空間が道路になる。
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TilePathGraphic : MaskableGraphic
    {
        const int JOINT_SEGMENTS = 8;
        const int CURVE_SEGMENTS = 6;

        [SerializeField]
        bool _alternateOrientation;

        [SerializeField, Range(0.04f, 0.14f)]
        float _strokeRatio = 0.075f;

        public bool AlternateOrientation
        {
            get => _alternateOrientation;
            set
            {
                if (_alternateOrientation == value)
                {
                    return;
                }

                _alternateOrientation = value;
                SetVerticesDirty();
            }
        }

        public float StrokeRatio => _strokeRatio;

        #region Graphic

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var temporaryRect = rectTransform.rect;
            var temporaryStrokeWidth =
                Mathf.Min(temporaryRect.width, temporaryRect.height) * _strokeRatio;

            if (_alternateOrientation)
            {
                AddPath(vertexHelper, temporaryRect, Direction.Up, Direction.Right, temporaryStrokeWidth);
                AddPath(vertexHelper, temporaryRect, Direction.Down, Direction.Left, temporaryStrokeWidth);
                return;
            }

            AddPath(vertexHelper, temporaryRect, Direction.Up, Direction.Left, temporaryStrokeWidth);
            AddPath(vertexHelper, temporaryRect, Direction.Right, Direction.Down, temporaryStrokeWidth);
        }

        void AddPath(
            VertexHelper vertexHelper,
            Rect rect,
            Direction firstEdge,
            Direction secondEdge,
            float strokeWidth)
        {
            var temporaryPoints = GetNormalizedPath(firstEdge, secondEdge);
            var temporaryLocalPoints = new Vector2[temporaryPoints.Count];
            for (var temporaryIndex = 0; temporaryIndex < temporaryPoints.Count; temporaryIndex++)
            {
                temporaryLocalPoints[temporaryIndex] = NormalizedToLocal(rect, temporaryPoints[temporaryIndex]);
            }

            var temporaryRadius = strokeWidth * 0.5f;
            for (var temporaryIndex = 0; temporaryIndex < temporaryLocalPoints.Length - 1; temporaryIndex++)
            {
                AddSegment(
                    vertexHelper,
                    temporaryLocalPoints[temporaryIndex],
                    temporaryLocalPoints[temporaryIndex + 1],
                    temporaryRadius);
            }

            foreach (var temporaryPoint in temporaryLocalPoints)
            {
                AddJoint(vertexHelper, temporaryPoint, temporaryRadius);
            }
        }

        void AddSegment(
            VertexHelper vertexHelper,
            Vector2 start,
            Vector2 end,
            float radius)
        {
            var temporaryDirection = (end - start).normalized;
            var temporaryNormal = new Vector2(-temporaryDirection.y, temporaryDirection.x) * radius;
            AddQuad(
                vertexHelper,
                start - temporaryNormal,
                start + temporaryNormal,
                end + temporaryNormal,
                end - temporaryNormal);
        }

        void AddJoint(VertexHelper vertexHelper, Vector2 center, float radius)
        {
            var temporaryCenterIndex = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, center);

            for (var temporaryIndex = 0; temporaryIndex <= JOINT_SEGMENTS; temporaryIndex++)
            {
                var temporaryAngle =
                    Mathf.PI * 2f * temporaryIndex / JOINT_SEGMENTS;
                AddVertex(
                    vertexHelper,
                    center + new Vector2(
                        Mathf.Cos(temporaryAngle),
                        Mathf.Sin(temporaryAngle)) * radius);
            }

            for (var temporaryIndex = 0; temporaryIndex < JOINT_SEGMENTS; temporaryIndex++)
            {
                vertexHelper.AddTriangle(
                    temporaryCenterIndex,
                    temporaryCenterIndex + temporaryIndex + 1,
                    temporaryCenterIndex + temporaryIndex + 2);
            }
        }

        void AddQuad(
            VertexHelper vertexHelper,
            Vector2 bottomLeft,
            Vector2 topLeft,
            Vector2 topRight,
            Vector2 bottomRight)
        {
            var temporaryStartIndex = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, bottomLeft);
            AddVertex(vertexHelper, topLeft);
            AddVertex(vertexHelper, topRight);
            AddVertex(vertexHelper, bottomRight);
            vertexHelper.AddTriangle(
                temporaryStartIndex,
                temporaryStartIndex + 1,
                temporaryStartIndex + 2);
            vertexHelper.AddTriangle(
                temporaryStartIndex,
                temporaryStartIndex + 2,
                temporaryStartIndex + 3);
        }

        void AddVertex(VertexHelper vertexHelper, Vector2 position)
        {
            var temporaryVertex = UIVertex.simpleVert;
            temporaryVertex.color = color;
            temporaryVertex.position = position;
            vertexHelper.AddVert(temporaryVertex);
        }

        #endregion

        #region Path Geometry

        public static IReadOnlyList<Vector2> GetRoadCenterline(
            TileCorner entryCorner,
            TileCorner exitCorner)
        {
            var temporaryForward =
                CreateForwardRoadCenterline(entryCorner, exitCorner);
            if (temporaryForward != null)
            {
                return temporaryForward;
            }

            var temporaryReverse =
                CreateForwardRoadCenterline(exitCorner, entryCorner);
            if (temporaryReverse == null)
            {
                throw new ArgumentException(
                    $"Corners must be opposite: {entryCorner} -> {exitCorner}");
            }

            Array.Reverse(temporaryReverse);
            return temporaryReverse;
        }

        public static Vector2 GetRoadPointFromCorner(
            TileCorner corner,
            float normalizedDistance)
        {
            var temporaryPath = GetRoadCenterline(
                corner,
                GetOppositeCorner(corner));
            var temporaryTotalLength = 0f;
            for (var temporaryIndex = 0;
                 temporaryIndex < temporaryPath.Count - 1;
                 temporaryIndex++)
            {
                temporaryTotalLength += Vector2.Distance(
                    temporaryPath[temporaryIndex],
                    temporaryPath[temporaryIndex + 1]);
            }

            var temporaryTargetDistance =
                temporaryTotalLength * Mathf.Clamp01(normalizedDistance);
            var temporaryTraversedDistance = 0f;
            for (var temporaryIndex = 0;
                 temporaryIndex < temporaryPath.Count - 1;
                 temporaryIndex++)
            {
                var temporaryStart = temporaryPath[temporaryIndex];
                var temporaryEnd = temporaryPath[temporaryIndex + 1];
                var temporarySegmentLength =
                    Vector2.Distance(temporaryStart, temporaryEnd);
                if (temporaryTraversedDistance +
                    temporarySegmentLength <
                    temporaryTargetDistance)
                {
                    temporaryTraversedDistance += temporarySegmentLength;
                    continue;
                }

                var temporarySegmentProgress =
                    temporarySegmentLength <= Mathf.Epsilon
                        ? 0f
                        : (temporaryTargetDistance -
                           temporaryTraversedDistance) /
                          temporarySegmentLength;
                return Vector2.Lerp(
                    temporaryStart,
                    temporaryEnd,
                    temporarySegmentProgress);
            }

            return temporaryPath[temporaryPath.Count - 1];
        }

        public static Vector2 GetRoadDirectionFromCorner(TileCorner corner)
        {
            var temporaryStart =
                GetRoadPointFromCorner(corner, 0f);
            var temporaryNext =
                GetRoadPointFromCorner(corner, 0.1f);
            return (temporaryNext - temporaryStart).normalized;
        }

        public static IReadOnlyList<Vector2> GetNormalizedPath(
            Direction entryDirection,
            Direction exitDirection)
        {
            var temporaryForward = CreateForwardPath(entryDirection, exitDirection);
            if (temporaryForward != null)
            {
                return temporaryForward;
            }

            var temporaryReverse = CreateForwardPath(exitDirection, entryDirection);
            if (temporaryReverse == null)
            {
                throw new ArgumentException(
                    $"Directions must form a corner: {entryDirection} -> {exitDirection}");
            }

            Array.Reverse(temporaryReverse);
            return temporaryReverse;
        }

        static Vector2[] CreateForwardPath(Direction firstEdge, Direction secondEdge)
        {
            if (firstEdge == Direction.Up && secondEdge == Direction.Left)
            {
                return CreateRoundedCornerPath(
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 0.5f));
            }

            if (firstEdge == Direction.Up && secondEdge == Direction.Right)
            {
                return CreateRoundedCornerPath(
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(1f, 0.5f));
            }

            if (firstEdge == Direction.Right && secondEdge == Direction.Down)
            {
                return CreateRoundedCornerPath(
                    new Vector2(1f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0f));
            }

            if (firstEdge == Direction.Down && secondEdge == Direction.Left)
            {
                return CreateRoundedCornerPath(
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 0.5f));
            }

            return null;
        }

        static Vector2[] CreateRoundedCornerPath(
            Vector2 edgeStart,
            Vector2 curveControl,
            Vector2 edgeEnd)
        {
            var temporaryPoints =
                new Vector2[CURVE_SEGMENTS + 1];
            for (var temporaryIndex = 0;
                 temporaryIndex <= CURVE_SEGMENTS;
                 temporaryIndex++)
            {
                var temporaryProgress =
                    (float)temporaryIndex / CURVE_SEGMENTS;
                var temporaryStartBlend = Vector2.Lerp(
                    edgeStart,
                    curveControl,
                    temporaryProgress);
                var temporaryEndBlend = Vector2.Lerp(
                    curveControl,
                    edgeEnd,
                    temporaryProgress);
                temporaryPoints[temporaryIndex] =
                    Vector2.Lerp(
                        temporaryStartBlend,
                        temporaryEndBlend,
                        temporaryProgress);
            }

            return temporaryPoints;
        }

        static TileCorner GetOppositeCorner(TileCorner corner)
        {
            return corner switch
            {
                TileCorner.TopLeft => TileCorner.BottomRight,
                TileCorner.TopRight => TileCorner.BottomLeft,
                TileCorner.BottomRight => TileCorner.TopLeft,
                TileCorner.BottomLeft => TileCorner.TopRight,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(corner),
                    corner,
                    null)
            };
        }

        static Vector2[] CreateForwardRoadCenterline(
            TileCorner firstCorner,
            TileCorner secondCorner)
        {
            if (firstCorner == TileCorner.BottomLeft &&
                secondCorner == TileCorner.TopRight)
            {
                return new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(0.18f, 0.18f),
                    new Vector2(0.38f, 0.38f),
                    new Vector2(0.62f, 0.62f),
                    new Vector2(0.82f, 0.82f),
                    new Vector2(1f, 1f)
                };
            }

            if (firstCorner == TileCorner.TopLeft &&
                secondCorner == TileCorner.BottomRight)
            {
                return new[]
                {
                    new Vector2(0f, 1f),
                    new Vector2(0.18f, 0.82f),
                    new Vector2(0.38f, 0.62f),
                    new Vector2(0.62f, 0.38f),
                    new Vector2(0.82f, 0.18f),
                    new Vector2(1f, 0f)
                };
            }

            return null;
        }

        static Vector2 NormalizedToLocal(Rect rect, Vector2 normalizedPoint)
        {
            return new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, normalizedPoint.x),
                Mathf.Lerp(rect.yMin, rect.yMax, normalizedPoint.y));
        }

        #endregion
    }
}
