using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum GolemState { Intro, Idle, Attack, Transition, Final, Exhausted, Dead }

/// <summary>
/// Контроллер босса «Каменный страж Живых скал». Единственный «мозг»: выбирает атаки, ведёт фазы,
/// оглушение, замедление, финальную атаку и смерть. Атаки выполняются СТРОГО по одной (корутина BrainLoop),
/// поэтому конфликтов между атаками, анимациями и переходом фаз быть не может.
/// </summary>
[RequireComponent(typeof(Health))]
public class GolemBoss : MonoBehaviour
{
    [Header("Ссылки")]
    public Transform player;
    public GolemRig rig;
    public GolemFinalAttack finalAttack;

    [Header("Арена")]
    [Tooltip("Y пола под ногами босса. 0 = взять из стартовой позиции объекта.")]
    public float groundY = -3.68f;
    public float minX = -6.2f;
    public float maxX = 6.2f;
    [Tooltip("Границы для опасных зон (внутренние грани стен арены).")]
    public float arenaMinX = -7.8f;
    public float arenaMaxX = 7.8f;

    [Header("Фазы")]
    [Range(0.1f, 0.9f)] public float phase2Threshold = 0.5f;
    public int finalAttackHealth = 10;
    public float transitionDuration = 2.4f;
    public float phase2MoveMultiplier = 1.35f;
    public float phase2CooldownScale = 0.75f;

    [Header("Движение")]
    public float walkSpeed = 2.2f;
    public float preferredMinDistance = 3.5f;
    public float preferredMaxDistance = 6.5f;

    [Header("Темп боя")]
    public float introDelay = 2f;
    public float thinkDelayPhase1 = 1.4f;
    public float thinkDelayPhase2 = 0.8f;
    [Range(0f, 1f)] public float comboChancePhase2 = 0.45f;
    public float comboDelay = 0.35f;

    [Header("Оглушение и уязвимость")]
    public float stunDamageMultiplier = 1.2f;
    public float exhaustDuration = 6f;
    public float exhaustDamageMultiplier = 1.3f;

    public GolemState State { get; private set; } = GolemState.Intro;
    public int Phase { get; private set; } = 1;
    public float SpeedMultiplier { get; private set; } = 1f;
    public float CooldownScale => Phase >= 2 ? phase2CooldownScale : 1f;
    public bool PlayerAlive
    {
        get
        {
            if (player == null) return false;
            Health h = player.GetComponent<Health>();
            return h == null || !h.IsDead;
        }
    }
    public Vector2 PlayerPos => player != null ? (Vector2)player.position : new Vector2(0f, groundY);
    public GolemRig Rig => rig;
    public bool Frozen { get; private set; }
    public float SlowRemaining => Mathf.Max(0f, slowUntil - Time.time);

    /// <summary>Время босса за кадр: останавливается при оглушении, масштабируется замедлением.</summary>
    public float Dt => (BossStunSystem.IsStunned || Frozen) ? 0f : Time.deltaTime * SpeedMultiplier;

    private Health health;
    private BossContactDamage contact;
    private GolemAttack[] attacks;
    private readonly List<GolemAttack> recent = new List<GolemAttack>();
    private float slowUntil;
    private bool wasStunned;
    private bool brainRunning;
    private GolemPose poseBeforeStun = GolemPose.Idle;

    private void Awake()
    {
        health = GetComponent<Health>();
        contact = GetComponent<BossContactDamage>();
        attacks = GetComponents<GolemAttack>();
        if (rig == null) rig = GetComponentInChildren<GolemRig>();
        if (finalAttack == null) finalAttack = GetComponent<GolemFinalAttack>();
        if (groundY == 0f) groundY = transform.position.y;
    }

    private void OnEnable()
    {
        if (health == null) health = GetComponent<Health>();
        health.Died += OnDied;
        health.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        if (health != null) { health.Died -= OnDied; health.Damaged -= OnDamaged; }
    }

