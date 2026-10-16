using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Zero.Editor
{
    public static class WatchfulHallAuthoringTools
    {
        private const string DataPath = "Assets/Game/Scenes/Demos/WatchfulHall_NavMesh.asset";
        private const string ControllerPath = "Assets/Game/Animation/WatchfulHallPatrol.controller";
        private const string ModelFolder = "Assets/Game/ModelV2/ModelGameUpgrade/TallmanCreepy/";
        private const string Prefix = "Meshy_AI_The_Hollow_Parasite_biped_Animation_";

        [MenuItem("Tools/Zero/Watchful Hall/Create Patrol Controller")]
        private static void CreateController()
        {
            AnimationClip walk = LoadClip("Walking");
            AnimationClip run = LoadClip("Running");
            AnimationClip attack = LoadClip("Skill_01");
            if (walk == null || run == null || attack == null) return;
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath)
                ?? AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            machine.defaultState = AddState(machine, "Walk", walk);
            AddState(machine, "Run", run);
            AddState(machine, "Attack", attack);
            AssetDatabase.SaveAssetIfDirty(controller);
            Debug.Log("WATCHFUL_HALL_ANIMATION_CREATED: Walk/Run/Attack, no scene modified");
        }

        private static AnimationClip LoadClip(string action)
        {
            string path = ModelFolder + Prefix + action + "_withSkin.fbx";
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__")) return clip;
            Debug.LogError("WATCHFUL_HALL_ANIMATION_MISSING: " + path);
            return null;
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string name, AnimationClip clip)
        {
            foreach (ChildAnimatorState child in machine.states)
            {
                if (child.state.name != name) continue;
                child.state.motion = clip;
                EditorUtility.SetDirty(child.state);
                return child.state;
            }
            AnimatorState state = machine.AddState(name);
            state.motion = clip;
            return state;
        }

        [MenuItem("Tools/Zero/Watchful Hall/Bake Selected Navigation")]
        private static void Bake()
        {
            GameObject selected = Selection.activeGameObject;
            if (EditorApplication.isPlayingOrWillChangePlaymode || selected == null) return;
            if (selected.scene.name != "Minigame4_WatchfulHall_Demo" || !selected.TryGetComponent(out NavMeshSurface surface)) return;
            Undo.RecordObject(surface, "Bake Watchful Hall Navigation");
            surface.BuildNavMesh();
            if (surface.navMeshData == null) return;
            NavMeshData generated = surface.navMeshData;
            generated.name = "WatchfulHall_NavMesh";
            NavMeshData existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(DataPath);
            if (existing == null) AssetDatabase.CreateAsset(generated, DataPath);
            else ReplaceData(surface, generated, existing);
            EditorUtility.SetDirty(surface);
            EditorSceneManager.MarkSceneDirty(selected.scene);
            Debug.Log("WATCHFUL_HALL_NAVIGATION_BAKED: save scene manually", surface);
        }

        private static void ReplaceData(NavMeshSurface surface, NavMeshData generated, NavMeshData existing)
        {
            surface.RemoveData();
            Undo.RecordObject(existing, "Update Watchful Hall Navigation");
            EditorUtility.CopySerialized(generated, existing);
            surface.navMeshData = existing;
            surface.AddData();
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssetIfDirty(existing);
            Object.DestroyImmediate(generated);
        }
    }
}
