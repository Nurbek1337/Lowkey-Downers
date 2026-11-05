using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ======================================================================
// Наземная волна (Fist Slam, Leap Slam, финальная атака)
// ======================================================================
public class GolemWave : MonoBehaviour
{
    public Transform source;
    public int damage = 16;
    public float speed = 7f;
    public float direction = 1f;
    public Vector2 hitSize = new Vector2(1.1f, 1.7f);
    public float minX = -8f, maxX = 8f;
    public bool parryable = true;

    private readonly HashSet<Health> hit = new HashSet<Health>();
    private float nextDust;

    public static GolemWave Spawn(Vector2 groundPos, float dir, int dmg, float spd, Vector2 size, Transform src,
        float minX, float maxX, bool parryable = true)
    {
        SpriteRenderer sr = CombatFx.MakeSprite("GolemWave", CombatFx.Art("fx_wave"), groundPos, Color.white, 12);
        sr.transform.localScale = new Vector3(dir >= 0f ? 1f : -1f, size.y / 1.7f, 1f);
        GolemWave w = sr.gameObject.AddComponent<GolemWave>();
        w.source = src; w.damage = dmg; w.speed = spd; w.direction = dir >= 0f ? 1f : -1f;
        w.hitSize = size; w.minX = minX; w.maxX = maxX; w.parryable = parryable;
        return w;
    }

    private void Update()
    {
        Vector3 p = transform.position;
        float nx = p.x + direction * speed * Time.deltaTime;

        // Каменные колонны-препятствия поглощают волну.
        foreach (GolemPillar pillar in GolemPillar.All)
        {
            if (pillar == null) continue;
            float px = pillar.transform.position.x;
            if ((direction > 0f && p.x < px && nx >= px - 0.4f) || (direction < 0f && p.x > px && nx <= px + 0.4f))
            {
                pillar.TakeHit();
                CombatFx.Debris(p, 4, 5f, new Color(0.7f, 0.6f, 0.5f, 1f));
                Destroy(gameObject);
                return;
            }
        }

        transform.position = new Vector3(nx, p.y, p.z);

        BossDamage.HitBox(new Vector2(nx, p.y + hitSize.y * 0.5f), hitSize, damage, source, hit, parryable);

        if (Time.time >= nextDust)
        {
            CombatFx.Dust(new Vector3(nx, p.y + 0.1f, 0f), 1, 0.3f, 0.5f);
            nextDust = Time.time + 0.08f;
        }

        if (nx < minX - 1f || nx > maxX + 1f) Destroy(gameObject);
    }
}

// ======================================================================
// Падающий камень (Rockfall, финальная атака)
// ======================================================================
public class GolemFallingRock : MonoBehaviour
{
    public Transform source;
    public int damage = 15;
    public float fallSpeed = 14f;
    public float impactRadius = 1.0f;
    public float groundY = -3.68f;

    private readonly HashSet<Health> hit = new HashSet<Health>();
    private bool landed;

    public static GolemFallingRock Spawn(float x, float groundY, float startY, float fallSpeed, int dmg, float impactRadius, Transform src)
    {
        SpriteRenderer sr = CombatFx.MakeSprite("GolemRock", CombatFx.Art("fx_rock"), new Vector3(x, startY, 0f), Color.white, 13);
        sr.transform.localScale = Vector3.one * Mathf.Max(0.8f, impactRadius * 0.9f);
        GolemFallingRock r = sr.gameObject.AddComponent<GolemFallingRock>();
        r.source = src; r.damage = dmg; r.fallSpeed = fallSpeed; r.impactRadius = impactRadius; r.groundY = groundY;
        return r;
    }

    private void Update()
    {
        if (landed) return;
        transform.Rotate(0f, 0f, 90f * Time.deltaTime);
        Vector3 p = transform.position;
        p.y -= fallSpeed * Time.deltaTime;

        // Камень бьёт всё на своём пути вниз (платформы от него не защищают).
        BossDamage.HitCircle(p, impactRadius * 0.6f, damage, source, hit);

        if (p.y <= groundY + 0.4f)
        {
            landed = true;
            p.y = groundY + 0.4f;
            transform.position = p;
            BossDamage.HitCircle(new Vector2(p.x, groundY + 0.3f), impactRadius, damage, source, hit);
            CombatFx.Debris(new Vector3(p.x, groundY + 0.3f, 0f), 7, 6f, new Color(0.7f, 0.6f, 0.5f, 1f));
            CombatFx.Dust(new Vector3(p.x, groundY + 0.1f, 0f), 4, 0.6f);
            CombatFx.Shake(0.1f, 0.12f);
            Destroy(gameObject, 0.01f);
            return;
        }
        transform.position = p;
    }
}

// ======================================================================
// Каменный шип из земли (Spike Eruption)
// ======================================================================
public class GolemSpike : MonoBehaviour
{
    private Transform source;
    private int damage;
    private Vector2 zone;
    private float warn;
    private float upTime;
    private float groundY;
    private SpriteRenderer sr;
    private readonly HashSet<Health> hit = new HashSet<Health>();

