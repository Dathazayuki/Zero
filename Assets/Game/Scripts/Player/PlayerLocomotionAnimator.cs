using UnityEngine;

namespace Zero.AnomalyPrototype.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FirstPersonController))]
    public sealed class PlayerLocomotionAnimator : MonoBehaviour
    {
        private enum LocomotionState
        {
            Unknown,
            Idle,
            Walk,
            Run
        }

        [Header("References")]
        [SerializeField] private FirstPersonController movementController;
        [SerializeField] private Animator characterAnimator;

        [Header("Animator States")]
        [SerializeField] private string baseLayerName = "Base Layer";
        [SerializeField] private string idleState = "Idle";
        [SerializeField] private string walkState = "Walk";
        [SerializeField] private string runState = "Run";
        [SerializeField, Min(0f)] private float transitionDuration = 0.12f;
        [SerializeField, Min(0f)] private float speedDampTime = 0.08f;

        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");

        private int idleStateHash;
        private int walkStateHash;
        private int runStateHash;
        private bool hasMoveSpeedParameter;
        private bool hasIsRunningParameter;
        private LocomotionState activeState;

        private void Awake()
        {
            if (movementController == null)
            {
                movementController = GetComponent<FirstPersonController>();
            }

            if (characterAnimator == null)
            {
                characterAnimator = GetComponentInChildren<Animator>(true);
            }

            CacheAnimatorContract();
        }

        private void OnEnable()
        {
            activeState = LocomotionState.Unknown;
        }

        private void LateUpdate()
        {
            if (movementController == null || characterAnimator == null || !characterAnimator.isActiveAndEnabled)
            {
                return;
            }

            bool isMoving = movementController.IsMoving;
            bool isRunning = isMoving && movementController.IsRunning;
            float normalizedSpeed = isRunning ? 1f : isMoving ? 0.5f : 0f;

            if (hasMoveSpeedParameter)
            {
                characterAnimator.SetFloat(MoveSpeedHash, normalizedSpeed, speedDampTime, Time.deltaTime);
            }

            if (hasIsRunningParameter)
            {
                characterAnimator.SetBool(IsRunningHash, isRunning);
            }

            LocomotionState desiredState = isRunning
                ? LocomotionState.Run
                : isMoving ? LocomotionState.Walk : LocomotionState.Idle;
            if (desiredState == activeState)
            {
                return;
            }

            int stateHash = desiredState == LocomotionState.Run
                ? runStateHash
                : desiredState == LocomotionState.Walk ? walkStateHash : idleStateHash;
            if (!characterAnimator.HasState(0, stateHash))
            {
                Debug.LogError($"PLAYER_ANIMATOR_STATE_MISSING: state={desiredState}", characterAnimator);
                return;
            }

            characterAnimator.CrossFadeInFixedTime(stateHash, transitionDuration, 0);
            activeState = desiredState;
        }

        private void CacheAnimatorContract()
        {
            idleStateHash = Animator.StringToHash($"{baseLayerName}.{idleState}");
            walkStateHash = Animator.StringToHash($"{baseLayerName}.{walkState}");
            runStateHash = Animator.StringToHash($"{baseLayerName}.{runState}");

            if (characterAnimator == null)
            {
                Debug.LogError("PLAYER_ANIMATOR_CONFIGURATION_INVALID: no Animator found below Player.", this);
                return;
            }

            // CharacterController owns world movement. Animation only drives the model pose.
            characterAnimator.applyRootMotion = false;
            characterAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            AnimatorControllerParameter[] parameters = characterAnimator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].nameHash == MoveSpeedHash &&
                    parameters[i].type == AnimatorControllerParameterType.Float)
                {
                    hasMoveSpeedParameter = true;
                }
                else if (parameters[i].nameHash == IsRunningHash &&
                         parameters[i].type == AnimatorControllerParameterType.Bool)
                {
                    hasIsRunningParameter = true;
                }
            }
        }
    }
}
