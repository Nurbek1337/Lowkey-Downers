using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Объекты")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Настройки стрельбы")]
    public float fireRate = 0.3f;
    public float firePointDistance = 1.2f;

    private float nextFireTime;
    private Camera mainCamera;
    private Vector2 aimDirection;
    private CharacterStats characterStats;
    private CharacterAbilityController abilityController;
    private float baseFireRate;

    void Start()
    {
        mainCamera = Camera.main;
        characterStats = GetComponent<CharacterStats>();
        abilityController = GetComponent<CharacterAbilityController>();
        baseFireRate = fireRate;
    }

    void Update()
    {
        AimAtMouse();

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            Shoot();
            float speedMultiplier = characterStats != null ? characterStats.AttackSpeed : 1f;
            nextFireTime = Time.time + baseFireRate / Mathf.Max(0.1f, speedMultiplier);
        }
    }

    public void SetCharacterStats(CharacterStats stats)
    {
        characterStats = stats;
        abilityController = GetComponent<CharacterAbilityController>();
        if (baseFireRate <= 0f) baseFireRate = fireRate;
    }

    void AimAtMouse()
    {
        if (mainCamera == null || firePoint == null)
            return;

        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = -mainCamera.transform.position.z;

        Vector3 mouseWorldPosition =
            mainCamera.ScreenToWorldPoint(mousePosition);

        aimDirection =
            ((Vector2)mouseWorldPosition -
             (Vector2)transform.position).normalized;

        // Перемещаем FirePoint вокруг игрока
        firePoint.position =
            (Vector2)transform.position +
            aimDirection * firePointDistance;

        // Поворачиваем FirePoint в сторону курсора
        firePoint.right = aimDirection;
    }

    void Shoot()
    {
        if (projectilePrefab == null || firePoint == null)
            return;
        if (abilityController == null)
            abilityController = GetComponent<CharacterAbilityController>();

        int shotCount = abilityController != null ? abilityController.ConsumeMulticastCount() : 1;
        for (int i = 0; i < shotCount; i++)
        {
            float angle = shotCount <= 1 ? 0f : (i - (shotCount - 1) / 2f) * 5f;
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, angle) * aimDirection;
            GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

            Collider2D[] playerColliders = GetComponentsInChildren<Collider2D>();
            Collider2D[] bulletColliders = bullet.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D playerCollider in playerColliders)
            foreach (Collider2D bulletCollider in bulletColliders)
                Physics2D.IgnoreCollision(playerCollider, bulletCollider);

            Projectile projectile = bullet.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.SetDirection(shotDirection);
                projectile.SetOwner(transform);
                if (characterStats != null) projectile.damage = characterStats.AttackDamage;
                projectile.bypassTargetInvulnerability = shotCount > 1;
            }
        }
    }
}