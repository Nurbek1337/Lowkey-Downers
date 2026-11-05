using UnityEngine;

public class SideViewMovement : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float jumpForce = 12f;

    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;

    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.15f;

    private Rigidbody2D rb;
    private PlayerDash dash;

    private float coyoteCounter;
    private float jumpBufferCounter;

    private bool isGrounded;
    private float baseMoveSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        dash = GetComponent<PlayerDash>();
        baseMoveSpeed = moveSpeed;
    }

    public void SetCharacterSpeedMultiplier(float multiplier)
    {
        if (baseMoveSpeed <= 0f) baseMoveSpeed = moveSpeed;
        moveSpeed = baseMoveSpeed * Mathf.Max(0.25f, multiplier);
    }

    void Update()
    {
        if (dash != null && dash.IsDashing)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            groundCheck.position,
            groundCheckRadius
        );

        isGrounded = false;

        foreach (Collider2D hit in hits)
        {
            if (hit.transform != transform && !hit.isTrigger)
            {
                isGrounded = true;
                break;
            }
        }

        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0 && coyoteCounter > 0)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpForce
            );

            jumpBufferCounter = 0;
            coyoteCounter = 0;
        }

        if (Input.GetKeyUp(KeyCode.Space) && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                rb.linearVelocity.y * 0.5f
            );
        }
    }

    void FixedUpdate()
    {
        if (dash != null && dash.IsDashing)
            return;

        float move = Input.GetAxisRaw("Horizontal");

        rb.linearVelocity = new Vector2(
            move * moveSpeed,
            rb.linearVelocity.y
        );
    }
}