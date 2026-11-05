using UnityEngine;

public enum CharacterId
{
    Ney,
    Gegwer,
    Smirqc,
    Shoshkabes,
    Nonlyy,
    KiberKotleta,
    Lili,
    Gafasa
}

[DisallowMultipleComponent]
public class CharacterStats : MonoBehaviour
{
    [Header("Персонаж")]
    public CharacterId character = CharacterId.Ney;

    [Header("Дополнительный интеллект")]
    [Tooltip("Базовые значения интеллекта не задаются автоматически. 0 = нейтральный множитель.")]
    [Min(0)] public float intelligenceBonus = 0f;

    [Header("Снаряд персонажа (необязательно)")]
    public GameObject projectilePrefabOverride;

    [Header("Базовая атака (можно настроить в Inspector)")]
    public bool useCharacterAttackDefaults = true;
    [Min(0.1f)] public float baseAttackSpeed = 1f;
    [Min(0)] public int baseAttackDamage = 10;

    [Header("Текущие характеристики (только чтение в игре)")]
    [SerializeField] private int maxHealth;
    [SerializeField] private float agility;
    [SerializeField] private int baseMana;

    public int MaxHealth => maxHealth;
    public float Agility => agility;
    public int MaxMana => Mathf.Max(1, Mathf.RoundToInt(baseMana * (1f + intelligenceBonus / 100f)));
    public float SkillDamageMultiplier => 1f + intelligenceBonus / 100f;
    public float AttackSpeed => baseAttackSpeed;
    public int AttackDamage => baseAttackDamage;
    public float AgilityMultiplier => Mathf.Max(0.25f, agility / 100f);

    private void Awake()
    {
        LoadBaseStats();
        ApplyToComponents();
    }

    public void LoadBaseStats()
    {
        switch (character)
        {
            case CharacterId.Ney:          maxHealth = 50;  agility = 200; baseMana = 100; if (useCharacterAttackDefaults) { baseAttackSpeed = 1.5f; baseAttackDamage = 7; } break;
            case CharacterId.Gegwer:       maxHealth = 300; agility = 50;  baseMana = 100; if (useCharacterAttackDefaults) { baseAttackSpeed = 0.85f; baseAttackDamage = 12; } break;
            case CharacterId.Smirqc:       maxHealth = 310; agility = 30;  baseMana = 120; if (useCharacterAttackDefaults) { baseAttackSpeed = 0.65f; baseAttackDamage = 18; } break;
            case CharacterId.Shoshkabes:   maxHealth = 140; agility = 90;  baseMana = 160; if (useCharacterAttackDefaults) { baseAttackSpeed = 1f; baseAttackDamage = 10; } break;
            case CharacterId.Nonlyy:       maxHealth = 140; agility = 80;  baseMana = 200; if (useCharacterAttackDefaults) { baseAttackSpeed = 1f; baseAttackDamage = 10; } break;
            case CharacterId.KiberKotleta: maxHealth = 130; agility = 80;  baseMana = 170; if (useCharacterAttackDefaults) { baseAttackSpeed = 1f; baseAttackDamage = 10; } break;
            case CharacterId.Lili:         maxHealth = 80;  agility = 110; baseMana = 120; if (useCharacterAttackDefaults) { baseAttackSpeed = 1f; baseAttackDamage = 10; } break;
            case CharacterId.Gafasa:       maxHealth = 100; agility = 160; baseMana = 100; if (useCharacterAttackDefaults) { baseAttackSpeed = 1f; baseAttackDamage = 10; } break;
        }
    }

    public void ApplyToComponents()
    {
        Health health = GetComponent<Health>();
        if (health != null) health.maxHealth = maxHealth;

        SideViewMovement movement = GetComponent<SideViewMovement>();
        if (movement != null) movement.SetCharacterSpeedMultiplier(AgilityMultiplier);

        PlayerDash dash = GetComponent<PlayerDash>();
        if (dash != null) dash.SetAgilityMultiplier(AgilityMultiplier);

        PlayerShooting shooting = GetComponent<PlayerShooting>();
        if (shooting != null)
        {
            shooting.SetCharacterStats(this);
            if (projectilePrefabOverride != null)
                shooting.projectilePrefab = projectilePrefabOverride;
        }
    }
}
