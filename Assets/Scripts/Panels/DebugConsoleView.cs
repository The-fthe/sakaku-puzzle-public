using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SakakuPuzzle
{
    [DisallowMultipleComponent]
    public sealed class DebugConsoleView : MonoBehaviour
    {
        [SerializeField] Image _background;
        [SerializeField] TMP_Text _mission;
        [SerializeField] TMP_Text _instruction;
        [SerializeField] TMP_Text _status;
        [SerializeField] TMP_Text _steps;
        [SerializeField] TMP_Text _minimum;
        [SerializeField] TMP_Text _best;
        [SerializeField] TMP_Text _details;
        [SerializeField] TMP_Text _leaderboard;
        [SerializeField] InkButtonView _hintButton;
        [SerializeField] InkButtonView _regenerateButton;
        [SerializeField] InkButtonView _closeButton;
        [SerializeField] InkButtonView _autoClearButton;
        [SerializeField] InkButtonView _creditButton;
        [SerializeField] RectTransform[] _metricCards;
        bool _isConfigured;

        public event Action HintRequested;
        public event Action RegenerateRequested;
        public event Action CloseRequested;
        public event Action AutoClearRequested;
        public event Action CreditRequested;

        public RectTransform RectTransform =>
            (RectTransform)transform;
        public Image Background => _background;
        public TMP_Text Mission => _mission;
        public TMP_Text Instruction => _instruction;
        public TMP_Text Status => _status;
        public TMP_Text Steps => _steps;
        public TMP_Text Minimum => _minimum;
        public TMP_Text Best => _best;
        public TMP_Text Details => _details;
        public TMP_Text Leaderboard => _leaderboard;
        public RectTransform[] MetricCards => _metricCards;
        public RectTransform HintButton => _hintButton.RectTransform;
        public RectTransform RegenerateButton =>
            _regenerateButton.RectTransform;
        public RectTransform CloseButton => _closeButton.RectTransform;
        public RectTransform HeaderCloseButton => CloseButton;
        public RectTransform AutoClearButton =>
            _autoClearButton.RectTransform;
        public RectTransform CreditButton => _creditButton.RectTransform;

        #region Unity Lifecycle

        void OnDestroy()
        {
            if (!_isConfigured)
            {
                return;
            }

            _hintButton.Clicked -= OnHint;
            _regenerateButton.Clicked -= OnRegenerate;
            _closeButton.Clicked -= OnClose;
            _autoClearButton.Clicked -= OnAutoClear;
            _creditButton.Clicked -= OnCredit;
        }

        #endregion

        #region Presentation

        public void Configure(PuzzleUiTheme theme)
        {
            if (!_isConfigured)
            {
                _hintButton.Clicked += OnHint;
                _regenerateButton.Clicked += OnRegenerate;
                _closeButton.Clicked += OnClose;
                _autoClearButton.Clicked += OnAutoClear;
                _creditButton.Clicked += OnCredit;
                _isConfigured = true;
            }

            _background.sprite = theme.RectangleSprite;
            _background.type = Image.Type.Sliced;
            var temporaryTexts = new[]
            {
                _mission,
                _instruction,
                _status,
                _steps,
                _minimum,
                _best,
                _details,
                _leaderboard
            };
            foreach (var temporaryText in temporaryTexts)
            {
                temporaryText.font = theme.DisplayFont;
            }

            _hintButton.Configure("1手ヒント", theme);
            _regenerateButton.Configure("パズル再生成", theme);
            _closeButton.Configure("×", theme);
            _autoClearButton.Configure("自動クリア", theme);
            _creditButton.Configure("着想・参考 ↗", theme);
        }

        #endregion

        void OnHint() => HintRequested?.Invoke();
        void OnRegenerate() => RegenerateRequested?.Invoke();
        void OnClose() => CloseRequested?.Invoke();
        void OnAutoClear() => AutoClearRequested?.Invoke();
        void OnCredit() => CreditRequested?.Invoke();
    }
}