    private void Start()
    {
        BossStunSystem.Immune = false;
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
        transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
        StartCoroutine(BrainLoop());
    }

    // ------------------------------------------------------------------
    private void Update()
    {
        if (State == GolemState.Dead) return;

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (SpeedMultiplier < 1f && Time.time >= slowUntil) SpeedMultiplier = 1f;

        bool stunned = BossStunSystem.IsStunned;
        if (stunned != wasStunned)
        {
            wasStunned = stunned;
            if (stunned)
            {
                poseBeforeStun = rig.Pose == GolemPose.Stunned ? GolemPose.Idle : rig.Pose;
                rig.SetPose(GolemPose.Stunned);
                CombatFx.Flash(transform.position + Vector3.up * 6.2f, new Color(1f, 0.95f, 0.5f, 0.9f), 2.2f, 0.4f);
            }
            else if (State == GolemState.Idle || State == GolemState.Attack || State == GolemState.Intro)
            {
                rig.SetPose(poseBeforeStun);
            }
        }

        // Уязвимость: оглушённый и истощённый босс получает больше урона.
        float mult = 1f;
        if (State == GolemState.Exhausted) mult = exhaustDamageMultiplier;
        else if (stunned) mult = stunDamageMultiplier;
        health.IncomingDamageMultiplier = mult;

        if (contact != null)
            contact.enabled = State == GolemState.Idle || State == GolemState.Attack || State == GolemState.Intro;
    }

    private void OnDamaged(int dealt, Transform source)
    {
        if (rig != null) rig.OnHit();
    }

    // ------------------------------------------------------------------
    // Мозг босса
    // ------------------------------------------------------------------
    private IEnumerator BrainLoop()
    {
        if (brainRunning) yield break;
        brainRunning = true;

        yield return Wait(introDelay);
        State = GolemState.Idle;

        while (State != GolemState.Dead)
        {
            // Игрок мёртв — босс просто стоит.
            if (!PlayerAlive) { rig.SetPose(GolemPose.Idle); rig.SetMoving(false); yield return null; continue; }

            // 1. Финальная атака (приоритетнее всего)
            if (health.CurrentHealth <= finalAttackHealth && finalAttack != null && State != GolemState.Dead)
            {
                if (Phase < 2) { yield return TransitionToPhase2(); }
                yield return RunFinal();
                continue;
            }

            // 2. Переход во вторую фазу
            if (Phase == 1 && health.Normalized <= phase2Threshold)
            {
                yield return TransitionToPhase2();
                continue;
            }

            // 3. Ходьба до следующей атаки
            State = GolemState.Idle;
            float think = Phase >= 2 ? thinkDelayPhase2 : thinkDelayPhase1;
            float t = 0f;
            while (t < think && PlayerAlive)
            {
                t += Dt;
                WalkStep();
                if (NeedsInterrupt()) break;
                yield return null;
            }
            rig.SetMoving(false);
            if (NeedsInterrupt() || !PlayerAlive) continue;

            // 4. Атака (+ комбо во второй фазе)
            GolemAttack attack = PickAttack(false, null);
            if (attack == null) { yield return null; continue; }

            yield return RunAttack(attack);

            if (Phase >= 2 && Random.value < comboChancePhase2 && !NeedsInterrupt() && PlayerAlive)
            {
                GolemAttack next = PickAttack(true, attack);
                if (next != null)
                {
                    yield return Wait(comboDelay);
                    yield return RunAttack(next);
                }
            }
        }
    }

    private bool NeedsInterrupt()
    {
        if (health.CurrentHealth <= finalAttackHealth && finalAttack != null) return true;
        if (Phase == 1 && health.Normalized <= phase2Threshold) return true;
        return false;
    }

