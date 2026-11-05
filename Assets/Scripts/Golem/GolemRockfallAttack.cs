using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// «Каменный обвал». Сверху падают валуны. Метки на земле показывают места падения: первый залп целится
/// в игрока, остальные накрывают арену. Действие игрока: постоянно двигаться и не стоять на месте
/// (платформы от камней не защищают).
/// </summary>
public class GolemRockfallAttack : GolemAttack
{
    [Header("Rockfall")]
    public int rocksPhase1 = 5;
    public int rocksPhase2 = 9;
    public int volleys = 3;
    public float volleyDelay = 0.55f;
    public float spawnHeight = 8.5f;
    public float minRockSpacing = 1.8f;

    private void Reset()
    {
        attackName = "Rockfall"; damage = 15; radius = 1.0f; speed = 14f; windup = 1.1f; duration = 0.8f; cooldown = 8f; weight = 1f;
    }

    public override IEnumerator Execute(GolemBoss boss)
    {
        boss.Rig.SetPose(GolemPose.SlamWindup);
        yield return boss.Wait(0.35f);

        int total = boss.Phase >= 2 ? rocksPhase2 : rocksPhase1;
        int perVolley = Mathf.Max(1, Mathf.CeilToInt((float)total / Mathf.Max(1, volleys)));
        float fall = Spd(boss);
        float fallTime = (spawnHeight - boss.groundY) / Mathf.Max(1f, fall);

        for (int v = 0; v < volleys; v++)
        {
            List<float> xs = ChooseTargets(boss, perVolley, v == 0);
            float wu = (v == 0 ? Wind(boss) : Wind(boss) * 0.7f);

            foreach (float x in xs)
                CombatFx.TelegraphCircle(new Vector2(x, boss.groundY + 0.15f), radius * 1.1f, wu / Mathf.Max(0.2f, boss.SpeedMultiplier), WarnColor);

            yield return boss.Wait(wu);

            foreach (float x in xs)
                GolemFallingRock.Spawn(x, boss.groundY, spawnHeight, fall, Dmg(boss), radius, boss.transform);

            if (v < volleys - 1) yield return boss.Wait(volleyDelay);
        }

        yield return boss.Wait(fallTime + duration);
    }

    private List<float> ChooseTargets(GolemBoss boss, int count, bool aimAtPlayer)
    {
        List<float> xs = new List<float>();
        if (aimAtPlayer) xs.Add(Mathf.Clamp(boss.PlayerPos.x + Random.Range(-0.6f, 0.6f), boss.arenaMinX + 0.8f, boss.arenaMaxX - 0.8f));

        int guard = 0;
        while (xs.Count < count && guard++ < 60)
        {
            float x = Random.Range(boss.arenaMinX + 0.8f, boss.arenaMaxX - 0.8f);
            bool ok = true;
            foreach (float e in xs) if (Mathf.Abs(e - x) < minRockSpacing) { ok = false; break; }
            if (ok) xs.Add(x);
        }
        return xs;
    }
}
