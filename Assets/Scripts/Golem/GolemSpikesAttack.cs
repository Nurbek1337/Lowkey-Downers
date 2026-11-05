using UnityEngine;
using System.Collections;

/// <summary>
/// «Каменные шипы». Босс бьёт ногой, и из земли цепочкой вырастают шипы, двигаясь в сторону игрока.
/// У каждого шипа свой телеграф (трещина + красная метка). Действие игрока: выбрать момент и перепрыгнуть
/// именно ту колонну, которая сейчас вспыхнет. Во второй фазе шипы идут в обе стороны.
/// </summary>
public class GolemSpikesAttack : GolemAttack
{
    [Header("Spike Eruption")]
    public int spikesPhase1 = 7;
    public int spikesPhase2 = 9;
    public float spacing = 1.25f;
    [Tooltip("Задержка между соседними шипами цепочки.")]
    public float stepDelay = 0.11f;
    [Tooltip("Сколько шип остаётся над землёй.")]
    public float spikeUpTime = 0.5f;
    public float startOffset = 1.8f;

    private void Reset()
    {
        attackName = "Spike Eruption"; damage = 16; radius = 1f; speed = 1f; windup = 0.7f; duration = 0.8f; cooldown = 7f;
        hitZone = new Vector2(0.9f, 2.2f); weight = 1f;
    }

    public override IEnumerator Execute(GolemBoss boss)
    {
        boss.FacePlayer();
        boss.Rig.SetPose(GolemPose.SlamWindup);
        yield return boss.Wait(0.5f);
        boss.Rig.SetPose(GolemPose.Slam);
        CombatFx.Shake(0.15f, 0.2f);
        CombatFx.Dust(boss.transform.position + Vector3.up * 0.1f, 5, 1.5f);

        int count = boss.Phase >= 2 ? spikesPhase2 : spikesPhase1;
        float dirToPlayer = Mathf.Sign(boss.PlayerPos.x - boss.transform.position.x);
        if (dirToPlayer == 0f) dirToPlayer = 1f;
        float warn = Wind(boss);
        float x0 = boss.transform.position.x;
        int dmg = Dmg(boss);

        for (int i = 0; i < count; i++)
        {
            float off = startOffset + i * spacing;
            TrySpawn(boss, x0 + dirToPlayer * off, warn, dmg);
            if (boss.Phase >= 2) TrySpawn(boss, x0 - dirToPlayer * off, warn, dmg);
            yield return boss.Wait(stepDelay);
        }

        yield return boss.Wait(warn + spikeUpTime + duration * 0.5f);
    }

    private void TrySpawn(GolemBoss boss, float x, float warn, int dmg)
    {
        if (x < boss.arenaMinX + 0.3f || x > boss.arenaMaxX - 0.3f) return;
        GolemSpike.Spawn(x, boss.groundY, warn, spikeUpTime, dmg, hitZone, boss.transform);
    }
}
