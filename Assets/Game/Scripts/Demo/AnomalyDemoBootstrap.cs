using System.Collections.Generic;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Core;
using Zero.AnomalyPrototype.HorrorEvents;
using Zero.AnomalyPrototype.Loop;
using Zero.AnomalyPrototype.Player;
using Zero.AnomalyPrototype.UI;
using Zero.AnomalyPrototype.Utilities;
using UnityEngine;
using UnityEngine.Rendering;

namespace Zero.AnomalyPrototype.Demo
{
    [DefaultExecutionOrder(-1000)]
    public sealed class AnomalyDemoBootstrap : MonoBehaviour
    {
        public const int CurrentSceneBakeVersion = 7;

        [Header("Scene Authoring")]
        [SerializeField] private bool sceneBaked;
        [SerializeField] private int sceneBakeVersion;
        [SerializeField] private AnomalyData[] authoredAnomalyData;
        [SerializeField] private Material[] authoredMaterials;
        [SerializeField] private Transform authoredHandmadeEnvironment;

        [Header("Baked Scene References")]
        [SerializeField] private GameManager bakedGameManager;
        [SerializeField] private LoopManager bakedLoopManager;
        [SerializeField] private AnomalyManager bakedAnomalyManager;
        [SerializeField] private HorrorEventManager bakedHorrorEventManager;
        [SerializeField] private ProgressManager bakedProgressManager;
        [SerializeField] private AudioManager bakedAudioManager;
        [SerializeField] private FirstPersonController bakedPlayer;
        [SerializeField] private PlayerInteraction bakedPlayerInteraction;
        [SerializeField] private Transform bakedEnvironmentRoot;
        [SerializeField] private Transform bakedLoopStart;
        [SerializeField] private GameObject bakedDecisionRoot;
        [SerializeField] private GameObject bakedExitRoot;
        [SerializeField] private DecisionTrigger[] bakedDecisionTriggers;
        [SerializeField] private PlayerTrigger bakedHorrorTrigger;
        [SerializeField] private PlayerTrigger bakedExitTrigger;
        [SerializeField] private AnomalyBehaviour[] bakedAnomalies;
        [SerializeField] private HorrorEventBehaviour[] bakedHorrorEvents;
        [SerializeField] private RuntimeHUD bakedHud;

        private readonly List<Material> runtimeMaterials = new List<Material>();
        private readonly List<AnomalyData> runtimeAnomalyData = new List<AnomalyData>();
        private bool built;

        private Material wallMaterial;
        private Material floorMaterial;
        private Material ceilingMaterial;
        private Material darkMaterial;
        private Material woodMaterial;
        private Material redMaterial;
        private Material blueMaterial;
        private Material greenMaterial;
        private Material brassMaterial;
        private Material paintingMaterial;

        private struct HandmadeLayout
        {
            public Vector3 LoopStartPosition;
            public Quaternion LoopStartRotation;
            public Vector3 UpDecisionPosition;
            public Vector3 DownDecisionPosition;
            public Vector3 HorrorTriggerPosition;
            public Vector3 HorrorTriggerSize;
            public Vector3 ExitPosition;
            public Quaternion ExitRotation;
        }

        public bool IsSceneBaked => sceneBaked && sceneBakeVersion == CurrentSceneBakeVersion;

        private void Awake()
        {
            if (IsSceneBaked)
            {
                built = true;
                InitializeBakedScene();
                return;
            }

            BuildForValidation();
        }

        public void ConfigureAuthoringAssets(AnomalyData[] anomalyDataAssets, Material[] materialAssets)
        {
            authoredAnomalyData = anomalyDataAssets;
            authoredMaterials = materialAssets;
        }

        public void ConfigureHandmadeEnvironment(Transform environmentRoot)
        {
            authoredHandmadeEnvironment = environmentRoot;
        }

        public void MarkSceneBaked()
        {
            sceneBaked = true;
            sceneBakeVersion = CurrentSceneBakeVersion;
        }

        public void BuildForValidation(bool startGame = true)
        {
            if (built)
            {
                return;
            }

            built = true;
            BuildPrototype(startGame);
        }

