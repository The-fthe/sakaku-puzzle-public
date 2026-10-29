using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SakakuPuzzle
{
    [DisallowMultipleComponent]
    public sealed class GameHudView : MonoBehaviour
    {
        const float MIN_MOVES_FONT_SIZE = 36f;
        const float MAX_MOVES_FONT_SIZE = 108f;
        const float LANDSCAPE_MAX_MOVES_FONT_SIZE = 88f;
        const float MOVES_DISPLAY_WIDTH = 380f;
        const float PORTRAIT_MOVES_DISPLAY_HEIGHT = 168f;
        const float LANDSCAPE_MOVES_DISPLAY_HEIGHT = 126f;

        [SerializeField] TMP_Text _title;
        [SerializeField] TMP_Text _subtitle;
        [SerializeField] RectTransform _movesContainer;
        [SerializeField] TMP_Text _moves;
        [SerializeField] TMP_Text _cumulativeMoves;
        [SerializeField] Image _divider;
        [SerializeField] InkButtonView _stageSelectButton;
        [SerializeField] InkButtonView _resetButton;
        [SerializeField] InkButtonView _debugButton;

        public event Action StageSelectRequested;
        public event Action ResetRequested;
        public event Action DebugRequested;

        public RectTransform RectTransform =>
            (RectTransform)transform;
        public TMP_Text Title => _title;
        public TMP_Text Subtitle => _subtitle;
        public RectTransform MovesContainer => _movesContainer;
        public TMP_Text Moves => _moves;
        public TMP_Text CumulativeMoves => _cumulativeMoves;
        public RectTransform Divider =>
            _divider != null ? _divider.rectTransform : null;
        public RectTransform StageSelectButton =>
            _stageSelectButton.RectTransform;
        public RectTransform ResetButton => _resetButton.RectTransform;
        public RectTransform DebugButton => _debugButton.RectTransform;

        #region Unity Lifecycle

        void Awake()
        {
            _stageSelectButton.Clicked += OnStageSelect;
            _resetButton.Clicked += OnReset;
            _debugButton.Clicked += OnDebug;
        }

        void OnDestroy()
        {
            _stageSelectButton.Clicked -= OnStageSelect;
            _resetButton.Clicked -= OnReset;
            _debugButton.Clicked -= OnDebug;
        }

        #endregion

        #region Presentation

        public void Configure(PuzzleUiTheme theme)
        {
            ConfigureText(_title, theme.DisplayFont);
            ConfigureText(_subtitle, theme.DisplayFont);
            _title.text = "白路（しろ）";
            _subtitle.text =
                "白紙の道を覗き、記憶でつなげ。";
            ConfigureMovesText(_moves, theme.DisplayFont);
            ConfigureText(_cumulativeMoves, theme.DisplayFont);
            _stageSelectButton.Configure("← ステージ選択", theme);
            _resetButton.Configure("再 やり直す", theme);
            _debugButton.Configure(">_", theme);
        }

        public void SetMovesDisplayCompact(bool compact)
        {
            if (compact)
            {
                SetTopLeft(
                    _movesContainer,
                    0f,
                    0f,
                    MOVES_DISPLAY_WIDTH,
                    LANDSCAPE_MOVES_DISPLAY_HEIGHT);
                SetTopLeft(
                    _moves.rectTransform,
                    70f,
                    0f,
                    240f,
                    98f);
                SetTopLeft(
                    _cumulativeMoves.rectTransform,
                    0f,
                    98f,
                    MOVES_DISPLAY_WIDTH,
                    28f);
                _moves.fontSizeMax =
                    LANDSCAPE_MAX_MOVES_FONT_SIZE;
                return;
            }

            SetTopLeft(
                _movesContainer,
                0f,
                0f,
                MOVES_DISPLAY_WIDTH,
                PORTRAIT_MOVES_DISPLAY_HEIGHT);
            SetTopLeft(
                _moves.rectTransform,
                70f,
                0f,
                240f,
                158f);
            SetTopLeft(
                _cumulativeMoves.rectTransform,
                0f,
                138f,
                MOVES_DISPLAY_WIDTH,
                30f);
            _moves.fontSizeMax = MAX_MOVES_FONT_SIZE;
        }

        public void SetDebugAvailable(bool available)
        {
            _debugButton.gameObject.SetActive(available);
        }

        static void ConfigureText(
            TMP_Text text,
            TMP_FontAsset font)
        {
            if (text == null)
            {
                return;
            }

            text.font = font;
        }

        static void ConfigureMovesText(
            TMP_Text text,
            TMP_FontAsset font)
        {
            ConfigureText(text, font);
            if (text == null)
            {
                return;
            }

            text.enableAutoSizing = true;
            text.fontSizeMin = MIN_MOVES_FONT_SIZE;
            text.fontSizeMax = MAX_MOVES_FONT_SIZE;
            text.textWrappingMode = TextWrappingModes.NoWrap;
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

        void OnStageSelect() => StageSelectRequested?.Invoke();
        void OnReset() => ResetRequested?.Invoke();
        void OnDebug() => DebugRequested?.Invoke();
    }
}
