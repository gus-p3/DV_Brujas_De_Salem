using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Configura los parámetros de importación para todos los sprites del entorno en Assets/Art/Environment:
/// Sprite (2D and UI), PPU 64, Bilinear, sin compresión y Full Rect (ideal para fondos repetibles).
/// </summary>
public static class EnvironmentImporter
{
    private const string EnvironmentFolder = "Assets/Art/Environment";
    private const float TargetPixelsPerUnit = 64f;

    [MenuItem("Brujas/2. Sprites/Configurar entorno")]
    public static void ConfigureEnvironmentSprites()
    {
        if (!Directory.Exists(EnvironmentFolder))
        {
            Debug.LogError($"[EnvironmentImporter] La carpeta {EnvironmentFolder} no existe.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { EnvironmentFolder });
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            bool changed = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, TargetPixelsPerUnit))
            {
                importer.spritePixelsPerUnit = TargetPixelsPerUnit;
                changed = true;
            }

            if (importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                changed = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.spriteMeshType != SpriteMeshType.FullRect)
            {
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[EnvironmentImporter] {count} sprites de entorno configurados con éxito a PPU 64, Bilinear, Full Rect y sin compresión.");
    }
}
