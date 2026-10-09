using UnityEditor;
using UnityEngine;

/// <summary>
/// Aplica los valores por defecto de importación a los sprites nuevos bajo Assets/Art:
/// Sprite (2D and UI), Pixels Per Unit = 64, Filter Bilinear y Compression None.
/// Solo actúa la primera vez que se importa un archivo, así no pisa ajustes manuales posteriores.
/// </summary>
public class SpriteImportDefaults : AssetPostprocessor
{
    public const int PixelsPerUnit = 64;
    private const string ArtFolder = "Assets/Art/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(ArtFolder) || !assetImporter.importSettingsMissing)
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;
        Apply(importer);
    }

    /// <summary>Configura un importador con las reglas de sprites del proyecto.</summary>
    public static void Apply(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
    }
}
