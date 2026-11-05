using UnityEngine;

/// <summary>
/// Замедление босса. Для GolemBoss замедление применяется внутри самого голема (GolemBoss.ApplySlow),
/// этот компонент оставлен для старого босса с BossMovement.
/// </summary>
[DisallowMultipleComponent]
public class BossStatusEffects : MonoBehaviour
{
    private BossMovement movement;
    private float originalSpeed;
    private float slowUntil;
    private float slowMultiplier = 1f;
    private bool slowActive;

    private void Awake()
    {
        movement = GetComponent<BossMovement>();
        if (movement != null) originalSpeed = movement.moveSpeed;
    }

    public void ApplySlow(float multiplier, float duration)
    {
        GolemBoss golem = GetComponent<GolemBoss>();
        if (golem != null) { golem.ApplySlow(multiplier, duration); return; }

        if (movement == null) movement = GetComponent<BossMovement>();
        if (movement == null) return;
        if (!slowActive) originalSpeed = movement.moveSpeed;
        slowMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
        movement.moveSpeed = originalSpeed * slowMultiplier;
        slowUntil = Mathf.Max(slowUntil, Time.time + duration);
        slowActive = true;
    }

    private void Update()
    {
        if (!slowActive || movement == null) return;
        if (Time.time >= slowUntil)
        {
            movement.moveSpeed = originalSpeed;
            slowActive = false;
        }
    }
}
