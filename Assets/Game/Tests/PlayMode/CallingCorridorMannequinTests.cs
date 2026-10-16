#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Flow;
using Zero.AnomalyPrototype.Player;

namespace Zero.Tests.PlayMode
{
    public sealed class CallingCorridorMannequinTests
    {
        private CallingCorridorSession session;
        private CallingCorridorThreat threat;
        private FirstPersonController player;
        private CallingCorridorGate gate;

        [UnitySetUp]
        public IEnumerator LoadDemo()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Game/Scenes/Demos/Minigame3_CallingCorridor_Demo.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            session = Object.FindFirstObjectByType<CallingCorridorSession>();
            threat = Object.FindFirstObjectByType<CallingCorridorThreat>();
            player = Object.FindFirstObjectByType<FirstPersonController>();
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!session.IsPlaying && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(session.IsPlaying, Is.True);
            Assert.That(threat.IsConfigured, Is.True);
            player.enabled = false;
            gate = Read<CallingCorridorGate[]>(session, "gates")[0];
            Write(threat, "warningSeconds", 0.1f);
            Write(threat, "minimumActiveSeconds", 3f);
        }

        [UnityTest]
        public IEnumerator FourthChoiceStartsMannequinOnceAndResetRearmsIt()
        {
            yield return ReachFourthChoice();
            Assert.That(threat.IsActive, Is.True);
            Assert.That(Read<GameObject>(threat, "visualRoot").activeSelf, Is.True);
            Assert.That(threat.GetComponent<NavMeshAgent>().isStopped, Is.False);
            Assert.That(Read<Animator>(threat, "animator").speed, Is.GreaterThan(0f));
            Assert.That(session.TryStartThreat(), Is.False);
            session.ResetAttempt();
            session.ResetAttempt();
            Assert.That(threat.IsActive, Is.False);
            Assert.That(threat.GetComponent<NavMeshAgent>().enabled, Is.False);
            yield return ReachFourthChoice();
            Assert.That(threat.IsActive, Is.True);
        }

        [UnityTest]
        public IEnumerator ShelteringPreventsCatchAndLetsMannequinPass()
        {
            yield return ReachFourthChoice();
            var shelter = Read<CallingCorridorSafeAlcove[]>(session, "alcoves")[1];
            BoxCollider volume = shelter.GetComponent<BoxCollider>();
            Vector3 feet = shelter.transform.TransformPoint(volume.center);
            feet.y = 0.04f;
            player.Teleport(feet, Quaternion.identity);
            Physics.SyncTransforms();
            Assert.That(session.IsSheltered, Is.True);
            Assert.That(session.TryCatch(), Is.False);
            yield return new WaitForSeconds(0.9f);
            Assert.That(threat.State, Is.EqualTo(CallingCorridorThreat.Motion.Passing));
            float deadline = Time.time + 14f;
            while (threat.IsActive && Time.time < deadline) yield return null;
            Assert.That(threat.IsActive, Is.False);
            Assert.That(session.Decision, Is.EqualTo(3));
            Assert.That(session.IsPlaying, Is.True);
        }

        [UnityTest]
        public IEnumerator CatchIsConsumedAndDisableDuringResetRestoresInput()
        {
            yield return ReachFourthChoice();
            Assert.That(session.TryCatch(), Is.True);
            Assert.That(session.TryCatch(), Is.False);
            yield return new WaitForSeconds(0.8f);
            Assert.That(session.Decision, Is.Zero);
            Assert.That(player.InputEnabled, Is.True);
            yield return ReachFourthChoice();
            Assert.That(session.TryCatch(), Is.True);
            session.enabled = false;
            yield return null;
            Assert.That(threat.IsActive, Is.False);
            Assert.That(player.InputEnabled, Is.True);
            Assert.That(Read<SceneFadeController>(session, "localFade").Alpha, Is.Zero);
        }

        private IEnumerator ReachFourthChoice()
        {
            CallingCorridorRoomEncounter[] rooms = Read<CallingCorridorRoomEncounter[]>(gate, "rooms");
            CallingCorridorDoor[] doors = Read<CallingCorridorDoor[]>(gate, "doors");
            for (int decision = 0; decision < 3; decision++)
            {
                Assert.That(threat.IsActive, Is.False);
                for (int i = 0; i < rooms.Length; i++)
                {
                    if (rooms[i].State != CallingCorridorRoomEncounter.OccupantState.RabbitWaiting) continue;
                    player.Teleport(rooms[i].transform.TransformPoint(new Vector3(-3.1f, 0.04f, 5.3f)), Quaternion.identity);
                    Assert.That(gate.TrySelectDoor(doors[i]), Is.True);
                    break;
                }
                Assert.That(threat.IsActive, Is.EqualTo(decision == 2),
                    "Start the fourth choice encounter immediately inside the correct room.");
                player.Teleport(new Vector3(0f, 0.04f, 0f), Quaternion.identity);
                Physics.SyncTransforms();
                yield return null;
                yield return null;
            }
            Assert.That(session.Decision, Is.EqualTo(3));
        }

