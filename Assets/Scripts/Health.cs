using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Здоровье игрока, босса и обычных врагов.
///
/// Урон разделён на два пути:
///  1) TakeBossDamage — удар, считающийся атакой босса. Для него работают парирование Ney,
///     защита Gafasa (Phase) и отражение Gegwer (BladeMail).
///  2) TakeDamage / TakeDamageRepeated — обычный урон (снаряды игрока по боссу, отражённый урон,
///     урон приручённых врагов и т.д.). Эти пути НЕ парируются и НЕ отражаются.
///
/// Для совместимости со старыми атаками босса, вызывающими TakeDamage(dmg) на игроке,
/// обычный урон по объекту с CharacterStats автоматически трактуется как урон босса.
/// </summary>
public class Health : MonoBehaviour
{
    public int maxHealth = 50;
    public float invulnerabilityTime = 1f;

    [Header("Защита босса от смерти до финальной атаки")]
    public bool protectAtLowHealth = false;
    public int protectionHealth = 10;

    [Header("Множитель входящего урона от босса (баланс персонажа)")]
    [Min(0f)] public float bossDamageMultiplier = 1f;

    private int currentHealth;
    public int CurrentHealth => currentHealth;
    public float Normalized => maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f;
    public bool IsDead { get; private set; }

    /// <summary>Множитель всего входящего урона (используется боссом: уязвимость при оглушении).</summary>
    public float IncomingDamageMultiplier { get; set; } = 1f;

    private bool isInvulnerable;
    private bool externalInvulnerability;
    public bool IsInvulnerable => externalInvulnerability || isInvulnerable;
    public bool IsExternallyInvulnerable => externalInvulnerability;

    public event Action<int, Transform> Damaged;   // (фактический урон, источник)
    public event Action<int> Healed;               // (фактическое лечение)
    public event Action Died;

    private SpriteRenderer spriteRenderer;
    private ParrySystem parrySystem;
    private CharacterStatusEffects statusEffects;
    private CharacterStats characterStats;

    private void Awake()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        IsDead = false;
    }

    private void Start()
    {
        Transform visual = transform.Find("PlayerVisual");
        spriteRenderer = visual != null ? visual.GetComponent<SpriteRenderer>() : GetComponent<SpriteRenderer>();

        CacheComponents();
        bool isNey = characterStats == null || characterStats.character == CharacterId.Ney;
        if (parrySystem == null && isNey && characterStats != null)
            parrySystem = gameObject.AddComponent<ParrySystem>();
    }

    private void CacheComponents()
    {
        if (parrySystem == null) parrySystem = GetComponent<ParrySystem>();
        if (statusEffects == null) statusEffects = GetComponent<CharacterStatusEffects>();
        if (characterStats == null) characterStats = GetComponent<CharacterStats>();
    }

#if UNITY_EDITOR
    private void Update()
    {
        // Отладка: H — получить 10 урона (только в редакторе).
        if (Input.GetKeyDown(KeyCode.H) && CompareTag("Player"))
            TakeDamage(10);
    }
