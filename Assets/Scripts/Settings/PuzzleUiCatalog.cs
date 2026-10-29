using UnityEngine;

namespace SakakuPuzzle
{
    [CreateAssetMenu(
        fileName = "PuzzleUiCatalog",
        menuName = "差覚パズル/UI/Catalog")]
    public sealed class PuzzleUiCatalog : ScriptableObject
    {
        #region Settings

        [SerializeField] PuzzleUiTheme _theme;
        [SerializeField] PuzzleLayoutSettings _layout;
        [SerializeField] PuzzleMotionSettings _motion;
        [SerializeField] LiveServiceAssets _liveServiceAssets;

        public PuzzleUiTheme Theme => _theme;
        public PuzzleLayoutSettings Layout => _layout;
        public PuzzleMotionSettings Motion => _motion;
        public LiveServiceAssets LiveServiceAssets => _liveServiceAssets;

        #endregion

        #region Prefabs

        [SerializeField] PuzzleBoardView _boardPrefab;
        [SerializeField] MapTileView _mapTilePrefab;
        [SerializeField] GameCursorView _gameCursorPrefab;
        [SerializeField] TravelerMarkerView _travelerMarkerPrefab;
        [SerializeField] TravelerBrushTrailView _travelerBrushTrailPrefab;
        [SerializeField] DestinationMarkerView _destinationMarkerPrefab;
        [SerializeField] GameHudView _gameHudPrefab;
        [SerializeField] DebugConsoleView _debugConsolePrefab;
        [SerializeField] ResultPanelView _resultPanelPrefab;
        [SerializeField] HowToPanelView _howToPanelPrefab;
        [SerializeField] StageSelectView _stageSelectViewPrefab;
        [SerializeField] StageNodeView _stageNodePrefab;
        [SerializeField] InkButtonView _inkButtonPrefab;
        [SerializeField] ScreenFadeTransition _screenTransitionPrefab;

        public PuzzleBoardView BoardPrefab => _boardPrefab;
        public MapTileView MapTilePrefab => _mapTilePrefab;
        public GameCursorView GameCursorPrefab => _gameCursorPrefab;
        public TravelerMarkerView TravelerMarkerPrefab =>
            _travelerMarkerPrefab;
        public TravelerBrushTrailView TravelerBrushTrailPrefab =>
            _travelerBrushTrailPrefab;
        public DestinationMarkerView DestinationMarkerPrefab =>
            _destinationMarkerPrefab;
        public GameHudView GameHudPrefab => _gameHudPrefab;
        public DebugConsoleView DebugConsolePrefab =>
            _debugConsolePrefab;
        public ResultPanelView ResultPanelPrefab => _resultPanelPrefab;
        public HowToPanelView HowToPanelPrefab => _howToPanelPrefab;
        public StageSelectView StageSelectViewPrefab =>
            _stageSelectViewPrefab;
        public InkButtonView InkButtonPrefab => _inkButtonPrefab;
        public ScreenFadeTransition ScreenTransitionPrefab =>
            _screenTransitionPrefab;

        #endregion

        #region Validation

        public bool IsValid(out string errorMessage)
        {
            if (_theme == null || _layout == null || _motion == null)
            {
                errorMessage = "UI設定Assetが不足しています。";
                return false;
            }

            if (_boardPrefab == null ||
                _mapTilePrefab == null ||
                _gameCursorPrefab == null ||
                _travelerMarkerPrefab == null ||
                _travelerBrushTrailPrefab == null ||
                _destinationMarkerPrefab == null ||
                _gameHudPrefab == null ||
                _debugConsolePrefab == null ||
                _resultPanelPrefab == null ||
                _howToPanelPrefab == null ||
                _stageSelectViewPrefab == null ||
                _stageNodePrefab == null ||
                _inkButtonPrefab == null ||
                _screenTransitionPrefab == null)
            {
                errorMessage = "UI Prefab参照が不足しています。";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        #endregion
    }
}
