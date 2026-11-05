using UnityEngine;
using TMPro;
using System.Collections;

public class RatingPopup : MonoBehaviour
{
    public TMP_Text ratingText;

    [Header("Animation")]
    public float showDuration = 0.7f;
    public float fadeDuration = 0.3f;
    public float maxScale = 1.25f;

    private Coroutine animationCoroutine;

    public void Show(string rating, Color color)
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimateRating(rating, color));
    }

    private IEnumerator AnimateRating(string rating, Color color)
    {
        ratingText.text = rating;
        ratingText.color = color;

        transform.localScale = Vector3.zero;

        Color startColor = color;
        startColor.a = 1f;
        ratingText.color = startColor;

        float timer = 0f;

        while (timer < 0.15f)
        {
            timer += Time.deltaTime;

            float progress = timer / 0.15f;
            transform.localScale = Vector3.Lerp(
                Vector3.zero,
                Vector3.one * maxScale,
                progress
            );

            yield return null;
        }

        transform.localScale = Vector3.one;

        yield return new WaitForSeconds(showDuration);

        timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            Color currentColor = ratingText.color;
            currentColor.a = 1f - timer / fadeDuration;
            ratingText.color = currentColor;

            yield return null;
        }

        ratingText.text = "";
        transform.localScale = Vector3.one;
    }
}