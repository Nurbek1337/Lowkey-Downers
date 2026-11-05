using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Глобальное оглушение босса. Оглушение нельзя "стакать" и нельзя ставить подряд:
/// после его окончания действует короткий иммунитет (PostStunImmunity), а на время
/// фазового перехода и финальной атаки босс помечает себя Immune = true.
/// </summary>
public static class BossStunSystem
{
    private static float stunEndTime;
    private static float immuneUntil;

    /// <summary>Пауза, в течение которой босса нельзя оглушить повторно после выхода из оглушения.</summary>
    public static float PostStunImmunity = 2f;

    /// <summary>Полный иммунитет к оглушению (фазовый переход, финальная атака).</summary>
    public static bool Immune { get; set; }

    public static bool IsStunned => Time.time < stunEndTime;
    public static float RemainingTime => IsStunned ? Mathf.Max(0f, stunEndTime - Time.time) : 0f;

    public static event Action<float> Stunned;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        // Защита от "залипшего" оглушения, если отключена перезагрузка домена при входе в Play Mode.
        stunEndTime = 0f;
        immuneUntil = 0f;
        Immune = false;
        PostStunImmunity = 2f;
        Stunned = null;
    }

    /// <summary>Пытается оглушить босса. Возвращает true, если оглушение сработало.</summary>
    public static bool Stun(float duration, Transform source)
    {
        if (duration <= 0f || Immune) return false;
        if (IsStunned || Time.time < immuneUntil) return false;

        stunEndTime = Time.time + duration;
        immuneUntil = stunEndTime + PostStunImmunity;
        Debug.Log("БОСС ОГЛУШЁН на " + duration.ToString("0.0") + " c.");
        Stunned?.Invoke(duration);
        return true;
    }

    /// <summary>Принудительно снимает оглушение (смерть босса, переход фазы).</summary>
    public static void Clear()
    {
        stunEndTime = 0f;
    }

    public static void UpdateStun()
    {
        if (stunEndTime > 0f && Time.time >= stunEndTime)
            stunEndTime = 0f;
    }

    // Ожидание, которое не расходует время во время оглушения.
    public static IEnumerator Wait(float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            if (!IsStunned) timer += Time.deltaTime;
            yield return null;
        }
    }

    public static IEnumerator WaitFixed(float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            if (!IsStunned) timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }
}
