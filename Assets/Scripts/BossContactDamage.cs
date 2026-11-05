using UnityEngine;

/// <summary>Урон при касании тела босса. Бьёт только игровых персонажей (не босса, не приручённых существ).</summary>
public class BossContactDamage : MonoBehaviour
{
    public int damage = 10;
    public float attackCooldown = 1f;

    private float nextAttackTime = 0f;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (BossStunSystem.IsStunned) return;
        if (Time.time < nextAttackTime) return;

        if (other.GetComponentInParent<CharacterStats>() == null) return;
        Health target = other.GetComponentInParent<Health>();
        if (target == null || target.IsDead) return;

        target.TakeBossDamage(damage, transform);
        nextAttackTime = Time.time + attackCooldown;
    }
}
