using UnityEngine;
using UnityEngine.UI;

public sealed class TestPlayTimeControls : MonoBehaviour
{
    [SerializeField] private Button addOneMinuteButton;
    [SerializeField] private Button addFiveMinutesButton;
    [SerializeField] private GameSessionTime2D gameTime;
    [SerializeField] private PlayTimeUI timeDisplay;

    public void Configure(Button oneMinute, Button fiveMinutes,
        GameSessionTime2D clock, PlayTimeUI display)
    {
        addOneMinuteButton = oneMinute;
        addFiveMinutesButton = fiveMinutes;
        gameTime = clock;
        timeDisplay = display;
    }

    private void OnEnable()
    {
        if (addOneMinuteButton != null)
            addOneMinuteButton.onClick.AddListener(AddOneMinute);
        if (addFiveMinutesButton != null)
            addFiveMinutesButton.onClick.AddListener(AddFiveMinutes);
    }

    private void OnDisable()
    {
        if (addOneMinuteButton != null)
            addOneMinuteButton.onClick.RemoveListener(AddOneMinute);
        if (addFiveMinutesButton != null)
            addFiveMinutesButton.onClick.RemoveListener(AddFiveMinutes);
    }

    public void AddOneMinute()
    {
        if (gameTime != null)
            gameTime.AdvanceSeconds(60f);
        if (timeDisplay != null)
            timeDisplay.RefreshText();
    }

    public void AddFiveMinutes()
    {
        if (gameTime != null)
            gameTime.AdvanceSeconds(300f);
        if (timeDisplay != null)
            timeDisplay.RefreshText();
    }
}
