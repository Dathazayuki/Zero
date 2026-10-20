using UnityEditor;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Editor
{
    internal static class RecordsRoomHorrorAssets
    {
        internal const string Folder = "Assets/Game/Prefabs/InfinityRoom3Demo/Anomalies";
        internal const string BoxPath = Folder + "/PF_IR3_HollowArchiveBox.prefab";

        internal static GameObject Node(Transform parent, string name)
        {
            var node = new GameObject(name);
            node.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(node, "Author Records Room horror");
            return node;
        }

        internal static Material Material(string name, Color color)
        {
            string path = Folder + "/M_IR3_" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.13f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        internal static GameObject Panel(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            GameObject node = GameObject.CreatePrimitive(PrimitiveType.Cube);
            node.name = name;
            node.transform.SetParent(parent, false);
            node.transform.localPosition = position;
            node.transform.localScale = size;
            node.GetComponent<Renderer>().sharedMaterial = material;
            Undo.RegisterCreatedObjectUndo(node, "Create archive box panel");
            return node;
        }

        internal static GameObject BoxPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoxPath);
            if (prefab != null) return prefab;
            GameObject root = Node(null, "PF_IR3_HollowArchiveBox");
            Material board = Material("ArchiveCardboard", new Color(0.30f, 0.22f, 0.12f));
            Material edge = Material("BoxReinforcement", new Color(0.22f, 0.15f, 0.08f));
            Panel(root.transform, "Bottom", new Vector3(0f, 0.012f, 0f), new Vector3(0.66f, 0.024f, 0.72f), board);
            Panel(root.transform, "WallLeft", new Vector3(-0.318f, 0.2f, 0f), new Vector3(0.024f, 0.38f, 0.72f), board);
            Panel(root.transform, "WallRight", new Vector3(0.318f, 0.2f, 0f), new Vector3(0.024f, 0.38f, 0.72f), board);
            Panel(root.transform, "WallBack", new Vector3(0f, 0.2f, 0.348f), new Vector3(0.612f, 0.38f, 0.024f), board);
            Front(root.transform, board, edge);
            Transform hinge = Node(root.transform, "LidHinge").transform;
            hinge.localPosition = new Vector3(0f, 0.399f, 0.36f);
            Panel(hinge, "Lid", new Vector3(0f, 0f, -0.36f), new Vector3(0.69f, 0.025f, 0.75f), board);
            Panel(hinge, "LidLip", new Vector3(0f, -0.02f, -0.725f), new Vector3(0.69f, 0.04f, 0.02f), edge);
            Panel(hinge, "Tape", new Vector3(0f, 0.014f, -0.36f), new Vector3(0.085f, 0.002f, 0.73f), edge);
            try { return PrefabUtility.SaveAsPrefabAsset(root, BoxPath); }
            finally { Object.DestroyImmediate(root); }
        }

        private static void Front(Transform root, Material board, Material edge)
        {
            // The hand slot is a genuine opening between four panels.
            Panel(root, "FrontLower", new Vector3(0f, 0.135f, -0.348f), new Vector3(0.612f, 0.25f, 0.024f), board);
            Panel(root, "FrontUpper", new Vector3(0f, 0.37f, -0.348f), new Vector3(0.612f, 0.04f, 0.024f), board);
            Panel(root, "SlotLeft", new Vector3(-0.218f, 0.305f, -0.348f), new Vector3(0.176f, 0.09f, 0.024f), board);
            Panel(root, "SlotRight", new Vector3(0.218f, 0.305f, -0.348f), new Vector3(0.176f, 0.09f, 0.024f), board);
            Material paper = Material("BoxLabel", new Color(0.66f, 0.61f, 0.46f));
            Panel(root, "ArchiveLabel", new Vector3(0f, 0.13f, -0.362f), new Vector3(0.24f, 0.12f, 0.002f), paper);
            for (int i = 0; i < 4; i++)
                Panel(root, "LabelLine" + i, new Vector3(-0.015f, 0.16f - i * 0.022f, -0.364f), new Vector3(0.16f, 0.004f, 0.001f), edge);
        }

        internal static ArchiveHeadLegs Creature(Transform parent, SkinnedMeshRenderer source)
        {
            Transform root = Node(parent, "HeadCreature").transform;
            var head = Node(root, "Head_FromSceneModel");
            var filter = head.AddComponent<MeshFilter>();
            filter.sharedMesh = HeadMesh(source);
            head.AddComponent<MeshRenderer>().sharedMaterials = source.sharedMaterials;
            Material leg = Material("HeadLegChitin", new Color(0.12f, 0.08f, 0.065f));
            var hips = new Transform[6];
            for (int i = 0; i < hips.Length; i++) hips[i] = Leg(root, i, leg);
            var rig = root.gameObject.AddComponent<ArchiveHeadLegs>();
            rig.Configure(hips);
            rig.Present(0f, 0f);
            root.gameObject.SetActive(false);
            return rig;
        }

        internal static Mesh HeadMesh(SkinnedMeshRenderer source)
        {
            string path = Folder + "/IR3_Head_FromScene.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            var mesh = new Mesh { name = "IR3_Head_FromScene" };
            source.BakeMesh(mesh);
            Vector3[] vertices = mesh.vertices;
            Vector3 center = source.bounds.center;
            Quaternion frame = Quaternion.Inverse(source.transform.root.rotation);
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = frame * (source.transform.TransformPoint(vertices[i]) - center);
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Transform Leg(Transform root, int index, Material material)
        {
            float side = index < 3 ? -1f : 1f;
            int row = index % 3;
            Transform hip = Node(root, "Leg_" + index).transform;
            hip.localPosition = new Vector3(side * 0.1f, -0.035f, (row - 1) * 0.12f);
            Vector3 knee = new Vector3(side * 0.34f, 0.16f, (row - 1) * 0.17f);
            Vector3 foot = new Vector3(side * 0.51f, -0.23f, (row - 1) * 0.29f);
            Segment(hip, "Upper", Vector3.zero, knee, material);
            Segment(hip, "Lower", knee, foot, material);
            return hip;
        }

        internal static void RefineLegs(Transform creature)
        {
            Material material = Material("HeadLegChitin", new Color(0.12f, 0.08f, 0.065f));
            for (int i = 0; i < 6; i++)
            {
                Transform hip = creature.Find("Leg_" + i);
                if (hip == null || hip.Find("KneeJoint") != null) continue;
                float side = i < 3 ? -1f : 1f;
                int row = i % 3;
                Vector3 knee = new Vector3(side * 0.34f, 0.16f, (row - 1) * 0.17f);
                Vector3 foot = new Vector3(side * 0.51f, -0.23f, (row - 1) * 0.29f);
                Joint(hip, "KneeJoint", knee, material);
                Joint(hip, "HipJoint", Vector3.zero, material);
                Segment(hip, "Claw", foot, foot + new Vector3(side * 0.06f, 0f, 0.06f), material);
            }
        }

        private static void Joint(Transform parent, string name, Vector3 point, Material material)
        {
            GameObject node = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            node.name = name;
            node.transform.SetParent(parent, false);
            node.transform.localPosition = point;
            node.transform.localScale = Vector3.one * 0.055f;
            node.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(node.GetComponent<Collider>());
            Undo.RegisterCreatedObjectUndo(node, "Add six-legged head joints");
        }

        private static void Segment(Transform parent, string name, Vector3 from, Vector3 to, Material material)
        {
            GameObject node = Node(parent, name);
            node.transform.localPosition = from;
            node.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (to - from).normalized);
            node.transform.localScale = new Vector3(1f, (to - from).magnitude, 1f);
            node.AddComponent<MeshFilter>().sharedMesh = LimbMesh();
            node.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Mesh LimbMesh()
        {
            string path = Folder + "/IR3_HeadLegSegment.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            const int sides = 12;
            var vertices = new Vector3[sides * 2];
            var triangles = new int[sides * 6];
            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i] = radial * 0.025f;
                vertices[i + sides] = Vector3.up + radial * 0.009f;
                int next = (i + 1) % sides, t = i * 6;
                triangles[t] = i; triangles[t + 1] = i + sides; triangles[t + 2] = next;
                triangles[t + 3] = next; triangles[t + 4] = i + sides; triangles[t + 5] = next + sides;
            }
            var mesh = new Mesh { name = "TaperedLeg", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
    }
}
