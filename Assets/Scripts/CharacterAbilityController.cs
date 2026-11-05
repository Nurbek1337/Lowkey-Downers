using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Уникальные способности восьми персонажей. В бою участвует только выбранный игрок (без союзников и переключения).
/// Стоимость, перезарядка и мана берутся из профиля CharacterStats; специфичные параметры — ниже, в Inspector.
/// Ney использует ParrySystem (клавиши E/Q), поэтому для него этот контроллер ничего не делает.
/// </summary>
[DisallowMultipleComponent]
public class CharacterAbilityController : MonoBehaviour
{
    [Header("Управление")]
    public KeyCode abilityKey = KeyCode.Q;

    [Header("BladeMail — Gegwer")]
    public float bladeMailDuration = 4f;
    [Range(0f, 1f)] public float bladeMailReflectFraction = 1f;
    [Tooltip("Максимум отражённого урона за один удар (защита от поломки босса).")]
    public int bladeMailPerHitCap = 40;
    [Tooltip("Максимум отражённого урона за одну активацию.")]
    public int bladeMailTotalCap = 90;

    [Header("EchoSlam — Smirqc")]
    public float echoRadius = 3f;
    public int echoBaseDamage = 50;
    public int echoDamagePerExtraEnemy = 30;

    [Header("Requiem of Souls — Shoshkabes")]
    public float soulGainInterval = 1f;
    public int maxSouls = 20;
    public int requiemDamagePerSoul = 8;
    [Range(0.1f, 1f)] public float requiemSlowMultiplier = 0.5f;
    public float requiemSlowDuration = 3f;

    [Header("Multicast — Nonlyy")]
    [Tooltip("Шанс срабатывания ХОТЯ БЫ 2 раза.")]
    [Range(0f, 1f)] public float multicastTwoChance = 0.75f;
    [Tooltip("Шанс срабатывания ХОТЯ БЫ 3 раза.")]
    [Range(0f, 1f)] public float multicastThreeChance = 0.30f;
    [Tooltip("Шанс срабатывания 4 раза.")]
    [Range(0f, 1f)] public float multicastFourChance = 0.15f;
    [Tooltip("Сколько секунд длится режим Multicast после активации.")]
    public float multicastDuration = 5f;

    [Header("Heal — KiberKotleta")]
    public int healAmount = 15;
    [Tooltip("Окно, в котором повторные лечения теряют эффективность.")]
    public float healFatigueWindow = 25f;
    [Range(0f, 0.5f)] public float healFatiguePerStack = 0.2f;
    [Range(0.1f, 1f)] public float healFatigueMinFactor = 0.4f;

    [Header("Taming — Lili")]
    [Range(0f, 1f)] public float normalEnemyTameChance = 0.70f;
    [Range(0f, 1f)] public float bossEffectChance = 0.30f;
    public float tameDuration = 15f;
    public float tameSearchRange = 14f;
    [Tooltip("Эффект на боссе: прерывание (оглушение) босса. Подчиняется иммунитету босса.")]
    public float bossEffectStunDuration = 2f;

    [Header("Phase — Gafasa")]
    public float phaseDuration = 6f;
    public float phaseImmunityWindow = 1f;
    [Tooltip("Бонус скорости передвижения на оставшееся время действия способности.")]
    public float phaseSpeedBonus = 0.3f;

    [Header("Ссылки")]
    public CharacterPartyManager partyManager;

    // ---- состояние для HUD ----
    public int CurrentSouls => souls;
    public bool IsMulticastReady => Time.time < multicastUntil;
    public float MulticastRemaining => Mathf.Max(0f, multicastUntil - Time.time);
    public float CooldownRemaining => Mathf.Max(0f, nextUseTime - Time.time);
    public float CooldownDuration { get; private set; } = 1f;
    public string AbilityName { get; private set; } = "";

    private CharacterStats stats;
    private CharacterMana mana;
    private CharacterStatusEffects status;
    private Health ownHealth;

    private float nextUseTime;
    private float nextSoulTime;
    private int souls;
    private float multicastUntil;
    private readonly List<int> multicastBag = new List<int>();
    private readonly List<float> healTimes = new List<float>();

