using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DigiPurity.UI;
using DigiPurity.World;

namespace DigiPurity.UI.Editor
{
    public static class TitleScreenEditorTools
    {
        [MenuItem("DIGIPURITY/Setup Title Screen in Active Scene", false, 10)]
        public static void SetupTitleScreenInActiveScene()
        {
            var controller = Object.FindAnyObjectByType<TitleScreenController>();
            if (controller == null)
            {
                var go = new GameObject("TitleScreenController");
                controller = go.AddComponent<TitleScreenController>();
                Undo.RegisterCreatedObjectUndo(go, "Create TitleScreenController");
            }

            // Ensure ScreenFader is present
            var fader = Object.FindAnyObjectByType<ScreenFader>();
            if (fader == null)
            {
                var faderGo = new GameObject("ScreenFader");
                faderGo.AddComponent<ScreenFader>();
                Undo.RegisterCreatedObjectUndo(faderGo, "Create ScreenFader");
            }

            // Ensure camera has black background for Undertale void aesthetic
            var cam = Camera.main;
            if (cam != null)
            {
                Undo.RecordObject(cam, "Set Camera Solid Black");
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }

            Selection.activeGameObject = controller.gameObject;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[DIGIPURITY] TitleScreenController configured in active scene.");
        }

        [MenuItem("DIGIPURITY/Build & Register Title Scene (Scene 0)", false, 11)]
        public static void BuildAndRegisterTitleScene()
        {
            string titleScenePath = "Assets/Scenes/TitleScene.unity";
            string sampleScenePath = "Assets/Scenes/SampleScene.unity";
            string testScenePath = "Assets/Scenes/TestScene.unity";

            // 1. Create TitleScene
            var titleScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Add clean camera with pure black solid color
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            cam.orthographicSize = 9.0f;
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            // Add TitleScreenController
            var titleGo = new GameObject("TitleScreenController");
            var controller = titleGo.AddComponent<TitleScreenController>();

            // Add ScreenFader
            var faderGo = new GameObject("ScreenFader");
            faderGo.AddComponent<ScreenFader>();

            // Save the scene
            EditorSceneManager.SaveScene(titleScene, titleScenePath);
            Debug.Log($"[DIGIPURITY] Created and saved {titleScenePath}.");

            // 2. Configure EditorBuildSettings with TitleScene as Scene 0
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();

            // Scene 0: TitleScene
            buildScenes.Add(new EditorBuildSettingsScene(titleScenePath, true));

            // Scene 1: SampleScene (First gameplay scene)
            if (System.IO.File.Exists(sampleScenePath))
            {
                buildScenes.Add(new EditorBuildSettingsScene(sampleScenePath, true));
            }

            // Scene 2: TestScene
            if (System.IO.File.Exists(testScenePath))
            {
                buildScenes.Add(new EditorBuildSettingsScene(testScenePath, true));
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log($"[DIGIPURITY] Build Settings updated: Scene 0 is '{titleScenePath}', loading '{sampleScenePath}' on Play.");
        }

        [MenuItem("DIGIPURITY/Toggle Auto Title Screen on Play", false, 12)]
        public static void ToggleAutoBootstrap()
        {
            int current = PlayerPrefs.GetInt("DIGIPURITY_DISABLE_TITLE_AUTOBOOT", 0);
            int next = (current == 0) ? 1 : 0;
            PlayerPrefs.SetInt("DIGIPURITY_DISABLE_TITLE_AUTOBOOT", next);
            PlayerPrefs.Save();
            Debug.Log($"[DIGIPURITY] Auto Title Screen on Play is now: {(next == 0 ? "ENABLED" : "DISABLED")}");
        }
    }
}