        private void BuildPrototype(bool startGame)
        {
            ConfigureAtmosphere();
            CreateMaterials();

            Transform managersRoot = CreateEmpty("Managers", transform).transform;
            Transform playerRoot = CreateEmpty("Player", transform).transform;
            Transform environmentRoot = CreateEmpty("Environment", transform).transform;
            Transform anomaliesRoot = CreateEmpty("Anomalies", transform).transform;
            Transform horrorRoot = CreateEmpty("HorrorEvents", transform).transform;
            Transform triggersRoot = CreateEmpty("Triggers", transform).transform;
            Transform loopPointsRoot = CreateEmpty("LoopPoints", transform).transform;
            Transform uiRoot = CreateEmpty("UI", transform).transform;

            GameManager gameManager = CreateManager<GameManager>("GameManager", managersRoot);
            LoopManager loopManager = CreateManager<LoopManager>("LoopManager", managersRoot);
            AnomalyManager anomalyManager = CreateManager<AnomalyManager>("AnomalyManager", managersRoot);
            HorrorEventManager horrorEventManager = CreateManager<HorrorEventManager>("HorrorEventManager", managersRoot);
            ProgressManager progressManager = CreateManager<ProgressManager>("ProgressManager", managersRoot);
            AudioManager audioManager = CreateManager<AudioManager>("AudioManager", managersRoot);

            bool usesHandmadeLayout = TryPrepareHandmadeLayout(environmentRoot, out HandmadeLayout handmadeLayout);
            FirstPersonController player = BuildPlayer(playerRoot, out PlayerInteraction playerInteraction, out Camera playerCamera);
            Vector3 loopStartPosition = usesHandmadeLayout
                ? handmadeLayout.LoopStartPosition
                : new Vector3(0f, 0.05f, -5.25f);
            Quaternion loopStartRotation = usesHandmadeLayout ? handmadeLayout.LoopStartRotation : Quaternion.identity;
            Transform loopStart = CreatePoint("LoopStart", loopPointsRoot, loopStartPosition, loopStartRotation);

            GameObject painting;
            Transform chair;
            Transform hourHand;
            Transform minuteHand;
            GameObject sideDoor;
            GameObject replacementWall;
            Transform statue;
            Light[] corridorLights;
            if (usesHandmadeLayout)
            {
                BuildFloorAnomalyEnvironment(environmentRoot, out painting, out chair, out hourHand,
                    out minuteHand, out sideDoor, out replacementWall, out statue, out corridorLights);
            }
            else
            {
                CreatePoint("LoopEnd", loopPointsRoot, new Vector3(0f, 0f, 16f), Quaternion.identity);
                BuildPenroseStaircase(environmentRoot, out painting, out chair, out hourHand,
                    out minuteHand, out sideDoor, out replacementWall, out statue, out corridorLights);
            }

            List<AnomalyBehaviour> anomalies = BuildAnomalies(
                anomaliesRoot,
                painting,
                chair,
                sideDoor,
                replacementWall,
                statue,
                player.transform,
                playerCamera);

            List<HorrorEventBehaviour> horrorEvents = BuildHorrorEvents(
                horrorRoot,
                environmentRoot,
                corridorLights,
                player.transform,
                audioManager);

            GameObject decisionRoot = CreateEmpty("DecisionGates", triggersRoot);
            if (!usesHandmadeLayout)
            {
                CreatePrimitive("CenterDecisionBlocker", PrimitiveType.Cube, decisionRoot.transform,
                    new Vector3(0f, 0.85f, 6.7f), new Vector3(1.45f, 1.7f, 1.5f), darkMaterial);
            }

            Quaternion decisionRotation = usesHandmadeLayout
                ? environmentRoot.rotation * Quaternion.Euler(0f, 180f, 0f)
                : Quaternion.identity;
            DecisionTrigger normalDecision = BuildDecisionGate(
                decisionRoot.transform,
                "DecisionUp_Normal",
                usesHandmadeLayout ? handmadeLayout.UpDecisionPosition : new Vector3(2.5f, 3.36f, 15.65f),
                decisionRotation,
                "UP — NO ANOMALY",
                blueMaterial,
                LoopChoice.Normal,
                loopManager);
            DecisionTrigger anomalyDecision = BuildDecisionGate(
                decisionRoot.transform,
                "DecisionDown_Anomaly",
                usesHandmadeLayout ? handmadeLayout.DownDecisionPosition : new Vector3(-2.5f, -3.36f, 15.65f),
                decisionRotation,
                "DOWN — ANOMALY",
                redMaterial,
                LoopChoice.Anomaly,
                loopManager);

            PlayerTrigger horrorTrigger = BuildPlayerTrigger(
                "HorrorTrigger",
                triggersRoot,
                usesHandmadeLayout ? handmadeLayout.HorrorTriggerPosition : new Vector3(0f, 1.5f, -0.5f),
                usesHandmadeLayout ? handmadeLayout.HorrorTriggerSize : new Vector3(9.5f, 3f, 1.2f),
                true);

            GameObject exitRoot = BuildExitGate(
                triggersRoot,
                usesHandmadeLayout ? handmadeLayout.ExitPosition : new Vector3(0f, 0f, 6.1f),
                usesHandmadeLayout ? handmadeLayout.ExitRotation : Quaternion.identity,
                out PlayerTrigger exitTrigger);
            exitRoot.SetActive(false);

            RuntimeHUD hud = uiRoot.gameObject.AddComponent<RuntimeHUD>();

            bakedGameManager = gameManager;
            bakedLoopManager = loopManager;
            bakedAnomalyManager = anomalyManager;
            bakedHorrorEventManager = horrorEventManager;
            bakedProgressManager = progressManager;
            bakedAudioManager = audioManager;
            bakedPlayer = player;
            bakedPlayerInteraction = playerInteraction;
            bakedEnvironmentRoot = environmentRoot;
            bakedLoopStart = loopStart;
            bakedDecisionRoot = decisionRoot;
            bakedExitRoot = exitRoot;
            bakedDecisionTriggers = new[] { normalDecision, anomalyDecision };
            bakedHorrorTrigger = horrorTrigger;
            bakedExitTrigger = exitTrigger;
            bakedAnomalies = anomalies.ToArray();
            bakedHorrorEvents = horrorEvents.ToArray();
            bakedHud = hud;

            if (startGame)
            {
                InitializeBakedScene();
            }
        }

        private void InitializeBakedScene()
        {
            if (bakedEnvironmentRoot == null || bakedGameManager == null || bakedLoopManager == null ||
                bakedAnomalyManager == null || bakedHorrorEventManager == null || bakedProgressManager == null ||
                bakedPlayer == null)
            {
                Debug.LogError("AnomalyDemo has incomplete baked references. Rebuild it from Tools > Anomaly Demo.", this);
                return;
            }

            if (bakedAnomalies != null)
            {
                for (int i = 0; i < bakedAnomalies.Length; i++)
                {
                    bakedAnomalies[i]?.CaptureNormalState();
                }
            }

            if (bakedHorrorEvents != null)
            {
                for (int i = 0; i < bakedHorrorEvents.Length; i++)
                {
                    bakedHorrorEvents[i]?.CaptureNormalState();
                }
            }

            var normalStateRegistry = new NormalStateRegistry();
            normalStateRegistry.CaptureFrom(bakedEnvironmentRoot);
            bakedProgressManager.Configure(5);
            bakedAnomalyManager.Initialize(bakedAnomalies, 0.62f, 2);
            bakedHorrorEventManager.Initialize(bakedHorrorEvents, 0.55f);
            bakedLoopManager.Initialize(
                bakedProgressManager,
                bakedAnomalyManager,
                bakedHorrorEventManager,
                normalStateRegistry,
                bakedPlayer,
                bakedLoopStart,
                bakedDecisionRoot,
                bakedExitRoot,
                bakedDecisionTriggers,
                bakedHorrorTrigger,
                bakedExitTrigger);
            bakedGameManager.Initialize(bakedLoopManager);
            bakedHud?.Initialize(bakedGameManager, bakedLoopManager, bakedProgressManager, bakedPlayerInteraction);
            bakedGameManager.StartGame();
        }

