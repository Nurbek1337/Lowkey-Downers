using UnityEngine;

/// <summary>
/// Устаревшая общая блокировка атак (используется только старыми атаками BossShooting, BossStompAttack и т.д.).
/// Новый каменный голем (GolemBoss) сам гарантирует, что одновременно идёт только одна атака.
/// </summary>
public static class BossAttackLock
{
    public static bool IsBusy { get; private set; }

    private static bool laserLock;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        IsBusy = false;
        laserLock = false;
    }

    public static bool TryStart()
    {
        if (IsBusy || laserLock) return false;
        IsBusy = true;
        return true;
    }

    public static bool TryStartLaser()
    {
        if (IsBusy) return false;
        IsBusy = true;
        laserLock = true;
        return true;
    }

    public static void End()
    {
        if (laserLock) return;
        IsBusy = false;
    }

    public static void EndLaser()
    {
        laserLock = false;
        IsBusy = false;
    }
}
