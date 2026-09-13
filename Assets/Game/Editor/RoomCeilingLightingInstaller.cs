using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Zero.EnvironmentLoop.Editor
{
    public static class RoomCeilingLightingInstaller
    {
        private const string PrefabPath = "Assets/Game/Prefabs/Environment/PF_FloorSegment.prefab";
        private const string MaterialPath =
            "Assets/Game/Materials/EnvironmentLoop/Loop_RoomCeilingEmission.mat";

        [MenuItem("Tools/Environment Loop/Install or Repair Room Ceiling Lights")]
        public static void Install()
        {
            Material emissionMaterial = EnsureEmissionMaterial();
            Material housingMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Game/Materials/EnvironmentLoop/Loop_ElevatorDoor.mat");

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot == null)
            {
                throw new InvalidOperationException($"Cannot load floor prefab at {PrefabPath}.");
            }

            try
            {
                Transform room = FindNamedTransform(prefabRoot.transform, "Room");
                if (room == null)
                {
                    throw new InvalidOperationException("PF_FloorSegment requires Geometry/Room.");
                }

                Transform lightingRoot = room.Find("RoomCeilingLights");
                bool createdRoot = lightingRoot == null;
                if (createdRoot)
                {
                    lightingRoot = new GameObject("RoomCeilingLights").transform;
                    lightingRoot.SetParent(room, false);
                }

                Bounds bounds = CalculateRoomBounds(room, lightingRoot);
                bool alongX = bounds.size.x >= bounds.size.z;
                Vector3 longAxis = alongX ? Vector3.right : Vector3.forward;
                float offset = Mathf.Clamp((alongX ? bounds.extents.x : bounds.extents.z) * 0.42f, 1.4f, 3.2f);
                float ceilingY = bounds.max.y - 0.1f;
                Vector3 fixtureScale = alongX
                    ? new Vector3(2.15f, 0.12f, 0.55f)
                    : new Vector3(0.55f, 0.12f, 2.15f);
                Vector3 panelScale = alongX
                    ? new Vector3(1.88f, 0.055f, 0.38f)
                    : new Vector3(0.38f, 0.055f, 1.88f);

                EnsureFixture(
                    lightingRoot,
                    "CeilingFixture_01",
                    bounds.center + longAxis * -offset + Vector3.up * (ceilingY - bounds.center.y),
                    fixtureScale,
                    panelScale,
                    housingMaterial,
                    emissionMaterial,
                    bounds);
                EnsureFixture(
                    lightingRoot,
                    "CeilingFixture_02",
                    bounds.center + longAxis * offset + Vector3.up * (ceilingY - bounds.center.y),
                    fixtureScale,
                    panelScale,
                    housingMaterial,
                    emissionMaterial,
                    bounds);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("ROOM_CEILING_LIGHT_INSTALL_PASS: two editable emissive ceiling fixtures installed per Room.");
        }

        private static void EnsureFixture(
            Transform parent,
            string fixtureName,
            Vector3 localPosition,
            Vector3 fixtureScale,
            Vector3 panelScale,
            Material housingMaterial,
            Material emissionMaterial,
            Bounds roomBounds)
        {
            Transform fixture = parent.Find(fixtureName);
            if (fixture == null)
            {
                fixture = new GameObject(fixtureName).transform;
                fixture.SetParent(parent, false);
                fixture.localPosition = localPosition;
                fixture.localRotation = Quaternion.identity;
                fixture.localScale = Vector3.one;
            }

            EnsureCube(fixture, "Housing", Vector3.zero, fixtureScale, housingMaterial);
            EnsureCube(fixture, "EmissivePanel", new Vector3(0f, -0.095f, 0f), panelScale, emissionMaterial);

            Transform lightTransform = fixture.Find("RoomLight");
            if (lightTransform == null)
            {
                lightTransform = new GameObject("RoomLight").transform;
                lightTransform.SetParent(fixture, false);
                lightTransform.localPosition = new Vector3(0f, -0.32f, 0f);
            }

            Light roomLight = lightTransform.GetComponent<Light>();
            if (roomLight == null)
            {
                roomLight = lightTransform.gameObject.AddComponent<Light>();
            }

            roomLight.type = LightType.Point;
            roomLight.color = new Color(0.78f, 0.88f, 1f);
            roomLight.intensity = 7f;
            roomLight.range = Mathf.Clamp(Mathf.Max(roomBounds.size.x, roomBounds.size.z) * 0.62f, 4.5f, 8.5f);
            roomLight.shadows = LightShadows.None;
            roomLight.renderMode = LightRenderMode.ForcePixel;
            roomLight.enabled = true;
        }

        private static GameObject EnsureCube(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            Transform existing = parent.Find(objectName);
            GameObject cube;
            if (existing == null)
            {
                cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = objectName;
                cube.transform.SetParent(parent, false);
            }
            else
            {
                cube = existing.gameObject;
            }

            cube.transform.localPosition = localPosition;
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = localScale;
            Collider collider = cube.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            MeshRenderer renderer = cube.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            return cube;
        }

        private static Material EnsureEmissionMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null)
                {
                    throw new InvalidOperationException("No supported Lit shader is available for Room lights.");
                }

                material = new Material(shader) { name = "Loop_RoomCeilingEmission" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            Color panelColor = new Color(0.78f, 0.9f, 1f, 1f);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", panelColor);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", panelColor);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", panelColor * 4.5f);
            }

            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Bounds CalculateRoomBounds(Transform room, Transform lightingRoot)
        {
            Transform anomalyContent = room.Find("AnomalyContent");
            Renderer[] renderers = room.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => !renderer.transform.IsChildOf(lightingRoot) &&
                                   (anomalyContent == null || !renderer.transform.IsChildOf(anomalyContent)))
                .ToArray();
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, new Vector3(8f, 3f, 8f));
            }

            Bounds world = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                world.Encapsulate(renderers[i].bounds);
            }

            Vector3 scale = room.lossyScale;
            Vector3 localSize = new Vector3(
                world.size.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                world.size.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
                world.size.z / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
            return new Bounds(room.InverseTransformPoint(world.center), localSize);
        }

        private static Transform FindNamedTransform(Transform root, string objectName)
        {
            if (root.name == objectName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindNamedTransform(root.GetChild(i), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
