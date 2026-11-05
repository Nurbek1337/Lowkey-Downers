using UnityEngine;

[DisallowMultipleComponent]
public class CharacterMana : MonoBehaviour
{
    public const int DefaultSkillCost = 30;
    public const float DefaultRegeneration = 5f;

    [Min(0)] public int skillCost = DefaultSkillCost;
    [Min(0f)] public float regenerationPerSecond = DefaultRegeneration;
    [SerializeField] private float currentMana;

    private CharacterStats stats;
    public float CurrentMana => currentMana;
    public float MaxMana => stats != null ? stats.MaxMana : 100f;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        if (stats != null) stats.LoadBaseStats();
        currentMana = MaxMana;
    }

    private void Update()
    {
        if (currentMana < MaxMana)
            currentMana = Mathf.Min(MaxMana, currentMana + regenerationPerSecond * Time.deltaTime);
    }

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
