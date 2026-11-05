using UnityEngine;

[DisallowMultipleComponent]
public class BossStatusEffects : MonoBehaviour
{
    private BossMovement movement;
    private float originalSpeed;
    private float slowUntil;
    private float slowMultiplier = 1f;
    private float lastAppliedSpeed;
    private bool slowActive;

    private void Awake()
    {
        movement = GetComponent<BossMovement>();
        if (movement != null) originalSpeed = movement.moveSpeed;
    }

    public void ApplySlow(float multiplier, float duration)
    {
        if (movement == null) movement = GetComponent<BossMovement>();
        if (movement == null) return;
        if (!slowActive) originalSpeed = movement.moveSpeed;
        slowMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
        movement.moveSpeed = originalSpeed * slowMultiplier;
        lastAppliedSpeed = movement.moveSpeed;
        slowUntil = Mathf.Max(slowUntil, Time.time + duration);
        slowActive = true;
    }

    private void Update()
    {
        if (!slowActive || movement == null) return;

        // Если другая система изменила скорость (например, вторая фаза), сохраняем новое базовое значение.
        if (!Mathf.Approximately(movement.moveSpeed, lastAppliedSpeed))
        {
            originalSpeed = movement.moveSpeed / slowMultiplier;
            movement.moveSpeed = originalSpeed * slowMultiplier;
            lastAppliedSpeed = movement.moveSpeed;
        }

        if (Time.time >= slowUntil)
        {
            movement.moveSpeed = originalSpeed;
            slowActive = false;
        }
    }
}
