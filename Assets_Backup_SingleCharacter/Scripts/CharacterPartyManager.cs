using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Выбор трёх игровых героев из ростера из восьми персонажей.
/// Один герой управляется игроком, двое остальных помогают автоматически.
/// </summary>
public class CharacterPartyManager : MonoBehaviour
{
    [Header("Все 8 игровых персонажей")]
    public CharacterStats[] roster = new CharacterStats[8];

    [Header("Текущая команда из трёх героев")]
    public CharacterStats[] party = new CharacterStats[3];

    [Header("Управление во время боя")]
    public KeyCode firstCharacterKey = KeyCode.Alpha1;
    public KeyCode secondCharacterKey = KeyCode.Alpha2;
    public KeyCode thirdCharacterKey = KeyCode.Alpha3;
    public KeyCode abilityKey = KeyCode.Q;

    [SerializeField] private int activeIndex;

    private readonly bool[] selected = new bool[8];
    private bool selectionMode = true;
    private float previousTimeScale = 1f;
    private int multicastCount = 1;
    private float multicastExpiresAt;

    public CharacterStats ActiveCharacter => IsValidPartyIndex(activeIndex) ? party[activeIndex] : null;

    private void Start()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // Стартовый выбор — три первых героя. Игрок может поменять состав перед боем.
        if (roster != null && roster.Length >= 3)
        {
            selected[0] = roster[0] != null;
            selected[1] = roster[1] != null;
            selected[2] = roster[2] != null;
        }

