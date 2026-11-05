using UnityEngine;

/// <summary>
/// Парирование Ney. Короткое окно (0,4 c) требует точного тайминга: удержание кнопки ничего не даёт,
/// после окна идёт перезарядка, а каждая попытка стоит ману. Успешное парирование оглушает босса,
/// возвращает ману и даёт короткую неуязвимость.
/// </summary>
public class ParrySystem : MonoBehaviour
{
    [Header("Управление")]
    public KeyCode parryKey = KeyCode.E;
    public KeyCode alternateKey = KeyCode.Q;

    [Header("Окно и перезарядка")]
    public float parryDuration = 0.4f;
    [Tooltip("Перезарядка после промаха (окно закончилось без парирования).")]
    public float parryCooldown = 1.5f;
    [Tooltip("Перезарядка после успешного парирования — награда за точность.")]
    public float successCooldown = 0.5f;

    [Header("Мана")]
    public int manaCost = 30;
    [Tooltip("Сколько маны возвращается при успешном парировании.")]
    public int manaRefundOnSuccess = 30;

    [Header("Награда за парирование")]
    public float bossStunDuration = 1f;
    public float successInvulnerability = 0.5f;

    [HideInInspector] public bool allowPlayerInput = true;

    public bool IsParrying { get; private set; }
    public float CooldownRemaining => Mathf.Max(0f, nextParryTime - Time.time);
    public float CooldownDuration { get; private set; } = 1f;

    private float parryEndTime;
    private float nextParryTime;
    private bool animationRequested;
    private CharacterMana mana;
    private Health health;

    void Awake()
    {
        mana = GetComponent<CharacterMana>();
        health = GetComponent<Health>();
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;

        if (IsParrying && Time.time >= parryEndTime)
        {
            // Окно закончилось без удара — промах, включается долгая перезарядка.
            IsParrying = false;
            nextParryTime = Time.time + parryCooldown;
            CooldownDuration = parryCooldown;
        }

        bool pressed = allowPlayerInput && (Input.GetKeyDown(parryKey) || Input.GetKeyDown(alternateKey));
        if (pressed) StartParryForAI();
    }

    public bool StartParryForAI()
    {
        if (IsParrying || Time.time < nextParryTime) return false;
        if (mana == null) mana = GetComponent<CharacterMana>();
        if (mana != null && !mana.TrySpend(manaCost)) return false;

        IsParrying = true;
        parryEndTime = Time.time + parryDuration;
        // Пока окно открыто, повторное нажатие невозможно (nextParryTime в будущем).
        nextParryTime = parryEndTime + parryCooldown;
        CooldownDuration = parryDuration + parryCooldown;
        animationRequested = true;
        CombatFx.Ring(transform.position, new Color(0.6f, 0.9f, 1f, 0.8f), 0.6f, 2.2f, parryDuration);
        return true;
    }

    /// <summary>Вызывается из Health при ударе босса. true — удар парирован.</summary>
    public bool TryBlock(Transform source)
    {
        if (!IsParrying) return false;

        IsParrying = false;
        nextParryTime = Time.time + successCooldown;
        CooldownDuration = successCooldown;

        if (mana == null) mana = GetComponent<CharacterMana>();
        if (mana != null) mana.Restore(manaRefundOnSuccess);
        if (health == null) health = GetComponent<Health>();
        if (health != null) health.GrantInvulnerability(successInvulnerability);

        BossStunSystem.Stun(bossStunDuration, transform);

        CombatFx.Flash(transform.position, new Color(0.7f, 0.95f, 1f, 0.95f), 3f, 0.3f);
        CombatFx.Ring(transform.position, new Color(1f, 1f, 1f, 0.9f), 0.8f, 4f, 0.35f);
        CombatFx.Shake(0.12f, 0.15f);
        Debug.Log("УСПЕШНОЕ ПАРИРОВАНИЕ! Босс оглушён.");
        return true;
    }

    public bool ConsumeAnimationRequest()
    {
        if (!animationRequested) return false;
        animationRequested = false;
        return true;
    }
}
