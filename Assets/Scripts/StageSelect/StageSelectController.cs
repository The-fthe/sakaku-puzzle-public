using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace SakakuPuzzle
{
    public sealed class StageSelectController : MonoBehaviour
    {
        static readonly Color PaperColor = new(0.95f, 0.93f, 0.86f);

        [SerializeField]
        Camera _uiCamera;
        [SerializeField]
        PuzzleUiCatalog _uiCatalog;

        PuzzleUiTheme Theme => _uiCatalog.Theme;

        Canvas _canvas;
        StageSelectView _stageSelectView;
        HowToPanelView _howToPanelView;
        ScreenFadeTransition _sceneTransition;
        bool _transitioning;

        #region Unity Lifecycle

        void Awake()
        {
            if (_uiCatalog == null)
            {
                Debug.LogError(
                    "[StageSelectController] UI Catalogが未設定です。",
                    this);
                enabled = false;
                return;
            }
            if (!_uiCatalog.IsValid(out var temporaryError))
            {
                Debug.LogError(
                    $"[StageSelectController] {temporaryError}",
                    this);
                enabled = false;
                return;
            }
            SoundFeedback.Configure(Theme);
            BgmPlayer.Play();
            LiveServices.Configure(_uiCatalog.LiveServiceAssets);
            EnsureCamera();
            BuildInterface();
        }

        void OnDestroy()
        {
            Cursor.visible = true;
            if (_stageSelectView != null)
            {
                _stageSelectView.StageSelected -= OpenStage;
                _stageSelectView.HowToRequested -= ShowHowTo;
            }
            if (_howToPanelView != null)
            {
                _howToPanelView.StartRequested -= HideHowTo;
            }
        }

        #endregion

        #region Construction

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
        }

        void BuildInterface()
        {
            EnsureEventSystem();

            var temporaryCanvasObject = new GameObject(
                "ステージ選択 Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            temporaryCanvasObject.transform.SetParent(transform, false);
            _canvas = temporaryCanvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = _uiCamera;
            _canvas.planeDistance = 1f;

            var temporaryScaler =
                temporaryCanvasObject.GetComponent<CanvasScaler>();
            temporaryScaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            temporaryScaler.referenceResolution =
                new Vector2(1080f, 1920f);
            temporaryScaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            temporaryScaler.matchWidthOrHeight = 0.5f;

            _stageSelectView = Instantiate(
                _uiCatalog.StageSelectViewPrefab,
                temporaryCanvasObject.transform);
            _stageSelectView.Configure(Theme);
            Stretch(_stageSelectView.RectTransform);
            _stageSelectView.StageSelected += OpenStage;
            _stageSelectView.HowToRequested += ShowHowTo;
            BuildHowToOverlay();
            _sceneTransition = Instantiate(
                _uiCatalog.ScreenTransitionPrefab,
                _canvas.transform);
            _sceneTransition.Configure(
                _canvas.transform,
                _uiCatalog.Motion);
        }

        void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var temporaryEventSystem =
                new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            var temporaryInputModule =
                temporaryEventSystem.AddComponent<InputSystemUIInputModule>();
            temporaryInputModule.AssignDefaultActions();
#else
            temporaryEventSystem.AddComponent<StandaloneInputModule>();
#endif
            temporaryEventSystem.transform.SetParent(transform, false);
        }

        void BuildHowToOverlay()
        {
            _howToPanelView = Instantiate(
                _uiCatalog.HowToPanelPrefab,
                _canvas.transform);
            _howToPanelView.Configure(Theme);
            _howToPanelView.StartRequested += HideHowTo;
            Stretch(_howToPanelView.RectTransform);
            _howToPanelView.Hide();
        }

        #endregion

        #region Navigation

        void OpenStage(int stageNumber)
        {
            if (_transitioning ||
                _sceneTransition == null)
            {
                return;
            }

            _transitioning = true;
            _stageSelectView.SetInteractable(false);
            _stageSelectView.PlayStageConfirmation(stageNumber);

            SoundFeedback.PlayRotate();
            _sceneTransition.FadeToScene(
                "Game",
                () => StageSession.SelectStage(stageNumber),
                0.16f);
        }

        void ShowHowTo()
        {
            var temporaryCanvasRect =
                (RectTransform)_canvas.transform;
            Canvas.ForceUpdateCanvases();
            _howToPanelView.FitToViewport(
                temporaryCanvasRect.rect.size);
            _howToPanelView.Show();
        }

        void HideHowTo()
        {
            _howToPanelView.Hide();
        }

        #endregion

        #region UI Helpers

        static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        #endregion
    }
}
