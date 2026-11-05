using UnityEngine;

/// <summary>
/// Показывает выбор одного из восьми героев при запуске сцены.
/// На время выбора останавливает игровой мир.
/// </summary>
public class CharacterSelectionController : MonoBehaviour
{
    [Header("Ссылки")]
    public CharacterStats playerStats;
    public CharacterPartyManager characterManager;

    private bool selectionFinished;
    private float previousTimeScale = 1f;
    private GUIStyle titleStyle;
    private GUIStyle cardStyle;
    private GUIStyle infoStyle;
    private GUIStyle buttonStyle;

    private static readonly CharacterId[] Characters =
    {
        CharacterId.Ney,
        CharacterId.Gegwer,
        CharacterId.Smirqc,
        CharacterId.Shoshkabes,
        CharacterId.Nonlyy,
        CharacterId.KiberKotleta,
        CharacterId.Lili,
        CharacterId.Gafasa
    };

    private void Awake()
    {
        if (playerStats == null) playerStats = GetComponent<CharacterStats>();
        if (characterManager == null) characterManager = FindAnyObjectByType<CharacterPartyManager>();
    }

    private void Start()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        selectionFinished = false;
    }

    private void OnDestroy()
    {
        if (!selectionFinished) Time.timeScale = previousTimeScale;
    }

    private void OnGUI()
    {
        if (selectionFinished) return;
        EnsureStyles();

        float panelWidth = Mathf.Min(900f, Screen.width - 40f);
        float panelHeight = Mathf.Min(650f, Screen.height - 30f);
        Rect panel = new Rect((Screen.width - panelWidth) * 0.5f,
            (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);

        GUI.color = new Color(0f, 0f, 0f, 0.88f);
        GUI.Box(panel, GUIContent.none);
        GUI.color = Color.white;

        GUILayout.BeginArea(new Rect(panel.x + 24f, panel.y + 18f, panel.width - 48f, panel.height - 36f));
        GUILayout.Label("ВЫБЕРИ СВОЕГО ПЕРСОНАЖА", titleStyle, GUILayout.Height(48f));
        GUILayout.Label("В этой игре ты управляешь только одним выбранным героем.", infoStyle, GUILayout.Height(28f));

        float gap = 8f;
        float cardWidth = (panel.width - 48f - gap) * 0.5f;
        for (int row = 0; row < 4; row++)
        {
            GUILayout.BeginHorizontal();
            DrawCharacterCard(Characters[row * 2], cardWidth);
            GUILayout.Space(gap);
            DrawCharacterCard(Characters[row * 2 + 1], cardWidth);
            GUILayout.EndHorizontal();
            GUILayout.Space(7f);
        }

        GUILayout.FlexibleSpace();
        GUILayout.Label("Управление: WASD / движение, мышь / атака, Shift / рывок, Q / способность (Ney: E или Q — парирование).", infoStyle);
        GUILayout.EndArea();
    }

    private void DrawCharacterCard(CharacterId id, float width)
    {
        GetCharacterInfo(id, out int hp, out int agility, out int mana, out float attackSpeed, out int damage, out string skill);
        GUILayout.BeginVertical(cardStyle, GUILayout.Width(width), GUILayout.Height(91f));
        GUILayout.Label(id.ToString(), titleStyle, GUILayout.Height(25f));
        GUILayout.Label("HP " + hp + "   Ловкость " + agility + "   Мана " + mana, infoStyle);
        GUILayout.Label("Атака " + damage + "   Скорость " + attackSpeed.ToString("0.##") + "   Навык: " + skill, infoStyle);
        if (GUILayout.Button("ВЫБРАТЬ", buttonStyle, GUILayout.Height(27f)))
            ChooseCharacter(id);
        GUILayout.EndVertical();
    }

    private void ChooseCharacter(CharacterId id)
    {
        if (selectionFinished) return;
        if (playerStats == null)
        {
            Debug.LogError("CharacterSelectionController: на игроке отсутствует CharacterStats.");
            return;
        }

        playerStats.character = id;
        playerStats.LoadBaseStats();
        playerStats.ApplyToComponents();


        CharacterMana mana = playerStats.GetComponent<CharacterMana>();
        if (mana != null) mana.RefreshAfterCharacterSelection();

        CharacterAbilityController ability = playerStats.GetComponent<CharacterAbilityController>();
        if (ability != null) ability.RefreshAfterCharacterSelection();

        ParrySystem parry = playerStats.GetComponent<ParrySystem>();
        if (parry != null)
        {
            parry.enabled = id == CharacterId.Ney;
            parry.allowPlayerInput = true;
        }

        if (characterManager != null)
            characterManager.ApplySelectedCharacter(playerStats);

        selectionFinished = true;
        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        Debug.Log("Начинаем игру за " + id + ". В команде только один игровой персонаж.");
    }

    private void EnsureStyles()
    {
        if (titleStyle != null) return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        cardStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(8, 8, 5, 5)
        };
        infoStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
        };
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };
    }

    private void GetCharacterInfo(CharacterId id, out int hp, out int agility, out int mana,
        out float attackSpeed, out int damage, out string skill)
    {
        hp = 100; agility = 100; mana = 100; attackSpeed = 1f; damage = 10;
        skill = CharacterAbilityController.NameOf(id);

        CharacterProfile[] list = playerStats != null && playerStats.profiles != null && playerStats.profiles.Length > 0
            ? playerStats.profiles : CharacterProfile.CreateDefaults();
        foreach (CharacterProfile p in list)
        {
            if (p == null || p.id != id) continue;
            hp = p.maxHealth; agility = Mathf.RoundToInt(p.agility); mana = p.maxMana;
            attackSpeed = p.attackSpeed; damage = p.attackDamage;
            break;
        }
    }
}
