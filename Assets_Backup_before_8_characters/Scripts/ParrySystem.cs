using UnityEngine;

public class ParrySystem : MonoBehaviour
{
    [Header("Настройки парирования")]
    public KeyCode parryKey = KeyCode.E;
    public float parryDuration = 0.25f;
    public float parryCooldown = 0.8f;

    [Header("Оглушение босса")]
    public float bossStunDuration = 1f;

    public bool IsParrying { get; private set; }

    private float parryEndTime;
    private float nextParryTime;
    private bool animationRequested;

    void Update()
    {
        if (IsParrying && Time.time >= parryEndTime)
            IsParrying = false;

        if (Input.GetKeyDown(parryKey) &&
            !IsParrying &&
            Time.time >= nextParryTime)
        {
            StartParry();
        }
    }

    void StartParry()
    {
        IsParrying = true;
        parryEndTime = Time.time + parryDuration;
        nextParryTime = Time.time + parryCooldown;
        animationRequested = true;

        Debug.Log("Игрок начал парирование!");
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
