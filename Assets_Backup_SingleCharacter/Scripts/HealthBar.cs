
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Health health;
    public Slider slider;

    void Start()
    {
        slider.maxValue = health.maxHealth;
        slider.value = health.CurrentHealth;
    }

    void Update()
    {
        slider.value = health.CurrentHealth;
    }
}
