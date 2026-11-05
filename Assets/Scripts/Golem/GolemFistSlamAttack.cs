using UnityEngine;
using System.Collections;

/// <summary>
/// «Удар кулаками по земле». Босс поднимает руки, затем бьёт в землю — в обе стороны расходятся каменные волны.
/// Действие игрока: прыжок над волной (или парирование Ney). Во второй фазе — серия из нескольких ударов.
/// Телеграф: красные полосы вдоль пола по пути волн + поднятые руки.
/// </summary>
public class GolemFistSlamAttack : GolemAttack
{
    [Header("Fist Slam")]
    public int slamsPhase1 = 1;
    public int slamsPhase2 = 2;
    [Tooltip("Пауза между ударами серии.")]
    public float slamGap = 0.9f;
    [Tooltip("Смещение кулаков от центра босса.")]
    public float fistOffset = 1.9f;

    private void Reset()
    {
        attackName = "Fist Slam"; damage = 16; radius = 2f; speed = 7f; windup = 1.1f; duration = 0.9f; cooldown = 5f;
        hitZone = new Vector2(1.1f, 1.7f); weight = 1.2f;
    }

    public override IEnumerator Execute(GolemBoss boss)
    {
        int count = boss.Phase >= 2 ? slamsPhase2 : slamsPhase1;
        for (int i = 0; i < count; i++)
        {
            boss.FacePlayer();
            float wu = i == 0 ? Wind(boss) : Wind(boss) * 0.65f;
            boss.Rig.SetPose(GolemPose.SlamWindup);

            Vector3 pos = boss.transform.position;
            float y = boss.groundY + 0.2f;
            float leftLen = pos.x - boss.arenaMinX, rightLen = boss.arenaMaxX - pos.x;
            CombatFx.Telegraph(new Vector2(pos.x - leftLen * 0.5f - 0.5f, y), new Vector2(Mathf.Max(0.1f, leftLen - 1f), 0.4f), wu / Mathf.Max(0.2f, boss.SpeedMultiplier), WarnColor);
            CombatFx.Telegraph(new Vector2(pos.x + rightLen * 0.5f + 0.5f, y), new Vector2(Mathf.Max(0.1f, rightLen - 1f), 0.4f), wu / Mathf.Max(0.2f, boss.SpeedMultiplier), WarnColor);

            yield return boss.Wait(wu);

            boss.Rig.SetPose(GolemPose.Slam);
            pos = boss.transform.position;
            float spd = Spd(boss);
            GolemWave.Spawn(new Vector2(pos.x - fistOffset, boss.groundY), -1f, Dmg(boss), spd, hitZone, boss.transform, boss.arenaMinX, boss.arenaMaxX);
            GolemWave.Spawn(new Vector2(pos.x + fistOffset, boss.groundY), 1f, Dmg(boss), spd, hitZone, boss.transform, boss.arenaMinX, boss.arenaMaxX);

            CombatFx.Shake(0.28f, 0.3f);
            CombatFx.Dust(new Vector3(pos.x, boss.groundY + 0.1f, 0f), 7, 2f, 0.9f);
            CombatFx.Debris(new Vector3(pos.x, boss.groundY + 0.3f, 0f), 8, 7f, StoneTint);
            CombatFx.Ring(new Vector3(pos.x, boss.groundY + 0.3f, 0f), new Color(1f, 0.6f, 0.25f, 0.9f), 0.5f, 5f, 0.4f);

            yield return boss.Wait(i < count - 1 ? slamGap : duration);
        }
    }
}
