using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// «Каменные стены» (вторая фаза). Босс бьёт по земле, и рядом с игроком вырастают временные каменные колонны.
/// Колонны сплошные: останавливают игрока, его снаряды и волны/осколки босса (служат укрытием от них),
/// но ломаются от попаданий, от прыжка босса или проходящего сквозь них босса и исчезают по таймеру.
/// Не создаются вплотную к игроку и стенам — игрока запереть нельзя.
/// </summary>
public class GolemBarricadeAttack : GolemAttack
{
    [Header("Stone Barricade")]
    public int pillarCount = 2;
    [Tooltip("Сколько секунд живёт колонна.")]
    public float pillarLifetime = 9f;
    public int pillarHits = 3;
    public float minDistanceFromPlayer = 2.6f;
    public float minDistanceBetween = 3.2f;
    public float distanceFromPlayerMin = 3.2f;
    public float distanceFromPlayerMax = 5.2f;

    private void Reset()
    {
        attackName = "Stone Barricade"; damage = 0; radius = 0.5f; speed = 1f; windup = 0.8f; duration = 0.9f; cooldown = 16f;
        minPhase = 2; weight = 0.8f; allowedInCombo = true;
    }

    public override bool CanUse(GolemBoss boss)
    {
        return base.CanUse(boss) && GolemPillar.Count == 0;
    }

    public override IEnumerator Execute(GolemBoss boss)
    {
        boss.FacePlayer();
        boss.Rig.SetPose(GolemPose.SlamWindup);

        List<float> xs = ChooseSpots(boss);
        float wu = Wind(boss);
        foreach (float x in xs)
            CombatFx.TelegraphCircle(new Vector2(x, boss.groundY + 0.15f), 0.9f, wu / Mathf.Max(0.2f, boss.SpeedMultiplier), new Color(0.6f, 0.8f, 1f, 0.55f));

        yield return boss.Wait(wu);

        boss.Rig.SetPose(GolemPose.Slam);
        foreach (float x in xs)
        {
            GolemPillar.Spawn(x, boss.groundY, pillarLifetime, pillarHits);
            CombatFx.Debris(new Vector3(x, boss.groundY + 0.3f, 0f), 6, 6f, StoneTint);
            CombatFx.Dust(new Vector3(x, boss.groundY + 0.1f, 0f), 3, 0.7f);
        }
        CombatFx.Shake(0.2f, 0.25f);
        yield return boss.Wait(duration);
    }

    private List<float> ChooseSpots(GolemBoss boss)
    {
        List<float> xs = new List<float>();
        float px = boss.PlayerPos.x;
        float bx = boss.transform.position.x;
        int guard = 0;
        while (xs.Count < pillarCount && guard++ < 80)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            float x = px + side * Random.Range(distanceFromPlayerMin, distanceFromPlayerMax);
            if (x < boss.arenaMinX + 1.2f || x > boss.arenaMaxX - 1.2f) continue;      // не вплотную к стенам
            if (Mathf.Abs(x - px) < minDistanceFromPlayer) continue;                   // не на игроке
            if (Mathf.Abs(x - bx) < 2.2f) continue;                                    // не внутри босса
            bool ok = true;
            foreach (float e in xs) if (Mathf.Abs(e - x) < minDistanceBetween) { ok = false; break; }
            if (ok) xs.Add(x);
        }
        return xs;
    }
}