    private IEnumerator RunAttack(GolemAttack attack)
    {
        State = GolemState.Attack;
        rig.SetMoving(false);
        FacePlayer();
        recent.Add(attack);
        if (recent.Count > 2) recent.RemoveAt(0);
        Debug.Log("Голем: " + attack.attackName);

        yield return attack.Execute(this);

        attack.MarkUsed(this);
        if (State == GolemState.Attack) State = GolemState.Idle;
        rig.SetPose(GolemPose.Idle);
        yield return null;
    }

    private GolemAttack PickAttack(bool comboOnly, GolemAttack exclude)
    {
        float total = 0f;
        List<GolemAttack> pool = new List<GolemAttack>();
        List<float> weights = new List<float>();
        foreach (GolemAttack a in attacks)
        {
            if (a == null || a == finalAttack || a == exclude) continue;
            if (comboOnly && !a.allowedInCombo) continue;
            if (!a.CanUse(this)) continue;
            float w = a.weight;
            if (recent.Count > 0 && recent[recent.Count - 1] == a) w *= 0.2f;
            else if (recent.Contains(a)) w *= 0.6f;
            if (w <= 0f) continue;
            pool.Add(a); weights.Add(w); total += w;
        }
        if (pool.Count == 0) return null;

        float roll = Random.value * total;
        for (int i = 0; i < pool.Count; i++)
        {
            roll -= weights[i];
            if (roll <= 0f) return pool[i];
        }
        return pool[pool.Count - 1];
    }

    // ------------------------------------------------------------------
    // Фазы
    // ------------------------------------------------------------------
    private IEnumerator TransitionToPhase2()
    {
        State = GolemState.Transition;
        BossStunSystem.Immune = true;
        BossStunSystem.Clear();
        SpeedMultiplier = 1f;
        health.SetExternalInvulnerability(true);

        FacePlayer();
        rig.SetPose(GolemPose.Roar);
        CombatFx.Shake(0.3f, transitionDuration);
        CombatFx.Ring(transform.position + Vector3.up * 3f, new Color(1f, 0.55f, 0.2f, 0.9f), 1f, 12f, 1.1f);

        float t = 0f;
        bool switched = false;
        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            if (!switched && t > transitionDuration * 0.4f)
            {
                switched = true;
                Phase = 2;
                rig.SetPhase(2);
                CombatFx.Debris(transform.position + Vector3.up * 3.5f, 22, 9f, new Color(0.7f, 0.62f, 0.55f, 1f), 1.2f);   // отваливаются каменные пластины
                CombatFx.Dust(transform.position, 8, 2.2f, 1.1f);
                CombatFx.Ring(transform.position + Vector3.up * 3f, new Color(1f, 0.3f, 0.1f, 0.9f), 1f, 14f, 0.9f);
            }
            yield return null;
        }
        if (!switched) { Phase = 2; rig.SetPhase(2); }

