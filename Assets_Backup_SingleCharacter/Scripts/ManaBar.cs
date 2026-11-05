using UnityEngine;
using UnityEngine.UI;

public class ManaBar : MonoBehaviour
{
    public CharacterMana mana;
    public Slider slider;

    private void Start()
    {
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (mana == null || slider == null) return;
        slider.maxValue = mana.MaxMana;
        slider.value = mana.CurrentMana;
    }
}
