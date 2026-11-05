using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerGameOver : MonoBehaviour
{
    public Health playerHealth;
    public GameObject gameOverPanel;

    private bool isDead = false;

    void Start()
    {
        if (playerHealth == null) playerHealth = GetComponent<Health>();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    void Update()
    {
        // Когда подключён CharacterPartyManager, смерть героя обрабатывает он (вызывая TriggerGameOver).
        if (FindAnyObjectByType<CharacterPartyManager>() != null) return;

        if (isDead || playerHealth == null) return;
        if (playerHealth.IsDead) TriggerGameOver();
    }

    public void TriggerGameOver()
    {
        if (isDead) return;
        isDead = true;

        // Отключаем управление и способности, но не Health и не себя.
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != null && script != this && !(script is Health))
                script.enabled = false;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Debug.Log("ИГРОК ПОГИБ!");
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
