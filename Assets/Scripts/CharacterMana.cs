using UnityEngine;

[DisallowMultipleComponent]
public class CharacterMana : MonoBehaviour
{
    public const int DefaultSkillCost = 30;
    public const float DefaultRegeneration = 5f;

    [Tooltip("Стоимость способности (может переопределяться профилем персонажа).")]
    [Min(0)] public int skillCost = DefaultSkillCost;
    [Tooltip("Восстановление маны в секунду (может переопределяться профилем персонажа).")]
    [Min(0f)] public float regenerationPerSecond = DefaultRegeneration;
    [SerializeField] private float currentMana;

    private CharacterStats stats;
    public float CurrentMana => currentMana;
    public float MaxMana => stats != null ? stats.MaxMana : 100f;
    public float Normalized => MaxMana > 0f ? currentMana / MaxMana : 0f;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        if (stats != null) stats.LoadBaseStats();
        ApplyProfile();
        currentMana = MaxMana;
    }

    private void ApplyProfile()
    {
        if (stats == null) return;
        skillCost = stats.AbilityManaCost;
        regenerationPerSecond = stats.ManaRegeneration;
    }

    private void Update()
    {
        if (currentMana < MaxMana)
            currentMana = Mathf.Min(MaxMana, currentMana + regenerationPerSecond * Time.deltaTime);
    }

    public void RefreshAfterCharacterSelection()
    {
        stats = GetComponent<CharacterStats>();
        if (stats != null) stats.LoadBaseStats();
        ApplyProfile();
        currentMana = MaxMana;
    }

    public bool HasEnough(int amount) { return currentMana >= amount; }

    public bool TrySpend(int amount)
    {
        if (currentMana < amount) return false;
        currentMana -= amount;
        return true;
    }

    public void Restore(float amount)
    {
        currentMana = Mathf.Clamp(currentMana + amount, 0f, MaxMana);
    }
}
