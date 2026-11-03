using UnityEngine;

public sealed class MidBossSpawner2D : MonoBehaviour
{
    [SerializeField] private MidBoss2D bossPrefab;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private PlayerStats player;
    [SerializeField, Min(0f)] private float firstSpawnAfterSeconds = 300f;
    [SerializeField, Range(1, 2)] private int spawnCount = 2;
    [SerializeField, Min(0f)] private float secondSpawnAfterSeconds = 600f;
    [SerializeField, Range(1, 5)] private int secondSpawnCount = 3;
    [SerializeField, Min(0.1f)] private float bossSeparation = 3.5f;
    [SerializeField, Min(0.1f)] private float outsideViewMargin = 2f;
    [SerializeField, Min(0.1f)] private float minimumPlayerDistance = 8f;

    private GameSessionTime2D sessionTime;
    private bool spawned;
    private bool secondWaveSpawned;

    public float FirstSpawnAfterSeconds => firstSpawnAfterSeconds;
    public int SpawnCount => spawnCount;
    public float SecondSpawnAfterSeconds => secondSpawnAfterSeconds;
    public int SecondSpawnCount => secondSpawnCount;
    public float BossSeparation => bossSeparation;
    public bool HasSpawned => spawned;
    public bool HasSecondWaveSpawned => secondWaveSpawned;

    public void Configure(MidBoss2D prefab, Camera camera, PlayerStats target)
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
        if (Time.timeScale <= 0f || MonsterSpawnGate2D.IsPaused || bossPrefab == null || worldCamera == null ||
            player == null || player.CurrentHealth <= 0f)
            return;
        if (sessionTime == null)
            sessionTime = FindFirstObjectByType<GameSessionTime2D>();
        if (sessionTime == null)
            return;
        if (!spawned && sessionTime.ElapsedSeconds >= firstSpawnAfterSeconds)
        {
            SpawnWave(spawnCount);
            spawned = true;
        }
        if (!secondWaveSpawned && sessionTime.ElapsedSeconds >= secondSpawnAfterSeconds)
        {
            SpawnWave(secondSpawnCount);
            secondWaveSpawned = true;
        }
    }

    private void SpawnWave(int count)
    {
        int firstSide = Random.Range(0, 4);
        float sideHalfExtent = firstSide < 2 ? worldCamera.orthographicSize :
            worldCamera.orthographicSize * worldCamera.aspect;
        float separation01 = Mathf.Min(0.5f,
            bossSeparation / (2f * Mathf.Max(0.1f, sideHalfExtent)));
        float groupCenter01 = Random.Range(0.4f, 0.6f);
        for (int index = 0; index < count; index++)
        {
            float sidePosition01 = groupCenter01 +
                (index - (count - 1) * 0.5f) * separation01;
            Vector2 position = BasicMeleeEnemySpawner2D.PositionOutsideView(worldCamera,
                player.transform.position, outsideViewMargin, minimumPlayerDistance,
                firstSide, sidePosition01);
            Instantiate(bossPrefab, position, Quaternion.identity);
        }
    }
}
