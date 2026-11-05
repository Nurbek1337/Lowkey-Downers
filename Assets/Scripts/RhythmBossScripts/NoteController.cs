
using UnityEngine;
using UnityEngine.InputSystem;

public class NoteController : MonoBehaviour
{
    [Header("Note Settings")]
    public float speed = 300f;
    public float hitTolerance = 35f;

    [Header("Lane Settings")]
    public int laneIndex;

    public RhythmBattleManager battleManager;

    private RectTransform rectTransform;
    private RectTransform laneRect;
    private RectTransform hitLineRect;

    private float hitLinePositionY;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        // Получаем дорожку, внутри которой находится нота
        laneRect = transform.parent.GetComponent<RectTransform>();

        // Находим HitLine в сцене
        GameObject hitLineObject = GameObject.Find("HitLine");

        if (hitLineObject != null)
        {
            hitLineRect = hitLineObject.GetComponent<RectTransform>();
        }

        if (hitLineRect == null || laneRect == null)
        {
            Debug.LogError("NoteController: не найдена HitLine или Lane!");
            enabled = false;
            return;
        }

        if (battleManager == null)
        {
            Debug.LogError("NoteController: BattleManager не назначен!");
            enabled = false;
            return;
        }

        UpdateHitLinePosition();
    }

    private void Update()
    {
        // Движение ноты вниз
        Vector2 position = rectTransform.anchoredPosition;
        position.y -= speed * Time.deltaTime;

        rectTransform.anchoredPosition = position;

        // Проверяем нажатие клавиши
        if (Keyboard.current != null && IsCorrectKeyPressed())
        {
            if (Mathf.Abs(position.y - hitLinePositionY) <= hitTolerance)
            {
                float distance = Mathf.Abs(position.y - hitLinePositionY);

                if (Keyboard.current != null &&
                    IsCorrectKeyPressed() &&
                    distance <= hitTolerance)
                {
                    battleManager.HitNote(distance);
                    Destroy(gameObject);
                    return;
                }
                Destroy(gameObject);
                return;
            }
        }

        // Проверяем, прошла ли нота HitLine
        if (position.y < hitLinePositionY - hitTolerance)
        {
            battleManager.MissNote();
            Destroy(gameObject);
        }
    }

    private void UpdateHitLinePosition()
    {
        // Переводим мировую позицию HitLine
        // в локальные координаты дорожки
        Vector3 localPosition =
            laneRect.InverseTransformPoint(hitLineRect.position);

        hitLinePositionY = localPosition.y;
    }

    private bool IsCorrectKeyPressed()
    {
        switch (laneIndex)
        {
            case 0:
                return Keyboard.current.dKey.wasPressedThisFrame;

            case 1:
                return Keyboard.current.fKey.wasPressedThisFrame;

            case 2:
                return Keyboard.current.spaceKey.wasPressedThisFrame;

            case 3:
                return Keyboard.current.jKey.wasPressedThisFrame;

            case 4:
                return Keyboard.current.kKey.wasPressedThisFrame;

            default:
                return false;
        }
    }
}
