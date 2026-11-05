using UnityEngine;

/// <summary>
/// Менеджер единственного игрового персонажа (имя сохранено для совместимости со сценами и скриптами).
/// Отряд, AI-напарники и переключение героев не используются.
/// </summary>
public class CharacterPartyManager : MonoBehaviour
{
    [Header("Единственный игровой персонаж")]
    public CharacterStats[] party = new CharacterStats[1];

    private bool gameOverShown;

    public CharacterStats ActiveCharacter
    {
        get
        {
            if (party == null || party.Length == 0) return null;
            return party[0];
        }
    }

    private void Start()
    {
        if (party == null || party.Length == 0 || party[0] == null)
        {
            CharacterStats found = FindAnyObjectByType<CharacterStats>();
            if (found == null)
            {
                Debug.LogError("CharacterPartyManager: не назначен игровой персонаж.");
                enabled = false;
                return;
            }
            party = new CharacterStats[] { found };
        }

        party = new CharacterStats[] { party[0] };
        ConfigurePlayer(ActiveCharacter);
        UpdateBossTargets(ActiveCharacter.transform);
    }

    private void Update()
    {
        CharacterStats player = ActiveCharacter;
        if (player == null || gameOverShown) return;

        Health health = player.GetComponent<Health>();
        if (health != null && health.IsDead) ShowGameOver(player);
    }

    private void ConfigurePlayer(CharacterStats stats)
    {
        if (stats == null) return;
        GameObject player = stats.gameObject;
        player.tag = "Player";

        if (player.GetComponent<CharacterMana>() == null) player.AddComponent<CharacterMana>();
        if (player.GetComponent<CharacterStatusEffects>() == null) player.AddComponent<CharacterStatusEffects>();
        if (player.GetComponent<CharacterAbilityController>() == null) player.AddComponent<CharacterAbilityController>();
        if (player.GetComponent<ParrySystem>() == null) player.AddComponent<ParrySystem>();

        CharacterAIController ai = player.GetComponent<CharacterAIController>();
        if (ai != null) ai.enabled = false;

        SetEnabled<SideViewMovement>(player, true);
        SetEnabled<PlayerDash>(player, true);
        SetEnabled<PlayerShooting>(player, true);
        SetEnabled<CharacterAbilityController>(player, true);

        ParrySystem parry = player.GetComponent<ParrySystem>();
        if (parry != null)
        {
            parry.enabled = stats.character == CharacterId.Ney;
            parry.allowPlayerInput = true;
        }

        Health playerHealth = player.GetComponent<Health>();
        foreach (HealthBar bar in FindObjectsByType<HealthBar>(FindObjectsInactive.Exclude))
        {
            // Полоса босса привязана к боссу — не перепривязываем её к игроку.
            if (bar.health != null && BossLocator.IsBoss(bar.health)) continue;
            bar.health = playerHealth;
        }

        CharacterMana playerMana = player.GetComponent<CharacterMana>();
        foreach (ManaBar bar in FindObjectsByType<ManaBar>(FindObjectsInactive.Exclude)) bar.mana = playerMana;
    }

    public void ApplySelectedCharacter(CharacterStats stats)
    {
        if (stats == null) return;
        party = new CharacterStats[] { stats };
        ConfigurePlayer(stats);
        UpdateBossTargets(stats.transform);
        gameOverShown = false;
    }

    private void UpdateBossTargets(Transform target)
    {
        foreach (GolemBoss golem in FindObjectsByType<GolemBoss>(FindObjectsInactive.Exclude)) golem.player = target;
        foreach (BossShooting attack in FindObjectsByType<BossShooting>(FindObjectsInactive.Exclude)) attack.player = target;
        foreach (BossDashAttack attack in FindObjectsByType<BossDashAttack>(FindObjectsInactive.Exclude)) attack.player = target;
        foreach (BossStompAttack attack in FindObjectsByType<BossStompAttack>(FindObjectsInactive.Exclude)) attack.player = target;
        foreach (BossLaserAttack attack in FindObjectsByType<BossLaserAttack>(FindObjectsInactive.Exclude)) attack.player = target;
        foreach (BossFinalAttack attack in FindObjectsByType<BossFinalAttack>(FindObjectsInactive.Exclude)) attack.player = target;
        foreach (BossMovement movement in FindObjectsByType<BossMovement>(FindObjectsInactive.Exclude)) movement.player = target;
    }

    // Оставлено для совместимости.
    public void SwitchTo(int index)
    {
        if (index != 0 || ActiveCharacter == null) return;
        ConfigurePlayer(ActiveCharacter);
        UpdateBossTargets(ActiveCharacter.transform);
    }

    private void ShowGameOver(CharacterStats player)
    {
        gameOverShown = true;
        PlayerGameOver gameOver = player.GetComponent<PlayerGameOver>();
        if (gameOver != null) gameOver.TriggerGameOver();
        Debug.Log("Игровой персонаж погиб.");
    }

    private void SetEnabled<T>(GameObject target, bool value) where T : Behaviour
    {
        T component = target.GetComponent<T>();
        if (component != null) component.enabled = value;
    }

    // ---- Совместимость с прежним API (используется CharacterAIController) ----
    public Transform GetBossTarget() { return BossLocator.Find(); }
    public int ConsumeMulticastCount()
    {
        CharacterAbilityController a = ActiveCharacter != null ? ActiveCharacter.GetComponent<CharacterAbilityController>() : null;
        return a != null ? a.ConsumeMulticastCount() : 1;
    }
}
