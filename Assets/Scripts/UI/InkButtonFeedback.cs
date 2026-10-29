using UnityEngine;
using UnityEngine.EventSystems;

namespace SakakuPuzzle
{
    public sealed class InkButtonFeedback :
        MonoBehaviour,
        IPointerDownHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerUpHandler
    {
        [SerializeField, Range(0.8f, 1f)]
        float _pressedScale = 0.94f;
        [SerializeField, Range(1f, 1.15f)]
        float _hoverScale = 1.025f;
        [SerializeField, Range(4f, 30f)]
        float _responseSpeed = 18f;

        RectTransform _rectTransform;
        bool _hovered;
        bool _pressed;
        float _confirmationElapsed = -1f;

        #region Unity Lifecycle

        void Awake()
        {
            _rectTransform = transform as RectTransform;
        }

        void OnEnable()
        {
            _hovered = false;
            _pressed = false;
            _confirmationElapsed = -1f;
            if (_rectTransform != null)
            {
                _rectTransform.localScale = Vector3.one;
            }
        }

        void Update()
        {
            if (_rectTransform == null)
            {
                return;
            }

            var temporaryScale = GetTargetScale();
            var temporaryResponse =
                1f - Mathf.Exp(-_responseSpeed * Time.unscaledDeltaTime);
            _rectTransform.localScale = Vector3.Lerp(
                _rectTransform.localScale,
                Vector3.one * temporaryScale,
                temporaryResponse);

            if (_confirmationElapsed >= 0f)
            {
                _confirmationElapsed += Time.unscaledDeltaTime;
            }
        }

        #endregion

        #region Pointer Feedback

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
        }

        public void PlayConfirmation()
        {
            _confirmationElapsed = 0f;
            _pressed = false;
        }

        float GetTargetScale()
        {
            if (_confirmationElapsed >= 0f)
            {
                if (_confirmationElapsed < 0.08f)
                {
                    return 0.92f;
                }

                if (_confirmationElapsed < 0.22f)
                {
                    return 1.08f;
                }

                return 1f;
            }

            if (_pressed)
            {
                return _pressedScale;
            }

            return _hovered ? _hoverScale : 1f;
        }

        #endregion
    }
}
