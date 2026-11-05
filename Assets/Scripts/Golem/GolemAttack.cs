using UnityEngine;
using System.Collections;

/// <summary>
/// Базовый класс атаки каменного голема. Каждая атака — отдельный компонент на объекте Boss.
/// Общие параметры (урон, радиус, скорость, время подготовки, длительность, перезарядка, зона поражения)
/// настраиваются в Inspector. Конкретная атака сама решает, как их использовать.
/// Выполняется ТОЛЬКО через GolemBoss, поэтому две атаки одновременно запуститься не могут.
/// </summary>
public abstract class GolemAttack : MonoBehaviour
{
    [Header("Общие параметры атаки")]
    public string attackName = "Attack";
    [Tooltip("Урон одного попадания.")]
    public int damage = 15;
    [Tooltip("Радиус опасной зоны (если применимо).")]
    public float radius = 2f;
    [Tooltip("Скорость волн / снарядов / падения.")]
    public float speed = 8f;
    [Tooltip("Время подготовки: босс показывает анимацию и телеграф опасной зоны.")]
    public float windup = 1f;
    [Tooltip("Длительность активной части или время восстановления после атаки.")]
    public float duration = 1f;
    [Tooltip("Перезарядка этой атаки после использования.")]
    public float cooldown = 6f;
    [Tooltip("Размер зоны поражения (ширина, высота).")]
    public Vector2 hitZone = new Vector2(1.2f, 1.7f);

    [Header("Выбор атаки")]
    [Min(0f)] public float weight = 1f;
    [Range(1, 2)] public int minPhase = 1;
    public float minPlayerDistance = 0f;
    public float maxPlayerDistance = 99f;
    [Tooltip("Можно ли использовать эту атаку как продолжение комбо во второй фазе.")]
    public bool allowedInCombo = true;

    [Header("Вторая фаза")]
    public float phase2DamageMultiplier = 1.15f;
    public float phase2SpeedMultiplier = 1.2f;
    public float phase2WindupMultiplier = 0.85f;
    public float phase2CooldownMultiplier = 0.8f;

    private float nextReadyTime;

    public bool IsReady => Time.time >= nextReadyTime;

    public virtual bool CanUse(GolemBoss boss)
    {
        if (!enabled || weight <= 0f) return false;
        if (boss.Phase < minPhase || !IsReady) return false;
        float d = Mathf.Abs(boss.PlayerPos.x - boss.transform.position.x);
        return d >= minPlayerDistance && d <= maxPlayerDistance;
    }

    public void MarkUsed(GolemBoss boss)
    {
        float cd = cooldown * boss.CooldownScale * (boss.Phase >= 2 ? phase2CooldownMultiplier : 1f);
        nextReadyTime = Time.time + cd;
    }

    public abstract IEnumerator Execute(GolemBoss boss);

    protected int Dmg(GolemBoss b) { return Mathf.RoundToInt(damage * (b.Phase >= 2 ? phase2DamageMultiplier : 1f)); }
    protected float Spd(GolemBoss b) { return speed * (b.Phase >= 2 ? phase2SpeedMultiplier : 1f) * b.SpeedMultiplier; }
    protected float Wind(GolemBoss b) { return windup * (b.Phase >= 2 ? phase2WindupMultiplier : 1f); }

    protected static readonly Color WarnColor = new Color(1f, 0.32f, 0.1f, 0.6f);
    protected static readonly Color StoneTint = new Color(0.7f, 0.62f, 0.55f, 1f);
}
