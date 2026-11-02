using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class PlayerMovement2D : MonoBehaviour
{
    [FormerlySerializedAs("moveSpeed"), SerializeField, Min(0f)]
    private float worldUnitsPerMoveSpeed = 5f;
    [SerializeField, Min(0.1f)] private float dashDistance = 2.5f;
    [SerializeField, Min(0.05f)] private float dashDuration = 0.18f;
    [SerializeField, Min(0f)] private float dashCooldownSeconds = 10f;

    private Rigidbody2D body;
    private PlayerStats stats;
    private Vector2 moveInput;
    private Vector2 dashDirection;
    private Vector2 lastMoveDirection;
    private float dashDistanceRemaining;
    private float dashReadyAt;
    private int dashCooldownReductionLevel;

    public float DashDistance => dashDistance;
    public float DashDuration => dashDuration;
    public float DashCooldownSeconds => Mathf.Max(0f, dashCooldownSeconds - dashCooldownReductionLevel);
    public float DashCooldownRemaining => Mathf.Max(0f, dashReadyAt - Time.time);
    public bool IsDashReady => dashDistanceRemaining <= 0f && DashCooldownRemaining <= 0f;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
        {
            moveInput = Vector2.zero;
            body.linearVelocity = Vector2.zero;
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        float horizontal = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
            - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
        float vertical = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
            - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);

        moveInput = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        if (moveInput.sqrMagnitude > 0f)
            lastMoveDirection = moveInput.normalized;

        if (keyboard.spaceKey.wasPressedThisFrame && IsDashReady)
            StartDashInMoveDirection();
    }

    private void FixedUpdate()
    {
        if (dashDistanceRemaining > 0f)
        {
            float dashSpeed = dashDistance / Mathf.Max(0.05f, dashDuration);
            float step = Mathf.Min(dashDistanceRemaining, dashSpeed * Time.fixedDeltaTime);
            body.linearVelocity = Vector2.zero;
            body.MovePosition(body.position + dashDirection * step);
            dashDistanceRemaining -= step;
            if (dashDistanceRemaining <= 0f)
            {
                dashDistanceRemaining = 0f;
                dashReadyAt = Time.time + DashCooldownSeconds;
            }
            return;
        }

        float moveSpeedStat = stats != null ? stats.MoveSpeed : 1f;
        body.linearVelocity = moveInput * worldUnitsPerMoveSpeed * moveSpeedStat;
    }

    private void StartDashInMoveDirection()
    {
        if (lastMoveDirection.sqrMagnitude < 0.001f)
            return;

        dashDirection = lastMoveDirection;
        dashDistanceRemaining = dashDistance;
        body.linearVelocity = Vector2.zero;
    }

    public void SetDashCooldownReduction(int level)
    {
        int clamped = Mathf.Clamp(level, 0, 5);
        int difference = clamped - dashCooldownReductionLevel;
        dashCooldownReductionLevel = clamped;
        if (difference > 0 && dashReadyAt > Time.time)
            dashReadyAt = Mathf.Max(Time.time, dashReadyAt - difference);
    }

    private void OnDisable()
    {
        dashDistanceRemaining = 0f;
        if (body != null)
            body.linearVelocity = Vector2.zero;
    }
}
