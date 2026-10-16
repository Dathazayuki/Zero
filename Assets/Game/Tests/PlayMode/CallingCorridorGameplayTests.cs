#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Flow;
using Zero.AnomalyPrototype.Player;

namespace Zero.Tests.PlayMode
{
    public sealed class CallingCorridorGameplayTests
    {
        private CallingCorridorSession session;
        private FirstPersonController player;
        private CallingCorridorGate gate;
        private CallingCorridorDoor[] doors;
        private CallingCorridorRoomEncounter[] rooms;

        [UnitySetUp]
        public IEnumerator LoadDemo()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Game/Scenes/Demos/Minigame3_CallingCorridor_Demo.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            session = Object.FindFirstObjectByType<CallingCorridorSession>();
            player = Object.FindFirstObjectByType<FirstPersonController>();
            Assert.That(session, Is.Not.Null);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!session.IsPlaying && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(session.IsPlaying, Is.True, "Save the configured scene before running.");
            player.enabled = false;
            gate = Read<CallingCorridorGate[]>(session, "gates")[0];
            doors = Read<CallingCorridorDoor[]>(gate, "doors");
            rooms = Read<CallingCorridorRoomEncounter[]>(gate, "rooms");
            Assert.That(doors.Length, Is.EqualTo(6));
            Assert.That(rooms.Length, Is.EqualTo(6));
        }

        [UnityTest]
        public IEnumerator AllSixDoorsOpenAndAcceptPhysicalWalkingIntoRooms()
        {
            for (int i = 0; i < doors.Length; i++)
            {
                session.ResetAttempt();
                Transform plane = Read<Transform>(doors[i], "crossingPlane");
                Transform leaf = Read<Transform>(doors[i], "movingLeaf");
                Transform hardware = Read<Transform>(doors[i], "movingHardware");
                Vector3 attachment = leaf.InverseTransformPoint(hardware.position);
                Quaternion closed = leaf.rotation;
                player.Teleport(plane.position - plane.forward * 1.5f - Vector3.up + Vector3.up * 0.04f, Quaternion.identity);
                Physics.SyncTransforms();
                yield return new WaitForSeconds(0.8f);
                Assert.That(Quaternion.Angle(closed, leaf.rotation), Is.InRange(85f, 95f));
                Assert.That(Vector3.Distance(attachment, leaf.InverseTransformPoint(hardware.position)), Is.LessThan(0.01f));
                yield return WalkTo(rooms[i].transform.TransformPoint(new Vector3(0f, 0.04f, 3.6f)));
                yield return WalkTo(rooms[i].transform.TransformPoint(new Vector3(-3.1f, 0.04f, 3.6f)));
                yield return WalkTo(rooms[i].transform.TransformPoint(new Vector3(-3.1f, 0.04f, 5.3f)));
                Assert.That(session.Decision != 0 || !session.IsPlaying, Is.True, "Room " + i + " did not consume entry.");
                yield return new WaitForSeconds(1.8f);
            }
        }

        [UnityTest]
        public IEnumerator RabbitEntryAdvancesImmediately_FleesAndConsumesDuplicateEntry()
        {
            int index = CorrectRoom();
            Enter(index);
            Assert.That(session.Decision, Is.EqualTo(1));
            Assert.That(rooms[index].TryEnter(player.GetComponent<CharacterController>()), Is.False);
            Assert.That(gate.TrySelectDoor(doors[index]), Is.False);
            GameObject rabbit = Read<GameObject>(rooms[index], "rabbit");
            Vector3 start = rabbit.transform.position;
            yield return new WaitForSeconds(0.4f);
            Assert.That(Vector3.Distance(start, rabbit.transform.position), Is.GreaterThan(0.1f));
            Assert.That(Read<Animator>(rooms[index], "rabbitAnimator").speed, Is.GreaterThan(0f));
            Assert.That(Read<GameObject>(rooms[index], "spider").activeSelf, Is.False);
            yield return new WaitForSeconds(1f);
            Assert.That(rabbit.activeSelf, Is.False);
            Assert.That(rooms[index].State, Is.EqualTo(CallingCorridorRoomEncounter.OccupantState.Consumed));
            TMP_Text board = Read<TMP_Text>(Read<CallingCorridorStation[]>(session, "stations")[1], "board");
            Assert.That(board.text, Does.Contain("1 / 7"));
        }