        private FirstPersonController BuildPlayer(
            Transform playerRoot,
            out PlayerInteraction interaction,
            out Camera playerCamera)
        {
            playerRoot.position = new Vector3(0f, 0.05f, -5.25f);
            CharacterController characterController = playerRoot.gameObject.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.32f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.stepOffset = 0.3f;
            characterController.slopeLimit = 50f;

            GameObject cameraObject = CreateEmpty("Camera", playerRoot);
            cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            playerCamera = cameraObject.AddComponent<Camera>();
            playerCamera.fieldOfView = 68f;
            playerCamera.nearClipPlane = 0.04f;
            playerCamera.farClipPlane = 120f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.tag = "MainCamera";

            FirstPersonController controller = playerRoot.gameObject.AddComponent<FirstPersonController>();
            controller.Configure(playerCamera);
            interaction = playerRoot.gameObject.AddComponent<PlayerInteraction>();
            interaction.Configure(playerCamera);
            return controller;
        }

        private bool TryPrepareHandmadeLayout(Transform gameplaySpace, out HandmadeLayout layout)
        {
            layout = default;
            if (authoredHandmadeEnvironment == null)
            {
                return false;
            }

            Transform observationFloor = FindNamedTransform(authoredHandmadeEnvironment, "Floor2");
            Transform upperLanding = FindNamedTransform(authoredHandmadeEnvironment, "Landing1");
            Transform lowerLanding = FindNamedTransform(authoredHandmadeEnvironment, "Landing2");
            Transform upperFlight = FindNamedTransform(authoredHandmadeEnvironment, "DownFlight_Anomaly (2)");
            Transform lowerFlight = FindNamedTransform(authoredHandmadeEnvironment, "UpFlight_Normal (1)");
            Transform upperStep = FindNamedTransform(upperFlight, "DownStep_01");
            Transform lowerStep = FindNamedTransform(lowerFlight, "UpStep_01");
            if (observationFloor == null || upperLanding == null || lowerLanding == null ||
                upperStep == null || lowerStep == null)
            {
                Debug.LogWarning(
                    "Environment-HandMade is missing Floor2, Landing1/Landing2, or the connecting first stair steps. " +
                    "The generated fallback layout will be used.", authoredHandmadeEnvironment);
                return false;
            }

            gameplaySpace.position = observationFloor.TransformPoint(new Vector3(0f, 0.5f, 0f));
            gameplaySpace.rotation = observationFloor.rotation;

            layout.LoopStartPosition = gameplaySpace.TransformPoint(new Vector3(0f, 0.05f, 5.65f));
            layout.LoopStartRotation = gameplaySpace.rotation * Quaternion.Euler(0f, 180f, 0f);
            layout.UpDecisionPosition = ResolveLandingDecisionPoint(upperLanding, upperStep);
            layout.DownDecisionPosition = ResolveLandingDecisionPoint(lowerLanding, lowerStep);
            layout.HorrorTriggerPosition = gameplaySpace.TransformPoint(new Vector3(0f, 1.5f, 1.25f));
            layout.HorrorTriggerSize = new Vector3(
                Mathf.Clamp(Mathf.Abs(observationFloor.lossyScale.x) - 1f, 4f, 11f), 3f, 1.4f);
            layout.ExitPosition = gameplaySpace.TransformPoint(new Vector3(0f, 0f, 0.5f));
            layout.ExitRotation = gameplaySpace.rotation * Quaternion.Euler(0f, 180f, 0f);
            return true;
        }

