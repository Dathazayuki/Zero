#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Demo;
using Zero.AnomalyPrototype.Player;
using Phase = Zero.AnomalyPrototype.Anomalies.CabaretRitualRules.Phase;

namespace Zero.Tests.PlayMode
{
    public sealed class CabaretEyeRitualAnomalySceneTests
    {
        private CabaretEyeRitualAnomaly anomaly;
        private CharacterController player;
        private CabaretRitualLighting lighting;

        [UnitySetUp]
        public IEnumerator LoadDemo()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Game/Scenes/Demos/InfinityRoom5_CrimsonCabaret_Demo.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            Object.FindFirstObjectByType<LivingRoomAnomalyPreview>().enabled = false;
            anomaly = Object.FindFirstObjectByType<CabaretEyeRitualAnomaly>();
            Assert.That(anomaly, Is.Not.Null);
            Assert.That(anomaly.IsConfigured, Is.True);
            player = Read<CharacterController>(anomaly, "player");
            player.GetComponent<FirstPersonController>().enabled = false;
            lighting = Read<CabaretRitualLighting>(anomaly, "lighting");
            Move(new Vector3(0f, .04f, -7.2f));
        }

        [UnityTest]
        public IEnumerator EntrySlamsDoorThenBlackoutRevealsTwoSpotsAndFlyingEyesAndResetRestoresRoom()
        {
            var room = ReadArray<Light>(lighting, "roomLights");
            var chair = Read<GameObject>(Read<CabaretRitualHost>(anomaly, "host"), "chair");
            Assert.That(chair.activeSelf, Is.False);
            float intensity = room[0].intensity;
            bool enabled = room[0].enabled;
            anomaly.Activate(); yield return null;
            Assert.That(anomaly.State, Is.EqualTo(Phase.Waiting));
            Move(new Vector3(0f, .04f, -5.9f)); yield return null; yield return null;
            Assert.That(anomaly.State, Is.EqualTo(Phase.Blackout));
            Assert.That(lighting.IsLocked, Is.True);
            Assert.That(Detail().activeSelf, Is.True);
            yield return WaitFor(Phase.Rising, 7f);
            Assert.That(chair.activeSelf, Is.True);
            Assert.That(Detail().activeSelf, Is.False);
            Assert.That(Read<Light>(lighting, "playerSpot").gameObject.activeSelf, Is.True);
            Assert.That(Read<Light>(lighting, "hostSpot").gameObject.activeSelf, Is.True);
            foreach (Light light in room) Assert.That(light.enabled, Is.False);
            var swarm = Read<CabaretRitualSwarm>(anomaly, "swarm");
            var eyes = ReadArray<Animator>(swarm, "eyes");
            Vector3 start = eyes[0].transform.position;
            yield return new WaitForSeconds(.6f);
            Assert.That(eyes[0].transform.position.y, Is.GreaterThan(start.y));
            Assert.That(eyes[0].GetCurrentAnimatorStateInfo(0).shortNameHash, Is.EqualTo(Animator.StringToHash("fly")));
            anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
            Assert.That(lighting.IsLocked, Is.False);
            Assert.That(chair.activeSelf, Is.False);
            Assert.That(Detail().activeSelf, Is.True);
            Assert.That(room[0].intensity, Is.EqualTo(intensity)); Assert.That(room[0].enabled, Is.EqualTo(enabled));
            foreach (Animator eye in eyes) Assert.That(eye.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator BaitingThreeLockedStrikesClosesEyesDropsSwarmAndUnlocksExit()
        {
            anomaly.Activate(); Move(new Vector3(0f, .04f, -5.9f));
            yield return WaitFor(Phase.Active, 9f);
            var targets = ReadArray<Renderer>(lighting, "targetEyes");
            for (int i = 0; i < 3; i++)
            {
                float timeout = Time.time + 9f;
                Vector3 previous = targets[i].transform.position;
                float travelled = 0f;
                while (anomaly.State == Phase.Active && Time.time < timeout)
                {
                    Move(targets[i].transform.position); yield return null;
                    travelled += Vector3.Distance(previous, targets[i].transform.position);
                    previous = targets[i].transform.position;
                }
                Assert.That(travelled, i == 0 ? Is.LessThan(.001f) : Is.GreaterThan(.02f));
                Assert.That(anomaly.State, Is.EqualTo(Phase.Warning));
                Vector3 locked = anomaly.SafeCentre;
                Move(locked + Vector3.right * 3.2f);
                yield return WaitFor(Phase.Attacking, 2f);
                Assert.That(Vector3.Distance(locked, anomaly.SafeCentre), Is.LessThan(.001f));
                yield return new WaitForSeconds(.32f);
                Assert.That((anomaly.StabilizedMask & (1 << i)) != 0, Is.True);
                if (i < 2) yield return WaitFor(Phase.Active, 2f);
            }
            yield return WaitFor(Phase.Safe, 4f);
            Assert.That(Detail().activeSelf, Is.True);
            Assert.That(anomaly.StabilizedMask, Is.EqualTo(7));
            Assert.That(lighting.IsLocked, Is.False);
            foreach (Renderer target in targets) Assert.That(target.gameObject.activeSelf, Is.False);
            var host = Read<Animator>(Read<CabaretRitualHost>(anomaly, "host"), "actor");
            Assert.That(host.GetCurrentAnimatorStateInfo(0).shortNameHash, Is.EqualTo(Animator.StringToHash("Sitting")));
            Assert.That(Read<GameObject>(Read<CabaretRitualSwarm>(anomaly, "swarm"), "wallEyes").activeSelf, Is.False);
            anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
            foreach (Renderer target in targets)
                Assert.That(target.transform.localScale.y, Is.EqualTo(2.4f).Within(.001f));
        }

        [UnityTest]
        public IEnumerator StayingInRedStrikeTriggersLasersAndDeathReleasesDoor()
        {
            anomaly.Activate(); Move(new Vector3(0f, .04f, -5.9f));
            yield return WaitFor(Phase.Active, 9f);
            yield return WaitFor(Phase.Warning, 7f);
            Move(anomaly.SafeCentre);
            yield return WaitFor(Phase.Attacking, 2f);
            var beams = ReadArray<LineRenderer>(Read<CabaretRitualSwarm>(anomaly, "swarm"), "lasers");
            Assert.That(beams[0].enabled, Is.True);
            Assert.That(Vector3.Distance(beams[0].GetPosition(1), anomaly.SafeCentre + Vector3.up * .12f), Is.LessThan(.001f));
            yield return new WaitForSeconds(.35f);
            Assert.That(lighting.IsLocked, Is.False);
            Assert.That(Read<PlayerDeathController>(anomaly, "death").IsDeathInProgress, Is.True);
        }

        private IEnumerator WaitFor(Phase state, float seconds)
        {
            float limit = Time.time + seconds;
            while (anomaly.State != state && Time.time < limit) yield return null;
            Assert.That(anomaly.State, Is.EqualTo(state));
        }

        [UnityTest]
        public IEnumerator EyeRoamsWithDistantPlayerButCannotEnterStageAndResetRestoresDetail()
        {
            anomaly.Activate(); Move(new Vector3(0f, .04f, -5.9f));
            yield return WaitFor(Phase.Active, 9f);
            var eye = ReadArray<Renderer>(lighting, "targetEyes")[0].GetComponent<CabaretRitualEye>();
            BoxCollider area = Read<BoxCollider>(eye, "movementArea");
            Vector3 home = eye.transform.position;
            float furthest = 0f;
            for (int i = 0; i < 300; i++)
            {
                eye.Present(.05f, .65f, false, new Vector3(50f, home.y, 50f));
                furthest = Mathf.Max(furthest, Vector3.Distance(home, eye.transform.position));
                Assert.That(area.bounds.Contains(eye.transform.position), Is.True);
            }
            Assert.That(furthest, Is.GreaterThan(2.2f));
            Assert.That(eye.transform.position.z, Is.LessThanOrEqualTo(3.55f + .001f));
            anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
            Assert.That(Detail().activeSelf, Is.True);
            Assert.That(Vector3.Distance(home, eye.transform.position), Is.LessThan(.001f));
        }

        private GameObject Detail() => Read<GameObject>(Read<CabaretRitualSwarm>(anomaly, "swarm"), "roomDetail");

        [UnityTest]
        public IEnumerator BothLivingEyesKeepMovingWhilePlayerPursuesOne()
        {
            anomaly.Activate(); Move(new Vector3(0f, .04f, -5.9f));
            yield return WaitFor(Phase.Active, 9f);
            var targets = ReadArray<Renderer>(lighting, "targetEyes");
            Vector3 previousNear = targets[1].transform.position, previousFar = targets[2].transform.position;
            float nearTravel = 0f, farTravel = 0f;
            for (int i = 0; i < 160; i++)
            {
                Vector3 pursuer = targets[1].transform.position + Vector3.forward;
                lighting.PresentTargets(.05f, i * .05f, 1, Phase.Active, pursuer);
                nearTravel += Vector3.Distance(previousNear, targets[1].transform.position);
                farTravel += Vector3.Distance(previousFar, targets[2].transform.position);
                previousNear = targets[1].transform.position; previousFar = targets[2].transform.position;
            }
            Assert.That(nearTravel, Is.GreaterThan(.1f));
            Assert.That(farTravel, Is.GreaterThan(.1f));
            anomaly.ResetAnomaly();
        }

        [UnityTest]
        public IEnumerator RemainingEyesFleePlayerWhenLightIsOnOppositeSide()
        {
            anomaly.Activate(); Move(new Vector3(0f, .04f, -5.9f));
            yield return WaitFor(Phase.Active, 9f);
            var eye = ReadArray<Renderer>(lighting, "targetEyes")[1].transform;
            Vector3 playerPosition = eye.position + Vector3.forward;
            Move(playerPosition);
            lighting.Track(20f, eye.position - Vector3.forward * 4f, Phase.Active, 1f);
            float initialDistance = Vector3.Distance(eye.position, playerPosition);
            for (int i = 0; i < 120; i++)
                lighting.PresentTargets(.05f, i * .05f, 1, Phase.Active, playerPosition);
            Assert.That(Vector3.Distance(eye.position, playerPosition), Is.GreaterThan(initialDistance + .5f));
            anomaly.ResetAnomaly();
        }

        private void Move(Vector3 point)
        {
            player.enabled = false; player.transform.position = point; player.enabled = true; Physics.SyncTransforms();
        }

        private static T Read<T>(Object source, string field) where T : Object
            => (T)new SerializedObject(source).FindProperty(field).objectReferenceValue;

        private static T[] ReadArray<T>(Object source, string field) where T : Object
        {
            var array = new SerializedObject(source).FindProperty(field);
            var result = new T[array.arraySize];
            for (int i = 0; i < result.Length; i++) result[i] = (T)array.GetArrayElementAtIndex(i).objectReferenceValue;
            return result;
        }
    }
}
#endif
