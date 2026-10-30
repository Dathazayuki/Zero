using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretRitualSwarm : MonoBehaviour
    {
        [SerializeField] private Animator[] eyes;
        [SerializeField] private GameObject wallEyes;
        [SerializeField] private LineRenderer[] lasers;
        [SerializeField] private GameObject roomDetail;
        [SerializeField, Min(.5f)] private float flyingHeight = 2.1f;
        [SerializeField, Min(0f)] private float orbitRadius = .45f;
        [SerializeField] private float facingYaw;
        private static readonly int Fly = Animator.StringToHash("fly");
        private Vector3[] homes, falls;
        private Quaternion[] rotations;
        private float[] fallTimes;
        private bool captured, ownsDetail, detailWasActive;
        public bool IsConfigured => eyes != null && eyes.Length >= 6 && wallEyes != null && lasers != null
            && lasers.Length == eyes.Length && roomDetail != null;

        public void Capture()
        {
            if (captured || !IsConfigured) return;
            captured = true;
            homes = new Vector3[eyes.Length]; falls = new Vector3[eyes.Length];
            rotations = new Quaternion[eyes.Length]; fallTimes = new float[eyes.Length];
            for (int i = 0; i < eyes.Length; i++)
            { homes[i] = eyes[i].transform.localPosition; rotations[i] = eyes[i].transform.localRotation; fallTimes[i] = -1f; }
        }

        public void Reveal()
        {
            Capture();
            if (!captured) return;
            wallEyes.SetActive(true);
            for (int i = 0; i < eyes.Length; i++)
            {
                fallTimes[i] = -1f;
                eyes[i].gameObject.SetActive(true); eyes[i].enabled = true;
                eyes[i].applyRootMotion = false; eyes[i].speed = 1f;
                eyes[i].cullingMode = AnimatorCullingMode.AlwaysAnimate;
                eyes[i].Rebind(); eyes[i].Play(Fly, 0, (i * .137f) % 1f); eyes[i].Update(0f);
            }
        }

        public void HideDetail()
        {
            if (ownsDetail || roomDetail == null) return;
            detailWasActive = roomDetail.activeSelf; ownsDetail = true;
            roomDetail.SetActive(false);
        }

        public void RestoreDetail()
        {
            if (!ownsDetail) return;
            if (roomDetail != null) roomDetail.SetActive(detailWasActive);
            ownsDetail = false;
        }

        public void Present(float time, float rise, int deadMask, Vector3 target, bool attack)
        {
            if (!captured) return;
            if (deadMask == 7 && wallEyes.activeSelf) wallEyes.SetActive(false);
            for (int i = 0; i < eyes.Length; i++)
            {
                bool dead = (deadMask & (1 << (i % 3))) != 0;
                if (dead) Fall(i, time);
                else FlyEye(i, time, rise, target);
                lasers[i].enabled = attack && !dead;
                if (!attack || dead) continue;
                lasers[i].SetPosition(0, eyes[i].transform.position + Vector3.up * .15f);
                lasers[i].SetPosition(1, target);
            }
        }

        private void FlyEye(int index, float time, float rise, Vector3 target)
        {
            Transform eye = eyes[index].transform;
            float angle = time * .65f + index * 2.39996f;
            Vector3 offset = new Vector3(Mathf.Sin(angle) * orbitRadius,
                flyingHeight + .2f * Mathf.Sin(time * 1.4f + index), Mathf.Cos(angle) * orbitRadius);
            eye.localPosition = homes[index] + offset * Mathf.SmoothStep(0f, 1f, rise);
            Vector3 direction = target - eye.position;
            if (direction.sqrMagnitude > .001f)
                eye.rotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(0f, facingYaw, 0f);
        }

        private void Fall(int index, float time)
        {
            Transform eye = eyes[index].transform;
            if (fallTimes[index] < 0f)
            { fallTimes[index] = time; falls[index] = eye.localPosition; eyes[index].enabled = false; }
            float seconds = time - fallTimes[index];
            Vector3 position = falls[index];
            position.y = Mathf.Max(homes[index].y, position.y - 4.9f * seconds * seconds);
            eye.localPosition = position;
            eye.localRotation = rotations[index] * Quaternion.Euler(0f, 0f, Mathf.Min(110f, seconds * 160f));
        }

        public void Restore()
        {
            RestoreDetail();
            if (wallEyes != null) wallEyes.SetActive(false);
            if (!captured) return;
            for (int i = 0; i < eyes.Length; i++)
            {
                if (eyes[i] == null) continue;
                if (lasers[i] != null) lasers[i].enabled = false;
                eyes[i].enabled = true;
                eyes[i].transform.SetLocalPositionAndRotation(homes[i], rotations[i]);
                eyes[i].gameObject.SetActive(false); fallTimes[i] = -1f;
            }
        }

        private void OnDisable() => Restore();
    }
}
