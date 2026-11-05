
using UnityEngine;
using System;

public class HealthSystem : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public int maxHealth = 100;

    public int CurrentHealth { get; private set; }

    public bool IsDead => CurrentHealth <= 0;

    // События изменения здоровья и смерти
    public event Action<int, int> OnHealthChanged;
    public event Action OnDeath;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0)
            return;

        CurrentHealth -= damage;

        if (CurrentHealth < 0)
            CurrentHealth = 0;

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (IsDead)
        {
            OnDeath?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        CurrentHealth += amount;

        if (CurrentHealth > maxHealth)
            CurrentHealth = maxHealth;

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void SetHealth(int amount)
    {
        CurrentHealth = Mathf.Clamp(amount, 0, maxHealth);

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (IsDead)
            OnDeath?.Invoke();
    }
}
