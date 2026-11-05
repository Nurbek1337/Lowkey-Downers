using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterAIController : MonoBehaviour
{
    public float followDistance = 2.5f;
    public float moveSpeed = 3f;
    public float attackRange = 7f;
    public float attackInterval = 0.8f;
    public int attackDamage = 7;

    private Rigidbody2D rb;
    private CharacterPartyManager party;
    private float nextAttack;
    private float nextSkill;
    private Health bossHealth;
    private CharacterStats stats;
    private float nextAutoParryTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<CharacterStats>();
        enabled = false;
    }

    private void Start()
    {
        party = FindAnyObjectByType<CharacterPartyManager>();
        Transform boss = party != null ? party.GetBossTarget() : null;
        if (boss != null) bossHealth = boss.GetComponent<Health>();
    }

    private void Update()
    {
        Health ownHealth = GetComponent<Health>();
        if (ownHealth != null && ownHealth.CurrentHealth <= 0) return;
        if (party == null || party.ActiveCharacter == null) return;
        Transform leader = party.ActiveCharacter.transform;
        float distanceToLeader = Vector2.Distance(transform.position, leader.position);
        if (distanceToLeader > followDistance)
        {
            Vector2 next = Vector2.MoveTowards(rb.position, leader.position, moveSpeed * Time.deltaTime);
            rb.MovePosition(next);
        }

        if (bossHealth == null)
        {
            Transform boss = party.GetBossTarget();
            if (boss != null) bossHealth = boss.GetComponent<Health>();
        }

        if (stats != null && stats.character == CharacterId.Ney && bossHealth != null &&
            Time.time >= nextAutoParryTime &&
            Vector2.Distance(transform.position, bossHealth.transform.position) <= 3.5f)
        {
            ParrySystem parry = GetComponent<ParrySystem>();
            if (parry != null) parry.StartParryForAI();
            nextAutoParryTime = Time.time + 1.2f;
        }

        if (bossHealth != null && Time.time >= nextAttack &&
            Vector2.Distance(transform.position, bossHealth.transform.position) <= attackRange)
        {
            int damage = stats != null ? stats.AttackDamage : attackDamage;
            CharacterAbilityController ability = GetComponent<CharacterAbilityController>();
            int repeats = ability != null ? ability.ConsumeMulticastCount() : 1;
            for (int i = 0; i < repeats; i++)
            {
                if (repeats > 1) bossHealth.TakeDamageRepeated(damage, transform);
                else bossHealth.TakeDamage(damage, transform);
            }
            float speedMultiplier = stats != null ? stats.AttackSpeed : 1f;
            nextAttack = Time.time + attackInterval / Mathf.Max(0.1f, speedMultiplier);
        }

        if (Time.time >= nextSkill)
        {
            CharacterAbilityController ability = GetComponent<CharacterAbilityController>();
            if (ability != null) ability.TryUseAbility(true);
            nextSkill = Time.time + 5f;
        }
    }
}
