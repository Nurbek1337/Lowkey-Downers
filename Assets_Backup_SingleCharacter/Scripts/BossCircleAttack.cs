
using UnityEngine;
using System.Collections;

public class BossCircleAttack : MonoBehaviour
{
    [Header("Объекты")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public Health bossHealth;

    [Header("Настройки атаки")]
    public int bulletCount = 8;
    public float attackCooldown = 10f;
    public float secondRingDelay = 1.2f;

    [Header("Предупреждение")]
    public float warningTime = 1.5f;
    public Color warningColor = Color.yellow;

    private SpriteRenderer[] sprites;

    void Start()
    {
        if (bossHealth == null)
            bossHealth = GetComponent<Health>();

        sprites = GetComponentsInChildren<SpriteRenderer>(true);

        StartCoroutine(AttackLoop());
    }

    IEnumerator AttackLoop()
    {
        // Ждём вторую фазу
        while (bossHealth == null ||
               bossHealth.CurrentHealth > bossHealth.maxHealth / 2)
        {
            yield return new WaitForSeconds(0.5f);
        }

        while (true)
        {
            yield return new WaitForSeconds(attackCooldown);

            if (projectilePrefab == null || firePoint == null)
                continue;

            while (!BossAttackLock.TryStart())
                yield return new WaitForSeconds(0.2f);

            // Предупреждение
            Color[] originalColors = new Color[sprites.Length];

            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                {
                    originalColors[i] = sprites[i].color;
                    sprites[i].color = warningColor;
                }
            }

            yield return new WaitForSeconds(warningTime);

            // Возвращаем цвет
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                    sprites[i].color = originalColors[i];
            }

            // Первый круг
            ShootCircle(0f);

            yield return new WaitForSeconds(secondRingDelay);

            // Второй круг
            ShootCircle(180f / bulletCount);

            BossAttackLock.End();
        }
    }

    void ShootCircle(float offset)
    {
        float angleStep = 360f / bulletCount;

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = i * angleStep + offset;

            Vector2 direction = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

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

            if (projectile != null)
                projectile.SetDirection(direction);
        }
    }
}
