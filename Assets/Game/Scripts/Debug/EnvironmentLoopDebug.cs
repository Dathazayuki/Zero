using UnityEngine;

namespace Zero.EnvironmentLoop
{
    public sealed class EnvironmentLoopDebug : MonoBehaviour
    {
        [SerializeField] private StairLoopManager stairLoopManager;
        [SerializeField] private FloorProgressManager floorProgressManager;
        [SerializeField] private bool visible = false;

        private GUIStyle labelStyle;
        private GUIStyle boxStyle;

        private void Awake()
        {
            visible = false;
        }

        public void Configure(StairLoopManager loopManager, FloorProgressManager progressManager)
        {
            stairLoopManager = loopManager;
            floorProgressManager = progressManager;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                visible = !visible;
            }

            DirectionalAnomalyGameManager anomalyGame = stairLoopManager?.AnomalyGameManager;
            if (anomalyGame != null && Input.GetKeyDown(KeyCode.F2))
            {
                anomalyGame.ForceCurrentRound(false);
            }

            if (anomalyGame != null && Input.GetKeyDown(KeyCode.F3))
            {
                anomalyGame.ForceCurrentRound(true);
            }

            if (anomalyGame != null && Input.GetKeyDown(KeyCode.F4))
            {
                anomalyGame.ForceCurrentAnomaly("observer.statue");
            }

            if (anomalyGame != null && Input.GetKeyDown(KeyCode.F5))
            {
                anomalyGame.ForceCurrentAnomaly("light.arrow-flicker");
            }

            if (anomalyGame != null && Input.GetKeyDown(KeyCode.F6))
            {
                anomalyGame.ForceCurrentAnomaly("arrow.flip-on-sight");
            }

            if (anomalyGame != null && Input.GetKeyDown(KeyCode.F7))
            {
                anomalyGame.ForceCurrentAnomaly("creature.ceiling");
            }

            if (anomalyGame != null && Input.GetKeyDown(KeyCode.F8))
            {
                anomalyGame.ForceCurrentAnomaly("reflection.wrong");
            }

            if (anomalyGame != null && Input.GetKeyDown(KeyCode.F10))
            {
                anomalyGame.ForceCurrentAnomaly("sign.inverted-exit");
            }

            if (anomalyGame != null && Input.GetKeyDown(KeyCode.F11))
            {
                anomalyGame.ForceCurrentAnomaly("creature.two_children");
            }

            if (anomalyGame != null && Input.GetKeyDown(KeyCode.F12))
            {
                anomalyGame.ForceCurrentAnomaly("object.missing-clock");
            }

            if (anomalyGame != null && Input.GetKeyDown(KeyCode.Alpha1))
            {
                anomalyGame.ForceCurrentAnomaly("creature.speed_animation");
            }

            if (anomalyGame != null && Input.GetKeyDown(KeyCode.Alpha2))
            {
                anomalyGame.ForceCurrentAnomaly("creature.horror_doll");
            }

            if (anomalyGame != null && Input.GetKeyDown(KeyCode.Alpha3))
            {
                anomalyGame.ForceCurrentAnomaly("environment.sea_of_blood");
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (anomalyGame != null && Input.GetKeyDown(KeyCode.F9))
            {
                anomalyGame.ForceCompleteRunForValidation();
            }
#endif
        }

        private void OnGUI()
        {
            if (!visible || stairLoopManager == null || floorProgressManager == null)
            {
                return;
            }

            EnsureStyles();
            string direction = floorProgressManager.HasTravelDirection
                ? floorProgressManager.LastTravelDirection.ToString().ToUpperInvariant()
                : "NONE";
            DirectionalAnomalyGameManager anomalyGame = stairLoopManager.AnomalyGameManager;
            string anomalyState = anomalyGame == null
                ? "Not initialized"
                : $"Round {anomalyGame.RoundNumber} | " +
                  $"Arrow: {anomalyGame.CurrentArrowDirection.ToString().ToUpperInvariant()} ({anomalyGame.ConsecutiveArrowCount}/{anomalyGame.MaximumConsecutiveArrowDirection}) | " +
                  $"Expected: {anomalyGame.CurrentExpectedDirection.ToString().ToUpperInvariant()} | " +
                  $"Anomaly: {(anomalyGame.CurrentRoomHasAnomaly ? anomalyGame.ActiveAnomalyName : "NORMAL")} | " +
                  $"Floor Number: {anomalyGame.DisplayedFloorNumber}/8 | " +
                  $"Correct/Wrong: {anomalyGame.CorrectDecisions}/{anomalyGame.IncorrectDecisions}";
            string text =
                $"ENVIRONMENT LOOP  [F1 UI] [F2 NORMAL] [F3 ANOMALY] [F4 OBSERVER] [F5 LIGHT] [F6 ARROW] [F7 CEILING] [F8 MIRROR] [F10 EXIT] [F11 TWO_CHILDREN] [F12 CLOCK] [1 SPEED_ANIM] [2 DOLL] [3 BLOOD] [F9 ENDING]\n" +
                $"Logical Floor: {floorProgressManager.LogicalFloorIndex}\n" +
                $"Travel Direction: {direction}\n" +
                $"Current: {stairLoopManager.CurrentSegment?.SegmentId ?? "None"}\n" +
                $"Below1: {stairLoopManager.Below1Segment?.SegmentId ?? "None"}\n" +
                $"Above1: {stairLoopManager.Above1Segment?.SegmentId ?? "None"}\n" +
                $"Pool: {stairLoopManager.PoolOrder}\n" +
                $"Active/Inactive/Total: {stairLoopManager.ActiveSegmentCount}/" +
                $"{stairLoopManager.InactiveSegmentCount}/{stairLoopManager.PoolSize}\n" +
                $"Recycle Count: {stairLoopManager.RecycleCount}\n" +
                $"Gameplay: {anomalyState}\n" +
                $"State: {stairLoopManager.LastOperationMessage}";

            GUI.Box(new Rect(16f, 16f, 780f, 272f), GUIContent.none, boxStyle);
            GUI.Label(new Rect(30f, 27f, 750f, 250f), text, labelStyle);
        }

        private void EnsureStyles()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                normal = { textColor = new Color(0.88f, 0.94f, 0.9f) }
            };
            boxStyle = new GUIStyle(GUI.skin.box);
        }
    }
}
