using System;
using System.Collections.Generic;
using System.Linq;
using Zero.AnomalyPrototype.Anomalies;
using Zero.EnvironmentLoop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Zero.EnvironmentLoop.Editor
{
    public static class RoomAnomalyInstaller
    {
        private const string PrefabPath = "Assets/Game/Prefabs/Environment/PF_FloorSegment.prefab";
        private const string ScenePath = "Assets/Game/Scenes/InfiniteStairwayScene.unity";
        private const string DataFolder = "Assets/Game/ScriptableObjects/Anomalies";

        [MenuItem("Tools/Environment Loop/Install or Repair Room Anomalies")]
        public static void Install()
        {
            InstallPrefabContent();
            InstallSceneManager();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("ROOM_ANOMALY_INSTALL_PASS: Room content and directional gameplay manager are ready.");
        }

        private static void InstallPrefabContent()
        {
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
                    throw new InvalidOperationException("PF_FloorSegment requires a child named Geometry/Room.");
                }

                Bounds roomBounds = CalculateLocalBounds(room);
                Transform content = EnsureChild(room, "AnomalyContent");

                Material furnitureMaterial = LoadMaterial("Assets/Game/Materials/EnvironmentLoop/Loop_Rail.mat");
                Material highlightMaterial = LoadMaterial("Assets/Game/Materials/EnvironmentLoop/Loop_Warning.mat");
                Material wallMaterial = LoadMaterial("Assets/Game/Materials/EnvironmentLoop/Loop_Concrete.mat");
                Material darkMaterial = LoadMaterial("Assets/Game/Materials/EnvironmentLoop/Loop_ElevatorDoor.mat");

                float floorY = roomBounds.min.y + 0.08f;
                Vector3 center = roomBounds.center;
                float halfX = Mathf.Max(2.4f, roomBounds.extents.x - 0.35f);
                float halfZ = Mathf.Max(2.4f, roomBounds.extents.z - 0.35f);

                Transform painting = EnsureRoot(content, "Painting_Target",
                    new Vector3(center.x - halfX * 0.45f, floorY + 1.75f, center.z + halfZ),
                    Quaternion.Euler(0f, 180f, 0f));
                GameObject paintingVisual = EnsureCube(painting, "PaintingVisual", Vector3.zero,
                    new Vector3(1.25f, 0.85f, 0.08f), highlightMaterial);

                Transform chair = EnsureRoot(content, "Chair_Target",
                    new Vector3(center.x - 0.9f, floorY, center.z - 0.35f), Quaternion.Euler(0f, 18f, 0f));
                EnsureCube(chair, "Seat", new Vector3(0f, 0.48f, 0f),
                    new Vector3(0.85f, 0.12f, 0.85f), furnitureMaterial);
                EnsureCube(chair, "Back", new Vector3(0f, 1.02f, 0.36f),
                    new Vector3(0.85f, 1.05f, 0.12f), furnitureMaterial);
                EnsureCube(chair, "Leg_FL", new Vector3(-0.32f, 0.22f, -0.3f),
                    new Vector3(0.1f, 0.45f, 0.1f), furnitureMaterial);
                EnsureCube(chair, "Leg_FR", new Vector3(0.32f, 0.22f, -0.3f),
                    new Vector3(0.1f, 0.45f, 0.1f), furnitureMaterial);
                EnsureCube(chair, "Leg_BL", new Vector3(-0.32f, 0.22f, 0.3f),
                    new Vector3(0.1f, 0.45f, 0.1f), furnitureMaterial);
                EnsureCube(chair, "Leg_BR", new Vector3(0.32f, 0.22f, 0.3f),
                    new Vector3(0.1f, 0.45f, 0.1f), furnitureMaterial);

                Transform clock = EnsureRoot(content, "Clock_Target",
                    new Vector3(center.x + halfX, floorY + 1.85f, center.z + 0.4f),
                    Quaternion.Euler(0f, -90f, 0f));
                EnsureCylinder(clock, "ClockFace", Vector3.zero, new Vector3(0.62f, 0.05f, 0.62f), wallMaterial);
                EnsureCube(clock, "HourHand", new Vector3(0f, 0.075f, 0.16f),
                    new Vector3(0.055f, 0.035f, 0.32f), darkMaterial);
                EnsureCube(clock, "MinuteHand", new Vector3(0.14f, 0.08f, 0f),
                    new Vector3(0.38f, 0.035f, 0.045f), darkMaterial);

                Transform doorWrapper = EnsureRoot(content, "Door_Target",
                    new Vector3(center.x + halfX * 0.45f, floorY + 1.05f, center.z - halfZ),
                    Quaternion.identity);
                GameObject doorVisual = EnsureCube(doorWrapper, "DoorVisual", Vector3.zero,
                    new Vector3(1.25f, 2.1f, 0.12f), darkMaterial);
                GameObject replacementWall = EnsureCube(doorWrapper, "ReplacementWall", Vector3.zero,
                    new Vector3(1.38f, 2.25f, 0.15f), wallMaterial);
                replacementWall.SetActive(false);

                Transform statue = EnsureRoot(content, "Statue_Target",
                    new Vector3(center.x + 1.15f, floorY, center.z + 0.2f), Quaternion.Euler(0f, -25f, 0f));
                EnsureCube(statue, "Body", new Vector3(0f, 0.72f, 0f),
                    new Vector3(0.58f, 1.25f, 0.42f), wallMaterial);
                EnsureSphere(statue, "Head", new Vector3(0f, 1.55f, 0f),
                    new Vector3(0.48f, 0.48f, 0.48f), wallMaterial);

                var missingPainting = GetOrAdd<MissingPaintingAnomaly>(content.gameObject);
                missingPainting.Initialize(LoadData("A01_MissingPainting.asset"));
                missingPainting.Configure(paintingVisual);

                var movingChair = GetOrAdd<MovingChairAnomaly>(content.gameObject);
                movingChair.Initialize(LoadData("A02_MovingChair.asset"));
                movingChair.Configure(chair, new Vector3(1.2f, 0f, -0.8f), new Vector3(0f, 38f, 0f));

                var missingDoor = GetOrAdd<MissingDoorAnomaly>(content.gameObject);
                missingDoor.Initialize(LoadData("A04_MissingDoor.asset"));
                missingDoor.Configure(doorVisual, replacementWall);

                var watchingStatue = GetOrAdd<WatchingStatueAnomaly>(content.gameObject);
                watchingStatue.Initialize(LoadData("A05_WatchingStatue.asset"));
                watchingStatue.Configure(statue, null, 120f);

                var observerStatue = GetOrAdd<ObserverStatueAnomaly>(content.gameObject);
                observerStatue.Initialize(LoadData("A06_ObserverStatue.asset"));
                observerStatue.Configure(statue, null, null);

                AnomalyBehaviour[] slots =
                {
                    missingPainting,
                    movingChair,
                    missingDoor,
                    watchingStatue,
                    observerStatue
                };
                Transform correctArrow = FindNamedTransform(room, "CorrectArrow");
                if (correctArrow == null)
                {
                    throw new InvalidOperationException("Room requires a child named CorrectArrow.");
                }

                CorrectArrowController arrowController = GetOrAdd<CorrectArrowController>(room.gameObject);
                Quaternion upRotation = correctArrow.localRotation;
                Quaternion downRotation = upRotation * Quaternion.AngleAxis(180f, Vector3.forward);
                arrowController.Configure(correctArrow, upRotation, downRotation);
                RoomAnomalyController controller = GetOrAdd<RoomAnomalyController>(room.gameObject);
                controller.Configure(room, slots, arrowController);
                EditorUtility.SetDirty(arrowController);
                EditorUtility.SetDirty(controller);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void InstallSceneManager()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            try
            {
                InfiniteBuildingManager building = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<InfiniteBuildingManager>(true))
                    .FirstOrDefault();
                if (building == null)
                {
                    throw new InvalidOperationException("InfiniteBuildingManager is missing from InfiniteStairwayScene.");
                }

                GetOrAdd<DirectionalAnomalyGameManager>(building.gameObject);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally
            {
                if (openedHere && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static AnomalyData LoadData(string fileName)
        {
            AnomalyData data = AssetDatabase.LoadAssetAtPath<AnomalyData>($"{DataFolder}/{fileName}");
            if (data == null)
            {
                throw new InvalidOperationException($"Missing anomaly data asset: {fileName}");
            }

            return data;
        }

        private static Material LoadMaterial(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        private static T GetOrAdd<T>(GameObject owner) where T : Component
        {
            T component = owner.GetComponent<T>();
            return component != null ? component : owner.AddComponent<T>();
        }

        private static Transform EnsureChild(Transform parent, string objectName)
        {
            Transform child = parent.Find(objectName);
            if (child != null)
            {
                return child;
            }

            var created = new GameObject(objectName).transform;
            created.SetParent(parent, false);
            return created;
        }

        private static Transform EnsureRoot(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            Quaternion localRotation)
        {
            Transform root = parent.Find(objectName);
            if (root == null)
            {
                root = new GameObject(objectName).transform;
                root.SetParent(parent, false);
            }

            root.localPosition = localPosition;
            root.localRotation = localRotation;
            root.localScale = Vector3.one;
            return root;
        }

        private static GameObject EnsureCube(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            return EnsurePrimitive(parent, objectName, PrimitiveType.Cube, localPosition, localScale, material);
        }

        private static GameObject EnsureCylinder(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject result = EnsurePrimitive(parent, objectName, PrimitiveType.Cylinder,
                localPosition, localScale, material);
            result.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return result;
        }

        private static GameObject EnsureSphere(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            return EnsurePrimitive(parent, objectName, PrimitiveType.Sphere, localPosition, localScale, material);
        }

        private static GameObject EnsurePrimitive(
            Transform parent,
            string objectName,
            PrimitiveType type,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            Transform existing = parent.Find(objectName);
            GameObject result;
            if (existing == null)
            {
                result = GameObject.CreatePrimitive(type);
                result.name = objectName;
                result.transform.SetParent(parent, false);
            }
            else
            {
                result = existing.gameObject;
            }

            result.transform.localPosition = localPosition;
            result.transform.localScale = localScale;
            Collider primitiveCollider = result.GetComponent<Collider>();
            if (primitiveCollider != null)
            {
                UnityEngine.Object.DestroyImmediate(primitiveCollider);
            }

            Renderer renderer = result.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            return result;
        }

        private static Bounds CalculateLocalBounds(Transform root)
        {
            Transform anomalyContent = root.Find("AnomalyContent");
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => anomalyContent == null || !renderer.transform.IsChildOf(anomalyContent))
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

            Vector3 scale = root.lossyScale;
            Vector3 localSize = new Vector3(
                world.size.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                world.size.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
                world.size.z / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
            return new Bounds(root.InverseTransformPoint(world.center), localSize);
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
