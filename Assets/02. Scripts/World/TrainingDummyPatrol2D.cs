using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(EnemyStatus2D), typeof(TrainingDummy2D))]
public sealed class TrainingDummyPatrol2D : MonoBehaviour
{
    [SerializeField, Min(0f)] private float speed = 1f;
    [SerializeField, Min(0.1f)] private float travelDistance = 0.8f;

    private Rigidbody2D body;
    private EnemyStatus2D status;
    private TrainingDummy2D dummy;
    private float startX;
    private float direction = -1f;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        status = GetComponent<EnemyStatus2D>();
        dummy = GetComponent<TrainingDummy2D>();
        startX = body.position.x;
    }

    private void FixedUpdate()
    {
        if (Time.timeScale <= 0f || !dummy.IsAlive)
            return;

        float step = speed * status.MovementMultiplier * Time.fixedDeltaTime;
        if (step <= 0f)
            return;
        float nextX = body.position.x + direction * step;
        if (nextX < startX - travelDistance || nextX > startX + travelDistance)
        {
            direction *= -1f;
            nextX = Mathf.Clamp(nextX, startX - travelDistance, startX + travelDistance);
        }
        body.MovePosition(new Vector2(nextX, body.position.y));
    }
}
