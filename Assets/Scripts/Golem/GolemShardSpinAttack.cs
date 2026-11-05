using UnityEngine;
using System.Collections;

/// <summary>
/// «Вращение с каменными осколками» (вторая фаза). Босс раскручивается, наращивая свечение, и выпускает
/// кольцами осколки, поворачивая каждое кольцо на небольшой угол — получаются «спирали» с просветами.
/// Действие игрока: найти просвет между осколками (по высоте и горизонтали), парировать осколок (Ney).
/// </summary>
public class GolemShardSpinAttack : GolemAttack
{
    [Header("Shard Spin")]
    public int volleys = 8;
    public int shardsPerVolley = 10;
    public float volleyInterval = 0.3f;
    public float rotationPerVolley = 17f;
    public float chestHeight = 3.3f;

    private void Reset()
    {
        attackName = "Shard Spin"; damage = 10; radius = 1f; speed = 6.5f; windup = 1.2f; duration = 0.9f; cooldown = 11f;
        minPhase = 2; weight = 1f; allowedInCombo = false;
    }

    public override IEnumerator Execute(GolemBoss boss)
    {
        boss.Rig.SetPose(GolemPose.SpinWindup);
        float wu = Wind(boss);
        Vector3 origin = boss.transform.position + Vector3.up * chestHeight;

        // Телеграф: пульсирующие кольца вокруг груди — осколки готовятся.
        float t = 0f; float nextRing = 0f;
        while (t < wu)
        {
            t += boss.Dt;
            if (Time.time >= nextRing)
            {
                CombatFx.Ring(origin, new Color(1f, 0.4f, 0.15f, 0.8f), 3f, 0.8f, 0.35f);
                nextRing = Time.time + 0.3f;
            }
            yield return null;
        }

        boss.Rig.SetPose(GolemPose.Spin);
        float offset = Random.Range(0f, 360f);
        float step = 360f / Mathf.Max(3, shardsPerVolley);
        float spd = Spd(boss);

        for (int v = 0; v < volleys; v++)
        {
            origin = boss.transform.position + Vector3.up * chestHeight;
            for (int i = 0; i < shardsPerVolley; i++)
            {
                float ang = (offset + v * rotationPerVolley + i * step) * Mathf.Deg2Rad;
                Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
                GolemShard.Spawn(origin, vel, Dmg(boss), boss.transform, boss.groundY, boss.arenaMinX, boss.arenaMaxX);
            }
            CombatFx.Flash(origin, new Color(1f, 0.6f, 0.25f, 0.8f), 2.5f, 0.15f);
            CombatFx.Shake(0.05f, 0.1f);
            yield return boss.Wait(volleyInterval);
        }

        boss.Rig.SetPose(GolemPose.Idle);
        yield return boss.Wait(duration);
    }
}
