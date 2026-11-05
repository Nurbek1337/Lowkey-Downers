using UnityEngine;

/// <summary>
/// Парирование Ney. Поддерживает ручной ввод игрока и автоматический вызов ИИ.
/// </summary>
public class ParrySystem : MonoBehaviour
{
    [Header("Настройки парирования")]
    public KeyCode parryKey = KeyCode.E;
    public float parryDuration = 0.4f;
    public float parryCooldown = 0.8f;

    [Header("Управление вводом")]
    public bool allowPlayerInput = true;

    [Header("Оглушение босса")]
    public float bossStunDuration = 1f;

    public bool IsParrying { get; private set; }

    private float parryEndTime;
    private float nextParryTime;
    private bool animationRequested;

    private void Update()
    {
        if (IsParrying && Time.time >= parryEndTime)
            IsParrying = false;

        if (allowPlayerInput && Input.GetKeyDown(parryKey))
            StartParry();
    }

    /// <summary>Запускает парирование из кода, например для союзника под управлением ИИ.</summary>
    public bool StartParryForAI()
    {
        return StartParry();
    }

    private bool StartParry()
    {
        if (IsParrying || Time.time < nextParryTime)
            return false;

        IsParrying = true;
        parryEndTime = Time.time + parryDuration;
        nextParryTime = Time.time + parryCooldown;
        animationRequested = true;
        return true;
    }

    public bool TryBlock()
    {
        if (!IsParrying)
            return false;

        IsParrying = false;
        BossStunSystem.Stun(bossStunDuration, transform);
        Debug.Log("УСПЕШНОЕ ПАРИРОВАНИЕ! Босс оглушён.");
        return true;
    }

    public bool ConsumeAnimationRequest()
    {
        if (!animationRequested)
            return false;

        animationRequested = false;
        return true;
    }
}
