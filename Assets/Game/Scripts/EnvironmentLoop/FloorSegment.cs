using System;
using Zero.AnomalyPrototype.Anomalies;
using Unity.AI.Navigation;
using UnityEngine;

namespace Zero.EnvironmentLoop
{
    public sealed class FloorSegment : MonoBehaviour
    {
        [Serializable]
        private struct TransformBaseline
        {
            public Transform Transform;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Vector3 LocalScale;
            public bool ActiveSelf;
        }

        [Serializable]
        private struct LightBaseline
        {
            public Light Light;
            public bool Enabled;
            public LightShadows Shadows;
        }

        [Header("Geometry")]
        [SerializeField] private Transform geometry;
        [SerializeField] private Transform observationRoom;
        [SerializeField] private Transform stairFlight01;
        [SerializeField] private Transform middleLanding;
        [SerializeField] private Transform stairFlight02;
        [SerializeField] private Transform elevatorSection;
        [SerializeField] private Transform environmentProps;

        [Header("Connection")]
        [SerializeField] private Transform upAnchor;
        [SerializeField] private Transform downAnchor;

        [Header("Triggers")]
        [SerializeField] private Transform triggerRoot;

        private TransformBaseline[] baselines;
        private LightBaseline[] lightBaselines;
        private NavMeshSurface[] navMeshSurfaces;
        private ObserverStatueAnomaly[] observerStatues;
        private CeilingCreatureAnomaly[] ceilingCreatures;
        private bool baselineCaptured;
        private SegmentCommitTrigger[] cachedCommitTriggers;
        private Transform cachedNumberRoot;
        private bool numberCached;

        private Transform NumberRoot
        {
            get
            {
                if (!numberCached)
                {
                    cachedNumberRoot = FindDescendant(transform, "Floor_Number");
                    numberCached = true;
                }
                return cachedNumberRoot;
            }
        }

        private SegmentCommitTrigger[] CommitTriggers => cachedCommitTriggers ??=
            GetComponentsInChildren<SegmentCommitTrigger>(true);

        public Transform Geometry => geometry;
        public Transform ObservationRoom => observationRoom;
        public Transform StairFlight01 => stairFlight01;
        public Transform MiddleLanding => middleLanding;
        public Transform StairFlight02 => stairFlight02;
        public Transform ElevatorSection => elevatorSection;
        public Transform EnvironmentProps => environmentProps;
        public Transform UpAnchor => upAnchor;
        public Transform DownAnchor => downAnchor;
        public string SegmentId => name;
        public bool HasFloorNumberDisplay => NumberRoot != null;

        public void Configure(
            Transform geometryRoot,
            Transform room,
            Transform firstFlight,
            Transform landing,
            Transform secondFlight,
            Transform elevator,
            Transform props,
            Transform upperAnchor,
            Transform lowerAnchor,
            Transform triggers)
        {
            geometry = geometryRoot;
            observationRoom = room;
            stairFlight01 = firstFlight;
            middleLanding = landing;
            stairFlight02 = secondFlight;
            elevatorSection = elevator;
            environmentProps = props;
            upAnchor = upperAnchor;
            downAnchor = lowerAnchor;
            triggerRoot = triggers;
        }

        public void InitializeSegment()
        {
            CaptureBaseline();
            ResetSegment();
        }

        public void PlaceAbove(FloorSegment segment)
        {
            if (segment == null || segment.UpAnchor == null || downAnchor == null)
            {
                return;
            }

            AlignConnection(downAnchor, segment.UpAnchor);
        }

        public void PlaceBelow(FloorSegment segment)
        {
            if (segment == null || segment.DownAnchor == null || upAnchor == null)
            {
                return;
            }

            AlignConnection(upAnchor, segment.DownAnchor);
        }

