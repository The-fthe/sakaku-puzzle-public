#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace SakakuPuzzle.Editor
{
    public static class GameBuild
    {
        const string STAGE_SELECT_SCENE_PATH =
            "Assets/Scenes/StageSelect.unity";
        const string GAME_SCENE_PATH =
            "Assets/Scenes/Game.unity";
        const string WINDOWS_BUILD_PATH = "Builds/Windows/SakakuPuzzle.exe";
        const string UNITYROOM_BUILD_PROFILE_PATH =
            "Assets/Settings/Build Profiles/unityroom.asset";
        const string LIVE_SERVICE_ASSETS_PATH =
            "Assets/GameAssets/Settings/LiveServiceAssets.asset";
        const string WEBGL_BUILD_PATH =
            "Builds/WebGL/unityroom_build";
        const string GAME_CURSOR_PATH =
            "Assets/GameAssets/Art/game-cursor.png";
        const string WEBGL_STREAMING_ASSETS_PATH =
            WEBGL_BUILD_PATH + "/StreamingAssets";
        static readonly string[] WebGLVideoRelativePaths =
        {
            "Tutorial/peek-how-to.mp4",
            "Tutorial/stage1-how-to.mp4"
        };
        const string ANDROID_BUILD_PATH = "Builds/Android/SakakuPuzzle.apk";

        [MenuItem("Tools/差覚パズル/経路と手数を検証")]
        public static void ValidatePuzzleLogic()
        {
            ValidatePuzzle();
        }

        public static void BuildWindows()
        {
            EnsureTextMeshProResources();
            ValidatePuzzle();
            EnsureScenes();
            ConfigurePlayer();
            Build(
                WINDOWS_BUILD_PATH,
                BuildTarget.StandaloneWindows64,
                "Windows");
        }

        [MenuItem("Tools/差覚パズル/WebGLをビルド")]
        public static void BuildWebGL()
        {
            EnsureTextMeshProResources();
            ValidatePuzzle();
            EnsureScenes();
            ConfigurePlayer();
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;
            ValidateWebGLVideoAssets("Assets/StreamingAssets");
            ValidateUnityroomConfiguration();
            BuildWebGLWithUnityroomProfile();
        }

        public static void BuildAndroid()
        {
            EnsureTextMeshProResources();
            ValidatePuzzle();
            EnsureScenes();
            ConfigurePlayer();
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            Build(ANDROID_BUILD_PATH, BuildTarget.Android, "Android");
        }

        static void ConfigurePlayer()
        {
            ConfigureDefaultCursor();
            PlayerSettings.productName = "白路";
            PlayerSettings.companyName = "FTHE";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Android,
                "com.fthe.sakakupuzzle");
        }

        static void ConfigureDefaultCursor()
        {
            var temporaryCursor =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    GAME_CURSOR_PATH);
            if (temporaryCursor == null)
            {
                throw new BuildFailedException(
                    $"Default Cursorが見つかりません: " +
                    GAME_CURSOR_PATH);
            }

            PlayerSettings.defaultCursor = temporaryCursor;
            PlayerSettings.cursorHotspot =
                GameCursorView.SystemHotspot;
        }

        static void Build(string buildPath, BuildTarget buildTarget, string platformName)
        {
            var temporaryBuildDirectory = Path.GetDirectoryName(buildPath);
            if (!string.IsNullOrEmpty(temporaryBuildDirectory))
            {
                Directory.CreateDirectory(temporaryBuildDirectory);
            }

            var temporaryOptions = new BuildPlayerOptions
            {
                scenes = new[]
                {
                    STAGE_SELECT_SCENE_PATH,
                    GAME_SCENE_PATH
                },
                locationPathName = buildPath,
                target = buildTarget,
                options = BuildOptions.None
            };
            var temporaryReport = BuildPipeline.BuildPlayer(temporaryOptions);
            if (temporaryReport.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"Build failed: {temporaryReport.summary.result}");
            }

            Debug.Log(
                $"{platformName} build created: {Path.GetFullPath(buildPath)}");
        }

        static void BuildWebGLWithUnityroomProfile()
        {
            var temporaryProfile =
                AssetDatabase.LoadAssetAtPath<BuildProfile>(
                    UNITYROOM_BUILD_PROFILE_PATH);
            if (temporaryProfile == null)
            {
                throw new BuildFailedException(
                    $"unityroom Build Profileが見つかりません: " +
                    UNITYROOM_BUILD_PROFILE_PATH);
            }

            ValidateUnityroomProfile(temporaryProfile);
            BuildProfile.SetActiveBuildProfile(temporaryProfile);
            ConfigureDefaultCursor();

            Directory.CreateDirectory(WEBGL_BUILD_PATH);
            var temporaryOptions = new BuildPlayerWithProfileOptions
            {
                buildProfile = temporaryProfile,
                locationPathName = WEBGL_BUILD_PATH,
                options = Application.isBatchMode
                    ? BuildOptions.None
                    : BuildOptions.AutoRunPlayer
            };
            var temporaryReport =
                BuildPipeline.BuildPlayer(temporaryOptions);
            if (temporaryReport.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"WebGL build failed: " +
                    temporaryReport.summary.result);
            }

            ValidateWebGLVideoAssets(WEBGL_STREAMING_ASSETS_PATH);
            Debug.Log(
                $"WebGL build created: " +
                $"{Path.GetFullPath(WEBGL_BUILD_PATH)}");
        }

        static void ValidateWebGLVideoAssets(string rootPath)
        {
            foreach (var temporaryRelativePath in WebGLVideoRelativePaths)
            {
                var temporaryVideoPath = Path.Combine(
                    rootPath,
                    temporaryRelativePath);
                if (!File.Exists(temporaryVideoPath))
                {
                    throw new BuildFailedException(
                        $"WebGL tutorial video was not found: " +
                        temporaryVideoPath);
                }
            }
        }

        static void ValidateUnityroomConfiguration()
        {
            var temporaryAssets =
                AssetDatabase.LoadAssetAtPath<LiveServiceAssets>(
                    LIVE_SERVICE_ASSETS_PATH);
            if (temporaryAssets == null)
            {
                throw new BuildFailedException(
                    $"unityroom設定が見つかりません: " +
                    LIVE_SERVICE_ASSETS_PATH);
            }

            var temporaryKeyAsset = temporaryAssets.UnityroomHmacKey;
            if (temporaryKeyAsset == null ||
                string.IsNullOrWhiteSpace(temporaryKeyAsset.text))
            {
                throw new BuildFailedException(
                    "unityroom HMACキーが設定されていません。" +
                    "Assets/Resources/Config/unityroom-hmac.txtを作成し、" +
                    "LiveServiceAssetsへ割り当ててください。");
            }
        }

        static void ValidateUnityroomProfile(BuildProfile profile)
        {
            var temporarySerializedProfile =
                new SerializedObject(profile);
            var temporaryPlatformSettings =
                temporarySerializedProfile.FindProperty(
                    "m_PlatformBuildProfile");
            var temporaryDevelopment =
                temporaryPlatformSettings?.FindPropertyRelative(
                    "m_Development");
            if (temporaryDevelopment == null ||
                temporaryDevelopment.boolValue)
            {
                throw new BuildFailedException(
                    "unityroom Build ProfileのDevelopment Buildを" +
                    "オフにしてください。");
            }

            var temporaryPlayerSettingsProperty =
                typeof(BuildProfile).GetProperty(
                    "playerSettings",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);
            var temporaryPlayerSettings =
                temporaryPlayerSettingsProperty?.GetValue(profile)
                as PlayerSettings;
            if (temporaryPlayerSettings == null)
            {
                throw new BuildFailedException(
                    "unityroom Build ProfileのPlayer Settingsを" +
                    "読み取れません。");
            }

            var temporarySerializedPlayerSettings =
                new SerializedObject(temporaryPlayerSettings);
            var temporaryCompression =
                temporarySerializedPlayerSettings.FindProperty(
                    "webGLCompressionFormat");
            var temporaryFallback =
                temporarySerializedPlayerSettings.FindProperty(
                    "webGLDecompressionFallback");
            if (temporaryCompression == null ||
                temporaryCompression.intValue !=
                (int)WebGLCompressionFormat.Gzip)
            {
                throw new BuildFailedException(
                    "unityroom Build ProfileのCompression Formatを" +
                    "Gzipにしてください。");
            }

            if (temporaryFallback == null ||
                temporaryFallback.boolValue)
            {
                throw new BuildFailedException(
                    "unityroom Build ProfileのDecompression Fallbackを" +
                    "オフにしてください。");
            }
        }

        static void ValidatePuzzle()
        {
            var temporarySeeds = new[] { 1201, 2302, 3403, 4504 };
            for (var temporaryVersion = 1;
                 temporaryVersion <= 4;
                 temporaryVersion++)
            {
                var temporaryDefinition = PuzzleDefinition.CreateStage(
                    temporaryVersion,
                    temporarySeeds[temporaryVersion - 1]);
                if (temporaryDefinition.StartIndex != 20 ||
                    temporaryDefinition.StartCorner != TileCorner.BottomLeft ||
                    temporaryDefinition.Goals.Count != temporaryVersion)
                {
                    throw new BuildFailedException(
                        $"ステージ{temporaryVersion}の開始位置または目的地数が不正です。");
                }

                for (var temporaryGoalIndex = 0;
                     temporaryGoalIndex <
                     temporaryDefinition.Goals.Count;
                     temporaryGoalIndex++)
                {
                    if (temporaryDefinition.Goals[
                            temporaryGoalIndex].Label !=
                        (temporaryGoalIndex + 1).ToString())
                    {
                        throw new BuildFailedException(
                            $"ステージ{temporaryVersion}の目的地番号が不正です。");
                    }
                }

                var temporaryState = new PuzzleState(temporaryDefinition);
                if (temporaryState.IsSolved)
                {
                    throw new BuildFailedException(
                        $"版{temporaryVersion}が回転なしでクリアできました。");
                }

                var temporaryInitialState = temporaryState.EncodedState;
                if (temporaryState.Rotate(-1) ||
                    temporaryState.Steps != 0 ||
                    temporaryState.CountRecordedRotations() != 0)
                {
                    throw new BuildFailedException(
                        "盤外タイルの操作が回転数として記録されました。");
                }

                temporaryState.Rotate(0);
                temporaryState.Rotate(0);
                if (temporaryState.EncodedState != temporaryInitialState ||
                    temporaryState.Steps != 2 ||
                    temporaryState.GetRotationCount(0) != 2 ||
                    temporaryState.CountRecordedRotations() != 2)
                {
                    throw new BuildFailedException(
                        "1プレイ内のタイル回転数を正確に記録できません。");
                }

                temporaryState.Reset();
                var temporarySolution =
                    temporaryState.FindShortestSolution();
                if (temporarySolution.Count !=
                    temporaryDefinition.MinimumSteps ||
                    temporarySolution.Count < 1)
                {
                    throw new BuildFailedException(
                        $"版{temporaryVersion}の最短手数が不正です。" +
                        $" expected={temporaryDefinition.MinimumSteps}," +
                        $" actual={temporarySolution.Count}");
                }

                foreach (var temporaryTile in temporarySolution)
                {
                    temporaryState.Rotate(temporaryTile);
                }

                var temporaryEvaluation =
                    temporaryState.TraceCurrentRoute();
                if (!temporaryState.IsSolved ||
                    !temporaryEvaluation.ReachedGoal ||
                    temporaryEvaluation.ReachedGoalCount !=
                    temporaryDefinition.Goals.Count)
                {
                    throw new BuildFailedException(
                        $"版{temporaryVersion}で全ゴールへ接続できません。");
                }

                var temporaryJourneys =
                    temporaryState
                        .FindSolvedJourneysByNearestGoal();
                if (temporaryJourneys.Count !=
                    temporaryDefinition.Goals.Count)
                {
                    throw new BuildFailedException(
                        $"版{temporaryVersion}の移動演出経路数が不正です。");
                }

                for (var temporaryJourneyIndex = 0;
                     temporaryJourneyIndex < temporaryJourneys.Count;
                     temporaryJourneyIndex++)
                {
                    var temporaryJourney =
                        temporaryJourneys[temporaryJourneyIndex];
                    var temporaryGoal =
                        temporaryDefinition.Goals[
                            temporaryJourney.GoalIndex];
                    if (temporaryJourney.Steps.Count == 0)
                    {
                        throw new BuildFailedException(
                            $"Aの移動経路が{temporaryGoal.Label}へ到達しません。");
                    }

                    var temporaryLastStep =
                        temporaryJourney.Steps[
                            temporaryJourney.Steps.Count - 1];
                    var temporaryActualVertex =
                        PuzzleDefinition.GetVertexId(
                            temporaryDefinition.Width,
                            temporaryLastStep.TileIndex,
                            temporaryLastStep.ExitCorner);
                    var temporaryExpectedVertex =
                        PuzzleDefinition.GetVertexId(
                            temporaryDefinition.Width,
                            temporaryGoal.TileIndex,
                            temporaryGoal.Corner);
                    if (temporaryActualVertex != temporaryExpectedVertex)
                    {
                        throw new BuildFailedException(
                            $"Aの移動経路が{temporaryGoal.Label}へ到達しません。");
                    }

                    foreach (var temporaryStep in
                             temporaryJourney.Steps)
                    {
                        var temporaryUsesRoadBetweenWalls =
                            (temporaryStep.EntryCorner ==
                             TileCorner.BottomLeft &&
                             temporaryStep.ExitCorner ==
                             TileCorner.TopRight) ||
                            (temporaryStep.EntryCorner ==
                             TileCorner.TopRight &&
                             temporaryStep.ExitCorner ==
                             TileCorner.BottomLeft) ||
                            (temporaryStep.EntryCorner ==
                             TileCorner.TopLeft &&
                             temporaryStep.ExitCorner ==
                             TileCorner.BottomRight) ||
                            (temporaryStep.EntryCorner ==
                             TileCorner.BottomRight &&
                             temporaryStep.ExitCorner ==
                             TileCorner.TopLeft);
                        if (!temporaryUsesRoadBetweenWalls)
                        {
                            throw new BuildFailedException(
                                "黒線上を道路として扱う経路があります。");
                        }
                    }
                }

                if (temporaryState.Steps != temporarySolution.Count ||
                    temporaryState.CountRecordedRotations() !=
                    temporaryState.Steps)
                {
                    throw new BuildFailedException(
                        "自動解法と記録された手数が一致しません。");
                }

                Debug.Log(
                    $"Version {temporaryVersion} validation passed: " +
                    $"{temporaryDefinition.Goals.Count} goals, " +
                    $"PAR {temporaryDefinition.MinimumSteps}, seed " +
                    $"{temporaryDefinition.Seed}.");
            }
        }

        public static void EnsureScenes()
        {
            Directory.CreateDirectory("Assets/Scenes");
            EnsureGameScene();
            EnsureStageSelectScene();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(
                    STAGE_SELECT_SCENE_PATH,
                    true),
                new EditorBuildSettingsScene(
                    GAME_SCENE_PATH,
                    true)
            };
            AssetDatabase.SaveAssets();
        }

        static void EnsureGameScene()
        {
            if (File.Exists(GAME_SCENE_PATH))
            {
                EditorSceneManager.OpenScene(
                    GAME_SCENE_PATH,
                    OpenSceneMode.Single);
                if (Object.FindAnyObjectByType<Camera>() != null &&
                    Object.FindAnyObjectByType<PuzzleSceneController>() != null)
                {
                    return;
                }
            }

            var temporaryScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            temporaryScene.name = "Game";

            var temporaryCameraObject = new GameObject(
                "Main Camera",
                typeof(Camera),
                typeof(AudioListener));
            temporaryCameraObject.tag = "MainCamera";
            temporaryCameraObject.transform.SetPositionAndRotation(
                new Vector3(0f, 0f, -10f),
                Quaternion.identity);
            var temporaryCamera = temporaryCameraObject.GetComponent<Camera>();
            temporaryCamera.orthographic = true;
            temporaryCamera.orthographicSize = 5f;
            temporaryCamera.clearFlags = CameraClearFlags.SolidColor;
            temporaryCamera.backgroundColor =
                new Color(0.95f, 0.93f, 0.86f);

            var temporaryGameObject = new GameObject("ゲーム画面");
            temporaryGameObject.AddComponent<PuzzleSceneController>();

            EditorSceneManager.SaveScene(
                temporaryScene,
                GAME_SCENE_PATH);
        }

        static void EnsureStageSelectScene()
        {
            if (File.Exists(STAGE_SELECT_SCENE_PATH))
            {
                EditorSceneManager.OpenScene(
                    STAGE_SELECT_SCENE_PATH,
                    OpenSceneMode.Single);
                if (Object.FindAnyObjectByType<Camera>() != null &&
                    Object.FindAnyObjectByType<StageSelectController>() != null)
                {
                    return;
                }
            }

            var temporaryScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            temporaryScene.name = "StageSelect";

            var temporaryCameraObject = new GameObject(
                "Main Camera",
                typeof(Camera),
                typeof(AudioListener));
            temporaryCameraObject.tag = "MainCamera";
            temporaryCameraObject.transform.SetPositionAndRotation(
                new Vector3(0f, 0f, -10f),
                Quaternion.identity);
            var temporaryCamera =
                temporaryCameraObject.GetComponent<Camera>();
            temporaryCamera.orthographic = true;
            temporaryCamera.orthographicSize = 5f;
            temporaryCamera.clearFlags =
                CameraClearFlags.SolidColor;
            temporaryCamera.backgroundColor =
                new Color(0.95f, 0.93f, 0.86f);

            var temporaryStageSelectObject =
                new GameObject("ステージ選択画面");
            temporaryStageSelectObject
                .AddComponent<StageSelectController>();
            EditorSceneManager.SaveScene(
                temporaryScene,
                STAGE_SELECT_SCENE_PATH);
        }

        public static void EnsureTextMeshProResources()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(
                    "Assets/TextMesh Pro/Resources/TMP Settings.asset") != null)
            {
                return;
            }

            var temporaryPackageInfo =
                UnityEditor.PackageManager.PackageInfo.FindForAssetPath(
                    "Packages/com.unity.ugui");
            if (temporaryPackageInfo == null)
            {
                throw new BuildFailedException(
                    "TextMeshProを含むcom.unity.uguiが見つかりません。");
            }

            var temporaryPackagePath = Path.Combine(
                temporaryPackageInfo.resolvedPath,
                "Package Resources",
                "TMP Essential Resources.unitypackage");
            AssetDatabase.ImportPackage(temporaryPackagePath, false);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
