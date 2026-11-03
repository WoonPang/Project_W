using UnityEngine;

public sealed class GameSessionTime2D : MonoBehaviour
{
    private float elapsedSeconds;

    public float ElapsedSeconds => elapsedSeconds;

    private void Update()
    {
        if (Time.timeScale > 0f)
            elapsedSeconds += Time.deltaTime;
    }

    public void AdvanceSeconds(float seconds)
    {
        if (Time.timeScale <= 0f || seconds <= 0f)
            return;
        elapsedSeconds += seconds;
    }
}
