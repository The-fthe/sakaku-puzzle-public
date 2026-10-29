using io.github.hatayama.uLoopMCP;
using UnityEditor;
using UnityEngine;

namespace SakakuPuzzle.Editor
{
    [InitializeOnLoad]
    public static class UloopAutoStart
    {
        #region Initialization

        static UloopAutoStart()
        {
            ScheduleStart();
        }

        [InitializeOnLoadMethod]
        static void RegisterAutoStart()
        {
            ScheduleStart();
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        static void RegisterAfterScriptReload()
        {
            ScheduleStart();
        }

        static void ScheduleStart()
        {
            EditorApplication.delayCall -= StartServer;
            EditorApplication.delayCall += StartServer;
        }

        #endregion

        #region Server

        public static void StartServer()
        {
            if (Application.isBatchMode || McpServerController.IsServerRunning)
            {
                return;
            }

            McpServerController.StartServer();
        }

        #endregion
    }
}
