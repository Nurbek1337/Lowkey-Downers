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

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        AimAtMouse();

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
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

        GameObject bullet = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.identity
        );

        // Игнорируем столкновения со всеми коллайдерами игрока
        Collider2D[] playerColliders =
            GetComponentsInChildren<Collider2D>();

        Collider2D[] bulletColliders =
            bullet.GetComponentsInChildren<Collider2D>();

        foreach (Collider2D playerCollider in playerColliders)
        {
            foreach (Collider2D bulletCollider in bulletColliders)
            {
                Physics2D.IgnoreCollision(
                    playerCollider,
                    bulletCollider
                );
            }
        }

        Projectile projectile =
            bullet.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.SetDirection(aimDirection);
        }
    }
}