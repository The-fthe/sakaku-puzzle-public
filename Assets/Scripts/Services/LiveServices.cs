using System.Collections.Generic;
using unityroom.Api;
using UnityEngine;

namespace SakakuPuzzle
{
    public static class LiveServices
    {
        const int UNITYROOM_BOARD_NO = 1;

        static bool IsInitialized;
        static UnityroomApiClient UnityroomClient;

        #region Initialization

        public static void Configure(LiveServiceAssets assets)
        {
            if (IsInitialized)
            {
                return;
            }

            IsInitialized = true;
            InitializeUnityroom(assets?.UnityroomHmacKey);
        }

        static void InitializeUnityroom(TextAsset keyAsset)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (keyAsset == null ||
                string.IsNullOrWhiteSpace(keyAsset.text))
            {
                Debug.LogError(
                    "[LiveServices] unityroom HMACキーが未設定のため、" +
                    "スコア送信を初期化できません。");
                return;
            }

            var temporaryClientObject = new GameObject(
                "Unityroom API Client");
            temporaryClientObject.SetActive(false);
            UnityroomClient =
                temporaryClientObject.AddComponent<UnityroomApiClient>();
            var temporaryKeyField = typeof(UnityroomApiClient).GetField(
                "HmacKey",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            if (temporaryKeyField == null)
            {
                Debug.LogError(
                    "[LiveServices] unityroomクライアントのHMAC設定欄を" +
                    "取得できませんでした。");
                UnityEngine.Object.Destroy(temporaryClientObject);
                return;
            }

            temporaryKeyField.SetValue(
                UnityroomClient,
                keyAsset.text.Trim());
            temporaryClientObject.SetActive(true);
#endif
        }

        #endregion

        #region Ranking

        public static void SubmitCompletedChallenge(
            IReadOnlyList<int> stageSteps)
        {
            if (!StageSession.CanSubmitRanking ||
                stageSteps == null ||
                stageSteps.Count != 4)
            {
                return;
            }

            var temporaryTotal = 0;
            foreach (var temporarySteps in stageSteps)
            {
                temporaryTotal += temporarySteps;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            if (UnityroomClient == null)
            {
                Debug.LogError(
                    "[LiveServices] unityroomクライアントが未初期化のため、" +
                    "スコアを送信できません。");
                return;
            }

            UnityroomClient.SendScore(
                UNITYROOM_BOARD_NO,
                temporaryTotal,
                ScoreboardWriteMode.HighScoreAsc);
#else
            Debug.Log(
                $"[Ranking] 合計 {temporaryTotal}手（実機送信なし）");
#endif
        }

        #endregion

    }
}
