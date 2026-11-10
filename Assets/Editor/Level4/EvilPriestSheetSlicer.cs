using UnityEngine;
using UnityEditor;
using System.IO;

namespace Salem.Level4
{
    /// <summary>
    /// Corta la spritesheet del Sacerdote Maligno (4 columnas × 4 filas) en frames individuales
    /// y los guarda en Assets/Art/Characters/Level4/EvilPriest/.
    ///
    /// Uso: coloca la hoja en Assets/Art/Characters/Level4/EvilPriest_Sheet.png
    ///      luego abre el menú  Salem ▸ Slice Evil Priest Sheet
    ///
    /// Nomenclatura de salida:
    ///   Fila 0 → idle_00.png … idle_03.png
    ///   Fila 1 → run_00.png  … run_03.png
    ///   Fila 2 → throw_00.png… throw_03.png
    ///   Fila 3 → attack_00.png…attack_03.png
    /// </summary>
    public static class EvilPriestSheetSlicer
    {
        private const string SheetPath  = "Assets/Art/Characters/Level4/EvilPriest_Sheet.png";
        private const string OutputDir  = "Assets/Art/Characters/Level4/EvilPriest";
        private const int    Cols       = 4;
        private const int    Rows       = 4;

        private static readonly string[] RowNames = { "idle", "run", "throw", "attack" };

        [MenuItem("Salem/Slice Evil Priest Sheet")]
        public static void SliceSheet()
        {
            var importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[EvilPriestSheetSlicer] No se encontró la hoja en '{SheetPath}'. " +
                               "Coloca la imagen del sacerdote maligno ahí y vuelve a intentarlo.");
                return;
            }

            // Forzar lectura/escritura para poder leer los píxeles
            bool wasReadable = importer.isReadable;
            importer.isReadable        = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode        = FilterMode.Point;
            importer.SaveAndReimport();

            var sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath);
            if (sheet == null)
            {
                Debug.LogError("[EvilPriestSheetSlicer] No se pudo cargar la textura.");
                RestoreImporter(importer, wasReadable);
                return;
            }

            if (!AssetDatabase.IsValidFolder(OutputDir))
                AssetDatabase.CreateFolder("Assets/Art/Characters/Level4", "EvilPriest");

            int cellW = sheet.width  / Cols;
            int cellH = sheet.height / Rows;

            for (int row = 0; row < Rows; row++)
            {
                string animName = RowNames[row];
                for (int col = 0; col < Cols; col++)
                {
                    // La fila 0 es la parte superior de la imagen (Y invertido en Texture2D)
                    int x = col * cellW;
                    int y = sheet.height - (row + 1) * cellH;

                    Color[] pixels = sheet.GetPixels(x, y, cellW, cellH);
                    var frameTex = new Texture2D(cellW, cellH, TextureFormat.RGBA32, false);
                    frameTex.SetPixels(pixels);
                    frameTex.Apply();

                    string fileName  = $"{animName}_{col:D2}.png";
                    string filePath  = Path.Combine(Application.dataPath,
                        "Art/Characters/Level4/EvilPriest", fileName);
                    File.WriteAllBytes(filePath, frameTex.EncodeToPNG());
                    Object.DestroyImmediate(frameTex);
                }
            }

            // También guardar holy_water.png (un cuadrado azul 32×32 de placeholder si no existe)
            string hwPath = Path.Combine(Application.dataPath, "Art/Characters/Level4/EvilPriest", "holy_water.png");
            if (!File.Exists(hwPath))
            {
                var hw = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                var hwPixels = new Color[32 * 32];
                Color glowBlue = new Color(0.6f, 0.85f, 1f, 0.9f);
                for (int i = 0; i < hwPixels.Length; i++) hwPixels[i] = glowBlue;
                hw.SetPixels(hwPixels);
                hw.Apply();
                File.WriteAllBytes(hwPath, hw.EncodeToPNG());
                Object.DestroyImmediate(hw);
            }

            RestoreImporter(importer, wasReadable);
            AssetDatabase.Refresh();

            int total = Rows * Cols;
            Debug.Log($"[EvilPriestSheetSlicer] ¡Listo! Se exportaron {total} frames a '{OutputDir}'.");
            EditorUtility.DisplayDialog("Sacerdote Maligno",
                $"Se cortaron {total} frames en:\n{OutputDir}\n\n" +
                "Ahora reconstruye el Nivel 4 con Salem ▸ Build Level 4 Scene.", "Entendido");
        }

        private static void RestoreImporter(TextureImporter imp, bool wasReadable)
        {
            imp.isReadable = wasReadable;
            imp.SaveAndReimport();
        }
    }
}
