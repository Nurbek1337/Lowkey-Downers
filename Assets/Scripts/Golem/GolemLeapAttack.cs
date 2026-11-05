using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// «Прыжок и сокрушающее приземление». Босс приседает, метка на земле следует за игроком и фиксируется
/// перед прыжком. Приземление: удар по зоне + две низкие ударные волны. Действие игрока: уйти из метки.
/// Во второй фазе после приземления идёт короткий второй прыжок.
/// </summary>
public class GolemLeapAttack : GolemAttack
{
    [Header("Leap Slam")]
    public float jumpHeight = 5.5f;
    [Tooltip("Время в воздухе.")]
    public float airTime = 0.85f;
    [Tooltip("За сколько секунд до прыжка метка перестаёт следовать за игроком.")]
    public float lockTime = 0.45f;
    [Range(0f, 1f)] public float waveDamageFraction = 0.6f;
    public float landingZoneHeight = 2.6f;
    public float pillarBreakRadius = 2.4f;

    private void Reset()
    {
        attackName = "Leap Slam"; damage = 28; radius = 2.6f; speed = 8f; windup = 1.2f; duration = 1.0f; cooldown = 9f;
        weight = 0.9f; minPlayerDistance = 2.5f; hitZone = new Vector2(1.0f, 1.3f);
    }

    public override IEnumerator Execute(GolemBoss boss)
    {
        int hops = boss.Phase >= 2 ? 2 : 1;
        for (int h = 0; h < hops; h++)
        {
            float wu = h == 0 ? Wind(boss) : Wind(boss) * 0.55f;
            boss.FacePlayer();
            boss.Rig.SetPose(GolemPose.Crouch);

            float targetX = Mathf.Clamp(boss.PlayerPos.x, boss.arenaMinX + 1.2f, boss.arenaMaxX - 1.2f);
            Vector3 markerPos = new Vector3(targetX, boss.groundY + 0.15f, 0f);
            SpriteRenderer marker = CombatFx.TelegraphCircle(markerPos, radius, 999f, WarnColor, -3);

            float t = 0f;
            while (t < wu)
            {
                float dt = boss.Dt;
                t += dt;
                if (t < wu - lockTime)
                {
                    targetX = Mathf.MoveTowards(targetX, Mathf.Clamp(boss.PlayerPos.x, boss.arenaMinX + 1.2f, boss.arenaMaxX - 1.2f), 9f * dt);
                    if (marker != null) marker.transform.position = new Vector3(targetX, boss.groundY + 0.15f, 0f);
                }
                else if (marker != null)
                {
                    float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 30f);   // метка зафиксирована — мигает
                    marker.color = new Color(1f, 0.15f, 0.05f, 0.85f * pulse);
                }
                yield return null;
            }
            if (marker != null) Destroy(marker.gameObject);

            // Полёт по параболе
            boss.Rig.SetPose(GolemPose.Airborne);
            Vector3 start = boss.transform.position;
            float u = 0f;
            float air = airTime / (boss.Phase >= 2 ? phase2SpeedMultiplier : 1f);
            while (u < 1f)
            {
                u += boss.Dt / Mathf.Max(0.05f, air);
                float k = Mathf.Clamp01(u);
                float x = Mathf.Lerp(start.x, targetX, k);
                float y = boss.groundY + 4f * jumpHeight * k * (1f - k);
                boss.transform.position = new Vector3(x, y, start.z);
                boss.FacePlayer();
                yield return null;
            }

            // Приземление
            boss.transform.position = new Vector3(targetX, boss.groundY, start.z);
            boss.Rig.SetPose(GolemPose.Land);
            LandingImpact(boss, targetX);
            yield return boss.Wait(h < hops - 1 ? 0.4f : duration);
        }
    }

    private void LandingImpact(GolemBoss boss, float x)
    {
        int dmg = Dmg(boss);
        HashSet<Health> already = new HashSet<Health>();
        BossDamage.HitBox(new Vector2(x, boss.groundY + landingZoneHeight * 0.5f), new Vector2(radius * 2f, landingZoneHeight), dmg, boss.transform, already);

        float spd = Spd(boss);
        int waveDmg = Mathf.Max(1, Mathf.RoundToInt(dmg * waveDamageFraction));
        GolemWave.Spawn(new Vector2(x - 1.2f, boss.groundY), -1f, waveDmg, spd, hitZone, boss.transform, boss.arenaMinX, boss.arenaMaxX);
        GolemWave.Spawn(new Vector2(x + 1.2f, boss.groundY), 1f, waveDmg, spd, hitZone, boss.transform, boss.arenaMinX, boss.arenaMaxX);

        // Колонны рядом с точкой приземления разрушаются.
        List<GolemPillar> toBreak = new List<GolemPillar>();
        foreach (GolemPillar p in GolemPillar.All)
            if (p != null && Mathf.Abs(p.transform.position.x - x) < pillarBreakRadius) toBreak.Add(p);
        foreach (GolemPillar p in toBreak) p.Shatter();

        Vector3 pos = new Vector3(x, boss.groundY + 0.2f, 0f);
        CombatFx.Shake(0.4f, 0.4f);
        CombatFx.Dust(pos, 10, 2.4f, 1f);
        CombatFx.Debris(pos, 14, 8f, StoneTint);
        CombatFx.Ring(pos, new Color(1f, 0.55f, 0.2f, 0.95f), 0.5f, radius * 2.2f, 0.45f);
        CombatFx.Flash(pos + Vector3.up * 0.4f, new Color(1f, 0.8f, 0.4f, 0.8f), 3.5f, 0.25f);
    }
}
