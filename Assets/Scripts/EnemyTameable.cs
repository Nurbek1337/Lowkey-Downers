using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Обычный враг, которого можно приручить (Lili). Босс этот компонент НЕ имеет и приручению не подлежит.
/// Приручённый враг идёт к боссу по земле и наносит ему урон, не трогая игрока и других союзников.
/// </summary>
[DisallowMultipleComponent]
public class EnemyTameable : MonoBehaviour
{
    public int contactDamage = 8;
    public float attackRange = 1.6f;
    public float attackInterval = 1f;
    public float moveSpeed = 3.2f;

    [Header("Мир")]
    [Tooltip("Y, на котором стоит приручённое существо (низ спрайта).")]
    public float groundY = -3.68f;
    public Color tamedColor = new Color(0.55f, 1f, 0.8f, 1f);

    private static readonly List<EnemyTameable> all = new List<EnemyTameable>();
    public static IReadOnlyList<EnemyTameable> All => all;

    private Transform bossTarget;
    private float expiresAt;
    private float nextAttack;
    private bool isTamed;
    private SpriteRenderer sr;

    public bool IsTamed => isTamed;
    public float TimeLeft => isTamed ? Mathf.Max(0f, expiresAt - Time.time) : 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { all.Clear(); }

    private void OnEnable() { if (!all.Contains(this)) all.Add(this); }
    private void OnDisable() { all.Remove(this); }

    public static bool AnyTamed()
    {
        for (int i = all.Count - 1; i >= 0; i--)
        {
            if (all[i] == null) { all.RemoveAt(i); continue; }
            if (all[i].isTamed) return true;
        }
        return false;
    }

    public static EnemyTameable FindNearest(Vector2 from, float range)
    {
        EnemyTameable best = null;
        float bestDist = range;
        for (int i = all.Count - 1; i >= 0; i--)
        {
            EnemyTameable e = all[i];
            if (e == null) { all.RemoveAt(i); continue; }
            if (e.isTamed) continue;
            Health h = e.GetComponent<Health>();
            if (h != null && h.IsDead) continue;
            float d = Vector2.Distance(from, e.transform.position);
            if (d < bestDist) { bestDist = d; best = e; }
        }
        return best;
    }

    public bool TryTame(Transform boss, float duration)
    {
        if (boss == null || isTamed) return false;
        bossTarget = boss;
        expiresAt = Time.time + duration;
        isTamed = true;
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = tamedColor;
        CombatFx.Flash(transform.position + Vector3.up * 0.5f, new Color(0.5f, 1f, 0.8f, 0.9f), 2f, 0.4f);
        return true;
    }

    private void Update()
    {
        if (!isTamed) return;

        Health own = GetComponent<Health>();
        if (Time.time >= expiresAt || bossTarget == null || (own != null && own.IsDead))
        {
            CombatFx.Debris(transform.position, 6, 4f, new Color(0.6f, 0.9f, 0.8f, 1f));
            Destroy(gameObject);
            return;
        }

        float dx = bossTarget.position.x - transform.position.x;
        if (sr != null) sr.flipX = dx < 0f;

        if (Mathf.Abs(dx) > attackRange)
        {
            float step = Mathf.Sign(dx) * moveSpeed * Time.deltaTime;
            transform.position = new Vector3(transform.position.x + step, transform.position.y, transform.position.z);
        }
        else if (Time.time >= nextAttack)
        {
            Health bossHealth = bossTarget.GetComponentInParent<Health>();
            if (bossHealth != null) bossHealth.TakeDamage(contactDamage, transform);
            nextAttack = Time.time + attackInterval;
            CombatFx.Debris(bossTarget.position + Vector3.up * 1.2f, 2, 3f, new Color(0.6f, 0.9f, 0.8f, 1f), 0.4f);
        }
    }
}
