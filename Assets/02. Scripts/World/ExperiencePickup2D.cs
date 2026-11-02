using UnityEngine;

public sealed class ExperiencePickup2D : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float experienceValue = 10f;
    [SerializeField, Min(0.1f)] private float attractionRadius = 2.75f;
    [SerializeField, Min(0.05f)] private float collectionRadius = 0.4f;
    [SerializeField, Min(0.1f)] private float initialPullSpeed = 3f;
    [SerializeField, Min(0f)] private float pullAcceleration = 18f;

    private PlayerProgress target;
    private float currentPullSpeed;

    public float ExperienceValue => experienceValue;

    public void Initialize(PlayerProgress playerProgress)
    {
        target = playerProgress;
        currentPullSpeed = initialPullSpeed;
    }

    public void Initialize(PlayerProgress playerProgress, float value)
    {
        experienceValue = Mathf.Max(0.1f, value);
        Initialize(playerProgress);
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        if (target == null)
            target = FindFirstObjectByType<PlayerProgress>();
        if (target == null)
            return;

        Vector2 playerPosition = target.transform.position;
        Vector2 pickupPosition = transform.position;
        float distance = Vector2.Distance(pickupPosition, playerPosition);
        if (distance > attractionRadius)
            return;

        if (distance <= collectionRadius)
        {
            Collect();
            return;
        }

        currentPullSpeed += pullAcceleration * Time.deltaTime;
        transform.position = Vector2.MoveTowards(pickupPosition, playerPosition,
            currentPullSpeed * Time.deltaTime);
        if (Vector2.Distance(transform.position, playerPosition) <= collectionRadius)
            Collect();
    }

    private void Collect()
    {
        if (target == null)
            return;

        target.AddExperience(experienceValue);
        Destroy(gameObject);
    }
}
