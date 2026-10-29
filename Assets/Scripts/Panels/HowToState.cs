using UnityEngine;

namespace SakakuPuzzle
{
    public static class HowToState
    {
        const string SEEN_KEY = "how_to_seen_v1";

        public static bool ShouldShowAutomatically =>
            PlayerPrefs.GetInt(SEEN_KEY, 0) == 0;

        #region State

        public static void MarkSeen()
        {
            PlayerPrefs.SetInt(SEEN_KEY, 1);
            PlayerPrefs.Save();
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem(
            "Tools/Sakaku Puzzle/Reset How To Seen")]
        public static void ResetForDebugMenu()
        {
            ResetForDebug();
            Debug.Log("[HowToState] Reset how-to seen state.");
        }

        public static void ResetForDebug()
        {
            PlayerPrefs.DeleteKey(SEEN_KEY);
            PlayerPrefs.Save();
        }
#endif

        #endregion
    }
}
