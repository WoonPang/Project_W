using UnityEngine;
using UnityEngine.UI;

public sealed class ExperiencePickupSpawner : MonoBehaviour
{
    [SerializeField] private Button spawnButton;
    [SerializeField] private ExperiencePickup2D pickupPrefab;
    [SerializeField, Min(1)] private int pickupCount = 10;
    [SerializeField, Min(0.1f)] private float minimumSpawnRadius = 3f;
    [SerializeField, Min(0.1f)] private float maximumSpawnRadius = 4.2f;

    private PlayerProgress playerProgress;

    public void Configure(Button button, ExperiencePickup2D prefab)
    {
        spawnButton = button;
        pickupPrefab = prefab;
    }

    private void Awake()
    {
        playerProgress = FindFirstObjectByType<PlayerProgress>();
        if (spawnButton != null)
            spawnButton.onClick.AddListener(SpawnPickups);
    }

    public void SpawnPickups()
    {
        if (Time.timeScale <= 0f || pickupPrefab == null)
            return;

        if (playerProgress == null)
            playerProgress = FindFirstObjectByType<PlayerProgress>();
        if (playerProgress == null)
            return;

        Vector3 center = playerProgress.transform.position;
        float angleOffset = Random.Range(0f, Mathf.PI * 2f);
        for (int index = 0; index < pickupCount; index++)
        {
            float angle = angleOffset + index * Mathf.PI * 2f / pickupCount;
            float distance = Random.Range(minimumSpawnRadius, maximumSpawnRadius);
            Vector3 position = center + new Vector3(
                Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, 0f);
            ExperiencePickup2D pickup = Instantiate(pickupPrefab, position, Quaternion.identity);
            pickup.Initialize(playerProgress);
        }
    }

    private void OnDestroy()
    {
        if (spawnButton != null)
            spawnButton.onClick.RemoveListener(SpawnPickups);
    }
}
