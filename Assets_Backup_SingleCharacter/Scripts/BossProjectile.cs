using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    public float speed = 7f;
    public int damage = 10;

    private Rigidbody2D rb;
    private Vector2 direction;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
        rb.linearVelocity = direction * speed;
    }

    void Start()
    {
        Destroy(gameObject, 5f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Health target = other.GetComponent<Health>();

        if (target != null)
        {
            target.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}