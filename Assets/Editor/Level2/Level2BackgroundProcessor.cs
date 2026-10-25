#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Procesador reproducible de editor para el fondo del Nivel 2 a partir de
/// "Assets/Screenshots/NIVEL2/Aldea medieval al atardecer.png".
/// Conserva intacto el archivo original en Screenshots, genera las 3 capas
/// continuas sin costuras en Assets/Art/Environment/Level2/ y las aplica
/// en Environment/Parallax respetando la composicion de referencia.
/// </summary>
public static class Level2BackgroundProcessor
{
    public const string SOURCE_ASSET_PATH = "Assets/Screenshots/NIVEL2/Aldea medieval al atardecer.png";
    public const string SKY_LAYER_PATH = "Assets/Art/Environment/Level2/Level2_Sky_Far.png";
    public const string VILLAGE_A_PATH = "Assets/Art/Environment/Level2/Level2_Village_Far.png";
    public const string VILLAGE_B_PATH = "Assets/Art/Environment/Level2/Level2_Village_Far_VarB.png";
    public const string FACADES_MID_PATH = "Assets/Art/Environment/Level2/Level2_Facades_Mid.png";
    private const string LEVEL2_SCENE_PATH = "Assets/Scenes/Level2.unity";

    private const int TILE_W = 2172;
    private const int TILE_H = 744;
    private const int SKY_H = 1320;
    private const int BLEND_W = 300;
    private const float PPU = 100f;

    // Parametros exactos de la composicion de referencia en Level2.unity
    private const float SCALE_UNIFORM = 2.00f;
    private const int COPY_START_INDEX = -3;
    private const int COPY_END_INDEX = 10; // 14 copias: Copy_0 .. Copy_13

    private const float SKY_POS_Y = 6.20f;
    private const float VILLAGE_POS_Y = 4.40f;
    private const float FACADES_POS_Y = 3.60f;

    private const float SKY_PARALLAX = 0.88f;
    private const float VILLAGE_PARALLAX = 0.70f;
    private const float FACADES_PARALLAX = 0.42f;

    private const int SKY_SORT_ORDER = -20;
    private const int VILLAGE_SORT_ORDER = -15;
    private const int FACADES_SORT_ORDER = -10;

    // Tinte calido suave para armonizar con el atardecer de Level 2 sin competir con el primer plano
    private static readonly Color SKY_TINT = new Color(0.98f, 0.94f, 0.90f, 1.00f);
    private static readonly Color VILLAGE_TINT = new Color(0.95f, 0.91f, 0.87f, 1.00f);
    private static readonly Color FACADES_TINT = new Color(0.92f, 0.88f, 0.84f, 1.00f);

    [MenuItem("Tools/Nivel 2/Fondo/1. Importar y Procesar 'Aldea medieval al atardecer.png'")]
    public static void ImportAndProcessSourceImage()
    {
        if (!File.Exists(SOURCE_ASSET_PATH))
        {
            Debug.LogError($"[Level2BackgroundProcessor] No existe el archivo fuente: {SOURCE_ASSET_PATH}");
            return;
        }

        // 1. Configurar Import Settings del archivo fuente sin renombrarlo ni cambiar su GUID
        ConfigureSourceTextureImporter(SOURCE_ASSET_PATH);

        // 2. Leer bytes del PNG original directamente para no depender de isReadable ni compresion previa
        byte[] rawBytes = File.ReadAllBytes(SOURCE_ASSET_PATH);
        Texture2D srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!srcTex.LoadImage(rawBytes))
        {
            UnityEngine.Object.DestroyImmediate(srcTex);
            Debug.LogError($"[Level2BackgroundProcessor] No se pudo decodificar {SOURCE_ASSET_PATH}");
            return;
        }

        int srcW = srcTex.width;
        int srcH = srcTex.height;
        Color[] srcPixels = srcTex.GetPixels();
        UnityEngine.Object.DestroyImmediate(srcTex);

