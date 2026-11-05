
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class BossDashAttack : MonoBehaviour
{
    [Header("Объекты")]
    public Transform player;
    public BossMovement bossMovement;

    [Header("Рывок")]
    public float dashSpeed = 14f;
    public float dashDuration = 0.5f;
    public float warningTime = 0.8f;
    public float attackCooldown = 10f;

    [Header("Границы арены")]
    public float minX = -6f;
    public float maxX = 6f;
    public float minY = -1f;
    public float maxY = 2f;

    [Header("Урон")]
    public int damage = 20;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private BossContactDamage contactDamage;
    private Color originalColor;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        contactDamage = GetComponent<BossContactDamage>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        StartCoroutine(AttackLoop());
    }

    IEnumerator AttackLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackCooldown);

            if (player == null)
                continue;

            while (!BossAttackLock.TryStart())
                yield return new WaitForSeconds(0.2f);

            yield return StartCoroutine(DashAttack());

            BossAttackLock.End();
        }
    }

    IEnumerator DashAttack()
    {
        if (bossMovement != null)
            bossMovement.enabled = false;

        if (contactDamage != null)
            contactDamage.enabled = false;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(warningTime);

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        Vector2 direction =
            ((Vector2)player.position - rb.position).normalized;

        float timer = 0f;
        bool hasHitPlayer = false;

        while (timer < dashDuration)
        {
            Vector2 nextPosition = rb.position +
                direction * dashSpeed * Time.fixedDeltaTime;

            nextPosition.x = Mathf.Clamp(nextPosition.x, minX, maxX);
            nextPosition.y = Mathf.Clamp(nextPosition.y, minY, maxY);

            rb.MovePosition(nextPosition);

            if (!hasHitPlayer)
            {
                Collider2D[] hits = Physics2D.OverlapBoxAll(
                    rb.position,
                    new Vector2(2f, 3f),
                    0f
                );

                foreach (Collider2D hit in hits)
                {
                    if (!hit.transform.root.CompareTag("Player"))
                        continue;

                    Health target = hit.GetComponentInParent<Health>();

                    if (target != null)
                    {
                        target.TakeDamage(damage);
                        hasHitPlayer = true;

                        Debug.Log("Босс попал рывком!");
                        break;
                    }
                }
            }

            timer += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        yield return new WaitForSeconds(1f);

        if (contactDamage != null)
            contactDamage.enabled = true;

        if (bossMovement != null)
            bossMovement.enabled = true;
    }
}
