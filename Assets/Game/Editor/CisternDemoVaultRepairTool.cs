using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Zero.Editor
{
    /// <summary>Explicit, repeatable authoring commands. Never runs on reload or saves a scene.</summary>
    public static class CisternDemoVaultRepairTool
    {
        private const string ScenePath = "Assets/Game/Scenes/Demos/Minigame5_FloodedCistern_Demo.unity";
        private const string MeshPath = "Assets/Game/Prefabs/CisternDemo/Cistern_QuarterVault.asset";
        private const string MaterialPath = "Assets/Game/Materials/CisternDemo/M_Cistern_Vault.mat";

        [MenuItem("Tools/Zero/Cistern Demo/Repair Closed Vaults And Piers")]
        [MenuItem("Tools/Zero/Cistern Demo/Create Modular Arch Master")]
        private static void CreateMaster()
        {
            Transform architecture = GetArchitecture();
            if (architecture == null) return;
            Material source = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Game/Materials/CisternDemo/M_Cistern_Porcelain.mat");
            if (source == null)
            {
                Debug.LogError("CISTERN_CONFIGURATION_INVALID: porcelain material missing", architecture);
                return;
            }
            Mesh mesh = StoreMesh(CisternQuarterVaultMesh.Create());
            Material material = GetVaultMaterial(source);
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create Cistern Modular Arch Master");
            Transform master = GetOrCreateChild(architecture, "Cistern_ArchQuarter_Master");
            ConfigureMaster(master.gameObject, mesh, material);
            GetOrCreateChild(architecture, "Cistern_ModularVaults");
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(architecture.gameObject.scene);
            Selection.activeGameObject = master.gameObject;
            Debug.Log($"CISTERN_ARCH_MASTER_READY: vertices={mesh.vertexCount}; scene unsaved", master);
        }

        private static Transform GetArchitecture()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
            {
                Debug.LogError("CISTERN_CONFIGURATION_INVALID: open the cistern demo in Edit Mode");
                return null;
            }
            foreach (GameObject candidate in scene.GetRootGameObjects())
            {
                if (candidate.name != "CisternDemo") continue;
                Transform architecture = candidate.transform.Find("Cistern_Architecture");
                if (architecture != null) return architecture;
            }
            Debug.LogError("CISTERN_CONFIGURATION_INVALID: architecture missing");
            return null;
        }

        private static Transform GetOrCreateChild(Transform parent, string childName)
        {
            Transform existing = parent.Find(childName);
            if (existing != null) return existing;
            GameObject child = new GameObject(childName);
            Undo.RegisterCreatedObjectUndo(child, "Create Cistern Authoring Object");
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void ConfigureMaster(GameObject target, Mesh mesh, Material material)
        {
            MeshFilter filter = target.GetComponent<MeshFilter>();
            if (filter == null) filter = Undo.AddComponent<MeshFilter>(target);
            MeshRenderer renderer = target.GetComponent<MeshRenderer>();
            if (renderer == null) renderer = Undo.AddComponent<MeshRenderer>(target);
            MeshCollider collider = target.GetComponent<MeshCollider>();
            if (collider == null) collider = Undo.AddComponent<MeshCollider>(target);
            Undo.RecordObjects(new Object[] { filter, renderer, collider }, "Assign Shared Vault Mesh");
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = material;
            collider.sharedMesh = null;
            collider.sharedMesh = mesh;
            collider.convex = false;
            GameObjectUtility.SetStaticEditorFlags(target,
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic);
        }

        private static Mesh StoreMesh(Mesh generated)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, MeshPath);
                return generated;
            }
            Undo.RecordObject(existing, "Update Cistern Quarter Vault Mesh");
            EditorUtility.CopySerialized(generated, existing);
            Object.DestroyImmediate(generated);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssetIfDirty(existing);
            return existing;
        }

        private static Material GetVaultMaterial(Material source)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null) return material;
            material = new Material(source) { name = "M_Cistern_Vault", enableInstancing = true };
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        [MenuItem("Tools/Zero/Cistern Demo/Finalize Modular Vault Replacement")]
        private static void FinalizeReplacement()
        {
            Transform architecture = GetArchitecture();
            if (architecture == null) return;
            Transform modules = architecture.Find("Cistern_ModularVaults");
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (!ValidateModules(modules, mesh)) return;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Replace Cistern Legacy Vaults And Piers");
            RemoveLegacyGeometry(architecture);
            Transform arches = architecture.parent.Find("Cistern_Arches");
            if (arches != null) Undo.DestroyObjectImmediate(arches.gameObject);
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(architecture.gameObject.scene);
            Debug.Log($"CISTERN_MODULAR_VAULT_VALIDATION_PASS: {modules.childCount} shared prefab quarters; scene unsaved", modules);
        }

        private static bool ValidateModules(Transform modules, Mesh mesh)
        {
            if (modules == null || mesh == null || mesh.vertexCount == 0)
            {
                Debug.LogError("CISTERN_CONFIGURATION_INVALID: modular vault mesh or instances missing", modules);
                return false;
            }
            MeshFilter[] filters = modules.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length == 0 || filters.Length % 4 != 0)
            {
                Debug.LogError($"CISTERN_CONFIGURATION_INVALID: quarters must form complete bays, found {filters.Length}", modules);
                return false;
            }
            foreach (MeshFilter filter in filters)
            {
                MeshCollider collider = filter.GetComponent<MeshCollider>();
                if (filter.sharedMesh == mesh && collider != null && collider.sharedMesh == mesh &&
                    PrefabUtility.IsPartOfPrefabInstance(filter)) continue;
                Debug.LogError("CISTERN_CONFIGURATION_INVALID: instance mesh, collider or prefab link invalid", filter);
                return false;
            }
            return true;
        }

        private static void RemoveLegacyGeometry(Transform architecture)
        {
            for (int i = architecture.childCount - 1; i >= 0; i--)
            {
                Transform child = architecture.GetChild(i);
                if (child.name.StartsWith("Pier_") || child.name.StartsWith("Roof_Bay_") ||
                    child.name == "Cistern_ClosedVaultCeiling" || child.name == "Cistern_ArchQuarter_Master")
                    Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        [MenuItem("Tools/Zero/Cistern Demo/Validate Modular Vault Traversal")]
        private static void ValidateTraversal()
        {
            Transform architecture = GetArchitecture();
            if (architecture == null) return;
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (!ValidateModules(architecture.Find("Cistern_ModularVaults"), mesh)) return;
            Physics.SyncTransforms();
            Transform arrival = architecture.parent.Find("Cistern_Markers/CisternArrivalAnchor");
            Transform firstStep = architecture.parent.Find("Cistern_Staircase/Exit_Step_01");
            if (arrival == null || firstStep == null)
            {
                Debug.LogError("CISTERN_CONFIGURATION_INVALID: arrival or first step missing", architecture);
                return;
            }
            Vector3 origin = arrival.position + architecture.up * 1.63f + architecture.forward;
            float distance = Vector3.Dot(firstStep.position - origin, architecture.forward) - 0.7f;
            if (Physics.SphereCast(origin, 0.25f, architecture.forward, out RaycastHit obstacle,
                distance, ~0, QueryTriggerInteraction.Ignore))
            {
                Debug.LogError($"CISTERN_TRAVERSAL_INVALID: central route blocked by {obstacle.collider.name}", architecture);
                return;
            }
            int covered = ValidateRoofCoverage(architecture, mesh);
            if (covered < 0) return;
            Debug.Log($"CISTERN_TRAVERSAL_VALIDATION_PASS: {covered} covered bays, {distance:F1}m central route clear", architecture);
        }

        private static int ValidateRoofCoverage(Transform architecture, Mesh mesh)
        {
            Transform floor = architecture.Find("Cistern_Floor");
            Collider shaftRoof = architecture.Find("Exit_RaisedRoof")?.GetComponent<Collider>();
            if (floor == null || shaftRoof == null) return -1;
            Bounds bounds = floor.GetComponent<MeshFilter>().sharedMesh.bounds;
            Vector3 minimum = floor.localPosition + Vector3.Scale(bounds.min, floor.localScale);
            Vector3 maximum = floor.localPosition + Vector3.Scale(bounds.max, floor.localScale);
            int covered = 0;
            for (float x = minimum.x + 3.4f; x < maximum.x - 0.4f; x += 6f)
            {
                for (float z = minimum.z + 6.4f; z < maximum.z - 3.4f; z += 6f)
                {
                    Vector3 point = architecture.TransformPoint(new Vector3(x + 0.01f, 1f, z + 0.01f));
                    Bounds shaft = shaftRoof.bounds;
                    if (point.x >= shaft.min.x && point.x <= shaft.max.x &&
                        point.z >= shaft.min.z && point.z <= shaft.max.z) continue;
                    if (Physics.Raycast(point, architecture.up, out RaycastHit roof, 3f,
                        ~0, QueryTriggerInteraction.Ignore) &&
                        roof.collider.GetComponent<MeshFilter>()?.sharedMesh == mesh)
                    {
                        covered++;
                        continue;
                    }
                    Debug.LogError($"CISTERN_TRAVERSAL_INVALID: ceiling missing over bay {x},{z}", architecture);
                    return -1;
                }
            }
            return covered;
        }
    }
}
