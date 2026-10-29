using System;
using System.Collections.Generic;

namespace SakakuPuzzle
{
    public sealed class BoardRoadReveal
    {
        readonly int _width;
        readonly int _height;
        readonly int[] _originTileIndices;
        readonly float _waveDelay;
        readonly float _fadeDuration;

        public BoardRoadReveal(
            int width,
            int height,
            IReadOnlyList<int> originTileIndices,
            float waveDelay,
            float fadeDuration)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (originTileIndices == null ||
                originTileIndices.Count == 0)
            {
                throw new ArgumentException(
                    "At least one reveal origin is required.",
                    nameof(originTileIndices));
            }

            _width = width;
            _height = height;
            _waveDelay = Math.Max(0f, waveDelay);
            _fadeDuration = Math.Max(float.Epsilon, fadeDuration);
            _originTileIndices =
                new int[originTileIndices.Count];
            for (var temporaryIndex = 0;
                 temporaryIndex < originTileIndices.Count;
                 temporaryIndex++)
            {
                var temporaryTileIndex =
                    originTileIndices[temporaryIndex];
                ValidateTileIndex(temporaryTileIndex);
                _originTileIndices[temporaryIndex] =
                    temporaryTileIndex;
            }

            var temporaryMaximumDistance = 0;
            for (var temporaryTileIndex = 0;
                 temporaryTileIndex < _width * _height;
                 temporaryTileIndex++)
            {
                temporaryMaximumDistance = Math.Max(
                    temporaryMaximumDistance,
                    GetDistanceToNearestOrigin(
                        temporaryTileIndex));
            }

            Duration =
                temporaryMaximumDistance * _waveDelay +
                _fadeDuration;
        }

        public float Duration { get; }

        public float GetRoadAlpha(
            int tileIndex,
            float elapsedTime)
        {
            ValidateTileIndex(tileIndex);
            var temporaryStartTime =
                GetDistanceToNearestOrigin(tileIndex) *
                _waveDelay;
            var temporaryProgress = Math.Clamp(
                (elapsedTime - temporaryStartTime) /
                _fadeDuration,
                0f,
                1f);
            return temporaryProgress *
                   temporaryProgress *
                   (3f - 2f * temporaryProgress);
        }

        int GetDistanceToNearestOrigin(int tileIndex)
        {
            var temporaryX = tileIndex % _width;
            var temporaryY = tileIndex / _width;
            var temporaryMinimumDistance = int.MaxValue;
            foreach (var temporaryOriginTileIndex
                     in _originTileIndices)
            {
                var temporaryOriginX =
                    temporaryOriginTileIndex % _width;
                var temporaryOriginY =
                    temporaryOriginTileIndex / _width;
                var temporaryDistance =
                    Math.Abs(temporaryX - temporaryOriginX) +
                    Math.Abs(temporaryY - temporaryOriginY);
                temporaryMinimumDistance = Math.Min(
                    temporaryMinimumDistance,
                    temporaryDistance);
            }

            return temporaryMinimumDistance;
        }

        void ValidateTileIndex(int tileIndex)
        {
            if (tileIndex < 0 ||
                tileIndex >= _width * _height)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tileIndex));
            }
        }
    }
}
