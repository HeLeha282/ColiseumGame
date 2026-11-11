using UnityEngine;

namespace ColiseumGame
{
    /// <summary>Сфера эссенции, падает с убитого врага. Подбирается, если подойти; исчезает со временем.</summary>
    public class EssenceOrb : MonoBehaviour
    {
        [SerializeField] private Renderer visual;
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float blinkBeforeEnd = 3f;

        private int value;
        private float lifeLeft;
        private GameBalance balance;
        private Vector3 basePos;

        public void Init(int essenceValue, GameBalance gameBalance)
        {
            value = essenceValue;
            balance = gameBalance;
            lifeLeft = balance.orbLifetime;
            basePos = transform.position;
        }

        private void Update()
        {
            if (balance == null) return;
            lifeLeft -= Time.deltaTime;
            if (lifeLeft <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            // Мигаем перед исчезновением.
            if (visual != null)
                visual.enabled = lifeLeft > blinkBeforeEnd || Mathf.Repeat(lifeLeft * 6f, 1f) > 0.4f;

            var player = PlayerController.Instance;
            if (player != null && player.IsAlive)
            {
                Vector3 target = player.transform.position + Vector3.up;
                float dist = Vector3.Distance(transform.position, target);
                if (dist <= balance.orbPickupRadius)
                {
                    GameManager.Instance.AddEssence(value);
                    Destroy(gameObject);
                    return;
                }
                if (dist <= balance.orbMagnetRadius)
                {
                    transform.position = Vector3.MoveTowards(transform.position, target, balance.orbMagnetSpeed * Time.deltaTime);
                    basePos = transform.position;
                    return;
                }
            }

            transform.position = basePos + Vector3.up * (Mathf.Sin(Time.time * 3f) * bobAmplitude);
        }
    }
}