        private static T Read<T>(object target, string field) =>
            (T)target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);

        private static void Write(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

        [UnityTest]
        public IEnumerator EachNewChoiceRestartsMovingEncounterBeforeReturningToHall()
        {
            yield return ReachFourthChoice();
            var rooms = Read<CallingCorridorRoomEncounter[]>(gate, "rooms");
            var doors = Read<CallingCorridorDoor[]>(gate, "doors");
            for (int next = 4; next <= 6; next++)
            {
                Write(threat, "activeElapsed", 1f);
                for (int i = 0; i < rooms.Length; i++)
                {
                    if (rooms[i].State != CallingCorridorRoomEncounter.OccupantState.RabbitWaiting) continue;
                    player.Teleport(rooms[i].transform.TransformPoint(new Vector3(-3.1f, 0.04f, 5.3f)), Quaternion.identity);
                    Assert.That(gate.TrySelectDoor(doors[i]), Is.True);
                    break;
                }
                Assert.That(session.Decision, Is.EqualTo(next));
                Assert.That(threat.IsActive, Is.True);
                Assert.That(Read<float>(threat, "activeElapsed"), Is.Zero);
                Assert.That(threat.GetComponent<NavMeshAgent>().isStopped, Is.False);
                Assert.That(Read<Animator>(threat, "animator").speed, Is.EqualTo(1f));
                Assert.That(session.TryStartThreat(), Is.False);
                player.Teleport(new Vector3(0f, 0.04f, 0f), Quaternion.identity);
                Physics.SyncTransforms();
                yield return null;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ShelteredPlayerDoesNotMakeThreatDisappearAtFirstEndpoint()
        {
            yield return ReachFourthChoice();
            var shelter = Read<CallingCorridorSafeAlcove[]>(session, "alcoves")[1];
            var volume = shelter.GetComponent<BoxCollider>();
            Vector3 feet = shelter.transform.TransformPoint(volume.center);
            feet.y = 0.04f;
            player.Teleport(feet, Quaternion.identity);
            Physics.SyncTransforms();
            Write(threat, "minimumActiveSeconds", 20f);
            yield return new WaitForSeconds(0.9f);
            Assert.That(threat.State, Is.EqualTo(CallingCorridorThreat.Motion.Passing));
            NavMeshAgent agent = threat.GetComponent<NavMeshAgent>();
            Vector3 endpoint = Read<Vector3>(threat, "exit");
            Assert.That(agent.Warp(endpoint), Is.True);
            agent.ResetPath();
            Write(threat, "activeElapsed", 3f);
            Write(threat, "nextRepath", Time.time + 10f);
            Write(threat, "elapsed", 3f);
            yield return null;
            yield return null;
            Assert.That(threat.IsActive, Is.True);
            Assert.That(Read<Vector3>(threat, "passingTarget"), Is.EqualTo(Read<Vector3>(threat, "spawnPosition")));
            Assert.That(agent.isStopped, Is.False);
            Write(threat, "activeElapsed", 21f);
            Assert.That(agent.Warp(Read<Vector3>(threat, "passingTarget")), Is.True);
            agent.ResetPath();
            Write(threat, "nextRepath", Time.time + 10f);
            yield return null;
            yield return null;
            Assert.That(threat.IsActive, Is.False);
        }

        [UnityTest]
        public IEnumerator WarningPlaysWhileThreatAlreadyMoves()
        {
            Write(threat, "warningSeconds", 3f);
            yield return ReachFourthChoice();
            Vector3 start = threat.transform.position;
            yield return new WaitForSeconds(0.3f);
            Assert.That(threat.State, Is.EqualTo(CallingCorridorThreat.Motion.Warning));
            Assert.That(Vector3.Distance(start, threat.transform.position), Is.GreaterThan(0.05f));
            Assert.That(Read<Animator>(threat, "animator").speed, Is.EqualTo(1f));
        }
    }
}
#endif
