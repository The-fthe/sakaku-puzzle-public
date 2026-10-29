using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SakakuPuzzle
{
    [DisallowMultipleComponent]
    public sealed class ResultPanelView : MonoBehaviour
    {
        [SerializeField] Image _shade;
        [SerializeField] Image _card;
        [SerializeField] TMP_Text _title;
        [SerializeField] TMP_Text _detail;
        [SerializeField] TMP_Text _leaderboard;
        [SerializeField] InkButtonView _nextButton;

        public event Action NextRequested;

        public RectTransform RectTransform =>
            (RectTransform)transform;
        public RectTransform CardRect => _card.rectTransform;
        public TMP_Text Title => _title;
        public TMP_Text Detail => _detail;
        public TMP_Text Leaderboard => _leaderboard;
        public RectTransform NextButton => _nextButton.RectTransform;

        #region Unity Lifecycle

        void Awake()
        {
            _nextButton.Clicked += OnNext;
        }

        void OnDestroy()
        {
            _nextButton.Clicked -= OnNext;
        }

        #endregion

        #region Presentation

        public void Configure(PuzzleUiTheme theme)
        {
            _shade.color = Color.clear;
            _shade.raycastTarget = false;
            _card.sprite = theme.SquareSprite;
            _card.type = Image.Type.Simple;
            _title.font = theme.DisplayFont;
            _detail.font = theme.DisplayFont;
            _leaderboard.font = theme.DisplayFont;
            _nextButton.Configure("次のステージへ", theme);
            Hide();
        }

        public void Show(
            string title,
            string detail,
            string leaderboard,
            string nextLabel)
        {
            _title.text = title;
            _detail.text = detail;
            _leaderboard.text = leaderboard;
            _nextButton.SetLabel(nextLabel);
            gameObject.SetActive(true);
            RectTransform.SetAsLastSibling();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void FitToViewport(Vector2 availableSize)
        {
            var temporaryScale = Mathf.Min(
                1f,
                availableSize.x / 840f,
                availableSize.y / 800f);
            _card.rectTransform.localScale =
                Vector3.one * temporaryScale;
        }

        public void LayoutAroundBoard(
            Vector2 availableSize,
            Vector2 boardTopLeft,
            float boardSize,
            bool portrait)
        {
            const float MARGIN = 16f;
            const float PORTRAIT_TRAILING_SPACE = 96f;
            ApplyCardLayout(portrait);
            var temporaryRegionX = portrait
                ? MARGIN
                : boardTopLeft.x + boardSize + MARGIN;
            var temporaryRegionY = portrait
                ? boardTopLeft.y + boardSize + MARGIN
                : boardTopLeft.y;
            var temporaryRegionWidth = portrait
                ? availableSize.x -
                  MARGIN * 2f -
                  PORTRAIT_TRAILING_SPACE
                : availableSize.x - temporaryRegionX - MARGIN;
            var temporaryRegionHeight = portrait
                ? availableSize.y - temporaryRegionY - MARGIN
                : boardSize;
            temporaryRegionWidth =
                Mathf.Max(1f, temporaryRegionWidth);
            temporaryRegionHeight =
                Mathf.Max(1f, temporaryRegionHeight);

            var temporaryCardRect = _card.rectTransform;
            var temporaryCardSize = temporaryCardRect.sizeDelta;
            var temporaryScale = Mathf.Min(
                1f,
                temporaryRegionWidth /
                Mathf.Max(1f, temporaryCardSize.x),
                temporaryRegionHeight /
                Mathf.Max(1f, temporaryCardSize.y));
            temporaryCardRect.anchorMin = new Vector2(0f, 1f);
            temporaryCardRect.anchorMax = new Vector2(0f, 1f);
            temporaryCardRect.pivot = new Vector2(0.5f, 0.5f);
            temporaryCardRect.anchoredPosition = new Vector2(
                temporaryRegionX + temporaryRegionWidth * 0.5f,
                -(temporaryRegionY +
                  temporaryRegionHeight * 0.5f));
            temporaryCardRect.localScale =
                Vector3.one * temporaryScale;
        }

        void ApplyCardLayout(bool portrait)
        {
            if (portrait)
            {
                _card.rectTransform.sizeDelta =
                    new Vector2(1040f, 360f);
                SetTopLeft(
                    _title.rectTransform,
                    40f,
                    22f,
                    620f,
                    96f);
                SetTopLeft(
                    _detail.rectTransform,
                    56f,
                    120f,
                    570f,
                    110f);
                SetTopLeft(
                    _leaderboard.rectTransform,
                    56f,
                    230f,
                    570f,
                    90f);
                SetTopLeft(
                    _nextButton.RectTransform,
                    690f,
                    120f,
                    310f,
                    120f);
                return;
            }

            _card.rectTransform.sizeDelta =
                new Vector2(760f, 720f);
            SetTopLeft(
                _title.rectTransform,
                40f,
                44f,
                680f,
                126f);
            SetTopLeft(
                _detail.rectTransform,
                60f,
                184f,
                640f,
                132f);
            SetTopLeft(
                _leaderboard.rectTransform,
                80f,
                338f,
                600f,
                174f);
            SetTopLeft(
                _nextButton.RectTransform,
                170f,
                594f,
                420f,
                82f);
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
            rectTransform.anchoredPosition =
                new Vector2(x, -y);
            rectTransform.sizeDelta =
                new Vector2(width, height);
        }

        #endregion

        void OnNext() => NextRequested?.Invoke();
    }
}
