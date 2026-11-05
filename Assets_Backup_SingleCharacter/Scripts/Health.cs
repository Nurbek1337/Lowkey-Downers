using UnityEngine;
using System.Collections;

public class Health : MonoBehaviour
{
    public int maxHealth = 50;
    public float invulnerabilityTime = 1f;

    [Header("Защита босса")]
    public bool protectAtLowHealth = false;
    public int protectionHealth = 10;

    private int currentHealth;
    public int CurrentHealth => currentHealth;

    private bool isInvulnerable = false;
    private bool externalInvulnerability = false;

    private SpriteRenderer spriteRenderer;
    private ParrySystem parrySystem;

    void Start()
    {
        currentHealth = maxHealth;

        Transform visual = transform.Find("PlayerVisual");

        if (visual != null)
            spriteRenderer = visual.GetComponent<SpriteRenderer>();
        else
            spriteRenderer = GetComponent<SpriteRenderer>();

        parrySystem = GetComponent<ParrySystem>();
        if (parrySystem == null && (CompareTag("Player") || gameObject.name == "BossPlayer2"))
            parrySystem = gameObject.AddComponent<ParrySystem>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H) && CompareTag("Player"))
        {
            TakeDamage(10);
        }
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, null, false);
    }

    public void TakeDamage(int damage, Transform attacker)
    {
        TakeDamage(damage, attacker, false);
    }

    public void TakeDamageRepeated(int damage, Transform attacker)
    {
        // Используется только для срабатываний Multicast: обычная неуязвимость
        // между попаданиями не съедает повторные удары одного прокаста.
        TakeDamage(damage, attacker, true);
    }

    private void TakeDamage(int damage, Transform attacker, bool bypassHitInvulnerability)
    {
        if (damage <= 0 || currentHealth <= 0) return;
        CharacterStatusEffects characterEffects = GetComponent<CharacterStatusEffects>();
        if (characterEffects != null && characterEffects.IsDamageImmune) return;

        // BladeMail отражает урон даже во время обычной неуязвимости после попадания.
        if (characterEffects != null && attacker != null)
        {
            int reflectedDamage = characterEffects.GetReflectedDamage(damage);
            if (reflectedDamage > 0)
            {
                Health attackerHealth = attacker.GetComponentInParent<Health>();
                if (attackerHealth != null) attackerHealth.TakeDamage(reflectedDamage, null);
            }
        }

        if (externalInvulnerability || (isInvulnerable && !bypassHitInvulnerability)) return;
        if (parrySystem == null) parrySystem = GetComponent<ParrySystem>();
        if (parrySystem != null && parrySystem.enabled && parrySystem.TryBlock()) return;

        currentHealth -= damage;
        if (protectAtLowHealth && currentHealth <= protectionHealth)
            currentHealth = protectionHealth;
        if (currentHealth < 0) currentHealth = 0;
        Debug.Log("Здоровье: " + currentHealth);
        if (currentHealth <= 0) { Die(); return; }
        if (!isInvulnerable)
            StartCoroutine(Invulnerability());
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || currentHealth <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        Debug.Log(gameObject.name + " восстановил здоровье. Текущее HP: " + currentHealth);
    }

    public void SetExternalInvulnerability(bool value)
    {
        externalInvulnerability = value;

        // После финальной атаки снова разрешаем убить босса
        if (!value)
            protectAtLowHealth = false;

        Debug.Log(
            gameObject.name +
            " неуязвимость: " + value
        );
    }

    IEnumerator Invulnerability()
    {
        isInvulnerable = true;

        float timer = 0;

        while (timer < invulnerabilityTime)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = !spriteRenderer.enabled;

            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        isInvulnerable = false;
    }

    void Die()
    {
        Debug.Log(gameObject.name + " погиб!");
    }
}
