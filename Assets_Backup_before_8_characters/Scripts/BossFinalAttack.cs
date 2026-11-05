using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossFinalAttack : MonoBehaviour
{
    [Header("Объекты")]
    public Transform player;
    public Health bossHealth;
    public GameObject projectilePrefab;
    public BossMovement bossMovement;
    public BossShooting bossShooting;
    public BossStompAttack bossStomp;
    public BossDashAttack bossDash;
    public BossLaserAttack bossLaser;
    public BossCircleAttack bossCircle;

    [Header("Границы арены")]
    public float minX = -7f;
    public float maxX = 7f;
    public float minY = -2f;
    public float maxY = 2f;

    [Header("Финальная атака")]
    public int activationHealth = 10;
    public int meteorDamage = 15;
    public int cloneDamage = 20;
    public int fireDamage = 5;
    public int finalDamage = 100;

    [Header("Метеориты")]
    public int meteorCount = 15;
    public float meteorFallSpeed = 20f;

    [Header("Двойники")]
    public int cloneCount = 6;
    public float cloneSpeed = 14f;

    [Header("Пули")]
    public int bulletCount = 20;
    public float bulletSpeed = 8f;

    private bool attackStarted;
    private SpriteRenderer bossSprite;
    private Camera mainCamera;
    private Vector3 cameraStartPosition;

    private static Sprite circleSprite;

    void Start()
    {
        if (bossHealth == null)
            bossHealth = GetComponent<Health>();

        bossSprite = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;

        if (mainCamera != null)
            cameraStartPosition = mainCamera.transform.position;
    }

    void Update()
    {
        if (attackStarted || bossHealth == null)
            return;

        if (bossHealth.CurrentHealth <= activationHealth)
        {
            attackStarted = true;
            StartCoroutine(FinalAttack());
        }
    }

    IEnumerator FinalAttack()
    {
        while (!BossAttackLock.TryStart())
            yield return null;

        Debug.Log("АПОКАЛИПСИС НАЧАЛСЯ!");

        bossHealth.SetExternalInvulnerability(true);

        DisableOtherAttacks();

        transform.position = new Vector3(
            0f, 0f, transform.position.z
        );

        // 1. Пробуждение
        yield return StartCoroutine(Awakening());

        // 2. Метеоритный ад
        yield return StartCoroutine(MeteorHell());

        // 3. Двойники
        yield return StartCoroutine(CloneHell());

        // 4. Кровавый шторм
        yield return StartCoroutine(BulletHell());

        // 5. Последний взрыв
        yield return StartCoroutine(Apocalypse());

        bossHealth.SetExternalInvulnerability(false);

        if (bossSprite != null)
            bossSprite.color = Color.gray;

        Debug.Log("БОСС ОСЛАБЛЕН! ДОБЕЙ ЕГО!");

        BossAttackLock.End();
    }

    void DisableOtherAttacks()
    {
        if (bossMovement != null) bossMovement.enabled = false;
        if (bossShooting != null) bossShooting.enabled = false;
        if (bossStomp != null) bossStomp.enabled = false;
        if (bossDash != null) bossDash.enabled = false;
        if (bossLaser != null) bossLaser.enabled = false;
        if (bossCircle != null) bossCircle.enabled = false;

        BossContactDamage contact =
            GetComponent<BossContactDamage>();

        if (contact != null)
            contact.enabled = false;
    }

    IEnumerator Awakening()
    {
        Debug.Log("ПРОБУЖДЕНИЕ АПОКАЛИПСИСА!");

        if (bossSprite != null)
            bossSprite.color = Color.red;

        StartCoroutine(CameraShake(2f, 0.35f));

        // Быстрое увеличение босса
        Vector3 originalScale = transform.localScale;
        Vector3 bigScale = originalScale * 1.3f;

        float timer = 0f;

        while (timer < 1f)
        {
            transform.localScale =
                Vector3.Lerp(originalScale, bigScale, timer);

            timer += Time.deltaTime * 2f;
            yield return null;
        }

        transform.localScale = originalScale * 1.3f;

        yield return new WaitForSeconds(0.5f);
    }

    // МЕТЕОРИТНЫЙ АД

    IEnumerator MeteorHell()
    {
        Debug.Log("МЕТЕОРИТНЫЙ АД!");

        for (int wave = 0; wave < 3; wave++)
        {
            List<Vector2> targets = new List<Vector2>();

            for (int i = 0; i < 5; i++)
            {
                Vector2 target = new Vector2(
                    Random.Range(minX, maxX),
                    Random.Range(minY, maxY)
                );

                targets.Add(target);

                CreateCircle(
                    target,
                    new Vector3(1.7f, 0.4f, 1f),
                    new Color(1f, 0f, 0f, 0.55f),
                    "MeteorWarning"
                );
            }

            yield return new WaitForSeconds(0.35f);

            foreach (Vector2 target in targets)
            {
                StartCoroutine(DropMeteor(target));
            }

            StartCoroutine(CameraShake(1f, 0.2f));

            yield return new WaitForSeconds(1.1f);
        }

        yield return new WaitForSeconds(1f);
    }

    IEnumerator DropMeteor(Vector2 target)
    {
        GameObject meteor = CreateCircle(
            new Vector2(target.x, maxY + 5f),
            new Vector3(1.1f, 1.1f, 1f),
            new Color(1f, 0.2f, 0f, 1f),
            "Meteor"
        );

        while (meteor != null &&
               meteor.transform.position.y > target.y)
        {
            meteor.transform.position +=
                Vector3.down * meteorFallSpeed * Time.deltaTime;

            yield return null;
        }

        if (meteor != null)
            Destroy(meteor);

        DamageInRadius(target, 1.6f, meteorDamage);

        GameObject explosion = CreateCircle(
            target,
            new Vector3(2.5f, 2.5f, 1f),
            new Color(1f, 0.1f, 0f, 0.75f),
            "MeteorExplosion"
        );

        yield return new WaitForSeconds(0.25f);

        if (explosion != null)
            Destroy(explosion);

        GameObject fire = CreateCircle(
            target,
            new Vector3(1.5f, 0.5f, 1f),
            new Color(1f, 0f, 0f, 0.7f),
            "FireZone"
        );

        StartCoroutine(FireZoneDamage(target, fire));
    }

    IEnumerator FireZoneDamage(Vector2 position, GameObject fire)
    {
        for (int i = 0; i < 5; i++)
        {
            DamageInRadius(position, 1f, fireDamage);
            yield return new WaitForSeconds(0.4f);
        }

        if (fire != null)
            Destroy(fire);
    }

    // ДВОЙНИКИ

    IEnumerator CloneHell()
    {
        Debug.Log("АРМИЯ ДВОЙНИКОВ!");

        for (int wave = 0; wave < 2; wave++)
        {
            List<GameObject> clones = new List<GameObject>();

            for (int i = 0; i < cloneCount; i++)
            {
                float x = i % 2 == 0 ? minX : maxX;
                float y = Random.Range(minY, maxY);

                GameObject clone = CreateCircle(
                    new Vector2(x, y),
                    new Vector3(1.5f, 2f, 1f),
                    new Color(0.9f, 0f, 0.1f, 1f),
                    "BossClone"
                );

                if (bossSprite != null)
                {
                    SpriteRenderer sr =
                        clone.GetComponent<SpriteRenderer>();

                    sr.sprite = bossSprite.sprite;
                }

                clones.Add(clone);
            }

            yield return new WaitForSeconds(0.3f);

            foreach (GameObject clone in clones)
            {
                if (clone == null || player == null)
                    continue;

                Vector2 target = player.position;

                while (clone != null &&
                       Vector2.Distance(
                           clone.transform.position,
                           target
                       ) > 0.3f)
                {
                    clone.transform.position =
                        Vector2.MoveTowards(
                            clone.transform.position,
                            target,
                            cloneSpeed * Time.deltaTime
                        );

                    yield return null;
                }

                if (clone != null)
                {
                    Vector2 explosionPosition =
                        clone.transform.position;

                    DamageInRadius(
                        explosionPosition,
                        1.8f,
                        cloneDamage
                    );

                    GameObject explosion = CreateCircle(
                        explosionPosition,
                        new Vector3(2f, 2f, 1f),
                        Color.red,
                        "CloneExplosion"
                    );

                    Destroy(explosion, 0.3f);
                    Destroy(clone);
                }
            }

            yield return new WaitForSeconds(0.3f);
        }
    }

    // КРОВАВЫЙ ШТОРМ

    IEnumerator BulletHell()
    {
        Debug.Log("КРОВАВЫЙ ШТОРМ!");

        if (projectilePrefab == null)
            yield break;

        for (int i = 0; i < 4; i++)
        {
            ShootBulletRing(i * 12f);

            yield return new WaitForSeconds(0.45f);

            ShootTargetedBurst(5);

            yield return new WaitForSeconds(0.35f);
        }

        yield return new WaitForSeconds(0.3f);
    }

    void ShootBulletRing(float offset)
    {
        for (int i = 0; i < bulletCount; i++)
        {
            float angle =
                i * (360f / bulletCount) + offset;

            Vector2 direction = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            SpawnBullet(direction);
        }
    }

    void ShootTargetedBurst(int count)
    {
        if (player == null)
            return;

        Vector2 direction =
            ((Vector2)player.position -
             (Vector2)transform.position).normalized;

        float baseAngle =
            Mathf.Atan2(direction.y, direction.x) *
            Mathf.Rad2Deg;

        for (int i = 0; i < count; i++)
        {
            float angle = baseAngle + (i - count / 2) * 12f;

            Vector2 shotDirection = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            SpawnBullet(shotDirection);
        }
    }

    void SpawnBullet(Vector2 direction)
    {
        GameObject bullet = Instantiate(
            projectilePrefab,
            transform.position,
            Quaternion.identity
        );

        BossProjectile projectile =
            bullet.GetComponent<BossProjectile>();

        if (projectile != null)
            projectile.SetDirection(direction);

        // Ускоряем снаряд, если он использует Rigidbody2D
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();

        if (rb != null)
            rb.linearVelocity = direction.normalized * bulletSpeed;
    }

    // АПОКАЛИПСИС

    IEnumerator Apocalypse()
    {
        Debug.Log("АБСОЛЮТНОЕ УНИЧТОЖЕНИЕ!");

        if (bossSprite != null)
            bossSprite.color = Color.white;

        GameObject orb = CreateCircle(
            transform.position,
            Vector3.one,
            Color.red,
            "ApocalypseOrb"
        );

        float timer = 0f;

        while (timer < 2f)
        {
            if (orb != null)
            {
                float scale = Mathf.Lerp(
                    1f,
                    7f,
                    timer / 2f
                );

                orb.transform.localScale =
                    new Vector3(scale, scale, 1f);
            }

            if (mainCamera != null)
                ShakeCamera(0.25f);

            timer += Time.deltaTime;
            yield return null;
        }

        if (orb != null)
            Destroy(orb);

        // Показываем опасную зону
        GameObject warning = CreateCircle(
            Vector2.zero,
            new Vector3(30f, 30f, 1f),
            new Color(1f, 0f, 0f, 0.35f),
            "ApocalypseWarning"
        );

        Vector2 safePosition = new Vector2(5.5f, -1.5f);

        GameObject safeZone = CreateCircle(
            safePosition,
            new Vector3(2.5f, 1.8f, 1f),
            new Color(0f, 1f, 0f, 0.7f),
            "SafeZone"
        );

        Debug.Log("БЕГИ В БЕЗОПАСНУЮ ЗОНУ!");

        yield return new WaitForSeconds(1.2f);

        // Три волны взрыва
        for (int i = 0; i < 3; i++)
        {
            GameObject explosion = CreateCircle(
                Vector2.zero,
                new Vector3(30f, 30f, 1f),
                new Color(1f, 0f, 0f, 0.8f),
                "ApocalypseExplosion"
            );

            if (player != null)
            {
                Vector2 playerPosition = player.position;

                if (Vector2.Distance(
                    playerPosition,
                    safePosition
                ) > 1.5f)
                {
                    DamageInRadius(
                        playerPosition,
                        100f,
                        finalDamage
                    );
                }
            }

            StartCoroutine(CameraShake(0.5f, 0.6f));

            yield return new WaitForSeconds(0.25f);

            if (explosion != null)
                Destroy(explosion);

            yield return new WaitForSeconds(0.2f);
        }

        if (warning != null) Destroy(warning);
        if (safeZone != null) Destroy(safeZone);

        yield return new WaitForSeconds(1f);
    }

    void DamageInRadius(Vector2 position, float radius, int damage)
    {
        if (player == null)
            return;

        if (Vector2.Distance(player.position, position) <= radius)
        {
            Health playerHealth =
                player.GetComponentInParent<Health>();

            if (playerHealth != null)
                playerHealth.TakeDamage(damage);
        }
    }

    GameObject CreateCircle(
        Vector2 position,
        Vector3 scale,
        Color color,
        string objectName)
    {
        GameObject obj = new GameObject(objectName);

        obj.transform.position = position;
        obj.transform.localScale = scale;

        SpriteRenderer renderer =
            obj.AddComponent<SpriteRenderer>();

        renderer.sprite = GetCircleSprite();
        renderer.color = color;
        renderer.sortingOrder = 10;

        return obj;
    }

    Sprite GetCircleSprite()
    {
        if (circleSprite != null)
            return circleSprite;

        Texture2D texture = new Texture2D(64, 64);
        texture.filterMode = FilterMode.Bilinear;

        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                float distance = Vector2.Distance(
                    new Vector2(x, y),
                    new Vector2(31.5f, 31.5f)
                );

                texture.SetPixel(
                    x,
                    y,
                    distance <= 31f ? Color.white : Color.clear
                );
            }
        }

        texture.Apply();

        circleSprite = Sprite.Create(
            texture,
            new Rect(0, 0, 64, 64),
            new Vector2(0.5f, 0.5f),
            64
        );

        return circleSprite;
    }

    IEnumerator CameraShake(float duration, float strength)
    {
        if (mainCamera == null)
            yield break;

        float timer = 0f;

        while (timer < duration)
        {
            ShakeCamera(strength);

            timer += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = cameraStartPosition;
    }

    void ShakeCamera(float strength)
    {
        if (mainCamera == null)
            return;

        Vector2 offset =
            Random.insideUnitCircle * strength;

        mainCamera.transform.position =
            cameraStartPosition +
            new Vector3(offset.x, offset.y, 0f);
    }
}