        health.SetExternalInvulnerability(false);
        BossStunSystem.Immune = false;
        rig.SetPose(GolemPose.Idle);
        State = GolemState.Idle;
    }

    private IEnumerator RunFinal()
    {
        State = GolemState.Final;
        BossStunSystem.Immune = true;
        BossStunSystem.Clear();
        SpeedMultiplier = 1f;
        health.SetExternalInvulnerability(true);   // во время финальной атаки босс неуязвим
        Debug.Log("Голем: ФИНАЛЬНАЯ АТАКА — «Пробуждение горы».");

        yield return finalAttack.Execute(this);

        // Атака пережита: снимаем неуязвимость и защиту от смерти — босса можно добить.
        health.SetExternalInvulnerability(false);
        health.ReleaseLowHealthProtection();
        BossStunSystem.Immune = false;

        State = GolemState.Exhausted;
        rig.SetPose(GolemPose.Exhausted);
        CombatFx.Flash(transform.position + Vector3.up * 3.5f, new Color(1f, 0.9f, 0.6f, 0.9f), 6f, 0.5f);
        Frozen = true;
        float t = 0f;
        while (t < exhaustDuration) { t += Time.deltaTime; yield return null; }
        Frozen = false;

        rig.SetPose(GolemPose.Idle);
        State = GolemState.Idle;
        // Финал не повторяется: порог здоровья больше не триггерит атаку.
        finalAttack = null;
    }

    // ------------------------------------------------------------------
    // Смерть
    // ------------------------------------------------------------------
    private void OnDied()
    {
        if (State == GolemState.Dead) return;
        State = GolemState.Dead;
        StopAllCoroutines();
        BossStunSystem.Immune = false;
        BossStunSystem.Clear();
        Frozen = false;
        if (contact != null) contact.enabled = false;
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        CombatFx.ClearAll();                       // все активные опасные зоны исчезают
        rig.PlayDeath();
        CombatFx.Shake(0.35f, 1.2f);
        CombatFx.Flash(transform.position + Vector3.up * 3.5f, new Color(1f, 0.85f, 0.5f, 1f), 9f, 0.8f);
        CombatFx.Ring(transform.position + Vector3.up * 3f, new Color(1f, 0.7f, 0.3f, 0.9f), 1f, 18f, 1.2f);
        yield return new WaitForSeconds(2.2f);
        BossDeath.ShowVictory();
        yield return new WaitForSeconds(1.6f);
        Destroy(gameObject);
    }

    // ------------------------------------------------------------------
    // Утилиты для атак
    // ------------------------------------------------------------------
    public void ApplySlow(float multiplier, float duration)
    {
        if (State == GolemState.Final || State == GolemState.Transition || State == GolemState.Dead) return;
        SpeedMultiplier = Mathf.Clamp(multiplier, 0.2f, 1f);
        slowUntil = Mathf.Max(slowUntil, Time.time + duration);   // обновляет длительность, а не складывается
        CombatFx.Flash(transform.position + Vector3.up * 3f, new Color(0.65f, 0.3f, 1f, 0.7f), 4f, 0.3f);
    }

    /// <summary>Ожидание в «времени босса»: замирает при оглушении, растягивается при замедлении.</summary>
    public IEnumerator Wait(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Dt;
            yield return null;
        }
    }

    public void FacePlayer()
    {
        if (player != null) rig.Face(player.position.x - transform.position.x);
    }

    public void Place(Vector3 pos)
    {
        transform.position = new Vector3(Mathf.Clamp(pos.x, arenaMinX + 0.5f, arenaMaxX - 0.5f), pos.y, transform.position.z);
    }

    private void WalkStep()
    {
        if (BossStunSystem.IsStunned || Frozen || player == null) { rig.SetMoving(false); return; }

        float dx = player.position.x - transform.position.x;
        float dist = Mathf.Abs(dx);
        float dir = 0f;
        if (dist > preferredMaxDistance) dir = Mathf.Sign(dx);
        else if (dist < preferredMinDistance) dir = -Mathf.Sign(dx);

        // Прижатый к стене босс не пятится — иначе он застрял бы в углу.
        float x = transform.position.x;
        if ((dir < 0f && x <= minX) || (dir > 0f && x >= maxX)) dir = 0f;

        rig.Face(dx);
        if (Mathf.Abs(dir) < 0.01f) { rig.SetMoving(false); return; }

        float speed = walkSpeed * (Phase >= 2 ? phase2MoveMultiplier : 1f) * SpeedMultiplier;
        float nx = Mathf.Clamp(x + dir * speed * Time.deltaTime, minX, maxX);
        transform.position = new Vector3(nx, groundY, transform.position.z);
        rig.SetMoving(true);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(minX, groundY, 0f), new Vector3(maxX, groundY, 0f));
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(arenaMinX, groundY - 0.3f, 0f), new Vector3(arenaMinX, groundY + 6f, 0f));
        Gizmos.DrawLine(new Vector3(arenaMaxX, groundY - 0.3f, 0f), new Vector3(arenaMaxX, groundY + 6f, 0f));
    }
}
