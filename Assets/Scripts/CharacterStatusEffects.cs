using UnityEngine;

/// <summary>
/// Временные эффекты игрового персонажа: BladeMail (Gegwer) и Phase (Gafasa).
/// Все параметры передаются при активации из CharacterAbilityController, где они настраиваются в Inspector.
/// </summary>
[DisallowMultipleComponent]
public class CharacterStatusEffects : MonoBehaviour
{
    // ---------------- BladeMail ----------------
    public bool IsBladeMailActive { get; private set; }
    public float BladeMailRemaining => IsBladeMailActive ? Mathf.Max(0f, bladeMailEnd - Time.time) : 0f;

    // ---------------- Phase ----------------
    public bool IsPhaseActive { get; private set; }
    public float PhaseRemaining => IsPhaseActive ? Mathf.Max(0f, phaseEnd - Time.time) : 0f;

    /// <summary>Полная защита от атак босса (только первая секунда способности Gafasa).</summary>
    public bool IsBossImmune { get; private set; }

    /// <summary>Совместимость со старым кодом.</summary>
    public bool IsDamageImmune => IsBossImmune;

    private float bladeMailEnd;
    private float bladeMailMultiplier = 1f;
    private int bladeMailPerHitCap = int.MaxValue;
    private int bladeMailTotalCap = int.MaxValue;
    private int bladeMailReflectedTotal;

    private float phaseEnd;
    private float phaseImmunityEnd;
    private float phaseSpeedBonus;

    private SpriteRenderer visual;
    private SideViewMovement movement;
    private float blockedFlashUntil;

    private void Awake()
    {
        Transform v = transform.Find("PlayerVisual");
        if (v != null) visual = v.GetComponent<SpriteRenderer>();
        movement = GetComponent<SideViewMovement>();
    }

    // ------------------------------------------------------------------
    public void ActivateBladeMail(float duration, float reflectMultiplier, int perHitCap, int totalCap)
    {
        bladeMailEnd = Time.time + duration;
        bladeMailMultiplier = Mathf.Max(0f, reflectMultiplier);
        bladeMailPerHitCap = perHitCap > 0 ? perHitCap : int.MaxValue;
        bladeMailTotalCap = totalCap > 0 ? totalCap : int.MaxValue;
        bladeMailReflectedTotal = 0;
        IsBladeMailActive = true;
    }

    /// <summary>Отражает урон источнику атаки. Урон ограничен лимитами за удар и за активацию.</summary>
    public void ReflectTo(Transform source, int receivedDamage)
    {
        if (!IsBladeMailActive || source == null || receivedDamage <= 0) return;

        Health target = source.GetComponentInParent<Health>();
        if (target == null || target.gameObject == gameObject) return;
        if (target.IsInvulnerable) return;

        int amount = Mathf.RoundToInt(receivedDamage * bladeMailMultiplier);
        amount = Mathf.Min(amount, bladeMailPerHitCap);
        amount = Mathf.Min(amount, bladeMailTotalCap - bladeMailReflectedTotal);
        if (amount <= 0) return;

        bladeMailReflectedTotal += amount;
        // attacker = null: отражённый урон не может отразиться повторно.
        target.TakeDamage(amount, null);
        CombatFx.Flash(target.transform.position + Vector3.up * 2.5f, new Color(1f, 0.35f, 0.3f, 0.9f), 1.2f, 0.25f);
    }

    public void ActivatePhase(float duration, float immunityWindow, float speedBonus)
    {
        phaseEnd = Time.time + duration;
        phaseImmunityEnd = Time.time + Mathf.Min(duration, immunityWindow);
        phaseSpeedBonus = speedBonus;
        IsPhaseActive = true;
        IsBossImmune = immunityWindow > 0f;
        if (movement != null) movement.SetTemporarySpeedMultiplier(1f + phaseSpeedBonus);
    }

    public void NotifyBlocked()
    {
        blockedFlashUntil = Time.time + 0.12f;
        CombatFx.Flash(transform.position, new Color(0.5f, 1f, 1f, 0.85f), 1.4f, 0.2f);
    }

    private void Update()
    {
        if (IsBladeMailActive && Time.time >= bladeMailEnd) IsBladeMailActive = false;

        if (IsBossImmune && Time.time >= phaseImmunityEnd) IsBossImmune = false;

        if (IsPhaseActive && Time.time >= phaseEnd)
        {
            IsPhaseActive = false;
            if (movement != null) movement.SetTemporarySpeedMultiplier(1f);
        }

        UpdateTint();
    }

    private void UpdateTint()
    {
        if (visual == null) return;

        Color c = Color.white;
        if (IsBladeMailActive) c = new Color(1f, 0.55f, 0.5f, 1f);
        if (IsPhaseActive) c = new Color(0.75f, 1f, 1f, 1f);
        if (IsBossImmune) c = new Color(0.55f, 1f, 1f, 0.45f + 0.15f * Mathf.Sin(Time.time * 30f));
        if (Time.time < blockedFlashUntil) c = Color.white;
        visual.color = c;
    }

    private void OnDisable()
    {
        if (visual != null) visual.color = Color.white;
    }
}
