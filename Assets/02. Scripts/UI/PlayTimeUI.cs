using UnityEngine;
using UnityEngine.UI;

public sealed class PlayTimeUI : MonoBehaviour
{
    [SerializeField] private Text timeText;

    private float elapsedSeconds;
    private int lastDisplayedSecond = -1;

    public float ElapsedSeconds => elapsedSeconds;

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

        elapsedSeconds += Time.deltaTime;
        RefreshText();
    }

    private void RefreshText()
    {
        if (timeText == null)
            return;

        int wholeSeconds = Mathf.FloorToInt(elapsedSeconds);
        if (wholeSeconds == lastDisplayedSecond)
            return;

        lastDisplayedSecond = wholeSeconds;
        timeText.text = $"{wholeSeconds / 60:00}:{wholeSeconds % 60:00}";
    }
}
