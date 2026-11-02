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

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(10);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isInvulnerable ||
            externalInvulnerability ||
            currentHealth <= 0)
            return;

        currentHealth -= damage;

        // Не даём боссу умереть до финальной атаки
        if (protectAtLowHealth && currentHealth <= protectionHealth)
        {
            currentHealth = protectionHealth;
        }

        if (currentHealth < 0)
            currentHealth = 0;

        Debug.Log("Здоровье: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(Invulnerability());
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