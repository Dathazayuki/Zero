using System.Collections;
using UnityEngine;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
    public sealed class CallingCorridorRoomEncounter : MonoBehaviour
    {
        public enum OccupantState { Dormant, RabbitWaiting, SpiderWaiting, Fleeing, Attacking, Consumed }
        [SerializeField] private GameObject rabbitPrefab;
        [SerializeField] private GameObject spiderPrefab;
        [SerializeField] private Transform rabbitSpawn;
        [SerializeField, Tooltip("Spider spawn position and rotation. Rotate this marker to author the waiting pose.")]
        private Transform spiderSpawn;
        [SerializeField] private Transform rabbitEscape;
        [SerializeField, Min(0.2f)] private float fleeSeconds = 1.2f;
        [SerializeField, Min(0.2f)] private float attackSeconds = 0.65f;
        private static readonly int RabbitMove = Animator.StringToHash("Move");
        private const string SpiderIdle = "CHsk|CHsk|CHsk|Idle";
        private const string SpiderWalk = "CHsk|CHsk|CHsk|walk";
        private CallingCorridorGate gate;
        private CallingCorridorDoor door;
        private GameObject rabbit;
        private GameObject spider;
        private Animator rabbitAnimator;
        private Animation spiderAnimation;
        private Transform spiderHead;
        private BoxCollider entryVolume;
        private Vector3 attackFacePosition;
        private Vector3 attackCameraPosition;
        private bool animationsReady;
        public OccupantState State { get; private set; }
        public bool IsConsumed => State == OccupantState.Fleeing || State == OccupantState.Attacking ||
            State == OccupantState.Consumed;
        public bool IsConfigured => rabbitPrefab != null && spiderPrefab != null && rabbitSpawn != null &&
            spiderSpawn != null && rabbitEscape != null &&
            (!Application.isPlaying || animationsReady);
        public Transform AttackTarget => spiderHead != null ? spiderHead : spiderSpawn;

        private void Awake() => TryGetComponent(out entryVolume);

        public bool Bind(CallingCorridorGate owner, CallingCorridorDoor choice)
        {
            gate = owner;
            door = choice;
            if (rabbitPrefab == null || spiderPrefab == null || rabbitSpawn == null || spiderSpawn == null || rabbitEscape == null) return false;
            if (rabbit == null) CreateActors();
            return animationsReady;
        }

        private void CreateActors()
        {
            rabbit = Instantiate(rabbitPrefab, transform);
            spider = Instantiate(spiderPrefab, transform);
            rabbit.name = "RuntimeRabbit";
            spider.name = "RuntimeSpiderHorror";
            rabbitAnimator = rabbit.GetComponentInChildren<Animator>(true);
            spiderAnimation = spider.GetComponentInChildren<Animation>(true);
            spiderHead = spider.transform.Find("CHsk/HeadBase");
            DisableActorColliders(rabbit);
            DisableActorColliders(spider);
            if (rabbitAnimator != null && rabbitAnimator.runtimeAnimatorController != null)
            {
                rabbit.SetActive(true);
                rabbitAnimator.applyRootMotion = false;
                rabbitAnimator.Rebind();
                rabbitAnimator.Update(0f);
            }
            if (spiderAnimation != null) spiderAnimation.playAutomatically = false;
            animationsReady = entryVolume != null && rabbitAnimator != null && rabbitAnimator.HasState(0, RabbitMove) &&
                spiderHead != null && spiderAnimation != null && spiderAnimation[SpiderIdle] != null && spiderAnimation[SpiderWalk] != null;
            ResetEncounter();
        }

        private static void DisableActorColliders(GameObject actor)
        {
            foreach (Collider body in actor.GetComponentsInChildren<Collider>(true)) body.enabled = false;
        }

        public void Configure(bool hasRabbit)
        {
            ResetEncounter();
            if (!animationsReady || rabbit == null || spider == null) return;
            rabbit.transform.SetPositionAndRotation(rabbitSpawn.position, rabbitSpawn.rotation);
            spider.transform.SetPositionAndRotation(spiderSpawn.position, spiderSpawn.rotation);
            rabbit.SetActive(hasRabbit);
            spider.SetActive(!hasRabbit);
            State = hasRabbit ? OccupantState.RabbitWaiting : OccupantState.SpiderWaiting;
            if (hasRabbit)
            {
                rabbitAnimator.speed = 0f;
                rabbitAnimator.Play(RabbitMove, 0, 0f);
                rabbitAnimator.Update(0f);
            }
            else PlaySpider(SpiderIdle, true);
        }

        public bool TryEnter(Collider other)
        {
            if (gate == null || IsConsumed || !gate.IsPlayer(other)) return false;
            if (door == null) return gate.TryEnterExit(this);
            return gate.IsAvailable && gate.TrySelectDoor(door);
        }

        private void OnTriggerEnter(Collider other) => TryEnter(other);
        private void OnTriggerStay(Collider other) => TryEnter(other);

        public void BeginFlee()
        {
            if (State != OccupantState.RabbitWaiting) return;
            State = OccupantState.Fleeing;
            StartCoroutine(Flee());
        }

        public bool GreetAtExit()
        {
            if (door != null || State != OccupantState.RabbitWaiting) return false;
            rabbitAnimator.speed = 0f;
            State = OccupantState.Consumed;
            return true;
        }

        private IEnumerator Flee()
        {
            Vector3 start = rabbit.transform.position;
            Vector3 end = rabbitEscape.position;
            Vector3 forward = end - start;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.01f) rabbit.transform.rotation = Quaternion.LookRotation(forward);
            rabbitAnimator.speed = 1.7f;
            rabbitAnimator.Play(RabbitMove, 0, 0f);
            for (float time = 0f; time < fleeSeconds; time += Time.deltaTime)
            {
                rabbit.transform.position = Vector3.Lerp(start, end, time / fleeSeconds);
                yield return null;
            }
            rabbitAnimator.speed = 0f;
            rabbit.SetActive(false);
            State = OccupantState.Consumed;
        }

        public IEnumerator Attack(FirstPersonController player)
        {
            if (State != OccupantState.SpiderWaiting || player == null) yield break;
            State = OccupantState.Attacking;
            PlaySpider(SpiderWalk, true);
            Vector3 start = spiderHead.position;
            Vector3 end = player.PlayerCamera.transform.position + player.PlayerCamera.transform.forward * 0.65f;
            attackCameraPosition = player.PlayerCamera.transform.position;
            for (float time = 0f; time < attackSeconds; time += Time.deltaTime)
            {
                if (State != OccupantState.Attacking || spider == null || !isActiveAndEnabled) yield break;
                float progress = Mathf.SmoothStep(0f, 1f, time / attackSeconds);
                attackFacePosition = Vector3.Lerp(start, end, progress) + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * 0.3f);
                yield return null;
            }
            attackFacePosition = end;
            State = OccupantState.Consumed;
        }

        private void LateUpdate()
        {
            if (State != OccupantState.Attacking && State != OccupantState.Consumed) return;
            if (spider == null || spiderHead == null || !spider.activeInHierarchy) return;
            Vector3 direction = attackCameraPosition - attackFacePosition;
            if (direction.sqrMagnitude < 0.01f) return;
            // Legacy Walk rotates the head; correct the animated face after sampling, not the model's assumed +Z.
            spider.transform.rotation = Quaternion.FromToRotation(spiderHead.forward, direction) * spider.transform.rotation;
            spider.transform.position += attackFacePosition - spiderHead.position;
        }

        private void PlaySpider(string clip, bool loop)
        {
            spiderAnimation[clip].wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
            spiderAnimation.CrossFade(clip, 0.1f);
        }

        public void ResetEncounter()
        {
            StopAllCoroutines();
            if (rabbitAnimator != null) rabbitAnimator.speed = 0f;
            if (spiderAnimation != null) spiderAnimation.Stop();
            if (rabbit != null) rabbit.SetActive(false);
            if (spider != null) spider.SetActive(false);
            State = OccupantState.Dormant;
        }

        private void OnDisable() => ResetEncounter();
    }
}