        // 3. Remuestrear a (TILE_W + BLEND_W) x TILE_H en coordenadas top-down (y=0 arriba, y=TILE_H-1 abajo)
        int extW = TILE_W + BLEND_W;
        Vector4[,] work = ResampleTopDown(srcPixels, srcW, srcH, extW, TILE_H);

        // 4. Igualar perfil horizontal de tono e iluminacion entre borde izquierdo y derecho
        HarmonizeHorizontalIllumination(work, extW, TILE_H);

        // 5. Generar mosaico horizontalmente continuo (TILE_W x TILE_H) mediante cross-fade 2D de franjas
        Vector4[,] seamless = BuildSeamlessTile(work, extW, TILE_W, TILE_H, BLEND_W);

        // 6. Generar Capa 1: Level2_Sky_Far.png (TILE_W x SKY_H) con cobertura extendida para vuelo alto
        GenerateSkyLayer(seamless, TILE_W, TILE_H, SKY_H, SKY_LAYER_PATH);

        // 7. Generar Capa 2: Level2_Village_Far.png (Var A) y Level2_Village_Far_VarB.png (Var B)
        GenerateVillageLayers(seamless, TILE_W, TILE_H, VILLAGE_A_PATH, VILLAGE_B_PATH);

        // 8. Generar Capa 3: Level2_Facades_Mid.png (muro de piedra, setos y arbustos inferiores con borde suave)
        GenerateFacadesMidLayer(seamless, TILE_W, TILE_H, FACADES_MID_PATH);

        AssetDatabase.Refresh();

        // Configurar importadores de las 4 texturas generadas
        // En Sky_Far ajustamos el pivote Y a 0.3787879f para que en posY=6.20, scale=2.00 cubra de y=-3.80 a y=+22.60
        ConfigureGeneratedLayerImporter(SKY_LAYER_PATH, new Vector2(0.5f, 1000f / 2640f));
        ConfigureGeneratedLayerImporter(VILLAGE_A_PATH, new Vector2(0.5f, 0.5f));
        ConfigureGeneratedLayerImporter(VILLAGE_B_PATH, new Vector2(0.5f, 0.5f));
        ConfigureGeneratedLayerImporter(FACADES_MID_PATH, new Vector2(0.5f, 0.5f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Level2BackgroundProcessor] Capas generadas desde '{SOURCE_ASSET_PATH}' ({srcW}x{srcH}) -> Sky({TILE_W}x{SKY_H}), VillageA/B({TILE_W}x{TILE_H}), FacadesMid({TILE_W}x{TILE_H}).");
    }

