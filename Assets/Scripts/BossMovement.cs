
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossMovement : MonoBehaviour
{
    public Transform player;

    [Header("Границы арены")]
    public float minX = -6f;
    public float maxX = 6f;
    public float minY = -1f;
    public float maxY = 2f;

    [Header("Движение")]
    public float moveSpeed = 3f;
    public float smoothTime = 0.8f;

    [Header("Позиция относительно игрока")]
    public float minDistance = 3f;
    public float maxDistance = 5f;
    public float changePositionTime = 3f;

    [Header("Парение")]
    public float hoverHeight = 0.4f;
    public float hoverSpeed = 2f;

    private Rigidbody2D rb;
    private Vector2 velocity;
    private Vector2 targetPosition;

    private float positionTimer;
    private float hoverOffset;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        hoverOffset = Random.Range(0f, 10f);
        ChooseNewPosition();
    }

    void Update()
    {
        if (player == null) return;

        positionTimer -= Time.deltaTime;

        if (positionTimer <= 0f)
        {
            ChooseNewPosition();
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;

        float hover = Mathf.Sin(
            Time.time * hoverSpeed + hoverOffset
        ) * hoverHeight;

        Vector2 destination = new Vector2(
            targetPosition.x,
            targetPosition.y + hover
        );

        Vector2 nextPosition = Vector2.SmoothDamp(
            rb.position,
            destination,
            ref velocity,
            smoothTime,
            moveSpeed,
            Time.fixedDeltaTime
        );

        nextPosition.x = Mathf.Clamp(nextPosition.x, minX, maxX);
        nextPosition.y = Mathf.Clamp(nextPosition.y, minY, maxY);

        rb.MovePosition(nextPosition);
    }

    void ChooseNewPosition()
    {
        if (player == null) return;

        float distance = Random.Range(minDistance, maxDistance);
        float side = Random.value < 0.5f ? -1f : 1f;

        float randomX = Random.Range(-1.5f, 1.5f);
        float randomY = Random.Range(-1f, 1f);

        targetPosition = new Vector2(
            player.position.x + side * distance + randomX,
            player.position.y + randomY
        );

        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);

        positionTimer = Random.Range(
            changePositionTime * 0.6f,
            changePositionTime * 1.5f
        );
    }
}
