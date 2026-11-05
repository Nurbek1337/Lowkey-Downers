using UnityEngine;

/// <summary>
/// Режиссёр арены «Живые скалы»: исправляет теги, создаёт частицы пыли и пульсацию кристаллов,
/// рисует компактный HUD (мана, способность, души, Multicast, имя и фаза босса).
/// Всё создаётся в рантайме и безопасно: при отсутствии ссылок HUD просто не рисуется.
/// </summary>
public class ArenaDirector : MonoBehaviour
{
    [Header("Ссылки (находятся автоматически)")]
    public CharacterStats player;
    public GolemBoss boss;

    [Header("Атмосфера")]
    public bool ambientDust = true;
    public Color dustColor = new Color(0.8f, 0.75f, 0.9f, 0.5f);
    public Transform crystalsRoot;
    public float crystalPulseSpeed = 1.6f;

    [Header("HUD")]
    public bool showHud = true;
    public string bossTitle = "ХРАНИТЕЛЬ ЖИВЫХ СКАЛ";

    private SpriteRenderer[] crystalGlows;
    private GUIStyle label, small;
    private CharacterMana mana;
    private CharacterAbilityController ability;
    private ParrySystem parry;
    private Health bossHealth;

    private void Start()
    {
        if (boss == null) boss = FindAnyObjectByType<GolemBoss>();
        if (player == null) player = FindAnyObjectByType<CharacterStats>();

        // Исправление: босс не должен иметь тег Player (иначе его атаки задевали бы его самого).
        if (boss != null && boss.CompareTag("Player")) boss.tag = "Untagged";
        if (player != null) player.tag = "Player";

        if (boss != null) bossHealth = boss.GetComponent<Health>();
        if (crystalsRoot != null) crystalGlows = crystalsRoot.GetComponentsInChildren<SpriteRenderer>();
        if (ambientDust) CreateDust();
    }

    private void CreateDust()
    {
        GameObject go = new GameObject("AmbientDust");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(0f, 0f, 0f);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        main.startColor = dustColor;
        main.gravityModifier = -0.01f;
        main.maxParticles = 160;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 14f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(18f, 9f, 1f);

        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.3f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.12f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
        rend.material = new Material(Shader.Find("Sprites/Default"));
        rend.sortingOrder = 25;

        ps.Play();
    }

    private void Update()
    {
        if (crystalGlows != null)
        {
            float k = 0.8f + 0.2f * Mathf.Sin(Time.time * crystalPulseSpeed);
            for (int i = 0; i < crystalGlows.Length; i++)
            {
                SpriteRenderer sr = crystalGlows[i];
                if (sr == null || !sr.name.Contains("Halo")) continue;
                Color c = sr.color;
                c.a = Mathf.Clamp01(0.35f * k * (0.7f + 0.3f * Mathf.Sin(Time.time * 0.9f + i)));
                sr.color = c;
            }
        }
    }

    private void OnGUI()
    {
        if (!showHud || player == null || Time.timeScale <= 0f) return;
        if (label == null)
        {
            label = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            small = new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = new Color(0.85f, 0.85f, 0.9f) } };
        }
        if (mana == null) mana = player.GetComponent<CharacterMana>();
        if (ability == null) ability = player.GetComponent<CharacterAbilityController>();
        if (parry == null) parry = player.GetComponent<ParrySystem>();

        float x = 16f, y = Screen.height - 96f;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.Box(new Rect(x - 6f, y - 6f, 290f, 90f), GUIContent.none);
        GUI.color = Color.white;

        GUI.Label(new Rect(x, y, 280f, 20f), player.character.ToString() + " — " + CharacterAbilityController.NameOf(player.character), label);
        if (mana != null) Bar(new Rect(x, y + 24f, 270f, 10f), mana.Normalized, new Color(0.25f, 0.5f, 1f), "Мана " + Mathf.FloorToInt(mana.CurrentMana) + "/" + Mathf.RoundToInt(mana.MaxMana));

        float cd = 0f, cdMax = 1f; string extra = "";
        if (player.character == CharacterId.Ney && parry != null)
        {
            cd = parry.CooldownRemaining; cdMax = Mathf.Max(0.01f, parry.CooldownDuration);
            extra = parry.IsParrying ? "ОКНО ПАРИРОВАНИЯ!" : "E / Q — парирование";
        }
        else if (ability != null)
        {
            cd = ability.CooldownRemaining; cdMax = Mathf.Max(0.01f, ability.CooldownDuration);
            extra = "Q — способность";
            if (player.character == CharacterId.Shoshkabes) extra = "Души: " + ability.CurrentSouls;
            if (player.character == CharacterId.Nonlyy && ability.IsMulticastReady) extra = "MULTICAST " + ability.MulticastRemaining.ToString("0.0") + "с";
        }
        Bar(new Rect(x, y + 40f, 270f, 10f), 1f - Mathf.Clamp01(cd / cdMax), cd > 0f ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.3f, 0.9f, 0.4f),
            cd > 0f ? "Перезарядка " + cd.ToString("0.0") + "с" : "Готово");
        GUI.Label(new Rect(x, y + 54f, 280f, 20f), extra, small);

        if (boss != null && bossHealth != null && boss.State != GolemState.Dead)
        {
            string phase = boss.State == GolemState.Final ? "ФИНАЛЬНАЯ АТАКА" : boss.State == GolemState.Exhausted ? "ИСТОЩЁН — ДОБЕЙ!" : (boss.Phase >= 2 ? "ФАЗА 2" : "ФАЗА 1");
            Rect r = new Rect(0f, 6f, Screen.width, 22f);
            GUIStyle c = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(r, bossTitle + "  ·  " + phase, c);
        }
    }

    private void Bar(Rect r, float fill, Color color, string text)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = color;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fill), r.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(r.x + 4f, r.y - 4f, r.width, 18f), text, small);
    }
}
