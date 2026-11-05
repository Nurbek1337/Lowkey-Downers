
using UnityEngine;

public class BossSecondPhase : MonoBehaviour
{
    public Health bossHealth;
    public BossMovement bossMovement;
    public BossShooting bossShooting;
    public BossStompAttack bossStomp;
    public BossDashAttack bossDash;

    private SpriteRenderer[] sprites;
    private bool phaseStarted;

    void Start()
    {
        sprites = GetComponentsInChildren<SpriteRenderer>(true);

        if (bossHealth == null)
            bossHealth = GetComponent<Health>();
    }

    void Update()
    {
        if (phaseStarted || bossHealth == null)
            return;

        if (bossHealth.CurrentHealth <= bossHealth.maxHealth / 2)
        {
            StartSecondPhase();
        }
    }

    void StartSecondPhase()
    {
        BossAttackLock.End();

        phaseStarted = true;

        foreach (SpriteRenderer sprite in sprites)
        {
            if (sprite != null)
                sprite.color = Color.red;
        }

        if (bossMovement != null)
            bossMovement.moveSpeed *= 1.5f;

        if (bossShooting != null)
            bossShooting.attackCooldown *= 0.5f;

        if (bossStomp != null)
            bossStomp.attackCooldown *= 0.5f;

        if (bossDash != null)
            bossDash.attackCooldown *= 0.5f;

        Debug.Log("ВТОРАЯ ФАЗА НАЧАЛАСЬ!");
    }
}
