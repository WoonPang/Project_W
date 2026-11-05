using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHudUI : MonoBehaviour
{
    private sealed class SkillStatusRow
    {
        public AugmentId Id;
        public RectTransform Rect;
        public CanvasGroup Group;
        public Text Label;
        public float AddedAt;
    }

    [SerializeField] private Image healthFill;
    [SerializeField] private Image experienceFill;
    [SerializeField] private Image rCooldownOverlay;
    [SerializeField] private Image rUsageGauge;
    [SerializeField] private Image dodgeCooldownOverlay;
    [SerializeField] private Text healthText;
    [SerializeField] private Text experienceText;
    [SerializeField] private Text attackText;
    [SerializeField] private Text attackSpeedText;
    [SerializeField] private Text moveSpeedText;
    [SerializeField] private Text rCountdownText;
    [SerializeField] private Text levelText;
    [SerializeField] private Button healthPlusButton;
    [SerializeField] private Button healthMinusButton;
    [SerializeField] private Button experiencePlusButton;
    [SerializeField] private Button levelUpButton;

    private PlayerStats playerStats;
    private PlayerProgress playerProgress;
    private RSkillCooldown rSkill;
    private PlayerMovement2D movement;
    private PlayerAugments augments;
    private PlayerAutomaticAugments2D automaticSkills;
    private Font runtimeFont;
    private readonly List<SkillStatusRow> skillRows = new List<SkillStatusRow>();

    public int SkillStatusRowCount => skillRows.Count;

    public void Configure(
        Image newHealthFill, Image newExperienceFill, Image newRCooldownOverlay,
        Text newHealthText, Text newExperienceText, Text newAttackText,
        Text newAttackSpeedText, Text newMoveSpeedText, Text newRCountdownText)
    {
        healthFill = newHealthFill;
        experienceFill = newExperienceFill;
        rCooldownOverlay = newRCooldownOverlay;
        healthText = newHealthText;
        experienceText = newExperienceText;
        attackText = newAttackText;
        attackSpeedText = newAttackSpeedText;
        moveSpeedText = newMoveSpeedText;
        rCountdownText = newRCountdownText;
    }

    public void ConfigureDebugButtons(Button healthPlus, Button healthMinus, Button experiencePlus)
    {
        healthPlusButton = healthPlus;
        healthMinusButton = healthMinus;
        experiencePlusButton = experiencePlus;
    }

    public void ConfigureLevelText(Text newLevelText)
    {
        levelText = newLevelText;
    }

    public void ConfigureLevelUpButton(Button button)
    {
        levelUpButton = button;
    }

    public void ConfigureUsageGauge(Image image)
    {
        rUsageGauge = image;
    }

    public void ConfigureDodgeCooldown(Image image)
    {
        dodgeCooldownOverlay = image;
    }

    private void Awake()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
        playerProgress = FindFirstObjectByType<PlayerProgress>();
        rSkill = FindFirstObjectByType<RSkillCooldown>();
        movement = FindFirstObjectByType<PlayerMovement2D>();
        augments = FindFirstObjectByType<PlayerAugments>();
        automaticSkills = FindFirstObjectByType<PlayerAutomaticAugments2D>();

        runtimeFont = ProjectKoreanFont.Load();
        if (runtimeFont != null)
        {
            foreach (Text label in GetComponentsInChildren<Text>(true))
                label.font = runtimeFont;
        }

        if (healthPlusButton != null)
            healthPlusButton.onClick.AddListener(AddHealth);
        if (healthMinusButton != null)
            healthMinusButton.onClick.AddListener(RemoveHealth);
        if (experiencePlusButton != null)
            experiencePlusButton.onClick.AddListener(AddExperience);
        if (levelUpButton != null)
            levelUpButton.onClick.AddListener(LevelUp);
        if (augments != null)
            augments.AugmentApplied += OnAugmentApplied;

        Refresh();
    }

    private void Update()
    {
        Refresh();
        RefreshSkillRows();
    }

    private void OnDestroy()
    {
        if (healthPlusButton != null)
            healthPlusButton.onClick.RemoveListener(AddHealth);
        if (healthMinusButton != null)
            healthMinusButton.onClick.RemoveListener(RemoveHealth);
        if (experiencePlusButton != null)
            experiencePlusButton.onClick.RemoveListener(AddExperience);
        if (levelUpButton != null)
            levelUpButton.onClick.RemoveListener(LevelUp);
        if (augments != null)
            augments.AugmentApplied -= OnAugmentApplied;
    }

    private void AddHealth()
    {
        if (playerStats != null)
            playerStats.AdjustHealth(10f);
        Refresh();
    }

    private void RemoveHealth()
    {
        if (playerStats != null)
            playerStats.AdjustHealth(-10f);
        Refresh();
    }

    private void AddExperience()
    {
        if (playerProgress != null)
            playerProgress.AddExperiencePercent(10f);
        Refresh();
    }

    private void LevelUp()
    {
        if (playerProgress != null)
            playerProgress.LevelUpImmediately();
        Refresh();
    }

    private void OnAugmentApplied(AugmentId id, int level)
    {
        if (id != AugmentId.Shield && id != AugmentId.EnemySlow &&
            id != AugmentId.EnemyStun)
            return;
        foreach (SkillStatusRow row in skillRows)
        {
            if (row.Id == id)
                return;
        }
        CreateSkillStatusRow(id);
    }

    private void CreateSkillStatusRow(AugmentId id)
    {
        GameObject rowObject = new GameObject("Skill Status " + id,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        rowObject.transform.SetParent(transform, false);
        RectTransform rect = rowObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(520f, 35f);
        rect.anchoredPosition = new Vector2(-530f, -300f - skillRows.Count * 40f);
        Image background = rowObject.GetComponent<Image>();
        background.color = new Color(0.05f, 0.07f, 0.1f, 0.78f);
        background.raycastTarget = false;
        CanvasGroup group = rowObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        GameObject accentObject = new GameObject("Accent",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        accentObject.transform.SetParent(rowObject.transform, false);
        RectTransform accentRect = accentObject.GetComponent<RectTransform>();
        accentRect.anchorMin = accentRect.anchorMax = new Vector2(0f, 0.5f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(6f, 35f);
        Image accent = accentObject.GetComponent<Image>();
        accent.color = id == AugmentId.Shield ? new Color(0.2f, 0.5f, 1f) :
            id == AugmentId.EnemySlow ? new Color(0.35f, 0.9f, 1f) :
            new Color(1f, 0.86f, 0.15f);
        accent.raycastTarget = false;

        GameObject labelObject = new GameObject("Status Text",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelObject.transform.SetParent(rowObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(16f, 0f);
        labelRect.offsetMax = new Vector2(-8f, 0f);
        Text label = labelObject.GetComponent<Text>();
        label.font = runtimeFont != null ? runtimeFont : rCountdownText.font;
        label.fontSize = 21;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = Color.white;
        label.raycastTarget = false;

        skillRows.Add(new SkillStatusRow
        {
            Id = id,
            Rect = rect,
            Group = group,
            Label = label,
            AddedAt = Time.time
        });
        RefreshSkillRows();
    }

    private void RefreshSkillRows()
    {
        if (automaticSkills == null)
            return;
        for (int index = 0; index < skillRows.Count; index++)
        {
            SkillStatusRow row = skillRows[index];
            float progress = Mathf.Clamp01((Time.time - row.AddedAt) / 0.45f);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            row.Rect.anchoredPosition = new Vector2(
                Mathf.Lerp(-530f, 25f, eased), -300f - index * 40f);
            row.Group.alpha = eased;
            row.Label.text = GetSkillStatusText(row.Id);
        }
    }

    private string GetSkillStatusText(AugmentId id)
    {
        if (id == AugmentId.Shield)
            return automaticSkills.ShieldReady
                ? "보호막 : 현재 보호막 활성화"
                : "보호막 : 재사용까지 " + FormatSeconds(automaticSkills.ShieldCooldownRemaining) + "초";
        if (id == AugmentId.EnemySlow)
            return automaticSkills.SlowActive
                ? "얼음지대 : 활성화 중 " + FormatSeconds(automaticSkills.SlowActiveRemaining) + "초"
                : automaticSkills.SlowCooldownRemaining > 0f
                    ? "얼음지대 : 재사용까지 " +
                      FormatSeconds(automaticSkills.SlowCooldownRemaining) + "초"
                    : "얼음지대 : 준비 완료";
        return automaticSkills.StunCharging
            ? "뇌전 : 발동까지 " + FormatSeconds(automaticSkills.StunChargeRemaining) + "초"
            : automaticSkills.StunCooldownRemaining > 0f
                ? "뇌전 : 재사용까지 " +
                  FormatSeconds(automaticSkills.StunCooldownRemaining) + "초"
                : "뇌전 : 준비 완료";
    }

    private static string FormatSeconds(float seconds)
    {
        float shown = Mathf.Ceil(Mathf.Max(0f, seconds) * 10f) / 10f;
        return shown.ToString("00.0", CultureInfo.InvariantCulture);
    }

    private void Refresh()
    {
        if (playerStats != null)
        {
            float healthRatio = playerStats.MaxHealth > 0f
                ? playerStats.CurrentHealth / playerStats.MaxHealth : 0f;
            healthFill.fillAmount = Mathf.Clamp01(healthRatio);
            healthText.text = $"HP : {Mathf.CeilToInt(playerStats.CurrentHealth)}/{Mathf.CeilToInt(playerStats.MaxHealth)}";
            attackText.text = $"공격력 : {playerStats.AttackDamage:0.##}";
            attackSpeedText.text = $"공격속도 : {playerStats.AttackSpeed:0.##}";
            moveSpeedText.text = $"이동속도 : {playerStats.MoveSpeed:0.##}";
        }

        float currentExperience = playerProgress != null ? playerProgress.CurrentExperience : 0f;
        float requiredExperience = playerProgress != null ? playerProgress.ExperienceToNextLevel : 100f;
        experienceFill.fillAmount = requiredExperience > 0f
            ? Mathf.Clamp01(currentExperience / requiredExperience) : 0f;
        float experiencePercent = playerProgress != null
            ? Mathf.Min(99.9f, Mathf.Round(Mathf.Max(0f,
                playerProgress.ExperiencePercent) * 10f) / 10f) : 0f;
        experienceText.text = $"EXP {experiencePercent.ToString("0.0", CultureInfo.InvariantCulture)}%";
        if (levelText != null)
            levelText.text = playerProgress != null ? playerProgress.CurrentLevel.ToString() : "1";

        float remaining = rSkill != null ? rSkill.RemainingSeconds : 0f;
        if (rUsageGauge != null)
        {
            float usageRemaining = rSkill != null ? rSkill.UsageRemaining01 : 0f;
            rUsageGauge.enabled = usageRemaining > 0f;
            rUsageGauge.fillAmount = usageRemaining;
        }
        bool coolingDown = remaining > 0f;
        rCooldownOverlay.enabled = coolingDown;
        rCountdownText.enabled = coolingDown;
        if (coolingDown)
        {
            rCooldownOverlay.fillAmount = remaining / rSkill.CooldownSeconds;
            rCountdownText.text = FormatSeconds(remaining);
        }

        if (dodgeCooldownOverlay != null)
        {
            float dodgeRemaining = movement != null ? movement.DashCooldownRemaining : 0f;
            dodgeCooldownOverlay.enabled = dodgeRemaining > 0f;
            if (dodgeRemaining > 0f)
                dodgeCooldownOverlay.fillAmount = dodgeRemaining / movement.DashCooldownSeconds;
        }
    }
}
