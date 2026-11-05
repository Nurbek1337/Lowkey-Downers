using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Процедурные эффекты боя (вспышки, кольца, обломки, тряска камеры) и загрузка спрайтов из Resources/Golem.
/// Если спрайт не найден, используется белый квадрат/круг — игра остаётся играбельной.
/// </summary>
public static class CombatFx
{
    private static Sprite square, circle;
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    private static Transform fxRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        square = null; circle = null; fxRoot = null;
        cache.Clear();
    }

    public static Transform Root
    {
        get
        {
            if (fxRoot == null) fxRoot = new GameObject("_CombatFx").transform;
            return fxRoot;
        }
    }

    public static Sprite Square
    {
        get
        {
            if (square == null)
            {
                Texture2D t = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                Color[] px = new Color[16];
                for (int i = 0; i < 16; i++) px[i] = Color.white;
                t.SetPixels(px); t.Apply();
                square = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            }
            return square;
        }
    }

    public static Sprite Circle
    {
        get
        {
            if (circle == null)
            {
                const int n = 64;
                Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2((n - 1) / 2f, (n - 1) / 2f)) / (n / 2f);
                    t.SetPixel(x, y, new Color(1, 1, 1, d <= 1f ? 1f : 0f));
                }
                t.Apply();
                circle = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            }
            return circle;
        }
    }

    /// <summary>Спрайт из Assets/Resources/Golem/. Возвращает null, если не найден.</summary>
    public static Sprite Art(string name)
    {
        if (cache.TryGetValue(name, out Sprite s) && s != null) return s;
        s = Resources.Load<Sprite>("Golem/" + name);
        cache[name] = s;
        return s;
    }

    public static SpriteRenderer MakeSprite(string name, Sprite sprite, Vector3 pos, Color color, int order, Transform parent = null)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent != null ? parent : Root, false);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite != null ? sprite : Square;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    // ------------------------------------------------------------------
    public static void Flash(Vector3 pos, Color color, float size, float life)
    {
        SpriteRenderer sr = MakeSprite("Flash", Art("fx_glow") ?? Circle, pos, color, 30);
        sr.transform.localScale = Vector3.one * size;
        FxSprite fx = sr.gameObject.AddComponent<FxSprite>();
        fx.life = life; fx.growPerSecond = size * 1.2f; fx.fade = true;
    }

    public static void Ring(Vector3 pos, Color color, float startSize, float endSize, float life)
    {
        SpriteRenderer sr = MakeSprite("Ring", Art("fx_ring"), pos, color, 29);
        sr.transform.localScale = Vector3.one * startSize;
        FxSprite fx = sr.gameObject.AddComponent<FxSprite>();
        fx.life = life; fx.growPerSecond = (endSize - startSize) / Mathf.Max(0.01f, life); fx.fade = true;
    }

    public static void Debris(Vector3 pos, int count, float speed, Color tint, float life = 0.9f)
    {
        for (int i = 0; i < count; i++)
        {
            Sprite sp = Art("fx_debris_" + Random.Range(0, 3));
            SpriteRenderer sr = MakeSprite("Debris", sp, pos + (Vector3)Random.insideUnitCircle * 0.3f, tint, 28);
            sr.transform.localScale = Vector3.one * Random.Range(0.8f, 1.4f);
            FxSprite fx = sr.gameObject.AddComponent<FxSprite>();
            float ang = Random.Range(25f, 155f) * Mathf.Deg2Rad;
            fx.velocity = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(speed * 0.4f, speed);
            fx.velocity.x += Random.Range(-speed * 0.4f, speed * 0.4f);
            fx.gravity = 14f; fx.life = life * Random.Range(0.7f, 1.2f); fx.spin = Random.Range(-360f, 360f); fx.fade = true;
        }
    }

    public static void Dust(Vector3 pos, int count, float spread, float life = 0.8f)
    {
        for (int i = 0; i < count; i++)
        {
            SpriteRenderer sr = MakeSprite("Dust", Art("fx_dust") ?? Circle, pos + new Vector3(Random.Range(-spread, spread), 0f, 0f),
                new Color(0.75f, 0.7f, 0.65f, 0.6f), 27);
            sr.transform.localScale = Vector3.one * Random.Range(0.5f, 1.1f);
            FxSprite fx = sr.gameObject.AddComponent<FxSprite>();
            fx.velocity = new Vector2(Random.Range(-1.2f, 1.2f) , Random.Range(0.3f, 1.4f));
            fx.life = life * Random.Range(0.7f, 1.3f); fx.growPerSecond = 0.9f; fx.fade = true;
        }
    }

    public static void Shake(float amplitude, float duration)
    {
        CameraShaker.Shake(amplitude, duration);
    }

    /// <summary>Телеграф опасной зоны: прямоугольник, который заполняется за время windup. Уничтожается сам.</summary>
    public static SpriteRenderer Telegraph(Vector2 center, Vector2 size, float windup, Color color, int order = -4)
    {
        SpriteRenderer sr = MakeSprite("Telegraph", Square, center, color, order);
        sr.transform.localScale = new Vector3(size.x * 4f, size.y * 4f, 1f);   // Square: 4px при PPU 4 = 1 юнит
        TelegraphFx fx = sr.gameObject.AddComponent<TelegraphFx>();
        fx.duration = windup; fx.baseColor = color;
        return sr;
    }

    public static SpriteRenderer TelegraphCircle(Vector2 center, float radius, float windup, Color color, int order = -4)
    {
        SpriteRenderer sr = MakeSprite("TelegraphCircle", Circle, center, color, order);
        sr.transform.localScale = new Vector3(radius * 2f, radius * 2f * 0.35f, 1f);  // плоский эллипс "на земле"
        TelegraphFx fx = sr.gameObject.AddComponent<TelegraphFx>();
        fx.duration = windup; fx.baseColor = color;
        return sr;
    }

    public static void ClearAll()
    {
        if (fxRoot != null)
            for (int i = fxRoot.childCount - 1; i >= 0; i--) Object.Destroy(fxRoot.GetChild(i).gameObject);
    }
}

