using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Samples the Blender mechanism cycle at the tower's actual firing rate.</summary>
    public sealed class CatapultVisual : MonoBehaviour
    {
        [SerializeField] private AnimationClip cycle;
        [SerializeField] private Transform projectileSocket;
        [SerializeField] private GameObject loadedRock;
        [SerializeField] private LineRenderer drawRope;
        [SerializeField] private Transform ropeDrum, ropeGuide, drawHook;

        // Blender frame 16 is the release pose in the 1–96 cycle.
        public const float ReleaseFraction = 15f / 95f;
        public bool IsCycling { get; private set; }
        public Vector3 ShotPosition { get; private set; }
        public Vector3 Muzzle => projectileSocket != null ? projectileSocket.position : transform.position + Vector3.up;
        private float elapsed, duration;
        private bool released;

        public void ResetPose()
        {
            IsCycling = false;
            elapsed = 0f;
            released = false;
            Sample(0f);
        }

        public void BeginShot(float fireInterval)
        {
            duration = Mathf.Max(0.1f, fireInterval);
            elapsed = 0f;
            released = false;
            IsCycling = true;
            Sample(0f);
        }

        /// <returns>True exactly once, when the mechanism releases the projectile.</returns>
        public bool Tick(float deltaTime)
        {
            if (!IsCycling) return false;
            elapsed = Mathf.Min(duration, elapsed + Mathf.Max(0f, deltaTime));
            float phase = elapsed / duration;
            bool fire = !released && phase >= ReleaseFraction;
            if (fire)
            {
                // Capture the release pose even when a slow frame jumps past it.
                Sample(ReleaseFraction);
                ShotPosition = Muzzle;
                released = true;
            }
            Sample(phase);
            if (phase >= 1f) IsCycling = false;
            return fire;
        }

        private void Sample(float phase)
        {
            if (cycle != null) cycle.SampleAnimation(gameObject, Mathf.Clamp01(phase) * cycle.length);
            if (loadedRock != null) loadedRock.SetActive(phase < ReleaseFraction || phase >= 0.93f);
            if (drawRope != null && ropeDrum != null && ropeGuide != null && drawHook != null)
            {
                drawRope.SetPosition(0, ropeDrum.position);
                drawRope.SetPosition(1, ropeGuide.position);
                bool attached = phase < 9f / 95f || phase >= 39f / 95f;
                drawRope.SetPosition(2, attached ? drawHook.position : ropeGuide.position + Vector3.up * 0.03f);
            }
        }

        private void OnEnable() => ResetPose();
        private void OnDisable() => IsCycling = false;
    }
}