        if (roster != null)
        {
            foreach (CharacterStats character in roster)
            {
                if (character != null)
                    character.gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (selectionMode) return;

        if (Input.GetKeyDown(firstCharacterKey)) SwitchTo(0);
        if (Input.GetKeyDown(secondCharacterKey)) SwitchTo(1);
        if (Input.GetKeyDown(thirdCharacterKey)) SwitchTo(2);

        Health activeHealth = ActiveCharacter != null ? ActiveCharacter.GetComponent<Health>() : null;
        if (activeHealth == null || activeHealth.CurrentHealth > 0) return;

        for (int offset = 1; offset <= party.Length; offset++)
        {
            int nextIndex = (activeIndex + offset) % party.Length;
            if (party[nextIndex] == null) continue;

            Health nextHealth = party[nextIndex].GetComponent<Health>();
            if (nextHealth != null && nextHealth.CurrentHealth > 0)
            {
                SwitchTo(nextIndex);
                Debug.Log("Герой погиб. Переключение на следующего союзника.");
                return;
            }
        }

        ShowPartyGameOver();
    }

    private void OnGUI()
    {
        if (!selectionMode) return;

        const float panelWidth = 620f;
        const float panelHeight = 560f;
        Rect panel = new Rect(
            (Screen.width - panelWidth) * 0.5f,
            (Screen.height - panelHeight) * 0.5f,
            panelWidth,
            panelHeight
        );

        GUILayout.BeginArea(panel, "ВЫБЕРИ ТРЁХ ГЕРОЕВ", GUI.skin.window);
        GUILayout.Space(8f);
        GUILayout.Label("В команде будет 3 персонажа: один под твоим управлением, двое — под управлением ИИ.");
        GUILayout.Space(8f);

        int selectedCount = 0;
        for (int i = 0; i < 8; i++)
        {
            CharacterStats character = roster != null && i < roster.Length ? roster[i] : null;
            if (character == null)
            {
                GUILayout.Label((i + 1) + ". Персонаж не создан");
                continue;
            }

            character.LoadBaseStats();
            bool newValue = GUILayout.Toggle(selected[i],
                (i + 1) + ". " + character.character +
                "   HP: " + character.MaxHealth +
                "   Ловкость: " + character.Agility +
                "   Мана: " + character.MaxMana);

            if (newValue && !selected[i] && CountSelected() >= 3)
                newValue = false;

            selected[i] = newValue;
            if (selected[i]) selectedCount++;
        }

        GUILayout.Space(10f);
        GUILayout.Label("Выбрано: " + selectedCount + " / 3");
        GUI.enabled = selectedCount == 3;
        if (GUILayout.Button("НАЧАТЬ БОЙ", GUILayout.Height(42f)))
            StartBattle();
        GUI.enabled = true;
        GUILayout.EndArea();
    }

    private int CountSelected()
    {
        int count = 0;
        for (int i = 0; i < selected.Length; i++)
            if (selected[i]) count++;
        return count;
    }

    private void StartBattle()
    {
        List<CharacterStats> chosen = new List<CharacterStats>(3);
        for (int i = 0; i < 8; i++)
        {
            if (selected[i] && roster != null && i < roster.Length && roster[i] != null)
                chosen.Add(roster[i]);
        }

        if (chosen.Count != 3) return;

        party = chosen.ToArray();
        Vector3 spawnPosition = party[0].transform.position;

        // Включаем только выбранную тройку. Остальные пять остаются в ростере.
        foreach (CharacterStats character in roster)
        {
            if (character != null)
                character.gameObject.SetActive(false);
        }

        for (int i = 0; i < party.Length; i++)
        {
            party[i].gameObject.SetActive(true);
            party[i].transform.position = spawnPosition + new Vector3((i - 1) * 1.15f, 0f, 0f);
            ConfigureMember(party[i]);
        }

        selectionMode = false;
        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        SwitchTo(0);
        Debug.Log("Команда выбрана: " + party[0].character + ", " + party[1].character + ", " + party[2].character);
    }

    public void SwitchTo(int index)
    {
        if (selectionMode || !IsValidPartyIndex(index) || party[index] == null) return;
        activeIndex = index;

        for (int i = 0; i < party.Length; i++)
        {
            if (party[i] == null) continue;
            bool controlled = i == activeIndex;

            SetEnabled<SideViewMovement>(party[i].gameObject, controlled);
            SetEnabled<PlayerDash>(party[i].gameObject, controlled);
            SetEnabled<PlayerShooting>(party[i].gameObject, controlled);
            SetEnabled<ParrySystem>(party[i].gameObject, party[i].character == CharacterId.Ney);

            ParrySystem parry = party[i].GetComponent<ParrySystem>();
            if (parry != null) parry.allowPlayerInput = controlled;

            SetEnabled<CharacterAIController>(party[i].gameObject, !controlled);
            SetEnabled<CharacterAbilityController>(party[i].gameObject, true);
            party[i].gameObject.tag = controlled ? "Player" : "Untagged";
        }

        Transform activeTransform = ActiveCharacter.transform;
        HealthBar[] healthBars = FindObjectsByType<HealthBar>();
        foreach (HealthBar bar in healthBars)
            bar.health = ActiveCharacter.GetComponent<Health>();

        ManaBar[] manaBars = FindObjectsByType<ManaBar>();
        foreach (ManaBar bar in manaBars)
            bar.mana = ActiveCharacter.GetComponent<CharacterMana>();

        BossShooting[] shootingAttacks = FindObjectsByType<BossShooting>();
        foreach (BossShooting attack in shootingAttacks) attack.player = activeTransform;
        BossDashAttack[] dashAttacks = FindObjectsByType<BossDashAttack>();
        foreach (BossDashAttack attack in dashAttacks) attack.player = activeTransform;
        BossStompAttack[] stompAttacks = FindObjectsByType<BossStompAttack>();
        foreach (BossStompAttack attack in stompAttacks) attack.player = activeTransform;
        BossLaserAttack[] laserAttacks = FindObjectsByType<BossLaserAttack>();
        foreach (BossLaserAttack attack in laserAttacks) attack.player = activeTransform;
        BossFinalAttack[] finalAttacks = FindObjectsByType<BossFinalAttack>();
        foreach (BossFinalAttack attack in finalAttacks) attack.player = activeTransform;
        BossMovement[] bossMovements = FindObjectsByType<BossMovement>();
        foreach (BossMovement movement in bossMovements) movement.player = activeTransform;

        Debug.Log("Активный персонаж: " + ActiveCharacter.character);
    }

    private void ConfigureMember(CharacterStats character)
    {
        GameObject member = character.gameObject;
        if (character.character == CharacterId.Ney && member.GetComponent<ParrySystem>() == null)
            member.AddComponent<ParrySystem>();
        if (member.GetComponent<CharacterMana>() == null) member.AddComponent<CharacterMana>();
        if (member.GetComponent<CharacterStatusEffects>() == null) member.AddComponent<CharacterStatusEffects>();
        if (member.GetComponent<CharacterAbilityController>() == null) member.AddComponent<CharacterAbilityController>();
        if (member.GetComponent<CharacterAIController>() == null) member.AddComponent<CharacterAIController>();
    }

    private void ShowPartyGameOver()
    {
        foreach (CharacterStats member in party)
        {
            if (member == null) continue;
            PlayerGameOver gameOver = member.GetComponent<PlayerGameOver>();
            if (gameOver != null && gameOver.gameOverPanel != null)
            {
                gameOver.gameOverPanel.SetActive(true);
                enabled = false;
                return;
            }
        }
        Debug.Log("Вся команда погибла. Назначь GameOverPanel в PlayerGameOver.");
        enabled = false;
    }

    private bool IsValidPartyIndex(int index) => party != null && index >= 0 && index < party.Length;

    private void SetEnabled<T>(GameObject target, bool value) where T : Behaviour
    {
        T component = target.GetComponent<T>();
        if (component != null) component.enabled = value;
    }

    public void ActivateMulticast(int count, float duration)
    {
        multicastCount = Mathf.Clamp(count, 1, 4);
        multicastExpiresAt = Time.time + duration;
        Debug.Log("Multicast подготовлен: " + multicastCount + " срабатываний.");
    }

    public int ConsumeMulticastCount()
    {
        if (Time.time > multicastExpiresAt || multicastCount <= 1)
        {
            multicastCount = 1;
            return 1;
        }
        int result = multicastCount;
        multicastCount = 1;
        multicastExpiresAt = 0f;
        return result;
    }

    public void HealAll(int amount)
    {
        foreach (CharacterStats member in party)
        {
            if (member == null) continue;
            Health health = member.GetComponent<Health>();
            if (health != null) health.Heal(amount);
        }
    }

    public Transform GetBossTarget()
    {
        BossMovement movement = FindAnyObjectByType<BossMovement>();
        return movement != null ? movement.transform : null;
    }
}
