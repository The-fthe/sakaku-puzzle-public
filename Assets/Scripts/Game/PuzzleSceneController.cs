using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace SakakuPuzzle
{
    public sealed class PuzzleSceneController : MonoBehaviour
    {
        const string INSPIRATION_CREDIT_URL =
            "https://x.com/shudankaineko";
        const float SKIP_LABEL_HORIZONTAL_PADDING = 32f;
        const float SKIP_LABEL_VERTICAL_PADDING = 10f;
        const float SKIP_LABEL_MINIMUM_FONT_SIZE = 10f;
        const float SKIP_LABEL_MAXIMUM_FONT_SIZE = 14f;
        const float SKIP_BUTTON_WIDTH = 210f;
        const float SKIP_BUTTON_HEIGHT = 60f;
        const float SKIP_BUTTON_BOTTOM_MARGIN = 26f;
        const float PEEK_AFTERIMAGE_DURATION = 0.7f;
        const float INK_ROAD_FADE_DURATION = 0.22f;
        const float CLEAR_REVEAL_ARRIVAL_HOLD_DURATION = 0.35f;
        const float CLEAR_REVEAL_WAVE_DELAY = 0.08f;
        const float CLEAR_REVEAL_TILE_FADE_DURATION = 0.38f;
        const float CLEAR_BOARD_HOLD_DURATION = 1.5f;
        enum RotationSource
        {
            Player,
            Hint,
            DebugAuto
        }

        static readonly Color PaperColor = new(0.95f, 0.93f, 0.86f);

        static readonly int MovingAnimatorId =
            Animator.StringToHash("Moving");
        static readonly int ArriveAnimatorId =
            Animator.StringToHash("Arrive");

        [SerializeField]
        Camera _uiCamera;
        [SerializeField]
        PuzzleUiCatalog _uiCatalog;

        PuzzleUiTheme Theme => _uiCatalog.Theme;
        PuzzleLayoutSettings Layout => _uiCatalog.Layout;
        PuzzleMotionSettings Motion => _uiCatalog.Motion;
        float TileGap => Layout.TileGap;
        float TravelSpeedInTiles => Motion.TravelSpeedInTiles;

        PuzzleDefinition _definition;
        PuzzleState _state;
        BoardPresentationState _boardPresentation;
        BoardRoadReveal _boardRoadReveal;
        IScoreRepository _scoreRepository;
        Texture2D _landscapeBackdrop;
        Texture2D _portraitBackdrop;

        Canvas _canvas;
        RawImage _backgroundImage;
        RectTransform _safeArea;
        RectTransform _header;
        RectTransform _titleRect;
        RectTransform _subtitleRect;
        RectTransform _headerLine;
        RectTransform _stageSelectButton;
        RectTransform _boardFrame;
        RectTransform _boardGrid;
        RectTransform _boardOverlay;
        PuzzleBoardView _boardView;
        RectTransform _sidePanel;
        GridLayoutGroup _gridLayout;
        TMP_Text _titleText;
        TMP_Text _subtitleText;
        TMP_Text _missionText;
        TMP_Text _instructionText;
        TMP_Text _statusText;
        TMP_Text _stepsText;
        TMP_Text _minimumText;
        TMP_Text _bestText;
        TMP_Text _leaderboardText;
        RectTransform[] _metricCards;
        RectTransform _hintButton;
        RectTransform _resetButton;
        RectTransform _debugButton;
        RectTransform _autoClearButton;
        RectTransform _inspirationCreditButton;
        RectTransform _debugToggleButton;
        RectTransform _topResetButton;
        RectTransform _skipButton;
        RectTransform _movesHudContainer;
        GameHudView _gameHud;
        RectTransform _resultOverlay;
        ResultPanelView _resultPanelView;
        HowToPanelView _howToPanelView;
        GameCursorView _gameCursorView;
        RectTransform _gameCursor;
        RectTransform _resultNextButton;
        TMP_Text _movesHudText;
        TMP_Text _cumulativeHudText;
        TMP_Text _resultTitleText;
        TMP_Text _resultDetailText;
        TMP_Text _resultLeaderboardText;
        TMP_Text _debugText;
        readonly MapTileView[] _tileViews = new MapTileView[25];

        RectTransform _startMarker;
        readonly List<RectTransform> _goalMarkers = new();
        TravelerMarkerView _startMarkerView;
        readonly List<DestinationMarkerView> _goalMarkerViews = new();
        RectTransform _traveler;
        TravelerMarkerView _travelerView;
        TravelerBrushTrailView _travelerBrushTrailView;
        Animator _travelerAnimator;
        ScreenFadeTransition _sceneTransition;
        readonly List<Vector2> _travelPoints = new();
        readonly List<float> _travelSegments = new();
        readonly List<int> _goalArrivalPointIndices = new();
        readonly List<float> _goalArrivalDistances = new();
        readonly List<int> _goalArrivalMarkerIndices = new();
        readonly List<TravelerRouteGeometry.WallSegment>
            _travelerWallSegments = new();
        float _travelDistance;
        float _travelLength;
        float _cellSize;
        Vector2 _travelerDirection = Vector2.up;
        int _nextGoalArrivalIndex;
        int _minimumSteps;
        int _bestSteps = -1;
        int _playerRotations;
        int _hintRotations;
        int _autoRotations;
        int _activeTileAnimations;
        bool _wasSolved;
        bool _debugMode;
        bool _assistedRun;
        bool _autoSolving;
        bool _resultShown;
        bool _stageCompletionRecorded;
        bool _travelerCompleting;
        bool _travelerRoadEndpointsOnly;
        bool _boardRoadRevealRunning;
        bool _boardRoadFullyRevealed;
        MapTileView _gameCursorHoveredTile;
        Coroutine _autoClearCoroutine;
        Coroutine _travelerCompletionCoroutine;
        Vector2Int _lastScreenSize;
        Rect _lastSafeArea;
        int _version = 1;
        int _puzzleSerial;

        #region Unity Lifecycle

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_uiCatalog == null)
            {
                return;
            }

            if (!_uiCatalog.IsValid(out var temporaryError))
            {
                Debug.LogError(
                    $"[PuzzleSceneController] {temporaryError}",
                    this);
            }
        }
#endif

        void Awake()
        {
            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            _version = StageSession.SelectedStage;
            _definition = PuzzleDefinition.CreateStage(
                _version,
                CreatePuzzleSeed());
            _state = new PuzzleState(_definition);
            _boardPresentation = new BoardPresentationState(
                _definition.Width,
                _definition.Height,
                PEEK_AFTERIMAGE_DURATION,
                INK_ROAD_FADE_DURATION);
            _boardPresentation.ResetToInitialMarkers(
                GetTravelerEndpointTileIndices());
            _boardRoadReveal = CreateBoardRoadReveal();
            _scoreRepository = new InMemoryScoreRepository();
            _minimumSteps = _definition.MinimumSteps;
            _landscapeBackdrop = Theme.LandscapeBackdrop;
            _portraitBackdrop = Theme.PortraitBackdrop;
            SoundFeedback.Configure(Theme);
            BgmPlayer.Play();
            LiveServices.Configure(_uiCatalog.LiveServiceAssets);

            Input.multiTouchEnabled = false;
            Input.simulateMouseWithTouches = true;
            Application.targetFrameRate = 60;

            CreateRuntimeAssets();
            EnsureCamera();
            BuildInterface();
        }

        void Start()
        {
            ApplyResponsiveLayout(true);
            RefreshInterface();
            if (HowToState.ShouldShowAutomatically)
            {
                _howToPanelView.Show();
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetByPlayer();
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                ApplyHint();
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                ToggleDebugMode();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                StartAutoClear();
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                SwitchVersion(_version);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OpenStageSelect();
            }

            var temporaryScreenSize = new Vector2Int(Screen.width, Screen.height);
            if (_lastScreenSize != temporaryScreenSize ||
                _lastSafeArea != Screen.safeArea)
            {
                ApplyResponsiveLayout(true);
            }

            UpdateTraveler();
            RefreshRoadPresentations(Time.unscaledTime);
            UpdateGameCursor();
        }

        void OnDestroy()
        {
            Cursor.visible = true;
            if (_howToPanelView != null)
            {
                _howToPanelView.StartRequested -= StartFromHowTo;
            }

            foreach (var temporaryTileView in _tileViews)
            {
                if (temporaryTileView == null)
                {
                    continue;
                }

                temporaryTileView.PointerEntered -=
                    OnMapTilePointerEntered;
                temporaryTileView.PointerExited -=
                    OnMapTilePointerExited;
            }
        }

        bool ValidateConfiguration()
        {
            if (_uiCatalog == null)
            {
                Debug.LogError(
                    "[PuzzleSceneController] PuzzleUiCatalogが未設定です。",
                    this);
                return false;
            }

            if (_uiCatalog.IsValid(out var temporaryError))
            {
                return true;
            }

            Debug.LogError(
                $"[PuzzleSceneController] {temporaryError}",
                this);
            return false;
        }

        #endregion

        #region Interface Construction

        void CreateRuntimeAssets()
        {
            _landscapeBackdrop = Theme.LandscapeBackdrop;
            _portraitBackdrop = Theme.PortraitBackdrop;
        }

        void EnsureCamera()
        {
            if (_uiCamera == null)
            {
                _uiCamera = Camera.main;
            }

            if (_uiCamera == null)
            {
                var temporaryCameraObject = new GameObject(
                    "Main Camera",
                    typeof(Camera),
                    typeof(AudioListener));
                temporaryCameraObject.tag = "MainCamera";
                _uiCamera = temporaryCameraObject.GetComponent<Camera>();
            }

            _uiCamera.transform.SetPositionAndRotation(
                new Vector3(0f, 0f, -10f),
                Quaternion.identity);
            _uiCamera.orthographic = true;
            _uiCamera.orthographicSize = 5f;
            _uiCamera.clearFlags = CameraClearFlags.SolidColor;
            _uiCamera.backgroundColor = PaperColor;
            _uiCamera.nearClipPlane = 0.1f;
            _uiCamera.farClipPlane = 100f;
        }

        void BuildInterface()
        {
            EnsureEventSystem();
            BuildCanvas();
            BuildHeader();
            BuildBoard();
            BuildSidePanel();
            BuildResultOverlay();
            BuildHowToOverlay();
            _debugToggleButton.SetAsLastSibling();
            _sceneTransition = Instantiate(
                _uiCatalog.ScreenTransitionPrefab,
                _canvas.transform);
            _sceneTransition.Configure(
                _canvas.transform,
                Motion);
            BuildGameCursor();
        }

        void BuildGameCursor()
        {
            _gameCursorView = Instantiate(
                _uiCatalog.GameCursorPrefab,
                _canvas.transform);
            _gameCursorView.name = "ゲーム内カーソル";
            _gameCursorView.Configure(
                Theme.GameCursorSprite,
                Color.black,
                GameCursorView.HOVER_ALPHA);
            _gameCursor = _gameCursorView.RectTransform;
            _gameCursorView.ApplyCanvasPresentation(_canvas);
        }

        void UpdateGameCursor()
        {
            if (_gameCursor == null ||
                !_gameCursor.gameObject.activeSelf)
            {
                return;
            }

            SetGameCursorScreenPosition(Input.mousePosition);
        }

        void SetGameCursorScreenPosition(Vector2 screenPosition)
        {
            _gameCursorView?.SetScreenPosition(
                _canvas,
                screenPosition);
        }

        void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var temporaryEventSystem = new GameObject(
                "EventSystem",
                typeof(EventSystem));
            temporaryEventSystem.transform.SetParent(transform, false);
