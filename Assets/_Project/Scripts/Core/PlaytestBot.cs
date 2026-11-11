#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ColiseumGame
{
    /// <summary>
    /// ТОЛЬКО ДЛЯ ТЕСТОВ В РЕДАКТОРЕ. Простой «игрок-бот» для автоматических прогонов через MCP:
    /// жмёт те же клавиши, что и человек (W/Shift/ЛКМ/1/2/пробел — через очередь событий Input System),
    /// а поворачивается напрямую (как будто двигает мышь). Работает в режиме 1-го лица.
    /// Добавляется вручную: gameObject.AddComponent&lt;PlaytestBot&gt;() на игрока в Play Mode.
    /// </summary>
    public class PlaytestBot : MonoBehaviour
    {
        public float defendRadius = 18f;
        public float meleeDistance = 2.2f;

        private PlayerController player;
        private PlayerCombat combat;
        private PlayerCameraController cam;
        private float clickTimer;
        private bool mouseDown;
        private float jumpTimer;

        private void Start()
        {
            player = GetComponent<PlayerController>();
            combat = GetComponent<PlayerCombat>();
            cam = GetComponent<PlayerCameraController>();
            cam.SetFirstPerson(true);
        }

        private void OnDisable() => Send(new List<Key>(), false);

        private void Update()
        {
            var keys = new List<Key>();
            bool mouse = false;
            var gm = GameManager.Instance;
            if (player.IsAlive && gm != null && gm.Phase != GamePhase.Victory && gm.Phase != GamePhase.Defeat && !UIState.MenuOpen)
                Think(keys, ref mouse);
            Send(keys, mouse);
        }

        private void Think(List<Key> keys, ref bool mouse)
        {
            Vector3 portal = Portal.Instance != null ? Portal.Instance.transform.position : Vector3.zero;
            Enemy target = PickTarget(portal);

            if (target == null)
            {
                // Нет угроз — собираем сферы, иначе возвращаемся к порталу.
                var orb = NearestOrb();
                Vector3 goal = orb != null ? orb.transform.position : portal + new Vector3(0f, 0f, -6f);
                if (Flat(goal - transform.position).magnitude > 1.5f) MoveTo(goal, keys);
                return;
            }

            if (target.IsFlying)
            {
                // Летуна бьём из лука: целимся, держим ЛКМ ~0.8 с, отпускаем.
                if (combat.Weapon != WeaponType.Bow) keys.Add(Key.Digit2);
                Aim(target.AimPoint);
                if (Flat(target.transform.position - transform.position).magnitude > 25f) MoveTo(target.transform.position, keys);
                if (!combat.IsCharging || combat.Charge01 < 0.8f) mouse = true;
                return;
            }

            if (combat.Weapon != WeaponType.Sword) keys.Add(Key.Digit1);
            Aim(target.AimPoint);
            float dist = Flat(target.transform.position - transform.position).magnitude;
            if (dist > meleeDistance + target.Radius)
            {
                keys.Add(Key.W);
                keys.Add(Key.LeftShift);
            }
            else
            {
                // Кликаем мечом с частотой чуть выше перезарядки.
                clickTimer -= Time.deltaTime;
                if (clickTimer <= 0f)
                {
                    clickTimer = 0.25f;
                    mouseDown = !mouseDown;
                }
                mouse = mouseDown;
            }
        }

        private Enemy PickTarget(Vector3 portal)
        {
            Enemy best = null;
            float bestScore = float.MaxValue;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.IsAlive) continue;
                float toPortal = Flat(e.transform.position - portal).magnitude;
                float toMe = Vector3.Distance(e.transform.position, transform.position);
                if (toPortal > defendRadius && toMe > 6f) continue; // далеко — пусть работают башни
                float score = toMe + toPortal * 0.5f;
                if (e.Armor > 0f) score -= 30f;            // броню босса снимает только игрок
                if (e.Config.hasShield) score -= 8f;       // щитоносцы — тоже наша работа
                if (e.IsFlying) score += 10f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = e;
                }
            }
            return best;
        }

        private EssenceOrb NearestOrb()
        {
            EssenceOrb best = null;
            float bestDist = 25f;
            foreach (var orb in FindObjectsOfType<EssenceOrb>())
            {
                float d = Vector3.Distance(orb.transform.position, transform.position);
                if (d < bestDist && Mathf.Abs(orb.transform.position.y - transform.position.y) < 2f)
                {
                    bestDist = d;
                    best = orb;
                }
            }
            return best;
        }

        private void MoveTo(Vector3 goal, List<Key> keys)
        {
            Aim(goal + Vector3.up * 1.6f);
            keys.Add(Key.W);
            keys.Add(Key.LeftShift);
        }

        private void Aim(Vector3 point)
        {
            Vector3 eye = transform.position + Vector3.up * 1.65f;
            Vector3 d = point - eye;
            Vector3 flat = Flat(d);
            if (flat.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(flat);
            float pitch = -Mathf.Atan2(d.y, flat.magnitude) * Mathf.Rad2Deg;
            cam.SetPitch(pitch);
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static void Send(List<Key> keys, bool mouse)
        {
            if (Keyboard.current != null)
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys.ToArray()));
            if (Mouse.current != null)
                InputSystem.QueueStateEvent(Mouse.current, new MouseState { buttons = (ushort)(mouse ? 1 : 0) });
        }
    }
}
#endif
