using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LobbyMenuUI : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button testButton;
    [SerializeField] private Button exitButton;
    private Font runtimeFont;

    public void Configure(Button play, Button test, Button exit)
    {
        playButton = play;
        testButton = test;
        exitButton = exit;
    }

    private void Awake()
    {
        Time.timeScale = 1f;
        MonsterSpawnGate2D.SetPaused(false);
        runtimeFont = Font.CreateDynamicFontFromOSFont(
            new[] { "Apple SD Gothic Neo", "Malgun Gothic", "Noto Sans CJK KR", "Arial" }, 64);
        if (runtimeFont != null)
        {
            foreach (Text label in GetComponentsInChildren<Text>(true))
                label.font = runtimeFont;
        }
        playButton.onClick.AddListener(StartGame);
        testButton.onClick.AddListener(StartTest);
        exitButton.onClick.AddListener(ExitGame);
    }

    private void StartGame() => SceneManager.LoadScene("Prototype_Play");
    private void StartTest() => SceneManager.LoadScene("Prototype_Test_Play");

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(StartGame);
        if (testButton != null)
            testButton.onClick.RemoveListener(StartTest);
        if (exitButton != null)
            exitButton.onClick.RemoveListener(ExitGame);
        if (runtimeFont != null)
            Destroy(runtimeFont);
    }
}
