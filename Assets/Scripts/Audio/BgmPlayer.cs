using UnityEngine;

namespace SakakuPuzzle
{
    /// <summary>
    /// Plays the selected background music continuously across scenes.
    /// </summary>
    public static class BgmPlayer
    {
        const string RESOURCE_PATH = "Audio/Bgm/forest-voice-one";

        static AudioSource Source;

        #region Initialization

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState()
        {
            Source = null;
        }

        static void EnsureSource()
        {
            if (Source != null)
            {
                return;
            }

            var temporaryObject = new GameObject(
                "Background Music",
                typeof(AudioSource));
            Object.DontDestroyOnLoad(temporaryObject);
            Source = temporaryObject.GetComponent<AudioSource>();
            Source.playOnAwake = false;
            Source.loop = true;
            Source.volume = 0.24f;
        }

        #endregion

        #region Playback

        public static void Play()
        {
            EnsureSource();
            var temporaryClip = Resources.Load<AudioClip>(
                RESOURCE_PATH);
            if (temporaryClip == null)
            {
                Debug.LogWarning(
                    $"[BGM] 音源を読み込めません: {RESOURCE_PATH}");
                return;
            }

            if (Source.clip == temporaryClip && Source.isPlaying)
            {
                return;
            }

            Source.clip = temporaryClip;
            Source.gameObject.name =
                $"Background Music [{temporaryClip.name}]";
            Source.Play();
        }

        #endregion
    }
}
