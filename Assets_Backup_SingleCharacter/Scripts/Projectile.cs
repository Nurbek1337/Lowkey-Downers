
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 12f;
    public int damage = 10;
    public bool bypassTargetInvulnerability;

    private Rigidbody2D rb;
    private Vector2 direction = Vector2.right;
    private Transform owner;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        rb.linearVelocity = direction * speed;
        Destroy(gameObject, 5f);
    }

    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
    }

    public void SetOwner(Transform newOwner)
    {
        owner = newOwner;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Health target = other.GetComponentInParent<Health>();

        if (target != null)
        {
            Transform attacker = owner != null ? owner : transform.root;
            if (bypassTargetInvulnerability) target.TakeDamageRepeated(damage, attacker);
            else target.TakeDamage(damage, attacker);
        }

        Destroy(gameObject);
    }
}