        private static Vector3 ResolveLandingDecisionPoint(Transform landing, Transform connectingStep)
        {
            Vector3 landingSurface = landing.TransformPoint(new Vector3(0f, 0.5f, 0f));
            Vector3 stepSurface = connectingStep.TransformPoint(new Vector3(0f, 0.5f, 0f));
            Transform adjacentStep = null;
            float nearestDistance = float.PositiveInfinity;
            Transform flight = connectingStep.parent;
            for (int i = 0; i < flight.childCount; i++)
            {
                Transform candidate = flight.GetChild(i);
                if (candidate == connectingStep ||
                    candidate.name.IndexOf("Step", System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                float distance = (candidate.position - connectingStep.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    adjacentStep = candidate;
                }
            }

            Vector3 towardLanding = adjacentStep != null
                ? Vector3.ProjectOnPlane(connectingStep.position - adjacentStep.position, landing.up)
                : Vector3.ProjectOnPlane(landingSurface - stepSurface, landing.up);
            if (towardLanding.sqrMagnitude < 0.01f)
            {
                towardLanding = -landing.forward;
            }

            Vector3 point = stepSurface + towardLanding.normalized * 1.05f;
            return point + landing.up * Vector3.Dot(landingSurface - point, landing.up);
        }

        private static Transform FindNamedTransform(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                if (string.Equals(descendants[i].name, objectName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return descendants[i];
                }
            }

            return null;
        }

        private void BuildFloorAnomalyEnvironment(
            Transform environmentRoot,
            out GameObject painting,
            out Transform chair,
            out Transform hourHand,
            out Transform minuteHand,
            out GameObject sideDoor,
            out GameObject replacementWall,
            out Transform statue,
            out Light[] corridorLights)
        {
            Transform floorContent = CreateEmpty("FloorAnomalyContent", environmentRoot).transform;
            Transform props = CreateEmpty("Props", floorContent).transform;
            Transform lightsRoot = CreateEmpty("Lights", floorContent).transform;

            CreatePrimitive("LeftExhibitWall", PrimitiveType.Cube, floorContent,
                new Vector3(-5.1f, 1.8f, 0f), new Vector3(0.2f, 3.6f, 12.2f), wallMaterial);
            CreatePrimitive("RightExhibitWall", PrimitiveType.Cube, floorContent,
                new Vector3(5.1f, 1.8f, 0f), new Vector3(0.2f, 3.6f, 12.2f), wallMaterial);
            CreatePrimitive("FarExhibitWall", PrimitiveType.Cube, floorContent,
                new Vector3(0f, 1.8f, 6.25f), new Vector3(10.4f, 3.6f, 0.2f), wallMaterial);

            painting = BuildPainting(props);
            BuildClock(props, out hourHand, out minuteHand);
            chair = BuildChair(props);
            BuildSideDoor(props, out sideDoor, out replacementWall);
            statue = BuildStatue(props);
            BuildExtraMemoryProps(props);
            corridorLights = BuildLights(lightsRoot, new[]
            {
                new Vector3(-3.2f, 3.45f, -4.2f),
                new Vector3(3.2f, 3.45f, -4.2f),
                new Vector3(0f, 3.45f, 0f),
                new Vector3(-3.2f, 3.45f, 4.1f),
                new Vector3(3.2f, 3.45f, 4.1f)
            });
        }

        private void BuildPenroseStaircase(
            Transform environmentRoot,
            out GameObject painting,
            out Transform chair,
            out Transform hourHand,
            out Transform minuteHand,
            out GameObject sideDoor,
            out GameObject replacementWall,
            out Transform statue,
            out Light[] corridorLights)
        {
            Transform staircase = CreateEmpty("PenroseStaircase", environmentRoot).transform;
            Transform observationLanding = CreateEmpty("ObservationLanding", staircase).transform;
            Transform upFlight = CreateEmpty("UpFlight_Normal", staircase).transform;
            Transform downFlight = CreateEmpty("DownFlight_Anomaly", staircase).transform;
            Transform paradoxStructure = CreateEmpty("ParadoxStructure", staircase).transform;
            Transform props = CreateEmpty("Props", environmentRoot).transform;
            Transform lightsRoot = CreateEmpty("Lights", environmentRoot).transform;

            CreatePrimitive("LandingFloor", PrimitiveType.Cube, observationLanding, new Vector3(0f, -0.15f, 0f),
                new Vector3(10f, 0.3f, 12f), floorMaterial);
            CreatePrimitive("EntranceWall", PrimitiveType.Cube, observationLanding, new Vector3(0f, 2f, -6.1f),
                new Vector3(10.4f, 4f, 0.22f), wallMaterial);
            CreatePrimitive("LeftMemoryWall", PrimitiveType.Cube, observationLanding, new Vector3(-5.1f, 2f, 0f),
                new Vector3(0.22f, 4f, 12.2f), wallMaterial);
            CreatePrimitive("RightMemoryWall", PrimitiveType.Cube, observationLanding, new Vector3(5.1f, 2f, 0f),
                new Vector3(0.22f, 4f, 12.2f), wallMaterial);
            CreatePrimitive("LandingFrontTrim", PrimitiveType.Cube, observationLanding, new Vector3(0f, 0.2f, -5.88f),
                new Vector3(9.8f, 0.38f, 0.14f), woodMaterial);

            const int stepCount = 14;
            const float stepDepth = 0.67f;
            const float stepRise = 0.24f;
            const float stairStartZ = 6f;
            for (int i = 0; i < stepCount; i++)
            {
                float z = stairStartZ + stepDepth * (i + 0.5f);
                float upSurface = stepRise * (i + 1);
                float downSurface = -stepRise * (i + 1);
                CreatePrimitive($"UpStep_{i + 1:00}", PrimitiveType.Cube, upFlight,
                    new Vector3(2.5f, upSurface - 0.12f, z), new Vector3(3f, 0.24f, stepDepth), ceilingMaterial);
                CreatePrimitive($"DownStep_{i + 1:00}", PrimitiveType.Cube, downFlight,
                    new Vector3(-2.5f, downSurface - 0.12f, z), new Vector3(3f, 0.24f, stepDepth), ceilingMaterial);
            }

            float totalRise = stepCount * stepRise;
            float stairLength = stepCount * stepDepth;
            float stairMidZ = stairStartZ + stairLength * 0.5f;
            float slopeAngle = Mathf.Atan2(totalRise, stairLength) * Mathf.Rad2Deg;
            for (int side = -1; side <= 1; side += 2)
            {
                float upRailX = 2.5f + side * 1.53f;
                float downRailX = -2.5f + side * 1.53f;
                CreatePrimitive($"UpRail_{side}", PrimitiveType.Cube, upFlight,
                    new Vector3(upRailX, totalRise * 0.5f + 0.82f, stairMidZ),
                    new Vector3(0.1f, 0.12f, stairLength + 0.5f), brassMaterial, true,
                    new Vector3(-slopeAngle, 0f, 0f));
                CreatePrimitive($"DownRail_{side}", PrimitiveType.Cube, downFlight,
                    new Vector3(downRailX, -totalRise * 0.5f + 0.82f, stairMidZ),
                    new Vector3(0.1f, 0.12f, stairLength + 0.5f), brassMaterial, true,
                    new Vector3(slopeAngle, 0f, 0f));
            }

            CreateTransitionLanding("UpperTransition", upFlight, new Vector3(2.5f, totalRise, 16.1f));
            CreateTransitionLanding("LowerTransition", downFlight, new Vector3(-2.5f, -totalRise, 16.1f));

            CreatePrimitive("ParadoxCore", PrimitiveType.Cube, paradoxStructure, new Vector3(0f, 0f, 11.2f),
                new Vector3(1.45f, 7.4f, 10.6f), darkMaterial, false);
            CreatePrimitive("UpperImpossibleBridge", PrimitiveType.Cube, paradoxStructure,
                new Vector3(0f, totalRise - 0.15f, 17.15f), new Vector3(8f, 0.3f, 2.1f), floorMaterial);
            CreatePrimitive("LowerImpossibleBridge", PrimitiveType.Cube, paradoxStructure,
                new Vector3(0f, -totalRise - 0.15f, 17.15f), new Vector3(8f, 0.3f, 2.1f), floorMaterial);
            CreatePrimitive("VoidBackdrop", PrimitiveType.Cube, paradoxStructure, new Vector3(0f, 0f, 18.3f),
                new Vector3(10f, 10f, 0.3f), darkMaterial);

            painting = BuildPainting(props);
            BuildClock(props, out hourHand, out minuteHand);
            chair = BuildChair(props);
            BuildSideDoor(props, out sideDoor, out replacementWall);
            statue = BuildStatue(props);
            BuildExtraMemoryProps(props);
            corridorLights = BuildLights(lightsRoot);
        }

        private void CreateTransitionLanding(string name, Transform parent, Vector3 surfacePosition)
        {
            GameObject root = CreateEmpty(name, parent);
            CreatePrimitive("Floor", PrimitiveType.Cube, root.transform,
                new Vector3(surfacePosition.x, surfacePosition.y - 0.15f, surfacePosition.z),
                new Vector3(3.4f, 0.3f, 3.5f), floorMaterial);
            CreatePrimitive("LeftWall", PrimitiveType.Cube, root.transform,
                new Vector3(surfacePosition.x - 1.65f, surfacePosition.y + 1.5f, surfacePosition.z),
                new Vector3(0.18f, 3f, 3.5f), darkMaterial);
            CreatePrimitive("RightWall", PrimitiveType.Cube, root.transform,
                new Vector3(surfacePosition.x + 1.65f, surfacePosition.y + 1.5f, surfacePosition.z),
                new Vector3(0.18f, 3f, 3.5f), darkMaterial);
            CreatePrimitive("OcclusionWall", PrimitiveType.Cube, root.transform,
                new Vector3(surfacePosition.x, surfacePosition.y + 1.5f, surfacePosition.z + 1.7f),
                new Vector3(3.5f, 3f, 0.18f), darkMaterial);
        }

        private GameObject BuildPainting(Transform props)
        {
            GameObject root = CreateEmpty("Painting", props);
            root.transform.localPosition = new Vector3(4.86f, 2.05f, -2.75f);
            root.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            CreatePrimitive("Frame", PrimitiveType.Cube, root.transform, Vector3.zero,
                new Vector3(1.7f, 1.25f, 0.12f), woodMaterial);
            CreatePrimitive("Canvas", PrimitiveType.Cube, root.transform, new Vector3(0f, 0f, -0.08f),
                new Vector3(1.38f, 0.94f, 0.08f), paintingMaterial, false);
            root.AddComponent<NormalStateSnapshot>();
            return root;
        }

        private void BuildClock(Transform props, out Transform hourHand, out Transform minuteHand)
        {
            GameObject root = CreateEmpty("Clock", props);
            root.transform.localPosition = new Vector3(-4.86f, 2.15f, -2.75f);
            root.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            CreatePrimitive("ClockBody", PrimitiveType.Cylinder, root.transform, Vector3.zero,
                new Vector3(0.62f, 0.07f, 0.62f), darkMaterial, false, new Vector3(90f, 0f, 0f));
            CreatePrimitive("ClockFace", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0f, 0.09f),
                new Vector3(0.52f, 0.03f, 0.52f), ceilingMaterial, false, new Vector3(90f, 0f, 0f));
            hourHand = CreatePrimitive("HourHand", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.16f, 0.145f),
                new Vector3(0.065f, 0.34f, 0.035f), darkMaterial, false).transform;
            minuteHand = CreatePrimitive("MinuteHand", PrimitiveType.Cube, root.transform, new Vector3(0.19f, 0.13f, 0.15f),
                new Vector3(0.055f, 0.48f, 0.03f), redMaterial, false, new Vector3(0f, 0f, -50f)).transform;
            root.AddComponent<NormalStateSnapshot>();
        }

