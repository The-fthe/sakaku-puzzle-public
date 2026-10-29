using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SakakuPuzzle
{
    [DisallowMultipleComponent]
    public sealed class StageSelectLayoutView : MonoBehaviour
    {
        const string ACCENT_HEX = "B13322";

        static readonly string[] JapaneseNumbers =
        {
            "一", "二", "三", "四"
        };

        [SerializeField] bool _portrait;
        [SerializeField] RawImage _background;
        [SerializeField] RectTransform _safeArea;
        [SerializeField] TMP_Text _title;
        [SerializeField] TMP_Text _subtitle;
        [SerializeField] TMP_Text _footer;
        [SerializeField] InkButtonView _howToButton;
        [SerializeField] StageNodeView[] _stageNodes;

        bool _eventsBound;

        public event Action<int> StageSelected;
        public event Action HowToRequested;

        public RectTransform SafeArea => _safeArea;

        #region Unity Lifecycle

        void OnEnable()
        {
            BindEvents();
        }

        void OnDisable()
        {
            UnbindEvents();
        }

        void OnDestroy()
        {
            UnbindEvents();
        }

        #endregion

        #region Configuration

        public void Configure(PuzzleUiTheme theme)
        {
            BindEvents();
            _background.texture = _portrait
                ? theme.PortraitBackdrop
                : theme.LandscapeBackdrop;
            _title.font = theme.DisplayFont;
            _subtitle.font = theme.DisplayFont;
            _footer.font = theme.DisplayFont;
            _howToButton.Configure("遊び方", theme);
            for (var temporaryIndex = 0;
                 temporaryIndex < _stageNodes.Length;
                 temporaryIndex++)
            {
                var temporaryStageNumber = temporaryIndex + 1;
                _stageNodes[temporaryIndex].Configure(
                    temporaryStageNumber,
                    JapaneseNumbers[temporaryIndex],
                    BuildStageDescription(
                        JapaneseNumbers[temporaryIndex],
                        temporaryStageNumber == 1
                            ? "ランキング挑戦"
                            : "自由に遊ぶ",
                        ACCENT_HEX),
                    theme);
            }
        }

        public void ApplySafeArea(
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            _safeArea.anchorMin = anchorMin;
            _safeArea.anchorMax = anchorMax;
            _safeArea.offsetMin = Vector2.zero;
            _safeArea.offsetMax = Vector2.zero;
        }

        public void SetInteractable(bool interactable)
        {
            foreach (var temporaryNode in _stageNodes)
            {
                temporaryNode.SetInteractable(interactable);
            }

            _howToButton.Interactable = interactable;
        }

        public StageNodeView GetStageNode(int stageNumber)
        {
            if (stageNumber < 1 ||
                stageNumber > _stageNodes.Length)
            {
                return null;
            }

            return _stageNodes[stageNumber - 1];
        }

        #endregion

        #region Text

        static string BuildStageDescription(
            string destinationNumber,
            string modeLabel,
            string accentHex)
        {
            return
                $"目的地 {destinationNumber}　" +
                $"<color=#{accentHex}>{modeLabel}</color>";
        }

        #endregion

        #region Events

        void BindEvents()
        {
            if (_eventsBound ||
                _howToButton == null ||
                _stageNodes == null)
            {
                return;
            }

            _howToButton.Clicked += NotifyHowToRequested;
            foreach (var temporaryNode in _stageNodes)
            {
                if (temporaryNode != null)
                {
                    temporaryNode.Clicked += NotifyStageSelected;
                }
            }

            _eventsBound = true;
        }

        void UnbindEvents()
        {
            if (!_eventsBound)
            {
                return;
            }

            _howToButton.Clicked -= NotifyHowToRequested;
            foreach (var temporaryNode in _stageNodes)
            {
                if (temporaryNode != null)
                {
                    temporaryNode.Clicked -= NotifyStageSelected;
                }
            }

            _eventsBound = false;
        }

        void NotifyStageSelected(StageNodeView stageNode)
        {
            StageSelected?.Invoke(stageNode.StageNumber);
        }

        void NotifyHowToRequested()
        {
            HowToRequested?.Invoke();
        }

        #endregion
    }
}
