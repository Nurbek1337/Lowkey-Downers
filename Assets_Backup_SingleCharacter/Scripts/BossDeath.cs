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
        if (health.CurrentHealth <= 0 && !isDead)
        {
            isDead = true;

            Debug.Log("BOSS DEFEATED!");

            GameObject victoryCanvas = GameObject.Find("VictoryCanvas");

            if (victoryCanvas != null)
            {
                Transform victory = victoryCanvas.transform.Find("VictoryText");

                if (victory != null)
                    victory.gameObject.SetActive(true);
            }

            GameObject canvas = GameObject.Find("BossHealthCanvas");

            if (canvas != null)
                Destroy(canvas);

            Destroy(gameObject);
        }
    }
}