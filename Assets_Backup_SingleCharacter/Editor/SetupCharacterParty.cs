#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupCharacterParty
{
    private static readonly CharacterId[] CharacterIds =
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

    private static readonly Color[] CharacterColors =
    {
        Color.white,
        new Color(0.85f, 0.35f, 0.35f),
        new Color(0.35f, 0.65f, 1f),
        new Color(0.75f, 0.4f, 0.95f),
        new Color(0.35f, 0.9f, 0.55f),
        new Color(1f, 0.75f, 0.25f),
        new Color(1f, 0.55f, 0.8f),
        new Color(0.45f, 0.9f, 0.9f)
    };

    [MenuItem("Lowkey Downers/Repair and Setup 8 Playable Characters")]
    public static void Setup()
    {
        GameObject player = GameObject.Find("BossPlayer2");
        if (player == null)
        {
            EditorUtility.DisplayDialog(
                "Lowkey Downers",
                "Не найден BossPlayer2. Открой исходную сцену BossArena2 и повтори.",
                "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
            "Установка восьми игровых персонажей",
            "Будут удалены только старые объекты Party_Gegwer, Party_Smirqc, CharacterRoster_* и CharacterPartyManager. Босс и остальные объекты арены останутся без изменений.",
            "Продолжить",
            "Отмена"))
            return;

        RemovePreviousGeneratedObjects();

        // В исходной сцене у Boss ошибочно стоит тег Player. Исправляем тег,
        // а игрока определяем только по точному имени BossPlayer2.
        GameObject boss = GameObject.Find("Boss");
        if (boss != null) boss.tag = "Untagged";
        player.tag = "Player";

        CharacterStats[] roster = new CharacterStats[8];
        CharacterStats ney = player.GetComponent<CharacterStats>();
        if (ney == null) ney = Undo.AddComponent<CharacterStats>(player);
        ney.character = CharacterId.Ney;
        ney.useCharacterAttackDefaults = true;
        ney.LoadBaseStats();
        ApplyCharacterColor(player, CharacterColors[0]);
        roster[0] = ney;

        for (int i = 1; i < CharacterIds.Length; i++)
        {
            GameObject clone = Object.Instantiate(
                player,
                player.transform.position,
                player.transform.rotation,
                player.transform.parent);

            Undo.RegisterCreatedObjectUndo(clone, "Create playable character " + CharacterIds[i]);
            clone.name = "CharacterRoster_" + CharacterIds[i];
            clone.tag = "Untagged";

            CharacterStats stats = clone.GetComponent<CharacterStats>();
            if (stats == null) stats = Undo.AddComponent<CharacterStats>(clone);
            stats.character = CharacterIds[i];
            stats.useCharacterAttackDefaults = true;
            stats.LoadBaseStats();
            ApplyCharacterColor(clone, CharacterColors[i]);
            clone.SetActive(false);
            roster[i] = stats;
        }

        GameObject managerObject = new GameObject("CharacterPartyManager");
        Undo.RegisterCreatedObjectUndo(managerObject, "Create Character Party Manager");
        CharacterPartyManager manager = Undo.AddComponent<CharacterPartyManager>(managerObject);
        manager.roster = roster;
        manager.party = new CharacterStats[3];
        Undo.AddComponent<BossStunUpdater>(managerObject);

        EditorSceneManager.MarkSceneDirty(player.scene);
        Selection.activeGameObject = managerObject;
        EditorUtility.DisplayDialog(
            "Готово",
            "В ростере созданы все 8 игровых персонажей. При запуске сцены появится меню выбора трёх героев. Один будет управляться игроком, двое — ИИ. Боссы не клонировались.",
            "OK");
    }

    private static void RemovePreviousGeneratedObjects()
    {
        CharacterPartyManager[] managers = Object.FindObjectsByType<CharacterPartyManager>(FindObjectsSortMode.None);
        foreach (CharacterPartyManager manager in managers)
            Undo.DestroyObjectImmediate(manager.gameObject);

        string[] oldNames =
        {
            "Party_Gegwer",
            "Party_Smirqc"
        };
        foreach (string oldName in oldNames)
        {
            GameObject oldObject = GameObject.Find(oldName);
            if (oldObject != null) Undo.DestroyObjectImmediate(oldObject);
        }

        // Удаляем только клоны, созданные предыдущей версией этого установщика.
        for (int i = 1; i < CharacterIds.Length; i++)
        {
            GameObject oldObject = GameObject.Find("CharacterRoster_" + CharacterIds[i]);
            if (oldObject != null) Undo.DestroyObjectImmediate(oldObject);
        }
    }

    private static void ApplyCharacterColor(GameObject character, Color color)
    {
        SpriteRenderer[] renderers = character.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
            renderer.color = color;
    }
}
#endif
