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

/// <summary>
/// Профиль персонажа. Все значения настраиваются в Inspector (CharacterStats -> Profiles) без правки кода.
/// Выносливость из старой таблицы не используется.
/// </summary>
[System.Serializable]
public class CharacterProfile
{
    public CharacterId id;
    [Header("Таблица характеристик")]
    public int maxHealth = 100;
    public float agility = 100f;
    public int maxMana = 100;
    [Header("Базовая атака")]
    public int attackDamage = 10;
    [Tooltip("Множитель скорости атаки: выстрелов в секунду = attackSpeed / PlayerShooting.fireRate.")]
    public float attackSpeed = 1f;
    public float projectileSpeed = 12f;
    [Header("Способность и мана")]
    public float intelligence = 0f;
    public int abilityManaCost = 30;
    public float manaRegeneration = 5f;
    public float abilityCooldown = 10f;
    [Header("Баланс боссфайта")]
    [Tooltip("Множитель урона, получаемого от атак босса (компенсирует разницу в запасе здоровья).")]
    public float bossDamageTaken = 1f;

    public CharacterProfile() { }

    public CharacterProfile(CharacterId id, int hp, float agi, int mana, int dmg, float atkSpeed, float cooldown, float bossTaken)
    {
        this.id = id; maxHealth = hp; agility = agi; maxMana = mana;
        attackDamage = dmg; attackSpeed = atkSpeed; abilityCooldown = cooldown; bossDamageTaken = bossTaken;
    }

    public static CharacterProfile[] CreateDefaults()
    {
        return new[]
        {
            //                          id                     HP   Agi  Mana dmg  spd   cd    bossTaken
            new CharacterProfile(CharacterId.Ney,          50,  200, 100, 5,  1.60f, 1.5f, 0.80f),
            new CharacterProfile(CharacterId.Gegwer,       300, 50,  100, 10, 0.75f, 10f,  1.40f),
            new CharacterProfile(CharacterId.Smirqc,       310, 30,  120, 11, 0.65f, 15f,  1.40f),
            new CharacterProfile(CharacterId.Shoshkabes,   140, 90,  160, 7,  1.00f, 15f,  1.00f),
            new CharacterProfile(CharacterId.Nonlyy,       140, 80,  200, 6,  1.00f, 8f,   0.95f),
            new CharacterProfile(CharacterId.KiberKotleta, 130, 80,  170, 7,  1.00f, 10f,  1.00f),
            new CharacterProfile(CharacterId.Lili,         80,  110, 120, 9,  1.00f, 15f,  0.70f),
            new CharacterProfile(CharacterId.Gafasa,       100, 160, 100, 7,  1.20f, 6f,   0.90f),
        };
    }
}

[DisallowMultipleComponent]
public class CharacterStats : MonoBehaviour
{
    [Header("Персонаж")]
    public CharacterId character = CharacterId.Ney;

    [Header("Профили всех персонажей (Inspector)")]
    public CharacterProfile[] profiles = CharacterProfile.CreateDefaults();

    [Header("Мобильность от ловкости")]
    [Tooltip("Множитель = (Ловкость/100)^экспонента. 1 = линейно; меньше 1 = сглаженно.")]
    [Range(0.1f, 1f)] public float agilityExponent = 0.35f;
    [Min(0.1f)] public float minMobility = 0.7f;
    [Min(1f)] public float maxMobility = 1.4f;

    [Header("Дополнительный интеллект (%)")]
    [Tooltip("0 = нейтрально. Увеличивает урон/силу способностей и немного регенерацию маны.")]
    [Min(0)] public float intelligenceBonus = 0f;

    [Header("Снаряд персонажа (необязательно)")]
    public GameObject projectilePrefabOverride;

    [Header("Базовая атака")]
    [Tooltip("Если включено — урон и скорость атаки берутся из профиля. Выключи, чтобы задать вручную ниже.")]
    public bool useCharacterAttackDefaults = true;
    [Min(0.1f)] public float baseAttackSpeed = 1f;
    [Min(0)] public int baseAttackDamage = 10;

    [Header("Текущие характеристики (только чтение в игре)")]
    [SerializeField] private int maxHealth;
    [SerializeField] private float agility;
    [SerializeField] private int baseMana;

    private CharacterProfile active;

    public CharacterProfile Profile
    {
        get
        {
            if (active == null || active.id != character) LoadBaseStats();
            return active;
        }
    }

    public int MaxHealth => maxHealth;
    public float Agility => agility;
    public int MaxMana => Mathf.Max(1, baseMana);
    public float TotalIntelligence => (Profile != null ? Profile.intelligence : 0f) + intelligenceBonus;
    public float SkillDamageMultiplier => 1f + TotalIntelligence / 100f;
    public float AttackSpeed => baseAttackSpeed;
    public int AttackDamage => baseAttackDamage;
    public float ProjectileSpeed => Profile != null ? Profile.projectileSpeed : 12f;
    public int AbilityManaCost => Profile != null ? Profile.abilityManaCost : CharacterMana.DefaultSkillCost;
    public float ManaRegeneration => (Profile != null ? Profile.manaRegeneration : CharacterMana.DefaultRegeneration) * (1f + TotalIntelligence / 200f);
    public float AbilityCooldown => Profile != null ? Profile.abilityCooldown : 10f;
    public float BossDamageTaken => Profile != null ? Profile.bossDamageTaken : 1f;

    /// <summary>Множитель скорости передвижения и рывка от ловкости.</summary>
    public float AgilityMultiplier => Mathf.Clamp(Mathf.Pow(Mathf.Max(1f, agility) / 100f, agilityExponent), minMobility, maxMobility);

    private void Awake()
    {
        LoadBaseStats();
        ApplyToComponents();
    }

    [ContextMenu("Сбросить профили к значениям по умолчанию")]
    public void ResetProfiles()
    {
        profiles = CharacterProfile.CreateDefaults();
        LoadBaseStats();
    }

    public void LoadBaseStats()
    {
        if (profiles == null || profiles.Length == 0) profiles = CharacterProfile.CreateDefaults();

        active = null;
        foreach (CharacterProfile p in profiles)
            if (p != null && p.id == character) { active = p; break; }
        if (active == null)
        {
            foreach (CharacterProfile p in CharacterProfile.CreateDefaults())
                if (p.id == character) { active = p; break; }
        }

        maxHealth = active.maxHealth;
        agility = active.agility;
        baseMana = active.maxMana;
        if (useCharacterAttackDefaults)
        {
            baseAttackSpeed = active.attackSpeed;
            baseAttackDamage = active.attackDamage;
        }
    }

    public void ApplyToComponents()
    {
        if (active == null) LoadBaseStats();

        Health health = GetComponent<Health>();
        if (health != null)
        {
            health.SetMaxHealthAndRefill(maxHealth);
            health.bossDamageMultiplier = BossDamageTaken;
        }

        SideViewMovement movement = GetComponent<SideViewMovement>();
        if (movement != null) movement.SetCharacterSpeedMultiplier(AgilityMultiplier);

        PlayerDash dash = GetComponent<PlayerDash>();
        if (dash != null) dash.SetAgilityMultiplier(AgilityMultiplier);

        PlayerShooting shooting = GetComponent<PlayerShooting>();
        if (shooting != null)
        {
            shooting.SetCharacterStats(this);
            if (projectilePrefabOverride != null) shooting.projectilePrefab = projectilePrefabOverride;
        }
    }
}
