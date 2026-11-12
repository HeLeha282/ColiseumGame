using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Королева гарпий (летающий босс): кружит высоко, призывает гарпий. Её нужно сбить на землю
    /// (гарпун Скорпиона, сеть Снайпера, Громоотвод обелиска, Цепная молния героя), подбежать и нажать E —
    /// оторвать крылья (катсцена): дальше она ходит по земле и получает двойной урон.
    /// </summary>
    public class BossHarpyQueen : BossBase, IEnemyDamageModifier
    {
        [SerializeField] private EnemyConfig minion;
        [SerializeField] private float summonInterval = 12f;
        [SerializeField] private int summonCount = 3;

        private float summonTimer = 5f;
        private bool clipped;

        public override string Hint => clipped
            ? "Крылья оторваны! Королева на земле и получает двойной урон — добивай!"
            : enemy.GroundedTime > 0f
                ? $"Королева сбита на землю! Подбеги и нажми E — оторви ей крылья! ({enemy.GroundedTime:0} с)"
                : "Королева гарпий высоко в небе. Сбей её на землю: гарпун Скорпиона, сеть Снайпера, Громоотвод обелиска или Цепная молния героя. Затем — E рядом.";

        public override string InteractPrompt => !clipped && enemy.GroundedTime > 0f ? "[E] Оторвать крылья" : null;

        public override void Interact()
        {
            PlayCutscene("БЕЗ КРЫЛЬЕВ!", () =>
            {
                clipped = true;
                enemy.Ground(100000f);
                Vfx.Flash(transform.position + Vector3.up, new Color(0.7f, 0.4f, 0.9f), 3f, 0.4f);
            });
        }

        public float ModifyIncomingDamage(DamageInfo info, float amount) => clipped ? amount * 2f : amount;

        private void Update()
        {
            if (!enemy.IsAlive || clipped || minion == null) return;
            summonTimer -= Time.deltaTime;
            if (summonTimer > 0f) return;
            summonTimer = summonInterval;
            var gm = GameManager.Instance;
            if (gm == null) return;
            for (int i = 0; i < summonCount; i++)
                gm.Spawner.SpawnChild(minion, enemy.Route, transform.position + Random.insideUnitSphere * 2f, enemy.HpMultiplier, null);
            Vfx.Text(transform.position + Vector3.up * 2f, "ПРИЗЫВ ГАРПИЙ", new Color(0.8f, 0.5f, 1f), 1.1f);
        }
    }
}
