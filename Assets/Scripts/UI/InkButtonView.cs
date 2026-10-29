using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SakakuPuzzle
{
    public enum InkButtonShape
    {
        Rectangle,
        Square
    }

    [DisallowMultipleComponent]
    public sealed class InkButtonView : MonoBehaviour
    {
        [SerializeField] Image _image;
        [SerializeField] Button _button;
        [SerializeField] TMP_Text _label;
        [SerializeField] InkButtonFeedback _feedback;
        [SerializeField] InkButtonShape _shape;

        bool _eventsBound;

        public event Action Clicked;

        public RectTransform RectTransform =>
            (RectTransform)transform;
        public bool Interactable
        {
            get => _button != null && _button.interactable;
            set
            {
                if (_button != null)
                {
                    _button.interactable = value;
                }
            }
        }

        #region Unity Lifecycle

        void Awake()
        {
            BindEvents();
        }

        void OnDestroy()
        {
            UnbindEvents();
        }

        #endregion

        #region Configuration

        public void Configure(
            string label,
            PuzzleUiTheme theme)
        {
            BindEvents();
            SetLabel(label);
            if (_image != null)
            {
                _image.sprite = _shape == InkButtonShape.Square
                    ? theme.SquareSprite
                    : theme.RectangleSprite;
                _image.type = _shape == InkButtonShape.Square
                    ? Image.Type.Simple
                    : Image.Type.Sliced;
                _image.color = Color.white;
            }

            if (_label != null)
            {
                _label.font = theme.DisplayFont;
            }
        }

        public void SetLabel(string label)
        {
            if (_label != null)
            {
                _label.text = label;
            }
        }

        public void SetLabelSafeArea(
            Vector2 padding,
            float minimumFontSize,
            float maximumFontSize)
        {
            if (_label == null)
            {
                return;
            }

            var temporaryLabelRect = _label.rectTransform;
            temporaryLabelRect.offsetMin = padding;
            temporaryLabelRect.offsetMax = -padding;
            _label.enableAutoSizing = true;
            _label.fontSizeMin = Mathf.Min(
                minimumFontSize,
                maximumFontSize);
            _label.fontSizeMax = Mathf.Max(
                minimumFontSize,
                maximumFontSize);
        }

        public void PlayConfirmation()
        {
            _feedback?.PlayConfirmation();
        }

        #endregion

        #region Events

        void BindEvents()
        {
            if (_eventsBound || _button == null)
            {
                return;
            }

            _button.onClick.AddListener(NotifyClicked);
            _eventsBound = true;
        }

        void UnbindEvents()
        {
            if (!_eventsBound || _button == null)
            {
                return;
            }

            _button.onClick.RemoveListener(NotifyClicked);
            _eventsBound = false;
        }

        void NotifyClicked()
        {
            Clicked?.Invoke();
        }

        #endregion
    }
}
