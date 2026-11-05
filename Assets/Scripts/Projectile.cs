using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 12f;
    public int damage = 10;
    public bool bypassTargetInvulnerability;

    private Rigidbody2D rb;
    private Vector2 direction = Vector2.right;
    private Transform owner;
    private bool consumed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        if (rb != null) rb.linearVelocity = direction * speed;
        Destroy(gameObject, 5f);
    }

    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
        if (rb != null) rb.linearVelocity = direction * speed;
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
        if (rb != null) rb.linearVelocity = direction * speed;
    }

    public void SetOwner(Transform newOwner)
    {
        owner = newOwner;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;

        // Снаряд игрока не бьёт самого игрока и приручённых союзников.
        if (other.GetComponentInParent<CharacterStats>() != null) return;
        if (owner != null && other.transform.IsChildOf(owner)) return;
        if (other.GetComponentInParent<Projectile>() != null) return;
        EnemyTameable ally = other.GetComponentInParent<EnemyTameable>();
        if (ally != null && ally.IsTamed) return;

        Health target = other.GetComponentInParent<Health>();
        if (target != null)
        {
            Transform attacker = owner != null ? owner : transform;
            if (bypassTargetInvulnerability) target.TakeDamageRepeated(damage, attacker);
            else target.TakeDamage(damage, attacker);
            CombatFx.Debris(transform.position, 2, 3f, new Color(0.7f, 0.62f, 0.55f, 1f), 0.5f);
        }

        consumed = true;
        Destroy(gameObject);
    }
}
