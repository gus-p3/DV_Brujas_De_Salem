using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Aplica los pivotes recalculados por ArtSource/replace_assets.py (sprites cuyo lienzo creció al reemplazar el arte),
/// para que la superficie caminable y la base de cada objeto queden en la misma posición del mundo.
/// </summary>
public static class ArtPivotApplier
{
    private const string PivotsFile = "ArtSource/replace_assets_pivots.json";

    [Serializable]
    private class PivotList
    {
        public PivotItem[] items;
    }

    [Serializable]
    private class PivotItem
    {
        public string path;
        public float x;
        public float y;
    }

    [MenuItem("Tools/Arte/Aplicar pivotes del reemplazo de assets")]
    public static void Apply()
    {
        if (!File.Exists(PivotsFile))
        {
            Debug.LogWarning($"[ArtPivotApplier] No existe {PivotsFile}");
            return;
        }
        PivotList list = JsonUtility.FromJson<PivotList>(File.ReadAllText(PivotsFile));
        foreach (PivotItem item in list.items)
        {
            TextureImporter imp = AssetImporter.GetAtPath(item.path) as TextureImporter;
            if (imp == null) continue;
            TextureImporterSettings s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            s.spritePivot = new Vector2(item.x, item.y);
            imp.SetTextureSettings(s);
            imp.SaveAndReimport();
            Debug.Log($"[ArtPivotApplier] {item.path} -> pivote ({item.x:F3}, {item.y:F3})");
        }
    }
}
