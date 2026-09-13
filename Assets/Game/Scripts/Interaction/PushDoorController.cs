using System.Collections;
using UnityEngine;

namespace Zero.AnomalyPrototype.Interaction
{
    /// <summary>
    /// Khi collider Player chạm vào cửa, cửa tự động xoay một góc 70 độ theo trục Y.
    /// Khi đẩy cửa: isOpen = true.
    /// Khi reset: isOpen = false và đưa cửa về góc đóng ban đầu.
    /// </summary>
    public class PushDoorController : MonoBehaviour
    {
        [Header("Door Pivot & Angle")]
        [SerializeField] private Transform doorPivot;
        [SerializeField] private float openAngle = 70f;
        [SerializeField] private float openDuration = 0.8f;
        [SerializeField] private bool openAwayFromPlayer = true;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openSound;

        [Header("State")]
        [SerializeField] private bool isOpen = false;

        public bool IsOpen => isOpen;

        private Quaternion initialRotation;
        private Coroutine rotateRoutine;

        private void Awake()
        {
            if (doorPivot == null)
            {
                doorPivot = transform;
            }

            initialRotation = doorPivot.localRotation;
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void OnEnable()
        {
            ResetDoor();
        }

        private void OnDisable()
        {
            ResetDoor();
        }

        private void Update()
        {
            // Tự động nhận diện khi hệ thống reset lại Transform cửa về góc ban đầu
            if (isOpen && rotateRoutine == null && Quaternion.Angle(doorPivot.localRotation, initialRotation) < 0.5f)
            {
                isOpen = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryPush(other.gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!isOpen)
            {
                TryPush(other.gameObject);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            TryPush(collision.gameObject);
        }

        private void TryPush(GameObject obj)
        {
            if (isOpen || obj == null)
            {
                return;
            }

            // Nhận diện Player qua Tag, CharacterController hoặc FirstPersonController
            if (obj.CompareTag("Player") ||
                obj.GetComponentInParent<CharacterController>() != null ||
                obj.GetComponentInParent<Player.FirstPersonController>() != null)
            {
                OpenDoor(obj.transform);
            }
        }

        public void OpenDoor(Transform player = null)
        {
            if (isOpen)
            {
                return;
            }

            isOpen = true;

            // Tính hướng mở ra xa khỏi người chơi
            float angle = openAngle;
            if (openAwayFromPlayer && player != null)
            {
                Vector3 toPlayer = (player.position - doorPivot.position).normalized;
                if (Vector3.Dot(doorPivot.forward, toPlayer) < 0f)
                {
                    angle = -openAngle;
                }
            }

            Quaternion targetRotation = initialRotation * Quaternion.Euler(0f, angle, 0f);

            if (rotateRoutine != null)
            {
                StopCoroutine(rotateRoutine);
            }

            rotateRoutine = StartCoroutine(AnimateOpen(targetRotation));

            if (audioSource != null && openSound != null)
            {
                audioSource.PlayOneShot(openSound);
            }
        }

        private IEnumerator AnimateOpen(Quaternion target)
        {
            Quaternion start = doorPivot.localRotation;
            float elapsed = 0f;
            float duration = Mathf.Max(0.1f, openDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smooth = t * t * (3f - 2f * t); // SmoothStep
                doorPivot.localRotation = Quaternion.Slerp(start, target, smooth);
                yield return null;
            }

            doorPivot.localRotation = target;
            rotateRoutine = null;
        }

        public void ResetDoor()
        {
            if (rotateRoutine != null)
            {
                StopCoroutine(rotateRoutine);
                rotateRoutine = null;
            }

            isOpen = false;

            if (doorPivot != null && initialRotation != default)
            {
                doorPivot.localRotation = initialRotation;
            }
        }
    }
}
