using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Demo;

namespace Zero.Editor
{
    public static class RecordsRoomBodyDrawersTool
    {
        private const string RoomName = "InfinityRoom3_RecordsRoom_Demo";
        private static readonly string[] Parts = { "LeftHand", "RightHand", "LeftArm", "RightArm", "LeftShoe", "RightShoe", "Torso", "LeftLeg", "RightLeg" };
        private static readonly int[] Sources = { 5, 6, 4, 7, 3, 8, 1, 0, 0 };
        private static readonly int[] Cabinets = { 15, 14, 13, 12, 11, 11, 10, 9, 8 };
        private static readonly int[] DrawerIndices = { 1, 2, 0, 2, 0, 2, 1, 3, 1 };
        private static readonly float[] Extensions = { 1.18f, 0.94f, 1.31f, 1.08f, 1.24f, 0.97f, 1.16f, 1.12f, 1.27f };

        [MenuItem("Tools/Zero/Records Room/Add Body Drawers Anomaly 13")]
        private static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != RoomName || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Transform room = Root(scene, RoomName);
            Transform parent = Require(room, "IR3_Anomalies");
            Transform source = Root(scene, "vintage_woman_3d_model");
            Transform observer = Require(Root(scene, "IR3_DemoPlayer"), "IR3_PreviewCamera");
            Transform[] drawers = Drawers(room);
            Transform existing = parent.Find("IR3_13_BodyDrawers");
            if (existing != null)
            {
                Rebind(existing.GetComponent<ArchiveBodyDrawersAnomaly>(), drawers, observer);
                EditorSceneManager.MarkSceneDirty(scene);
                return;
            }
            Prepare(source, out Mesh[] meshes, out Material[][] materials);
            GameObject node = RecordsRoomHorrorAssets.Node(parent, "IR3_13_BodyDrawers");
            var contents = new GameObject[Parts.Length];
            for (int i = 0; i < Parts.Length; i++) contents[i] = Content(drawers[i], meshes[i], materials[i], i);
            var anomaly = Undo.AddComponent<ArchiveBodyDrawersAnomaly>(node);
            Configure(anomaly, observer, drawers, contents);
            RecordsRoomHorrorTool.Data(anomaly, 13, "The Staff Filed In Drawers", "Right-hand drawers open one after another, revealing the archivist's body parts, excluding the head.");
            var preview = parent.GetComponent<LivingRoomAnomalyPreview>();
            var roster = new List<AnomalyBehaviour>(preview.Anomalies) { anomaly };
            roster.Sort((a, b) => string.CompareOrdinal(a.Data.Id, b.Data.Id));
            RecordsRoomHorrorTool.SetRoster(preview, roster);
            EditorUtility.SetDirty(anomaly);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = node;
            Debug.Log("IR3_BODY_DRAWERS_AUTHORED: nine parts, no head, F3 roster updated; save scene manually", node);
        }

        private static void Prepare(Transform source, out Mesh[] meshes, out Material[][] materials)
        {
            meshes = new Mesh[Parts.Length];
            materials = new Material[Parts.Length][];
            for (int i = 0; i < Parts.Length; i++)
            {
                var renderer = Require(source, "tripo_part_" + Sources[i]).GetComponent<SkinnedMeshRenderer>();
                if (renderer == null) throw new InvalidOperationException("IR3_BODY_SOURCE_MISSING: " + Parts[i]);
                meshes[i] = RecordsRoomBodyParts.Bake(renderer, Parts[i], i < 7 ? 0 : (i == 7 ? -1 : 1));
                materials[i] = renderer.sharedMaterials;
            }
        }

        private static Transform[] Drawers(Transform room)
        {
            var result = new Transform[Parts.Length];
            for (int i = 0; i < result.Length; i++)
            {
                int cabinet = Cabinets[i], drawer = DrawerIndices[i];
                string name = "IR3_Cabinet1 (" + cabinet + ")";
                result[i] = Require(room, "IR3_Furniture/" + name + "/IR3_Cabinet1_Drawer" + drawer);
                if (!result[i].gameObject.activeInHierarchy)
                    throw new InvalidOperationException("IR3_DRAWER_INACTIVE: " + name);
                Require(result[i], "DrawerTray/Bottom");
            }
            return result;
        }

        private static GameObject Content(Transform drawer, Mesh mesh, Material[] materials, int index)
        {
            var content = RecordsRoomHorrorAssets.Node(drawer, "IR3_13_Contents_" + Parts[index]);
            Renderer floor = Require(drawer, "DrawerTray/Bottom").GetComponent<Renderer>();
            RecordsRoomBodyParts.Place(content.transform, floor, mesh, materials, Rotation(index));
            content.SetActive(false);
            return content;
        }

        private static Quaternion Rotation(int index)
        {
            if (index == 7) return Quaternion.Euler(235f, 0f, 0f);
            if (index >= 6) return Quaternion.Euler(270f, 0f, 0f);
            return index < 4 ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
        }

        private static void Rebind(ArchiveBodyDrawersAnomaly anomaly, Transform[] drawers, Transform observer)
        {
            var settings = new SerializedObject(anomaly);
            var parts = new GameObject[Parts.Length];
            SerializedProperty references = settings.FindProperty("contents");
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = (GameObject)references.GetArrayElementAtIndex(i).objectReferenceValue;
                Undo.SetTransformParent(parts[i].transform, drawers[i], "Bind body parts to active drawer prefabs");
                Undo.RecordObject(parts[i].transform, "Center body part group inside drawer");
                parts[i].transform.localPosition = Vector3.zero;
                parts[i].transform.localRotation = Quaternion.identity;
                parts[i].transform.localScale = Vector3.one;
                Renderer renderer = parts[i].GetComponentInChildren<Renderer>(true);
                Undo.RecordObject(renderer.transform, "Fit body part inside active tray");
                RecordsRoomBodyParts.Fit(renderer, Require(drawers[i], "DrawerTray/Bottom").GetComponent<Renderer>(), Rotation(i));
                Undo.RecordObject(parts[i], "Hide normal drawer contents");
                parts[i].SetActive(false);
            }
            Undo.RecordObject(anomaly, "Bind active archive drawers");
            Configure(anomaly, observer, drawers, parts);
            EditorUtility.SetDirty(anomaly);
        }

        private static void Configure(ArchiveBodyDrawersAnomaly anomaly, Transform observer, Transform[] drawers, GameObject[] parts)
        {
            anomaly.Configure(observer, drawers, parts);
            var settings = new SerializedObject(anomaly);
            SerializedProperty extensions = settings.FindProperty("extensionMultipliers");
            extensions.arraySize = drawers.Length;
            for (int i = 0; i < drawers.Length; i++)
                extensions.GetArrayElementAtIndex(i).floatValue = Extensions[i];
            settings.ApplyModifiedProperties();
        }

        private static Transform Root(Scene scene, string name)
        {
            foreach (GameObject node in scene.GetRootGameObjects()) if (node.name == name) return node.transform;
            throw new InvalidOperationException("IR3_BODY_ROOT_MISSING: " + name);
        }

        private static Transform Require(Transform parent, string path)
        {
            Transform result = parent.Find(path);
            if (result == null) throw new InvalidOperationException("IR3_BODY_TARGET_MISSING: " + path);
            return result;
        }
    }
}
