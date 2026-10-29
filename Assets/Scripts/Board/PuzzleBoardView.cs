using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SakakuPuzzle
{
    [DisallowMultipleComponent]
    public sealed class PuzzleBoardView : MonoBehaviour
    {
        [SerializeField] RectTransform _gridRoot;
        [SerializeField] TileBoundaryGraphic _tileBoundaries;
        [SerializeField] RectTransform _overlayRoot;
        [SerializeField] GridLayoutGroup _gridLayout;

        const float BOUNDARY_PHASE_DURATION = 0.6f;
        const float BOUNDARY_CLEAR_FADE_DURATION = 0.2f;

        Coroutine _boundaryPulseCoroutine;
        Coroutine _boundaryClearCoroutine;

        public RectTransform RectTransform =>
            (RectTransform)transform;
        public RectTransform GridRoot => _gridRoot;
        public RectTransform OverlayRoot => _overlayRoot;
        public GridLayoutGroup GridLayout => _gridLayout;
        public bool IsBoundaryPulseRunning { get; private set; }

        public void Configure()
        {
            if (TryGetComponent<Image>(out var temporaryFrame))
            {
                temporaryFrame.type = Image.Type.Simple;
            }

            RestartBoundaryPulse();
        }

        public void RestartBoundaryPulse()
        {
            StopBoundaryCoroutines();
            IsBoundaryPulseRunning = true;
            SetBoundaryAlpha(1f);
            if (Application.isPlaying &&
                isActiveAndEnabled &&
                _tileBoundaries != null)
            {
                _boundaryPulseCoroutine =
                    StartCoroutine(AnimateBoundaryPulse());
            }
        }

        public void StopBoundaryPulseForClear()
        {
            StopBoundaryCoroutines();
            IsBoundaryPulseRunning = false;
            if (Application.isPlaying &&
                isActiveAndEnabled &&
                _tileBoundaries != null)
            {
                _boundaryClearCoroutine =
                    StartCoroutine(FadeBoundaryForClear());
                return;
            }

            SetBoundaryAlpha(0f);
        }

        void OnDisable()
        {
            StopBoundaryCoroutines();
            IsBoundaryPulseRunning = false;
        }

        IEnumerator AnimateBoundaryPulse()
        {
            while (true)
            {
                yield return HoldBoundaryAlpha(1f);
                yield return FadeBoundaryAlpha(1f, 0f);
                yield return HoldBoundaryAlpha(0f);
                yield return FadeBoundaryAlpha(0f, 1f);
            }
        }

        IEnumerator HoldBoundaryAlpha(float alpha)
        {
            SetBoundaryAlpha(alpha);
            var temporaryElapsed = 0f;
            while (temporaryElapsed < BOUNDARY_PHASE_DURATION)
            {
                temporaryElapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        IEnumerator FadeBoundaryAlpha(float from, float to)
        {
            var temporaryElapsed = 0f;
            while (temporaryElapsed < BOUNDARY_PHASE_DURATION)
            {
                temporaryElapsed += Time.unscaledDeltaTime;
                var temporaryProgress = Mathf.Clamp01(
                    temporaryElapsed / BOUNDARY_PHASE_DURATION);
                SetBoundaryAlpha(
                    Mathf.SmoothStep(from, to, temporaryProgress));
                yield return null;
            }

            SetBoundaryAlpha(to);
        }

        IEnumerator FadeBoundaryForClear()
        {
            var temporaryStartAlpha =
                _tileBoundaries.canvasRenderer.GetAlpha();
            var temporaryElapsed = 0f;
            while (temporaryElapsed < BOUNDARY_CLEAR_FADE_DURATION)
            {
                temporaryElapsed += Time.unscaledDeltaTime;
                SetBoundaryAlpha(
                    Mathf.Lerp(
                        temporaryStartAlpha,
                        0f,
                        Mathf.Clamp01(
                            temporaryElapsed /
                            BOUNDARY_CLEAR_FADE_DURATION)));
                yield return null;
            }

            SetBoundaryAlpha(0f);
            _boundaryClearCoroutine = null;
        }

        void StopBoundaryCoroutines()
        {
            if (_boundaryPulseCoroutine != null)
            {
                StopCoroutine(_boundaryPulseCoroutine);
                _boundaryPulseCoroutine = null;
            }

            if (_boundaryClearCoroutine != null)
            {
                StopCoroutine(_boundaryClearCoroutine);
                _boundaryClearCoroutine = null;
            }
        }

        void SetBoundaryAlpha(float alpha)
        {
            if (_tileBoundaries != null)
            {
                _tileBoundaries.canvasRenderer.SetAlpha(alpha);
            }
        }
    }
}
