
using UnityEngine;
using TMPro;

public class RhythmBattleManager : MonoBehaviour
{
    [Header("Health Systems")]
    public HealthSystem playerHealth;
    public HealthSystem bossHealth;

    [Header("Battle Settings")]
    public int damageToBoss = 25;
    public int damageToPlayer = 10;

    [Header("Score Settings")]
    public int score = 0;
    public int combo = 0;
    public ComboAnimator comboAnimator;

    public int sickScore = 350;
    public int goodScore = 200;
    public int badScore = 100;

    [Header("UI")]
    public TMP_Text ratingText;

    [Header("Score UI")]
    public TMP_Text scoreText;
    public TMP_Text comboText;

    private bool battleEnded = false;

    private void Start()
    {
        Debug.Log("Ритм-бой начался!");

        if (playerHealth != null)
            playerHealth.OnDeath += PlayerDefeated;

        if (bossHealth != null)
            bossHealth.OnDeath += BossDefeated;

        if (ratingText != null)
            ratingText.text = "";
        UpdateScoreUI();
    }

    public void HitNote()
    {
        HitNote(0f);
    }

    public void HitNote(float distance)
    {
        if (battleEnded)
            return;

        string rating;
        int points;
        Color ratingColor;

        if (distance <= 10f)
        {
            rating = "SICK!";
            points = sickScore;
            ratingColor = Color.cyan;
        }
        else if (distance <= 22f)
        {
            rating = "GOOD!";
            points = goodScore;
            ratingColor = Color.green;
        }
        else
        {
            rating = "BAD!";
            points = badScore;
            ratingColor = new Color(1f, 0.5f, 0f);
        }

        score += points;
        combo++;

        UpdateScoreUI();

        ComboAnimator scoreAnimator = scoreText != null
            ? scoreText.GetComponent<ComboAnimator>()
            : null;

        if (scoreAnimator != null)
            scoreAnimator.Play();

        if (comboAnimator != null)
            comboAnimator.Play();

        ShowRating(rating, ratingColor);

        if (bossHealth != null)
            bossHealth.TakeDamage(damageToBoss);

        Debug.Log(
            rating + " | +" + points +
            " Score: " + score +
            " | Combo: " + combo
        );
    }

    public void MissNote()
    {
        if (battleEnded)
            return;

        combo = 0;
        UpdateScoreUI();

        ShowRating("MISS!", Color.red);

        if (playerHealth != null)
            playerHealth.TakeDamage(damageToPlayer);

        Debug.Log(
            "MISS! | Score: " + score +
            " | Combo сброшен."
        );
    }

    private void ShowRating(string rating, Color color)
    {
        if (ratingText == null) return;

        RatingPopup popup = ratingText.GetComponent<RatingPopup>();

        if (popup != null)
        {
            popup.Show(rating, color);
        }
        else
        {
            ratingText.text = rating;
            ratingText.color = color;
        }
    }

    private void PlayerDefeated()
    {
        battleEnded = true;
        Debug.Log("Поражение! Игрок проиграл.");
    }

    private void BossDefeated()
    {
        battleEnded = true;
        Debug.Log("Победа! Босс побеждён.");
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnDeath -= PlayerDefeated;

        if (bossHealth != null)
            bossHealth.OnDeath -= BossDefeated;
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "SCORE: " + score.ToString("D6");

        if (comboText != null)
            comboText.text = "COMBO: " + combo;
    }
}
