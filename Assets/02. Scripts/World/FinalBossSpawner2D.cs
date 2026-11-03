using UnityEngine;

public sealed class FinalBossSpawner2D : MonoBehaviour
{
    [SerializeField] private FinalBoss2D bossPrefab;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private PlayerStats player;
    [SerializeField, Min(0f)] private float spawnAfterSeconds = 900f;
    [SerializeField, Min(0.1f)] private float outsideViewMargin = 2.5f;
    [SerializeField, Min(0.1f)] private float minimumPlayerDistance = 9f;

    private GameSessionTime2D sessionTime;
    private bool spawned;

    public float SpawnAfterSeconds => spawnAfterSeconds;
    public bool HasSpawned => spawned;

    public void Configure(FinalBoss2D prefab, Camera camera, PlayerStats target)
    {
        bossPrefab = prefab;
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
        if (Time.timeScale <= 0f || MonsterSpawnGate2D.IsPaused || spawned || bossPrefab == null || worldCamera == null ||
            player == null || player.CurrentHealth <= 0f)
            return;
        if (sessionTime == null)
            sessionTime = FindFirstObjectByType<GameSessionTime2D>();
        if (sessionTime == null || sessionTime.ElapsedSeconds < spawnAfterSeconds)
            return;

        Vector2 position = BasicMeleeEnemySpawner2D.PositionOutsideView(worldCamera,
            player.transform.position, outsideViewMargin, minimumPlayerDistance,
            Random.Range(0, 4), Random.value);
        Instantiate(bossPrefab, position, Quaternion.identity);
        spawned = true;
    }
}
