using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Editor
{
    internal static class RecordsRoomFinalEncounterAssets
    {
        private static readonly string[] Parts = { "LeftHand", "RightHand", "LeftArm", "RightArm", "LeftShoe", "RightShoe", "Torso", "LeftLeg", "RightLeg", "Pelvis" };
        private static readonly int[] SourceParts = { 5, 6, 4, 7, 3, 8, 1, 0, 0, 0 };
        private static readonly HumanBodyBones[] TagBones = { HumanBodyBones.LeftHand, HumanBodyBones.RightHand,
            HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot,
            HumanBodyBones.Chest, HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg, HumanBodyBones.Hips };
        private static readonly Vector3[] ScatterPoints =
        {
            new Vector3(-2.3f, 0.9f, -3f), new Vector3(0.1f, 1.1f, -2.7f),
            new Vector3(2.7f, 1.2f, -2.7f), new Vector3(-2.2f, 1f, 0.5f),
            new Vector3(0.2f, 1f, 0.5f), new Vector3(2.8f, 0.9f, 0.8f),
            new Vector3(0.2f, 1.1f, -1.7f), new Vector3(-1.7f, 1.3f, 2.8f), new Vector3(2.3f, 1.1f, 2.9f),
            new Vector3(-2.4f, 1.1f, -1.7f)
        };

        internal static ArchiveAssemblyPresentation Body(Transform node, Transform source, ArchiveBodyDrawersAnomaly drawers)
        {
            Transform existing = node.Find("HeadlessEmployee");
            Animator actor = existing != null ? existing.GetComponent<Animator>() : CloneActor(node, source);
            Transform targets = Node(node, "AssemblyPoses");
            Transform parts = Node(node, "ScatteredParts");
            var sourceData = new SerializedObject(drawers);
            SerializedProperty contents = sourceData.FindProperty("contents");
            var pieceRefs = new Transform[Parts.Length];
            var poseRefs = new Transform[Parts.Length];
            var bodies = new Rigidbody[Parts.Length];
            for (int i = 0; i < Parts.Length; i++)
            {
                Transform pose = Node(targets, Parts[i]);
                SkinnedMeshRenderer part = source.Find("tripo_part_" + SourceParts[i]).GetComponent<SkinnedMeshRenderer>();
                int side = i == 7 ? -1 : i == 8 ? 1 : i == 9 ? 2 : 0;
                pose.SetPositionAndRotation(actor.transform.TransformPoint(Center(part, source, side)), actor.transform.rotation);
                pieceRefs[i] = SourcePiece(parts, contents, part, i);
                poseRefs[i] = pose;
                bodies[i] = pieceRefs[i].GetComponent<Rigidbody>();
                AttachTag(actor, pieceRefs[i], pose, i);
            }
            GameObject circle = node.Find("FloorSatanCircle") != null ? node.Find("FloorSatanCircle").gameObject : Circle(node);
            var view = node.GetComponent<ArchiveAssemblyPresentation>();
            if (view == null) view = Undo.AddComponent<ArchiveAssemblyPresentation>(node.gameObject);
            var data = new SerializedObject(view);
            Array(data, "pieces", pieceRefs);
            Array(data, "assemblyPoses", poseRefs);
            Array(data, "bodies", bodies);
            data.FindProperty("circle").objectReferenceValue = circle;
            data.FindProperty("actor").objectReferenceValue = actor;
            data.FindProperty("agent").objectReferenceValue = actor.GetComponent<NavMeshAgent>();
            data.FindProperty("focus").objectReferenceValue = actor.transform.Find("AttackFocus");
            data.ApplyModifiedProperties();
            return view;
        }

        private static Transform Node(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            return existing != null ? existing : RecordsRoomHorrorAssets.Node(parent, name).transform;
        }

        private static Transform SourcePiece(Transform parent, SerializedProperty contents, SkinnedMeshRenderer part, int index)
        {
            if (index == 9) return Piece(parent, RecordsRoomBodyParts.Bake(part, "Pelvis", 2), part.sharedMaterials, index);
            var original = (GameObject)contents.GetArrayElementAtIndex(index).objectReferenceValue;
            MeshFilter mesh = original.GetComponentInChildren<MeshFilter>(true);
            return Piece(parent, mesh.sharedMesh, mesh.GetComponent<Renderer>().sharedMaterials, index);
        }

        private static void AttachTag(Animator actor, Transform piece, Transform pose, int index)
        {
            Transform bone = actor.GetBoneTransform(TagBones[index]);
            if (bone == null) bone = actor.transform;
            string name = "IR3_18_Tag_" + index;
            if (bone.Find(name) != null) return;
            Transform source = piece.Find("ArchiveTag");
            GameObject tag = UnityEngine.Object.Instantiate(source.gameObject, bone);
            Undo.RegisterCreatedObjectUndo(tag, "Attach archive tag to animated body");
            tag.name = name;
            tag.transform.SetPositionAndRotation(pose.TransformPoint(source.localPosition), pose.rotation * source.localRotation);
        }

        private static Animator CloneActor(Transform parent, Transform source)
        {
            GameObject copy = UnityEngine.Object.Instantiate(source.gameObject, parent);
            Undo.RegisterCreatedObjectUndo(copy, "Create headless archive employee");
            copy.name = "HeadlessEmployee";
            copy.transform.SetPositionAndRotation(new Vector3(0f, 0.02f, 3.5f), Quaternion.Euler(0f, 180f, 0f));
            copy.transform.localScale = Vector3.one;
            copy.transform.Find("Head").gameObject.SetActive(false);
            foreach (Collider collider in copy.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            var actor = copy.GetComponent<Animator>();
            actor.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Game/Animation/RecordRoomWoman.controller");
            actor.applyRootMotion = false;
            actor.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var agent = copy.GetComponent<NavMeshAgent>();
            if (agent == null) agent = copy.AddComponent<NavMeshAgent>();
            agent.enabled = false;
            agent.radius = 0.28f;
            agent.height = 0.9f;
            agent.acceleration = 16f;
            agent.angularSpeed = 540f;
            agent.stoppingDistance = 0.65f;
            Transform focus = RecordsRoomHorrorAssets.Node(copy.transform, "AttackFocus").transform;
            focus.localPosition = new Vector3(0f, 0.75f, 0.2f);
            actor.enabled = false;
            copy.SetActive(false);
            return actor;
        }

        private static Transform Piece(Transform parent, Mesh mesh, Material[] materials, int index)
        {
            Transform existing = parent.Find("IR3_18_" + Parts[index]);
            if (existing != null) return existing;
            GameObject node = RecordsRoomHorrorAssets.Node(parent, "IR3_18_" + Parts[index]);
            node.AddComponent<MeshFilter>().sharedMesh = mesh;
            node.AddComponent<MeshRenderer>().sharedMaterials = materials;
            node.transform.SetPositionAndRotation(ScatterPoints[index], Quaternion.Euler(270f, index * 37f, 0f));
            var collider = node.AddComponent<BoxCollider>();
            collider.center = mesh.bounds.center;
            collider.size = mesh.bounds.size;
            var body = node.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.mass = 0.7f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            Label(node.transform, mesh.bounds, index);
            node.SetActive(false);
            return node.transform;
        }

        private static void Label(Transform part, Bounds bounds, int index)
        {
            Transform tag = RecordsRoomHorrorAssets.Node(part, "ArchiveTag").transform;
            tag.localPosition = new Vector3(0f, 0f, bounds.max.z + 0.008f);
            tag.localRotation = Quaternion.Euler(0f, 180f, index % 2 == 0 ? -8f : 11f);
            RecordsRoomMarkAssets.Quad(tag, "Paper", RecordsRoomHorrorAssets.Material("BodyTagPaper", new Color(0.63f, 0.57f, 0.43f)), new Vector2(0.11f, 0.06f));
            TextMeshPro text = RecordsRoomDossierAssets.Text(tag, "Record", "ARCHIVE\n" + (index + 1).ToString("00"), new Vector2(0.10f, 0.05f));
            text.fontSize = 0.18f;
            text.transform.localPosition = Vector3.back * 0.001f;
        }

        private static Vector3 Center(SkinnedMeshRenderer renderer, Transform source, int side)
        {
            var baked = new Mesh();
            renderer.BakeMesh(baked);
            try
            {
                Vector3[] vertices = baked.vertices;
                for (int i = 0; i < vertices.Length; i++) vertices[i] = source.InverseTransformPoint(renderer.transform.TransformPoint(vertices[i]));
                int[] triangles = baked.triangles;
                Bounds bounds = default;
                bool first = true;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 center = (vertices[triangles[i]] + vertices[triangles[i + 1]] + vertices[triangles[i + 2]]) / 3f;
                    if (side == 2 ? center.y <= 1.05f : side != 0 && (center.y > 1.05f || center.x * side < 0f)) continue;
                    for (int corner = 0; corner < 3; corner++)
                    {
                        Vector3 point = vertices[triangles[i + corner]];
                        if (first) { bounds = new Bounds(point, Vector3.zero); first = false; }
                        else bounds.Encapsulate(point);
                    }
                }
                return bounds.center;
            }
            finally { UnityEngine.Object.DestroyImmediate(baked); }
        }

        internal static GameObject Circle(Transform parent)
        {
            GameObject mark = RecordsRoomMarkAssets.Quad(parent, "FloorSatanCircle", RecordsRoomBloodMarkAssets.Material("SatanCircle"), Vector2.one * 2.1f);
            mark.transform.SetPositionAndRotation(new Vector3(0f, 0.014f, 3.5f), Quaternion.Euler(90f, 0f, 0f));
            mark.SetActive(false);
            return mark;
        }

        private static void Array(SerializedObject target, string name, UnityEngine.Object[] values)
        {
            SerializedProperty property = target.FindProperty(name);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
