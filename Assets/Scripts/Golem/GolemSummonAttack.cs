using UnityEngine;
using System.Collections;

/// <summary>
/// «Пробуждение осколков» (вторая фаза). Из земли выходят каменные приспешники — обычные враги.
/// Они бегут к игроку и бьют при касании. Их можно убить, а Lili может приручить; Smirqc получает бонус
/// EchoSlam за каждого обычного врага в радиусе. Одновременно живёт не больше maxAlive приспешников.
/// </summary>
public class GolemSummonAttack : GolemAttack
{
    [Header("Summon Pebbles")]
    public int minionsPerCast = 2;
    public int maxAlive = 3;
    public int minionHealth = 25;
    public float minionSpeed = 2.8f;
    public float minionLifetime = 25f;

    private void Reset()
    {
        attackName = "Summon Pebbles"; damage = 8; radius = 1f; speed = 2.8f; windup = 1.0f; duration = 0.8f; cooldown = 18f;
        minPhase = 2; weight = 0.7f; allowedInCombo = false;
    }

    public override bool CanUse(GolemBoss boss)
    {
        return base.CanUse(boss) && GolemMinion.Count < maxAlive;
    }

    public override IEnumerator Execute(GolemBoss boss)
    {
        boss.FacePlayer();
        boss.Rig.SetPose(GolemPose.SlamWindup);
        float wu = Wind(boss);

        float bx = boss.transform.position.x;
        float dir = Mathf.Sign(boss.PlayerPos.x - bx); if (dir == 0f) dir = 1f;
        int n = Mathf.Min(minionsPerCast, Mathf.Max(0, maxAlive - GolemMinion.Count));
        float[] xs = new float[n];
        for (int i = 0; i < n; i++)
        {
            xs[i] = Mathf.Clamp(bx + dir * (2.6f + i * 1.4f), boss.arenaMinX + 0.8f, boss.arenaMaxX - 0.8f);
            CombatFx.TelegraphCircle(new Vector2(xs[i], boss.groundY + 0.15f), 0.7f, wu / Mathf.Max(0.2f, boss.SpeedMultiplier), new Color(0.9f, 0.7f, 0.2f, 0.55f));
        }

        yield return boss.Wait(wu);

        boss.Rig.SetPose(GolemPose.Slam);
        CombatFx.Shake(0.15f, 0.2f);
        for (int i = 0; i < n; i++)
            GolemMinion.Spawn(xs[i], boss.groundY, minionHealth, Dmg(boss), minionSpeed, minionLifetime);

        yield return boss.Wait(duration);
    }
}
