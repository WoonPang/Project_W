using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Lives on the HUD. Result animations use unscaled time while gameplay is frozen.
public sealed class GameOverUI : MonoBehaviour
{
    private const string LobbyScene = "Prototype_Lobby";
    private const float IntroDuration = 1.45f;
    private const float ExitDuration = 1.15f;
    private static readonly Color Panel = new Color(0.43f, 0.43f, 0.43f);
    private static readonly Color Cell = new Color(0.84f, 0.84f, 0.84f);
    private static readonly Color ClearPanel = new Color(0.22f, 0.22f, 0.22f);

    private enum Phase { Playing, WaitingForDeathFade, Entering, Showing, Leaving }
    private Phase phase;
    private PlayerStats stats;
    private PlayerProgress progress;
    private PlayerDamageFeedbackUI deathFeedback;
    private GameSessionTime2D sessionTime;
    private PlayTimeUI playTime;
    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform bodyRect;
    private Image background;
    private RawImage clearVeil;
    private Image resultPanel;
    private Image lobbyButtonImage;
    private Text titleText;
    private Text lobbyButtonText;
    private CanvasGroup content;
    private Button lobbyButton;
    private Text[] resultValues;
    private Texture2D portraitTexture;
    private Texture2D clearVeilTexture;
    private Font font;
    private bool ownsFont;
    private float phaseElapsed;
    private float veilProgress;
    private bool isVictory;
    private GameOverResult2D result;

    public GameOverResult2D Result => result;

    private void Awake()
    {
        stats = FindFirstObjectByType<PlayerStats>();
        progress = FindFirstObjectByType<PlayerProgress>();
        deathFeedback = GetComponent<PlayerDamageFeedbackUI>();
        sessionTime = FindFirstObjectByType<GameSessionTime2D>();
        playTime = GetComponent<PlayTimeUI>();
        font = Font.CreateDynamicFontFromOSFont(
            new[] { "Apple SD Gothic Neo", "Malgun Gothic", "Noto Sans CJK KR", "Arial" }, 64);
        ownsFont = font != null;
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildScreen();
        FinalBoss2D.Defeated += OnFinalBossDefeated;
        canvas.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (phase == Phase.Playing && stats != null && stats.CurrentHealth <= 0f)
        {
            result = CaptureResult();
            Time.timeScale = 0f;
            canvas.gameObject.SetActive(true);
            phase = Phase.WaitingForDeathFade;
        }

        if (phase == Phase.WaitingForDeathFade &&
            (deathFeedback == null || deathFeedback.DeathFadeComplete))
        {
            FillResult();
            background.color = Color.black;
            content.alpha = 0f;
            content.blocksRaycasts = false;
            phaseElapsed = 0f;
            phase = Phase.Entering;
        }

        if (phase != Phase.Entering && phase != Phase.Leaving)
            return;

        phaseElapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(phaseElapsed /
            (phase == Phase.Entering ? IntroDuration : ExitDuration));
        float eased = Mathf.SmoothStep(0f, 1f, t);
        if (phase == Phase.Entering)
        {
            float contentProgress = isVictory ? Mathf.Clamp01((t - 0.18f) / 0.82f) : t;
            content.alpha = Mathf.SmoothStep(0f, 1f, contentProgress);
            if (isVictory)
                veilProgress = eased;
        }
        else
        {
            content.alpha = 1f - eased;
            if (isVictory)
                clearVeil.color = new Color(1f, 1f, 1f, 1f - eased);
        }

        if (t < 1f)
            return;
        if (phase == Phase.Entering)
        {
            phase = Phase.Showing;
            content.blocksRaycasts = true;
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(LobbyScene);
        }
    }

    private void ReturnToLobby()
    {
        if (phase != Phase.Showing)
            return;
        phase = Phase.Leaving;
        phaseElapsed = 0f;
        content.blocksRaycasts = false;
    }

    private GameOverResult2D CaptureResult()
    {
        float seconds = sessionTime != null ? sessionTime.ElapsedSeconds :
            playTime != null ? playTime.ElapsedSeconds : 0f;
        return new GameOverResult2D(progress != null ? progress.CurrentLevel : 1,
            seconds, progress != null ? progress.EnemiesDefeated : 0);
    }

    private void OnFinalBossDefeated()
    {
        if (phase != Phase.Playing || stats == null || stats.CurrentHealth <= 0f)
            return;

        isVictory = true;
        result = CaptureResult();
        Time.timeScale = 0f;
        MonsterSpawnGate2D.SetPaused(true);
        foreach (CombatTarget2D target in FindObjectsByType<CombatTarget2D>(
                     FindObjectsSortMode.None))
        {
            if (target is FinalBoss2D || target is TrainingDummy2D)
                continue;
            EnemyClearFade2D fade = target.GetComponent<EnemyClearFade2D>();
            if (fade == null)
                fade = target.gameObject.AddComponent<EnemyClearFade2D>();
            fade.Begin(0.7f);
        }
        foreach (EnemyProjectile2D projectile in FindObjectsByType<EnemyProjectile2D>(
                     FindObjectsSortMode.None))
            Destroy(projectile.gameObject);

        titleText.text = "Game Clear!";
        titleText.color = Color.black;
        resultPanel.color = ClearPanel;
        lobbyButtonImage.color = ClearPanel;
        lobbyButtonText.color = Color.white;
        clearVeil.gameObject.SetActive(true);
        clearVeil.color = Color.white;
        veilProgress = 0f;
        FillResult();
        canvas.gameObject.SetActive(true);
        content.alpha = 0f;
        content.blocksRaycasts = false;
        phaseElapsed = 0f;
        phase = Phase.Entering;
    }

