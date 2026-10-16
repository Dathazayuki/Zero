using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Zero.Editor
{
    public static class CisternNavigationBakeTool
    {
        private const string DataPath = "Assets/Game/Scenes/Demos/Cistern_Chase_NavMesh.asset";

        [MenuItem("Tools/Zero/Cistern Demo/Bake Selected Navigation Surface")]
        private static void BakeSelectedSurface()
        {
            GameObject selected = Selection.activeGameObject;
            if (EditorApplication.isPlayingOrWillChangePlaymode || selected == null ||
                !selected.TryGetComponent(out NavMeshSurface surface) ||
                selected.scene.name != "Minigame5_FloodedCistern_Demo")
            {
                Debug.LogError("CISTERN_NAVIGATION_BAKE_REJECTED: select the demo NavMeshSurface", selected);
                return;
            }
            Undo.RecordObject(surface, "Bake Cistern Navigation");
            var previous = surface.navMeshData;
            surface.BuildNavMesh();
            if (surface.navMeshData == null)
            {
                Debug.LogError("CISTERN_NAVIGATION_BAKE_FAILED", surface);
                return;
            }
            var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(DataPath);
            if (existing == null) AssetDatabase.CreateAsset(surface.navMeshData, DataPath);
            else UpdateData(surface, existing);
            if (previous != null && !EditorUtility.IsPersistent(previous)) Object.DestroyImmediate(previous);
            EditorUtility.SetDirty(surface);
            EditorSceneManager.MarkSceneDirty(selected.scene);
            Debug.Log("CISTERN_NAVIGATION_BAKED: scene remains unsaved", surface);
        }

        private static void UpdateData(NavMeshSurface surface, UnityEngine.AI.NavMeshData existing)
        {
            var generated = surface.navMeshData;
            surface.RemoveData();
            Undo.RecordObject(existing, "Update Cistern Navigation Data");
            EditorUtility.CopySerialized(generated, existing);
            surface.navMeshData = existing;
            surface.AddData();
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssetIfDirty(existing);
            Object.DestroyImmediate(generated);
        }
    }
}
