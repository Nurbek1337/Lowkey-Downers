
using UnityEngine;
using UnityEngine.UI;

public class UniversalHealthBar : MonoBehaviour
{
    public HealthSystem healthSystem;
    public Slider slider;

    private void Start()
    {
        if (healthSystem == null || slider == null)
        {
            Debug.LogError("HealthSystem или Slider не назначен!");
            return;
        }

        slider.minValue = 0;
        slider.maxValue = healthSystem.maxHealth;
        slider.value = healthSystem.CurrentHealth;

        healthSystem.OnHealthChanged += UpdateHealthBar;
    }

    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        slider.maxValue = maxHealth;
        slider.value = currentHealth;
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
            healthSystem.OnHealthChanged -= UpdateHealthBar;
    }
}
