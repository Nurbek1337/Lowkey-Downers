
using UnityEngine;
using System.Collections;

public class BossShooting : MonoBehaviour
{
    [Header("Объекты")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public Transform player;

    [Header("Перезарядка")]
    public float attackCooldown = 5f;

    void Start()
    {
        StartCoroutine(AttackLoop());
    }

    IEnumerator AttackLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackCooldown);

            if (player == null || firePoint == null)
                continue;

            while (!BossAttackLock.TryStart())
                yield return new WaitForSeconds(0.2f);

            int attack = Random.Range(0, 2);

            if (attack == 0)
            {
                yield return StartCoroutine(TargetedAttack());
            }
            else
            {
                FanAttack();
            }

            BossAttackLock.End();
        }
    }

    IEnumerator TargetedAttack()
    {
        for (int i = 0; i < 3; i++)
        {
            Vector2 direction =
                (player.position - firePoint.position).normalized;

            ShootProjectile(direction);

            yield return new WaitForSeconds(0.3f);
        }
    }

    void FanAttack()
    {
        Vector2 direction =
            (player.position - firePoint.position).normalized;

        float[] angles = { -40f, -20f, 0f, 20f, 40f };

        foreach (float angle in angles)
        {
            Vector2 shotDirection =
                Quaternion.Euler(0, 0, angle) * direction;

            ShootProjectile(shotDirection);
        }
    }

    void ShootProjectile(Vector2 direction)
    {
        GameObject bullet = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.identity
        );

        Collider2D[] bossColliders =
            GetComponentsInChildren<Collider2D>();

        Collider2D[] bulletColliders =
            bullet.GetComponentsInChildren<Collider2D>();

        foreach (Collider2D bossCollider in bossColliders)
        {
            foreach (Collider2D bulletCollider in bulletColliders)
            {
                Physics2D.IgnoreCollision(
                    bossCollider,
                    bulletCollider
                );
            }
        }

        BossProjectile projectile =
            bullet.GetComponent<BossProjectile>();

        projectile.SetDirection(direction);
    }
}
