using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Zero.Utilities
{
    [DisallowMultipleComponent]
    public sealed class GameplayCameraRecorder : MonoBehaviour
    {
        public enum RecordMode
        {
            [Tooltip("Ghi lại toàn bộ: Thân nhân vật, toàn bộ cử động xương (Idle, Walk, Run) và Camera ngó nghiêng")]
            FullCharacterAndCamera,

            [Tooltip("Chỉ ghi lại vị trí và góc nhìn của Camera")]
            CameraOnly
        }

        [Header("Recording Mode")]
        [SerializeField] private RecordMode mode = RecordMode.FullCharacterAndCamera;

        [Tooltip("Root của nhân vật (Player). Nếu để trống sẽ tự động tìm Player cha.")]
        [SerializeField] private GameObject characterRoot;

        [Header("Save Settings")]
        [Tooltip("Tên file animation được tạo trong Assets/Game/Animation/")]
        [SerializeField] private string clipName = "Ending2_GameplayReplay";

        [Tooltip("Tốc độ khung hình của animation (khuyên dùng 60)")]
        [SerializeField, Range(30, 120)] private int frameRate = 60;

        [Header("Control")]
        [Tooltip("Phím tắt để Bật / Tắt quay (Mặc định: Phím K)")]
        [SerializeField] private KeyCode toggleKeyFallback = KeyCode.K;
        private KeyCode ToggleKeyFallback => toggleKeyFallback;

        private bool isRecording;
        private float startTime;

#if UNITY_EDITOR
        private GameObjectRecorder gameObjectRecorder;
#endif

        // Fallback cho CameraOnly mode
        private readonly List<float> timestamps = new List<float>();
        private readonly List<Vector3> recordedPositions = new List<Vector3>();
        private readonly List<Quaternion> recordedRotations = new List<Quaternion>();

        public bool IsRecording => isRecording;
        public float RecordedDuration => isRecording ? Time.time - startTime : 0f;

        private void Awake()
        {
            ResolveTarget();
        }

        private void ResolveTarget()
        {
            if (characterRoot == null)
            {
                var fpc = GetComponentInParent<Zero.AnomalyPrototype.Player.FirstPersonController>();
                if (fpc != null)
                {
                    characterRoot = fpc.gameObject;
                }
                else
                {
                    characterRoot = transform.root.gameObject;
                }
            }
        }

        private void Update()
        {
            bool keyPressed = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.kKey.wasPressedThisFrame)
            {
                keyPressed = true;
            }
#else
            if (Input.GetKeyDown(ToggleKeyFallback))
            {
                keyPressed = true;
            }
#endif

            if (keyPressed)
            {
                if (!isRecording)
                {
                    StartRecording();
                }
                else
                {
                    StopAndSaveRecording();
                }
            }
        }

        private void LateUpdate()
        {
            if (!isRecording) return;

#if UNITY_EDITOR
            if (mode == RecordMode.FullCharacterAndCamera && gameObjectRecorder != null)
            {
                // Chụp lại toàn bộ trạng thái của nhân vật (bao gồm xương, chân tay cử động sau khi Animator cập nhật)
                gameObjectRecorder.TakeSnapshot(Time.deltaTime);
                return;
            }
#endif

            if (mode == RecordMode.CameraOnly)
            {
                timestamps.Add(Time.time - startTime);
                recordedPositions.Add(transform.position);
                recordedRotations.Add(transform.rotation);
            }
        }

        public void StartRecording()
        {
            ResolveTarget();
            isRecording = true;
            startTime = Time.time;
            timestamps.Clear();
            recordedPositions.Clear();
            recordedRotations.Clear();

#if UNITY_EDITOR
            if (mode == RecordMode.FullCharacterAndCamera)
            {
                GameObject target = characterRoot != null ? characterRoot : gameObject;
                gameObjectRecorder = new GameObjectRecorder(target);
                // Bind toàn bộ Transform trong hierarchy nhân vật (thân, camera, toàn bộ xương cử động)
                gameObjectRecorder.BindComponentsOfType<Transform>(target, true);
                Debug.Log($"<color=red><b>[REC START]</b></color> Đang ghi lại <b>Toàn Bộ Nhân Vật & Animation & Camera</b> trên [{target.name}]... (Bấm K để Dừng)", this);
                return;
            }
#endif

            Debug.Log("<color=red><b>[REC START]</b></color> Đang ghi lại Camera... (Bấm K để Dừng)", this);
        }

        public void StopAndSaveRecording()
        {
            if (!isRecording) return;
            isRecording = false;

            float duration = Time.time - startTime;
            if (duration < 0.2f)
            {
                Debug.LogWarning("[REC] Thời gian quay quá ngắn, không có đủ dữ liệu.", this);
#if UNITY_EDITOR
                gameObjectRecorder?.ResetRecording();
                gameObjectRecorder = null;
#endif
                return;
            }

            Debug.Log($"<color=green><b>[REC STOP]</b></color> Đã dừng ghi ({duration:F2}s). Đang xuất Animation Clip...", this);

#if UNITY_EDITOR
            try
            {
                AnimationClip clip = new AnimationClip
                {
                    frameRate = frameRate
                };

                if (mode == RecordMode.FullCharacterAndCamera && gameObjectRecorder != null)
                {
                    // Lưu toàn bộ cử động thân, xương (Idle, Walk, Run) và camera vào clip
                    gameObjectRecorder.SaveToClip(clip);
                    gameObjectRecorder.ResetRecording();
                    gameObjectRecorder = null;
                }
                else
                {
                    AnimationCurve posX = new AnimationCurve();
                    AnimationCurve posY = new AnimationCurve();
                    AnimationCurve posZ = new AnimationCurve();
                    AnimationCurve rotX = new AnimationCurve();
                    AnimationCurve rotY = new AnimationCurve();
                    AnimationCurve rotZ = new AnimationCurve();
                    AnimationCurve rotW = new AnimationCurve();

                    for (int i = 0; i < timestamps.Count; i++)
                    {
                        float t = timestamps[i];
                        Vector3 p = recordedPositions[i];
                        Quaternion r = recordedRotations[i];

                        posX.AddKey(t, p.x);
                        posY.AddKey(t, p.y);
                        posZ.AddKey(t, p.z);
                        rotX.AddKey(t, r.x);
                        rotY.AddKey(t, r.y);
                        rotZ.AddKey(t, r.z);
                        rotW.AddKey(t, r.w);
                    }

                    clip.SetCurve("", typeof(Transform), "localPosition.x", posX);
                    clip.SetCurve("", typeof(Transform), "localPosition.y", posY);
                    clip.SetCurve("", typeof(Transform), "localPosition.z", posZ);
                    clip.SetCurve("", typeof(Transform), "localRotation.x", rotX);
                    clip.SetCurve("", typeof(Transform), "localRotation.y", rotY);
                    clip.SetCurve("", typeof(Transform), "localRotation.z", rotZ);
                    clip.SetCurve("", typeof(Transform), "localRotation.w", rotW);
                }

                clip.EnsureQuaternionContinuity();

                string folderPath = "Assets/Game/Animation";
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    AssetDatabase.CreateFolder("Assets/Game", "Animation");
                }

                string savePath = $"{folderPath}/{clipName}.anim";
                AssetDatabase.CreateAsset(clip, savePath);
                AssetDatabase.SaveAssets();

                Debug.Log($"<color=cyan><b>[THÀNH CÔNG]</b></color> Đã xuất Animation Clip đầy đủ nhân vật tại: <b>{savePath}</b>", clip);
                Selection.activeObject = clip;
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex, this);
            }
#endif
        }
    }
}
