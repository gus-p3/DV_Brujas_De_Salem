using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Corta y configura los spritesheets de corazones (Heart_Break, Heart_Heal) y Checkpoint,
/// e importa los iconos de UI e ítems con PPU 64 y Sprite (2D and UI).
/// </summary>
public static class HeartAndCheckpointSlicer
{
    private const float TargetPpu = 64f;

    [MenuItem("Brujas/2. Sprites/Cortar corazones")]
    [MenuItem("Brujas/Sprites/Cortar corazones")]
    public static void SliceAllHeartsAndCheckpoint()
    {
        // 1. Cortar Heart_Break (6 cuadros de 128x128, pivote centro)
        SliceSpriteSheet("Assets/Art/UI/Heart_Break.png", 128, 128, new Vector2(0.5f, 0.5f), SpriteAlignment.Center);

        // 2. Cortar Heart_Heal (4 cuadros de 128x128, pivote centro)
        SliceSpriteSheet("Assets/Art/UI/Heart_Heal.png", 128, 128, new Vector2(0.5f, 0.5f), SpriteAlignment.Center);

        // 3. Cortar Env_Checkpoint (6 cuadros de 192x256, pivote inferior centro)
        SliceSpriteSheet("Assets/Art/Environment/Env_Checkpoint.png", 192, 256, new Vector2(0.5f, 0.0f), SpriteAlignment.BottomCenter);

        // 4. Configurar iconos individuales (Single Sprite)
        ConfigureSingleSprite("Assets/Art/UI/ui_icon_heart.png");
        ConfigureSingleSprite("Assets/Art/UI/ui_icon_heart_full.png");
        ConfigureSingleSprite("Assets/Art/UI/ui_icon_heart_empty.png");
        ConfigureSingleSprite("Assets/Art/Environment/Item_Heart.png");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[HeartAndCheckpointSlicer] Sprites de corazones y checkpoint cortados y configurados correctamente.");
    }

    public static void SliceSpriteSheet(string assetPath, int cellWidth, int cellHeight, Vector2 pivot, SpriteAlignment alignment)
    {
        if (!File.Exists(assetPath))
        {
            Debug.LogError($"[HeartAndCheckpointSlicer] Archivo no encontrado: {assetPath}");
            return;
        }

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[HeartAndCheckpointSlicer] No se pudo obtener el TextureImporter para {assetPath}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = TargetPpu;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        int cols = width / cellWidth;
        int rows = height / cellHeight;

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        Dictionary<string, GUID> existingIds = new Dictionary<string, GUID>();
        foreach (SpriteRect existing in dataProvider.GetSpriteRects())
        {
            existingIds[existing.name] = existing.spriteID;
        }

        string baseName = Path.GetFileNameWithoutExtension(assetPath);
        List<SpriteRect> rects = new List<SpriteRect>();
        int index = 0;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                string spriteName = $"{baseName}_{index}";
                SpriteRect rect = new SpriteRect
                {
                    name = spriteName,
                    rect = new Rect(c * cellWidth, height - (r + 1) * cellHeight, cellWidth, cellHeight),
                    alignment = alignment,
                    pivot = pivot,
                    spriteID = existingIds.TryGetValue(spriteName, out GUID id) ? id : GUID.Generate()
                };
                rects.Add(rect);
                index++;
            }
        }

        dataProvider.SetSpriteRects(rects.ToArray());

        ISpriteNameFileIdDataProvider nameFileIds = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameFileIds != null)
        {
            List<SpriteNameFileIdPair> pairs = new List<SpriteNameFileIdPair>();
            foreach (SpriteRect rect in rects)
            {
                pairs.Add(new SpriteNameFileIdPair(rect.name, rect.spriteID));
            }
            nameFileIds.SetNameFileIdPairs(pairs);
        }

        dataProvider.Apply();
        importer.SaveAndReimport();
        Debug.Log($"[HeartAndCheckpointSlicer] {baseName} cortado en {rects.Count} cuadros.");
    }

    public static void ConfigureSingleSprite(string assetPath)
    {
        if (!File.Exists(assetPath)) return;

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = TargetPpu;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }
}