    private void Awake()
    {
        CacheRefs();
        Refresh();
    }

    private void CacheRefs()
    {
        stats = GetComponent<CharacterStats>();
        mana = GetComponent<CharacterMana>();
        status = GetComponent<CharacterStatusEffects>();
        ownHealth = GetComponent<Health>();
    }

    public void RefreshAfterCharacterSelection()
    {
        CacheRefs();
        Refresh();
    }

    private void Refresh()
    {
        souls = 0;
        nextUseTime = 0f;
        multicastUntil = 0f;
        multicastBag.Clear();
        healTimes.Clear();
        nextSoulTime = Time.time + soulGainInterval;
        CooldownDuration = stats != null ? Mathf.Max(0.01f, stats.AbilityCooldown) : 10f;
        AbilityName = stats != null ? NameOf(stats.character) : "";
    }

    public static string NameOf(CharacterId id)
    {
        switch (id)
        {
            case CharacterId.Ney: return "Parry";
            case CharacterId.Gegwer: return "BladeMail";
            case CharacterId.Smirqc: return "EchoSlam";
            case CharacterId.Shoshkabes: return "Requiem of Souls";
            case CharacterId.Nonlyy: return "Multicast";
            case CharacterId.KiberKotleta: return "Heal";
            case CharacterId.Lili: return "Taming";
            case CharacterId.Gafasa: return "Phase";
        }
        return "";
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || stats == null) return;
        if (ownHealth != null && ownHealth.IsDead) return;

        if (stats.character == CharacterId.Shoshkabes && Time.time >= nextSoulTime)
        {
            // Души копятся строго по 1 за интервал и никогда не превышают максимум.
            souls = Mathf.Min(maxSouls, souls + 1);
            nextSoulTime = Time.time + soulGainInterval;
        }

