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
    private bool baseCaptured;
    private float characterMultiplier = 1f;
    private float temporaryMultiplier = 1f;

    void Awake()
    {
        EnsureBase();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        dash = GetComponent<PlayerDash>();
    }

    // База запоминается ровно один раз (до первого применения множителей) —
    // раньше она перезаписывалась в Start уже умноженным значением, из-за чего скорость
    // применялась дважды и зависела от порядка Awake/Start.
    private void EnsureBase()
    {
        if (baseCaptured) return;
        baseMoveSpeed = moveSpeed;
        baseCaptured = true;
    }

    public float CurrentSpeed => baseMoveSpeed * characterMultiplier * temporaryMultiplier;

    public void SetCharacterSpeedMultiplier(float multiplier)
    {
        EnsureBase();
        characterMultiplier = Mathf.Max(0.25f, multiplier);
        moveSpeed = CurrentSpeed;
    }

    public void SetTemporarySpeedMultiplier(float multiplier)
    {
        EnsureBase();
        temporaryMultiplier = Mathf.Max(0.1f, multiplier);
        moveSpeed = CurrentSpeed;
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;
        if (dash != null && dash.IsDashing) return;
        if (rb == null || groundCheck == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius);

        isGrounded = false;
        foreach (Collider2D hit in hits)
        {
            if (hit.transform != transform && !hit.isTrigger)
            {
                isGrounded = true;
                break;
            }
        }

        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space)) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0 && coyoteCounter > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0;
            coyoteCounter = 0;
        }

        if (Input.GetKeyUp(KeyCode.Space) && rb.linearVelocity.y > 0)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        if (dash != null && dash.IsDashing) return;

        float move = Time.timeScale <= 0f ? 0f : Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(move * moveSpeed, rb.linearVelocity.y);
    }
}