    public static GolemSpike Spawn(float x, float groundY, float warn, float upTime, int dmg, Vector2 zone, Transform src)
    {
        SpriteRenderer sr = CombatFx.MakeSprite("GolemSpike", CombatFx.Art("fx_spike"), new Vector3(x, groundY, 0f), Color.white, 11);
        sr.enabled = false;
        GolemSpike s = sr.gameObject.AddComponent<GolemSpike>();
        s.source = src; s.damage = dmg; s.zone = zone; s.warn = warn; s.upTime = upTime; s.groundY = groundY; s.sr = sr;
        s.StartCoroutine(s.Run());
        return s;
    }

    private IEnumerator Run()
    {
        Vector3 pos = transform.position;
        // Телеграф: трещина в земле + красное пятно.
        CombatFx.Telegraph(new Vector2(pos.x, groundY + 0.12f), new Vector2(zone.x, 0.28f), warn, new Color(1f, 0.3f, 0.1f, 0.65f), -3);
        float t = 0f;
        while (t < warn)
        {
            t += Time.deltaTime;
            if (Random.value < 0.15f) CombatFx.Dust(pos + Vector3.up * 0.1f, 1, 0.3f, 0.4f);
            yield return null;
        }

        sr.enabled = true;
        CombatFx.Debris(pos, 4, 5f, new Color(0.7f, 0.6f, 0.5f, 1f), 0.6f);
        CombatFx.Shake(0.06f, 0.1f);

        float e = 0f;
        while (e < upTime)
        {
            e += Time.deltaTime;
            float rise = Mathf.Clamp01(e / 0.12f);
            float fall = Mathf.Clamp01((upTime - e) / 0.15f);
            float k = Mathf.Min(rise, fall);
            transform.localScale = new Vector3(1f, Mathf.Max(0.05f, k), 1f);
            BossDamage.HitBox(new Vector2(pos.x, groundY + zone.y * 0.5f * k), new Vector2(zone.x, zone.y * k), damage, source, hit);
            yield return null;
        }
        Destroy(gameObject);
    }
}

// ======================================================================
// Осколок (Shard Spin)
// ======================================================================
public class GolemShard : MonoBehaviour
{
    public Transform source;
    public int damage = 10;
    public Vector2 velocity;
    public float groundY = -3.68f;
    public float minX = -9f, maxX = 9f;
    private readonly HashSet<Health> hit = new HashSet<Health>();

    public static GolemShard Spawn(Vector2 pos, Vector2 vel, int dmg, Transform src, float groundY, float minX, float maxX)
    {
        SpriteRenderer sr = CombatFx.MakeSprite("GolemShard", CombatFx.Art("fx_shard"), pos, Color.white, 14);
        GolemShard s = sr.gameObject.AddComponent<GolemShard>();
        s.source = src; s.damage = dmg; s.velocity = vel; s.groundY = groundY; s.minX = minX; s.maxX = maxX;
        float ang = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
        s.transform.rotation = Quaternion.Euler(0f, 0f, ang);
        return s;
    }

    private void Update()
    {
        Vector3 p = transform.position + (Vector3)(velocity * Time.deltaTime);
        transform.position = p;

        if (BossDamage.HitCircle(p, 0.35f, damage, source, hit) > 0 || hit.Count > 0)
        {
            Destroy(gameObject);
            return;
        }

        foreach (GolemPillar pillar in GolemPillar.All)
        {
            if (pillar == null) continue;
            Vector2 pp = pillar.transform.position;
            if (Mathf.Abs(p.x - pp.x) < 0.7f && p.y > groundY && p.y < groundY + 3f)
            {
                pillar.TakeHit();
                CombatFx.Debris(p, 3, 4f, new Color(0.7f, 0.6f, 0.5f, 1f), 0.4f);
                Destroy(gameObject);
                return;
            }
        }

        if (p.y <= groundY + 0.2f || p.x < minX - 1f || p.x > maxX + 1f || p.y > 9f)
        {
            if (p.y <= groundY + 0.2f) CombatFx.Debris(p, 2, 3f, new Color(0.7f, 0.6f, 0.5f, 1f), 0.4f);
            Destroy(gameObject);
        }
    }
}

// ======================================================================
// Каменная колонна — временное препятствие (Stone Barricade)
// ======================================================================
public class GolemPillar : MonoBehaviour
{
    private static readonly List<GolemPillar> all = new List<GolemPillar>();
    public static IReadOnlyList<GolemPillar> All => all;
    public static int Count { get { all.RemoveAll(p => p == null); return all.Count; } }

    private float lifetime;
    private int hitsLeft;
    private float bornAt;
    private SpriteRenderer sr;
    private float groundY;
    private bool dying;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { all.Clear(); }

