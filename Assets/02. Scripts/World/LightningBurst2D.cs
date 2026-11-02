using UnityEngine;

public sealed class LightningBurst2D : MonoBehaviour
{
    private struct Bolt
    {
        public Transform Transform;
        public SpriteRenderer Renderer;
        public Vector3 Origin;
        public Vector3 Drift;
        public float Delay;
        public float Scale;
    }

    private const float Lifetime = 0.85f;
    private Bolt[] bolts;
    private Texture2D texture;
    private Sprite sprite;
    private Texture2D impactTexture;
    private Sprite impactSprite;
    private SpriteRenderer mainStrike;
    private SpriteRenderer impactFlash;
    private float age;

    public static void Spawn(Transform target)
    {
        GameObject root = new GameObject("Stun Lightning Burst");
        root.transform.SetParent(target, false);
        root.transform.localPosition = Vector3.zero;
        root.AddComponent<LightningBurst2D>().Initialize();
    }

    private void Initialize()
    {
        CreateBoltSprite();
        CreateImpactSprite();
        GameObject strike = new GameObject("Main Lightning Strike");
        strike.transform.SetParent(transform, false);
        strike.transform.localPosition = new Vector3(0f, 1f, 0f);
        strike.transform.localScale = new Vector3(1.25f, 2.2f, 1f);
        mainStrike = strike.AddComponent<SpriteRenderer>();
        mainStrike.sprite = sprite;
        mainStrike.color = new Color(1f, 0.95f, 0.18f, 1f);
        mainStrike.sortingOrder = 32;

        GameObject flash = new GameObject("Lightning Impact Flash");
        flash.transform.SetParent(transform, false);
        flash.transform.localPosition = Vector3.zero;
        impactFlash = flash.AddComponent<SpriteRenderer>();
        impactFlash.sprite = impactSprite;
        impactFlash.color = new Color(1f, 0.88f, 0.12f, 0.65f);
        impactFlash.sortingOrder = 31;

        bolts = new Bolt[10];
        for (int index = 0; index < bolts.Length; index++)
        {
            GameObject particle = new GameObject("Yellow Lightning " + (index + 1));
            particle.transform.SetParent(transform, false);
            SpriteRenderer renderer = particle.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(1f, 0.88f, 0.08f, 1f);
            renderer.sortingOrder = 30;
            float angle = index * Mathf.PI * 2f / bolts.Length + Random.Range(-0.2f, 0.2f);
            Vector3 radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            bolts[index] = new Bolt
            {
                Transform = particle.transform,
                Renderer = renderer,
                Origin = radial * Random.Range(0.25f, 0.6f),
                Drift = radial * Random.Range(0.15f, 0.4f) + Vector3.up * 0.18f,
                Delay = Random.Range(0f, 0.18f),
                Scale = Random.Range(0.45f, 0.7f)
            };
            particle.transform.localRotation = Quaternion.Euler(0f, 0f,
                Random.Range(-35f, 35f));
        }
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        age += Time.deltaTime;
        float strikeLife = Mathf.Clamp01(1f - age / 0.38f);
        mainStrike.enabled = strikeLife > 0f;
        mainStrike.color = new Color(1f, 0.95f, 0.18f, strikeLife);
        float flashProgress = Mathf.Clamp01(age / 0.45f);
        impactFlash.enabled = flashProgress < 1f;
        impactFlash.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.7f, flashProgress);
        impactFlash.color = new Color(1f, 0.88f, 0.12f,
            0.65f * (1f - flashProgress));
        for (int index = 0; index < bolts.Length; index++)
        {
            Bolt bolt = bolts[index];
            bool started = age >= bolt.Delay;
            bolt.Renderer.enabled = started;
            if (!started)
                continue;
            float progress = Mathf.Clamp01((age - bolt.Delay) / (Lifetime - bolt.Delay));
            bolt.Transform.localPosition = bolt.Origin + bolt.Drift * progress;
            bolt.Transform.localScale = Vector3.one * bolt.Scale * (0.7f + 0.3f * progress);
            Color color = bolt.Renderer.color;
            color.a = 1f - progress;
            bolt.Renderer.color = color;
        }

        if (age >= Lifetime)
            Destroy(gameObject);
    }

    private void CreateBoltSprite()
    {
        const int width = 24;
        const int height = 56;
        texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        Color32[] pixels = new Color32[width * height];
        DrawSegment(pixels, width, height, 11, 55, 5, 39);
        DrawSegment(pixels, width, height, 5, 39, 17, 28);
        DrawSegment(pixels, width, height, 17, 28, 7, 12);
        DrawSegment(pixels, width, height, 7, 12, 12, 0);
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f), 56f);
    }

    private void CreateImpactSprite()
    {
        const int size = 64;
        impactTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float radius = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) /
                           (size * 0.5f);
            float alpha = Mathf.Clamp01(1f - radius);
            pixels[y * size + x] = new Color32(255, 255, 255,
                (byte)Mathf.RoundToInt(alpha * alpha * 255f));
        }
        impactTexture.SetPixels32(pixels);
        impactTexture.Apply(false, false);
        impactSprite = Sprite.Create(impactTexture, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), size);
    }

    private static void DrawSegment(Color32[] pixels, int width, int height,
        int x0, int y0, int x1, int y1)
    {
        int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
        for (int step = 0; step <= steps; step++)
        {
            float t = step / (float)steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                int px = x + dx;
                int py = y + dy;
                if (px >= 0 && px < width && py >= 0 && py < height)
                    pixels[py * width + px] = new Color32(255, 255, 255, 255);
            }
        }
    }

    private void OnDestroy()
    {
        if (sprite != null)
            Destroy(sprite);
        if (texture != null)
            Destroy(texture);
        if (impactSprite != null)
            Destroy(impactSprite);
        if (impactTexture != null)
            Destroy(impactTexture);
    }
}
