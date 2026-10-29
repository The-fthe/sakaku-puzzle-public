using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SakakuPuzzle
{
    [DisallowMultipleComponent]
    public sealed class DestinationMarkerView : MonoBehaviour
    {
        [SerializeField] Image _image;
        [SerializeField] CanvasGroup _canvasGroup;
        [SerializeField] Image _arrivalRing;
        [SerializeField] CanvasGroup _arrivalRingGroup;

        Coroutine _arrivalCoroutine;

        public RectTransform RectTransform =>
            (RectTransform)transform;

        #region Configuration

        public void Configure(PuzzleUiTheme theme)
        {
            name = "目的地";
            if (_image != null)
            {
                _image.sprite = theme.DestinationSprite;
                _image.color = Color.white;
                _image.raycastTarget = false;
            }

            ResetMarker();
        }

        public void ResetMarker()
        {
            if (_arrivalCoroutine != null)
            {
                StopCoroutine(_arrivalCoroutine);
                _arrivalCoroutine = null;
            }

            gameObject.SetActive(true);
            RectTransform.localScale = Vector3.one;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            if (_arrivalRing != null)
            {
                _arrivalRing.gameObject.SetActive(false);
                _arrivalRing.rectTransform.localScale =
                    Vector3.one * 0.58f;
            }

            if (_arrivalRingGroup != null)
            {
                _arrivalRingGroup.alpha = 0f;
            }
        }

        public void PlayArrival(PuzzleMotionSettings motion)
        {
            if (_arrivalCoroutine != null)
            {
                return;
            }

            if (_arrivalRing != null)
            {
                _arrivalRing.gameObject.SetActive(true);
            }

            _arrivalCoroutine = StartCoroutine(
                AnimateArrival(motion.DestinationScatterDuration));
        }

        #endregion

        IEnumerator AnimateArrival(float duration)
        {
            var temporaryElapsed = 0f;
            while (temporaryElapsed < duration)
            {
                temporaryElapsed += Time.unscaledDeltaTime;
                var temporaryProgress =
                    Mathf.Clamp01(temporaryElapsed / duration);
                var temporaryScale = temporaryProgress < 0.35f
                    ? Mathf.Lerp(1f, 1.05f, temporaryProgress / 0.35f)
                    : Mathf.Lerp(
                        1.05f,
                        0f,
                        (temporaryProgress - 0.35f) / 0.65f);
                RectTransform.localScale =
                    Vector3.one * temporaryScale;
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha =
                        1f - temporaryProgress;
                }

                if (_arrivalRing != null)
                {
                    _arrivalRing.rectTransform.localScale =
                        Vector3.one *
                        Mathf.Lerp(
                            0.58f,
                            1.42f,
                            temporaryProgress);
                }

                if (_arrivalRingGroup != null)
                {
                    _arrivalRingGroup.alpha =
                        Mathf.Sin(temporaryProgress * Mathf.PI) *
                        0.72f;
                }

                yield return null;
            }

            _arrivalCoroutine = null;
            gameObject.SetActive(false);
        }
    }
}
