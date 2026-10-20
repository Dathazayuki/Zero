using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Demo;

namespace Zero.Editor
{
    public static class RecordsRoomDossierAnomalyTool
    {
        private const string SceneName = "InfinityRoom3_RecordsRoom_Demo";
        private const string Files = "IR3_Furniture/IR3_DetailedFiles/";

        [MenuItem("Tools/Zero/Records Room/Add Dossier Anomalies 09 10 11")]
        private static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != SceneName || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Transform room = SceneRoot(scene, SceneName);
            Transform root = Require(room, "IR3_Anomalies");
            Transform camera = Require(SceneRoot(scene, "IR3_DemoPlayer"), "IR3_PreviewCamera");
            SkinnedMeshRenderer head = Require(SceneRoot(scene, "vintage_woman_3d_model"), "Head").GetComponent<SkinnedMeshRenderer>();
            Require(room, Files + "IR3_File_A_L3_S2_B2");
            RecordsRoomDossierAssets.Portrait(false);
            RecordsRoomDossierAssets.Portrait(true);
            RecordsRoomBloodMarkAssets.Material("Smile");
            var preview = root.GetComponent<LivingRoomAnomalyPreview>();
            var entries = new List<AnomalyBehaviour>(preview.Anomalies);
            Add(entries, ClosedFiles(room, root));
            Add(entries, Face(room, root, head, camera));
            Add(entries, Portrait(room, root, camera.GetComponent<Camera>()));
            entries.Sort((a, b) => string.CompareOrdinal(a.Data.Id, b.Data.Id));
            RecordsRoomHorrorTool.SetRoster(preview, entries);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("IR3_DOSSIER_AUTHORED: IR3-09/10/11 wired to F3; save scene manually", root);
        }

