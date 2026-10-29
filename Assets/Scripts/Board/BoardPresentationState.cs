using System;
using System.Collections.Generic;

namespace SakakuPuzzle
{
    public enum RoadPresentation
    {
        WhitePaper,
        Peeking,
        Afterimage,
        Inked
    }

    public sealed class BoardPresentationState
    {
        const float AFTERIMAGE_ALPHA = 0.22f;

        readonly int _width;
        readonly int _height;
        readonly float _afterimageDuration;
        readonly float _inkFadeDuration;
        readonly bool[] _reachableTargets;
        readonly float[] _inkTransitionStartAlphas;
        readonly float[] _inkTransitionStartedAt;
        int _focusedTileIndex = -1;
        int _previousFocusedTileIndex = -1;
        float _afterimageExpiresAt;

        public BoardPresentationState(
            int width,
            int height,
            float afterimageDuration)
            : this(
                width,
                height,
                afterimageDuration,
                0.22f)
        {
        }

        public BoardPresentationState(
            int width,
            int height,
            float afterimageDuration,
            float inkFadeDuration)
        {
            _width = width;
            _height = height;
            _afterimageDuration = afterimageDuration;
            _inkFadeDuration = Math.Max(
                float.Epsilon,
                inkFadeDuration);
            _reachableTargets = new bool[width * height];
            _inkTransitionStartAlphas =
                new float[width * height];
            _inkTransitionStartedAt =
                new float[width * height];
        }

        #region Presentation

        public void Reset()
        {
            _focusedTileIndex = -1;
            _previousFocusedTileIndex = -1;
            _afterimageExpiresAt = 0f;
            Array.Clear(
                _reachableTargets,
                0,
                _reachableTargets.Length);
            Array.Clear(
                _inkTransitionStartAlphas,
                0,
                _inkTransitionStartAlphas.Length);
            Array.Clear(
                _inkTransitionStartedAt,
                0,
                _inkTransitionStartedAt.Length);
        }

        public void ResetToInitialMarker(int markerTileIndex)
        {
            ResetToInitialMarkers(
                new[] { markerTileIndex });
        }

        public void ResetToInitialMarkers(
            IReadOnlyCollection<int> markerTileIndices)
        {
            Reset();
            if (markerTileIndices == null)
            {
                return;
            }

            foreach (var temporaryMarkerTileIndex
                     in markerTileIndices)
            {
                if (temporaryMarkerTileIndex < 0 ||
                    temporaryMarkerTileIndex >=
                    _reachableTargets.Length)
                {
                    continue;
                }

                _reachableTargets[temporaryMarkerTileIndex] = true;
                _inkTransitionStartAlphas[
                    temporaryMarkerTileIndex] = 1f;
            }
        }

        public void SetReachableTiles(
            IReadOnlyCollection<int> tileIndices,
            float currentTime)
        {
            var temporaryReachableTiles =
                tileIndices == null
                    ? new HashSet<int>()
                    : new HashSet<int>(tileIndices);
            for (var temporaryTileIndex = 0;
                 temporaryTileIndex <
                 _reachableTargets.Length;
                 temporaryTileIndex++)
            {
                var temporaryIsReachable =
                    temporaryReachableTiles.Contains(
                        temporaryTileIndex);
                if (_reachableTargets[temporaryTileIndex] ==
                    temporaryIsReachable)
                {
                    continue;
                }

                _inkTransitionStartAlphas[
                    temporaryTileIndex] =
                    GetInkAlpha(
                        temporaryTileIndex,
                        currentTime);
                _inkTransitionStartedAt[
                    temporaryTileIndex] =
                    currentTime;
                _reachableTargets[temporaryTileIndex] =
                    temporaryIsReachable;
            }
        }

        public void Focus(int tileIndex, float currentTime)
        {
            if (tileIndex < 0 || tileIndex >= _width * _height)
            {
                return;
            }

            if (_focusedTileIndex == tileIndex)
            {
                return;
            }

            if (_focusedTileIndex >= 0)
            {
                _previousFocusedTileIndex = _focusedTileIndex;
                _afterimageExpiresAt =
                    currentTime + _afterimageDuration;
            }

            _focusedTileIndex = tileIndex;
        }

        public void ClearFocus(int tileIndex, float currentTime)
        {
            if (_focusedTileIndex != tileIndex)
            {
                return;
            }

            _previousFocusedTileIndex = _focusedTileIndex;
            _focusedTileIndex = -1;
            _afterimageExpiresAt =
                currentTime + _afterimageDuration;
        }

        public RoadPresentation GetRoadPresentation(
            int tileIndex,
            float currentTime)
        {
            if (tileIndex < 0 ||
                tileIndex >= _width * _height)
            {
                return RoadPresentation.WhitePaper;
            }

            if (GetInkAlpha(tileIndex, currentTime) > 0f)
            {
                return RoadPresentation.Inked;
            }

            if (_focusedTileIndex >= 0 &&
                tileIndex == _focusedTileIndex)
            {
                return RoadPresentation.Peeking;
            }

            if (_previousFocusedTileIndex >= 0 &&
                currentTime < _afterimageExpiresAt &&
                tileIndex == _previousFocusedTileIndex)
            {
                return RoadPresentation.Afterimage;
            }

            return RoadPresentation.WhitePaper;
        }

        public float GetRoadAlpha(
            int tileIndex,
            float currentTime)
        {
            if (tileIndex < 0 ||
                tileIndex >= _width * _height)
            {
                return 0f;
            }

            var temporaryInkAlpha =
                GetInkAlpha(tileIndex, currentTime);
            if (_focusedTileIndex >= 0 &&
                tileIndex == _focusedTileIndex)
            {
                return 1f;
            }

            if (temporaryInkAlpha > 0f)
            {
                return temporaryInkAlpha;
            }

            if (_previousFocusedTileIndex >= 0 &&
                currentTime < _afterimageExpiresAt &&
                tileIndex == _previousFocusedTileIndex)
            {
                return AFTERIMAGE_ALPHA;
            }

            return 0f;
        }

        float GetInkAlpha(
            int tileIndex,
            float currentTime)
        {
            var temporaryTargetAlpha =
                _reachableTargets[tileIndex] ? 1f : 0f;
            var temporaryProgress = Math.Clamp(
                (currentTime -
                 _inkTransitionStartedAt[tileIndex]) /
                _inkFadeDuration,
                0f,
                1f);
            var temporaryEased =
                temporaryProgress *
                temporaryProgress *
                (3f - 2f * temporaryProgress);
            return _inkTransitionStartAlphas[tileIndex] +
                   (temporaryTargetAlpha -
                    _inkTransitionStartAlphas[tileIndex]) *
                   temporaryEased;
        }

        #endregion
    }
}
