using UnityEngine;
using UnityEngine.AI;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class ArchiveAssemblyPresentation : MonoBehaviour
    {
        [SerializeField] private Transform[] pieces;
        [SerializeField] private Transform[] assemblyPoses;
        [SerializeField] private Rigidbody[] bodies;
        [SerializeField] private GameObject circle;
        [SerializeField] private Animator actor;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Transform focus;
        [SerializeField, Min(0.1f)] private float chaseSpeed = 3.8f;
        private static readonly int Crawl = Animator.StringToHash("Running Crawl");
        private static readonly int Attack = Animator.StringToHash("Attack");
        private Vector3[] starts, phaseStarts;
        private Quaternion[] rotations, phaseRotations;
        private float[] heights, routeLengths;
        private Vector3[][] routes;
        private Vector3 actorStart;
        private Quaternion actorRotation;
        private float nextRepath;
        public Vector3 FocusPoint => focus != null ? focus.position : transform.position;
        public bool IsConfigured => actor != null && agent != null && circle != null && focus != null &&
            pieces != null && pieces.Length > 0 && assemblyPoses != null && bodies != null &&
            pieces.Length == assemblyPoses.Length && bodies.Length == pieces.Length && ReferencesValid();

        private bool ReferencesValid()
        {
            for (int i = 0; i < pieces.Length; i++)
                if (pieces[i] == null || assemblyPoses[i] == null || bodies[i] == null) return false;
            return true;
        }

        public void Capture()
        {
            if (starts != null || !IsConfigured) return;
            int count = pieces.Length;
            starts = new Vector3[count];
            rotations = new Quaternion[count];
            phaseStarts = new Vector3[count];
            phaseRotations = new Quaternion[count];
            heights = new float[count];
            routes = new Vector3[count][];
            routeLengths = new float[count];
            for (int i = 0; i < count; i++)
            {
                starts[i] = pieces[i].localPosition;
                rotations[i] = pieces[i].localRotation;
                heights[i] = pieces[i].GetComponent<Renderer>().bounds.extents.y;
            }
            actorStart = actor.transform.position;
            actorRotation = actor.transform.rotation;
        }

        public void Scatter()
        {
            Capture();
            if (starts == null) return;
            circle.SetActive(true);
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i].gameObject.SetActive(true);
                bodies[i].isKinematic = false;
                bodies[i].useGravity = true;
                bodies[i].WakeUp();
            }
        }

        public bool BeginCrawlTogether()
        {
            if (starts == null) return false;
            for (int i = 0; i < pieces.Length; i++)
            {
                StopBody(bodies[i]);
                float angle = i * Mathf.PI * 2f / pieces.Length;
                Vector3 end = actorStart + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.35f;
                if (!Route(i, end)) return false;
                phaseRotations[i] = pieces[i].rotation;
            }
            return true;
        }

        private bool Route(int index, Vector3 end)
        {
            if (!NavMesh.SamplePosition(pieces[index].position, out NavMeshHit startHit, 2f, NavMesh.AllAreas)) return false;
            if (!NavMesh.SamplePosition(end, out NavMeshHit endHit, 1f, NavMesh.AllAreas)) return false;
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, path)) return false;
            if (path.status != NavMeshPathStatus.PathComplete) return false;
            routes[index] = path.corners;
            float length = 0f;
            for (int i = 1; i < routes[index].Length; i++) length += Vector3.Distance(routes[index][i - 1], routes[index][i]);
            routeLengths[index] = length;
            return routes[index].Length > 0;
        }

        public void CrawlTogether(float progress)
        {
            for (int i = 0; i < pieces.Length; i++)
            {
                float t = Mathf.Clamp01(progress * 1.15f - i * 0.018f);
                Vector3 position = RoutePoint(i, t);
                position.y += heights[i] + 0.012f;
                pieces[i].position = position;
                pieces[i].rotation = phaseRotations[i] * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 42f + i) * 2f);
            }
        }

        private Vector3 RoutePoint(int index, float progress)
        {
            Vector3[] points = routes[index];
            float remaining = routeLengths[index] * progress;
            for (int i = 1; i < points.Length; i++)
            {
                float length = Vector3.Distance(points[i - 1], points[i]);
                if (remaining <= length) return Vector3.Lerp(points[i - 1], points[i], length > 0f ? remaining / length : 1f);
                remaining -= length;
            }
            return points[points.Length - 1];
        }

        public void BeginAssembly()
        {
            for (int i = 0; i < pieces.Length; i++)
            {
                phaseStarts[i] = pieces[i].position;
                phaseRotations[i] = pieces[i].rotation;
            }
        }

        public void Assemble(float progress)
        {
            float t = Mathf.SmoothStep(0f, 1f, progress);
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i].position = Vector3.Lerp(phaseStarts[i], assemblyPoses[i].position, t);
                pieces[i].rotation = Quaternion.Slerp(phaseRotations[i], assemblyPoses[i].rotation, t);
            }
        }

        public bool BeginChase()
        {
            if (!NavMesh.SamplePosition(actorStart, out NavMeshHit hit, 1.2f, NavMesh.AllAreas)) return false;
            foreach (Transform piece in pieces) piece.gameObject.SetActive(false);
            actor.transform.SetPositionAndRotation(hit.position, actorRotation);
            actor.gameObject.SetActive(true);
            actor.enabled = true;
            actor.applyRootMotion = false;
            actor.Rebind();
            actor.Play(Crawl, 0, 0f);
            agent.enabled = true;
            if (!agent.Warp(hit.position)) return false;
            agent.speed = chaseSpeed;
            agent.isStopped = false;
            nextRepath = 0f;
            return agent.isOnNavMesh;
        }

        public bool Pursue(Transform observer)
        {
            if (!agent.enabled || !agent.isOnNavMesh) return false;
            if (Time.time >= nextRepath)
            {
                nextRepath = Time.time + 0.18f;
                agent.SetDestination(observer.root.position);
            }
            Vector3 offset = observer.root.position - actor.transform.position;
            if (Mathf.Abs(offset.y) > 1.5f) return false;
            offset.y = 0f;
            if (offset.sqrMagnitude > 0.85f * 0.85f) return false;
            return !Physics.Linecast(FocusPoint, observer.position, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)
                || hit.transform.IsChildOf(observer.root);
        }

        public void AttackPlayer(Transform observer)
        {
            StopAgent();
            Vector3 direction = observer.position - actor.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f) actor.transform.rotation = Quaternion.LookRotation(direction);
            actor.CrossFadeInFixedTime(Attack, 0.08f, 0, 0f);
        }

        private void StopAgent()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.ResetPath();
            if (agent != null) agent.enabled = false;
        }

        private static void StopBody(Rigidbody body)
        {
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.isKinematic = true;
            body.useGravity = false;
        }

        public void Restore()
        {
            StopAgent();
            if (actor != null) actor.gameObject.SetActive(false);
            if (circle != null) circle.SetActive(false);
            if (starts == null) return;
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i] == null || bodies[i] == null) continue;
                StopBody(bodies[i]);
                pieces[i].SetLocalPositionAndRotation(starts[i], rotations[i]);
                bodies[i].position = pieces[i].position;
                bodies[i].rotation = pieces[i].rotation;
                pieces[i].gameObject.SetActive(false);
                routes[i] = null;
            }
            if (actor != null) actor.transform.SetPositionAndRotation(actorStart, actorRotation);
        }

        private void OnDisable() => Restore();
    }
}
