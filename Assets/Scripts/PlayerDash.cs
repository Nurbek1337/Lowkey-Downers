using UnityEngine;
using System.Collections;

public class PlayerDash : MonoBehaviour
{
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    private Rigidbody2D rb;
    private bool canDash = true;

    public bool IsDashing { get; private set; }
    public float CooldownRemaining { get; private set; }

    private float baseDashSpeed;
    private float baseDashCooldown;
    private bool baseCaptured;

    void Awake() { EnsureBase(); }
    void Start() { rb = GetComponent<Rigidbody2D>(); }

    // База фиксируется один раз — см. комментарий в SideViewMovement.
    private void EnsureBase()
    {
        if (baseCaptured) return;
        baseDashSpeed = dashSpeed;
        baseDashCooldown = dashCooldown;
        baseCaptured = true;
    }

    public void SetAgilityMultiplier(float multiplier)
    {
        EnsureBase();
        float m = Mathf.Max(0.25f, multiplier);
        dashSpeed = baseDashSpeed * m;
        dashCooldown = baseDashCooldown / m;
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;
        if (CooldownRemaining > 0f) CooldownRemaining = Mathf.Max(0f, CooldownRemaining - Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && rb != null)
            StartCoroutine(Dash());
    }

    IEnumerator Dash()
    {
        canDash = false;
        IsDashing = true;

        float direction = Input.GetAxisRaw("Horizontal");
        if (direction == 0) direction = transform.localScale.x < 0f ? -1f : 1f;
        direction = Mathf.Sign(direction);

        float oldGravity = rb.gravityScale;
        rb.gravityScale = 0;
        rb.linearVelocity = new Vector2(direction * dashSpeed, 0);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = oldGravity;
        rb.linearVelocity = Vector2.zero;
        IsDashing = false;

        CooldownRemaining = dashCooldown;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void OnDisable()
    {
        // Если игрока отключили во время рывка (смерть), возвращаем гравитацию и состояние.
        if (IsDashing && rb != null) rb.gravityScale = 1f;
        IsDashing = false;
        canDash = true;
    }
}