        private static Transform SceneRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root.transform;
            throw new InvalidOperationException("IR3_ROOT_MISSING: " + name);
        }

        private static Transform Require(Transform root, string path)
        {
            Transform result = root.Find(path);
            if (result == null) throw new InvalidOperationException("IR3_DOSSIER_TARGET_MISSING: " + path);
            return result;
        }

        private static void Add(List<AnomalyBehaviour> entries, AnomalyBehaviour entry)
        {
            if (!entries.Contains(entry)) entries.Add(entry);
        }

        private static AnomalyBehaviour ClosedFiles(Transform room, Transform root)
        {
            Transform existing = root.Find("IR3_09_ClosedDossiers");
            Transform node = existing != null ? existing : RecordsRoomHorrorAssets.Node(root, "IR3_09_ClosedDossiers").transform;
            return RecordsRoomRevisedAnomaliesTool.ConfigureSmiles(node,
                Require(room, "IR3_Furniture/IR3_DetailedFiles"), RecordsRoomBloodMarkAssets.Material("Smile"));
        }

        private static AnomalyBehaviour Face(Transform room, Transform root, SkinnedMeshRenderer source, Transform camera)
        {
            Transform existing = root.Find("IR3_10_FaceBetweenFiles");
            if (existing != null) return existing.GetComponent<AnomalyBehaviour>();
            GameObject node = RecordsRoomHorrorAssets.Node(root, "IR3_10_FaceBetweenFiles");
            GameObject gap = RecordsRoomHorrorAssets.Node(node.transform, "GapFiles");
            GameObject[] normal = GapFiles(room, gap.transform);
            Bounds bounds = normal[2].GetComponent<Renderer>().bounds;
            Transform head = RecordsRoomHorrorAssets.Node(node.transform, "Head_FromSceneModel").transform;
            head.gameObject.AddComponent<MeshFilter>().sharedMesh = RecordsRoomHorrorAssets.HeadMesh(source);
            head.gameObject.AddComponent<MeshRenderer>().sharedMaterials = source.sharedMaterials;
            head.SetPositionAndRotation(new Vector3(bounds.min.x + 0.09f, bounds.center.y, bounds.center.z), Quaternion.Euler(0f, 270f, 0f));
            head.localScale = new Vector3(0.85f, 0.7f, 0.65f);
            Recess(gap.transform, bounds);
            head.gameObject.SetActive(false);
            gap.SetActive(false);
            var anomaly = node.AddComponent<FamilyPeekAnomaly>();
            anomaly.Configure(head, camera, Vector3.right * 0.4f);
            anomaly.ConfigureVisibility(new[] { gap }, normal);
            var settings = new SerializedObject(anomaly);
            settings.FindProperty("faceObserver").boolValue = true;
            settings.FindProperty("approachDistance").floatValue = 1.35f;
            settings.FindProperty("retreatSeconds").floatValue = 0.8f;
            settings.ApplyModifiedProperties();
            RecordsRoomHorrorTool.Data(anomaly, 10, "A Face Between The Files", "An existing model's face watches between files, then withdraws as the Player approaches.");
            return anomaly;
        }

        private static GameObject[] GapFiles(Transform room, Transform parent)
        {
            var normal = new GameObject[5];
            for (int i = 0; i < normal.Length; i++)
            {
                normal[i] = Require(room, Files + "IR3_File_A_L3_S2_B" + i).gameObject;
                if (i == 2) continue;
                GameObject copy = UnityEngine.Object.Instantiate(normal[i], parent);
                Undo.RegisterCreatedObjectUndo(copy, "Open a physical gap between archive files");
                copy.name = normal[i].name + "_GapVariant";
                copy.transform.SetPositionAndRotation(normal[i].transform.position + Vector3.forward * (i < 2 ? -0.105f : 0.105f), normal[i].transform.rotation);
                copy.transform.localScale = normal[i].transform.lossyScale;
            }
            return normal;
        }

        private static void Recess(Transform parent, Bounds bounds)
        {
            Material black = RecordsRoomHorrorAssets.Material("FileRecess", new Color(0.012f, 0.009f, 0.008f));
            GameObject backing = RecordsRoomMarkAssets.Quad(parent, "DarkRecess", black, new Vector2(0.35f, 0.34f));
            backing.transform.SetPositionAndRotation(new Vector3(bounds.center.x - 0.01f, bounds.center.y, bounds.center.z), Quaternion.Euler(0f, 90f, 0f));
            Material skin = RecordsRoomHorrorAssets.Material("FingertipResidue", new Color(0.49f, 0.35f, 0.29f));
            for (int i = 0; i < 3; i++)
            {
                GameObject finger = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                finger.name = "RemainingFingertip_" + i;
                finger.transform.SetParent(parent, false);
                finger.transform.position = new Vector3(bounds.min.x - 0.005f, bounds.min.y + 0.02f + i * 0.012f, bounds.center.z - 0.14f + i * 0.026f);
                finger.transform.localScale = new Vector3(0.023f, 0.035f, 0.024f);
                finger.GetComponent<Renderer>().sharedMaterial = skin;
                UnityEngine.Object.DestroyImmediate(finger.GetComponent<Collider>());
                Undo.RegisterCreatedObjectUndo(finger, "Add fingers left in the archive gap");
            }
        }

        private static AnomalyBehaviour Portrait(Transform room, Transform root, Camera camera)
        {
            Transform existing = root.Find("IR3_11_FormingPortrait");
            if (existing != null) return existing.GetComponent<AnomalyBehaviour>();
            Transform original = Require(room, "IR3_Decor/IR3_Noticeboard/IR3_Notice_0 (3)");
            GameObject node = RecordsRoomHorrorAssets.Node(root, "IR3_11_FormingPortrait");
            node.transform.position = original.position + Vector3.back * 0.008f;
            GameObject paper = RecordsRoomMarkAssets.Quad(node.transform, "PersonnelDossier", RecordsRoomHorrorAssets.Material("PersonnelPaper", new Color(0.72f, 0.67f, 0.54f)), new Vector2(0.30f, 0.36f));
            TextMeshPro header = RecordsRoomDossierAssets.Text(node.transform, "Department", "ARCHIVE / PERSONNEL", new Vector2(0.27f, 0.04f));
            header.fontSize = 0.23f;
            header.transform.localPosition = new Vector3(0f, 0.144f, -0.002f);
            TextMeshPro record = RecordsRoomDossierAssets.Text(node.transform, "Record", "STAFF ID 047\nRECORD DEPARTMENT", new Vector2(0.27f, 0.065f));
            record.fontSize = 0.19f;
            record.transform.localPosition = new Vector3(0f, -0.13f, -0.002f);
            GameObject photo = RecordsRoomMarkAssets.Quad(node.transform, "EmployeePhoto", RecordsRoomDossierAssets.PortraitMaterial(), new Vector2(0.14f, 0.187f));
            photo.transform.localPosition = new Vector3(0f, 0.01f, -0.003f);
            var anomaly = node.AddComponent<ApproachingPaintingAnomaly>();
            anomaly.Configure(photo.GetComponent<Renderer>(), camera, new[] { RecordsRoomDossierAssets.BlankPhoto(), RecordsRoomDossierAssets.Portrait(false), RecordsRoomDossierAssets.Portrait(true) });
            RecordsRoomHorrorTool.Data(anomaly, 11, "The Employee Photo Develops", "The blank employee portrait develops closed eyes, then open eyes after looking away and back.");
            Undo.RecordObject(original.gameObject, "Replace notice with permanent employee dossier");
            original.gameObject.SetActive(false);
            return anomaly;
        }
    }
}
