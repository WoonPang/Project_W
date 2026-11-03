using UnityEngine;

public sealed class BasicMeleeEnemySpawner2D : MonoBehaviour
{
    [SerializeField] private BasicMeleeEnemy2D enemyPrefab;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private PlayerStats player;
    [SerializeField, Min(0.1f)] private float initialSpawnInterval = 2.2f;
    [SerializeField, Min(0.1f)] private float minimumSpawnInterval = 0.65f;
    [SerializeField, Min(1f)] private float rampSeconds = 360f;
    [SerializeField, Min(1)] private int maximumAlive = 40;
    [SerializeField, Min(0.1f)] private float outsideViewMargin = 1.5f;
    [SerializeField, Min(0.1f)] private float minimumPlayerDistance = 7f;

    private float elapsedSeconds;
    private float nextSpawnIn;
    private GameSessionTime2D sessionTime;

    public float ElapsedSeconds
    {
        get
        {
            if (sessionTime == null)
                sessionTime = FindFirstObjectByType<GameSessionTime2D>();
            return sessionTime != null ? sessionTime.ElapsedSeconds : elapsedSeconds;
        }
    }
    public int MaximumAlive => maximumAlive;

    public void Configure(BasicMeleeEnemy2D prefab, Camera camera, PlayerStats target)
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
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || MonsterSpawnGate2D.IsPaused || enemyPrefab == null || worldCamera == null || player == null ||
            player.CurrentHealth <= 0f)
            return;

        if (sessionTime == null)
            elapsedSeconds += Time.deltaTime;
        nextSpawnIn -= Time.deltaTime;
        if (nextSpawnIn > 0f)
            return;

        if (FindObjectsByType<BasicMeleeEnemy2D>(FindObjectsSortMode.None).Length < maximumAlive)
            SpawnOne();

        float progress = Mathf.Clamp01(ElapsedSeconds / rampSeconds);
        nextSpawnIn = Mathf.Lerp(initialSpawnInterval, minimumSpawnInterval, progress);
    }

    public BasicMeleeEnemy2D SpawnOne()
    {
        if (enemyPrefab == null || worldCamera == null || player == null)
            return null;
        Vector2 position = PositionOutsideView(worldCamera, player.transform.position,
            outsideViewMargin, minimumPlayerDistance, Random.Range(0, 4), Random.value);
        return Instantiate(enemyPrefab, position, Quaternion.identity);
    }

    public static Vector2 PositionOutsideView(Camera camera, Vector2 playerPosition,
        float margin, float minimumDistance, int edge, float axis01)
    {
        float halfHeight = camera.orthographicSize;
        float halfWidth = halfHeight * camera.aspect;
        Vector2 center = camera.transform.position;
        float axis = Mathf.Lerp(-1f, 1f, Mathf.Clamp01(axis01));
        Vector2 position;
        Vector2 outward;

        switch (edge % 4)
        {
            case 0:
                position = center + new Vector2(-halfWidth - margin, axis * halfHeight);
                outward = Vector2.left;
                break;
            case 1:
                position = center + new Vector2(halfWidth + margin, axis * halfHeight);
                outward = Vector2.right;
                break;
            case 2:
                position = center + new Vector2(axis * halfWidth, halfHeight + margin);
                outward = Vector2.up;
                break;
            default:
                position = center + new Vector2(axis * halfWidth, -halfHeight - margin);
                outward = Vector2.down;
                break;
        }

        // Moving only farther outward keeps the spawn outside the camera at every zoom level.
        for (int attempt = 0; attempt < 2 &&
            Vector2.Distance(position, playerPosition) < minimumDistance; attempt++)
            position += outward * (minimumDistance - Vector2.Distance(position, playerPosition) + 0.01f);
        return position;
    }
}