    [MenuItem("Tools/Nivel 2/Fondo/2. Aplicar Fondo a Level2.unity (Composicion de Referencia)")]
    public static void ApplyBackgroundToLevel2()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.path != LEVEL2_SCENE_PATH)
        {
            EditorSceneManager.OpenScene(LEVEL2_SCENE_PATH, OpenSceneMode.Single);
        }

        Sprite skySprite = LoadSingleSprite(SKY_LAYER_PATH);
        Sprite villageASprite = LoadSingleSprite(VILLAGE_A_PATH);
        Sprite villageBSprite = LoadSingleSprite(VILLAGE_B_PATH);
        Sprite facadesMidSprite = LoadSingleSprite(FACADES_MID_PATH);

        if (skySprite == null || villageASprite == null || facadesMidSprite == null)
        {
            ImportAndProcessSourceImage();
            skySprite = LoadSingleSprite(SKY_LAYER_PATH);
            villageASprite = LoadSingleSprite(VILLAGE_A_PATH);
            villageBSprite = LoadSingleSprite(VILLAGE_B_PATH);
            facadesMidSprite = LoadSingleSprite(FACADES_MID_PATH);
        }

        if (skySprite == null || villageASprite == null || facadesMidSprite == null)
        {
            Debug.LogError("[Level2BackgroundProcessor] No se pudieron cargar los sprites procesados del fondo.");
            return;
        }

        GameObject envRoot = GameObject.Find("Environment");
        if (envRoot == null) envRoot = new GameObject("Environment");

        Transform parallaxTr = envRoot.transform.Find("Parallax");
        GameObject parallaxRoot = parallaxTr != null ? parallaxTr.gameObject : new GameObject("Parallax");
        parallaxRoot.transform.SetParent(envRoot.transform, false);
        parallaxRoot.transform.position = Vector3.zero;
        parallaxRoot.transform.rotation = Quaternion.identity;
        parallaxRoot.transform.localScale = Vector3.one;

        // Eliminar cualquier capa residual que no forme parte de las 3 capas de la composicion de referencia
        List<GameObject> extraLayers = new List<GameObject>();
        foreach (Transform ch in parallaxRoot.transform)
        {
            if (ch.name != "Sky_Far" && ch.name != "Village_Far" && ch.name != "Facades_Mid")
            {
                extraLayers.Add(ch.gameObject);
            }
        }
        foreach (GameObject go in extraLayers)
        {
            UnityEngine.Object.DestroyImmediate(go);
        }

        BuildReferenceParallaxLayer(
            parallaxRoot.transform,
            "Sky_Far",
            skySprite,
            skySprite,
            SKY_PARALLAX,
            SKY_SORT_ORDER,
            SKY_POS_Y,
            SCALE_UNIFORM,
            SKY_TINT);

        BuildReferenceParallaxLayer(
            parallaxRoot.transform,
            "Village_Far",
            villageASprite,
            villageBSprite != null ? villageBSprite : villageASprite,
            VILLAGE_PARALLAX,
            VILLAGE_SORT_ORDER,
            VILLAGE_POS_Y,
            SCALE_UNIFORM,
            VILLAGE_TINT);

        BuildReferenceParallaxLayer(
            parallaxRoot.transform,
            "Facades_Mid",
            facadesMidSprite,
            facadesMidSprite,
            FACADES_PARALLAX,
            FACADES_SORT_ORDER,
            FACADES_POS_Y,
            SCALE_UNIFORM,
            FACADES_TINT);

        Physics2D.SyncTransforms();
        Scene sc = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(sc);
        EditorSceneManager.SaveScene(sc);

        Debug.Log("[Level2BackgroundProcessor] Fondo 'Aldea medieval al atardecer.png' aplicado en Level2.unity conservando la composicion de referencia.");
    }

    private static void BuildReferenceParallaxLayer(
        Transform parent,
        string layerName,
        Sprite spriteA,
        Sprite spriteB,
        float parallaxFactor,
        int sortingOrder,
        float posY,
        float uniformScale,
        Color tint)
    {
        Transform layerTr = parent.Find(layerName);
        GameObject layerGO = layerTr != null ? layerTr.gameObject : new GameObject(layerName);
        layerGO.transform.SetParent(parent, false);
        layerGO.transform.position = new Vector3(0f, posY, 0f);
        layerGO.transform.localRotation = Quaternion.identity;
        layerGO.transform.localScale = Vector3.one;

        // Asegurar que no existan colliders en el padre
        foreach (Collider2D col in layerGO.GetComponents<Collider2D>())
        {
            UnityEngine.Object.DestroyImmediate(col);
        }

        for (int i = layerGO.transform.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(layerGO.transform.GetChild(i).gameObject);
        }

        SpriteRenderer parentSr = layerGO.GetComponent<SpriteRenderer>();
        if (parentSr != null) UnityEngine.Object.DestroyImmediate(parentSr);

        ParallaxLayer pl = layerGO.GetComponent<ParallaxLayer>();
        if (pl == null) pl = layerGO.AddComponent<ParallaxLayer>();
        pl.Setup(parallaxFactor, false, true);

        float tileWorldWidth = (spriteA.rect.width / spriteA.pixelsPerUnit) * uniformScale;

        for (int i = COPY_START_INDEX; i <= COPY_END_INDEX; i++)
        {
            int copyIndex = i - COPY_START_INDEX;
            GameObject copy = new GameObject($"Copy_{copyIndex}");
            copy.transform.SetParent(layerGO.transform, false);
            copy.transform.localPosition = new Vector3(i * tileWorldWidth, 0f, 0f);
            copy.transform.localRotation = Quaternion.identity;
            copy.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);

            SpriteRenderer sr = copy.AddComponent<SpriteRenderer>();
            sr.sprite = (Mathf.Abs(i) % 2 == 0) ? spriteA : spriteB;
            sr.drawMode = SpriteDrawMode.Simple;
            sr.color = tint;
            sr.sortingLayerName = "Background";
            sr.sortingOrder = sortingOrder;
        }

        pl.CalculateSpriteWidth();
        EditorUtility.SetDirty(layerGO);
    }

    public static void ConfigureSourceTextureImporter(string assetPath)
    {
        TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti == null) return;

        TextureImporterSettings settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spritePivot = new Vector2(0.5f, 0.5f);
        settings.spritePixelsPerUnit = PPU;
        settings.filterMode = FilterMode.Bilinear;
        settings.wrapMode = TextureWrapMode.Clamp;
        settings.alphaIsTransparency = true;
        settings.mipmapEnabled = false;
        ti.SetTextureSettings(settings);

        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = PPU;
        ti.filterMode = FilterMode.Bilinear;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.maxTextureSize = 4096;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();
    }

    private static void ConfigureGeneratedLayerImporter(string assetPath, Vector2 pivot)
    {
        TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti == null) return;

        TextureImporterSettings settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spritePixelsPerUnit = PPU;
        settings.filterMode = FilterMode.Bilinear;
        settings.wrapMode = TextureWrapMode.Clamp;
        settings.alphaIsTransparency = true;
        settings.mipmapEnabled = false;
        ti.SetTextureSettings(settings);

        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = PPU;
        ti.filterMode = FilterMode.Bilinear;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.maxTextureSize = 4096;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();
    }

    private static Vector4[,] ResampleTopDown(Color[] srcBottomUp, int srcW, int srcH, int dstW, int dstH)
    {
        Vector4[,] dst = new Vector4[dstH, dstW];
        for (int y = 0; y < dstH; y++)
        {
            float vTop = (y / (float)(dstH - 1)) * (srcH - 1);
            // En Texture2D.GetPixels(), la fila 0 es la inferior; invertimos para trabajar en top-down (y=0 arriba)
            float srcY = (srcH - 1) - vTop;
            int y0 = Mathf.Clamp(Mathf.FloorToInt(srcY), 0, srcH - 1);
            int y1 = Mathf.Clamp(y0 + 1, 0, srcH - 1);
            float fy = srcY - y0;

            for (int x = 0; x < dstW; x++)
            {
                float srcX = (x / (float)(dstW - 1)) * (srcW - 1);
                int x0 = Mathf.Clamp(Mathf.FloorToInt(srcX), 0, srcW - 1);
                int x1 = Mathf.Clamp(x0 + 1, 0, srcW - 1);
                float fx = srcX - x0;

                Color c00 = srcBottomUp[y0 * srcW + x0];
                Color c10 = srcBottomUp[y0 * srcW + x1];
                Color c01 = srcBottomUp[y1 * srcW + x0];
                Color c11 = srcBottomUp[y1 * srcW + x1];

                Color c0 = Color.Lerp(c00, c10, fx);
                Color c1 = Color.Lerp(c01, c11, fx);
                Color c = Color.Lerp(c0, c1, fy);
                dst[y, x] = new Vector4(c.r * 255f, c.g * 255f, c.b * 255f, 255f);
            }
        }
        return dst;
    }

    private static void HarmonizeHorizontalIllumination(Vector4[,] img, int w, int h)
    {
        int stripW = 220;
        Vector3[] leftRaw = new Vector3[h];
        Vector3[] rightRaw = new Vector3[h];

        for (int y = 0; y < h; y++)
        {
            Vector3 lSum = Vector3.zero;
            Vector3 rSum = Vector3.zero;
            for (int x = 0; x < stripW; x++)
            {
                Vector4 lp = img[y, x];
                Vector4 rp = img[y, w - stripW + x];
                lSum += new Vector3(lp.x, lp.y, lp.z);
                rSum += new Vector3(rp.x, rp.y, rp.z);
            }
            leftRaw[y] = lSum / stripW;
            rightRaw[y] = rSum / stripW;
        }

        // Suavizado vertical con ventana de 65 px para evitar cualquier banda horizontal
        int radius = 32;
        Vector3[] diffSmooth = new Vector3[h];
        for (int y = 0; y < h; y++)
        {
            Vector3 lAvg = Vector3.zero;
            Vector3 rAvg = Vector3.zero;
            int count = 0;
            for (int dy = -radius; dy <= radius; dy++)
            {
                int yy = Mathf.Clamp(y + dy, 0, h - 1);
                lAvg += leftRaw[yy];
                rAvg += rightRaw[yy];
                count++;
            }
            diffSmooth[y] = (rAvg - lAvg) / count;
        }

        for (int y = 0; y < h; y++)
        {
            // Peso vertical: 1.0 en cielo/montañas (y < 340), atenuandose suavemente en el muro inferior
            float vWeight = y < 340 ? 1.0f : Mathf.Clamp(1.0f - (y - 340f) / 260f, 0.35f, 1.0f);
            Vector3 diff = diffSmooth[y] * vWeight;

            for (int x = 0; x < w; x++)
            {
                float ramp = 0.5f - (x / (float)(w - 1));
                Vector4 p = img[y, x];
                p.x = Mathf.Clamp(p.x + ramp * diff.x, 0f, 255f);
                p.y = Mathf.Clamp(p.y + ramp * diff.y, 0f, 255f);
                p.z = Mathf.Clamp(p.z + ramp * diff.z, 0f, 255f);
                img[y, x] = p;
            }
        }
    }

    private static Vector4[,] BuildSeamlessTile(Vector4[,] src, int extW, int tileW, int h, int blendW)
    {
        Vector4[,] tile = new Vector4[h, tileW];
        int copyLimit = tileW - blendW;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < copyLimit; x++)
            {
                tile[y, x] = src[y, x + blendW];
            }

            for (int i = 0; i < blendW; i++)
            {
                int x = copyLimit + i;
                float t = (i + 1f) / (blendW + 1f);
                float alpha = 0.5f * (1f - Mathf.Cos(Mathf.PI * t));
                Vector4 rightSlice = src[y, x + blendW];
                Vector4 leftSlice = src[y, i];
                tile[y, x] = Vector4.Lerp(rightSlice, leftSlice, alpha);
            }

            // Garantizar coincidencia exacta en el pixel de borde para filtrado bilineal
            tile[y, tileW - 1] = tile[y, 0];
        }
        return tile;
    }

    private static void GenerateSkyLayer(Vector4[,] seamless, int w, int srcH, int skyH, string outPath)
    {
        // En Level2_Sky_Far.png (2172 x 1320):
        // - Las filas y = 0 .. 539 extienden suavemente el cenit del cielo hacia arriba (hasta world Y = +22.60 u)
        // - Las filas y = 540 .. 900 contienen la banda superior de cielo y nubes de "Aldea medieval al atardecer.png" (y_src = 0..112)
        // - Las filas y = 901 .. 1319 descienden con un gradiente horizontalmente uniforme (0 rayas verticales)
        Vector4[,] sky = new Vector4[skyH, w];

        Vector3 topZenithMean = Vector3.zero;
        Vector3 lowerSkyMean = Vector3.zero;
        for (int x = 0; x < w; x++)
        {
            Vector4 pTop = seamless[2, x];
            topZenithMean += new Vector3(pTop.x, pTop.y, pTop.z);
            Vector4 pLow = seamless[108, x];
            lowerSkyMean += new Vector3(pLow.x, pLow.y, pLow.z);
        }
        topZenithMean /= w;
        lowerSkyMean /= w;

        Vector3 deepZenith = new Vector3(
            Mathf.Clamp(topZenithMean.x * 0.76f, 0f, 255f),
            Mathf.Clamp(topZenithMean.y * 0.82f, 0f, 255f),
            Mathf.Clamp(topZenithMean.z * 0.94f, 0f, 255f));
        Vector3 warmHorizon = new Vector3(244f, 206f, 162f);

        int cloudStartY = 540;
        int cloudEndY = 900;

        for (int y = 0; y < skyH; y++)
        {
            if (y < cloudStartY)
            {
                float t = y / (float)cloudStartY;
                float smoothT = t * t * (3f - 2f * t);
                // Fundir hacia la fila 0 real solo en los ultimos 70 px antes de cloudStartY para evitar rayas verticales
                float detailFade = Mathf.Clamp01((y - (cloudStartY - 70f)) / 70f);
                detailFade = detailFade * detailFade * (3f - 2f * detailFade);

                Vector3 baseGrad = Vector3.Lerp(deepZenith, topZenithMean, smoothT);
                for (int x = 0; x < w; x++)
                {
                    Vector4 row0 = seamless[0, x];
                    Vector3 row0Rgb = new Vector3(row0.x, row0.y, row0.z);
                    Vector3 c = Vector3.Lerp(baseGrad, row0Rgb, detailFade);
                    sky[y, x] = new Vector4(c.x, c.y, c.z, 255f);
                }
            }
            else if (y <= cloudEndY)
            {
                float u = (y - cloudStartY) / (float)(cloudEndY - cloudStartY);
                float srcY = u * 112f; // y_src = 0 .. 112 (puro cielo y nubes altas, sin tocar el campanario)
                int y0 = Mathf.Clamp(Mathf.FloorToInt(srcY), 0, 112);
                int y1 = Mathf.Clamp(y0 + 1, 0, 112);
                float fy = srcY - y0;

                // En la parte baja de la banda de nubes (u = 0.55 .. 1.0), fundir al 100% con lowerSkyMean
                float horizonFade = Mathf.Clamp01((u - 0.55f) / 0.45f);
                horizonFade = horizonFade * horizonFade * (3f - 2f * horizonFade);

                // En la parte alta (u = 0.0 .. 0.12), asegurar union suave con el cenit
                for (int x = 0; x < w; x++)
                {
                    Vector4 c = Vector4.Lerp(seamless[y0, x], seamless[y1, x], fy);
                    Vector3 rgb = new Vector3(c.x, c.y, c.z);
                    rgb = Vector3.Lerp(rgb, lowerSkyMean, horizonFade);
                    sky[y, x] = new Vector4(rgb.x, rgb.y, rgb.z, 255f);
                }
            }
            else
            {
                float t = (y - cloudEndY) / (float)(skyH - 1 - cloudEndY);
                float smoothT = t * t * (3f - 2f * t);
                Vector3 c = Vector3.Lerp(lowerSkyMean, warmHorizon, smoothT * 0.78f);
                for (int x = 0; x < w; x++)
                {
                    sky[y, x] = new Vector4(c.x, c.y, c.z, 255f);
                }
            }
            sky[y, w - 1] = sky[y, 0];
        }

        SaveTopDownTexture(sky, w, skyH, outPath);
    }

    private static void GenerateVillageLayers(Vector4[,] seamless, int w, int h, string pathA, string pathB)
    {
        Vector4[,] villageA = new Vector4[h, w];
        Vector4[,] villageB = new Vector4[h, w];

        for (int y = 0; y < h; y++)
        {
            // Degradado suave de alfa en la banda superior del cielo (y = 14 .. 126) por encima del campanario (y=148)
            // para que las nubes de Sky_Far se desplacen con su propio paralaje sin corte horizontal
            float alpha = 255f;
            if (y < 126)
            {
                float t = Mathf.Clamp01((y - 14f) / 112f);
                alpha = 255f * (t * t * (3f - 2f * t));
            }

            // En la variante B aplicamos una sutil modulacion calida de atardecer en el interior del tile
            // (con mascara sin^2(pi*x) = 0 en los bordes para mantener costuras 100% identicas a la variante A)
            float vertMask = Mathf.Clamp01(1f - Mathf.Abs(y - 430f) / 210f);

            for (int x = 0; x < w; x++)
            {
                Vector4 p = seamless[y, x];
                p.w = alpha;
                villageA[y, x] = p;

                float xn = x / (float)(w - 1);
                float s = Mathf.Sin(Mathf.PI * xn);
                float mod = (s * s) * vertMask;

                Vector4 pb = p;
                pb.x = Mathf.Clamp(p.x + mod * 16f, 0f, 255f);
                pb.y = Mathf.Clamp(p.y + mod * 7f, 0f, 255f);
                pb.z = Mathf.Clamp(p.z - mod * 12f, 0f, 255f);
                villageB[y, x] = pb;
            }

            villageA[y, w - 1] = villageA[y, 0];
            villageB[y, 0] = villageA[y, 0];
            villageB[y, w - 1] = villageA[y, 0];
        }

        SaveTopDownTexture(villageA, w, h, pathA);
        SaveTopDownTexture(villageB, w, h, pathB);
    }

    private static void GenerateFacadesMidLayer(Vector4[,] seamless, int w, int h, string outPath)
    {
        // Extrae la banda inferior de "Aldea medieval al atardecer.png" (setos verdes, muro de piedra gris
        // y arbustos de base: y_src = 532 .. 743) con una transicion suave de alfa en el borde superior (532..572)
        // para que se desplace en primer plano de fondo (parallaxFactor = 0.42, order = -10) sin cortes.
        Vector4[,] mid = new Vector4[h, w];
        int shiftUpPx = 40; // Compensa la diferencia entre posY=3.60 (Facades_Mid) y posY=4.40 (Village_Far): 0.80u = 40px

        for (int y = 0; y < h; y++)
        {
            int srcY = y + shiftUpPx;
            if (srcY < 532)
            {
                for (int x = 0; x < w; x++)
                {
                    mid[y, x] = Vector4.zero;
                }
                continue;
            }

            // Si srcY supera h-1 (en la franja subterranea inferior), reflejar verticalmente el follaje en vez de repetir la ultima fila
            int sampledY = srcY < h ? srcY : Mathf.Clamp((h - 1) - (srcY - (h - 1)), 660, h - 1);
            float alpha = 255f;
            if (srcY < 572)
            {
                float t = Mathf.Clamp01((srcY - 532f) / 40f);
                alpha = 255f * (t * t * (3f - 2f * t));
            }

            for (int x = 0; x < w; x++)
            {
                Vector4 p = seamless[sampledY, x];
                p.w = alpha;
                mid[y, x] = p;
            }
            mid[y, w - 1] = mid[y, 0];
        }

        SaveTopDownTexture(mid, w, h, outPath);
    }

    private static void SaveTopDownTexture(Vector4[,] topDown, int w, int h, string assetPath)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[w * h];

        for (int yTop = 0; yTop < h; yTop++)
        {
            int yBottom = (h - 1) - yTop;
            int rowOffset = yBottom * w;
            for (int x = 0; x < w; x++)
            {
                Vector4 c = topDown[yTop, x];
                byte r = (byte)Mathf.Clamp(Mathf.RoundToInt(c.x), 0, 255);
                byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(c.y), 0, 255);
                byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(c.z), 0, 255);
                byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(c.w), 0, 255);
                pixels[rowOffset + x] = new Color32(r, g, b, a);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        byte[] png = tex.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(tex);

        string dir = Path.GetDirectoryName(assetPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllBytes(assetPath, png);
    }

    private static Sprite LoadSingleSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null) return s;
        UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (UnityEngine.Object o in all)
        {
            if (o is Sprite sp) return sp;
        }
        return null;
    }
}
#endif
