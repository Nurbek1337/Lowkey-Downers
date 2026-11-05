using UnityEngine;

[DisallowMultipleComponent]
public class CharacterStatusEffects : MonoBehaviour
{
    public bool IsBladeMailActive { get; private set; }
    public bool IsPhaseActive { get; private set; }
    public bool IsDamageImmune { get; private set; }

    private float bladeMailEnd;
    private float phaseEnd;
    private float phaseImmunityEnd;
    private float bladeMailMultiplier = 1f;

    public void ActivateBladeMail(float duration, float reflectMultiplier = 1f)
    {
        bladeMailEnd = Mathf.Max(bladeMailEnd, Time.time + duration);
        bladeMailMultiplier = reflectMultiplier;
        IsBladeMailActive = true;
    }

    public int GetReflectedDamage(int incomingDamage)
    {
        return IsBladeMailActive ? Mathf.RoundToInt(incomingDamage * bladeMailMultiplier) : 0;
    }

    public void ActivatePhase(float duration, float immunityWindow)
    {
        phaseEnd = Time.time + duration;
        phaseImmunityEnd = Time.time + immunityWindow;
        IsPhaseActive = true;
        IsDamageImmune = immunityWindow > 0f;
    }

    private void Update()
    {
        if (IsBladeMailActive && Time.time >= bladeMailEnd)
            IsBladeMailActive = false;

        if (IsPhaseActive && Time.time >= phaseEnd)
            IsPhaseActive = false;

        if (IsDamageImmune && Time.time >= phaseImmunityEnd)
            IsDamageImmune = false;
    }
}
