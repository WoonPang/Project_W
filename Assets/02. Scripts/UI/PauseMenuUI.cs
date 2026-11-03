using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button backButton;

    private bool isPaused;
    private float timeScaleBeforePause = 1f;
    private LevelUpUI levelUpUI;

    public bool IsPaused => isPaused;

    public void Configure(GameObject overlay, GameObject main, GameObject options,
        Button resume, Button option, Button quit, Button back)
    {
        overlayRoot = overlay;
        mainPanel = main;
        optionPanel = options;
        resumeButton = resume;
        optionButton = option;
        quitButton = quit;
        backButton = back;
    }

    private void Awake()
    {
        levelUpUI = FindFirstObjectByType<LevelUpUI>();
        overlayRoot.SetActive(false);
        mainPanel.SetActive(true);
        optionPanel.SetActive(false);

        resumeButton.onClick.AddListener(ResumeGame);
        optionButton.onClick.AddListener(OpenOptions);
        quitButton.onClick.AddListener(ReturnToLobby);
        backButton.onClick.AddListener(ShowMainPanel);
    }

    private void Update()
    {
        if (levelUpUI != null && levelUpUI.IsOpen)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            return;

        if (!isPaused)
            OpenMenu();
        else if (optionPanel.activeSelf)
            ShowMainPanel();
        else
            ResumeGame();
    }

    public void OpenMenu()
    {
        if (isPaused)
            return;

        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;
        isPaused = true;
        overlayRoot.SetActive(true);
        ShowMainPanel();
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        Time.timeScale = timeScaleBeforePause;
        isPaused = false;
        overlayRoot.SetActive(false);
    }

    private void OpenOptions()
    {
        mainPanel.SetActive(false);
        optionPanel.SetActive(true);
    }

    private void ShowMainPanel()
    {
        optionPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    private void ReturnToLobby()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("Prototype_Lobby");
    }

    private void OnDestroy()
    {
        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(ResumeGame);
        if (optionButton != null)
            optionButton.onClick.RemoveListener(OpenOptions);
        if (quitButton != null)
            quitButton.onClick.RemoveListener(ReturnToLobby);
        if (backButton != null)
            backButton.onClick.RemoveListener(ShowMainPanel);
        if (isPaused)
            Time.timeScale = timeScaleBeforePause;
    }
}
