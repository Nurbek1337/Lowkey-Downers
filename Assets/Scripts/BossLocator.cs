using UnityEngine;

/// <summary>
/// Единая точка поиска босса для способностей персонажей.
/// Поддерживает нового каменного голема (GolemBoss) и старого босса (BossMovement).
/// Босс НЕ считается "обычным врагом" (см. EnemyTameable) — это отдельная сущность.
/// </summary>
public static class BossLocator
{
    private static GolemBoss golem;
    private static BossMovement legacy;

    public static Transform Find()
    {
        if (golem == null) golem = Object.FindAnyObjectByType<GolemBoss>();
        if (golem != null) return golem.transform;
        if (legacy == null) legacy = Object.FindAnyObjectByType<BossMovement>();
        return legacy != null ? legacy.transform : null;
    }

    public static Health FindHealth()
    {
        Transform t = Find();
        return t != null ? t.GetComponent<Health>() : null;
    }

    public static bool IsBoss(Component c)
    {
        if (c == null) return false;
        return c.GetComponentInParent<GolemBoss>() != null || c.GetComponentInParent<BossMovement>() != null;
    }

    /// <summary>Замедление босса (Requiem of Souls). Не складывается, а обновляет длительность.</summary>
    public static void ApplySlow(float multiplier, float duration)
    {
        Transform boss = Find();
        if (boss == null) return;
        GolemBoss g = boss.GetComponent<GolemBoss>();
        if (g != null) { g.ApplySlow(multiplier, duration); return; }
        BossStatusEffects fx = boss.GetComponent<BossStatusEffects>();
        if (fx == null) fx = boss.gameObject.AddComponent<BossStatusEffects>();
        fx.ApplySlow(multiplier, duration);
    }

    /// <summary>Кратковременное прерывание босса (Lili). Подчиняется иммунитету к оглушению.</summary>
    public static bool Interrupt(float duration, Transform source)
    {
        return BossStunSystem.Stun(duration, source);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        golem = null;
        legacy = null;
    }
}
