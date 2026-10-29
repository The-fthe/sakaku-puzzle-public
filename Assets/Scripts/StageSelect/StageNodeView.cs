using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SakakuPuzzle
{
    [DisallowMultipleComponent]
    public sealed class StageNodeView : MonoBehaviour
    {
        [SerializeField] Image _image;
        [SerializeField] Button _button;
        [SerializeField] TMP_Text _number;
        [SerializeField] TMP_Text _description;
        [SerializeField] InkButtonFeedback _feedback;
        [SerializeField, Range(0f, 20f)]
        float _horizontalFloatAmplitude = 8f;
        [SerializeField, Range(0f, 20f)]
        float _verticalFloatAmplitude = 7f;
        [SerializeField, Range(1f, 5f)]
        float _floatDuration = 2.6f;

        RectTransform _rectTransform;
        Vector2 _restingPosition;
        Vector2 _descriptionRestingPosition;
        bool _eventsBound;
        bool _idleFloating;
        bool _positionsCaptured;

        public event Action<StageNodeView> Clicked;

        public int StageNumber { get; private set; }
        public RectTransform RectTransform =>
            (RectTransform)transform;

        #region Unity Lifecycle

        void Awake()
        {
            _rectTransform = (RectTransform)transform;
            CaptureRestingPositions();
            BindEvents();
        }

        void OnEnable()
        {
            CaptureRestingPositions();
            if (_button != null && _button.interactable)
            {
                StartIdleFloat();
            }
        }

        void Update()
        {
            if (!_idleFloating ||
                StageNumber <= 0 ||
                _rectTransform == null ||
                _description == null)
            {
                return;
            }

            var temporaryPhase =
                Time.unscaledTime / _floatDuration *
                Mathf.PI * 2f +
                (StageNumber - 1) * Mathf.PI * 0.5f;
            var temporaryOffset = new Vector2(
                Mathf.Sin(temporaryPhase) *
                _horizontalFloatAmplitude,
                Mathf.Cos(temporaryPhase) *
                _verticalFloatAmplitude);
            _rectTransform.anchoredPosition =
                _restingPosition + temporaryOffset;
            _description.rectTransform.anchoredPosition =
                _descriptionRestingPosition - temporaryOffset;
        }

        void OnDisable()
        {
            StopIdleFloat();
        }

        void OnDestroy()
        {
            UnbindEvents();
        }

        #endregion

        #region Configuration

        public void Configure(
            int stageNumber,
            string number,
            string description,
            PuzzleUiTheme theme)
        {
            BindEvents();
            StageNumber = stageNumber;
            name = $"ステージ {stageNumber}";
            _image.sprite = theme.StageNodeSprite;
            _image.color = Color.white;
            _number.font = theme.DisplayFont;
            _number.text = number;
            _description.font = theme.DisplayFont;
            _description.fontSize = 44f;
            _description.enableAutoSizing = true;
            _description.fontSizeMin = 34f;
            _description.fontSizeMax = 44f;
            _description.textWrappingMode =
                TextWrappingModes.NoWrap;
            _description.overflowMode = TextOverflowModes.Overflow;
            _description.richText = true;
            _description.text = description;
            if (_button.interactable)
            {
                StartIdleFloat();
            }
        }

        public void SetInteractable(bool interactable)
        {
            _button.interactable = interactable;
            if (interactable)
            {
                StartIdleFloat();
            }
            else
            {
                StopIdleFloat();
            }
        }

        public void PlayConfirmation()
        {
            StopIdleFloat();
            _feedback?.PlayConfirmation();
        }

        #endregion

        #region Idle Animation

        void CaptureRestingPositions()
        {
            if (_positionsCaptured ||
                _description == null)
            {
                return;
            }

            _rectTransform ??= (RectTransform)transform;
            _restingPosition = _rectTransform.anchoredPosition;
            _descriptionRestingPosition =
                _description.rectTransform.anchoredPosition;
            _positionsCaptured = true;
        }

        void StartIdleFloat()
        {
            CaptureRestingPositions();
            _idleFloating = _positionsCaptured;
        }

        void StopIdleFloat()
        {
            _idleFloating = false;
            if (!_positionsCaptured)
            {
                return;
            }

            _rectTransform.anchoredPosition = _restingPosition;
            _description.rectTransform.anchoredPosition =
                _descriptionRestingPosition;
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
            Clicked?.Invoke(this);
        }

        #endregion
    }
}