        if (Input.GetKeyDown(abilityKey)) TryUseAbility();
    }

    /// <summary>Совместимость со старым AI-кодом (союзники отключены).</summary>
    public bool TryUseAbility(bool fromAI) { return TryUseAbility(); }

    public bool TryUseAbility()
    {
        if (stats == null) CacheRefs();
        if (stats == null || Time.timeScale <= 0f) return false;
        if (ownHealth != null && ownHealth.IsDead) return false;
        if (stats.character == CharacterId.Ney) return false;      // Parry живёт в ParrySystem
        if (Time.time < nextUseTime) return false;

        // Проверки "бессмысленного" применения — без траты маны и перезарядки.
        if (!CanUseNow()) return false;

        int cost = stats.AbilityManaCost;
        if (mana != null && !mana.TrySpend(cost))
        {
            Debug.Log("Недостаточно маны!");
            return false;
        }

        CooldownDuration = Mathf.Max(0.01f, stats.AbilityCooldown);
        nextUseTime = Time.time + CooldownDuration;
        Execute();
        return true;
    }

    private bool CanUseNow()
    {
        switch (stats.character)
        {
            case CharacterId.KiberKotleta:
                return ownHealth != null && ownHealth.CurrentHealth < ownHealth.maxHealth;
            case CharacterId.Shoshkabes:
                return souls > 0 && BossLocator.Find() != null;
            case CharacterId.Lili:
                if (EnemyTameable.AnyTamed()) { Debug.Log("Lili уже контролирует одного союзника."); return false; }
                return true;
        }
        return true;
    }

    private void Execute()
    {
        float power = stats.SkillDamageMultiplier;
        switch (stats.character)
        {
            case CharacterId.Gegwer:
                if (status != null) status.ActivateBladeMail(bladeMailDuration, bladeMailReflectFraction, bladeMailPerHitCap, bladeMailTotalCap);
                CombatFx.Ring(transform.position, new Color(1f, 0.4f, 0.35f, 0.9f), 0.8f, 3.2f, 0.4f);
                break;
            case CharacterId.Smirqc: EchoSlam(power); break;
            case CharacterId.Shoshkabes: Requiem(power); break;
            case CharacterId.Nonlyy: ActivateMulticast(); break;
            case CharacterId.KiberKotleta: Heal(power); break;
            case CharacterId.Lili: TameTarget(); break;
            case CharacterId.Gafasa:
                if (status != null) status.ActivatePhase(phaseDuration, phaseImmunityWindow, phaseSpeedBonus);
                CombatFx.Flash(transform.position, new Color(0.5f, 1f, 1f, 0.9f), 3.5f, 0.35f);
                break;
        }
    }

    // ------------------------------------------------------------------
    // Smirqc
    // ------------------------------------------------------------------
    private void EchoSlam(float power)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, echoRadius);
        HashSet<Health> counted = new HashSet<Health>();
        List<Health> normalEnemies = new List<Health>();
        Health boss = null;

        foreach (Collider2D hit in hits)
        {
            if (hit.transform.root == transform.root) continue;
            if (hit.GetComponentInParent<CharacterStats>() != null) continue;     // не бьём себя

            Health h = hit.GetComponentInParent<Health>();
            if (h == null || h.IsDead || !counted.Add(h)) continue;

            if (BossLocator.IsBoss(h)) { boss = h; continue; }                    // босс — не "обычный враг"

            EnemyTameable tame = h.GetComponent<EnemyTameable>();
            if (tame == null || tame.IsTamed) continue;                           // только обычные враги, не союзники
            normalEnemies.Add(h);
        }

        int extraEnemies = normalEnemies.Count;
        if (boss != null)
        {
            // Если рядом только босс — всё равно базовые 50. Каждый обычный враг добавляет 30.
            int damage = Mathf.RoundToInt((echoBaseDamage + extraEnemies * echoDamagePerExtraEnemy) * power);
            boss.TakeDamage(damage, transform);
        }
        int enemyDamage = Mathf.RoundToInt(echoBaseDamage * power);
        foreach (Health e in normalEnemies) e.TakeDamage(enemyDamage, transform);

        CombatFx.Ring(transform.position, new Color(1f, 0.75f, 0.3f, 0.9f), 0.8f, echoRadius * 2.1f, 0.35f);
        CombatFx.Debris(transform.position, 8, 6f, new Color(0.75f, 0.65f, 0.55f, 1f));
        CombatFx.Shake(0.15f, 0.2f);
    }

    // ------------------------------------------------------------------
    // Shoshkabes
    // ------------------------------------------------------------------
    private void Requiem(float power)
    {
        int consumed = souls;
        souls = 0;
        Transform target = BossLocator.Find();
        if (target == null || consumed <= 0) return;

        int damage = Mathf.RoundToInt(consumed * requiemDamagePerSoul * power);
        Health h = target.GetComponent<Health>();
        if (h != null) h.TakeDamage(damage, transform);

        // Замедление обновляет длительность, а не складывается; иммунитет босса учитывается в GolemBoss.
        BossLocator.ApplySlow(requiemSlowMultiplier, requiemSlowDuration);

        int rings = Mathf.Clamp(Mathf.CeilToInt(consumed / 5f), 1, 4);
        for (int i = 0; i < rings; i++)
            CombatFx.Ring(target.position + Vector3.up * 2.5f, new Color(0.65f, 0.2f, 1f, 0.8f), 1.2f + i * 0.8f, 5f + i, 0.5f);
        CombatFx.Flash(target.position + Vector3.up * 2.5f, new Color(0.7f, 0.3f, 1f, 0.9f), 3f, 0.3f);
    }

    // ------------------------------------------------------------------
    // Nonlyy — Multicast
    // ------------------------------------------------------------------
    private void ActivateMulticast()
    {
        multicastUntil = Time.time + multicastDuration;
        multicastBag.Clear();
        CombatFx.Ring(transform.position, new Color(0.4f, 0.8f, 1f, 0.9f), 0.8f, 2.8f, 0.4f);
    }

    /// <summary>
    /// Сколько раз сработает атака/способность. Шансы 75/30/15 трактуются как "хотя бы N раз"
    /// (накопительно), поэтому точные вероятности: x1 = 25%, x2 = 45%, x3 = 15%, x4 = 15%.
    /// Результаты берутся из перемешанного "мешка" из 20 значений — нет ни неожиданно длинных серий,
    /// ни бесконечных цепочек (максимум 4 повтора, повторы сами не провоцируют новых повторов).
    /// </summary>
    public int ConsumeMulticastCount()
    {
        if (stats == null || stats.character != CharacterId.Nonlyy) return 1;
        if (Time.time >= multicastUntil) return 1;

        if (multicastBag.Count == 0) RefillBag();
        int last = multicastBag.Count - 1;
        int result = multicastBag[last];
        multicastBag.RemoveAt(last);
        return Mathf.Clamp(result, 1, 4);
    }

    private void RefillBag()
    {
        float p2 = Mathf.Clamp01(multicastTwoChance);
        float p3 = Mathf.Clamp(multicastThreeChance, 0f, p2);
        float p4 = Mathf.Clamp(multicastFourChance, 0f, p3);
        float[] exact = { 1f - p2, p2 - p3, p3 - p4, p4 };   // x1, x2, x3, x4

        const int size = 20;
        int[] counts = new int[4];
        float[] rem = new float[4];
        int total = 0;
        for (int i = 0; i < 4; i++)
        {
            float v = exact[i] * size;
            counts[i] = Mathf.FloorToInt(v);
            rem[i] = v - counts[i];
            total += counts[i];
        }
        while (total < size)
        {
            int best = 0;
            for (int i = 1; i < 4; i++) if (rem[i] > rem[best]) best = i;
            counts[best]++; rem[best] = -1f; total++;
        }
        for (int i = 0; i < 4; i++)
            for (int k = 0; k < counts[i]; k++) multicastBag.Add(i + 1);

        for (int i = multicastBag.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = multicastBag[i]; multicastBag[i] = multicastBag[j]; multicastBag[j] = tmp;
        }
    }

    // ------------------------------------------------------------------
    // KiberKotleta
    // ------------------------------------------------------------------
    private void Heal(float power)
    {
        healTimes.RemoveAll(t => Time.time - t > healFatigueWindow);
        float factor = Mathf.Max(healFatigueMinFactor, 1f - healFatiguePerStack * healTimes.Count);
        healTimes.Add(Time.time);

        int amount = Mathf.Max(1, Mathf.RoundToInt(healAmount * power * factor));
        int healed = ownHealth != null ? ownHealth.Heal(amount) : 0;   // никогда не выше максимума
        CombatFx.Flash(transform.position, new Color(0.4f, 1f, 0.5f, 0.9f), 2.5f, 0.4f);
        Debug.Log("KiberKotleta: лечение +" + healed + " (эффективность " + Mathf.RoundToInt(factor * 100f) + "%)");
    }

    // ------------------------------------------------------------------
    // Lili
    // ------------------------------------------------------------------
    private void TameTarget()
    {
        Transform boss = BossLocator.Find();
        EnemyTameable enemy = EnemyTameable.FindNearest(transform.position, tameSearchRange);

        if (enemy != null)
        {
            // Обычный враг: 70% шанс. Босс в этом случае не затрагивается.
            if (Random.value <= normalEnemyTameChance && enemy.TryTame(boss, tameDuration))
                Debug.Log("Lili приручила врага на " + tameDuration + " с.");
            else
                CombatFx.Flash(enemy.transform.position, new Color(1f, 0.3f, 0.3f, 0.8f), 1.5f, 0.25f);
            return;
        }

        // Обычных врагов нет: 30% шанс эффекта на боссе. Босс не превращается в миньона — он лишь прерывается.
        if (boss != null && Random.value <= bossEffectChance)
        {
            if (BossLocator.Interrupt(bossEffectStunDuration, transform))
                CombatFx.Flash(boss.position + Vector3.up * 3f, new Color(1f, 0.5f, 0.9f, 0.9f), 4f, 0.4f);
        }
        else if (boss != null)
        {
            CombatFx.Flash(boss.position + Vector3.up * 3f, new Color(0.6f, 0.6f, 0.6f, 0.6f), 2f, 0.2f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, echoRadius);
    }
}
