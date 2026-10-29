using UnityEngine;

namespace SakakuPuzzle
{
    [CreateAssetMenu(
        fileName = "PuzzleMotionSettings",
        menuName = "差覚パズル/UI/Motion Settings")]
    public sealed class PuzzleMotionSettings : ScriptableObject
    {
        #region Timing

        [SerializeField, Range(0.1f, 1f)] float _tileRotationDuration = 0.36f;
        [SerializeField, Range(1f, 1.5f)] float _tileRotationPeakScale = 1.2f;
        [SerializeField, Range(0.1f, 1f)] float _fadeInDuration = 0.34f;
        [SerializeField, Range(0.1f, 1f)] float _fadeOutDuration = 0.42f;
        [SerializeField, Range(0f, 0.5f)] float _fadeHoldDuration = 0.10f;
        [SerializeField, Range(1f, 8f)] float _travelSpeedInTiles = 3.2f;
        [SerializeField, Range(0.4f, 1f)] float _trailWidthRatio = 0.8f;
        [SerializeField, Range(0.2f, 0.6f)] float _trailVisibleInkWidthRatio = 0.4f;
        [SerializeField, Range(0f, 0.08f)] float _trailWallClearanceRatio = 0.02f;
        [SerializeField, Range(0.3f, 1.5f)] float _trailTextureTileRatio = 0.72f;
        [SerializeField, Range(0.3f, 0.75f)] float _travelRouteCornerRadiusRatio = 0.64f;
        [SerializeField, Range(4, 12)] int _travelRouteCornerSegments = 8;
        [SerializeField, Range(0.1f, 1f)] float _destinationScatterDuration = 0.45f;

        public float TileRotationDuration => _tileRotationDuration;
        public float TileRotationPeakScale => _tileRotationPeakScale;
        public float AnimationDeltaTime =>
            ResolveAnimationDeltaTime(
                Time.deltaTime,
                Time.unscaledDeltaTime,
                Time.captureFramerate);
        public float FadeInDuration => _fadeInDuration;
        public float FadeOutDuration => _fadeOutDuration;
        public float FadeHoldDuration => _fadeHoldDuration;
        public float TravelSpeedInTiles => _travelSpeedInTiles;
        public float TrailWidthRatio => _trailWidthRatio;
        public float TrailVisibleInkWidthRatio =>
            _trailVisibleInkWidthRatio;
        public float TrailWallClearanceRatio =>
            _trailWallClearanceRatio;
        public float TrailTextureTileRatio =>
            _trailTextureTileRatio;
        public float TravelRouteCornerRadiusRatio =>
            _travelRouteCornerRadiusRatio;
        public int TravelRouteCornerSegments =>
            _travelRouteCornerSegments;
        public float DestinationScatterDuration =>
            _destinationScatterDuration;

        public static float ResolveAnimationDeltaTime(
            float deltaTime,
            float unscaledDeltaTime,
            int captureFrameRate)
        {
            return captureFrameRate > 0
                ? deltaTime
                : unscaledDeltaTime;
        }

        #endregion
    }
}
