using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Configura los sprites de pociones (frascos e iconos como Single Sprite) y corta
/// las hojas de salpicadura (Splash_*) en 4 cuadros de 256x256 con PPU 64.
/// </summary>
public static class PotionArtImporter
{
    private const float TargetPpu = 64f;

    [MenuItem("Brujas/2. Sprites/Configurar pociones y salpicaduras")]
    public static void ImportAndSlicePotionAssets()
    {
        // 1. Frascos de items y proyectiles (Single)
        string[] singleItems = {
            "Assets/Art/Items/PotionItem_Blind.png",
            "Assets/Art/Items/PotionItem_Hypnotize.png",
            "Assets/Art/Items/PotionItem_Sleep.png",
            "Assets/Art/Items/PotionProjectile_Blind.png",
            "Assets/Art/Items/PotionProjectile_Hypnotize.png",
            "Assets/Art/Items/PotionProjectile_Sleep.png",
            "Assets/Art/Effects/Icon_Zzz.png",
            "Assets/Art/Effects/Icon_Spiral.png",
            "Assets/Art/Effects/Icon_Blinded.png"
        };

        foreach (string path in singleItems)
        {
            HeartAndCheckpointSlicer.ConfigureSingleSprite(path);
        }

        // 2. Salpicaduras múltiples (4 cuadros de 256x256, pivote Centro)
        string[] splashes = {
            "Assets/Art/Effects/Splash_Blind.png",
            "Assets/Art/Effects/Splash_Hypnotize.png",
            "Assets/Art/Effects/Splash_Sleep.png"
        };

        foreach (string path in splashes)
        {
            HeartAndCheckpointSlicer.SliceSpriteSheet(path, 256, 256, new Vector2(0.5f, 0.5f), SpriteAlignment.Center);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PotionArtImporter] Sprites de pociones y salpicaduras configurados exitosamente.");
    }
}
