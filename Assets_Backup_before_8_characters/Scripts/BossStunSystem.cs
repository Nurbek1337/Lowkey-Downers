using UnityEngine;
using System.Collections;

public static class BossStunSystem
{
    private static float stunEndTime;

    public static bool IsStunned => Time.time < stunEndTime;

    public static float RemainingTime =>
        IsStunned ? Mathf.Max(0f, stunEndTime - Time.time) : 0f;

    public static void Stun(float duration, Transform player)
    {
        if (duration <= 0f)
            return;

        stunEndTime = Mathf.Max(stunEndTime, Time.time + duration);
        Debug.Log("БОСС ОГЛУШЁН!");
    }

    public static void UpdateStun()
    {
        if (stunEndTime > 0f && Time.time >= stunEndTime)
        {
            stunEndTime = 0f;
            Debug.Log("БОСС ВЫШЕЛ ИЗ ОГЛУШЕНИЯ!");
        }
    }

    // Ожидание, которое не расходует время во время оглушения.
    public static IEnumerator Wait(float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            if (!IsStunned)
                timer += Time.deltaTime;

            yield return null;
        }
    }

    public static IEnumerator WaitFixed(float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            if (!IsStunned)
                timer += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }
    }
}
