
using UnityEngine;

public class BossContactDamage : MonoBehaviour
{
    public int damage = 10;
    public float attackCooldown = 1f;

    private float nextAttackTime = 0f;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < nextAttackTime)
            return;

        Health target = other.GetComponent<Health>();

        if (target != null)
        {
            target.TakeDamage(damage);
            nextAttackTime = Time.time + attackCooldown;

            Debug.Log("Босс нанёс игроку урон!");
        }
    }
}