#endif

    // ------------------------------------------------------------------
    // Обычный урон
    // ------------------------------------------------------------------
    public void TakeDamage(int damage) { TakeDamage(damage, null); }

    public void TakeDamage(int damage, Transform attacker)
    {
        if (IsPlayerControlled())
        {
            // Старые атаки босса вызывают TakeDamage на игроке — считаем это ударом босса.
            TakeBossDamage(damage, attacker);
            return;
        }
        ApplyGeneric(damage, attacker, false);
    }

    /// <summary>Повторный проц Multicast: не съедается неуязвимостью между попаданиями.</summary>
    public void TakeDamageRepeated(int damage, Transform attacker)
    {
        if (IsPlayerControlled()) { TakeBossDamage(damage, attacker); return; }
        ApplyGeneric(damage, attacker, true);
    }

    private bool IsPlayerControlled()
    {
        if (characterStats == null) characterStats = GetComponent<CharacterStats>();
        return characterStats != null;
    }

    private bool ApplyGeneric(int damage, Transform attacker, bool bypassIFrames)
    {
        if (damage <= 0 || IsDead || externalInvulnerability) return false;
        if (isInvulnerable && !bypassIFrames) return false;

        int final = Mathf.Max(1, Mathf.RoundToInt(damage * IncomingDamageMultiplier));
        ApplyRaw(final, attacker, !bypassIFrames);
        return true;
    }

    // ------------------------------------------------------------------
    // Удар босса (парирование / Phase / BladeMail)
    // ------------------------------------------------------------------
    /// <param name="parryable">Может ли удар быть парирован (Ney).</param>
    /// <param name="reflectable">Может ли удар быть отражён (Gegwer).</param>
    /// <param name="phaseable">Блокируется ли удар защитой Gafasa. Урон приспешников — false.</param>
    /// <returns>true — урон был нанесён; false — заблокирован/парирован/проигнорирован.</returns>
    public bool TakeBossDamage(int damage, Transform source, bool parryable = true, bool reflectable = true, bool phaseable = true)
    {
        if (damage <= 0 || IsDead || externalInvulnerability) return false;
        CacheComponents();

        // 1) Защита Gafasa действует только против атак босса и только пока длится окно.
        if (phaseable && statusEffects != null && statusEffects.IsBossImmune)
        {
            statusEffects.NotifyBlocked();
            return false;
        }

        // 2) Парирование проверяется ДО неуязвимости после попадания: удар в окно всегда засчитывается.
        if (parryable && parrySystem != null && parrySystem.enabled && parrySystem.TryBlock(source))
            return false;

        if (isInvulnerable) return false;

        int final = Mathf.Max(1, Mathf.RoundToInt(damage * bossDamageMultiplier * IncomingDamageMultiplier));
        int before = currentHealth;
        ApplyRaw(final, source, true);
        int dealt = before - currentHealth;

        // 3) BladeMail отражает урон, который реально был получен, источнику атаки.
        if (dealt > 0 && reflectable && statusEffects != null && source != null)
            statusEffects.ReflectTo(source, dealt);

        return true;
    }

    private void ApplyRaw(int damage, Transform source, bool startIFrames)
    {
        int before = currentHealth;
        currentHealth -= damage;
        if (protectAtLowHealth && currentHealth < protectionHealth)
            currentHealth = protectionHealth;
        if (currentHealth < 0) currentHealth = 0;

        int dealt = before - currentHealth;
        if (dealt > 0) Damaged?.Invoke(dealt, source);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (startIFrames && invulnerabilityTime > 0f && !isInvulnerable)
            StartCoroutine(Invulnerability(invulnerabilityTime));
    }

    // ------------------------------------------------------------------
    // Лечение и служебные методы
    // ------------------------------------------------------------------
    public void SetMaxHealthAndRefill(int value)
    {
        maxHealth = Mathf.Max(1, value);
        currentHealth = maxHealth;
        IsDead = false;
    }

    /// <summary>Лечит, не превышая максимум. Возвращает фактически восстановленное здоровье.</summary>
    public int Heal(int amount)
    {
        if (amount <= 0 || IsDead) return 0;
        int before = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        int healed = currentHealth - before;
        if (healed > 0) Healed?.Invoke(healed);
        return healed;
    }

    public void SetExternalInvulnerability(bool value)
    {
        externalInvulnerability = value;

        // После финальной атаки босса снова можно добить: если здоровье упёрлось в защиту — снимаем её.
        if (!value && protectAtLowHealth && currentHealth <= protectionHealth)
            protectAtLowHealth = false;
    }

    public void ReleaseLowHealthProtection()
    {
        protectAtLowHealth = false;
    }

    /// <summary>Короткая неуязвимость (например, после успешного парирования).</summary>
    public void GrantInvulnerability(float seconds)
    {
        if (seconds <= 0f || IsDead || isInvulnerable) return;
        StartCoroutine(Invulnerability(seconds));
    }

    private IEnumerator Invulnerability(float duration)
    {
        isInvulnerable = true;
        float timer = 0f;
        while (timer < duration)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        isInvulnerable = false;
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;
        isInvulnerable = false;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        Debug.Log(gameObject.name + " погиб!");
        Died?.Invoke();
    }
}
