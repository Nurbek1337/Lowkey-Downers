using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Общие проверки попадания атак босса по игроку. Урон наносится только объектам с CharacterStats
/// (игрок), поэтому босс не ранит сам себя, приручённых существ и приспешников.
/// Каждая зона поражения хранит множество уже задетых целей — один удар попадает по игроку максимум один раз.
/// </summary>
public static class BossDamage
{
    public static Health PlayerHealthOf(Collider2D col)
    {
        if (col == null) return null;
        CharacterStats cs = col.GetComponentInParent<CharacterStats>();
        return cs != null ? cs.GetComponent<Health>() : null;
    }

    public static int HitBox(Vector2 center, Vector2 size, int damage, Transform source, HashSet<Health> already,
        bool parryable = true, bool reflectable = true, bool phaseable = true)
    {
        Collider2D[] cols = Physics2D.OverlapBoxAll(center, size, 0f);
        return Apply(cols, damage, source, already, parryable, reflectable, phaseable);
    }

    public static int HitCircle(Vector2 center, float radius, int damage, Transform source, HashSet<Health> already,
        bool parryable = true, bool reflectable = true, bool phaseable = true)
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(center, radius);
        return Apply(cols, damage, source, already, parryable, reflectable, phaseable);
    }

    private static int Apply(Collider2D[] cols, int damage, Transform source, HashSet<Health> already,
        bool parryable, bool reflectable, bool phaseable)
    {
        int landed = 0;
        foreach (Collider2D c in cols)
        {
            Health h = PlayerHealthOf(c);
            if (h == null || h.IsDead) continue;
            if (already != null && !already.Add(h)) continue;
            if (h.TakeBossDamage(damage, source, parryable, reflectable, phaseable)) landed++;
        }
        return landed;
    }
}
