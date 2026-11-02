using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AshGroundSetup
{
    private const string ScenePath = "Assets/01. Scenes/Prototype.unity";
    private const string TexturePath = "Assets/03. Asset/Sprites/AshGroundTile.png";
    private const int TextureSize = 1024;
    private const float PixelsPerUnit = 40f;

    public static void Apply()
    {
        Sprite ashSprite = CreateAshSprite();
        var scene = EditorSceneManager.OpenScene(ScenePath);
        GameObject ground = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Grid Ground" || root.name == "Ash Ground")
            {
                ground = root;
                break;
            }
        }
        if (ground == null)
            throw new InvalidOperationException("Ground object is missing from Prototype.");

        ground.name = "Ash Ground";
        ground.SetActive(true);
        SpriteRenderer renderer = ground.GetComponent<SpriteRenderer>();
        renderer.sprite = ashSprite;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = new Vector2(100f, 100f);
        renderer.color = Color.white;
        renderer.sortingOrder = -10;
        ground.GetComponent<TiledGroundFollow2D>().ConfigureTileWorldSize(TextureSize / PixelsPerUnit);

        Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (camera == null)
            throw new InvalidOperationException("Prototype camera is missing.");
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(42, 41, 40, 255);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("Dark ash ground with scattered flecks created.");
    }

    private static Sprite CreateAshSprite()
    {
        string absolutePath = Path.Combine(Application.dataPath, "03. Asset/Sprites/AshGroundTile.png");
        if (!File.Exists(absolutePath))
        {
            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            try
            {
                Color32[] pixels = new Color32[TextureSize * TextureSize];
                for (int y = 0; y < TextureSize; y++)
                {
                    for (int x = 0; x < TextureSize; x++)
                    {
                        float broad = TileNoise(x, y, 5, 11);
                        float medium = TileNoise(x, y, 21, 29);
                        float grain = TileNoise(x, y, 81, 47);
                        int shade = Mathf.RoundToInt(42f + (broad - 0.5f) * 7f +
                            (medium - 0.5f) * 5f + (grain - 0.5f) * 3f);
                        pixels[y * TextureSize + x] = new Color32(
                            (byte)(shade + 1), (byte)shade, (byte)(shade - 1), 255);
                    }
                }

                var random = new System.Random(20261031);
                for (int i = 0; i < 4200; i++)
                {
                    int x = random.Next(TextureSize);
                    int y = random.Next(TextureSize);
                    int radius = random.NextDouble() < 0.84 ? 1 : 2;
                    int shade = random.NextDouble() < 0.62
                        ? random.Next(55, 76) : random.Next(25, 36);
                    PaintSpeck(pixels, x, y, radius, shade);
                }
                for (int i = 0; i < 125; i++)
                {
                    int x = random.Next(TextureSize);
                    int y = random.Next(TextureSize);
                    PaintSpeck(pixels, x, y, random.Next(2, 5), random.Next(48, 64));
                }

                texture.SetPixels32(pixels);
                texture.Apply(false);
                File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = TextureSize;
        importer.SaveAndReimport();

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath);
        if (sprite == null)
            throw new InvalidOperationException("Ash texture could not be imported as a sprite.");
        return sprite;
    }

    private static float TileNoise(int x, int y, int cells, int seed)
    {
        float gx = x * cells / (float)TextureSize;
        float gy = y * cells / (float)TextureSize;
        int ix = Mathf.FloorToInt(gx);
        int iy = Mathf.FloorToInt(gy);
        float tx = Mathf.SmoothStep(0f, 1f, gx - ix);
        float ty = Mathf.SmoothStep(0f, 1f, gy - iy);
        float a = Mathf.Lerp(Hash(ix, iy, cells, seed), Hash(ix + 1, iy, cells, seed), tx);
        float b = Mathf.Lerp(Hash(ix, iy + 1, cells, seed), Hash(ix + 1, iy + 1, cells, seed), tx);
        return Mathf.Lerp(a, b, ty);
    }

    private static float Hash(int x, int y, int cells, int seed)
    {
        unchecked
        {
            uint a = (uint)((x % cells) * 73856093);
            uint b = (uint)((y % cells) * 19349663);
            uint h = a ^ b ^ (uint)(seed * 83492791);
            h ^= h >> 16;
            h *= 2246822519u;
            h ^= h >> 13;
            return (h & 65535u) / 65535f;
        }
    }

    private static void PaintSpeck(Color32[] pixels, int centerX, int centerY, int radius, int shade)
    {
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx * dx + dy * dy > radius * radius)
                    continue;
                int x = (centerX + dx + TextureSize) % TextureSize;
                int y = (centerY + dy + TextureSize) % TextureSize;
                pixels[y * TextureSize + x] = new Color32(
                    (byte)(shade + 2), (byte)shade, (byte)(shade - 2), 255);
            }
        }
    }

    private static void Validate()
    {
        GameObject ground = GameObject.Find("Ash Ground");
        if (ground == null || !ground.activeSelf)
            throw new InvalidOperationException("Ash ground is not active.");
        SpriteRenderer renderer = ground.GetComponent<SpriteRenderer>();
        if (renderer.sprite == null || renderer.sprite.name != "AshGroundTile" ||
            renderer.drawMode != SpriteDrawMode.Tiled)
            throw new InvalidOperationException("Ash ground sprite is not configured.");
        Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (camera == null || camera.backgroundColor != new Color32(42, 41, 40, 255))
            throw new InvalidOperationException("Camera clear color is incorrect.");
        Debug.Log("Ash ground texture, world-space tiling, and camera validated.");
    }
}
