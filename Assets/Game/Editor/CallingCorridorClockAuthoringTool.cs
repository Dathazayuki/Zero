using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Editor
{
    public static class CallingCorridorClockAuthoringTool
    {
        private const string Folder = "Assets/Game/Prefabs/CallingCorridorDemo/";

        [MenuItem("Tools/Zero/Calling Corridor/Upgrade Shared Clock Hands")]
        private static void Upgrade()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string path = Folder + "PF_CC_PuzzleClock.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                CallingCorridorClock clock = root.GetComponent<CallingCorridorClock>();
                var serialized = new SerializedObject(clock);
                Transform hour = (Transform)serialized.FindProperty("hourPivot").objectReferenceValue;
                Transform minute = hour.parent.Find("MinutePivot");
                if (minute == null) minute = new GameObject("MinutePivot").transform;
                minute.SetParent(hour.parent, false);
                GameObject hourHand = hour.Find("HourHand").gameObject;
                ConfigureShaft(hourHand.transform, 0.10f, 0.20f, 0.035f, -0.13f);
                Transform minuteHand = minute.Find("MinuteHand");
                if (minuteHand == null)
                {
                    minuteHand = Object.Instantiate(hourHand, minute).transform;
                    minuteHand.name = "MinuteHand";
                }
                ConfigureShaft(minuteHand, 0.12f, 0.24f, 0.023f, -0.155f);
                Material ink = hourHand.GetComponent<MeshRenderer>().sharedMaterial;
                AddMarker(hour, "HourSolidCircle", CircleMesh("ClockHourSymbol", 0f), ink, 0.235f);
                AddMarker(minute, "MinuteHollowCircle", CircleMesh("ClockMinuteSymbol", 0.66f), ink, 0.285f);
                serialized.FindProperty("minutePivot").objectReferenceValue = minute;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                clock.SetHands(CallingCorridorRules.ClockHandDigit(0, 0, CallingCorridorRules.Hand.Hour),
                    CallingCorridorRules.ClockHandDigit(0, 0, CallingCorridorRules.Hand.Minute));
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("CALLING_CORRIDOR_CLOCK_UPGRADED: shared prefab has two hands and solid/hollow markers; scene not saved");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ConfigureShaft(Transform shaft, float y, float length, float width, float z)
        {
            shaft.localPosition = new Vector3(0f, y, z);
            shaft.localRotation = Quaternion.identity;
            shaft.localScale = new Vector3(width, length, 0.018f);
        }

        private static void AddMarker(Transform pivot, string name, Mesh mesh, Material ink, float y)
        {
            Transform marker = pivot.Find(name);
            if (marker == null)
            {
                marker = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)).transform;
                marker.SetParent(pivot, false);
            }
            marker.localPosition = new Vector3(0f, y, -0.17f);
            marker.localRotation = Quaternion.identity;
            marker.localScale = Vector3.one * 0.038f;
            marker.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = marker.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = ink;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static Mesh CircleMesh(string name, float innerRadius)
        {
            string path = Folder + name + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            const int segments = 64;
            var vertices = new Vector3[segments * 2];
            var triangles = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                vertices[i * 2] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                vertices[i * 2 + 1] = vertices[i * 2] * innerRadius;
                int next = (i + 1) % segments * 2;
                int offset = i * 6;
                triangles[offset] = i * 2;
                triangles[offset + 1] = i * 2 + 1;
                triangles[offset + 2] = next;
                triangles[offset + 3] = next;
                triangles[offset + 4] = i * 2 + 1;
                triangles[offset + 5] = next + 1;
            }
            mesh = new Mesh { name = name, vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
    }
}
