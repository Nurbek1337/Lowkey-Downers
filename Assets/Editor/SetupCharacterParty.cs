#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupCharacterParty
{
    [MenuItem("Lowkey Downers/Setup Single Character Selection")]
    public static void SetupSingleCharacterSelection()
    {
        GameObject player = GameObject.Find("BossPlayer2");
        if (player == null)
        {
            EditorUtility.DisplayDialog("Lowkey Downers",
                "Не найден игровой объект BossPlayer2. Открой сцену BossArena2 и повтори.", "OK");
            return;
        }

        // Удаляем только старые клоны героев, созданные прежним ошибочным установщиком.
        CharacterStats[] allStats = Object.FindObjectsByType<CharacterStats>(FindObjectsInactive.Include);
        foreach (CharacterStats stats in allStats)
        {
            if (stats == null || stats.gameObject == player) continue;
            if (stats.gameObject.name.StartsWith("Party_"))
                Undo.DestroyObjectImmediate(stats.gameObject);
        }

        CharacterStats playerStats = player.GetComponent<CharacterStats>();
        if (playerStats == null) playerStats = Undo.AddComponent<CharacterStats>(player);
        playerStats.character = CharacterId.Ney;
        player.tag = "Player";

        // В исходной сцене тег Player ошибочно стоял на боссе. Исправляем это,
        // чтобы босс целился в BossPlayer2, а не в самого себя.
        GameObject boss = GameObject.Find("Boss");
        if (boss != null && boss != player && boss.CompareTag("Player"))
            boss.tag = "Untagged";

        CharacterPartyManager manager = Object.FindAnyObjectByType<CharacterPartyManager>();
        GameObject managerObject;
        if (manager == null)
        {
            managerObject = new GameObject("CharacterPartyManager");
            Undo.RegisterCreatedObjectUndo(managerObject, "Create Single Character Manager");
            manager = Undo.AddComponent<CharacterPartyManager>(managerObject);
        }
        else
        {
            managerObject = manager.gameObject;
        }

        manager.party = new CharacterStats[] { playerStats };
        manager.enabled = true;
        if (managerObject.GetComponent<BossStunUpdater>() == null)
            Undo.AddComponent<BossStunUpdater>(managerObject);

        CharacterSelectionController selection = player.GetComponent<CharacterSelectionController>();
        if (selection == null) selection = Undo.AddComponent<CharacterSelectionController>(player);
        selection.playerStats = playerStats;
        selection.characterManager = manager;
        selection.enabled = true;

        // Старые AI-компоненты на игровом объекте не должны управлять персонажем.
        CharacterAIController ai = player.GetComponent<CharacterAIController>();
        if (ai != null) ai.enabled = false;

        Selection.activeGameObject = player;
        EditorSceneManager.MarkSceneDirty(player.scene);
        EditorSceneManager.MarkSceneDirty(managerObject.scene);
        EditorUtility.DisplayDialog("Lowkey Downers",
            "Готово! Теперь в игре один герой. Нажми Cmd+S, затем Play: в начале появится выбор одного из 8 персонажей. Напарники и переключение 1/2/3 отключены.", "OK");
    }
}
#endif