        public void ResetSegment()
        {
            if (!baselineCaptured)
            {
                CaptureBaseline();
            }

            for (int i = 0; i < baselines.Length; i++)
            {
                TransformBaseline baseline = baselines[i];
                if (baseline.Transform == null)
                {
                    continue;
                }

                baseline.Transform.localPosition = baseline.LocalPosition;
                baseline.Transform.localRotation = baseline.LocalRotation;
                baseline.Transform.localScale = baseline.LocalScale;
                baseline.Transform.gameObject.SetActive(baseline.ActiveSelf);
            }

            AudioSource[] audioSources = GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < audioSources.Length; i++)
            {
                audioSources[i].Stop();
                audioSources[i].time = 0f;
            }

            ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            Animator[] animators = GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                animators[i].Rebind();
                animators[i].Update(0f);
            }

            Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                rigidbodies[i].linearVelocity = Vector3.zero;
                rigidbodies[i].angularVelocity = Vector3.zero;
            }

            SegmentCommitTrigger[] commitTriggers = CommitTriggers;
            for (int i = 0; i < commitTriggers.Length; i++)
            {
                commitTriggers[i].ResetTrigger();
            }

            RoomAnomalyController roomAnomalies = GetComponentInChildren<RoomAnomalyController>(true);
            roomAnomalies?.ResetRoom();
            HideFloorNumber();

        }

        public int ConfigureCommitTriggers(StairLoopManager manager)
        {
            SegmentCommitTrigger[] triggers = CommitTriggers;
            for (int i = 0; i < triggers.Length; i++)
            {
                triggers[i].Configure(manager, this);
            }

            return triggers.Length;
        }

        public void ConfigureNavigation(StairLoopManager manager)
        {
            foreach (var reflection in GetComponentsInChildren<WrongReflectionAnomaly>(true))
                reflection.BindFloorContext(manager, this);

            if (!baselineCaptured)
            {
                CaptureBaseline();
            }

            for (int i = 0; i < observerStatues.Length; i++)
            {
                observerStatues[i]?.BindFloorContext(manager, this);
            }

            for (int i = 0; i < ceilingCreatures.Length; i++)
            {
                ceilingCreatures[i]?.BindFloorContext(manager, this);
            }
        }

        public void SetNavigationActive(bool active)
        {
            if (!baselineCaptured)
            {
                CaptureBaseline();
            }

            for (int i = 0; i < navMeshSurfaces.Length; i++)
            {
                NavMeshSurface surface = navMeshSurfaces[i];
                if (surface != null)
                {
                    surface.enabled = active;
                }
            }
        }

        public int CountEnabledNavigationSurfaces()
        {
            int count = 0;
            if (navMeshSurfaces == null)
            {
                return count;
            }

            for (int i = 0; i < navMeshSurfaces.Length; i++)
            {
                if (navMeshSurfaces[i] != null && navMeshSurfaces[i].enabled &&
                    navMeshSurfaces[i].gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }

        public void ConsumeCommitTriggers()
        {
            SegmentCommitTrigger[] triggers = CommitTriggers;
            for (int i = 0; i < triggers.Length; i++)
            {
                triggers[i].ConsumeWithoutCommit();
            }
        }

        public SegmentCommitTrigger GetCommitTrigger()
        {
            SegmentCommitTrigger[] triggers = CommitTriggers;
            SegmentCommitTrigger fallback = null;
            for (int i = 0; i < triggers.Length; i++)
            {
                if (triggers[i].IsArmed)
                {
                    return triggers[i];
                }

                fallback ??= triggers[i];
            }

            return fallback;
        }

        public void HideFloorNumber()
        {
            Transform floorNumber = NumberRoot;
            if (floorNumber == null)
            {
                return;
            }

            for (int i = 0; i < floorNumber.childCount; i++)
            {
                Transform child = floorNumber.GetChild(i);
                if (TryParseFloorNumberName(child.name, out _))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        public void ShowFloorNumber(int number)
        {
            int displayedNumber = Mathf.Clamp(number, 0, 8);
            Transform floorNumber = NumberRoot;
            if (floorNumber == null)
            {
                return;
            }

            for (int i = 0; i < floorNumber.childCount; i++)
            {
                Transform child = floorNumber.GetChild(i);
                if (TryParseFloorNumberName(child.name, out int childNumber))
                {
                    child.gameObject.SetActive(childNumber == displayedNumber);
                }
            }
        }

        public void ApplyLightingBudget(
            bool enableAuthoredShadowLights,
            bool enableRealtimeShadows,
            int maximumShadowCasters = int.MaxValue)
        {
            if (!baselineCaptured)
            {
                CaptureBaseline();
            }

            int remainingShadowCasters = Mathf.Max(0, maximumShadowCasters);
            for (int i = 0; i < lightBaselines.Length; i++)
            {
                LightBaseline baseline = lightBaselines[i];
                if (baseline.Light == null)
                {
                    continue;
                }

                bool authoredShadowCaster = baseline.Shadows != LightShadows.None;
                baseline.Light.enabled = baseline.Enabled &&
                                         (enableAuthoredShadowLights || !authoredShadowCaster);
                bool receivesShadowBudget = baseline.Light.enabled && authoredShadowCaster &&
                                            enableRealtimeShadows && remainingShadowCasters > 0;
                baseline.Light.shadows = receivesShadowBudget
                    ? baseline.Shadows
                    : LightShadows.None;
                if (receivesShadowBudget)
                {
                    remainingShadowCasters--;
                }
            }
        }

        public int CountEnabledLights(bool shadowCastersOnly = false)
        {
            int count = 0;
            if (lightBaselines == null)
            {
                return count;
            }

            for (int i = 0; i < lightBaselines.Length; i++)
            {
                Light light = lightBaselines[i].Light;
                if (light != null && light.enabled && light.gameObject.activeInHierarchy &&
                    (!shadowCastersOnly || light.shadows != LightShadows.None))
                {
                    count++;
                }
            }

            return count;
        }

        public bool IsConnectedAbove(
            FloorSegment lower,
            float positionTolerance = 0.002f,
            float angleTolerance = 0.1f)
        {
            return lower != null && downAnchor != null && lower.UpAnchor != null &&
                   Vector3.Distance(downAnchor.position, lower.UpAnchor.position) <= positionTolerance &&
                   Quaternion.Angle(downAnchor.rotation, lower.UpAnchor.rotation) <= angleTolerance;
        }

        private void CaptureBaseline()
        {
            Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
            baselines = new TransformBaseline[Mathf.Max(0, allTransforms.Length - 1)];
            int writeIndex = 0;
            for (int i = 0; i < allTransforms.Length; i++)
            {
                Transform child = allTransforms[i];
                if (child == transform)
                {
                    continue;
                }

                baselines[writeIndex++] = new TransformBaseline
                {
                    Transform = child,
                    LocalPosition = child.localPosition,
                    LocalRotation = child.localRotation,
                    LocalScale = child.localScale,
                    ActiveSelf = child.gameObject.activeSelf
                };
            }

            Light[] allLights = GetComponentsInChildren<Light>(true);
            lightBaselines = new LightBaseline[allLights.Length];
            for (int i = 0; i < allLights.Length; i++)
            {
                Light light = allLights[i];
                lightBaselines[i] = new LightBaseline
                {
                    Light = light,
                    Enabled = light.enabled,
                    Shadows = light.shadows
                };
            }

            navMeshSurfaces = GetComponentsInChildren<NavMeshSurface>(true);
            observerStatues = GetComponentsInChildren<ObserverStatueAnomaly>(true);
            ceilingCreatures = GetComponentsInChildren<CeilingCreatureAnomaly>(true);

            baselineCaptured = true;
        }

        private void AlignConnection(Transform ownAnchor, Transform targetAnchor)
        {
            Vector3 anchorLocalPosition = transform.InverseTransformPoint(ownAnchor.position);
            Quaternion anchorLocalRotation = Quaternion.Inverse(transform.rotation) * ownAnchor.rotation;
            Quaternion rootRotation = targetAnchor.rotation * Quaternion.Inverse(anchorLocalRotation);
            Vector3 rootPosition = targetAnchor.position - rootRotation * anchorLocalPosition;
            transform.SetPositionAndRotation(rootPosition, rootRotation);
        }

        private static bool TryParseFloorNumberName(string objectName, out int number)
        {
            string value = objectName;
            if (value.StartsWith("Number", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring("Number".Length);
            }

            return int.TryParse(value, out number) && number >= 0 && number <= 8;
        }

        private static Transform FindDescendant(Transform root, string objectName)
        {
            if (root == null || root.name == objectName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDescendant(root.GetChild(i), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private void OnDrawGizmosSelected()
        {
            if (upAnchor != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(upAnchor.position, 0.18f);
                Gizmos.DrawRay(upAnchor.position, upAnchor.forward * 1.2f);
            }

            if (downAnchor != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(downAnchor.position, 0.18f);
                Gizmos.DrawRay(downAnchor.position, downAnchor.forward * 1.2f);
            }
        }
    }
}
