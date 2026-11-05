using UnityEngine;

public static class ProjectKoreanFont
{
    private const string ResourcePath = "Fonts/NanumGothic-Regular";
    private static Font cachedFont;

    public static Font Load()
    {
        if (cachedFont == null)
            cachedFont = Resources.Load<Font>(ResourcePath);

        if (cachedFont == null)
            Debug.LogError("Korean UI font is missing: Assets/Resources/Fonts/NanumGothic-Regular.ttf");

        return cachedFont;
    }
}