    public static GolemPillar Spawn(float x, float groundY, float lifetime, int hits)
    {
        SpriteRenderer sr = CombatFx.MakeSprite("GolemPillar", CombatFx.Art("fx_pillar"), new Vector3(x, groundY, 0f), Color.white, 6);
        GolemPillar p = sr.gameObject.AddComponent<GolemPillar>();
        p.lifetime = lifetime; p.hitsLeft = hits; p.sr = sr; p.groundY = groundY; p.bornAt = Time.time;

        // Сплошной коллайдер: блокирует игрока, его снаряды и волны босса.
        BoxCollider2D col = sr.gameObject.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.0f, 2.8f);
        col.offset = new Vector2(0f, 1.4f);
        all.Add(p);
        return p;
    }

    private void Update()
    {
        if (dying) return;
        float age = Time.time - bornAt;
        float rise = Mathf.Clamp01(age / 0.25f);
        transform.localScale = new Vector3(1f, Mathf.Max(0.05f, rise), 1f);

        // Предупреждение о скором исчезновении — мигание.
        if (age > lifetime - 1.5f && sr != null)
            sr.color = Mathf.Sin(Time.time * 20f) > 0f ? Color.white : new Color(1f, 0.7f, 0.5f, 1f);

        if (age >= lifetime) { Shatter(); return; }

        // Босс, идущий сквозь колонну, разбивает её.
        Transform boss = BossLocator.Find();
        if (boss != null && Mathf.Abs(boss.position.x - transform.position.x) < 1.6f && boss.position.y < groundY + 1.5f)
            Shatter();
    }

    public void TakeHit()
    {
        if (dying) return;
        hitsLeft--;
        CombatFx.Debris(transform.position + Vector3.up * 1.4f, 3, 4f, new Color(0.7f, 0.6f, 0.5f, 1f), 0.5f);
        if (hitsLeft <= 0) Shatter();
    }

    public void Shatter()
    {
        if (dying) return;
        dying = true;
        CombatFx.Debris(transform.position + Vector3.up * 1.4f, 10, 6f, new Color(0.7f, 0.6f, 0.5f, 1f));
        CombatFx.Dust(transform.position, 4, 0.6f);
        all.Remove(this);
        Destroy(gameObject);
    }

    private void OnDestroy() { all.Remove(this); }
}

// ======================================================================
// Каменный приспешник — «обычный враг» (его можно приручить и он считается для EchoSlam)
// ======================================================================
public class GolemMinion : MonoBehaviour
{
    private static int count;
    public static int Count => count;

    public int contactDamage = 8;
    public float moveSpeed = 2.8f;
    public float contactCooldown = 1.2f;
    public float lifetime = 25f;

    private float groundY;
    private float bornAt;
    private float nextHit;
    private Health health;
    private EnemyTameable tameable;
    private SpriteRenderer sr;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { count = 0; }

    public static GolemMinion Spawn(float x, float groundY, int hp, int dmg, float speed, float lifetime)
    {
        Sprite sp = CombatFx.Art("fx_pebble");
        SpriteRenderer sr = CombatFx.MakeSprite("GolemMinion", sp, new Vector3(x, groundY, 0f), Color.white, 9);
        GameObject go = sr.gameObject;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.1f, 0.85f);
        col.offset = new Vector2(0f, 0.45f);

        Health h = go.AddComponent<Health>();
        h.SetMaxHealthAndRefill(hp);
        h.invulnerabilityTime = 0f;

        EnemyTameable t = go.AddComponent<EnemyTameable>();
        t.groundY = groundY;
        t.contactDamage = dmg;

        GolemMinion m = go.AddComponent<GolemMinion>();
        m.contactDamage = dmg; m.moveSpeed = speed; m.lifetime = lifetime; m.groundY = groundY;
        m.health = h; m.tameable = t; m.sr = sr; m.bornAt = Time.time;
        count++;
        CombatFx.Debris(new Vector3(x, groundY, 0f), 6, 5f, new Color(0.7f, 0.6f, 0.5f, 1f));
        return m;
    }

    private void Update()
    {
        if (health != null && health.IsDead) { Die(); return; }
        if (tameable != null && tameable.IsTamed) return;        // приручённым управляет EnemyTameable
        if (Time.time - bornAt > lifetime) { Die(); return; }

        // Идём к игрокам по земле.
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null) return;
        float dx = playerObj.transform.position.x - transform.position.x;
        if (sr != null) sr.flipX = dx < 0f;
        if (Mathf.Abs(dx) > 0.6f)
            transform.position += new Vector3(Mathf.Sign(dx) * moveSpeed * Time.deltaTime, 0f, 0f);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (tameable != null && tameable.IsTamed) return;     // приручённый НЕ бьёт игрока
        if (Time.time < nextHit) return;
        Health target = BossDamage.PlayerHealthOf(other);
        if (target == null) return;
        // Урон приспешника — не атака босса: Phase (Gafasa) его не блокирует, но парирование и BladeMail работают.
        target.TakeBossDamage(contactDamage, transform, true, true, false);
        nextHit = Time.time + contactCooldown;
    }

    private void Die()
    {
        CombatFx.Debris(transform.position + Vector3.up * 0.4f, 8, 5f, new Color(0.7f, 0.6f, 0.5f, 1f));
        Destroy(gameObject);
    }

    private void OnDestroy() { count = Mathf.Max(0, count - 1); }
}
