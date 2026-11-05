using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Объекты")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Настройки стрельбы")]
    [Tooltip("Базовый интервал между выстрелами при скорости атаки 1.")]
    public float fireRate = 0.3f;
    public float firePointDistance = 1.2f;
    [Tooltip("Угол между снарядами при Multicast.")]
    public float multicastSpreadAngle = 5f;

    private float nextFireTime;
    private Camera mainCamera;
    private Vector2 aimDirection = Vector2.right;
    private CharacterStats characterStats;
    private CharacterAbilityController abilityController;

    void Start()
    {
        mainCamera = Camera.main;
        characterStats = GetComponent<CharacterStats>();
        abilityController = GetComponent<CharacterAbilityController>();
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;   // меню выбора персонажа / пауза

        AimAtMouse();

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            Shoot();
            float speedMultiplier = characterStats != null ? characterStats.AttackSpeed : 1f;
            nextFireTime = Time.time + fireRate / Mathf.Max(0.1f, speedMultiplier);
        }
    }

    public void SetCharacterStats(CharacterStats stats)
    {
        characterStats = stats;
        abilityController = GetComponent<CharacterAbilityController>();
    }

    void AimAtMouse()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null || firePoint == null) return;

        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = -mainCamera.transform.position.z;
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(mousePosition);

        Vector2 dir = (Vector2)mouseWorld - (Vector2)transform.position;
        if (dir.sqrMagnitude > 0.0001f) aimDirection = dir.normalized;

        firePoint.position = (Vector2)transform.position + aimDirection * firePointDistance;
        firePoint.right = aimDirection;
    }

    void Shoot()
    {
        if (projectilePrefab == null || firePoint == null) return;
        if (abilityController == null) abilityController = GetComponent<CharacterAbilityController>();

        // Multicast (только Nonlyy): базовая атака повторяется с полным уроном каждого снаряда.
        int shotCount = abilityController != null ? abilityController.ConsumeMulticastCount() : 1;
        Collider2D[] playerColliders = GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < shotCount; i++)
        {
            float angle = shotCount <= 1 ? 0f : (i - (shotCount - 1) / 2f) * multicastSpreadAngle;
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, angle) * aimDirection;
            GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

            Collider2D[] bulletColliders = bullet.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D playerCollider in playerColliders)
            foreach (Collider2D bulletCollider in bulletColliders)
                Physics2D.IgnoreCollision(playerCollider, bulletCollider);

            Projectile projectile = bullet.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.SetOwner(transform);
                if (characterStats != null)
                {
                    projectile.damage = characterStats.AttackDamage;
                    projectile.speed = characterStats.ProjectileSpeed;
                }
                projectile.SetDirection(shotDirection);
                projectile.bypassTargetInvulnerability = shotCount > 1;
            }
        }
    }
}
