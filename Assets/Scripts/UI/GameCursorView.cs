using UnityEngine;
using UnityEngine.UI;

namespace SakakuPuzzle
{
    [DisallowMultipleComponent]
    public sealed class GameCursorView : MonoBehaviour
    {
        public const float SIZE = 46f;
        public const float HOVER_ALPHA = 0.62f;
        public const int SORTING_ORDER = 100;
        public static readonly Vector2 SystemHotspot =
            new(6f, 2f);
        public static readonly Vector2 Hotspot =
            new(0.174f, 0.936f);
        static readonly Vector2 GameReferenceResolution =
            new(1280f, 720f);
        const float GAME_MATCH_WIDTH_OR_HEIGHT = 0.5f;

        [SerializeField] Image _image;

        Color _opaqueColor;
        Color _hoverColor;
        Texture2D _hoverTexture;
        float _hoverAlpha;
        bool _hovering;

        public RectTransform RectTransform =>
            (RectTransform)transform;

        public static bool ShouldShowForDevice(
            bool isMobilePlatform,
            DeviceType deviceType)
        {
            return !isMobilePlatform &&
                   deviceType != DeviceType.Handheld;
        }

        #region Unity Lifecycle

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                RefreshSystemCursor();
            }
        }

        void OnDestroy()
        {
            Cursor.visible = true;
            Cursor.SetCursor(
                null,
                Vector2.zero,
                CursorMode.Auto);
            if (_hoverTexture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_hoverTexture);
            }
            else
            {
                DestroyImmediate(_hoverTexture);
            }
        }

        #endregion

        #region Configuration

        public void Configure(
            Sprite sprite,
            Color opaqueColor,
            float hoverAlpha)
        {
            _opaqueColor = opaqueColor;
            _hoverColor = new Color(
                opaqueColor.r,
                opaqueColor.g,
                opaqueColor.b,
                hoverAlpha);
            _hoverAlpha = hoverAlpha;

            if (_image == null)
            {
                return;
            }

            _image.sprite = sprite;
            _image.color = _opaqueColor;
            _image.preserveAspect = true;
            _image.raycastTarget = false;
        }

        public void SetHovering(bool hovering)
        {
            _hovering = hovering;
            if (_image != null)
            {
                _image.color = hovering
                    ? _hoverColor
                    : _opaqueColor;
            }

            RefreshSystemCursor();
        }

        public void ApplyCanvasPresentation(Canvas canvas)
        {
            var temporaryRectTransform = RectTransform;
            temporaryRectTransform.anchorMin = Vector2.one * 0.5f;
            temporaryRectTransform.anchorMax = Vector2.one * 0.5f;
            temporaryRectTransform.pivot = Hotspot;
            temporaryRectTransform.sizeDelta = Vector2.one *
                CalculateSizeForCanvas(
                    canvas,
                    new Vector2(Screen.width, Screen.height));

            if (TryGetComponent<Canvas>(out var temporaryCursorCanvas))
            {
                temporaryCursorCanvas.overrideSorting = true;
                temporaryCursorCanvas.sortingOrder = SORTING_ORDER;
            }

            temporaryRectTransform.SetAsLastSibling();
            var temporaryShouldShow = ShouldShowForDevice(
                Application.isMobilePlatform,
                SystemInfo.deviceType);
            gameObject.SetActive(temporaryShouldShow);
            if (_image != null)
            {
                _image.enabled = false;
            }
            Cursor.visible = temporaryShouldShow;
            Cursor.SetCursor(
                null,
                Vector2.zero,
                CursorMode.Auto);
        }

        public static float CalculateSizeForCanvas(
            Canvas canvas,
            Vector2 screenSize)
        {
            if (canvas == null ||
                !canvas.TryGetComponent<CanvasScaler>(
                    out var temporaryScaler) ||
                temporaryScaler.uiScaleMode !=
                    CanvasScaler.ScaleMode.ScaleWithScreenSize ||
                temporaryScaler.screenMatchMode !=
                    CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
            {
                return SIZE;
            }

            var temporaryGameScale = CalculateScaleFactor(
                screenSize,
                GameReferenceResolution,
                GAME_MATCH_WIDTH_OR_HEIGHT);
            var temporaryCanvasScale = CalculateScaleFactor(
                screenSize,
                temporaryScaler.referenceResolution,
                temporaryScaler.matchWidthOrHeight);
            return SIZE * temporaryGameScale /
                   Mathf.Max(temporaryCanvasScale, Mathf.Epsilon);
        }

        static float CalculateScaleFactor(
            Vector2 screenSize,
            Vector2 referenceResolution,
            float matchWidthOrHeight)
        {
            var temporaryLogWidth = Mathf.Log(
                screenSize.x / referenceResolution.x,
                2f);
            var temporaryLogHeight = Mathf.Log(
                screenSize.y / referenceResolution.y,
                2f);
            return Mathf.Pow(
                2f,
                Mathf.Lerp(
                    temporaryLogWidth,
                    temporaryLogHeight,
                    matchWidthOrHeight));
        }

        public void SetScreenPosition(
            Canvas canvas,
            Vector2 screenPosition)
        {
            if (canvas == null ||
                !gameObject.activeSelf ||
                _image == null ||
                !_image.enabled)
            {
                return;
            }

            var temporaryCanvasRect =
                (RectTransform)canvas.transform;
            var temporaryCamera =
                canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    temporaryCanvasRect,
                    screenPosition,
                    temporaryCamera,
                    out var temporaryLocalPosition))
            {
                RectTransform.anchoredPosition = temporaryLocalPosition;
            }
        }

        void RefreshSystemCursor()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            Cursor.visible = true;
            Cursor.SetCursor(
                _hovering ? GetHoverTexture() : null,
                _hovering ? SystemHotspot : Vector2.zero,
                CursorMode.Auto);
        }

        Texture2D GetHoverTexture()
        {
            if (_hoverTexture != null ||
                _image == null ||
                _image.sprite == null)
            {
                return _hoverTexture;
            }

            var temporarySource = _image.sprite.texture;
            var temporaryPixels = temporarySource.GetPixels32();
            for (var temporaryIndex = 0;
                 temporaryIndex < temporaryPixels.Length;
                 temporaryIndex++)
            {
                var temporaryPixel = temporaryPixels[temporaryIndex];
                temporaryPixel.a = (byte)Mathf.RoundToInt(
                    temporaryPixel.a * _hoverAlpha);
                temporaryPixels[temporaryIndex] = temporaryPixel;
            }

            _hoverTexture = new Texture2D(
                temporarySource.width,
                temporarySource.height,
                TextureFormat.RGBA32,
                false)
            {
                name = "game-cursor-hover-runtime",
                filterMode = temporarySource.filterMode,
                wrapMode = TextureWrapMode.Clamp
            };
            _hoverTexture.SetPixels32(temporaryPixels);
            _hoverTexture.Apply(false, false);
            return _hoverTexture;
        }

        #endregion
    }
}
