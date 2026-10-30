using System.IO;
using UnityEditor;
using UnityEngine;

public static class RoundedMaskMaker
{
    [MenuItem("Tools/Create Rounded Card Mask")]
    public static void Create()
    {
        const int size = 128;
        const int radius = 32;
        const string folder = "Assets/Art";
        const string path = folder + "/RoundedCardMask.png";

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float cx = Mathf.Clamp(px, radius, size - radius);
                float cy = Mathf.Clamp(py, radius, size - radius);
                float dist = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        Directory.CreateDirectory(folder);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = new Vector4(radius, radius, radius, radius);
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        Debug.Log("Created " + path);
    }
}