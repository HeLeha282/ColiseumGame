using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Простой эффект: объект быстро сжимается и исчезает (вспышка взрыва баллисты).</summary>
    public class ShrinkAndDestroy : MonoBehaviour
    {
        [SerializeField] private float duration = 0.3f;

        private Vector3 startScale;
        private float t;

        private void Start() => startScale = transform.localScale;

        private void Update()
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t / duration);
            if (t >= duration) Destroy(gameObject);
        }
    }
}
