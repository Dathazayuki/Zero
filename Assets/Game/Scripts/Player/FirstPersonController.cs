using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Zero.AnomalyPrototype.Flow;

namespace Zero.AnomalyPrototype.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [Header("Movement Speeds")]
        [FormerlySerializedAs("moveSpeed")]
        [SerializeField, Min(0.1f)] private float walkSpeed = 3.2f;
        [SerializeField, Min(0.1f)] private float runSpeed = 6.0f;
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.08f;
        [SerializeField] private float gravity = -22f;
        [SerializeField, Range(30f, 89f)] private float verticalLookLimit = 85f;

        [Header("Camera Head Bob & Shake")]
        [SerializeField] private bool enableHeadBob = true;
        [SerializeField, Min(0f)] private float walkBobFrequency = 8f;
        [SerializeField, Min(0f)] private float walkBobAmount = 0.025f;
        [SerializeField, Min(0f)] private float runBobFrequency = 12.5f;
        [SerializeField, Min(0f)] private float runBobAmount = 0.055f;
        [SerializeField, Min(0f)] private float runShakeIntensity = 0.015f;
        [SerializeField, Min(0f)] private float runRollShakeAngle = 1.2f;
        [SerializeField, Min(0.1f)] private float bobResetSpeed = 10f;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource footstepAudioSource;
        [SerializeField] private AudioSource breathingAudioSource;

        [Header("Audio Clips")]
        [Tooltip("List of footstep audio clips for walking/running.")]
        [SerializeField] private AudioClip[] footstepClips;
        [Tooltip("Looping audio clip of heavy breathing / panting while sprinting.")]
        [SerializeField] private AudioClip breathingClip;

        [Header("Footstep Settings")]
        [SerializeField, Min(0.1f)] private float walkStepInterval = 0.52f;
        [SerializeField, Min(0.1f)] private float runStepInterval = 0.32f;

        [Header("References")]
        [SerializeField] private Camera playerCamera;

        private CharacterController characterController;
        private Vector3 initialCameraLocalPosition;
        private Vector3 currentCameraOffset;
        private float verticalVelocity;
        private float pitch;
        private float currentTilt;
        private float bobTimer;
        private float stepTimer;
        private bool inputEnabled = true;
        private bool cursorLocked;
        private bool cinematicLocked;
        private float speedMultiplier = 1f;
        private float stumbleIntensity;
        private float elevatorRideIntensity;
        public void SetElevatorRideFeedback(float intensity) => elevatorRideIntensity = Mathf.Clamp01(intensity);
        public void ClearElevatorRideFeedback() => elevatorRideIntensity = 0f;
        private bool forcedBreathing;
        private SourceSoundPlayback footstepPlayback;
        private SourceSoundPlayback breathingPlayback;

        public Camera PlayerCamera => playerCamera;
        public bool InputEnabled => inputEnabled && Time.timeScale > 0f;
        public bool IsRunning { get; private set; }
        public bool IsMoving { get; private set; }
        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        public float CurrentSpeed => IsRunning ? runSpeed : walkSpeed;
        public AudioSource FootstepAudioSource => footstepAudioSource;
        public AudioSource BreathingAudioSource => breathingAudioSource;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (GetComponentInChildren<Animator>(true) != null &&
                GetComponent<PlayerLocomotionAnimator>() == null)
            {
                // Keep locomotion animation available in every scene that uses this
                // controller and includes a character model. No scene-time setup or
                // root-motion ownership is required.
                gameObject.AddComponent<PlayerLocomotionAnimator>();
            }

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>(true);
            }

            if (playerCamera != null)
            {
                initialCameraLocalPosition = playerCamera.transform.localPosition;
            }

            InitializeAudioSources();
        }

        private void Start()
        {
            SetCursorLocked(true);
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (cinematicLocked)
            {
                if (!cursorLocked)
                {
                    SetCursorLocked(true);
                }

                UpdateBreathingAudio();
                return;
            }

            if (!inputEnabled)
            {
                UpdateCameraEffects(false, false);
                UpdateBreathingAudio();
                return;
            }

            if (!cursorLocked)
            {
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    SetCursorLocked(true);
                }

                UpdateCameraEffects(false, false);
                UpdateBreathingAudio();
                return;
            }

            UpdateLook(mouse);
            UpdateMovement(keyboard, mouse);
            UpdateAudio();
        }

        public void Configure(
            Camera cameraReference,
            float speed = 3.2f,
            float sensitivity = 0.08f,
            float runningSpeed = 6.0f)
        {
            playerCamera = cameraReference;
            walkSpeed = speed;
            runSpeed = runningSpeed;
            mouseSensitivity = sensitivity;

            if (playerCamera != null)
            {
                initialCameraLocalPosition = playerCamera.transform.localPosition;
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!enabled)
            {
                verticalVelocity = 0f;
                IsMoving = false;
                IsRunning = false;
                stepTimer = 0f;
                SetCursorLocked(false);
            }
        }

        public void SetCinematicLock(bool locked)
        {
            cinematicLocked = locked;
            inputEnabled = !locked;
            verticalVelocity = 0f;
            IsMoving = false;
            IsRunning = false;
            stepTimer = 0f;
            SetCursorLocked(true);

            if (locked)
            {
                footstepAudioSource?.Stop();
                breathingAudioSource?.Stop();
            }
        }

        public void SetCameraRestPose(Vector3 localPosition)
        {
            initialCameraLocalPosition = localPosition;
            pitch = 0f;
            bobTimer = 0f;
            currentCameraOffset = Vector3.zero;
            currentTilt = 0f;

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>(true);
            }

            if (playerCamera != null)
            {
                playerCamera.transform.localPosition = initialCameraLocalPosition;
                playerCamera.transform.localRotation = Quaternion.identity;
            }
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            characterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            characterController.enabled = true;

            pitch = 0f;
            verticalVelocity = 0f;
            bobTimer = 0f;
            stepTimer = 0f;
            currentCameraOffset = Vector3.zero;
            currentTilt = 0f;
            IsMoving = false;
            IsRunning = false;

            if (playerCamera != null)
            {
                playerCamera.transform.localPosition = initialCameraLocalPosition;
                playerCamera.transform.localRotation = Quaternion.identity;
            }

            if (breathingAudioSource != null)
            {
                breathingAudioSource.Stop();
            }
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Clamp01(multiplier);
        }

        public void SetStumbleIntensity(float intensity)
        {
            stumbleIntensity = Mathf.Clamp01(intensity);
        }

        public void SetForcedBreathing(bool forced)
        {
            forcedBreathing = forced;
            if (forced && breathingAudioSource != null)
            {
                if (breathingClip != null && breathingAudioSource.clip != breathingClip)
                {
                    breathingAudioSource.clip = breathingClip;
                }
                breathingAudioSource.loop = true;
                if (!breathingAudioSource.isPlaying && breathingAudioSource.clip != null)
                {
                    GameAudioBootstrap.BindSource(ref breathingPlayback, breathingAudioSource, AudioBus.Sfx, false).Play("player.breathing", breathingAudioSource.clip, false);
                }
            }
            else if (!forced && breathingAudioSource != null && !IsRunning)
            {
                breathingAudioSource.Stop();
            }
        }

        public void ResetLocomotionModifiers()
        {
            ClearElevatorRideFeedback();
            speedMultiplier = 1f;
            stumbleIntensity = 0f;
            forcedBreathing = false;
        }

        private void UpdateLook(Mouse mouse)
        {
            if (mouse == null || playerCamera == null)
            {
                return;
            }

            Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y, -verticalLookLimit, verticalLookLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, currentTilt);
            transform.Rotate(Vector3.up, delta.x, Space.Self);
        }

        private void UpdateMovement(Keyboard keyboard, Mouse mouse)
        {
            if (characterController == null)
            {
                IsMoving = false;
                IsRunning = false;
                UpdateCameraEffects(false, false);
                return;
            }

            float horizontal = 0f;
            float forward = 0f;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed) horizontal -= 1f;
                if (keyboard.dKey.isPressed) horizontal += 1f;
                if (keyboard.sKey.isPressed) forward -= 1f;
                if (keyboard.wKey.isPressed) forward += 1f;
            }

            Vector2 input = Vector2.ClampMagnitude(new Vector2(horizontal, forward), 1f);
            IsMoving = input.sqrMagnitude > 0.001f;

            bool shiftPressed = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
            bool rmbPressed = mouse != null && mouse.rightButton.isPressed;
            bool sprintRequested = shiftPressed || rmbPressed;

            IsRunning = IsMoving && sprintRequested && characterController.isGrounded;

            float activeSpeed = (IsRunning ? runSpeed : walkSpeed) * speedMultiplier;
            Vector3 moveDirection = transform.right * input.x + transform.forward * input.y;
            Vector3 planarVelocity = moveDirection * activeSpeed;

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            planarVelocity.y = verticalVelocity;
            characterController.Move(planarVelocity * Time.deltaTime);

            UpdateCameraEffects(IsMoving && characterController.isGrounded, IsRunning);
        }

        private void UpdateCameraEffects(bool isWalkingOrRunning, bool isSprinting)
        {
            if (playerCamera == null)
            {
                return;
            }

            Vector3 targetOffset = Vector3.zero;
            float targetTilt = 0f;

            if (enableHeadBob && isWalkingOrRunning)
            {
                float frequency = isSprinting ? runBobFrequency : walkBobFrequency;
                float bobAmount = isSprinting ? runBobAmount : walkBobAmount;

                bobTimer += Time.deltaTime * frequency;

                float horizontalBob = Mathf.Cos(bobTimer * 0.5f) * bobAmount * 0.75f;
                float verticalBob = Mathf.Sin(bobTimer) * bobAmount;

                if (isSprinting)
                {
                    // Camera shake / subtle noise and roll tilt when running
                    float noiseX = (Mathf.PerlinNoise(Time.time * 22f, 0f) - 0.5f) * 2f * runShakeIntensity;
                    float noiseY = (Mathf.PerlinNoise(0f, Time.time * 22f) - 0.5f) * 2f * runShakeIntensity;
                    targetOffset = new Vector3(horizontalBob + noiseX, verticalBob + noiseY, 0f);

                    float rollSway = Mathf.Sin(bobTimer * 0.5f) * runRollShakeAngle;
                    float noiseRoll = (Mathf.PerlinNoise(Time.time * 14f, 15f) - 0.5f) * runRollShakeAngle * 0.4f;
                    targetTilt = rollSway + noiseRoll;
                }
                else
                {
                    targetOffset = new Vector3(horizontalBob, verticalBob, 0f);
                    targetTilt = 0f;
                }
            }
            else
            {
                bobTimer = 0f;
            }

            if (stumbleIntensity > 0f)
            {
                float stumbleTimer = Time.time * 2.8f;
                targetTilt += Mathf.Sin(stumbleTimer) * (7.5f * stumbleIntensity);
                float stumbleX = Mathf.Cos(stumbleTimer * 0.5f) * (0.08f * stumbleIntensity);
                float stumbleY = -Mathf.Abs(Mathf.Sin(stumbleTimer)) * (0.07f * stumbleIntensity);
                targetOffset += new Vector3(stumbleX, stumbleY, 0f);
            }

            if (elevatorRideIntensity > 0f)
            {
                float vibration = (Mathf.PerlinNoise(Time.time * 12f, 0.3f) - 0.5f) * 2f * elevatorRideIntensity;
                targetOffset += new Vector3(vibration * 0.005f, vibration * 0.012f, 0f);
                targetTilt += vibration * 0.35f;
            }
            currentCameraOffset = Vector3.Lerp(currentCameraOffset, targetOffset, Time.deltaTime * bobResetSpeed);
            currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * bobResetSpeed);

            playerCamera.transform.localPosition = initialCameraLocalPosition + currentCameraOffset;
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, currentTilt);
        }

        private void UpdateAudio()
        {
            UpdateFootstepsAudio();
            UpdateBreathingAudio();
        }

        private void UpdateFootstepsAudio()
        {
            if (characterController == null || !characterController.isGrounded || !IsMoving)
            {
                // Reset stepTimer slightly before full interval so footstep plays promptly upon starting
                stepTimer = (IsRunning ? runStepInterval : walkStepInterval) * 0.75f;
                return;
            }

            float currentInterval = IsRunning ? runStepInterval : walkStepInterval;
            stepTimer += Time.deltaTime;

            if (stepTimer >= currentInterval)
            {
                stepTimer = 0f;
                PlayFootstepSound(IsRunning);
            }
        }

        private void PlayFootstepSound(bool isRunning)
        {
            if (footstepAudioSource == null)
            {
                return;
            }

            AudioClip clipToPlay = null;
            if (footstepClips != null && footstepClips.Length > 0)
            {
                int index = Random.Range(0, footstepClips.Length);
                clipToPlay = footstepClips[index];
            }

            footstepAudioSource.pitch = Random.Range(0.93f, 1.07f) * (isRunning ? 1.12f : 1.0f);
            GameAudioBootstrap.BindSource(ref footstepPlayback, footstepAudioSource, AudioBus.Sfx, false).Play(clipToPlay == null ? "player.footstep" : null, clipToPlay);
        }

        private void UpdateBreathingAudio()
        {
            if (breathingAudioSource == null)
            {
                return;
            }

            if (breathingClip != null && breathingAudioSource.clip != breathingClip)
            {
                breathingAudioSource.clip = breathingClip;
                breathingAudioSource.loop = true;
            }

            bool shouldBreathe = forcedBreathing || (IsRunning && IsMoving && characterController != null && characterController.isGrounded);
            if (shouldBreathe)
            {
                if (!breathingAudioSource.isPlaying && breathingAudioSource.clip != null)
                {
                    GameAudioBootstrap.BindSource(ref breathingPlayback, breathingAudioSource, AudioBus.Sfx, false).Play("player.breathing", breathingAudioSource.clip, false);
                }
            }
            else
            {
                if (breathingAudioSource.isPlaying)
                {
                    breathingAudioSource.Stop();
                }
            }
        }

        private void InitializeAudioSources()
        {
            if (footstepAudioSource == null)
            {
                footstepAudioSource = gameObject.AddComponent<AudioSource>();
                footstepAudioSource.playOnAwake = false;
                footstepAudioSource.spatialBlend = 0f; // 2D local player audio
            }

            if (breathingAudioSource == null)
            {
                breathingAudioSource = gameObject.AddComponent<AudioSource>();
                breathingAudioSource.playOnAwake = false;
                breathingAudioSource.loop = true;
                breathingAudioSource.spatialBlend = 0f;
                if (breathingClip != null)
                {
                    breathingAudioSource.clip = breathingClip;
                }
            }
        }

        private void SetCursorLocked(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (footstepAudioSource != null)
            {
                footstepAudioSource.Stop();
            }

            if (breathingAudioSource != null)
            {
                breathingAudioSource.Stop();
            }
        }
    }
}