        private Transform BuildChair(Transform props)
        {
            GameObject root = CreateEmpty("Chair", props);
            root.transform.localPosition = new Vector3(2.15f, 0f, 4f);
            root.transform.localRotation = Quaternion.Euler(0f, -18f, 0f);
            CreatePrimitive("Seat", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.82f, 0f),
                new Vector3(1.05f, 0.14f, 1f), woodMaterial);
            CreatePrimitive("Back", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.5f, 0.42f),
                new Vector3(1.05f, 1.3f, 0.12f), woodMaterial);
            float[] signs = { -1f, 1f };
            for (int x = 0; x < signs.Length; x++)
            {
                for (int z = 0; z < signs.Length; z++)
                {
                    CreatePrimitive($"Leg_{x}_{z}", PrimitiveType.Cube, root.transform,
                        new Vector3(signs[x] * 0.4f, 0.4f, signs[z] * 0.37f),
                        new Vector3(0.12f, 0.8f, 0.12f), woodMaterial);
                }
            }

            root.AddComponent<NormalStateSnapshot>();
            return root.transform;
        }

        private void BuildSideDoor(Transform props, out GameObject door, out GameObject wallReplacement)
        {
            door = CreateEmpty("Door", props);
            door.transform.localPosition = new Vector3(-4.88f, 0f, 2.2f);
            door.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            CreatePrimitive("DoorPanel", PrimitiveType.Cube, door.transform, new Vector3(0f, 1.45f, 0f),
                new Vector3(1.75f, 2.9f, 0.12f), woodMaterial, false);
            CreatePrimitive("DoorInset", PrimitiveType.Cube, door.transform, new Vector3(0f, 1.45f, -0.08f),
                new Vector3(1.42f, 2.55f, 0.04f), darkMaterial, false);
            CreatePrimitive("Handle", PrimitiveType.Sphere, door.transform, new Vector3(0.58f, 1.42f, -0.18f),
                new Vector3(0.12f, 0.12f, 0.12f), brassMaterial, false);
            door.AddComponent<NormalStateSnapshot>();

            wallReplacement = CreatePrimitive("DoorWallReplacement", PrimitiveType.Cube, props,
                new Vector3(-4.89f, 1.45f, 2.2f), new Vector3(0.13f, 2.9f, 1.78f), wallMaterial, false);
            wallReplacement.SetActive(false);
            wallReplacement.AddComponent<NormalStateSnapshot>();
        }

        private Transform BuildStatue(Transform props)
        {
            GameObject root = CreateEmpty("Statue", props);
            root.transform.localPosition = new Vector3(2.65f, 1.08f, 4.15f);
            root.transform.localRotation = Quaternion.Euler(0f, -145f, 0f);
            CreatePrimitive("Body", PrimitiveType.Capsule, root.transform, Vector3.zero,
                new Vector3(0.72f, 1.08f, 0.72f), darkMaterial);
            CreatePrimitive("Head", PrimitiveType.Sphere, root.transform, new Vector3(0f, 1.17f, 0f),
                new Vector3(0.62f, 0.68f, 0.62f), ceilingMaterial);
            CreatePrimitive("Face", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.17f, 0.31f),
                new Vector3(0.35f, 0.24f, 0.05f), darkMaterial, false);
            root.AddComponent<NormalStateSnapshot>();
            return root.transform;
        }

        private void BuildExtraMemoryProps(Transform props)
        {
            GameObject pipes = CreateEmpty("Pipes", props);
            pipes.transform.localPosition = new Vector3(-4.78f, 3.25f, -0.5f);
            for (int i = 0; i < 3; i++)
            {
                CreatePrimitive($"Pipe_{i}", PrimitiveType.Cylinder, pipes.transform,
                    new Vector3(0f, 0f, i * 0.28f), new Vector3(0.07f, 2.3f, 0.07f), brassMaterial, false,
                    new Vector3(90f, 0f, 0f));
            }

            GameObject crate = CreatePrimitive("Crate", PrimitiveType.Cube, props, new Vector3(-2.85f, 0.55f, -3f),
                new Vector3(1.1f, 1.1f, 1.1f), woodMaterial);
            crate.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
        }

        private Light[] BuildLights(Transform lightsRoot, Vector3[] positions = null)
        {
            var lights = new List<Light>();
            int index = 0;
            Vector3[] lightPositions = positions ?? new[]
            {
                new Vector3(0f, 3.75f, -4f),
                new Vector3(0f, 3.75f, 0f),
                new Vector3(0f, 3.75f, 4f),
                new Vector3(2.5f, 7.2f, 12f),
                new Vector3(-2.5f, 0.5f, 12f)
            };
            for (int positionIndex = 0; positionIndex < lightPositions.Length; positionIndex++)
            {
                GameObject fixture = CreateEmpty($"CeilingLight_{index++:00}", lightsRoot);
                fixture.transform.localPosition = lightPositions[positionIndex];
                CreatePrimitive("Housing", PrimitiveType.Cube, fixture.transform, Vector3.zero,
                    new Vector3(1.25f, 0.12f, 0.34f), darkMaterial, false);
                CreatePrimitive("Glow", PrimitiveType.Cube, fixture.transform, new Vector3(0f, -0.08f, 0f),
                    new Vector3(1.05f, 0.04f, 0.24f), ceilingMaterial, false);

                GameObject lightObject = CreateEmpty("PointLight", fixture.transform);
                lightObject.transform.localPosition = new Vector3(0f, -0.3f, 0f);
                Light lightComponent = lightObject.AddComponent<Light>();
                lightComponent.type = LightType.Point;
                lightComponent.range = 10.5f;
                lightComponent.intensity = 2.25f;
                lightComponent.color = new Color(0.88f, 0.9f, 0.72f);
                lightComponent.shadows = LightShadows.None;
                lights.Add(lightComponent);
                fixture.AddComponent<NormalStateSnapshot>();
            }

            return lights.ToArray();
        }

        private List<AnomalyBehaviour> BuildAnomalies(
            Transform anomaliesRoot,
            GameObject painting,
            Transform chair,
            GameObject sideDoor,
            GameObject replacementWall,
            Transform statue,
            Transform player,
            Camera playerCamera)
        {
            var result = new List<AnomalyBehaviour>();

            MissingPaintingAnomaly missingPainting = CreateAnomaly<MissingPaintingAnomaly>(
                "01_StrangePainting", anomaliesRoot, "strange.painting", "Strange Painting",
                AnomalyType.ObjectChange, 1, 1.2f, "A strange painting appears where the wall was empty.");
            missingPainting.Configure(painting);
            result.Add(missingPainting);

            MovingChairAnomaly movingChair = CreateAnomaly<MovingChairAnomaly>(
                "02_MovingChair", anomaliesRoot, "moving.chair", "Moving Chair",
                AnomalyType.ObjectChange, 1, 1.15f, "The chair has shifted into the corridor.");
            movingChair.Configure(chair, new Vector3(-1.25f, 0f, 1.15f), new Vector3(0f, 34f, 0f));
            result.Add(movingChair);

            MissingDoorAnomaly missingDoor = CreateAnomaly<MissingDoorAnomaly>(
                "04_MissingDoor", anomaliesRoot, "missing.door", "Missing Door",
                AnomalyType.Environment, 1, 1.1f, "A side door has become blank wall.");
            missingDoor.Configure(sideDoor, replacementWall);
            result.Add(missingDoor);

            WatchingStatueAnomaly watchingStatue = CreateAnomaly<WatchingStatueAnomaly>(
                "05_WatchingStatue", anomaliesRoot, "watching.statue", "Watching Statue",
                AnomalyType.Psychological, 2, 0.95f, "The statue keeps turning toward the player.");
            watchingStatue.Configure(statue, player);
            result.Add(watchingStatue);

            ObserverStatueAnomaly observerStatue = CreateAnomaly<ObserverStatueAnomaly>(
                "06_ObserverStatue", anomaliesRoot, "observer.statue", "Observer Statue",
                AnomalyType.Observer, 3, 0.85f, "It approaches only while unobserved.");
            observerStatue.Configure(statue, player, playerCamera);
            result.Add(observerStatue);

            return result;
        }

        private List<HorrorEventBehaviour> BuildHorrorEvents(
            Transform horrorRoot,
            Transform environmentRoot,
            Light[] corridorLights,
            Transform player,
            AudioManager audioManager)
        {
            var result = new List<HorrorEventBehaviour>();

            GameObject flickerObject = CreateEmpty("01_LightFlicker", horrorRoot);
            LightFlickerEvent flicker = flickerObject.AddComponent<LightFlickerEvent>();
            flicker.Initialize("Light Flicker", 1f);
            flicker.Configure(corridorLights, 4, 0.1f);
            result.Add(flicker);

            GameObject footstepsObject = CreateEmpty("02_FootstepsBehind", horrorRoot);
            FootstepsBehindEvent footsteps = footstepsObject.AddComponent<FootstepsBehindEvent>();
            footsteps.Initialize("Footsteps Behind", 1f);
            footsteps.Configure(player, audioManager);
            result.Add(footsteps);

            Transform shadowMarkers = CreateEmpty("ShadowMarkers", environmentRoot).transform;
            Transform shadowStart = CreateLocalPoint(
                "ShadowStart", shadowMarkers, new Vector3(-4.1f, 1.1f, 3.65f), Quaternion.identity);
            Transform shadowEnd = CreateLocalPoint(
                "ShadowEnd", shadowMarkers, new Vector3(4.1f, 1.1f, 3.65f), Quaternion.identity);
            GameObject silhouette = CreatePrimitive("ShadowSilhouette", PrimitiveType.Capsule, environmentRoot,
                new Vector3(-4.1f, 1.1f, 3.65f), new Vector3(0.7f, 1.1f, 0.7f), darkMaterial, false);
            silhouette.SetActive(false);
            GameObject shadowEventObject = CreateEmpty("03_ShadowCrossing", horrorRoot);
            ShadowCrossingEvent shadowEvent = shadowEventObject.AddComponent<ShadowCrossingEvent>();
            shadowEvent.Initialize("Shadow Crossing", 1f);
            shadowEvent.Configure(silhouette.transform, shadowStart, shadowEnd, 0.62f);
            result.Add(shadowEvent);

            return result;
        }

        private DecisionTrigger BuildDecisionGate(
            Transform parent,
            string name,
            Vector3 landingPosition,
            Quaternion landingRotation,
            string label,
            Material accent,
            LoopChoice choice,
            LoopManager loopManager)
        {
            GameObject root = CreateEmpty(name, parent);
            root.transform.position = landingPosition;
            root.transform.rotation = landingRotation;
            CreatePrimitive("LeftPost", PrimitiveType.Cube, root.transform,
                new Vector3(-1.25f, 1.5f, 0f),
                new Vector3(0.18f, 3f, 0.18f), accent, false);
            CreatePrimitive("RightPost", PrimitiveType.Cube, root.transform,
                new Vector3(1.25f, 1.5f, 0f),
                new Vector3(0.18f, 3f, 0.18f), accent, false);
            CreatePrimitive("Header", PrimitiveType.Cube, root.transform,
                new Vector3(0f, 3.05f, 0f),
                new Vector3(2.68f, 0.2f, 0.18f), accent, false);
            CreateLocalLabel(label, root.transform, new Vector3(0f, 3.42f, -0.14f), 0.105f,
                choice == LoopChoice.Normal ? new Color(0.55f, 0.72f, 1f) : new Color(1f, 0.42f, 0.36f));

            GameObject triggerObject = CreateEmpty("Trigger", root.transform);
            triggerObject.transform.localPosition = new Vector3(0f, 1.5f, 0.8f);
            BoxCollider collider = triggerObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(3.1f, 3f, 1.5f);
            collider.isTrigger = true;
            DecisionTrigger trigger = triggerObject.AddComponent<DecisionTrigger>();
            trigger.Configure(loopManager, choice);
            return trigger;
        }

        private GameObject BuildExitGate(
            Transform parent,
            Vector3 gatePosition,
            Quaternion gateRotation,
            out PlayerTrigger exitTrigger)
        {
            GameObject root = CreateEmpty("Exit", parent);
            root.transform.position = gatePosition;
            root.transform.rotation = gateRotation;
            CreatePrimitive("ExitThreshold", PrimitiveType.Cube, root.transform, new Vector3(0f, -0.12f, 0.25f),
                new Vector3(3.2f, 0.24f, 2.4f), floorMaterial);
            CreatePrimitive("ExitHeader", PrimitiveType.Cube, root.transform, new Vector3(0f, 3.12f, 0f),
                new Vector3(3.2f, 0.22f, 0.22f), greenMaterial, false);
            CreatePrimitive("ExitLeft", PrimitiveType.Cube, root.transform, new Vector3(-1.5f, 1.5f, 0f),
                new Vector3(0.2f, 3f, 0.22f), greenMaterial, false);
            CreatePrimitive("ExitRight", PrimitiveType.Cube, root.transform, new Vector3(1.5f, 1.5f, 0f),
                new Vector3(0.2f, 3f, 0.22f), greenMaterial, false);
            CreateLocalLabel("EXIT", root.transform, new Vector3(0f, 3.5f, -0.14f), 0.18f,
                new Color(0.45f, 1f, 0.62f));
            exitTrigger = BuildPlayerTrigger("ExitTrigger", root.transform, root.transform.TransformPoint(new Vector3(0f, 1.5f, 0.55f)),
                new Vector3(2.8f, 3f, 1.2f), true);
            return root;
        }

        private PlayerTrigger BuildPlayerTrigger(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 size,
            bool oncePerLoop)
        {
            GameObject triggerObject = CreateEmpty(name, parent);
            triggerObject.transform.position = position;
            BoxCollider collider = triggerObject.AddComponent<BoxCollider>();
            collider.size = size;
            collider.isTrigger = true;
            PlayerTrigger trigger = triggerObject.AddComponent<PlayerTrigger>();
            trigger.Configure(oncePerLoop);
            return trigger;
        }

        private void ConfigureAtmosphere()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.055f, 0.062f, 0.07f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.025f, 0.028f, 0.034f);
            RenderSettings.fogStartDistance = 12f;
            RenderSettings.fogEndDistance = 58f;
        }

        private void CreateMaterials()
        {
            wallMaterial = ResolveMaterial("Wall", new Color(0.19f, 0.2f, 0.18f));
            floorMaterial = ResolveMaterial("Floor", new Color(0.085f, 0.09f, 0.095f));
            ceilingMaterial = ResolveMaterial("Ceiling", new Color(0.57f, 0.58f, 0.51f));
            darkMaterial = ResolveMaterial("Almost Black", new Color(0.018f, 0.02f, 0.025f));
            woodMaterial = ResolveMaterial("Old Wood", new Color(0.24f, 0.105f, 0.055f));
            redMaterial = ResolveMaterial("Anomaly Red", new Color(0.48f, 0.045f, 0.035f), new Color(0.18f, 0.01f, 0.005f));
            blueMaterial = ResolveMaterial("Normal Blue", new Color(0.04f, 0.18f, 0.42f), new Color(0.01f, 0.04f, 0.14f));
            greenMaterial = ResolveMaterial("Exit Green", new Color(0.05f, 0.5f, 0.17f), new Color(0.02f, 0.28f, 0.06f));
            brassMaterial = ResolveMaterial("Brass", new Color(0.55f, 0.38f, 0.08f));
            paintingMaterial = ResolveMaterial("Painting", new Color(0.2f, 0.42f, 0.27f));
        }

        private Material ResolveMaterial(string materialName, Color color, Color? emission = null)
        {
            if (authoredMaterials != null)
            {
                for (int i = 0; i < authoredMaterials.Length; i++)
                {
                    if (authoredMaterials[i] != null && authoredMaterials[i].name == materialName)
                    {
                        return authoredMaterials[i];
                    }
                }
            }

            return CreateMaterial(materialName, color, emission);
        }

        private Material CreateMaterial(string materialName, Color color, Color? emission = null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            var material = new Material(shader)
            {
                name = materialName,
                color = color,
                enableInstancing = true,
                hideFlags = HideFlags.DontSave
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (emission.HasValue && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }

            runtimeMaterials.Add(material);
            return material;
        }

        private T CreateManager<T>(string objectName, Transform parent) where T : Component
        {
            return CreateEmpty(objectName, parent).AddComponent<T>();
        }

        private T CreateAnomaly<T>(
            string objectName,
            Transform parent,
            string id,
            string displayName,
            AnomalyType type,
            int difficulty,
            float weight,
            string description) where T : AnomalyBehaviour
        {
            GameObject module = CreateEmpty(objectName, parent);
            T anomaly = module.AddComponent<T>();
            AnomalyData data = ResolveAnomalyData(id);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<AnomalyData>();
                data.name = $"{displayName} Data";
                data.hideFlags = HideFlags.DontSave;
                data.Configure(id, displayName, type, difficulty, weight, false, description);
                runtimeAnomalyData.Add(data);
            }

            anomaly.Initialize(data);
            return anomaly;
        }

        private AnomalyData ResolveAnomalyData(string id)
        {
            if (authoredAnomalyData == null)
            {
                return null;
            }

            for (int i = 0; i < authoredAnomalyData.Length; i++)
            {
                if (authoredAnomalyData[i] != null && authoredAnomalyData[i].Id == id)
                {
                    return authoredAnomalyData[i];
                }
            }

            return null;
        }

        private GameObject CreatePrimitive(
            string objectName,
            PrimitiveType type,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool keepCollider = true,
            Vector3? eulerRotation = null)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = objectName;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = position;
            primitive.transform.localRotation = Quaternion.Euler(eulerRotation ?? Vector3.zero);
            primitive.transform.localScale = scale;

            Renderer renderer = primitive.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            if (!keepCollider)
            {
                Collider primitiveCollider = primitive.GetComponent<Collider>();
                if (primitiveCollider != null)
                {
                    primitiveCollider.enabled = false;
                    DestroyRuntimeObject(primitiveCollider);
                }
            }

            return primitive;
        }

        private static GameObject CreateEmpty(string objectName, Transform parent)
        {
            var result = new GameObject(objectName);
            result.transform.SetParent(parent, false);
            return result;
        }

        private static Transform CreatePoint(
            string objectName,
            Transform parent,
            Vector3 position,
            Quaternion rotation)
        {
            GameObject point = CreateEmpty(objectName, parent);
            point.transform.position = position;
            point.transform.rotation = rotation;
            return point.transform;
        }

        private static Transform CreateLocalPoint(
            string objectName,
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation)
        {
            GameObject point = CreateEmpty(objectName, parent);
            point.transform.localPosition = localPosition;
            point.transform.localRotation = localRotation;
            return point.transform;
        }

        private static void CreateLocalLabel(
            string text,
            Transform parent,
            Vector3 localPosition,
            float characterSize,
            Color color)
        {
            GameObject labelObject = CreateEmpty($"{text}_Label", parent);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localRotation = Quaternion.identity;
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 72;
            label.characterSize = characterSize;
            label.color = color;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < runtimeMaterials.Count; i++)
            {
                if (runtimeMaterials[i] != null)
                {
                    DestroyRuntimeObject(runtimeMaterials[i]);
                }
            }

            for (int i = 0; i < runtimeAnomalyData.Count; i++)
            {
                if (runtimeAnomalyData[i] != null)
                {
                    DestroyRuntimeObject(runtimeAnomalyData[i]);
                }
            }
        }

        private static void DestroyRuntimeObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
