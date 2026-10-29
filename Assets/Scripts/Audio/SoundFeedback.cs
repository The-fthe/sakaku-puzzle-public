using UnityEngine;

namespace SakakuPuzzle
{
    public static class SoundFeedback
    {
        static AudioSource Source;
        static AudioClip RotateClip;
        static AudioClip GoalClip;
        static AudioClip ClearClip;

        #region Initialization

        public static void Configure(PuzzleUiTheme theme)
        {
            if (Source != null)
            {
                return;
            }

            var temporaryAudioObject = new GameObject(
                "Sakaku Sound Feedback");
            Source = temporaryAudioObject.AddComponent<AudioSource>();
            Source.playOnAwake = false;
            Source.loop = false;
            Source.spatialBlend = 0f;
            Object.DontDestroyOnLoad(temporaryAudioObject);

            RotateClip = theme.RotateClip;
            GoalClip = theme.GoalClip;
            ClearClip = theme.ClearClip;
        }

        #endregion

        #region Playback

        public static void PlayRotate()
        {
            Play(RotateClip, 0.42f);
        }

        public static void PlayGoalArrival()
        {
            Play(GoalClip, 0.48f);
        }

        public static void PlayStageClear()
        {
            Play(ClearClip, 0.55f);
        }

        static void Play(AudioClip clip, float volume)
        {
            if (Source == null || clip == null)
            {
                return;
            }

            Source.PlayOneShot(clip, volume);
        }

        #endregion
    }
}
