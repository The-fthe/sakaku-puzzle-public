using UnityEngine;

namespace SakakuPuzzle
{
    [CreateAssetMenu(
        fileName = "PuzzleLayoutSettings",
        menuName = "差覚パズル/UI/Layout Settings")]
    public sealed class PuzzleLayoutSettings : ScriptableObject
    {
        #region Board

        [SerializeField, Range(0f, 12f)] float _tileGap;
        [SerializeField, Range(0.2f, 0.6f)] float _travelerSizeRatio = 0.36f;
        [SerializeField, Range(20f, 64f)] float _travelerMinSize = 30f;
        [SerializeField, Range(24f, 80f)] float _travelerMaxSize = 46f;
        [SerializeField, Range(0.25f, 0.8f)] float _destinationSizeRatio = 0.46f;
        [SerializeField, Range(32f, 96f)] float _destinationMinSize = 42f;
        [SerializeField, Range(32f, 128f)] float _destinationMaxSize = 58f;

        public float TileGap => _tileGap;
        public float TravelerSizeRatio => _travelerSizeRatio;
        public float TravelerMinSize => _travelerMinSize;
        public float TravelerMaxSize => _travelerMaxSize;
        public float DestinationSizeRatio => _destinationSizeRatio;
        public float DestinationMinSize => _destinationMinSize;
        public float DestinationMaxSize => _destinationMaxSize;

        #endregion
    }
}
