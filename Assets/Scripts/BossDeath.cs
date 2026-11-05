using UnityEngine;

public class BossDeath : MonoBehaviour
{
    private Health health;
    private bool isDead = false;

    void Start()
    {
        health = GetComponent<Health>();
    }

    void Update()
    {
        if (health == null || isDead) return;
        if (!health.IsDead) return;

        isDead = true;
        Debug.Log("BOSS DEFEATED!");

        GolemBoss golem = GetComponent<GolemBoss>();
        if (golem != null)
        {
            // Голем сам проигрывает анимацию смерти и вызывает ShowVictory().
            return;
        }

        ShowVictory();
        Destroy(gameObject);
    }

    public static void ShowVictory()
    {
        GameObject victoryCanvas = GameObject.Find("VictoryCanvas");
        if (victoryCanvas != null)
        {
            Transform victory = victoryCanvas.transform.Find("VictoryText");
            if (victory != null) victory.gameObject.SetActive(true);
        }

        GameObject canvas = GameObject.Find("BossHealthCanvas");
        if (canvas != null) Destroy(canvas);
    }
}
