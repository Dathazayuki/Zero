using System;
using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class KitchenRatSwarmActor : MonoBehaviour
    {
        [SerializeField] private Transform[] rats = Array.Empty<Transform>();
        [SerializeField] private Transform[] routes = Array.Empty<Transform>();
        [SerializeField, Min(0.1f)] private float runSpeed = 2.8f;
        [SerializeField, Min(0.05f)] private float contactRadius = 0.52f;
        [SerializeField, Min(0f)] private float spawnInterval = 0.025f;
        [SerializeField, Min(1f)] private float turnSpeed = 720f;
        private Vector3[][] points;
        private Animator[] animators;
        private int[] segments;
        private float[] segmentTimes;
        private float elapsed;
        private bool trampling;
        private static readonly int Run = Animator.StringToHash("Run");
        public bool Finished { get; private set; }
        public bool IsConfigured => rats.Length > 8 && rats.Length == routes.Length;

        public void Capture()
        {
            if (points != null || !IsConfigured) return;
            points = new Vector3[rats.Length][];
            animators = new Animator[rats.Length]; segments = new int[rats.Length];
            segmentTimes = new float[rats.Length];
            for (int i = 0; i < rats.Length; i++)
            {
                points[i] = new Vector3[routes[i].childCount];
                for (int j = 0; j < points[i].Length; j++) points[i][j] = routes[i].GetChild(j).position;
                animators[i] = rats[i].GetComponentInChildren<Animator>(true);
            }
        }
        public void Begin() { Capture(); ResetSwarm(); }
        public bool Tick(float delta, Vector3 player)
        {
            if (points == null) return false;
            elapsed += delta; bool hit = false; Finished = true;
            int count = trampling ? rats.Length - 8 : rats.Length;
            for (int i = 0; i < count; i++)
            {
                if (segments[i] == points[i].Length - 1) continue;
                Finished = false;
                if (elapsed < i * spawnInterval) continue;
                hit |= Advance(i, player, delta);
            }
            return hit;
        }
        private bool Advance(int index, Vector3 player, float delta)
        {
            Transform rat = rats[index];
            if (!rat.gameObject.activeSelf)
            {
                rat.rotation = MovementRotation(points[index][1] - points[index][0], Vector3.right);
                rat.gameObject.SetActive(true);
                if (animators[index] != null) animators[index].Play(Run, 0, 0f);
            }
            Vector3 before = rat.position;
            Vector3 target = points[index][segments[index] + 1];
            MoveRat(index, target, delta);
            OrientRat(index, rat.position - before, delta);
            bool hit = Touches(before, rat.position, player, contactRadius);
            if ((rat.position - target).sqrMagnitude < 0.00001f)
            { segments[index]++; segmentTimes[index] = 0f; }
            if (segments[index] == points[index].Length - 1) rat.gameObject.SetActive(false);
            return hit;
        }
        private void OrientRat(int index, Vector3 movement, float delta)
        {
            Vector3 start = points[index][segments[index]];
            Vector3 target = points[index][segments[index] + 1];
            bool descending = start.y - target.y > 0.05f;
            if (!descending) movement.y = 0f;
            if (movement.sqrMagnitude < 0.000001f) return;
            Transform rat = rats[index];
            Quaternion desired = descending ? MovementRotation(movement, rat.right) : Quaternion.LookRotation(movement);
            rat.rotation = Quaternion.RotateTowards(rat.rotation, desired, turnSpeed * delta);
        }
        public static Quaternion MovementRotation(Vector3 movement, Vector3 right)
        {
            Vector3 forward = movement.normalized;
            Vector3 up = Vector3.Cross(forward, right);
            if (up.sqrMagnitude < 0.0001f) up = Mathf.Abs(forward.y) > 0.95f ? Vector3.forward : Vector3.up;
            return Quaternion.LookRotation(forward, up.normalized);
        }
        private void MoveRat(int index, Vector3 target, float delta)
        {
            Vector3 start = points[index][segments[index]];
            float height = start.y - target.y;
            if (height > 0.3f)
            {
                segmentTimes[index] += delta;
                float duration = Mathf.Sqrt(2f * height / 9.81f);
                rats[index].position = DropPosition(start, target, Mathf.Clamp01(segmentTimes[index] / duration));
            }
            else rats[index].position = Vector3.MoveTowards(rats[index].position, target,
                runSpeed * (0.96f + index % 4 * 0.025f) * delta);
        }
        public static Vector3 DropPosition(Vector3 start, Vector3 end, float progress)
        {
            float t = Mathf.Clamp01(progress);
            Vector3 point = Vector3.Lerp(start, end, t);
            float horizontal = Vector3.ProjectOnPlane(end - start, Vector3.up).magnitude;
            point.y = Mathf.Lerp(start.y, end.y, t * t);
            if (horizontal > 0.3f) point.y += Mathf.Sin(t * Mathf.PI) * 0.18f;
            return point;
        }
        public static bool Touches(Vector3 start, Vector3 end, Vector3 player, float radius)
        {
            if (Mathf.Min(start.y, end.y) > player.y + 0.65f || Mathf.Max(start.y, end.y) < player.y - 0.4f) return false;
            start.y = end.y = player.y;
            Vector3 line = end - start;
            float t = line.sqrMagnitude > 0.000001f ? Mathf.Clamp01(Vector3.Dot(player - start, line) / line.sqrMagnitude) : 0f;
            return (start + line * t - player).sqrMagnitude <= radius * radius;
        }
        public void BeginTrampling()
        {
            trampling = true;
            for (int i = rats.Length - 8; i < rats.Length; i++)
            { rats[i].gameObject.SetActive(true); if (animators[i] != null) animators[i].Play(Run, 0, 0f); }
        }
        public void Trample(Vector3 position, Vector3 forward, float time)
        {
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            for (int i = 0; i < 8; i++)
            {
                float t = (time * 1.8f + i * 0.19f) % 1f;
                Vector3 point = position + forward * (0.4f + (i % 3) * 0.2f) + right * Mathf.Lerp(-1.2f, 1.2f, t);
                point.y = points[0][points[0].Length - 1].y;
                rats[rats.Length - 8 + i].SetPositionAndRotation(point, Quaternion.LookRotation(right));
            }
        }
        public void ResetSwarm()
        {
            elapsed = 0f; trampling = false; Finished = false;
            for (int i = 0; i < rats.Length; i++)
            {
                if (rats[i] == null) continue;
                rats[i].gameObject.SetActive(false);
                if (points == null) continue;
                segments[i] = 0; segmentTimes[i] = 0f; rats[i].position = points[i][0];
            }
        }
    }
}
