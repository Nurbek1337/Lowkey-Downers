using UnityEngine;

public class ParrySystem : MonoBehaviour
{
    [Header("Настройки парирования")]
    public KeyCode parryKey = KeyCode.E;
    public float parryDuration = 0.25f;
    public float parryCooldown = 0.8f;

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

        Debug.Log("УСПЕШНОЕ ПАРИРОВАНИЕ! Урон заблокирован.");

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
