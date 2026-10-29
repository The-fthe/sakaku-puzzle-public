using System.Collections.Generic;
using UnityEngine;

namespace SakakuPuzzle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class TravelerBrushTrailView : MonoBehaviour
    {
        [SerializeField] LineRenderer _lineRenderer;

        readonly List<Vector2> _path = new();
        readonly List<float> _distances = new();

        float _totalLength;
        float _textureTileLength = 64f;

        #region Configuration

        public void Configure(
            IReadOnlyList<Vector2> path,
            float width,
            float textureTileLength,
            Rect? clipBounds = null)
        {
            _path.Clear();
            _distances.Clear();
            _totalLength = 0f;
            _textureTileLength = Mathf.Max(1f, textureTileLength);

            if (_lineRenderer == null)
            {
                return;
            }

            _lineRenderer.useWorldSpace = false;
            _lineRenderer.alignment = LineAlignment.TransformZ;
            _lineRenderer.textureMode = LineTextureMode.Tile;
            var temporaryWidth = Mathf.Max(1f, width);
            _lineRenderer.widthMultiplier = temporaryWidth;
            _lineRenderer.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.22f),
                new Keyframe(0.05f, 0.88f),
                new Keyframe(0.5f, 1f),
                new Keyframe(0.94f, 0.82f),
                new Keyframe(1f, 0.18f));
            _lineRenderer.numCornerVertices = 0;
            _lineRenderer.numCapVertices = 0;

            if (path == null)
            {
                Clear();
                return;
            }

            for (var temporaryIndex = 0;
                 temporaryIndex < path.Count;
                 temporaryIndex++)
            {
                var temporaryPoint = clipBounds.HasValue
                    ? ClampToBounds(
                        path[temporaryIndex],
                        clipBounds.Value,
                        temporaryWidth * 0.5f)
                    : path[temporaryIndex];
                _path.Add(temporaryPoint);
                if (temporaryIndex > 0)
                {
                    _totalLength += Vector2.Distance(
                        _path[temporaryIndex - 1],
                        temporaryPoint);
                }

                _distances.Add(_totalLength);
            }

            Clear();
        }

        static Vector2 ClampToBounds(
            Vector2 point,
            Rect bounds,
            float inset)
        {
            var temporaryMinX = Mathf.Min(
                bounds.center.x,
                bounds.xMin + inset);
            var temporaryMaxX = Mathf.Max(
                bounds.center.x,
                bounds.xMax - inset);
            var temporaryMinY = Mathf.Min(
                bounds.center.y,
                bounds.yMin + inset);
            var temporaryMaxY = Mathf.Max(
                bounds.center.y,
                bounds.yMax - inset);
            return new Vector2(
                Mathf.Clamp(
                    point.x,
                    temporaryMinX,
                    temporaryMaxX),
                Mathf.Clamp(
                    point.y,
                    temporaryMinY,
                    temporaryMaxY));
        }

        #endregion

        #region Drawing

        public void SetDistance(float distance)
        {
            if (_lineRenderer == null ||
                _path.Count < 2 ||
                distance <= 0f)
            {
                Clear();
                return;
            }

            var temporaryDistance =
                Mathf.Clamp(distance, 0f, _totalLength);
            if (temporaryDistance >= _totalLength)
            {
                _lineRenderer.positionCount = _path.Count;
                for (var temporaryIndex = 0;
                     temporaryIndex < _path.Count;
                     temporaryIndex++)
                {
                    SetPosition(
                        temporaryIndex,
                        _path[temporaryIndex]);
                }

                UpdateTextureScale(temporaryDistance);
                return;
            }

            var temporarySegmentIndex = 0;
            while (temporarySegmentIndex < _path.Count - 2 &&
                   _distances[temporarySegmentIndex + 1] <
                   temporaryDistance)
            {
                temporarySegmentIndex++;
            }

            _lineRenderer.positionCount =
                temporarySegmentIndex + 2;
            for (var temporaryIndex = 0;
                 temporaryIndex <= temporarySegmentIndex;
                 temporaryIndex++)
            {
                SetPosition(
                    temporaryIndex,
                    _path[temporaryIndex]);
            }

            var temporarySegmentStartDistance =
                _distances[temporarySegmentIndex];
            var temporarySegmentEndDistance =
                _distances[temporarySegmentIndex + 1];
            var temporaryProgress = Mathf.InverseLerp(
                temporarySegmentStartDistance,
                temporarySegmentEndDistance,
                temporaryDistance);
            SetPosition(
                temporarySegmentIndex + 1,
                Vector2.Lerp(
                    _path[temporarySegmentIndex],
                    _path[temporarySegmentIndex + 1],
                    temporaryProgress));
            UpdateTextureScale(temporaryDistance);
        }

        public void Complete()
        {
            SetDistance(_totalLength);
        }

        public void SetVisible(bool visible)
        {
            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = visible;
            }
        }

        public void Clear()
        {
            if (_lineRenderer != null)
            {
                _lineRenderer.positionCount = 0;
            }
        }

        void SetPosition(int index, Vector2 position)
        {
            _lineRenderer.SetPosition(
                index,
                new Vector3(position.x, position.y, 0f));
        }

        void UpdateTextureScale(float visibleLength)
        {
            _lineRenderer.textureScale = new Vector2(
                Mathf.Max(1f, visibleLength / _textureTileLength),
                1f);
        }

        #endregion
    }
}
