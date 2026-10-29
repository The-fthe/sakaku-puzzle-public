using System;
using System.Collections;
using UnityEngine;

namespace SakakuPuzzle
{
    [DisallowMultipleComponent]
    public sealed class StageSelectView : MonoBehaviour
    {
        [SerializeField] StageSelectLayoutView _portraitLayout;
        [SerializeField] StageSelectLayoutView _landscapeLayout;

        bool _eventsBound;
        Vector2Int _lastScreenSize;
        Rect _lastSafeArea;

        public event Action<int> StageSelected;
        public event Action HowToRequested;

        public RectTransform RectTransform =>
            (RectTransform)transform;

        #region Unity Lifecycle

        void OnEnable()
        {
            BindEvents();
            ApplyCurrentViewport(true);
        }

        void Update()
        {
            ApplyCurrentViewport(false);
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
            _portraitLayout.Configure(theme);
            _landscapeLayout.Configure(theme);
            ApplyCurrentViewport(true);
        }

        public void ApplyViewport(
            Vector2 screenSize,
            Rect safeArea)
        {
            var temporaryWidth = Mathf.Max(1f, screenSize.x);
            var temporaryHeight = Mathf.Max(1f, screenSize.y);
            var temporaryAnchorMin = new Vector2(
                safeArea.xMin / temporaryWidth,
                safeArea.yMin / temporaryHeight);
            var temporaryAnchorMax = new Vector2(
                safeArea.xMax / temporaryWidth,
                safeArea.yMax / temporaryHeight);

            _portraitLayout.ApplySafeArea(
                temporaryAnchorMin,
                temporaryAnchorMax);
            _landscapeLayout.ApplySafeArea(
                temporaryAnchorMin,
                temporaryAnchorMax);

            var temporaryPortrait =
                temporaryHeight > temporaryWidth * 1.08f;
            _portraitLayout.gameObject.SetActive(temporaryPortrait);
            _landscapeLayout.gameObject.SetActive(!temporaryPortrait);
        }

        public void SetInteractable(bool interactable)
        {
            _portraitLayout.SetInteractable(interactable);
            _landscapeLayout.SetInteractable(interactable);
        }

        public void PlayStageConfirmation(int stageNumber)
        {
            var temporaryLayout = _portraitLayout.gameObject.activeSelf
                ? _portraitLayout
                : _landscapeLayout;
            var temporaryNode =
                temporaryLayout.GetStageNode(stageNumber);
            if (temporaryNode == null)
            {
                return;
            }

            temporaryNode.PlayConfirmation();
            StartCoroutine(
                AnimateStageRipple(temporaryNode.RectTransform));
        }

        #endregion

        #region Responsive Layout

        void ApplyCurrentViewport(bool force)
        {
            var temporaryScreenSize =
                new Vector2Int(Screen.width, Screen.height);
            var temporarySafeArea = Screen.safeArea;
            if (!force &&
                temporaryScreenSize == _lastScreenSize &&
                temporarySafeArea == _lastSafeArea)
            {
                return;
            }

            _lastScreenSize = temporaryScreenSize;
            _lastSafeArea = temporarySafeArea;
            ApplyViewport(temporaryScreenSize, temporarySafeArea);
        }

        #endregion

        #region Events

        void BindEvents()
        {
            if (_eventsBound ||
                _portraitLayout == null ||
                _landscapeLayout == null)
            {
                return;
            }

            _portraitLayout.StageSelected += NotifyStageSelected;
            _landscapeLayout.StageSelected += NotifyStageSelected;
            _portraitLayout.HowToRequested += NotifyHowToRequested;
            _landscapeLayout.HowToRequested += NotifyHowToRequested;
            _eventsBound = true;
        }

        void UnbindEvents()
        {
            if (!_eventsBound)
            {
                return;
            }

            _portraitLayout.StageSelected -= NotifyStageSelected;
            _landscapeLayout.StageSelected -= NotifyStageSelected;
            _portraitLayout.HowToRequested -= NotifyHowToRequested;
            _landscapeLayout.HowToRequested -= NotifyHowToRequested;
            _eventsBound = false;
        }

        void NotifyStageSelected(int stageNumber)
        {
            StageSelected?.Invoke(stageNumber);
        }

        void NotifyHowToRequested()
        {
            HowToRequested?.Invoke();
        }

        #endregion

        #region Animation

        static IEnumerator AnimateStageRipple(
            RectTransform selectedNode)
        {
            const float DURATION = 0.34f;
            var temporaryElapsed = 0f;
            while (temporaryElapsed < DURATION)
            {
                temporaryElapsed += Time.unscaledDeltaTime;
                var temporaryProgress =
                    Mathf.Clamp01(temporaryElapsed / DURATION);
                selectedNode.localScale =
                    Vector3.one * Mathf.Lerp(
                        1f,
                        1.12f,
                        Mathf.Sin(temporaryProgress * Mathf.PI));
                yield return null;
            }

            selectedNode.localScale = Vector3.one;
        }

        #endregion
    }
}
