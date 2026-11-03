using UnityEngine;

public sealed class ChargingRangedEnemySpawner2D : MonoBehaviour
{
    [SerializeField] private ChargingRangedEnemy2D enemyPrefab;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private PlayerStats player;
    [SerializeField, Min(0f)] private float firstSpawnAfterSeconds = 540f;
    [SerializeField, Min(0.1f)] private float initialSpawnInterval = 12f;
    [SerializeField, Min(0.1f)] private float minimumSpawnInterval = 6f;
    [SerializeField, Min(1f)] private float rampSeconds = 360f;
    [SerializeField, Min(1)] private int maximumAlive = 6;
    [SerializeField, Min(0.1f)] private float outsideViewMargin = 1.5f;
    [SerializeField, Min(0.1f)] private float minimumPlayerDistance = 7f;

    private GameSessionTime2D sessionTime;
    private float fallbackElapsedSeconds;
    private float nextSpawnIn;

    public float FirstSpawnAfterSeconds => firstSpawnAfterSeconds;
    public float ElapsedSeconds
    {
        get
        {
            if (sessionTime == null)
                sessionTime = FindFirstObjectByType<GameSessionTime2D>();
            return sessionTime != null ? sessionTime.ElapsedSeconds : fallbackElapsedSeconds;
        }
    }

    public void Configure(ChargingRangedEnemy2D prefab, Camera camera, PlayerStats target)
    {
        enemyPrefab = prefab;
        worldCamera = camera;
        player = target;
    }

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
        if (player == null)
            player = FindFirstObjectByType<PlayerStats>();
        sessionTime = FindFirstObjectByType<GameSessionTime2D>();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || MonsterSpawnGate2D.IsPaused || enemyPrefab == null || worldCamera == null ||
            player == null || player.CurrentHealth <= 0f)
            return;
        if (sessionTime == null)
            fallbackElapsedSeconds += Time.deltaTime;
        float elapsed = ElapsedSeconds;
        if (elapsed < firstSpawnAfterSeconds)
            return;
        nextSpawnIn -= Time.deltaTime;
        if (nextSpawnIn > 0f)
            return;
        if (FindObjectsByType<ChargingRangedEnemy2D>(FindObjectsSortMode.None).Length < maximumAlive)
        {
            Vector2 position = BasicMeleeEnemySpawner2D.PositionOutsideView(worldCamera,
                player.transform.position, outsideViewMargin, minimumPlayerDistance,
                Random.Range(0, 4), Random.value);
            Instantiate(enemyPrefab, position, Quaternion.identity);
        }
        float progress = Mathf.Clamp01((elapsed - firstSpawnAfterSeconds) / rampSeconds);
        nextSpawnIn = Mathf.Lerp(initialSpawnInterval, minimumSpawnInterval, progress);
    }
}