        [UnityTest]
        public IEnumerator SpiderLeapsThenResetsWholeMinigame_NotOnlyCurrentRound()
        {
            Enter(CorrectRoom());
            yield return ReturnToHall();
            int wrong = (CorrectRoom() + 1) % 6;
            Enter(wrong);
            Assert.That(player.InputEnabled, Is.False);
            GameObject spider = Read<GameObject>(rooms[wrong], "spider");
            Vector3 start = spider.transform.position;
            yield return new WaitForSeconds(0.65f);
            Assert.That(Vector3.Distance(start, spider.transform.position), Is.GreaterThan(0.5f));
            yield return new WaitForSeconds(1f);
            Assert.That(session.IsPlaying, Is.True);
            Assert.That(session.Decision, Is.Zero);
            Assert.That(player.InputEnabled, Is.True);
            Assert.That(Read<SceneFadeController>(session, "localFade").Alpha, Is.Zero);
            Transform checkpoint = Read<CallingCorridorStation[]>(session, "stations")[0].Checkpoint;
            Assert.That(Vector3.Distance(player.transform.position, checkpoint.position), Is.LessThan(0.2f));
        }

        [UnityTest]
        public IEnumerator ResetDuringFleeOrAttackCancelsCallbacksAndRestoresInput()
        {
            Enter(CorrectRoom());
            session.ResetAttempt();
            session.ResetAttempt();
            Enter((CorrectRoom() + 1) % 6);
            yield return new WaitForSeconds(0.6f);
            session.ResetAttempt();
            session.ResetAttempt();
            yield return new WaitForSeconds(1.6f);
            Assert.That(session.Decision, Is.Zero);
            Assert.That(session.IsPlaying, Is.True);
            Assert.That(player.InputEnabled, Is.True);
            Assert.That(Read<SceneFadeController>(session, "localFade").Alpha, Is.Zero);
            foreach (CallingCorridorRoomEncounter room in rooms)
                Assert.That(room.State, Is.EqualTo(CallingCorridorRoomEncounter.OccupantState.RabbitWaiting)
                    .Or.EqualTo(CallingCorridorRoomEncounter.OccupantState.SpiderWaiting));
        }

