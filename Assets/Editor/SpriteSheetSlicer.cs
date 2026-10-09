using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Corta automáticamente los spritesheets (una sola fila horizontal) en celdas de 256x256
/// con el pivote en "Bottom Center". Mantiene los IDs de los sprites al volver a cortar,
/// de modo que los AnimationClips existentes no pierdan sus referencias.
/// </summary>
public static class SpriteSheetSlicer
{
    public const int CellSize = 256;
    private const string WitchFolder = "Assets/Art/Characters/Witch";
    private const string WitchSheetFilter = "Witch_*.png";
    private static readonly Vector2 BottomCenterPivot = new Vector2(0.5f, 0f);

    [MenuItem("Brujas/Sprites/Cortar spritesheets de la bruja")]
    public static void SliceWitchSheets()
    {
        SliceFolder(WitchFolder, WitchSheetFilter);
    }

    /// <summary>Corta todas las hojas de una carpeta que coincidan con el filtro.</summary>
    public static void SliceFolder(string folder, string searchPattern)
    {
        string[] files = Directory.GetFiles(folder, searchPattern);
        foreach (string file in files)
        {
            string assetPath = file.Replace('\\', '/');
            SliceSheet(assetPath, CellSize, CellSize);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SpriteSheetSlicer] {files.Length} spritesheets cortados en celdas de {CellSize}x{CellSize}.");
    }

    /// <summary>Corta una hoja en celdas de tamaño fijo, de izquierda a derecha y de arriba abajo.</summary>
    public static void SliceSheet(string assetPath, int cellWidth, int cellHeight)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[SpriteSheetSlicer] No se encontró el importador de {assetPath}");
            return;
        }

        // Reglas de importación del proyecto + modo de sprites múltiples
        SpriteImportDefaults.Apply(importer);
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.SaveAndReimport();

        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        int columns = width / cellWidth;
        int rows = height / cellHeight;

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        // Se reutilizan los IDs de los sprites ya existentes (por nombre) para no romper referencias
        Dictionary<string, GUID> existingIds = new Dictionary<string, GUID>();
        foreach (SpriteRect existing in dataProvider.GetSpriteRects())
        {
            existingIds[existing.name] = existing.spriteID;
        }

        string baseName = Path.GetFileNameWithoutExtension(assetPath);
        List<SpriteRect> spriteRects = new List<SpriteRect>();
        int index = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                string spriteName = $"{baseName}_{index}";
                SpriteRect rect = new SpriteRect
                {
                    name = spriteName,
                    // Unity cuenta Y desde abajo: la primera fila es la de más arriba
                    rect = new Rect(column * cellWidth, height - (row + 1) * cellHeight, cellWidth, cellHeight),
                    alignment = SpriteAlignment.BottomCenter,
                    pivot = BottomCenterPivot,
                    spriteID = existingIds.TryGetValue(spriteName, out GUID id) ? id : GUID.Generate()
                };
                spriteRects.Add(rect);
                index++;
            }
        }

        dataProvider.SetSpriteRects(spriteRects.ToArray());

        // Asocia nombre e ID de cada sprite para que el fileID sea estable
        ISpriteNameFileIdDataProvider nameFileIds = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        List<SpriteNameFileIdPair> pairs = new List<SpriteNameFileIdPair>();
        foreach (SpriteRect rect in spriteRects)
        {
            pairs.Add(new SpriteNameFileIdPair(rect.name, rect.spriteID));
        }
        nameFileIds.SetNameFileIdPairs(pairs);

        dataProvider.Apply();
        importer.SaveAndReimport();
        Debug.Log($"[SpriteSheetSlicer] {baseName}: {spriteRects.Count} cuadros ({width}x{height}).");
    }
}
