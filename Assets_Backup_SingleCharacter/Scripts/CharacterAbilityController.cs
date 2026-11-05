using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class CharacterAbilityController : MonoBehaviour
{
    [Header("Управление")]
    public KeyCode abilityKey = KeyCode.Q;
    [Header("Общие настройки")]
    public float abilityCooldown = 10f;
    public int defaultManaCost = 30;
    [Header("BladeMail — Gegwer")]
    public float bladeMailDuration = 4f;
    [Header("EchoSlam — Smirqc")]
    public float echoRadius = 3f;
    public int echoBaseDamage = 50;
    public int echoDamagePerExtraEnemy = 30;
    [Header("Requiem of Souls — Shoshkabes")]
    public float soulGainInterval = 1f;
    public int maxSouls = 20;
    public int requiemDamagePerSoul = 8;
    public float requiemSlowMultiplier = 0.5f;
    public float requiemSlowDuration = 3f;
    [Header("Multicast — Nonlyy")]
    [Range(0f, 1f)] public float multicastTwoChance = 0.75f;
    [Range(0f, 1f)] public float multicastThreeChance = 0.30f;
    [Range(0f, 1f)] public float multicastFourChance = 0.15f;
    public float multicastDuration = 8f;
    [Header("Heal — KiberKotleta")]
    public int healAmount = 15;
    [Header("Tame — Lili")]
    [Range(0f, 1f)] public float normalEnemyTameChance = 0.70f;
    [Range(0f, 1f)] public float bossTameChance = 0.30f;
    public float tameDuration = 15f;
    [Header("Phase — Gafasa")]
    public float phaseDuration = 6f;
    public float phaseImmunityWindow = 1f;
    [Header("Ссылки")]
    public Transform bossTarget;
    public CharacterPartyManager partyManager;

    public int CurrentSouls => souls;
    public bool IsMulticastReady => Time.time < multicastReadyUntil;

    private CharacterStats stats;
    private CharacterMana mana;
    private CharacterStatusEffects status;
    private float nextUseTime;
    private float nextSoulTime;
    private int souls;
    private float multicastReadyUntil;
    private int pendingMulticastCount = 1;
    private bool useMulticastDamage;
    private int multicastRequiemSouls;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        mana = GetComponent<CharacterMana>();
        status = GetComponent<CharacterStatusEffects>();
        if (partyManager == null) partyManager = FindAnyObjectByType<CharacterPartyManager>();
        if (stats != null && stats.character == CharacterId.Shoshkabes)
            nextSoulTime = Time.time + soulGainInterval;
        if (stats != null)
        {
            switch (stats.character)
            {
                case CharacterId.Gegwer: abilityCooldown = 10f; break;
                case CharacterId.Smirqc: abilityCooldown = 15f; break;
                case CharacterId.Shoshkabes: abilityCooldown = 15f; break;
                case CharacterId.Nonlyy: abilityCooldown = 8f; break;
                case CharacterId.KiberKotleta: abilityCooldown = 10f; break;
                case CharacterId.Lili: abilityCooldown = 15f; break;
                case CharacterId.Gafasa: abilityCooldown = 6f; break;
            }
        }
    }

    private void Update()
    {
        if (stats != null && stats.character == CharacterId.Shoshkabes && Time.time >= nextSoulTime)
        {
            souls = Mathf.Min(maxSouls, souls + 1);
            nextSoulTime = Time.time + soulGainInterval;
        }

        bool isActiveCharacter = partyManager == null || partyManager.ActiveCharacter == stats;
        if (Input.GetKeyDown(abilityKey) && enabled && isActiveCharacter)
            TryUseAbility(false);
    }

    public bool TryUseAbility(bool fromAI)
    {
        if (stats == null) stats = GetComponent<CharacterStats>();
        Health ownHealth = GetComponent<Health>();
        if (ownHealth != null && ownHealth.CurrentHealth <= 0) return false;
        if (stats == null || Time.time < nextUseTime) return false;
        if (stats.character == CharacterId.Ney) return false; // Ney uses E parry.
        if (mana == null) mana = GetComponent<CharacterMana>();
        if (mana != null && !mana.TrySpend(defaultManaCost))
        {
            if (!fromAI) Debug.Log("Недостаточно маны!");
            return false;
        }

        nextUseTime = Time.time + abilityCooldown;
        int repeatCount = stats.character != CharacterId.Nonlyy && partyManager != null
            ? partyManager.ConsumeMulticastCount()
            : 1;
        useMulticastDamage = repeatCount > 1;
        multicastRequiemSouls = 0;
        for (int i = 0; i < repeatCount; i++)
            ExecuteAbility();
        useMulticastDamage = false;
        multicastRequiemSouls = 0;
        return true;
    }

    private void ExecuteAbility()
    {
        float skillMultiplier = stats != null ? stats.SkillDamageMultiplier : 1f;
        switch (stats.character)
        {
            case CharacterId.Gegwer:
                if (status != null) status.ActivateBladeMail(bladeMailDuration, 1f);
                Debug.Log("Gegwer: BladeMail активирован на 4 секунды.");
                break;
            case CharacterId.Smirqc:
                EchoSlam(skillMultiplier);
                break;
            case CharacterId.Shoshkabes:
                Requiem(skillMultiplier);
                break;
            case CharacterId.Nonlyy:
                ActivateMulticast();
                break;
            case CharacterId.KiberKotleta:
                if (partyManager != null) partyManager.HealAll(healAmount);
                else if (GetComponent<Health>() != null) GetComponent<Health>().Heal(healAmount);
                break;
            case CharacterId.Lili:
                TameTarget();
                break;
            case CharacterId.Gafasa:
                if (status != null) status.ActivatePhase(phaseDuration, phaseImmunityWindow);
                Debug.Log("Gafasa: Phase активирован на 6 секунд, неуязвимость — первую секунду.");
                break;
        }
    }

    private void EchoSlam(float multiplier)
    {
        Vector2 center = transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, echoRadius);
        int enemyCount = 0;
        Health bossHealth = null;
        HashSet<Health> countedEnemies = new HashSet<Health>();

        foreach (Collider2D hit in hits)
        {
            if (hit.transform.root == transform.root) continue;
            if (hit.GetComponentInParent<CharacterStats>() != null) continue;
            Health health = hit.GetComponentInParent<Health>();
            if (health == null || !countedEnemies.Add(health)) continue;
            enemyCount++;
            if (health.GetComponent<BossMovement>() != null || health.GetComponentInParent<BossMovement>() != null)
                bossHealth = health;
        }

        int damage = Mathf.RoundToInt((echoBaseDamage + Mathf.Max(0, enemyCount - 1) * echoDamagePerExtraEnemy) * multiplier);
        if (bossHealth != null)
        {
            if (useMulticastDamage) bossHealth.TakeDamageRepeated(damage, transform);
            else bossHealth.TakeDamage(damage, transform);
        }
        Debug.Log("Smirqc: EchoSlam, урон " + damage + ", целей в радиусе: " + enemyCount);
    }

    private void Requiem(float multiplier)
    {
        int consumedSouls;
        if (useMulticastDamage && multicastRequiemSouls > 0)
        {
            consumedSouls = multicastRequiemSouls;
        }
        else
        {
            if (souls <= 0)
            {
                Debug.Log("У Shoshkabes пока нет душ.");
                return;
            }
            consumedSouls = souls;
            souls = 0;
            if (useMulticastDamage) multicastRequiemSouls = consumedSouls;
        }
        int damage = Mathf.RoundToInt(consumedSouls * requiemDamagePerSoul * multiplier);
        Transform target = bossTarget != null ? bossTarget : (partyManager != null ? partyManager.GetBossTarget() : null);
        if (target != null)
        {
            Health targetHealth = target.GetComponentInParent<Health>();
            if (targetHealth != null)
            {
                if (useMulticastDamage) targetHealth.TakeDamageRepeated(damage, transform);
                else targetHealth.TakeDamage(damage, transform);
            }
            BossStatusEffects slow = target.GetComponent<BossStatusEffects>();
            if (slow == null) slow = target.gameObject.AddComponent<BossStatusEffects>();
            slow.ApplySlow(requiemSlowMultiplier, requiemSlowDuration);
            SpawnRequiemWaves(transform.position, consumedSouls);
        }
        Debug.Log("Shoshkabes: Requiem выпустил " + consumedSouls + " душ, урон " + damage);
    }

    private void SpawnRequiemWaves(Vector3 center, int count)
    {
        int waveCount = Mathf.Clamp(Mathf.CeilToInt(count / 4f), 1, 5);
        for (int i = 0; i < waveCount; i++)
        {
            GameObject wave = new GameObject("RequiemWave");
            wave.transform.position = center;
            wave.transform.localScale = Vector3.one * (1.5f + i * 1.1f);
            SpriteRenderer renderer = wave.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateRingSprite();
            renderer.color = new Color(0.55f, 0.1f, 0.9f, 0.7f);
            renderer.sortingOrder = 15;
            StartCoroutine(ExpandWave(wave));
        }
    }

    private Sprite CreateRingSprite()
    {
        Texture2D texture = new Texture2D(64, 64);
        texture.filterMode = FilterMode.Bilinear;
        for (int x = 0; x < 64; x++)
        for (int y = 0; y < 64; y++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
            texture.SetPixel(x, y, d > 25f && d < 31f ? Color.white : Color.clear);
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
    }

    private IEnumerator ExpandWave(GameObject wave)
    {
        float timer = 0f;
        Vector3 start = wave.transform.localScale;
        while (timer < 0.5f && wave != null)
        {
            timer += Time.deltaTime;
            wave.transform.localScale = start * (1f + timer * 2f);
            yield return null;
        }
        if (wave != null) Destroy(wave);
    }

    private void ActivateMulticast()
    {
        float roll = Random.value;
        pendingMulticastCount = roll <= multicastFourChance ? 4 :
            roll <= multicastThreeChance ? 3 :
            roll <= multicastTwoChance ? 2 : 1;
        multicastReadyUntil = Time.time + multicastDuration;
        if (partyManager != null)
            partyManager.ActivateMulticast(pendingMulticastCount, multicastDuration);
        Debug.Log("Nonlyy: Multicast подготовлен — повторений: " + pendingMulticastCount + ". Следующая атака или навык партии в течение 8 секунд.");
    }

    public int ConsumeMulticastCount()
    {
        if (partyManager != null)
            return partyManager.ConsumeMulticastCount();
        if (stats == null || stats.character != CharacterId.Nonlyy || Time.time > multicastReadyUntil)
            return 1;
        int result = Mathf.Max(1, pendingMulticastCount);
        pendingMulticastCount = 1;
        multicastReadyUntil = 0f;
        return result;
    }

    private void TameTarget()
    {
        EnemyTameable[] enemies = FindObjectsByType<EnemyTameable>();
        foreach (EnemyTameable existing in enemies)
        {
            if (existing != null && existing.IsTamed)
            {
                Debug.Log("Lili уже контролирует одного союзного врага.");
                return;
            }
        }
        EnemyTameable selected = null;
        float closest = float.MaxValue;
        foreach (EnemyTameable enemy in enemies)
        {
            if (enemy == null || enemy.IsTamed) continue;
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < closest) { closest = distance; selected = enemy; }
        }

        if (selected != null)
        {
            if (Random.value <= normalEnemyTameChance &&
                selected.TryTame(partyManager != null ? partyManager.GetBossTarget() : bossTarget, tameDuration))
                Debug.Log("Lili приручила врага на 15 секунд.");
            else
                Debug.Log("Lili не смогла приручить обычного врага.");
            return;
        }

        Transform boss = bossTarget != null ? bossTarget : (partyManager != null ? partyManager.GetBossTarget() : null);
        if (boss != null && Random.value <= bossTameChance)
        {
            // В боссфайте босс не превращается в миньона: успешное применение прерывает его на 2 секунды.
            BossStunSystem.Stun(2f, transform);
            Debug.Log("Lili успешно остановила босса на 2 секунды!");
        }
        else Debug.Log("Lili не смогла никого приручить.");
    }

    private void OnDrawGizmosSelected()
    {
        if (stats != null && stats.character == CharacterId.Smirqc)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, echoRadius);
        }
    }
}
