
using UnityEngine;
using System.Collections;

public class BossStompAttack : MonoBehaviour
{
    [Header("Объекты")]
    public Transform player;
    public BossMovement bossMovement;

    [Header("Атака")]
    public float attackCooldown = 8f;
    public float warningTime = 0.8f;
    public float jumpHeight = 2f;
    public float jumpSpeed = 5f;
    public float fallSpeed = 12f;

    [Header("Границы арены")]
    public float minX = -6f;
    public float maxX = 6f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        StartCoroutine(AttackLoop());
    }

    IEnumerator AttackLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(
                Random.Range(attackCooldown * 0.8f, attackCooldown * 1.3f)
            );

            if (player == null)
                continue;

            while (!BossAttackLock.TryStart())
                yield return new WaitForSeconds(0.2f);

            yield return StartCoroutine(StompAttack());

            BossAttackLock.End();
        }
    }

    IEnumerator StompAttack()
    {
        if (bossMovement != null)
            bossMovement.enabled = false;

        float targetX = Mathf.Clamp(player.position.x, minX, maxX);

        Vector2 jumpTarget = new Vector2(
            targetX,
            transform.position.y + jumpHeight
        );

        if (spriteRenderer != null)
            spriteRenderer.color = Color.red;

        yield return StartCoroutine(MoveTo(jumpTarget, jumpSpeed));

        yield return new WaitForSeconds(warningTime);

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        Vector2 landingPosition = new Vector2(
            transform.position.x,
            -1f
        );

        yield return StartCoroutine(MoveTo(landingPosition, fallSpeed));

        // Босс приземлился
        Debug.Log("Босс выполнил Stomp!");

        yield return new WaitForSeconds(1f);

        if (bossMovement != null)
            bossMovement.enabled = true;
    }

    IEnumerator MoveTo(Vector2 target, float speed)
    {
        while (Vector2.Distance(rb.position, target) > 0.05f)
        {
            Vector2 nextPosition = Vector2.MoveTowards(
                rb.position,
                target,
                speed * Time.fixedDeltaTime
            );

            rb.MovePosition(nextPosition);

            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(target);
    }
}
