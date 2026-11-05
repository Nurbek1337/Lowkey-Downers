using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class BossLaserAttack : MonoBehaviour
{
    [Header("Объекты")]
    public Transform player;
    public BossMovement bossMovement;

    [Header("Позиция босса")]
    public Vector2 arenaCenter = new Vector2(0f, 0f);
    public float moveSpeed = 5f;

    [Header("Лазеры")]
    public float laserLength = 15f;
    public float laserWidth = 0.15f;

    [Header("Скорость вращения")]
    public float startRotationSpeed = 20f;
    public float maxRotationSpeed = 90f;
    public float accelerationTime = 6f;
    public float slowdownTime = 3f;

    [Header("Время атаки")]
    public float attackCooldown = 12f;
    public float warningTime = 1.5f;
    public float damageCooldown = 0.5f;

    [Header("Урон")]
    public int laserDamage = 10;

    private Rigidbody2D rb;
    private LineRenderer[] lasers = new LineRenderer[4];
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private Health playerHealth;
    private float nextDamageTime;
    private float currentAngle;

    private RigidbodyConstraints2D originalConstraints;
    private bool originalMovementState;
    private bool laserLockOwned;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalConstraints = rb.constraints;
        CreateLasers();
    }

    void Start()
    {
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (player != null)
            playerHealth = player.GetComponent<Health>();

        StartCoroutine(AttackLoop());
    }

    void CreateLasers()
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject laserObject = new GameObject("BossLaser_" + i);
            laserObject.transform.SetParent(transform);

            LineRenderer line = laserObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = laserWidth;
            line.endWidth = laserWidth;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = Color.red;
            line.endColor = Color.red;
            line.sortingOrder = 10;
            line.enabled = false;

            lasers[i] = line;
        }
    }

    IEnumerator AttackLoop()
    {
        while (true)
        {
            yield return BossStunSystem.Wait(attackCooldown);

            if (player == null)
                continue;

            while (!BossAttackLock.TryStartLaser())
                yield return BossStunSystem.Wait(0.2f);

            laserLockOwned = true;

            yield return StartCoroutine(LaserAttack());

            BossAttackLock.EndLaser();
            laserLockOwned = false;
        }
    }

    IEnumerator LaserAttack()
    {
        if (bossMovement != null)
        {
            originalMovementState = bossMovement.enabled;
            bossMovement.enabled = false;
        }

        rb.constraints = originalConstraints;

        while (Vector2.Distance(rb.position, arenaCenter) > 0.1f)
        {
            if (BossStunSystem.IsStunned)
            {
                yield return null;
                continue;
            }

            Vector2 nextPosition = Vector2.MoveTowards(
                rb.position,
                arenaCenter,
                moveSpeed * Time.fixedDeltaTime
            );

            rb.MovePosition(nextPosition);
            yield return new WaitForFixedUpdate();
        }

        rb.position = arenaCenter;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.red;

        yield return BossStunSystem.Wait(warningTime);

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        foreach (LineRenderer laser in lasers)
            laser.enabled = true;

        currentAngle = 0f;
        nextDamageTime = 0f;

        float timer = 0f;

        while (timer < accelerationTime)
        {
            if (BossStunSystem.IsStunned)
            {
                yield return null;
                continue;
            }

            float progress = timer / accelerationTime;
            float speed = Mathf.Lerp(
                startRotationSpeed,
                maxRotationSpeed,
                progress
            );

            RotateLasers(speed);
            timer += Time.deltaTime;
            yield return null;
        }

        timer = 0f;

        while (timer < slowdownTime)
        {
            if (BossStunSystem.IsStunned)
            {
                yield return null;
                continue;
            }

            float progress = timer / slowdownTime;
            float speed = Mathf.Lerp(
                maxRotationSpeed,
                0f,
                progress
            );

            RotateLasers(speed);
            timer += Time.deltaTime;
            yield return null;
        }

        foreach (LineRenderer laser in lasers)
            laser.enabled = false;

        yield return BossStunSystem.Wait(1f);

        rb.constraints = originalConstraints;

        if (bossMovement != null)
            bossMovement.enabled = originalMovementState;

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        Debug.Log("ЛАЗЕР ЗАВЕРШЁН.");
    }

    void RotateLasers(float speed)
    {
        float rotation = speed * Time.deltaTime;
        currentAngle += rotation;

        Vector2 origin = rb.position;

        for (int i = 0; i < 4; i++)
        {
            float angle = currentAngle + i * 90f;

            Vector2 direction = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            Vector2 end = origin + direction * laserLength;

            lasers[i].SetPosition(0, origin);
            lasers[i].SetPosition(1, end);

            CheckLaserHit(origin, direction);
        }
    }

    void CheckLaserHit(Vector2 origin, Vector2 direction)
    {
        if (BossStunSystem.IsStunned || player == null || playerHealth == null)
            return;

        RaycastHit2D[] hits = Physics2D.RaycastAll(
            origin,
            direction,
            laserLength
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider.transform.root != player.root)
                continue;

            if (Time.time >= nextDamageTime)
            {
                playerHealth.TakeDamage(laserDamage);
                nextDamageTime = Time.time + damageCooldown;
            }

            break;
        }
    }

    void OnDisable()
    {
        foreach (LineRenderer laser in lasers)
        {
            if (laser != null)
                laser.enabled = false;
        }

        if (rb != null)
            rb.constraints = originalConstraints;

        if (bossMovement != null)
            bossMovement.enabled = originalMovementState;

        if (laserLockOwned)
        {
            BossAttackLock.EndLaser();
            laserLockOwned = false;
        }
    }
}
