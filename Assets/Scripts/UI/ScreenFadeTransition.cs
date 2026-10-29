using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SakakuPuzzle
{
    public sealed class ScreenFadeTransition : MonoBehaviour
    {
        [SerializeField] CanvasGroup _canvasGroup;
        [SerializeField] RectTransform _overlay;
        [SerializeField] Image _image;

        float _fadeInDuration = 0.34f;
        float _fadeOutDuration = 0.42f;
        float _opaqueHoldDuration = 0.10f;
        bool _isTransitioning;

        public bool IsTransitioning => _isTransitioning;

        #region Initialization

        public void Configure(
            Transform parent,
            PuzzleMotionSettings motion)
        {
            if (_overlay == null ||
                _canvasGroup == null ||
                _image == null)
            {
                Debug.LogError(
                    "[ScreenFadeTransition] Prefab参照が不足しています。",
                    this);
                return;
            }

            transform.SetParent(parent, false);
            _overlay.anchorMin = Vector2.zero;
            _overlay.anchorMax = Vector2.one;
            _overlay.offsetMin = Vector2.zero;
            _overlay.offsetMax = Vector2.zero;
            _image.raycastTarget = true;
            _fadeInDuration = motion.FadeInDuration;
            _fadeOutDuration = motion.FadeOutDuration;
            _opaqueHoldDuration = motion.FadeHoldDuration;
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _overlay.SetAsLastSibling();
            StartCoroutine(FadeIn());
        }

        #endregion

        #region Transition

        public bool FadeToScene(
            string sceneName,
            Action beforeLoad = null,
            float delay = 0f)
        {
            if (_isTransitioning || _canvasGroup == null)
            {
                return false;
            }

            _isTransitioning = true;
            _overlay.SetAsLastSibling();
            _canvasGroup.blocksRaycasts = true;
            StartCoroutine(
                FadeOutAndLoad(sceneName, beforeLoad, delay));
            return true;
        }

        IEnumerator FadeIn()
        {
            yield return AnimateAlpha(
                1f,
                0f,
                _fadeInDuration);
            _canvasGroup.blocksRaycasts = false;
        }

        IEnumerator FadeOutAndLoad(
            string sceneName,
            Action beforeLoad,
            float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }

            yield return AnimateAlpha(
                _canvasGroup.alpha,
                1f,
                _fadeOutDuration);
            yield return new WaitForSecondsRealtime(
                _opaqueHoldDuration);
            beforeLoad?.Invoke();
            SceneManager.LoadScene(sceneName);
        }

        IEnumerator AnimateAlpha(
            float startAlpha,
            float endAlpha,
            float duration)
        {
            var temporaryElapsed = 0f;
            while (temporaryElapsed < duration)
            {
                temporaryElapsed += Time.unscaledDeltaTime;
                var temporaryProgress =
                    Mathf.Clamp01(temporaryElapsed / duration);
                var temporaryEased =
                    temporaryProgress * temporaryProgress *
                    (3f - 2f * temporaryProgress);
                _canvasGroup.alpha = Mathf.Lerp(
                    startAlpha,
                    endAlpha,
                    temporaryEased);
                yield return null;
            }

            _canvasGroup.alpha = endAlpha;
        }

        #endregion
    }
}
