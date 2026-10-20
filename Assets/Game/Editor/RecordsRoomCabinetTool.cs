using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Editor
{
    public static class RecordsRoomCabinetTool
    {
        private const string PrefabPath = "Assets/Game/Prefabs/MINIGAME/Deco/IR3_Cabinet1.prefab";
        private const float Wall = 0.018f;

        [MenuItem("Tools/Zero/Records Room/Upgrade Hollow Cabinet Prefab And Scene")]
        private static void Upgrade()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            GameObject asset = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                UpgradeCabinet(asset.transform);
                PrefabUtility.SaveAsPrefabAsset(asset, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(asset); }
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != "InfinityRoom3_RecordsRoom_Demo") return;
            foreach (GameObject root in scene.GetRootGameObjects()) UpgradeScene(root.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("RECORDS_CABINET_UPGRADED: hollow body, four complete trays; save scene manually");
        }

        private static void UpgradeScene(Transform root)
        {
            Transform furniture = root.Find("IR3_Furniture");
            if (furniture == null) return;
            foreach (Transform cabinet in furniture)
                if (cabinet.name.StartsWith("IR3_Cabinet", StringComparison.Ordinal)) UpgradeCabinet(cabinet);
            Transform anomaly = root.Find("IR3_Anomalies/IR3_04_AjarDrawer");
            Transform drawer = furniture.Find("IR3_Cabinet1/IR3_Cabinet1_Drawer3");
            if (anomaly == null || drawer == null) return;
            var movement = anomaly.GetComponent<LivingRoomArrangementAnomaly>();
            Undo.RecordObject(movement, "Migrate complete drawer anomaly");
            movement.Configure(new[] { drawer }, new[] { drawer.parent.InverseTransformVector(Vector3.right * 0.025f) },
                new Vector3[1], Array.Empty<GameObject>(), Array.Empty<GameObject>());
            EditorUtility.SetDirty(movement);
        }

        private static void UpgradeCabinet(Transform cabinet)
        {
            Transform body = cabinet.Find(cabinet.name + "_Body");
            if (body == null) return;
            Transform first = cabinet.Find(cabinet.name + "_Drawer0");
            if (first == null) return;
            float facing = Mathf.Sign(first.localPosition.x - body.localPosition.x);
            Housing(body, facing);
            for (int i = 0; i < 4; i++)
            {
                Transform drawer = cabinet.Find(cabinet.name + "_Drawer" + i);
                if (drawer == null) continue;
                Tray(body, drawer);
                Reparent(cabinet.Find(cabinet.name + "_Handle" + i), drawer);
                Reparent(cabinet.Find(cabinet.name + "_Label" + i), drawer);
            }
        }

        private static void Reparent(Transform part, Transform drawer)
        {
            if (part != null) Undo.SetTransformParent(part, drawer, "Group complete drawer");
        }

        private static Bounds BoundsOf(Transform part) => part.GetComponent<MeshFilter>().sharedMesh.bounds;

        private static void Housing(Transform body, float facing)
        {
            Bounds b = BoundsOf(body);
            Material material = body.GetComponent<MeshRenderer>().sharedMaterial;
            Transform shell = Node(body, "HollowHousing");
            Vector3 c = b.center, s = b.size;
            Panel(shell, "RearWall", c + Vector3.left * facing * (s.x - Wall) * 0.5f, new Vector3(Wall, s.y, s.z), material);
            Panel(shell, "Top", c + Vector3.up * (s.y - Wall) * 0.5f, new Vector3(s.x, Wall, s.z), material);
            Panel(shell, "Base", c + Vector3.down * (s.y - Wall) * 0.5f, new Vector3(s.x, Wall, s.z), material);
            Panel(shell, "LeftWall", c + Vector3.back * (s.z - Wall) * 0.5f, new Vector3(s.x, s.y, Wall), material);
            Panel(shell, "RightWall", c + Vector3.forward * (s.z - Wall) * 0.5f, new Vector3(s.x, s.y, Wall), material);
            Undo.RecordObject(body.GetComponent<MeshRenderer>(), "Disable solid cabinet body");
            body.GetComponent<MeshRenderer>().enabled = false;
            foreach (Collider collider in body.GetComponents<Collider>())
            {
                Undo.RecordObject(collider, "Remove solid cabinet collision");
                collider.enabled = false;
            }
        }

        private static void Tray(Transform body, Transform drawer)
        {
            Bounds b = BoundsOf(body), front = BoundsOf(drawer);
            float depth = b.size.x - Wall * 4f;
            float width = Mathf.Min(front.size.z, b.size.z - Wall * 3f);
            float height = front.size.y * 0.65f;
            float facing = Mathf.Sign(drawer.localPosition.x - body.localPosition.x);
            Material material = drawer.GetComponent<MeshRenderer>().sharedMaterial;
            Transform tray = Node(drawer, "DrawerTray");
            Vector3 c = front.center + Vector3.left * facing * (depth * 0.5f + front.extents.x);
            c.y = front.min.y + height * 0.5f + Wall;
            Panel(tray, "Bottom", c + Vector3.down * (height - Wall) * 0.5f,
                new Vector3(depth, Wall, width), material);
            Panel(tray, "Rear", c + Vector3.left * facing * (depth - Wall) * 0.5f,
                new Vector3(Wall, height, width), material);
            Panel(tray, "LeftSide", c + Vector3.back * (width - Wall) * 0.5f,
                new Vector3(depth, height, Wall), material);
            Panel(tray, "RightSide", c + Vector3.forward * (width - Wall) * 0.5f,
                new Vector3(depth, height, Wall), material);
            Rails(body, drawer, c, depth, width);
        }

        private static void Rails(Transform body, Transform drawer, Vector3 center, float depth, float width)
        {
            Transform rails = Node(body.Find("HollowHousing"), drawer.name + "_FixedRails");
            Material material = body.GetComponent<MeshRenderer>().sharedMaterial;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Vector3 local = center + new Vector3(0f, -0.035f, sign * (width + Wall) * 0.5f);
                Vector3 point = rails.InverseTransformPoint(drawer.TransformPoint(local));
                Panel(rails, sign < 0 ? "RailL" : "RailR", point, new Vector3(depth, 0.022f, 0.018f), material);
            }
        }

        private static Transform Node(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing;
            var node = new GameObject(name);
            node.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(node, "Build hollow cabinet");
            return node.transform;
        }

        private static void Panel(Transform parent, string name, Vector3 center, Vector3 size, Material material)
        {
            Transform existing = parent.Find(name);
            GameObject panel = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RecordObject(panel.transform, "Resize cabinet panel");
            panel.name = name;
            panel.transform.SetParent(parent, false);
            panel.transform.localPosition = center;
            panel.transform.localScale = size;
            panel.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (existing == null) Undo.RegisterCreatedObjectUndo(panel, "Build cabinet panel");
        }
    }
}
