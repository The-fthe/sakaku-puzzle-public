using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SakakuPuzzle
{
    [DisallowMultipleComponent]
    public sealed class MapTileView :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        const float AFTERIMAGE_ALPHA = 0.22f;

        static readonly Color DebugRouteColor = new(0.78f, 0.93f, 0.87f);

        [SerializeField] RectTransform _visualRect;
        [SerializeField] Image _visualImage;
        [SerializeField] Outline _rotationOutline;
        [SerializeField] Button _button;
        [SerializeField] TilePathGraphic _path;
        [SerializeField] Canvas _sortingCanvas;

        Color _tileColor = Color.white;

        public event Action<MapTileView> Clicked;
        public event Action<MapTileView> PointerEntered;
        public event Action<MapTileView> PointerExited;

        public int Index { get; private set; }
        public RectTransform RectTransform =>
            (RectTransform)transform;
        public RectTransform VisualRect => _visualRect;
        public Button Button => _button;
        public TilePathGraphic Path => _path;
        public Canvas SortingCanvas => _sortingCanvas;
        public float RotationAngle { get; set; }
        public float DisplayedAngle { get; set; }
        public bool IsAnimating { get; set; }
        public Coroutine AnimationCoroutine { get; set; }
        #region Unity Lifecycle

        void Awake()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(NotifyClicked);
            }
        }

        void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(NotifyClicked);
            }
        }

        #endregion

        #region Configuration

        public void Configure(
            int index,
            PuzzleUiTheme theme)
        {
            Index = index;
            name = $"マップタイル {index + 1:00}";
            if (_visualImage != null)
            {
                _visualImage.type = Image.Type.Simple;
                _tileColor = _visualImage.color;
            }

            if (_rotationOutline != null)
            {
                _rotationOutline.effectDistance =
                    new Vector2(4f, -4f);
                _rotationOutline.useGraphicAlpha = true;
                _rotationOutline.enabled = false;
            }

            if (_path != null)
            {
                _path.AlternateOrientation = false;
            }

            if (_button != null)
            {
                var temporaryColors = _button.colors;
                temporaryColors.normalColor = Color.white;
                temporaryColors.highlightedColor =
                    new Color(1f, 0.96f, 0.78f);
                temporaryColors.pressedColor =
                    new Color(0.98f, 0.88f, 0.62f);
                temporaryColors.selectedColor =
                    temporaryColors.highlightedColor;
                temporaryColors.disabledColor =
                    new Color(0.72f, 0.72f, 0.68f, 0.72f);
                _button.colors = temporaryColors;
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (_button != null)
            {
                _button.interactable = interactable;
            }
        }

        public void SetRoadPresentation(
            RoadPresentation presentation)
        {
            var temporaryAlpha = presentation switch
            {
                RoadPresentation.Peeking or
                RoadPresentation.Inked => 1f,
                RoadPresentation.Afterimage =>
                    AFTERIMAGE_ALPHA,
                _ => 0f
            };
            SetRoadAlpha(temporaryAlpha);
        }

        public void SetRoadAlpha(float alpha)
        {
            if (_path == null)
            {
                return;
            }

            _path.canvasRenderer.SetAlpha(
                Mathf.Clamp01(alpha));
        }

        public void SetDebugRouteHighlighted(bool highlighted)
        {
            if (_visualImage != null)
            {
                _visualImage.color = highlighted
                    ? DebugRouteColor
                    : _tileColor;
            }
        }

        public void SetRotationOutlineVisible(bool visible)
        {
            if (_rotationOutline != null)
            {
                _rotationOutline.enabled = visible;
            }
        }

        #endregion

        #region Pointer Input

        public void OnPointerEnter(PointerEventData eventData)
        {
            PointerEntered?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PointerExited?.Invoke(this);
        }

        #endregion

        void NotifyClicked()
        {
            Clicked?.Invoke(this);
        }
    }
}
