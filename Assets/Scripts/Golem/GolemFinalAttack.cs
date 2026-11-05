using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ФИНАЛЬНАЯ АТАКА «Пробуждение горы» (запускается при 10 HP, ~13 секунд, босс неуязвим).
/// Этапы:
///  1. Пробуждение — босс уходит в центр, трясётся арена.
///  2. Тектонические волны — волны от стен через всю арену (прыгай).
///  3. Обвал — залпы камней с безопасным коридором рядом с игроком (беги в просвет).
///  4. Последний удар — босс поднимает руки; весь пол опасен, кроме двух «карманов» (подсвечены голубым).
///     Удар не блокируется Phase и не отражается, но уклониться можно: встать в карман или быть в воздухе.
/// После атаки босс истощён (уязвим, получает больше урона) и снова может быть убит.
/// Эта атака не участвует в случайном выборе — её запускает GolemBoss.
/// </summary>
public class GolemFinalAttack : GolemAttack
{
    [Header("1. Пробуждение")]
    public float awakenTime = 1.8f;

    [Header("2. Тектонические волны")]
    public int waveRounds = 3;
    public float waveWarn = 0.9f;
    public float waveRoundGap = 0.5f;
    public int waveDamage = 14;
    public float waveSpeed = 8f;
    public Vector2 waveZone = new Vector2(1.1f, 1.7f);

    [Header("3. Обвал")]
    public int rockSalvos = 3;
    public int rocksPerSalvo = 6;
    public float rockWarn = 0.9f;
    public int rockDamage = 14;
    public float rockFallSpeed = 14f;
    public float rockRadius = 1.0f;
    public float safeLaneWidth = 2.6f;
    public float rockSpacing = 1.6f;

    [Header("4. Последний удар")]
    public float finalSlamWarn = 2.2f;
    public int finalSlamDamage = 30;
    public float safePocketWidth = 2.8f;
    public float slamZoneHeight = 1.7f;

    private void Reset()
    {
        attackName = "Awakening of the Mountain"; damage = 30; windup = 2.2f; duration = 13f; cooldown = 999f;
        weight = 0f; allowedInCombo = false;
    }

    // Не участвует в случайном выборе.
    public override bool CanUse(GolemBoss boss) { return false; }

    public override IEnumerator Execute(GolemBoss boss)
    {
        // ---------- 1. Пробуждение ----------
        boss.FacePlayer();
        Coroutine shake = boss.StartCoroutine(ShakeLoop());
        boss.Rig.SetPose(GolemPose.FinalCharge);
        Vector3 start = boss.transform.position;
        float t = 0f;
        while (t < awakenTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / (awakenTime * 0.5f));
            boss.Place(new Vector3(Mathf.Lerp(start.x, 0f, k), boss.groundY, start.z));
            if (Random.value < 0.35f) CombatFx.Ring(boss.transform.position + Vector3.up * 3.2f, new Color(1f, 0.9f, 0.6f, 0.8f), 5f, 0.8f, 0.4f);
            if (Random.value < 0.5f) CombatFx.Debris(new Vector3(Random.Range(boss.arenaMinX, boss.arenaMaxX), boss.groundY + 7f, 0f), 1, 1f, StoneTint, 1.2f);
            yield return null;
        }

        // ---------- 2. Тектонические волны ----------
        float y = boss.groundY + 0.2f;
        float width = boss.arenaMaxX - boss.arenaMinX;
        for (int r = 0; r < waveRounds; r++)
        {
            bool fromLeft = (r % 2 == 0);
            bool both = (r == waveRounds - 1);
            CombatFx.Telegraph(new Vector2((boss.arenaMinX + boss.arenaMaxX) * 0.5f, y), new Vector2(width, 0.4f), waveWarn, WarnColor);
            yield return new WaitForSeconds(waveWarn);

            if (fromLeft || both)
                GolemWave.Spawn(new Vector2(boss.arenaMinX, boss.groundY), 1f, waveDamage, waveSpeed, waveZone, boss.transform, boss.arenaMinX - 2f, boss.arenaMaxX + 2f);
            if (!fromLeft || both)
                GolemWave.Spawn(new Vector2(boss.arenaMaxX, boss.groundY), -1f, waveDamage, waveSpeed, waveZone, boss.transform, boss.arenaMinX - 2f, boss.arenaMaxX + 2f);
            CombatFx.Shake(0.25f, 0.3f);
            yield return new WaitForSeconds(width / waveSpeed * 0.55f + waveRoundGap);
        }