#if ENABLE_INPUT_SYSTEM
            temporaryEventSystem.AddComponent<InputSystemUIInputModule>();
#else
            temporaryEventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        void BuildCanvas()
        {
            var temporaryCanvasObject = new GameObject(
                "Puzzle UI Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            temporaryCanvasObject.transform.SetParent(transform, false);

            _canvas = temporaryCanvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = _uiCamera;
            _canvas.planeDistance = 1f;

            var temporaryScaler = temporaryCanvasObject.GetComponent<CanvasScaler>();
            temporaryScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            temporaryScaler.referenceResolution = new Vector2(1280f, 720f);
            temporaryScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            temporaryScaler.matchWidthOrHeight = 0.5f;

            _backgroundImage = CreateUiObject<RawImage>(
                "生成背景",
                temporaryCanvasObject.transform);
            Stretch(_backgroundImage.rectTransform);
            _backgroundImage.raycastTarget = false;
            _backgroundImage.color = Color.white;

            _safeArea = CreateRect("セーフエリア", temporaryCanvasObject.transform);
            Stretch(_safeArea);
        }

        void BuildHeader()
        {
            var temporaryHud = Instantiate(
                _uiCatalog.GameHudPrefab,
                _safeArea);
            temporaryHud.Configure(Theme);
            temporaryHud.StageSelectRequested += OpenStageSelect;
            temporaryHud.ResetRequested += ResetByPlayer;
            temporaryHud.DebugRequested += ToggleDebugMode;
            temporaryHud.SetDebugAvailable(
                Debug.isDebugBuild || Application.isEditor);
            _gameHud = temporaryHud;
            _header = temporaryHud.RectTransform;
            _titleText = temporaryHud.Title;
            _titleRect = temporaryHud.Title.rectTransform;
            _subtitleText = temporaryHud.Subtitle;
            _subtitleRect = temporaryHud.Subtitle.rectTransform;
            _headerLine = temporaryHud.Divider;
            _stageSelectButton = temporaryHud.StageSelectButton;
            _movesHudContainer = temporaryHud.MovesContainer;
            _movesHudText = temporaryHud.Moves;
            _cumulativeHudText = temporaryHud.CumulativeMoves;
            _topResetButton = temporaryHud.ResetButton;
            _debugToggleButton = temporaryHud.DebugButton;
        }

        void BuildBoard()
        {
            var temporaryBoard = Instantiate(
                _uiCatalog.BoardPrefab,
                _safeArea);
            temporaryBoard.Configure();
            _boardView = temporaryBoard;
            _boardFrame = temporaryBoard.RectTransform;
            _boardGrid = temporaryBoard.GridRoot;
            _boardOverlay = temporaryBoard.OverlayRoot;
            _gridLayout = temporaryBoard.GridLayout;
            _gridLayout.spacing = new Vector2(TileGap, TileGap);

            for (var temporaryIndex = 0; temporaryIndex < _definition.TileCount; temporaryIndex++)
            {
                BuildTile(temporaryIndex);
            }

            RebuildEndpointMarkers();
            _boardOverlay.SetAsLastSibling();
            BuildTraveler();
        }

        void BuildTile(int tileIndex)
        {
            var temporaryView = Instantiate(
                _uiCatalog.MapTilePrefab,
                _boardGrid);
            temporaryView.Configure(tileIndex, Theme);
            temporaryView.Clicked += OnMapTileClicked;
            temporaryView.PointerEntered +=
                OnMapTilePointerEntered;
            temporaryView.PointerExited +=
                OnMapTilePointerExited;
            temporaryView.SetRoadPresentation(
                RoadPresentation.WhitePaper);
            temporaryView.RotationAngle =
                _state.IsAlternateOrientation(tileIndex) ? -90f : 0f;
            temporaryView.DisplayedAngle = temporaryView.RotationAngle;
            temporaryView.VisualRect.localEulerAngles =
                new Vector3(0f, 0f, temporaryView.RotationAngle);
            _tileViews[tileIndex] = temporaryView;
        }

        void OnMapTileClicked(MapTileView tile)
        {
            RotateTile(tile.Index, RotationSource.Player);
        }

        void OnMapTilePointerEntered(MapTileView tile)
        {
            _gameCursorHoveredTile = tile;
            _gameCursorView?.SetHovering(true);
            _boardPresentation.Focus(
                tile.Index,
                Time.unscaledTime);
            RefreshRoadPresentations(Time.unscaledTime);
        }

        void OnMapTilePointerExited(MapTileView tile)
        {
            if (_gameCursorHoveredTile == tile)
            {
                _gameCursorHoveredTile = null;
                _gameCursorView?.SetHovering(false);
            }

            _boardPresentation.ClearFocus(
                tile.Index,
                Time.unscaledTime);
            RefreshRoadPresentations(Time.unscaledTime);
        }

        void RebuildEndpointMarkers()
        {
            if (_startMarker != null)
            {
                _startMarker.gameObject.SetActive(false);
                Destroy(_startMarker.gameObject);
            }

            foreach (var temporaryMarker in _goalMarkers)
            {
                if (temporaryMarker != null)
                {
                    temporaryMarker.gameObject.SetActive(false);
                    Destroy(temporaryMarker.gameObject);
                }
            }

            _goalMarkers.Clear();
            _goalMarkerViews.Clear();
            _startMarkerView = Instantiate(
                _uiCatalog.TravelerMarkerPrefab,
                _tileViews[_definition.StartIndex].RectTransform);
            _startMarkerView.name = "プレイヤー開始位置";
            _startMarkerView.Configure(Theme);
            _startMarker = _startMarkerView.RectTransform;
            PositionMarkerAtCorner(
                _startMarker,
                _definition.StartCorner);
            FaceStartMarkerIntoRoad();

            for (var temporaryIndex = 0;
                 temporaryIndex < _definition.Goals.Count;
                 temporaryIndex++)
            {
                var temporaryGoal = _definition.Goals[temporaryIndex];
                var temporaryMarker = Instantiate(
                    _uiCatalog.DestinationMarkerPrefab,
                    _tileViews[temporaryGoal.TileIndex].RectTransform);
                temporaryMarker.Configure(Theme);
                PositionMarkerAtCorner(
                    temporaryMarker.RectTransform,
                    temporaryGoal.Corner);
                _goalMarkerViews.Add(temporaryMarker);
                _goalMarkers.Add(temporaryMarker.RectTransform);
            }
        }

        void BuildTraveler()
        {
            _travelerBrushTrailView = Instantiate(
                _uiCatalog.TravelerBrushTrailPrefab,
                _boardOverlay);
            _travelerBrushTrailView.name = "旅人の筆跡";
            var temporaryTrailRect =
                (RectTransform)_travelerBrushTrailView.transform;
            temporaryTrailRect.anchorMin =
                new Vector2(0.5f, 0.5f);
            temporaryTrailRect.anchorMax =
                new Vector2(0.5f, 0.5f);
            temporaryTrailRect.pivot =
                new Vector2(0.5f, 0.5f);
            temporaryTrailRect.anchoredPosition = Vector2.zero;
            temporaryTrailRect.localRotation =
                Quaternion.identity;
            temporaryTrailRect.localScale = Vector3.one;

            _travelerView = Instantiate(
                _uiCatalog.TravelerMarkerPrefab,
                _boardOverlay);
            _travelerView.name = "プレイヤー移動キャラクター";
            _travelerView.Configure(Theme);
            _traveler = _travelerView.RectTransform;
            _traveler.anchorMin = new Vector2(0.5f, 0.5f);
            _traveler.anchorMax = new Vector2(0.5f, 0.5f);
            _traveler.pivot = new Vector2(0.5f, 0.5f);
            _traveler.sizeDelta = new Vector2(52f, 52f);
            _travelerAnimator = _travelerView.Animator;
            _traveler.gameObject.SetActive(false);
        }

        void BuildSidePanel()
        {
            var temporaryConsole = Instantiate(
                _uiCatalog.DebugConsolePrefab,
                _safeArea);
            temporaryConsole.Configure(Theme);
            temporaryConsole.HintRequested += ApplyHint;
            temporaryConsole.RegenerateRequested +=
                () => SwitchVersion(_version);
            temporaryConsole.CloseRequested += ToggleDebugMode;
            temporaryConsole.AutoClearRequested += StartAutoClear;
            temporaryConsole.CreditRequested += OpenInspirationCredit;
            _sidePanel = temporaryConsole.RectTransform;
            _missionText = temporaryConsole.Mission;
            _instructionText = temporaryConsole.Instruction;
            _statusText = temporaryConsole.Status;
            _stepsText = temporaryConsole.Steps;
            _minimumText = temporaryConsole.Minimum;
            _bestText = temporaryConsole.Best;
            _debugText = temporaryConsole.Details;
            _leaderboardText = temporaryConsole.Leaderboard;
            _metricCards = temporaryConsole.MetricCards;
            _hintButton = temporaryConsole.HintButton;
            _resetButton = temporaryConsole.RegenerateButton;
            _debugButton = temporaryConsole.HeaderCloseButton;
            _autoClearButton = temporaryConsole.AutoClearButton;
            _inspirationCreditButton = temporaryConsole.CreditButton;
            _sidePanel.gameObject.SetActive(false);
        }

        void BuildResultOverlay()
        {
            var temporaryResult = Instantiate(
                _uiCatalog.ResultPanelPrefab,
                _safeArea);
            temporaryResult.Configure(Theme);
            _resultPanelView = temporaryResult;
            temporaryResult.NextRequested += ContinueAfterResult;
            _resultOverlay = temporaryResult.RectTransform;
            Stretch(_resultOverlay);
            _resultTitleText = temporaryResult.Title;
            _resultDetailText = temporaryResult.Detail;
            _resultLeaderboardText = temporaryResult.Leaderboard;
            _resultNextButton = temporaryResult.NextButton;

            var temporarySkip = Instantiate(
                _uiCatalog.InkButtonPrefab,
                _safeArea);
            temporarySkip.name = "移動演出をスキップ";
            temporarySkip.Configure("演出をスキップ", Theme);
            temporarySkip.SetLabelSafeArea(
                new Vector2(
                    SKIP_LABEL_HORIZONTAL_PADDING,
                    SKIP_LABEL_VERTICAL_PADDING),
                SKIP_LABEL_MINIMUM_FONT_SIZE,
                SKIP_LABEL_MAXIMUM_FONT_SIZE);
            temporarySkip.Clicked += SkipTraveler;
            _skipButton = temporarySkip.RectTransform;
            _skipButton.SetAsLastSibling();
            _skipButton.gameObject.SetActive(false);
            temporaryResult.Hide();
        }

        void BuildHowToOverlay()
        {
            _howToPanelView = Instantiate(
                _uiCatalog.HowToPanelPrefab,
                _safeArea);
            _howToPanelView.Configure(Theme);
            _howToPanelView.StartRequested += StartFromHowTo;
            Stretch(_howToPanelView.RectTransform);
            _howToPanelView.Hide();
        }

        void StartFromHowTo()
        {
            HowToState.MarkSeen();
            _howToPanelView.Hide();
        }

        #endregion

        #region Responsive Layout

        void ApplyResponsiveLayout(bool force)
        {
            var temporaryScreenSize = new Vector2Int(Screen.width, Screen.height);
            var temporarySafeArea = Screen.safeArea;
            if (!force &&
                _lastScreenSize == temporaryScreenSize &&
                _lastSafeArea == temporarySafeArea)
            {
                return;
            }

            _lastScreenSize = temporaryScreenSize;
            _lastSafeArea = temporarySafeArea;

            var temporaryCanvasRect =
                (RectTransform)_canvas.transform;
            _safeArea.anchorMin = new Vector2(
                temporarySafeArea.xMin / Mathf.Max(1f, Screen.width),
                temporarySafeArea.yMin / Mathf.Max(1f, Screen.height));
            _safeArea.anchorMax = new Vector2(
                temporarySafeArea.xMax / Mathf.Max(1f, Screen.width),
                temporarySafeArea.yMax / Mathf.Max(1f, Screen.height));
            _safeArea.offsetMin = Vector2.zero;
            _safeArea.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();

            var temporaryWidth = _safeArea.rect.width;
            var temporaryHeight = _safeArea.rect.height;
            _howToPanelView?.FitToViewport(
                new Vector2(temporaryWidth, temporaryHeight));
            var temporaryPortrait = temporaryHeight > temporaryWidth * 1.08f;
            _backgroundImage.texture = temporaryPortrait
                ? _portraitBackdrop
                : _landscapeBackdrop;
            _backgroundImage.uvRect = new Rect(0f, 0f, 1f, 1f);

            if (temporaryPortrait)
            {
                LayoutPortrait(temporaryWidth, temporaryHeight);
            }
            else
            {
                LayoutLandscape(temporaryWidth, temporaryHeight);
            }

            LayoutBoardCells();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_boardGrid);

            if (_wasSolved)
            {
                BuildTravelerRoute();
            }
        }

        void LayoutLandscape(float width, float height)
        {
            _subtitleText.alignment = TextAlignmentOptions.Left;
            _subtitleText.fontSize = 24f;
            const float HEADER_HEIGHT = 132f;
            _gameHud.SetMovesDisplayCompact(true);
            SetTopLeft(_header, 0f, 0f, width, HEADER_HEIGHT);
            SetTopLeft(_titleRect, 42f, 14f, 310f, 62f);
            SetTopLeft(
                _stageSelectButton,
                width - 450f,
                8f,
                256f,
                80f);
            SetTopLeft(
                _topResetButton,
                width - 210f,
                8f,
                208f,
                80f);
            SetTopLeft(
                _debugToggleButton,
                width - 102f,
                height - 102f,
                80f,
                80f);
            SetTopLeft(
                _movesHudContainer,
                width * 0.5f - 190f,
                0f,
                380f,
                126f);
            SetTopLeft(_subtitleRect, 80f, 72f, 324f, 36f);
            SetTopLeft(
                _headerLine,
                48f,
                HEADER_HEIGHT - 2f,
                width - 96f,
                1f);

            var temporaryBoardSize = Mathf.Min(
                560f,
                height - HEADER_HEIGHT - 24f,
                width * 0.54f);
            var temporaryBoardX = (width - temporaryBoardSize) * 0.5f;
            var temporaryBoardY =
                HEADER_HEIGHT +
                (height - HEADER_HEIGHT - temporaryBoardSize) * 0.5f;
            SetTopLeft(
                _boardFrame,
                temporaryBoardX,
                temporaryBoardY,
                temporaryBoardSize,
                temporaryBoardSize);
            _resultPanelView?.LayoutAroundBoard(
                new Vector2(width, height),
                new Vector2(
                    temporaryBoardX,
                    temporaryBoardY),
                temporaryBoardSize,
                false);

            var temporaryPanelWidth =
                Mathf.Min(500f, width * 0.40f);
            var temporaryPanelX =
                width - temporaryPanelWidth - 22f;
            var temporaryPanelY = HEADER_HEIGHT + 14f;
            var temporaryPanelHeight =
                height - temporaryPanelY - 22f;
            SetTopLeft(
                _sidePanel,
                temporaryPanelX,
                temporaryPanelY,
                temporaryPanelWidth,
                temporaryPanelHeight);
            LayoutSidePanel(
                temporaryPanelWidth,
                temporaryPanelHeight,
                false);
            SetTopLeft(
                _skipButton,
                32f,
                height -
                SKIP_BUTTON_HEIGHT -
                SKIP_BUTTON_BOTTOM_MARGIN,
                SKIP_BUTTON_WIDTH,
                SKIP_BUTTON_HEIGHT);
        }

        void LayoutPortrait(float width, float height)
        {
            const float HEADER_HEIGHT = 172f;
            _subtitleText.fontSize = 18f;
            _gameHud.SetMovesDisplayCompact(false);
            SetTopLeft(_header, 0f, 0f, width, HEADER_HEIGHT);
            SetTopLeft(_titleRect, 24f, 12f, width * 0.31f, 64f);
            SetTopLeft(
                _stageSelectButton,
                width - 220f,
                4f,
                196f,
                80f);
            SetTopLeft(
                _topResetButton,
                width - 220f,
                88f,
                196f,
                80f);
            SetTopLeft(
                _debugToggleButton,
                width - 102f,
                height - 102f,
                80f,
                80f);
            SetTopLeft(
                _subtitleRect,
                24f,
                82f,
                width * 0.31f,
                30f);
            _subtitleText.alignment = TextAlignmentOptions.Left;
            SetTopLeft(
                _headerLine,
                32f,
                HEADER_HEIGHT - 3f,
                width - 64f,
                1f);

            var temporaryBoardSize = Mathf.Min(width - 64f, 620f);
            var temporaryBoardX = (width - temporaryBoardSize) * 0.5f;
            var temporaryBoardY =
                HEADER_HEIGHT +
                (height - HEADER_HEIGHT - temporaryBoardSize) * 0.5f;
            SetTopLeft(
                _movesHudContainer,
                width * 0.5f - 190f,
                temporaryBoardY - 180f,
                380f,
                168f);
            SetTopLeft(
                _boardFrame,
                temporaryBoardX,
                temporaryBoardY,
                temporaryBoardSize,
                temporaryBoardSize);
            _resultPanelView?.LayoutAroundBoard(
                new Vector2(width, height),
                new Vector2(
                    temporaryBoardX,
                    temporaryBoardY),
                temporaryBoardSize,
                true);

            var temporaryPanelHeight =
                Mathf.Min(560f, height * 0.38f);
            var temporaryPanelY =
                height - temporaryPanelHeight - 20f;
            SetTopLeft(
                _sidePanel,
                20f,
                temporaryPanelY,
                width - 40f,
                temporaryPanelHeight);
            LayoutSidePanel(
                width - 40f,
                temporaryPanelHeight,
                true);
            SetTopLeft(
                _skipButton,
                24f,
                height -
                SKIP_BUTTON_HEIGHT -
                SKIP_BUTTON_BOTTOM_MARGIN,
                SKIP_BUTTON_WIDTH,
                SKIP_BUTTON_HEIGHT);
        }

        void LayoutBoardCells()
        {
            var temporaryInnerSize =
                _boardFrame.rect.width *
                (_boardGrid.anchorMax.x - _boardGrid.anchorMin.x) +
                _boardGrid.sizeDelta.x;
            _cellSize =
                (temporaryInnerSize - TileGap * (_definition.Width - 1)) /
                _definition.Width;
            _gridLayout.cellSize = new Vector2(_cellSize, _cellSize);

            var temporaryMarkerSize =
                Mathf.Clamp(
                    _cellSize * Layout.DestinationSizeRatio,
                    Layout.DestinationMinSize,
                    Layout.DestinationMaxSize);
            var temporaryPlayerSize =
                Mathf.Clamp(
                    _cellSize * Layout.TravelerSizeRatio,
                    Layout.TravelerMinSize,
                    Layout.TravelerMaxSize);
            _startMarker.sizeDelta =
                new Vector2(temporaryPlayerSize, temporaryPlayerSize);
            _traveler.sizeDelta =
                new Vector2(temporaryPlayerSize, temporaryPlayerSize);
            PositionMarkerAtCorner(_startMarker, _definition.StartCorner);
            FaceStartMarkerIntoRoad();
            for (var temporaryIndex = 0;
                 temporaryIndex < _goalMarkers.Count;
                 temporaryIndex++)
            {
                var temporaryMarker = _goalMarkers[temporaryIndex];
                var temporaryGoal = _definition.Goals[temporaryIndex];
                temporaryMarker.sizeDelta =
                    new Vector2(temporaryMarkerSize, temporaryMarkerSize);
                PositionMarkerAtCorner(
                    temporaryMarker,
                    temporaryGoal.Corner);
            }
        }

        void LayoutSidePanel(float width, float height, bool portrait)
        {
            var temporaryPadding = portrait ? 24f : 28f;
            var temporaryContentWidth = width - temporaryPadding * 2f;
            const float HEADER_CLOSE_SIZE = 44f;
            SetTopLeft(
                _missionText.rectTransform,
                temporaryPadding,
                16f,
                temporaryContentWidth - HEADER_CLOSE_SIZE - 12f,
                22f);
            SetTopLeft(
                _debugButton,
                temporaryPadding +
                temporaryContentWidth -
                HEADER_CLOSE_SIZE,
                8f,
                HEADER_CLOSE_SIZE,
                HEADER_CLOSE_SIZE);

            SetTopLeft(
                _instructionText.rectTransform,
                temporaryPadding,
                43f,
                temporaryContentWidth,
                62f);
            SetTopLeft(
                _statusText.rectTransform,
                temporaryPadding,
                106f,
                temporaryContentWidth,
                28f);

            var temporaryMetricY = 138f;
            var temporaryMetricGap = 10f;
            var temporaryMetricWidth =
                (temporaryContentWidth - temporaryMetricGap * 2f) / 3f;
            for (var temporaryIndex = 0; temporaryIndex < _metricCards.Length; temporaryIndex++)
            {
                SetTopLeft(
                    _metricCards[temporaryIndex],
                    temporaryPadding +
                    (temporaryMetricWidth + temporaryMetricGap) * temporaryIndex,
                    temporaryMetricY,
                    temporaryMetricWidth,
                    70f);

                foreach (RectTransform temporaryChild in _metricCards[temporaryIndex])
                {
                    var temporaryChildWidth = temporaryMetricWidth - 8f;
                    temporaryChild.sizeDelta = new Vector2(
                        temporaryChildWidth,
                        temporaryChild.sizeDelta.y);
                }
            }

            var temporaryButtonY = 216f;
            var temporaryButtonGap = 12f;
            var temporaryButtonWidth =
                (temporaryContentWidth - temporaryButtonGap) * 0.5f;
            SetTopLeft(
                _hintButton,
                temporaryPadding,
                temporaryButtonY,
                temporaryButtonWidth,
                50f);
            SetTopLeft(
                _resetButton,
                temporaryPadding + temporaryButtonWidth + temporaryButtonGap,
                temporaryButtonY,
                temporaryButtonWidth,
                50f);

            var temporaryDebugButtonY = 274f;
            SetTopLeft(
                _autoClearButton,
                temporaryPadding,
                temporaryDebugButtonY,
                temporaryContentWidth,
                50f);

            var temporaryDetailsY = 336f;
            var temporaryCreditHeight = 32f;
            var temporaryCreditY = height - temporaryCreditHeight - 8f;
            var temporaryDetailsHeight = Mathf.Max(
                0f,
                temporaryCreditY - temporaryDetailsY - 6f);
            SetTopLeft(
                _leaderboardText.rectTransform,
                temporaryPadding,
                temporaryDetailsY,
                temporaryContentWidth,
                temporaryDetailsHeight);
            SetTopLeft(
                _debugText.rectTransform,
                temporaryPadding,
                temporaryDetailsY,
                temporaryContentWidth,
                temporaryDetailsHeight);
            SetTopLeft(
                _inspirationCreditButton,
                temporaryPadding,
                temporaryCreditY,
                temporaryContentWidth,
                temporaryCreditHeight);
        }

        void PositionMarkerAtCorner(RectTransform marker, TileCorner corner)
        {
            var temporaryInset = Mathf.Max(
                marker.sizeDelta.x * 0.58f,
                _cellSize * 0.20f);
            var temporaryNormalizedPosition =
                TilePathGraphic.GetRoadPointFromCorner(
                    corner,
                    temporaryInset / Mathf.Max(_cellSize, 0.01f));
            marker.anchorMin = temporaryNormalizedPosition;
            marker.anchorMax = temporaryNormalizedPosition;
            marker.anchoredPosition = Vector2.zero;
            marker.pivot = new Vector2(0.5f, 0.5f);
        }

        void FaceStartMarkerIntoRoad()
        {
            if (_startMarkerView == null)
            {
                return;
            }

            _startMarkerView.FaceDirection(
                TilePathGraphic.GetRoadDirectionFromCorner(
                    _definition.StartCorner));
        }

        #endregion

        #region Puzzle Interaction

        void OpenInspirationCredit()
        {
            Application.OpenURL(INSPIRATION_CREDIT_URL);
        }

        void OpenStageSelect()
        {
            if (_sceneTransition == null)
            {
                return;
            }

            _sceneTransition.FadeToScene(
                "StageSelect",
                StageSession.AbandonToStageSelect);
        }

        void SwitchVersion(int version)
        {
            if (_autoClearCoroutine != null)
            {
                StopCoroutine(_autoClearCoroutine);
                _autoClearCoroutine = null;
            }

            foreach (var temporaryView in _tileViews)
            {
                if (temporaryView?.AnimationCoroutine != null)
                {
                    StopCoroutine(temporaryView.AnimationCoroutine);
                    temporaryView.AnimationCoroutine = null;
                }
            }

            _version = Mathf.Clamp(version, 1, 4);
            _definition = PuzzleDefinition.CreateStage(
                _version,
                CreatePuzzleSeed());
            _state = new PuzzleState(_definition);
            _boardRoadReveal = CreateBoardRoadReveal();
            _minimumSteps = _definition.MinimumSteps;
            _bestSteps = -1;
            RebuildEndpointMarkers();
            ResetPuzzle();
            ApplyResponsiveLayout(true);
        }

        int CreatePuzzleSeed()
        {
            _puzzleSerial++;
            return unchecked(
                Environment.TickCount ^
                (_puzzleSerial * 7919) ^
                (int)DateTime.UtcNow.Ticks);
        }

        void RotateTile(int tileIndex, RotationSource source)
        {
            if (_state.IsSolved ||
                _activeTileAnimations > 0 ||
                (_autoSolving && source != RotationSource.DebugAuto) ||
                !_state.Rotate(tileIndex))
            {
                return;
            }

            switch (source)
            {
                case RotationSource.Player:
                    _playerRotations++;
                    break;
                case RotationSource.Hint:
                    _hintRotations++;
                    _assistedRun = true;
                    StageSession.MarkAssisted();
                    break;
                case RotationSource.DebugAuto:
                    _autoRotations++;
                    _assistedRun = true;
                    StageSession.MarkAssisted();
                    break;
            }

            var temporaryView = _tileViews[tileIndex];
            _travelerBrushTrailView?.SetVisible(false);
            temporaryView.RotationAngle -= 90f;
            SoundFeedback.PlayRotate();
            temporaryView.AnimationCoroutine = StartCoroutine(
                AnimateTileRotation(temporaryView));
            RefreshInterface();
        }

        IEnumerator AnimateTileRotation(MapTileView tileView)
        {
            var temporaryDuration = Motion.TileRotationDuration;
            var temporaryPeakScale = Motion.TileRotationPeakScale;
            var temporaryVisual = tileView.VisualRect;
            var temporaryStartAngle = tileView.DisplayedAngle;
            var temporaryTargetAngle = tileView.RotationAngle;
            var temporaryElapsed = 0f;
            tileView.IsAnimating = true;
            _activeTileAnimations++;
            tileView.SetRotationOutlineVisible(true);
            tileView.SortingCanvas.overrideSorting = true;
            tileView.SortingCanvas.sortingOrder = 1;

            while (temporaryElapsed < temporaryDuration)
            {
                temporaryElapsed += Motion.AnimationDeltaTime;
                var temporaryProgress =
                    Mathf.Clamp01(
                        temporaryElapsed / temporaryDuration);
                var temporaryEased =
                    temporaryProgress * temporaryProgress *
                    (3f - 2f * temporaryProgress);
                var temporaryScale = temporaryProgress < 0.45f
                    ? Mathf.Lerp(
                        1f,
                        temporaryPeakScale,
                        temporaryProgress / 0.45f)
                    : Mathf.Lerp(
                        temporaryPeakScale,
                        1f,
                        (temporaryProgress - 0.45f) / 0.55f);
                temporaryVisual.localScale = new Vector3(
                    temporaryScale,
                    temporaryScale,
                    1f);
                temporaryVisual.localEulerAngles = new Vector3(
                    0f,
                    0f,
                    Mathf.Lerp(
                        temporaryStartAngle,
                        temporaryTargetAngle,
                        temporaryEased));
                yield return null;
            }

            temporaryVisual.localScale = Vector3.one;
            temporaryVisual.localEulerAngles =
                new Vector3(0f, 0f, temporaryTargetAngle);
            tileView.DisplayedAngle = temporaryTargetAngle;
            tileView.SetRotationOutlineVisible(false);
            tileView.SortingCanvas.overrideSorting = false;
            tileView.IsAnimating = false;
            tileView.AnimationCoroutine = null;
            _activeTileAnimations =
                Mathf.Max(0, _activeTileAnimations - 1);
            EvaluateWin();
            RefreshInterface();
        }

        void ApplyHint()
        {
            if (_state.IsSolved)
            {
                return;
            }

            var temporarySolution = _state.FindShortestSolution();
            if (temporarySolution.Count == 0)
            {
                return;
            }

            RotateTile(temporarySolution[0], RotationSource.Hint);
        }

        void ToggleDebugMode()
        {
            if (!Debug.isDebugBuild && !Application.isEditor)
            {
                return;
            }

            _debugMode = !_debugMode;
            _sidePanel.gameObject.SetActive(_debugMode);
            if (_debugMode)
            {
                _sidePanel.SetAsLastSibling();
                _debugToggleButton.SetAsLastSibling();
            }
            RefreshInterface();
        }

        void StartAutoClear()
        {
            if (_autoSolving)
            {
                return;
            }

            _debugMode = true;
            if (_state.IsSolved)
            {
                ResetPuzzle();
            }

            _assistedRun = true;
            StageSession.MarkAssisted();
            _autoClearCoroutine = StartCoroutine(AutoClearPuzzle());
        }

        IEnumerator AutoClearPuzzle()
        {
            _autoSolving = true;
            RefreshInterface();

            var temporarySolution = _state.FindShortestSolution();
            foreach (var temporaryTileIndex in temporarySolution)
            {
                RotateTile(temporaryTileIndex, RotationSource.DebugAuto);
                while (_tileViews[temporaryTileIndex].IsAnimating)
                {
                    yield return null;
                }

                yield return new WaitForSecondsRealtime(0.08f);
            }

            _autoSolving = false;
            _autoClearCoroutine = null;
            RefreshInterface();
        }

        void ResetPuzzle()
        {
            if (_autoClearCoroutine != null)
            {
                StopCoroutine(_autoClearCoroutine);
                _autoClearCoroutine = null;
            }

            if (_travelerCompletionCoroutine != null)
            {
                StopCoroutine(_travelerCompletionCoroutine);
                _travelerCompletionCoroutine = null;
            }

            _state.Reset();
            _boardPresentation.ResetToInitialMarkers(
                GetTravelerEndpointTileIndices());
            _wasSolved = false;
            _assistedRun = false;
            _autoSolving = false;
            _resultShown = false;
            _stageCompletionRecorded = false;
            _travelerCompleting = false;
            _boardRoadRevealRunning = false;
            _boardRoadFullyRevealed = false;
            _playerRotations = 0;
            _hintRotations = 0;
            _autoRotations = 0;
            _activeTileAnimations = 0;
            _gameCursorHoveredTile = null;
            _gameCursorView?.SetHovering(false);
            _boardView?.RestartBoundaryPulse();
            foreach (var temporaryView in _tileViews)
            {
                if (temporaryView == null)
                {
                    continue;
                }

                if (temporaryView.AnimationCoroutine != null)
                {
                    StopCoroutine(temporaryView.AnimationCoroutine);
                    temporaryView.AnimationCoroutine = null;
                }

                temporaryView.IsAnimating = false;
                temporaryView.SetRotationOutlineVisible(false);
                temporaryView.SortingCanvas.overrideSorting = false;
                temporaryView.RotationAngle =
                    _state.IsAlternateOrientation(temporaryView.Index)
                        ? -90f
                        : 0f;
                temporaryView.DisplayedAngle =
                    temporaryView.RotationAngle;
                temporaryView.VisualRect.localScale = Vector3.one;
                temporaryView.VisualRect.localEulerAngles =
                    new Vector3(
                        0f,
                        0f,
                        temporaryView.RotationAngle);
            }

            _travelPoints.Clear();
            _travelSegments.Clear();
            _goalArrivalPointIndices.Clear();
            _goalArrivalDistances.Clear();
            _goalArrivalMarkerIndices.Clear();
            _nextGoalArrivalIndex = 0;
            _traveler.gameObject.SetActive(false);
            _travelerRoadEndpointsOnly = false;
            if (_travelerAnimator != null)
            {
                _travelerAnimator.ResetTrigger(ArriveAnimatorId);
                _travelerAnimator.SetBool(
                    MovingAnimatorId,
                    false);
            }
            _startMarker.gameObject.SetActive(true);
            foreach (var temporaryGoalMarker in _goalMarkerViews)
            {
                temporaryGoalMarker.ResetMarker();
            }
            _travelerBrushTrailView?.Clear();
            _travelerBrushTrailView?.SetVisible(false);
            _travelerDirection = Vector2.up;
            _resultOverlay.gameObject.SetActive(false);
            _skipButton.gameObject.SetActive(false);

            RefreshRoadPresentations(Time.unscaledTime);
            RefreshInterface();
        }

        void ResetByPlayer()
        {
            StageSession.RecordRestart();
            ResetPuzzle();
        }

        void EvaluateWin()
        {
            if (!_state.IsSolved || _wasSolved)
            {
                return;
            }

            _wasSolved = true;
            if (!_assistedRun &&
                (_bestSteps < 0 || _state.Steps < _bestSteps))
            {
                _bestSteps = _state.Steps;
            }

            if (!_assistedRun)
            {
                _scoreRepository.Submit(new ScoreEntry("あなた", _state.Steps));
            }

            if (!_stageCompletionRecorded)
            {
                StageSession.CompleteStage(_version, _state.Steps);
                _stageCompletionRecorded = true;
                if (_version == 4)
                {
                    LiveServices.SubmitCompletedChallenge(
                        StageSession.StageSteps);
                }
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_boardGrid);
            BuildTravelerRoute();
            BeginTraveler();
        }

        void RefreshInterface()
        {
            var temporaryEvaluation = _state.TraceCurrentRoute();
            _missionText.text =
                $"ステージ {_version}　目的地 {_definition.Goals.Count}個";
            _instructionText.text =
                "キャラクターですべての目的地を巡ろう\n" +
                "すべての道をつないでください";
            _subtitleText.text =
                $"ステージ{_version}：目的地 {_definition.Goals.Count}個";
            var temporaryRoute = new HashSet<int>();
            if (_debugMode && !temporaryEvaluation.ReachedGoal)
            {
                foreach (var temporaryStep in temporaryEvaluation.Steps)
                {
                    temporaryRoute.Add(temporaryStep.TileIndex);
                }
            }

            var temporaryTravelerIsFollowingRoute =
                _traveler != null &&
                _traveler.gameObject.activeSelf &&
                !_resultShown;
            for (var temporaryIndex = 0;
                 temporaryIndex < _tileViews.Length;
                temporaryIndex++)
            {
                var temporaryView = _tileViews[temporaryIndex];
                if (!temporaryTravelerIsFollowingRoute)
                {
                    temporaryView.SetDebugRouteHighlighted(
                        temporaryRoute.Contains(temporaryIndex));
                }
                temporaryView.Button.interactable =
                    !temporaryEvaluation.ReachedGoal &&
                    !_autoSolving &&
                    _activeTileAnimations == 0;
            }

            _stepsText.text = _state.Steps.ToString();
            _movesHudText.text =
                JapaneseNumberFormatter.Format(_state.Steps);
            _cumulativeHudText.text = StageSession.IsRankingChallenge
                ? $"今回 {_state.Steps}手　通算 " +
                  $"{StageSession.CumulativeSteps + (_stageCompletionRecorded ? 0 : _state.Steps)}手"
                : $"今回 {_state.Steps}手";
            _minimumText.text = _minimumSteps.ToString();
            _bestText.text = _bestSteps < 0 ? "—" : _bestSteps.ToString();
            _statusText.text = temporaryEvaluation.ReachedGoal
                ? _assistedRun
                    ? "検証クリア：すべての目的地への接続を確認"
                    : _state.Steps == _minimumSteps
                        ? "最短手数でクリア！ キャラクターが出発します"
                        : "道が完成！ キャラクターが目的地を巡ります"
                : _autoSolving
                    ? "自動クリアで経路を検証中…"
                : $"接続済み {temporaryEvaluation.ReachedGoalCount}/" +
                  $"{temporaryEvaluation.TotalGoalCount}　" +
                  "すべての目的地へ道をつないでください";
            _statusText.color = temporaryEvaluation.ReachedGoal
                ? new Color(0.48f, 0.83f, 0.94f)
                : new Color(0.63f, 0.75f, 0.76f);

            SetButtonLabel(
                _debugButton,
                "×");
            SetButtonLabel(
                _autoClearButton,
                _autoSolving ? "自動クリア中…" : "自動クリア");
            _autoClearButton.GetComponent<Button>().interactable = !_autoSolving;
            _debugText.gameObject.SetActive(_debugMode);
            _leaderboardText.gameObject.SetActive(false);
            RefreshDebugPanel(temporaryEvaluation);
            RefreshLeaderboard();
        }

        void RefreshRoadPresentations(float currentTime)
        {
            if (_boardRoadRevealRunning ||
                _boardRoadFullyRevealed ||
                _travelerRoadEndpointsOnly)
            {
                return;
            }

            for (var temporaryTileIndex = 0;
                 temporaryTileIndex < _tileViews.Length;
                 temporaryTileIndex++)
            {
                var temporaryTileView =
                    _tileViews[temporaryTileIndex];
                if (temporaryTileView == null)
                {
                    continue;
                }

                temporaryTileView.SetRoadAlpha(
                    _boardPresentation.GetRoadAlpha(
                        temporaryTileIndex,
                        currentTime));
            }
        }

        void ShowTravelerEndpointRoads()
        {
            for (var temporaryTileIndex = 0;
                 temporaryTileIndex < _tileViews.Length;
                 temporaryTileIndex++)
            {
                _tileViews[temporaryTileIndex]
                    ?.SetRoadAlpha(
                        IsTravelerEndpointTile(
                            temporaryTileIndex)
                            ? 1f
                            : 0f);
            }
        }

        bool IsTravelerEndpointTile(int tileIndex)
        {
            if (tileIndex == _definition.StartIndex)
            {
                return true;
            }

            foreach (var temporaryGoal in _definition.Goals)
            {
                if (tileIndex == temporaryGoal.TileIndex)
                {
                    return true;
                }
            }

            return false;
        }

        BoardRoadReveal CreateBoardRoadReveal()
        {
            var temporaryOriginTileIndices =
                GetTravelerEndpointTileIndices();
            return new BoardRoadReveal(
                _definition.Width,
                _definition.Height,
                temporaryOriginTileIndices,
                CLEAR_REVEAL_WAVE_DELAY,
                CLEAR_REVEAL_TILE_FADE_DURATION);
        }

        List<int> GetTravelerEndpointTileIndices()
        {
            var temporaryEndpointTileIndices =
                new List<int>(_definition.Goals.Count + 1)
                {
                    _definition.StartIndex
                };
            foreach (var temporaryGoal in _definition.Goals)
            {
                temporaryEndpointTileIndices.Add(
                    temporaryGoal.TileIndex);
            }

            return temporaryEndpointTileIndices;
        }

        void RefreshDebugPanel(RouteEvaluation evaluation)
        {
            if (!_debugMode)
            {
                return;
            }

            var temporaryRotatedTiles = new List<string>();
            for (var temporaryIndex = 0;
                 temporaryIndex < _definition.TileCount;
                 temporaryIndex++)
            {
                var temporaryCount = _state.GetRotationCount(temporaryIndex);
                if (temporaryCount > 0)
                {
                    temporaryRotatedTiles.Add(
                        $"{temporaryIndex + 1}番×{temporaryCount}");
                }
            }

            var temporaryRotationDetail = temporaryRotatedTiles.Count == 0
                ? "なし"
                : string.Join("、", temporaryRotatedTiles);
            var temporaryCountMatches =
                _state.Steps == _state.CountRecordedRotations();
            _debugText.text =
                "<b>デバッグ状態</b>\n" +
                $"経路：{GetRouteTerminationLabel(evaluation)} " +
                $"[{evaluation.ReachedGoalCount}/{evaluation.TotalGoalCount}] " +
                $"（{evaluation.Steps.Count}タイル）\n" +
                $"回転：{_state.Steps}［手{_playerRotations}／" +
                $"ヒ{_hintRotations}／自{_autoRotations}］　" +
                $"記録：{(temporaryCountMatches ? "OK" : "不一致")}\n" +
                $"タイル別：{temporaryRotationDetail}";
        }

        static string GetRouteTerminationLabel(RouteEvaluation evaluation)
        {
            if (evaluation.ReachedGoal)
            {
                return "全目的地へ正しく到達";
            }

            if (evaluation.ReachedGoalCount > 0)
            {
                return "一部目的地のみ接続";
            }

            return evaluation.Termination switch
            {
                RouteTermination.ReachedWrongBoundary => "別の外周へ接続",
                RouteTermination.ClosedNetwork => "目的地とは未接続",
                _ => "不明"
            };
        }

        static void SetButtonLabel(RectTransform buttonRect, string label)
        {
            if (buttonRect.TryGetComponent<Button>(out var temporaryButton))
            {
                var temporaryLabel =
                    temporaryButton.GetComponentInChildren<TMP_Text>();
                if (temporaryLabel != null)
                {
                    temporaryLabel.text = label;
                }
            }
        }

        void RefreshLeaderboard()
        {
            var temporaryScores = _scoreRepository.GetTopScores();
            var temporaryLines = new List<string>
            {
                "<b>ローカルランキング</b>"
            };
            var temporaryCount = Mathf.Min(4, temporaryScores.Count);
            for (var temporaryIndex = 0;
                 temporaryIndex < temporaryCount;
                 temporaryIndex++)
            {
                var temporaryScore = temporaryScores[temporaryIndex];
                temporaryLines.Add(
                    $"{temporaryIndex + 1}.  {temporaryScore.PlayerName}　{temporaryScore.Steps}手");
            }

            _leaderboardText.text = string.Join("\n", temporaryLines);
        }

        #endregion

        #region Traveler Animation

        void BuildTravelerRoute()
        {
            _travelPoints.Clear();
            _travelSegments.Clear();
            _goalArrivalPointIndices.Clear();
            _goalArrivalDistances.Clear();
            _goalArrivalMarkerIndices.Clear();
            _travelLength = 0f;

            var temporaryJourneys =
                _state.FindSolvedJourneysByNearestGoal();
            for (var temporaryJourneyIndex = 0;
                 temporaryJourneyIndex < temporaryJourneys.Count;
                 temporaryJourneyIndex++)
            {
                var temporaryJourney =
                    temporaryJourneys[temporaryJourneyIndex];
                var temporarySteps = temporaryJourney.Steps;
                var temporaryStartMarker =
                    temporaryJourneyIndex == 0
                        ? _startMarker
                        : _goalMarkers[
                            temporaryJourneys[
                                temporaryJourneyIndex - 1]
                                .GoalIndex];
                var temporaryJourneyStartPoint =
                    WorldToOverlay(
                        temporaryStartMarker.position);
                if (_travelPoints.Count == 0 ||
                    Vector2.Distance(
                        _travelPoints[
                            _travelPoints.Count - 1],
                        temporaryJourneyStartPoint) > 0.01f)
                {
                    _travelPoints.Add(
                        temporaryJourneyStartPoint);
                }

                for (var temporaryStepIndex = 0;
                     temporaryStepIndex < temporarySteps.Count;
                     temporaryStepIndex++)
                {
                    var temporaryStep = temporarySteps[temporaryStepIndex];
                    var temporaryTileRect =
                        _tileViews[temporaryStep.TileIndex].RectTransform;
                    var temporaryPath = TilePathGraphic.GetRoadCenterline(
                        temporaryStep.EntryCorner,
                        temporaryStep.ExitCorner);

                    for (var temporaryEndpointIndex = 0;
                         temporaryEndpointIndex < 2;
                         temporaryEndpointIndex++)
                    {
                        var temporaryPointIndex =
                            temporaryEndpointIndex == 0
                                ? 0
                                : temporaryPath.Count - 1;
                        var temporaryNormalized =
                            temporaryPath[temporaryPointIndex];
                        var temporaryRect = temporaryTileRect.rect;
                        var temporaryLocal = new Vector2(
                            Mathf.Lerp(
                                temporaryRect.xMin,
                                temporaryRect.xMax,
                                temporaryNormalized.x),
                            Mathf.Lerp(
                                temporaryRect.yMin,
                                temporaryRect.yMax,
                                temporaryNormalized.y));
                        var temporaryPoint = WorldToOverlay(
                            temporaryTileRect.TransformPoint(
                                temporaryLocal));

                        if (_travelPoints.Count == 0 ||
                            Vector2.Distance(
                                _travelPoints[
                                    _travelPoints.Count - 1],
                                temporaryPoint) > 0.01f)
                        {
                            _travelPoints.Add(temporaryPoint);
                        }
                    }

                }

                var temporaryGoalPoint = WorldToOverlay(
                    _goalMarkers[
                        temporaryJourney.GoalIndex].position);
                if (_travelPoints.Count == 0 ||
                    Vector2.Distance(
                        _travelPoints[
                            _travelPoints.Count - 1],
                        temporaryGoalPoint) > 0.01f)
                {
                    _travelPoints.Add(
                        temporaryGoalPoint);
                }

                if (_travelPoints.Count > 0)
                {
                    _goalArrivalPointIndices.Add(
                        _travelPoints.Count - 1);
                    _goalArrivalMarkerIndices.Add(
                        temporaryJourney.GoalIndex);
                }
            }

            var temporaryTrailWidth =
                _traveler.rect.width * Motion.TrailWidthRatio;
            var temporaryVisibleInkHalfWidth =
                temporaryTrailWidth *
                Motion.TrailVisibleInkWidthRatio *
                0.5f;
            var temporaryWallMargin =
                _cellSize * Motion.TrailWallClearanceRatio;
            BuildTravelerWallSegments();
            var temporarySourcePointMappings = new List<int>();
            var temporaryRoundedPoints =
                TravelerRouteGeometry.BuildRoundedPath(
                    _travelPoints,
                    _cellSize *
                    Motion.TravelRouteCornerRadiusRatio,
                    Motion.TravelRouteCornerSegments,
                    temporarySourcePointMappings,
                    temporaryPoint =>
                        TravelerRouteGeometry.HasRequiredClearance(
                            temporaryPoint,
                            _travelerWallSegments,
                            temporaryVisibleInkHalfWidth,
                            temporaryWallMargin,
                            _boardOverlay.rect));
            for (var temporaryIndex = 0;
                 temporaryIndex < _goalArrivalPointIndices.Count;
                 temporaryIndex++)
            {
                _goalArrivalPointIndices[temporaryIndex] =
                    temporarySourcePointMappings[
                        _goalArrivalPointIndices[temporaryIndex]];
            }

            _travelPoints.Clear();
            _travelPoints.AddRange(temporaryRoundedPoints);

            var temporaryDistancesAtPoints =
                new float[_travelPoints.Count];
            for (var temporaryIndex = 0;
                 temporaryIndex < _travelPoints.Count - 1;
                 temporaryIndex++)
            {
                var temporaryLength = Vector2.Distance(
                    _travelPoints[temporaryIndex],
                    _travelPoints[temporaryIndex + 1]);
                _travelSegments.Add(temporaryLength);
                _travelLength += temporaryLength;
                temporaryDistancesAtPoints[temporaryIndex + 1] =
                    _travelLength;
            }

            foreach (var temporaryPointIndex in
                     _goalArrivalPointIndices)
            {
                _goalArrivalDistances.Add(
                    temporaryDistancesAtPoints[temporaryPointIndex]);
            }

            _travelerBrushTrailView?.Configure(
                _travelPoints,
                temporaryTrailWidth,
                _cellSize * Motion.TrailTextureTileRatio);
        }

        void BuildTravelerWallSegments()
        {
            _travelerWallSegments.Clear();
            foreach (var temporaryTileView in _tileViews)
            {
                var temporaryPathGraphic =
                    temporaryTileView?.Path;
                if (temporaryPathGraphic == null)
                {
                    continue;
                }

                if (temporaryPathGraphic.AlternateOrientation)
                {
                    AddTravelerWallSegments(
                        temporaryPathGraphic,
                        Direction.Up,
                        Direction.Right);
                    AddTravelerWallSegments(
                        temporaryPathGraphic,
                        Direction.Down,
                        Direction.Left);
                    continue;
                }

                AddTravelerWallSegments(
                    temporaryPathGraphic,
                    Direction.Up,
                    Direction.Left);
                AddTravelerWallSegments(
                    temporaryPathGraphic,
                    Direction.Right,
                    Direction.Down);
            }
        }

        void AddTravelerWallSegments(
            TilePathGraphic pathGraphic,
            Direction startDirection,
            Direction endDirection)
        {
            var temporaryRect = pathGraphic.rectTransform.rect;
            var temporaryNormalizedPoints =
                TilePathGraphic.GetNormalizedPath(
                    startDirection,
                    endDirection);
            var temporaryPoints =
                new Vector2[temporaryNormalizedPoints.Count];
            for (var temporaryIndex = 0;
                 temporaryIndex < temporaryPoints.Length;
                 temporaryIndex++)
            {
                var temporaryNormalized =
                    temporaryNormalizedPoints[temporaryIndex];
                var temporaryLocal = new Vector2(
                    Mathf.Lerp(
                        temporaryRect.xMin,
                        temporaryRect.xMax,
                        temporaryNormalized.x),
                    Mathf.Lerp(
                        temporaryRect.yMin,
                        temporaryRect.yMax,
                        temporaryNormalized.y));
                temporaryPoints[temporaryIndex] =
                    WorldToOverlay(
                        pathGraphic.rectTransform.TransformPoint(
                            temporaryLocal));
            }

            var temporaryLocalHalfWidth =
                Mathf.Min(
                    temporaryRect.width,
                    temporaryRect.height) *
                pathGraphic.StrokeRatio *
                0.5f;
            var temporaryOverlayOrigin =
                WorldToOverlay(
                    pathGraphic.rectTransform.TransformPoint(
                        Vector2.zero));
            var temporaryOverlayRadiusPoint =
                WorldToOverlay(
                    pathGraphic.rectTransform.TransformPoint(
                        Vector2.right *
                        temporaryLocalHalfWidth));
            var temporaryOverlayHalfWidth =
                Vector2.Distance(
                    temporaryOverlayOrigin,
                    temporaryOverlayRadiusPoint);
            for (var temporaryIndex = 0;
                 temporaryIndex < temporaryPoints.Length - 1;
                 temporaryIndex++)
            {
                _travelerWallSegments.Add(
                    new TravelerRouteGeometry.WallSegment(
                        temporaryPoints[temporaryIndex],
                        temporaryPoints[temporaryIndex + 1],
                        temporaryOverlayHalfWidth));
            }
        }

        void BeginTraveler()
        {
            if (_travelPoints.Count < 2)
            {
                return;
            }

            _travelDistance = 0f;
            _nextGoalArrivalIndex = 0;
            _travelerCompleting = false;
            _travelerRoadEndpointsOnly = true;
            ShowTravelerEndpointRoads();
            _travelerBrushTrailView?.SetVisible(true);
            _travelerBrushTrailView?.SetDistance(0f);
            foreach (var temporaryTile in _tileViews)
            {
                temporaryTile.SetDebugRouteHighlighted(false);
            }
            _startMarker.gameObject.SetActive(false);
            foreach (var temporaryGoalMarker in _goalMarkers)
            {
                temporaryGoalMarker.gameObject.SetActive(true);
            }

            _traveler.gameObject.SetActive(true);
            if (_travelerAnimator != null)
            {
                _travelerAnimator.Rebind();
                _travelerAnimator.Update(0f);
                _travelerAnimator.SetBool(
                    MovingAnimatorId,
                    true);
            }
            _skipButton.gameObject.SetActive(true);
            _traveler.anchoredPosition = _travelPoints[0];
            FaceTraveler(
                _travelPoints[1] - _travelPoints[0]);
            _traveler.SetAsLastSibling();
        }

        void UpdateTraveler()
        {
            if (!_traveler.gameObject.activeSelf ||
                _travelPoints.Count < 2 ||
                _travelDistance >= _travelLength)
            {
                return;
            }

            _travelDistance = Mathf.Min(
                _travelLength,
                _travelDistance +
                Time.deltaTime *
                _cellSize *
                TravelSpeedInTiles *
                GetTravelSpeedMultiplier());
            _travelerBrushTrailView?.SetDistance(
                _travelDistance);
            UpdateReachedGoals();

            if (_travelDistance >= _travelLength)
            {
                _traveler.anchoredPosition =
                    _travelPoints[_travelPoints.Count - 1];
                _travelerBrushTrailView?.Complete();
                if (_travelerAnimator != null)
                {
                    _travelerAnimator.SetBool(
                        MovingAnimatorId,
                        false);
                }
                _statusText.text = _assistedRun
                    ? "検証完了：すべての目的地を巡りました"
                    : "クリア！ すべての目的地を巡りました";
                BeginTravelerCompletion();
                return;
            }

            var temporaryRemaining = _travelDistance;
            for (var temporaryIndex = 0;
                 temporaryIndex < _travelSegments.Count;
                 temporaryIndex++)
            {
                var temporarySegmentLength = _travelSegments[temporaryIndex];
                if (temporaryRemaining <= temporarySegmentLength)
                {
                    var temporaryProgress = temporarySegmentLength <= 0f
                        ? 1f
                        : temporaryRemaining / temporarySegmentLength;
                    _traveler.anchoredPosition = Vector2.Lerp(
                        _travelPoints[temporaryIndex],
                        _travelPoints[temporaryIndex + 1],
                        temporaryProgress);
                    FaceTraveler(
                        _travelPoints[temporaryIndex + 1] -
                        _travelPoints[temporaryIndex]);
                    return;
                }

                temporaryRemaining -= temporarySegmentLength;
            }
        }

        void SkipTraveler()
        {
            if (_travelPoints.Count < 2)
            {
                return;
            }

            _travelDistance = _travelLength;
            _travelerBrushTrailView?.Complete();
            UpdateReachedGoals();
            _traveler.anchoredPosition =
                _travelPoints[_travelPoints.Count - 1];
            if (_travelerAnimator != null)
            {
                _travelerAnimator.SetBool(
                    MovingAnimatorId,
                    false);
            }
            BeginTravelerCompletion();
        }

        float GetTravelSpeedMultiplier()
        {
            var temporaryStartDistance =
                _nextGoalArrivalIndex > 0 &&
                _nextGoalArrivalIndex - 1 <
                _goalArrivalDistances.Count
                    ? _goalArrivalDistances[
                        _nextGoalArrivalIndex - 1]
                    : 0f;
            var temporaryEndDistance =
                _nextGoalArrivalIndex <
                _goalArrivalDistances.Count
                    ? _goalArrivalDistances[
                        _nextGoalArrivalIndex]
                    : _travelLength;
            var temporaryProgress = Mathf.InverseLerp(
                temporaryStartDistance,
                temporaryEndDistance,
                _travelDistance);
            var temporaryAcceleration = Mathf.SmoothStep(
                0.38f,
                1.28f,
                Mathf.Clamp01(temporaryProgress / 0.22f));
            var temporaryDeceleration = Mathf.SmoothStep(
                0.38f,
                1.28f,
                Mathf.Clamp01(
                    (1f - temporaryProgress) / 0.20f));
            return Mathf.Min(
                temporaryAcceleration,
                temporaryDeceleration);
        }

        IEnumerator CompleteTravelerAfterArrival()
        {
            yield return new WaitForSecondsRealtime(
                CLEAR_REVEAL_ARRIVAL_HOLD_DURATION);
            _boardRoadRevealRunning = true;
            _travelerRoadEndpointsOnly = false;
            _boardView?.StopBoundaryPulseForClear();

            var temporaryElapsed = 0f;
            while (temporaryElapsed <
                   _boardRoadReveal.Duration)
            {
                temporaryElapsed +=
                    Motion.AnimationDeltaTime;
                for (var temporaryTileIndex = 0;
                     temporaryTileIndex <
                     _tileViews.Length;
                     temporaryTileIndex++)
                {
                    _tileViews[temporaryTileIndex]
                        ?.SetRoadAlpha(
                            IsTravelerEndpointTile(
                                temporaryTileIndex)
                                ? 1f
                                : _boardRoadReveal.GetRoadAlpha(
                                    temporaryTileIndex,
                                    temporaryElapsed));
                }

                yield return null;
            }

            foreach (var temporaryTileView in _tileViews)
            {
                temporaryTileView?.SetRoadAlpha(1f);
            }

            _boardRoadRevealRunning = false;
            _boardRoadFullyRevealed = true;
            yield return new WaitForSecondsRealtime(
                CLEAR_BOARD_HOLD_DURATION);
            _travelerCompletionCoroutine = null;
            ShowResult();
        }

        void BeginTravelerCompletion()
        {
            if (_travelerCompleting)
            {
                return;
            }

            _travelerCompleting = true;
            _travelerCompletionCoroutine =
                StartCoroutine(
                    CompleteTravelerAfterArrival());
        }

        void ShowResult()
        {
            if (_resultShown)
            {
                return;
            }

            _resultShown = true;
            _skipButton.gameObject.SetActive(false);
            SoundFeedback.PlayStageClear();
            _resultTitleText.text = _version == 4
                ? "全ステージ完了"
                : "ステージ完了";
            var temporaryTotal = StageSession.IsRankingChallenge ||
                                 StageSession.CanSubmitRanking
                ? StageSession.CumulativeSteps
                : _state.Steps;
            _resultDetailText.text = _version == 4
                ? $"今回 {_state.Steps}手\n合計 {temporaryTotal}手"
                : $"今回の手数　{_state.Steps}手\n最短　{_minimumSteps}手";
            _resultLeaderboardText.text = string.Empty;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (_version == 4 && StageSession.CanSubmitRanking)
            {
                _resultLeaderboardText.text =
                    "ランキングはunityroomのゲームページで確認できます";
            }
#endif
            SetButtonLabel(
                _resultNextButton,
                _version < 4 ? "次のステージへ" : "ステージ選択へ");
            _resultOverlay.gameObject.SetActive(true);
            _resultOverlay.SetAsLastSibling();
        }

        void ContinueAfterResult()
        {
            if (_sceneTransition == null ||
                _sceneTransition.IsTransitioning)
            {
                return;
            }

            if (_version < 4)
            {
                _sceneTransition.FadeToScene(
                    "Game",
                    () =>
                    {
                        if (StageSession.IsRankingChallenge)
                        {
                            StageSession.ContinueToNextStage();
                        }
                        else
                        {
                            StageSession.SelectStage(_version + 1);
                        }
                    });
                return;
            }

            OpenStageSelect();
        }

        void UpdateReachedGoals()
        {
            while (_nextGoalArrivalIndex <
                   _goalArrivalDistances.Count &&
                   _travelDistance >=
                   _goalArrivalDistances[_nextGoalArrivalIndex] - 0.01f)
            {
                if (_nextGoalArrivalIndex <
                    _goalArrivalMarkerIndices.Count)
                {
                    var temporaryMarkerIndex =
                        _goalArrivalMarkerIndices[
                            _nextGoalArrivalIndex];
                    if (temporaryMarkerIndex >= 0 &&
                        temporaryMarkerIndex <
                        _goalMarkerViews.Count)
                    {
                        _goalMarkerViews[temporaryMarkerIndex]
                            .PlayArrival(Motion);
                    }
                }

                if (_travelerAnimator != null)
                {
                    _travelerAnimator.SetTrigger(
                        ArriveAnimatorId);
                }
                SoundFeedback.PlayGoalArrival();
                _nextGoalArrivalIndex++;
            }
        }

        void FaceTraveler(Vector2 direction)
        {
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            _travelerView?.FaceDirection(direction);
            _travelerDirection = direction.normalized;
        }

        Vector2 WorldToOverlay(Vector3 worldPosition)
        {
            return _boardOverlay.InverseTransformPoint(worldPosition);
        }

        #endregion

        #region UI Helpers

        static T CreateUiObject<T>(string objectName, Transform parent)
            where T : Component
        {
            var temporaryObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(T));
            temporaryObject.transform.SetParent(parent, false);
            return temporaryObject.GetComponent<T>();
        }

        static RectTransform CreateRect(string objectName, Transform parent)
        {
            var temporaryObject = new GameObject(objectName, typeof(RectTransform));
            temporaryObject.transform.SetParent(parent, false);
            return temporaryObject.GetComponent<RectTransform>();
        }

        static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        static void SetTopLeft(
            RectTransform rectTransform,
            float x,
            float y,
            float width,
            float height)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(x, -y);
            rectTransform.sizeDelta = new Vector2(width, height);
        }

        #endregion

    }
}
