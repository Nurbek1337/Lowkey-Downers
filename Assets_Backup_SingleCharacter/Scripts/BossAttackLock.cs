public static class BossAttackLock
{
    public static bool IsBusy { get; private set; }

    private static bool laserLock;

    // Обычные атаки
    public static bool TryStart()
    {
        if (IsBusy || laserLock)
            return false;

        IsBusy = true;
        return true;
    }

    // Лазер получает отдельную блокировку
    public static bool TryStartLaser()
    {
        if (IsBusy)
            return false;

        IsBusy = true;
        laserLock = true;

        return true;
    }

    // Обычная атака не может снять блокировку лазера
    public static void End()
    {
        if (laserLock)
            return;

        IsBusy = false;
    }

    // Только лазер может снять свою блокировку
    public static void EndLaser()
    {
        laserLock = false;
        IsBusy = false;
    }
}