    private void LateUpdate()
    {
        if (canvasRect == null || bodyRect == null)
            return;

        Vector2 visibleSize = canvasRect.rect.size;
        if (visibleSize.x <= 0f || visibleSize.y <= 0f)
            return;
        float fit = Mathf.Min(1f, (visibleSize.x - 64f) / 1500f,
            (visibleSize.y - 64f) / 1000f);
        bodyRect.localScale = Vector3.one * Mathf.Max(0.1f, fit);
        if (isVictory && clearVeil != null)
        {
            float side = visibleSize.magnitude * 1.6f * veilProgress;
            clearVeil.rectTransform.sizeDelta = new Vector2(side, side);
        }
    }

    private void FillResult()
    {
        resultValues[0].text = result.Level.ToString();
        resultValues[1].text = result.DisplayTime;
        resultValues[2].text = result.Kills.ToString();
        resultValues[3].text = $"{result.Level} × 15 = {result.LevelScore}";
        resultValues[4].text = $"{result.Seconds} × 3 = {result.TimeScore}";
        resultValues[5].text = $"{result.Kills} × 2 = {result.KillScore}";
        resultValues[6].text = $"{result.TotalScore} / 10 = {result.Coins}";
    }

    private void BuildScreen()
    {
        GameObject root = new GameObject("Game Result Screen", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = root.GetComponent<Canvas>();
        canvasRect = root.GetComponent<RectTransform>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform black = Rect("Black Background", root.transform, Vector2.zero,
            new Vector2(1920f, 1080f));
        black.anchorMin = Vector2.zero;
        black.anchorMax = Vector2.one;
        black.offsetMin = Vector2.zero;
        black.offsetMax = Vector2.zero;
        background = black.GetComponent<Image>();
        background.color = Color.clear;
        background.raycastTarget = true;

        clearVeilTexture = CreateClearVeilTexture();
        RectTransform veilRect = Rect("Clear Gray Blur", root.transform,
            Vector2.zero, Vector2.zero, false);
        clearVeil = veilRect.gameObject.AddComponent<RawImage>();
        clearVeil.texture = clearVeilTexture;
        clearVeil.color = Color.white;
        clearVeil.raycastTarget = false;
        clearVeil.gameObject.SetActive(false);

        RectTransform body = Rect("Results", root.transform, Vector2.zero,
            new Vector2(1500f, 1000f), false);
        bodyRect = body;
        content = body.gameObject.AddComponent<CanvasGroup>();
        content.alpha = 0f;
        content.blocksRaycasts = false;
        titleText = Label("Game Over...", body, new Vector2(0f, 385f),
            new Vector2(1500f, 200f), 195, Color.white);

        RectTransform table = Rect("Result Panel", body, new Vector2(0f, -20f),
            new Vector2(980f, 500f));
        resultPanel = table.GetComponent<Image>();
        resultPanel.color = Panel;
        RectTransform resultHeader = CellRect("Result Header", table,
            new Vector2(-40f, 200f), new Vector2(530f, 100f));
        RectTransform scoreHeader = CellRect("Score Header", table,
            new Vector2(357.5f, 200f), new Vector2(265f, 100f));
        Label("Result", resultHeader, Vector2.zero, new Vector2(520f, 94f), 72, Color.black);
        Label("환산점수", scoreHeader, Vector2.zero, new Vector2(255f, 94f), 44, Color.black);

        string[] names = { "최종 레벨", "플레이 시간", "처치한 적" };
        resultValues = new Text[7];
        for (int i = 0; i < 3; i++)
        {
            float rowY = 100f - 100f * i;
            RectTransform nameCell = CellRect(names[i], table,
                new Vector2(-152.5f, rowY), new Vector2(305f, 100f));
            RectTransform valueCell = CellRect(names[i] + " Value", table,
                new Vector2(112.5f, rowY), new Vector2(225f, 100f));
            RectTransform scoreCell = CellRect(names[i] + " Score", table,
                new Vector2(357.5f, rowY), new Vector2(265f, 100f));
            Label(names[i], nameCell, Vector2.zero,
                new Vector2(295f, 94f), 43, Color.black);
            resultValues[i] = Label("0", valueCell, Vector2.zero,
                new Vector2(215f, 94f), 60, Color.black);
            resultValues[i + 3] = Label("0 × 0 = 0", scoreCell, Vector2.zero,
                new Vector2(255f, 94f), 36, Color.black);
        }
        RectTransform coinName = CellRect("획득 코인", table,
            new Vector2(-152.5f, -200f), new Vector2(305f, 100f));
        RectTransform coinValue = CellRect("획득 코인 Value", table,
            new Vector2(245f, -200f), new Vector2(490f, 100f));
        Label("획득 코인", coinName, Vector2.zero,
            new Vector2(295f, 94f), 43, Color.black);
        resultValues[6] = Label("0 / 10 = 0", coinValue, Vector2.zero,
            new Vector2(480f, 94f), 48, Color.black);

        portraitTexture = CreatePortraitTexture();
        RectTransform portrait = Rect("Portrait Placeholder", body,
            new Vector2(-490f, -20f), new Vector2(245f, 245f), false);
        RawImage portraitImage = portrait.gameObject.AddComponent<RawImage>();
        portraitImage.texture = portraitTexture;
        portraitImage.raycastTarget = false;

        RectTransform buttonRect = Rect("로비로 돌아가기", body,
            new Vector2(0f, -390f), new Vector2(700f, 110f));
        lobbyButtonImage = buttonRect.GetComponent<Image>();
        lobbyButtonImage.color = Cell;
        lobbyButton = buttonRect.gameObject.AddComponent<Button>();
        lobbyButton.targetGraphic = lobbyButtonImage;
        lobbyButton.onClick.AddListener(ReturnToLobby);
        lobbyButtonText = Label("로비로 돌아가기", buttonRect, Vector2.zero,
            new Vector2(690f, 104f), 76, Color.black);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position,
        Vector2 size, bool image = true)
    {
        GameObject element = new GameObject(name, typeof(RectTransform),
            typeof(CanvasRenderer));
        element.transform.SetParent(parent, false);
        RectTransform rect = element.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        if (image)
            element.AddComponent<Image>();
        return rect;
    }

    private static RectTransform CellRect(string name, Transform parent,
        Vector2 position, Vector2 size)
    {
        RectTransform border = Rect(name + " Border", parent, position, size);
        border.GetComponent<Image>().color = Color.black;
        RectTransform face = Rect(name, border, Vector2.zero, size - new Vector2(2f, 2f));
        face.GetComponent<Image>().color = Cell;
        return face;
    }

    private Text Label(string value, Transform parent, Vector2 position, Vector2 size,
        int sizeOfFont, Color color)
    {
        RectTransform rect = Rect(value + " Text", parent, position, size, false);
        Text label = rect.gameObject.AddComponent<Text>();
        label.font = font;
        label.text = value;
        label.fontSize = sizeOfFont;
        label.color = color;
        label.alignment = TextAnchor.MiddleCenter;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = Mathf.Max(20, sizeOfFont / 2);
        label.resizeTextMaxSize = sizeOfFont;
        label.raycastTarget = false;
        return label;
    }

    private static Texture2D CreateClearVeilTexture()
    {
        const int width = 256;
        Texture2D texture = new Texture2D(width, width, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[width * width];
        for (int y = 0; y < width; y++)
        for (int x = 0; x < width; x++)
        {
            float dx = (x + 0.5f - width * 0.5f) / (width * 0.5f);
            float dy = (y + 0.5f - width * 0.5f) / (width * 0.5f);
            float radius = Mathf.Sqrt(dx * dx + dy * dy);
            float edge = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(0.72f, 1f, radius));
            pixels[y * width + x] = new Color32(235, 235, 235,
                (byte)Mathf.RoundToInt(218f * (1f - edge)));
        }
        texture.SetPixels32(pixels);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        return texture;
    }

    private static Texture2D CreatePortraitTexture()
    {
        const int width = 128;
        Texture2D texture = new Texture2D(width, width, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[width * width];
        for (int y = 0; y < width; y++)
        for (int x = 0; x < width; x++)
        {
            float dx = (x + 0.5f - width * 0.5f) / (width * 0.5f);
            float dy = (y + 0.5f - width * 0.5f) / (width * 0.5f);
            float radius = Mathf.Sqrt(dx * dx + dy * dy);
            pixels[y * width + x] = radius > 1f ? new Color32(0, 0, 0, 0) :
                radius > 0.965f ? new Color32(0, 0, 0, 255) :
                new Color32(216, 216, 216, 255);
        }
        texture.SetPixels32(pixels);
        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();
        return texture;
    }

    private void OnDestroy()
    {
        FinalBoss2D.Defeated -= OnFinalBossDefeated;
        if (isVictory)
            MonsterSpawnGate2D.SetPaused(false);
        if (phase != Phase.Playing)
            Time.timeScale = 1f;
        if (lobbyButton != null)
            lobbyButton.onClick.RemoveListener(ReturnToLobby);
        if (portraitTexture != null)
            Destroy(portraitTexture);
        if (clearVeilTexture != null)
            Destroy(clearVeilTexture);
        if (ownsFont && font != null)
            Destroy(font);
        if (canvas != null)
            Destroy(canvas.gameObject);
    }
}
