using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerGameOver : MonoBehaviour
{
    public Health playerHealth;
    public GameObject gameOverPanel;

    private bool isDead = false;

    void Start()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<Health>();

        gameOverPanel.SetActive(false);
    }

    void Update()
    {
        if (isDead || playerHealth == null)
            return;

        if (playerHealth.CurrentHealth <= 0)
            Die();
    }

    void Die()
    {
        isDead = true;

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

        gameOverPanel.SetActive(true);

        Debug.Log("ИГРОК ПОГИБ!");
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}