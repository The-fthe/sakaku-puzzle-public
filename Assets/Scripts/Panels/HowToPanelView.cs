using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SakakuPuzzle
{
    [DisallowMultipleComponent]
    public sealed class HowToPanelView : MonoBehaviour
    {
        const string PEEK_VIDEO_RELATIVE_PATH =
            "Tutorial/peek-how-to.mp4";
        const string CLEAR_VIDEO_RELATIVE_PATH =
            "Tutorial/stage1-how-to.mp4";
        const int PAGE_COUNT = 2;
        const int VIDEO_WIDTH = 960;
        const int VIDEO_HEIGHT = 540;

        [SerializeField] Image _shade;
        [SerializeField] Image _card;
        [SerializeField] RawImage _videoDisplay;
        [SerializeField] GameObject _poster;
        [SerializeField] RawImage _posterImage;
        [SerializeField] Texture2D _peekPosterTexture;
        [SerializeField] Texture2D _clearPosterTexture;
        [SerializeField] TMP_Text _posterCaption;
        [SerializeField] TMP_Text _title;
        [SerializeField] TMP_Text _pageTitle;
        [SerializeField] TMP_Text _description;
        [SerializeField] TMP_Text _minimumSteps;
        [SerializeField] TMP_Text _status;
        [SerializeField] Button _videoTouchButton;
        [SerializeField] GameObject _playbackIndicator;
        [SerializeField] TMP_Text _playbackLabel;
        [SerializeField] InkButtonView _previousButton;
        [SerializeField] InkButtonView _nextButton;
        [SerializeField] InkButtonView _startButton;
        [SerializeField] VideoPlayer _videoPlayer;

        RenderTexture _renderTexture;
        Coroutine _hidePlaybackIndicatorCoroutine;
        bool _eventsBound;
        int _currentPageIndex;

        public event Action StartRequested;

        public RectTransform RectTransform =>
            (RectTransform)transform;
        public RectTransform CardRect => _card.rectTransform;
        public TMP_Text Title => _title;
        public TMP_Text MinimumSteps => _minimumSteps;
        public InkButtonView NextButton => _nextButton;
        public VideoPlayer VideoPlayer => _videoPlayer;
        public string CurrentVideoRelativePath =>
            _currentPageIndex == 0
                ? PEEK_VIDEO_RELATIVE_PATH
                : CLEAR_VIDEO_RELATIVE_PATH;

        #region Unity Lifecycle

        void Awake()
        {
            BindEvents();
        }

        void OnDestroy()
        {
            UnbindEvents();
            ReleaseRenderTexture();
        }

        #endregion

        #region Presentation

        public void Configure(PuzzleUiTheme theme)
        {
            BindEvents();
            _shade.color = new Color(0.04f, 0.04f, 0.03f, 0.82f);

            foreach (var temporaryText in new[]
                     {
                         _title,
                         _pageTitle,
                         _description,
                         _minimumSteps,
                         _status,
                         _playbackLabel
                     })
            {
                temporaryText.font = theme.DisplayFont;
            }

            _title.text = "遊び方";
            _description.text =
                "1. 覗く：注目しているタイルの道だけが見える。\n" +
                "2. 回す：クリックで90度回転。";
            _minimumSteps.text =
                "ステージ1は最短2手。手数に数えるのは回転だけ。";
            _status.text = "動画を読み込み中…";

            _previousButton.Configure("前", theme);
            _nextButton.Configure("次", theme);
            _startButton.Configure("ゲームを始める", theme);

            _videoPlayer.playOnAwake = false;
            _videoPlayer.isLooping = true;
            _videoPlayer.source = VideoSource.Url;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _playbackIndicator.SetActive(false);
            SetPage(0, false);
            Hide();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            RectTransform.SetAsLastSibling();
            SetPage(0, Application.isPlaying);
        }

        public void Hide()
        {
            if (Application.isPlaying && _videoPlayer != null)
            {
                _videoPlayer.Stop();
            }

            ReleaseRenderTexture();
            gameObject.SetActive(false);
        }

        public void FitToViewport(Vector2 availableSize)
        {
            var temporaryScale = Mathf.Min(
                1f,
                availableSize.x / 1040f,
                availableSize.y / 1000f);
            _card.rectTransform.localScale =
                Vector3.one * temporaryScale;
        }

        #endregion

        #region Video

        void SetPage(int pageIndex, bool prepareVideo)
        {
            _currentPageIndex =
                (pageIndex % PAGE_COUNT + PAGE_COUNT) % PAGE_COUNT;
            _pageTitle.text = _currentPageIndex == 0
                ? "1 / 2　覗く"
                : "2 / 2　回す・クリア";
            _posterImage.texture = _currentPageIndex == 0
                ? _peekPosterTexture
                : _clearPosterTexture;
            _posterCaption.text = _currentPageIndex == 0
                ? "カーソルを近づけて道を覗こう"
                : "タイルを回して目的地までつなごう";
            _poster.SetActive(true);
            HidePlaybackIndicator();
            _status.gameObject.SetActive(true);
            _status.text = "動画を読み込み中…";
            _videoTouchButton.interactable = false;

            if (!prepareVideo)
            {
                return;
            }

            _videoPlayer.Stop();
            PrepareVideo();
        }

        void PrepareVideo()
        {
            ReleaseRenderTexture();
            _renderTexture = new RenderTexture(
                VIDEO_WIDTH,
                VIDEO_HEIGHT,
                0,
                RenderTextureFormat.ARGB32)
            {
                name = "HowToVideoRenderTexture"
            };
            _renderTexture.Create();
            _videoDisplay.texture = _renderTexture;
            _videoPlayer.targetTexture = _renderTexture;
            _videoPlayer.url = BuildVideoUrl(
                CurrentVideoRelativePath);
            _videoPlayer.Prepare();
        }

        static string BuildVideoUrl(string relativePath)
        {
            return Path.Combine(
                Application.streamingAssetsPath,
                relativePath);
        }

        void OnVideoPrepared(VideoPlayer source)
        {
            _poster.SetActive(false);
            HidePlaybackIndicator();
            _status.gameObject.SetActive(false);
            _videoTouchButton.interactable = true;
            source.Play();
        }

        void OnVideoError(VideoPlayer source, string message)
        {
            _poster.SetActive(true);
            _status.gameObject.SetActive(true);
            _status.text =
                "動画を読み込めませんでした。説明を読んでゲームを始められます。";
            HidePlaybackIndicator();
            _videoTouchButton.interactable = false;
        }

        void OnVideoTouched()
        {
            if (!_videoPlayer.isPrepared)
            {
                return;
            }

            if (_videoPlayer.isPlaying)
            {
                _videoPlayer.Pause();
                ShowPlaybackIndicator("停止", false);
                return;
            }

            _videoPlayer.Play();
            ShowPlaybackIndicator("再開", true);
        }

        void OnPrevious()
        {
            SetPage(_currentPageIndex - 1, Application.isPlaying);
        }

        void OnNext()
        {
            SetPage(_currentPageIndex + 1, Application.isPlaying);
        }

        void ShowPlaybackIndicator(
            string label,
            bool hideAfterDelay)
        {
            StopPlaybackIndicatorCoroutine();
            _playbackLabel.text = label;
            _playbackIndicator.SetActive(true);

            if (hideAfterDelay)
            {
                _hidePlaybackIndicatorCoroutine =
                    StartCoroutine(HidePlaybackIndicatorAfterDelay());
            }
        }

        IEnumerator HidePlaybackIndicatorAfterDelay()
        {
            yield return new WaitForSecondsRealtime(0.55f);
            _playbackIndicator.SetActive(false);
            _hidePlaybackIndicatorCoroutine = null;
        }

        void HidePlaybackIndicator()
        {
            StopPlaybackIndicatorCoroutine();
            _playbackIndicator.SetActive(false);
        }

        void StopPlaybackIndicatorCoroutine()
        {
            if (_hidePlaybackIndicatorCoroutine == null)
            {
                return;
            }

            StopCoroutine(_hidePlaybackIndicatorCoroutine);
            _hidePlaybackIndicatorCoroutine = null;
        }

        void ReleaseRenderTexture()
        {
            if (_renderTexture == null)
            {
                return;
            }

            if (_videoPlayer != null)
            {
                _videoPlayer.targetTexture = null;
            }

            if (_videoDisplay != null)
            {
                _videoDisplay.texture = null;
            }

            _renderTexture.Release();
            Destroy(_renderTexture);
            _renderTexture = null;
        }

        #endregion

        #region Events

        void BindEvents()
        {
            if (_eventsBound ||
                _videoTouchButton == null ||
                _playbackIndicator == null ||
                _playbackLabel == null ||
                _previousButton == null ||
                _nextButton == null ||
                _startButton == null ||
                _videoPlayer == null)
            {
                return;
            }

            _videoTouchButton.onClick.AddListener(OnVideoTouched);
            _previousButton.Clicked += OnPrevious;
            _nextButton.Clicked += OnNext;
            _startButton.Clicked += OnStart;
            _videoPlayer.prepareCompleted += OnVideoPrepared;
            _videoPlayer.errorReceived += OnVideoError;
            _eventsBound = true;
        }

        void UnbindEvents()
        {
            if (!_eventsBound)
            {
                return;
            }

            _videoTouchButton.onClick.RemoveListener(OnVideoTouched);
            _previousButton.Clicked -= OnPrevious;
            _nextButton.Clicked -= OnNext;
            _startButton.Clicked -= OnStart;
            _videoPlayer.prepareCompleted -= OnVideoPrepared;
            _videoPlayer.errorReceived -= OnVideoError;
            _eventsBound = false;
        }

        void OnStart()
        {
            StartRequested?.Invoke();
        }

        #endregion
    }
}
