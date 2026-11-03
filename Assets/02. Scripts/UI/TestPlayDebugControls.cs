using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class TestPlayDebugControls : MonoBehaviour
{
    [SerializeField] private Button invincibleButton;
    [SerializeField] private Button mobMenuButton;
    [SerializeField] private Button midBossButton;
    [SerializeField] private Button finalBossButton;
    [SerializeField] private Button spawnToggleButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private GameObject mobMenu;
    [SerializeField] private Button[] mobButtons;
    [SerializeField] private CombatTarget2D[] mobPrefabs;
    [SerializeField] private MidBoss2D midBossPrefab;
    [SerializeField] private FinalBoss2D finalBossPrefab;
    [SerializeField] private PlayerStats player;
    [SerializeField] private Camera worldCamera;

    private UnityAction[] mobActions;

    public void Configure(Button invincible, Button menu, Button midBoss, Button finalBoss,
        Button spawnToggle, Button clear, GameObject menuRoot, Button[] choices,
        CombatTarget2D[] regularPrefabs, MidBoss2D midPrefab, FinalBoss2D finalPrefab,
        PlayerStats target, Camera camera)
    {
        invincibleButton = invincible;
        mobMenuButton = menu;
        midBossButton = midBoss;
        finalBossButton = finalBoss;
        spawnToggleButton = spawnToggle;
        clearButton = clear;
        mobMenu = menuRoot;
        mobButtons = choices;
        mobPrefabs = regularPrefabs;
        midBossPrefab = midPrefab;
        finalBossPrefab = finalPrefab;
        player = target;
        worldCamera = camera;
    }

    private void OnEnable()
    {
        MonsterSpawnGate2D.SetPaused(false);
        if (mobMenu != null)
            mobMenu.SetActive(false);
        RefreshLabels();

        if (invincibleButton != null)
            invincibleButton.onClick.AddListener(ToggleInvincibility);
        if (mobMenuButton != null)
            mobMenuButton.onClick.AddListener(ToggleMobMenu);
        if (midBossButton != null)
            midBossButton.onClick.AddListener(SpawnMidBoss);
        if (finalBossButton != null)
            finalBossButton.onClick.AddListener(SpawnFinalBoss);
        if (spawnToggleButton != null)
            spawnToggleButton.onClick.AddListener(ToggleSpawnPause);
        if (clearButton != null)
            clearButton.onClick.AddListener(ClearMonsters);

        if (mobButtons == null)
            return;
        mobActions = new UnityAction[mobButtons.Length];
        for (int index = 0; index < mobButtons.Length; index++)
        {
            if (mobButtons[index] == null)
                continue;
            int choice = index;
            mobActions[index] = () => SpawnMob(choice);
            mobButtons[index].onClick.AddListener(mobActions[index]);
        }
    }

    private void OnDisable()
    {
        if (invincibleButton != null)
            invincibleButton.onClick.RemoveListener(ToggleInvincibility);
        if (mobMenuButton != null)
            mobMenuButton.onClick.RemoveListener(ToggleMobMenu);
        if (midBossButton != null)
            midBossButton.onClick.RemoveListener(SpawnMidBoss);
        if (finalBossButton != null)
            finalBossButton.onClick.RemoveListener(SpawnFinalBoss);
        if (spawnToggleButton != null)
            spawnToggleButton.onClick.RemoveListener(ToggleSpawnPause);
        if (clearButton != null)
            clearButton.onClick.RemoveListener(ClearMonsters);
        if (mobButtons != null && mobActions != null)
        {
            for (int index = 0; index < Mathf.Min(mobButtons.Length, mobActions.Length); index++)
                if (mobButtons[index] != null && mobActions[index] != null)
                    mobButtons[index].onClick.RemoveListener(mobActions[index]);
        }
        if (player != null)
            player.SetDebugInvincible(false);
        MonsterSpawnGate2D.SetPaused(false);
    }

    public void ToggleInvincibility()
    {
        if (Time.timeScale <= 0f || player == null)
            return;
        player.SetDebugInvincible(!player.IsDebugInvincible);
        RefreshLabels();
    }

    public void ToggleMobMenu()
    {
        if (Time.timeScale > 0f && mobMenu != null)
            mobMenu.SetActive(!mobMenu.activeSelf);
    }

    public void SpawnMob(int index)
    {
        if (mobPrefabs == null || index < 0 || index >= mobPrefabs.Length)
            return;
        Spawn(mobPrefabs[index], 7f);
        if (mobMenu != null)
            mobMenu.SetActive(false);
    }

    public void SpawnMidBoss()
    {
        Spawn(midBossPrefab, 8f);
    }

    public void SpawnFinalBoss()
    {
        Spawn(finalBossPrefab, 9f);
    }

    private void Spawn(CombatTarget2D prefab, float minimumDistance)
    {
        if (Time.timeScale <= 0f || prefab == null || player == null || worldCamera == null)
            return;
        Vector2 position = BasicMeleeEnemySpawner2D.PositionOutsideView(worldCamera,
            player.transform.position, 1.5f, minimumDistance,
            Random.Range(0, 4), Random.value);
        Instantiate(prefab.gameObject, position, Quaternion.identity);
    }

    public void ToggleSpawnPause()
    {
        if (Time.timeScale <= 0f)
            return;
        MonsterSpawnGate2D.SetPaused(!MonsterSpawnGate2D.IsPaused);
        RefreshLabels();
    }

    public void ClearMonsters()
    {
        if (Time.timeScale <= 0f)
            return;
        foreach (CombatTarget2D target in FindObjectsByType<CombatTarget2D>(FindObjectsSortMode.None))
            Destroy(target.gameObject);
    }

    private void RefreshLabels()
    {
        SetLabel(invincibleButton, player != null && player.IsDebugInvincible ? "무적: 켜짐" : "무적: 꺼짐");
        SetLabel(spawnToggleButton, MonsterSpawnGate2D.IsPaused ? "몬스터 스폰 재개" : "몬스터 스폰 정지");
    }

    private static void SetLabel(Button button, string value)
    {
        if (button != null)
        {
            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.text = value;
        }
    }
}
