using UnityEngine;
using UnityEngine.UI;

public sealed class PlayTimeUI : MonoBehaviour
{
    [SerializeField] private Text timeText;

    private float elapsedSeconds;
    private GameSessionTime2D sessionTime;
    private int lastDisplayedSecond = -1;

    public float ElapsedSeconds
    {
        get
        {
            if (sessionTime == null)
                sessionTime = FindFirstObjectByType<GameSessionTime2D>();
            return sessionTime != null ? sessionTime.ElapsedSeconds : elapsedSeconds;
        }
    }

    public void Configure(Text label)
    {
        timeText = label;
    }

    private void Awake()
    {
        RefreshText();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        if (sessionTime == null)
            elapsedSeconds += Time.deltaTime;
        RefreshText();
    }

    public void RefreshText()
    {
        if (timeText == null)
            return;

        int wholeSeconds = Mathf.FloorToInt(ElapsedSeconds);
        if (wholeSeconds == lastDisplayedSecond)
            return;

        lastDisplayedSecond = wholeSeconds;
        timeText.text = $"{wholeSeconds / 60:00}:{wholeSeconds % 60:00}";
    }
}