        // ---------- 3. Обвал ----------
        for (int s = 0; s < rockSalvos; s++)
        {
            float warn = Mathf.Max(0.55f, rockWarn - s * 0.12f);
            float laneCenter = Mathf.Clamp(boss.PlayerPos.x + Random.Range(-1.5f, 1.5f), boss.arenaMinX + safeLaneWidth * 0.5f, boss.arenaMaxX - safeLaneWidth * 0.5f);
            List<float> xs = new List<float>();
            for (float x = boss.arenaMinX + 0.8f; x <= boss.arenaMaxX - 0.8f && xs.Count < rocksPerSalvo + 4; x += rockSpacing)
            {
                if (Mathf.Abs(x - laneCenter) < safeLaneWidth * 0.5f + rockRadius * 0.6f) continue;   // безопасный коридор
                xs.Add(x + Random.Range(-0.25f, 0.25f));
            }
            // безопасная зона отмечена голубым
            CombatFx.Telegraph(new Vector2(laneCenter, boss.groundY + 0.15f), new Vector2(safeLaneWidth, 0.3f), warn, new Color(0.4f, 0.9f, 1f, 0.55f), -3);
            foreach (float x in xs)
                CombatFx.TelegraphCircle(new Vector2(x, boss.groundY + 0.15f), rockRadius * 1.1f, warn, WarnColor);

            yield return new WaitForSeconds(warn);
            foreach (float x in xs)
                GolemFallingRock.Spawn(x, boss.groundY, boss.groundY + 12f, rockFallSpeed, rockDamage, rockRadius, boss.transform);
            yield return new WaitForSeconds(0.65f);
        }
        yield return new WaitForSeconds(0.9f);

        // ---------- 4. Последний удар ----------
        boss.Rig.SetPose(GolemPose.FinalCharge);
        float p1 = Random.Range(boss.arenaMinX + 1.8f, -0.8f);
        float p2 = Random.Range(0.8f, boss.arenaMaxX - 1.8f);
        if (Random.value < 0.5f) { float tmp = p1; p1 = p2; p2 = tmp; }
        float[] pockets = { p1, p2 };

        // Красный пол + голубые карманы
        CombatFx.Telegraph(new Vector2((boss.arenaMinX + boss.arenaMaxX) * 0.5f, boss.groundY + 0.35f), new Vector2(width, 0.7f), finalSlamWarn, new Color(1f, 0.15f, 0.05f, 0.65f), -4);
        foreach (float p in pockets)
        {
            CombatFx.Telegraph(new Vector2(p, boss.groundY + 0.4f), new Vector2(safePocketWidth, 0.8f), finalSlamWarn, new Color(0.35f, 0.95f, 1f, 0.9f), -3);
            CombatFx.Flash(new Vector3(p, boss.groundY + 0.5f, 0f), new Color(0.4f, 0.95f, 1f, 0.7f), safePocketWidth, finalSlamWarn);
        }
        yield return new WaitForSeconds(finalSlamWarn);

        boss.Rig.SetPose(GolemPose.Slam);
        ApplyFinalSlam(boss, pockets);
        CombatFx.Shake(0.55f, 0.6f);
        CombatFx.Flash(boss.transform.position + Vector3.up * 1f, new Color(1f, 0.85f, 0.5f, 1f), 14f, 0.4f);
        CombatFx.Ring(new Vector3(0f, boss.groundY + 0.3f, 0f), new Color(1f, 0.6f, 0.2f, 0.95f), 1f, width * 1.1f, 0.6f);
        CombatFx.Dust(new Vector3(0f, boss.groundY + 0.1f, 0f), 18, width * 0.5f, 1.2f);
        CombatFx.Debris(new Vector3(0f, boss.groundY + 0.4f, 0f), 30, 10f, StoneTint, 1.2f);
        yield return new WaitForSeconds(0.9f);

        boss.StopCoroutine(shake);
    }

    private void ApplyFinalSlam(GolemBoss boss, float[] pockets)
    {
        // Проверяем каждого игрока отдельно: в кармане или в воздухе — безопасно.
        Vector2 center = new Vector2((boss.arenaMinX + boss.arenaMaxX) * 0.5f, boss.groundY + slamZoneHeight * 0.5f);
        Vector2 size = new Vector2(boss.arenaMaxX - boss.arenaMinX + 2f, slamZoneHeight);
        Collider2D[] cols = Physics2D.OverlapBoxAll(center, size, 0f);
        HashSet<Health> handled = new HashSet<Health>();
        foreach (Collider2D c in cols)
        {
            Health h = BossDamage.PlayerHealthOf(c);
            if (h == null || h.IsDead || !handled.Add(h)) continue;
            float px = h.transform.position.x;
            bool inPocket = false;
            foreach (float p in pockets) if (Mathf.Abs(px - p) <= safePocketWidth * 0.5f) inPocket = true;
            if (inPocket) continue;
            // Удар можно парировать (Ney), но нельзя заблокировать Phase и нельзя отразить.
            h.TakeBossDamage(finalSlamDamage, boss.transform, true, false, false);
        }
    }

    private IEnumerator ShakeLoop()
    {
        while (true)
        {
            CombatFx.Shake(0.09f, 0.3f);
            yield return new WaitForSeconds(0.25f);
        }
    }
}
