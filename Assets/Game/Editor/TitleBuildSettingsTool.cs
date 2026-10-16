using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Zero.Editor
{
    public static class TitleBuildSettingsTool
    {
        private const string TitlePath = "Assets/Game/Scenes/TitleScene.unity";

        [MenuItem("Tools/Zero/Title/Set Title As Startup Scene")]
        private static void SetTitleAsStartupScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TitlePath) == null)
            {
                Debug.LogError($"TITLE_BUILD_CONFIGURATION_INVALID: missing={TitlePath}");
                return;
            }

            var scenes = new List<EditorBuildSettingsScene>();
            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i].path != TitlePath) scenes.Add(existing[i]);
            }

            scenes.Insert(0, new EditorBuildSettingsScene(TitlePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"TITLE_BUILD_SCENE_READY: index=0, preserved={existing.Length}");
        }
    }
}
