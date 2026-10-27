using System;
using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class KitchenCockroachSwarmAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform[] insects = Array.Empty<Transform>();
        [SerializeField] private Transform[] routes = Array.Empty<Transform>();
        [SerializeField, Min(0.1f)] private float activationDistance = 2f;
        [SerializeField, Min(0.1f)] private float crawlSpeed = 1.1f;
        private Vector3[][] points;
        private Quaternion[][] poses;
        private int[] segments;
        private Animator[] animators;
        private bool[] running;
        private static readonly int IdleState = Animator.StringToHash("Idle");
        private static readonly int RunState = Animator.StringToHash("Run");
        private float elapsed;
        public bool HasScattered { get; private set; }

        public override void CaptureNormalState()
        {
            if (points != null || player == null || insects.Length == 0 || insects.Length != routes.Length) return;
            for (int i = 0; i < routes.Length; i++)
                if (routes[i] == null || insects[i] == null || routes[i].childCount < 2) return;
            points = new Vector3[routes.Length][];
            poses = new Quaternion[routes.Length][];
            segments = new int[routes.Length];
            animators = new Animator[routes.Length];
            running = new bool[routes.Length];
            for (int i = 0; i < routes.Length; i++) CaptureRoute(i);
        }

        private void CaptureRoute(int index)
        {
            Transform route = routes[index];
            animators[index] = insects[index].GetComponentInChildren<Animator>(true);
            points[index] = new Vector3[route.childCount];
            poses[index] = new Quaternion[route.childCount];
            for (int j = 0; j < route.childCount; j++)
            {
                points[index][j] = route.GetChild(j).position;
                poses[index][j] = route.GetChild(j).rotation;
            }
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (points == null) return;
            for (int i = 0; i < insects.Length; i++)
            {
                insects[i].SetPositionAndRotation(points[i][0], poses[i][0]);
                insects[i].gameObject.SetActive(true);
                if (animators[i] != null) animators[i].Play(IdleState, 0, 0f);
            }
        }

        private void Update()
        {
            if (!IsActive || points == null || player == null) return;
            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            if (!HasScattered && offset.sqrMagnitude <= activationDistance * activationDistance) HasScattered = true;
            if (!HasScattered) return;
            elapsed += Time.deltaTime;
            for (int i = 0; i < insects.Length; i++) AdvanceInsect(i);
        }

        private void AdvanceInsect(int index)
        {
            Transform insect = insects[index];
            if (insect == null || !insect.gameObject.activeSelf || elapsed < (index % 7) * 0.045f) return;
            int next = segments[index] + 1;
            if (!running[index])
            {
                running[index] = true;
                if (animators[index] != null) animators[index].CrossFadeInFixedTime(RunState, 0.05f, 0);
            }
            float speed = crawlSpeed * (0.8f + (index % 5) * 0.13f);
            insect.position = Vector3.MoveTowards(insect.position, points[index][next], speed * Time.deltaTime);
            insect.rotation = Quaternion.RotateTowards(insect.rotation, poses[index][next], 720f * Time.deltaTime);
            if ((insect.position - points[index][next]).sqrMagnitude > 0.00001f) return;
            segments[index] = next;
            if (next == points[index].Length - 1) insect.gameObject.SetActive(false);
        }

        protected override void OnResetAnomaly()
        {
            HasScattered = false;
            elapsed = 0f;
            for (int i = 0; i < insects.Length; i++) ResetInsect(i);
        }

        private void ResetInsect(int index)
        {
            if (insects[index] == null) return;
            insects[index].gameObject.SetActive(false);
            if (points == null) return;
            segments[index] = 0;
            running[index] = false;
            insects[index].SetPositionAndRotation(points[index][0], poses[index][0]);
        }

        private void OnDisable() => ResetAnomaly();
    }
}
