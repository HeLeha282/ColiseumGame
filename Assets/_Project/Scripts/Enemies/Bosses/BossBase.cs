using System.Collections.Generic;
using UnityEngine;

namespace ColiseumGame
{
    /// <summary>
    /// Основа босса: подсказка на экране (что делать), действие на E рядом с боссом и катсцена.
    /// Конкретные боссы переопределяют Hint/InteractPrompt/Interact.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class BossBase : MonoBehaviour
    {
        public static readonly List<BossBase> All = new List<BossBase>();

        protected Enemy enemy;

        public Enemy Enemy => enemy;
        public string Name => enemy.Config.displayName;
        /// <summary>Текст подсказки под полоской HP босса.</summary>
        public virtual string Hint => enemy.Config.bossHint;
        /// <summary>Если не null — рядом с боссом можно нажать E (текст подсказки).</summary>
        public virtual string InteractPrompt => null;
        public virtual float InteractRange => 5f;

        protected virtual void Awake() => enemy = GetComponent<Enemy>();
        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        public bool CanInteract(Vector3 playerPos)
        {
            if (InteractPrompt == null || !enemy.IsAlive) return false;
            Vector3 d = playerPos - transform.position;
            d.y = 0f;
            return d.magnitude <= InteractRange + enemy.Radius;
        }

        public virtual void Interact() { }

        protected void PlayCutscene(string caption, System.Action climax) => Cutscene.Play(transform, caption, climax);

        /// <summary>Создать примитив-«голову»/деталь на боссе (визуал без коллайдера).</summary>
        protected Transform Part(PrimitiveType type, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Vfx.Mat(color, 1.2f);
            return go.transform;
        }
    }
}