        [UnityTest]
        public IEnumerator SevenRabbitEntriesAndRealExitCompleteExactlyOnce()
        {
            int count = 0;
            System.Action callback = () => count++;
            session.Completed += callback;
            Assert.That(session.TryFinish(), Is.False);
            CallingCorridorRoomEncounter exitRoom = gate.ExitRoom;
            Assert.That(exitRoom.State, Is.EqualTo(CallingCorridorRoomEncounter.OccupantState.Dormant));
            for (int i = 0; i < 7; i++)
            {
                Enter(CorrectRoom());
                Assert.That(session.Decision, Is.EqualTo(i + 1));
                yield return ReturnToHall();
            }
            Assert.That(count, Is.Zero);
            Assert.That(exitRoom.State, Is.EqualTo(CallingCorridorRoomEncounter.OccupantState.RabbitWaiting));
            foreach (CallingCorridorRoomEncounter room in rooms)
                Assert.That(room.State, Is.EqualTo(CallingCorridorRoomEncounter.OccupantState.Dormant));
            player.Teleport(new Vector3(0f, 0.04f, 33f), Quaternion.identity);
            Physics.SyncTransforms();
            Transform exit = Read<Transform>(gate, "exitPlane");
            Vector3 beyondExit = exit.position + exit.forward;
            beyondExit.y = 0.04f;
            yield return WalkTo(beyondExit);
            Assert.That(session.State, Is.EqualTo(CallingCorridorSession.AttemptState.Completed));
            Assert.That(session.Decision, Is.EqualTo(7));
            Assert.That(exitRoom.TryEnter(player.GetComponent<CharacterController>()), Is.False);
            Assert.That(session.TryFinish(), Is.False);
            yield return AssertStationaryExitRabbit(exitRoom);
            session.ResetAttempt();
            session.Completed -= callback;
            Assert.That(count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DisableDuringAttackClearsActorsAndRestoresCameraInput()
        {
            Quaternion original = player.PlayerCamera.transform.localRotation;
            Enter((CorrectRoom() + 1) % 6);
            yield return new WaitForSeconds(0.6f);
            session.enabled = false;
            yield return new WaitForSeconds(1f);
            Assert.That(player.InputEnabled, Is.True);
            Assert.That(Quaternion.Angle(original, player.PlayerCamera.transform.localRotation), Is.LessThan(0.1f));
            Assert.That(Read<SceneFadeController>(session, "localFade").Alpha, Is.Zero);
            foreach (CallingCorridorRoomEncounter room in rooms)
                Assert.That(room.State, Is.EqualTo(CallingCorridorRoomEncounter.OccupantState.Dormant));
        }

        [UnityTest]
        public IEnumerator MissingReferencesFailSafeWithoutInputLock()
        {
            GameObject invalid = new GameObject("Invalid Calling Corridor");
            invalid.SetActive(false);
            var broken = invalid.AddComponent<CallingCorridorSession>();
            LogAssert.Expect(LogType.Error, "CALLING_CORRIDOR_CONFIGURATION_INVALID: references or baked navigation missing");
            yield return broken.Prepare();
            Assert.That(broken.IsReady, Is.False);
            Assert.That(broken.TryPassage(0), Is.False);
            Assert.That(player.InputEnabled, Is.True);
            Object.Destroy(invalid);
        }

        [UnityTest]
        public IEnumerator SpiderUsesAuthoredSpawnRotationAndFacesCameraDuringLeap()
        {
            CallingCorridorRoomEncounter room = rooms[(CorrectRoom() + 1) % 6];
            Transform marker = Read<Transform>(room, "spiderSpawn");
            marker.localRotation = Quaternion.Euler(8f, 137f, -5f);
            room.Configure(false);
            yield return null;
            Transform actor = Read<GameObject>(room, "spider").transform;
            Assert.That(Quaternion.Angle(actor.rotation, marker.rotation), Is.LessThan(0.01f),
                "Waiting rotation must preserve the authored marker rotation, including pitch and roll.");
            Transform head = room.AttackTarget;
            player.Teleport(room.transform.TransformPoint(new Vector3(-3.1f, 0.04f, 3.8f)), Quaternion.identity);
            Transform view = player.PlayerCamera.transform;
            view.rotation = Quaternion.LookRotation(head.position - view.position);
            Vector3 target = view.position + view.forward * 0.65f;
            room.StartCoroutine(room.Attack(player));
            yield return new WaitForSeconds(0.35f);
            Assert.That(Vector3.Dot(head.forward.normalized, (view.position - head.position).normalized),
                Is.GreaterThan(0.8f), "Face must also track Player halfway through the animated leap.");
            yield return new WaitForSeconds(0.4f);
            yield return null;
            Assert.That(Vector3.Distance(head.position, target), Is.LessThan(0.3f));
            Assert.That(Vector3.Dot(head.forward.normalized, (view.position - head.position).normalized),
                Is.GreaterThan(0.8f), "Animated face must point at Player during the leap.");
            Assert.That(Vector3.Dot(head.position - view.position, view.forward), Is.GreaterThan(0.2f),
                "The head must stop in front of the camera, not pass behind it.");
        }

        [UnityTest]
        public IEnumerator WorldLabelsFitTheirBoundsAndNeverShowSealedDossier()
        {
            foreach (TMP_Text text in session.transform.root.GetComponentsInChildren<TMP_Text>())
            {
                text.ForceMeshUpdate();
                Assert.That(text.isTextOverflowing, Is.False, text.name + " overflows.");
                Assert.That(text.isTextTruncated, Is.False, text.name + " truncates.");
                Assert.That(text.text, Does.Not.Contain("DOSSIER SEALED"));
                Assert.That(text.text, Does.Not.Contain("READ THE HOUR"));
                Assert.That(text.text, Does.Not.Contain("WAIT HERE"));
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClockCluesAdvanceImmediatelyAndResetWithAttempt()
        {
            var puzzle = Object.FindFirstObjectByType<CallingCorridorClockPuzzle>();
            Assert.That(puzzle, Is.Not.Null);
            CallingCorridorClock[] clocks = Read<CallingCorridorClock[]>(puzzle, "clocks");
            for (int decision = 0; decision <= 7; decision++)
            {
                AssertClockClues(clocks, decision);
                AssertClockBoard(decision);
                if (decision == 7) break;
                Enter(CorrectRoom());
                AssertClockClues(clocks, decision + 1);
                AssertClockBoard(decision + 1);
                yield return ReturnToHall();
            }
            session.ResetAttempt();
            AssertClockClues(clocks, 0);
            AssertClockBoard(0);
            puzzle.enabled = false;
            Enter(CorrectRoom());
            AssertClockClues(clocks, 0);
            puzzle.enabled = true;
            AssertClockClues(clocks, 1);
        }

        private static void AssertClockClues(CallingCorridorClock[] clocks, int decision)
        {
            Assert.That(clocks.Length, Is.EqualTo(3));
            for (int i = 0; i < clocks.Length; i++)
            {
                int hour = CallingCorridorRules.ClockHandDigit(decision, i, CallingCorridorRules.Hand.Hour);
                int minute = CallingCorridorRules.ClockHandDigit(decision, i, CallingCorridorRules.Hand.Minute);
                Assert.That(clocks[i].HourDigit, Is.EqualTo(hour));
                Assert.That(clocks[i].MinuteDigit, Is.EqualTo(minute));
                Assert.That(Read<GameObject>(clocks[i], "dial").activeSelf, Is.EqualTo(hour > 0));
                Assert.That(Read<GameObject>(clocks[i], "offlineCover").activeSelf, Is.EqualTo(hour <= 0));
                AssertHand(Read<Transform>(clocks[i], "hourPivot"), hour, "HourSolidCircle");
                AssertHand(Read<Transform>(clocks[i], "minutePivot"), minute, "MinuteHollowCircle");
            }
        }

        private static void AssertHand(Transform pivot, int digit, string marker)
        {
            Quaternion expected = Quaternion.Euler(0f, 0f, digit > 0 ? -30f * digit : 0f);
            Assert.That(Quaternion.Angle(pivot.localRotation, expected), Is.LessThan(0.01f));
            Assert.That(pivot.Find(marker).GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
        }

        private void AssertClockBoard(int decision)
        {
            TMP_Text board = Read<TMP_Text>(Read<CallingCorridorStation[]>(session, "stations")[0], "board");
            board.ForceMeshUpdate();
            Assert.That(board.isTextOverflowing, Is.False);
            Assert.That(board.isTextTruncated, Is.False);
            Assert.That(board.font.HasCharacter('\u25cf', true, true), Is.True, "Missing solid-circle glyph.");
            Assert.That(board.font.HasCharacter('\u25cb', true, true), Is.True, "Missing hollow-circle glyph.");
            if (decision < 7) Assert.That(board.text, Does.EndWith(CallingCorridorRules.ClockSymbols(decision)));
            else Assert.That(board.text, Does.Not.Contain("\u25cf").And.Not.Contain("\u25cb"));
        }

        private IEnumerator AssertStationaryExitRabbit(CallingCorridorRoomEncounter room)
        {
            Transform rabbit = Read<GameObject>(room, "rabbit").transform;
            Vector3 position = rabbit.position;
            Quaternion rotation = rabbit.rotation;
            Assert.That(Quaternion.Angle(rotation, Read<Transform>(room, "rabbitSpawn").rotation), Is.LessThan(0.1f));
            yield return new WaitForSeconds(1.4f);
            Assert.That(rabbit.gameObject.activeSelf, Is.True);
            Assert.That(Vector3.Distance(position, rabbit.position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(rotation, rabbit.rotation), Is.LessThan(0.1f));
            Assert.That(Read<Animator>(room, "rabbitAnimator").speed, Is.Zero);
        }

        private IEnumerator WalkTo(Vector3 target)
        {
            CharacterController body = player.GetComponent<CharacterController>();
            float deadline = Time.time + 6f;
            while (Vector3.Distance(new Vector3(player.transform.position.x, target.y, player.transform.position.z), target) > 0.15f && Time.time < deadline)
            {
                Vector3 direction = target - player.transform.position;
                direction.y = 0f;
                body.Move((direction.normalized * 3f + Vector3.down) * Time.deltaTime);
                yield return null;
            }
        }

        private void Enter(int index)
        {
            player.Teleport(rooms[index].transform.TransformPoint(new Vector3(-3.1f, 0.04f, 5.3f)), Quaternion.identity);
            Physics.SyncTransforms();
            Assert.That(gate.TrySelectDoor(doors[index]), Is.True);
        }

        private IEnumerator ReturnToHall()
        {
            player.Teleport(new Vector3(0f, 0.04f, 0f), Quaternion.identity);
            Physics.SyncTransforms();
            yield return null;
            yield return null;
        }

        private int CorrectRoom()
        {
            for (int i = 0; i < rooms.Length; i++)
                if (rooms[i].State == CallingCorridorRoomEncounter.OccupantState.RabbitWaiting) return i;
            Assert.Fail("Exactly one unconsumed room must contain a Rabbit.");
            return -1;
        }

        private static T Read<T>(object target, string field) =>
            (T)target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    }
}
#endif
