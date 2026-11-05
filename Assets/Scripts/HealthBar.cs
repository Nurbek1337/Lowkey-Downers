using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Health health;
    public Slider slider;

    void Start()
    {
        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    // Максимум обновляется каждый кадр: после выбора персонажа (HP 50..310) шкала должна менять масштаб.
    private void Refresh()
    {
        if (health == null || slider == null) return;
        slider.maxValue = health.maxHealth;
        slider.value = health.CurrentHealth;
    }
}
