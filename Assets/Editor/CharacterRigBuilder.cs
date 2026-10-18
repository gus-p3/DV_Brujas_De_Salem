using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Construye un prefab de rig por piezas a partir de su JSON y de la carpeta de piezas PNG.
/// Configura la importación de cada pieza (PPU y pivote del JSON) y crea la jerarquía de huesos.
/// </summary>
public static class CharacterRigBuilder
{
    private const string WitchJson = "Assets/Art/Characters/WitchRig/witch_rig.json";
    private const string WitchParts = "Assets/Art/Characters/WitchRig/Parts";
    private const string WitchPrefab = "Assets/Prefabs/WitchRig.prefab";
    private const string SortingLayer = "Gameplay";
    // Altura en juego ≈ 1.6 u (igual que el sprite anterior de la bruja: 256 px a 64 PPU con escala 0.45)
    private const float WitchScale = 1.12f;

    [MenuItem("Tools/Rig/Construir rig de la Bruja")]
    public static void BuildWitch()
    {
        GameObject prefab = Build(WitchJson, WitchParts, WitchPrefab, "WitchRig");
        if (prefab != null)
        {
            Selection.activeObject = prefab;
            Debug.Log($"[CharacterRigBuilder] Prefab creado: {WitchPrefab}");
        }
    }

    [MenuItem("Tools/Rig/Integrar rig en Witch.prefab")]
    public static void IntegrateIntoWitchPrefab()
    {
        const string witchPath = "Assets/Prefabs/Witch.prefab";
        GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WitchPrefab);
        if (rigPrefab == null) BuildWitch();
        rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WitchPrefab);

        GameObject root = PrefabUtility.LoadPrefabContents(witchPath);
        try
        {
            Transform old = root.transform.Find("Rig");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, root.transform);
            rig.name = "Rig";
            // Igual que el Visual anterior: los pies se hunden un poco en el pasto
            rig.transform.localPosition = new Vector3(0f, -0.06f, 0f);
            rig.transform.SetAsLastSibling();

            Animator visual = root.GetComponentInChildren<Animator>(true);
            RigSpriteProxy proxy = root.GetComponent<RigSpriteProxy>();
            if (proxy == null) proxy = root.AddComponent<RigSpriteProxy>();
            proxy.Setup(visual != null ? visual.GetComponent<SpriteRenderer>() : null, rig.GetComponent<WitchRigAnimator>());

            PrefabUtility.SaveAsPrefabAsset(root, witchPath);
            Debug.Log("[CharacterRigBuilder] Rig integrado en Witch.prefab");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    public static GameObject Build(string jsonPath, string partsFolder, string prefabPath, string rootName)
    {
        TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
        if (json == null)
        {
            Debug.LogError($"[CharacterRigBuilder] No se encontró {jsonPath}");
            return null;
        }
        CharacterRigData data = CharacterRigData.FromJson(json.text);

        ConfigureSprites(data, partsFolder);

        GameObject root = new GameObject(rootName);
        try
        {
            root.AddComponent<SortingGroup>().sortingLayerName = SortingLayer;
            BuildBones(data, partsFolder, root.transform);
            WitchRigAnimator animator = root.AddComponent<WitchRigAnimator>();
            animator.Configure(data.ppu, WitchScale);
            animator.SetFaceSprites(
                LoadSprite(partsFolder, "eye_open"), LoadSprite(partsFolder, "eye_half"),
                LoadSprite(partsFolder, "eye_closed"), LoadSprite(partsFolder, "mouth_smile"),
                LoadSprite(partsFolder, "mouth_o"));

            Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
            return PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static Sprite LoadSprite(string folder, string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{name}.png");
    }

    private static void ConfigureSprites(CharacterRigData data, string partsFolder)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { partsFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;

            string spriteName = Path.GetFileNameWithoutExtension(path);
            imp.GetSourceTextureWidthAndHeight(out int w, out int h);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            if (data.TryGetPivot(spriteName, out Vector2 px))
            {
                // JSON: px desde arriba a la izquierda -> Unity: normalizado desde abajo a la izquierda
                pivot = new Vector2(px.x / w, 1f - px.y / h);
            }

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = data.ppu;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.maxTextureSize = 512;

            TextureImporterSettings s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            s.spritePivot = pivot;
            s.spriteMeshType = SpriteMeshType.Tight;
            imp.SetTextureSettings(s);
            imp.SaveAndReimport();
        }
    }

    private static void BuildBones(CharacterRigData data, string partsFolder, Transform rigRoot)
    {
        Dictionary<string, Transform> made = new Dictionary<string, Transform>();
        Dictionary<string, float> worldRot = new Dictionary<string, float>();
        Dictionary<string, float> worldScale = new Dictionary<string, float>();

        // Los padres aparecen antes que los hijos en el JSON
        foreach (RigBoneDef b in data.bones)
        {
            GameObject go = new GameObject(b.name);
            Transform t = go.transform;
            Vector2 pos = new Vector2(b.pos[0], b.pos[1]);

            if (b.parent == null)
            {
                t.SetParent(rigRoot, false);
                t.localPosition = new Vector3(pos.x, -pos.y, 0f) / data.ppu;
                worldRot[b.name] = b.rot;
                worldScale[b.name] = b.scale;
            }
            else
            {
                RigBoneDef parent = data.Find(b.parent);
                t.SetParent(made[b.parent], false);
                Vector2 delta = (pos - new Vector2(parent.pos[0], parent.pos[1])) / data.ppu;
                Vector2 v = new Vector2(delta.x, -delta.y);
                // Llevar el desplazamiento al espacio local del padre (sin su rotación ni escala de reposo)
                v = Quaternion.Euler(0f, 0f, -worldRot[b.parent]) * v;
                v /= worldScale[b.parent];
                t.localPosition = v;
                worldRot[b.name] = worldRot[b.parent] + b.rot;
                worldScale[b.name] = worldScale[b.parent] * b.scale;
            }

            t.localRotation = Quaternion.Euler(0f, 0f, b.rot);
            t.localScale = new Vector3(b.scale, b.scale, 1f);

            if (!string.IsNullOrEmpty(b.sprite))
            {
                Sprite sprite = LoadSprite(partsFolder, b.sprite);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingLayerName = SortingLayer;
                sr.sortingOrder = b.order;
            }

            if (b.tip != null && b.tip.Length == 2 && b.pivot != null && b.pivot.Length == 2)
            {
                // Punto de apoyo (planta del pie) en el espacio local del hueso
                Transform tip = new GameObject(b.name + "_tip").transform;
                tip.SetParent(t, false);
                tip.localPosition = new Vector3(b.tip[0] - b.pivot[0], -(b.tip[1] - b.pivot[1]), 0f) / data.ppu;
            }

            made[b.name] = t;
        }
    }
}
