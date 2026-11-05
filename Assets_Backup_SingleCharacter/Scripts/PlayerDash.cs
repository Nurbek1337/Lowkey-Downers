
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
    private float baseDashSpeed;
    private float baseDashCooldown;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        baseDashSpeed = dashSpeed;
        baseDashCooldown = dashCooldown;
    }

    public void SetAgilityMultiplier(float multiplier)
    {
        if (baseDashSpeed <= 0f) baseDashSpeed = dashSpeed;
        if (baseDashCooldown <= 0f) baseDashCooldown = dashCooldown;
        float safeMultiplier = Mathf.Max(0.25f, multiplier);
        dashSpeed = baseDashSpeed * safeMultiplier;
        dashCooldown = baseDashCooldown / safeMultiplier;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartCoroutine(Dash());
        }
    }

    IEnumerator Dash()
    {
        canDash = false;
        IsDashing = true;

        float direction = Input.GetAxisRaw("Horizontal");

        if (direction == 0)
            direction = 1;

        float oldGravity = rb.gravityScale;
        rb.gravityScale = 0;

        rb.linearVelocity = new Vector2(direction * dashSpeed, 0);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = oldGravity;
        rb.linearVelocity = Vector2.zero;

        IsDashing = false;

        yield return new WaitForSeconds(dashCooldown);

        canDash = true;
    }
}
