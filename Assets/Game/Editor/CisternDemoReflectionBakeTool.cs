using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Zero.Editor
{
    public static class CisternDemoReflectionBakeTool
    {
        private const string ScenePath = "Assets/Game/Scenes/Demos/Minigame5_FloodedCistern_Demo.unity";
        private const string TexturePath = "Assets/Game/Materials/CisternDemo/Cistern_Reflection.exr";

        [MenuItem("Tools/Zero/Cistern Demo/Bake Selected Reflection Probe")]
        private static void BakeSelectedProbe()
        {
            GameObject selected = Selection.activeGameObject;
            if (EditorApplication.isPlayingOrWillChangePlaymode || selected == null ||
                selected.scene.path != ScenePath || !selected.TryGetComponent(out ReflectionProbe probe))
            {
                Debug.LogError("CISTERN_REFLECTION_CONFIGURATION_INVALID: select the demo reflection probe", selected);
                return;
            }

            Undo.RecordObject(probe, "Bake Cistern Demo Reflection");
            probe.mode = ReflectionProbeMode.Baked;
            if (!Lightmapping.BakeReflectionProbe(probe, TexturePath))
            {
                Debug.LogError("CISTERN_REFLECTION_BAKE_FAILED", probe);
                return;
            }

            AssetDatabase.ImportAsset(TexturePath);
            // Baked texture ownership normally lives in LightingDataAsset. The demo
            // deliberately has no global light bake, so serialize the cube explicitly.
            probe.customBakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(TexturePath);
            probe.mode = ReflectionProbeMode.Custom;
            EditorUtility.SetDirty(probe);
            EditorSceneManager.MarkSceneDirty(selected.scene);
            Debug.Log("CISTERN_REFLECTION_BAKED: scene remains unsaved", probe);
        }
    }
}
