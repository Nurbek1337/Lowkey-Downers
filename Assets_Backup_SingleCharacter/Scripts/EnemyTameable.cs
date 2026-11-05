using UnityEngine;

[DisallowMultipleComponent]
public class EnemyTameable : MonoBehaviour
{
    public int contactDamage = 8;
    public float attackRange = 2f;
    public float attackInterval = 1f;
    public float moveSpeed = 3f;

    private Transform bossTarget;
    private float expiresAt;
    private float nextAttack;
    private bool isTamed;

    public bool IsTamed => isTamed;

    public bool TryTame(Transform boss, float duration)
    {
        if (boss == null || isTamed) return false;
        bossTarget = boss;
        expiresAt = Time.time + duration;
        isTamed = true;
        return true;
    }

    private void Update()
    {
        if (!isTamed) return;
        if (Time.time >= expiresAt || bossTarget == null)
        {
            Destroy(gameObject);
            return;
        }

        float distance = Vector2.Distance(transform.position, bossTarget.position);
        if (distance > attackRange)
            transform.position = Vector2.MoveTowards(transform.position, bossTarget.position, moveSpeed * Time.deltaTime);
        else if (Time.time >= nextAttack)
        {
            Health bossHealth = bossTarget.GetComponentInParent<Health>();
            if (bossHealth != null) bossHealth.TakeDamage(contactDamage);
            nextAttack = Time.time + attackInterval;
        }
    }
}
