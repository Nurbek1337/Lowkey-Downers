using UnityEngine;

public class PlayerVisualAnimator : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D body;
    private PlayerDash dash;
    private ParrySystem parry;

    private string currentState = "";
    private string lockedState = "";
    private float lockUntil;
    private bool wasDashing;

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponentInParent<Rigidbody2D>();
        dash = GetComponentInParent<PlayerDash>();
        parry = GetComponentInParent<ParrySystem>();
    }

    void Update()
    {
        if (animator == null || spriteRenderer == null || body == null)
            return;

        animator.speed = 1f;

        float x = Input.GetAxisRaw("Horizontal");

        if (x < -0.01f)
            spriteRenderer.flipX = true;
        else if (x > 0.01f)
            spriteRenderer.flipX = false;

        // Парирование
        if (parry != null && parry.ConsumeAnimationRequest())
            PlayAction("Parry");

        // Бросок кинжала
        if (Input.GetMouseButtonDown(0))
            PlayAction("Dagger_Throw");

        // Рывок
        bool isDashing = dash != null && dash.IsDashing;

        if (isDashing && !wasDashing)
            PlayAction("Dash");

        wasDashing = isDashing;

        string state;

        if (Time.time < lockUntil)
        {
            state = lockedState;
        }
        else if (body.linearVelocity.y > 0.2f)
        {
            state = "Jump";
        }
        else if (Mathf.Abs(x) > 0.01f)
        {
            state = "Walk";
        }
        else
        {
            state = "Idle";
        }

        PlayState(state);
    }

    void PlayAction(string state)
    {
        float length = GetClipLength(state);

        if (length <= 0f)
            return;

        lockedState = state;
        lockUntil = Time.time + length;
        PlayState(state);
    }

    void PlayState(string state)
    {
        if (currentState == state)
            return;

        if (!animator.HasState(0, Animator.StringToHash(state)))
        {
            Debug.LogWarning("Нет состояния анимации: " + state);
            return;
        }

        animator.Play(state, 0, 0f);
        currentState = state;
    }

    float GetClipLength(string state)
    {
        if (animator.runtimeAnimatorController == null)
            return 0f;

        string clipName = "Player_" + state;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
                return clip.length;
        }

        Debug.LogWarning("Не найден клип: " + clipName);
        return 0f;
    }
}
