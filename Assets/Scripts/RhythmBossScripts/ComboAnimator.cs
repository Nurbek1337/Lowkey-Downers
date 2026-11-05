using UnityEngine;
using System.Collections;

public class ComboAnimator : MonoBehaviour
{
    public float animationDuration = 0.15f;
    public float maxScale = 1.3f;

    private Coroutine animationCoroutine;

    public void Play()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = originalScale * maxScale;

        float timer = 0f;

        while (timer < animationDuration)
        {
            timer += Time.deltaTime;

            transform.localScale = Vector3.Lerp(
                originalScale,
                targetScale,
                timer / animationDuration
            );

            yield return null;
        }

        timer = 0f;

        while (timer < animationDuration)
        {
            timer += Time.deltaTime;

            transform.localScale = Vector3.Lerp(
                targetScale,
                originalScale,
                timer / animationDuration
            );

            yield return null;
        }

        transform.localScale = originalScale;
    }
}