/// <summary>Простой летящий/гаснущий спрайт.</summary>
public class FxSprite : MonoBehaviour
{
    public Vector2 velocity;
    public float gravity;
    public float life = 0.6f;
    public float growPerSecond;
    public float spin;
    public bool fade = true;

    private float t;
    private SpriteRenderer sr;
    private Color start;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) start = sr.color;
    }

    private void Update()
    {
        t += Time.deltaTime;
        velocity.y -= gravity * Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        if (spin != 0f) transform.Rotate(0f, 0f, spin * Time.deltaTime);
        if (growPerSecond != 0f) transform.localScale += Vector3.one * growPerSecond * Time.deltaTime;
        if (fade && sr != null)
        {
            Color c = start; c.a = start.a * Mathf.Clamp01(1f - t / life);
            sr.color = c;
        }
        if (t >= life) Destroy(gameObject);
    }
}

/// <summary>Мигающий телеграф: прозрачность растёт к концу подготовки, затем исчезает.</summary>
public class TelegraphFx : MonoBehaviour
{
    public float duration = 1f;
    public Color baseColor = new Color(1f, 0.2f, 0.1f, 0.5f);
    public float extraLife = 0.1f;
    private float t;
    private SpriteRenderer sr;

    private void Start() { sr = GetComponent<SpriteRenderer>(); }

    private void Update()
    {
        t += Time.deltaTime;
        if (sr != null)
        {
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, duration));
            float pulse = 0.75f + 0.25f * Mathf.Sin(t * (10f + 14f * k));
            Color c = baseColor; c.a = baseColor.a * Mathf.Lerp(0.35f, 1f, k) * pulse;
            sr.color = c;
        }
        if (t >= duration + extraLife) Destroy(gameObject);
    }
}

/// <summary>Тряска камеры. Добавляется на Main Camera автоматически.</summary>
public class CameraShaker : MonoBehaviour
{
    private static CameraShaker instance;
    private float amplitude, until, total;
    private Vector3 basePos;
    private bool active;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    public static void Shake(float amp, float duration)
    {
        if (instance == null)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            instance = cam.GetComponent<CameraShaker>();
            if (instance == null) instance = cam.gameObject.AddComponent<CameraShaker>();
        }
        instance.StartShake(amp, duration);
    }

    private void StartShake(float amp, float duration)
    {
        if (!active) basePos = transform.position;
        if (amp >= amplitude || Time.time > until) { amplitude = amp; total = duration; until = Time.time + duration; }
        active = true;
    }

    private void LateUpdate()
    {
        if (!active) return;
        if (Time.time >= until)
        {
            transform.position = basePos;
            active = false; amplitude = 0f;
            return;
        }
        float k = (until - Time.time) / Mathf.Max(0.01f, total);
        Vector2 o = Random.insideUnitCircle * amplitude * k;
        transform.position = basePos + new Vector3(o.x, o.y, 0f);
    }
}
