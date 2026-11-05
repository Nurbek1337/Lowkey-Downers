
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ReceptorController : MonoBehaviour
{
    public int laneIndex;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color pressedColor = Color.yellow;

    [Header("Animation")]
    public float pressedScale = 1.15f;
    public float animationSpeed = 12f;

    private Image receptorImage;
    private Vector3 normalScale;

    private void Start()
    {
        receptorImage = GetComponent<Image>();
        normalScale = transform.localScale;

        if (receptorImage == null)
        {
            Debug.LogError("Receptor Image not found!");
            enabled = false;
            return;
        }

        receptorImage.color = normalColor;
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        bool isPressed = false;

        switch (laneIndex)
        {
            case 0:
                isPressed = Keyboard.current.dKey.isPressed;
                break;
            case 1:
                isPressed = Keyboard.current.fKey.isPressed;
                break;
            case 2:
                isPressed = Keyboard.current.spaceKey.isPressed;
                break;
            case 3:
                isPressed = Keyboard.current.jKey.isPressed;
                break;
            case 4:
                isPressed = Keyboard.current.kKey.isPressed;
                break;
        }

        Color targetColor = isPressed ? pressedColor : normalColor;

        Vector3 targetScale = isPressed
            ? normalScale * pressedScale
            : normalScale;

        receptorImage.color = Color.Lerp(
            receptorImage.color,
            targetColor,
            animationSpeed * Time.deltaTime
        );

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            animationSpeed * Time.deltaTime
        );
    }
}
