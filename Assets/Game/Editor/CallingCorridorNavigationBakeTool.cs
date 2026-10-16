using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Zero.Editor
{
    public static class CallingCorridorNavigationBakeTool
    {
        private const string DataPath = "Assets/Game/Scenes/Demos/CallingCorridor_NavMesh.asset";

        [MenuItem("Tools/Zero/Calling Corridor/Bake Selected Navigation")]
        private static void Bake()
        {
            GameObject selected = Selection.activeGameObject;
            if (EditorApplication.isPlayingOrWillChangePlaymode || selected == null) return;
            if (selected.scene.name != "Minigame3_CallingCorridor_Demo" ||
                !selected.TryGetComponent(out NavMeshSurface surface))
            {
                Debug.LogError("CALLING_CORRIDOR_BAKE_REJECTED: select the demo navigation surface", selected);
                return;
            }
            Undo.RecordObject(surface, "Bake Calling Corridor Navigation");
            surface.BuildNavMesh();
            if (surface.navMeshData == null) return;
            NavMeshData generated = surface.navMeshData;
            generated.name = "CallingCorridor_NavMesh";
            NavMeshData existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(DataPath);
            if (existing == null) AssetDatabase.CreateAsset(generated, DataPath);
            else ReplaceData(surface, generated, existing);
            EditorUtility.SetDirty(surface);
            EditorSceneManager.MarkSceneDirty(selected.scene);
            Debug.Log("CALLING_CORRIDOR_NAVIGATION_BAKED: save scene manually", surface);
        }

        private static void ReplaceData(NavMeshSurface surface, NavMeshData generated, NavMeshData existing)
        {
            surface.RemoveData();
            Undo.RecordObject(existing, "Update Calling Corridor Navigation");
            EditorUtility.CopySerialized(generated, existing);
            surface.navMeshData = existing;
            surface.AddData();
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssetIfDirty(existing);
            Object.DestroyImmediate(generated);
        }
    }
}
