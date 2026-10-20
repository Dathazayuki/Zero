using System.Collections;
using UnityEngine;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class WatchfulHallThreatDirector : MonoBehaviour
    {
        [SerializeField] private WatchfulHallPatrol[] patrols;
        [SerializeField] private WatchfulHallWatcher[] watchers;
        [SerializeField] private WatchfulHallHidingSpots hiding;
        [SerializeField] private Transform entrance;
        [SerializeField] private SceneFadeController fade;
        [SerializeField, Range(0.1f, 0.6f)] private float catchLookSeconds = 0.35f;
        private WatchfulHallSession session;
        private WatchfulHallPatrol responder;
        private Coroutine replay;
        private bool previousInput;
        private bool ownsInput;
        private WatchfulHallPatrol catchAttacker;
        private Quaternion playerRotation;
        private Quaternion cameraRotation;
        private Quaternion cameraWorldRotation;
        private Quaternion attackerRotation;
        private float catchLookElapsed;
        public FirstPersonController Player { get; private set; }
        public WatchfulHallHidingSpots Hiding => hiding;
        public WatchfulHallPatrol Responder => responder;
        public bool IsReady { get; private set; }
        public bool IsPlaying => IsReady && isActiveAndEnabled && session != null && session.isActiveAndEnabled
            && !session.IsCompleted && replay == null;
        public int CatchCount { get; private set; }

        public bool Bind(WatchfulHallSession owner, FirstPersonController player)
        {
            session = owner;
            Player = player;
            if (fade != null) fade.SetImmediate(0f);
            IsReady = ReferencesReady();
            if (!IsReady)
            {
                StopActors();
                Debug.LogError("WATCHFUL_HALL_CONFIGURATION_INVALID: missing threat references", this);
                return false;
            }
            for (int i = 0; i < watchers.Length; i++) watchers[i].Bind(this);
            for (int i = 0; i < patrols.Length; i++)
                if (patrols[i] == null || !patrols[i].Bind(this)) IsReady = false;
            fade.SetImmediate(0f);
            if (!IsReady) Debug.LogError("WATCHFUL_HALL_CONFIGURATION_INVALID: patrol navigation or references", this);
            return IsReady;
        }

        private bool ReferencesReady() => patrols != null && patrols.Length == 2 && WatchersReady()
            && hiding != null && entrance != null && fade != null && Player != null;

        private bool WatchersReady()
        {
            if (watchers == null || watchers.Length == 0) return false;
            for (int i = 0; i < watchers.Length; i++)
            {
                if (watchers[i] == null || !watchers[i].IsConfigured) return false;
                for (int j = 0; j < i; j++)
                    if (watchers[j] == watchers[i]) return false;
            }
            return true;
        }

        public bool CanSee(Vector3 origin, Vector3 forward, float range, float angle)
        {
            if (Player == null || hiding.FindCabinet(Player.transform.position) != null) return false;
            Vector3 target = Player.transform.position + Vector3.up * 1.2f;
            Vector3 direction = target - origin;
            if (direction.sqrMagnitude > range * range) return false;
            Vector3 flat = Vector3.ProjectOnPlane(direction, Vector3.up);
            Vector3 facing = Vector3.ProjectOnPlane(forward, Vector3.up);
            if (Vector3.Angle(facing, flat) > angle * 0.5f) return false;
            return ClearLine(origin, target, Player.transform);
        }

        public bool ClearLine(Vector3 origin, Vector3 target, Transform targetRoot)
        {
            Vector3 direction = target - origin;
            if (direction.sqrMagnitude < 0.001f) return true;
            if (!Physics.Raycast(origin, direction.normalized, out RaycastHit hit, direction.magnitude, ~0, QueryTriggerInteraction.Ignore)) return true;
            return targetRoot != null && hit.transform.IsChildOf(targetRoot);
        }

        public WatchfulHallPatrol ReportWatcher(Vector3 lastSeen, WatchfulHallPatrol assigned)
        {
            if (!IsPlaying) return null;
            bool canKeep = assigned != null && assigned.isActiveAndEnabled
                && !float.IsInfinity(assigned.PathDistance(lastSeen));
            responder = canKeep ? assigned : NearestPatrol(lastSeen);
            if (responder != null && !responder.ReceiveReport(lastSeen)) responder = null;
            return responder;
        }

        private WatchfulHallPatrol NearestPatrol(Vector3 point)
        {
            WatchfulHallPatrol nearest = null;
            float best = float.PositiveInfinity;
            for (int i = 0; i < patrols.Length; i++)
            {
                if (!patrols[i].isActiveAndEnabled) continue;
                float distance = patrols[i].PathDistance(point);
                if (distance >= best) continue;
                best = distance;
                nearest = patrols[i];
            }
            return nearest;
        }

        public bool TryCatch(WatchfulHallPatrol attacker)
        {
            if (!IsPlaying || attacker == null) return false;
            CatchCount++;
            previousInput = Player.InputEnabled;
            playerRotation = Player.transform.rotation;
            if (Player.PlayerCamera != null)
            {
                cameraRotation = Player.PlayerCamera.transform.localRotation;
                cameraWorldRotation = Player.PlayerCamera.transform.rotation;
            }
            catchLookElapsed = 0f;
            catchAttacker = attacker;
            attackerRotation = attacker.transform.rotation;
            ownsInput = true;
            Player.SetCinematicLock(true);
            session.PauseCharging();
            StopActors();
            FaceCatch(0f);
            attacker.PlayAttack();
            replay = StartCoroutine(ReplayCatch());
            return true;
        }

        private IEnumerator ReplayCatch()
        {
            yield return new WaitForSeconds(0.65f);
            yield return fade.FadeToColor(Color.black, 1f, 0.2f);
            yield return new WaitForSeconds(0.15f);
            ReleaseInput();
            Player.Teleport(entrance.position, entrance.rotation);
            session.ResetAttempt();
        }

        private void LateUpdate()
        {
            if (!ownsInput) return;
            catchLookElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(catchLookElapsed / Mathf.Clamp(catchLookSeconds, 0.1f, 0.6f));
            FaceCatch(Mathf.SmoothStep(0f, 1f, progress));
        }

        private void FaceCatch(float progress)
        {
            if (Player == null || catchAttacker == null) return;
            Vector3 direction = Vector3.ProjectOnPlane(Player.transform.position - catchAttacker.transform.position, Vector3.up);
            if (direction.sqrMagnitude > 0.0001f)
            {
                catchAttacker.transform.rotation = Quaternion.LookRotation(direction);
                Player.transform.rotation = Quaternion.Slerp(playerRotation, Quaternion.LookRotation(-direction), progress);
            }
            if (Player.PlayerCamera == null) return;
            Transform view = Player.PlayerCamera.transform;
            Vector3 gaze = catchAttacker.AttackLookPoint - view.position;
            if (gaze.sqrMagnitude > 0.0001f)
                view.rotation = Quaternion.Slerp(cameraWorldRotation, Quaternion.LookRotation(gaze), progress);
        }

        public void ResetThreats()
        {
            StopAllCoroutines();
            replay = null;
            ReleaseInput();
            if (!IsReady) return;
            responder = null;
            for (int i = 0; i < watchers.Length; i++) watchers[i].ResetWatcher();
            for (int i = 0; i < patrols.Length; i++) patrols[i].ResetPatrol();
            fade.SetImmediate(0f);
            if (!isActiveAndEnabled) StopActors();
        }

        public void StopActors()
        {
            if (watchers != null)
                for (int i = 0; i < watchers.Length; i++)
                    if (watchers[i] != null) watchers[i].StopWatcher();
            if (patrols == null) return;
            for (int i = 0; i < patrols.Length; i++)
                if (patrols[i] != null) patrols[i].StopPatrol();
        }

        private void ReleaseInput()
        {
            if (!ownsInput) return;
            ownsInput = false;
            if (catchAttacker != null) catchAttacker.transform.rotation = attackerRotation;
            catchAttacker = null;
            if (Player == null) return;
            Player.transform.rotation = playerRotation;
            if (Player.PlayerCamera != null) Player.PlayerCamera.transform.localRotation = cameraRotation;
            Player.SetCinematicLock(false);
            Player.SetInputEnabled(previousInput);
        }

        public void Suspend()
        {
            StopAllCoroutines();
            replay = null;
            StopActors();
            ReleaseInput();
            if (fade != null) fade.SetImmediate(0f);
        }

        private void OnDisable() => Suspend();
    }
}
