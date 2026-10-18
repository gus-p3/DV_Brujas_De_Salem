using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Importa los fotogramas de los aldeanos (Assets/Art/Characters/Villagers/&lt;personaje&gt;/&lt;animación&gt;_NN.png),
/// crea un VillagerAnimationSet por personaje y lo aplica a los prefabs de aldeano y de guardia.
/// Los fotogramas los exporta ArtSource/Villagers/export_villagers.py.
/// </summary>
public static class VillagerSpriteSetBuilder
{
    private const string Root = "Assets/Art/Characters/Villagers";
    private const float PixelsPerUnit = 128f;
    private const float CanvasHeight = 320f;
    private const float PivotFromBottom = 8f;

    private static readonly string[] Anims =
        { "idle", "walk", "run", "notice", "scream", "blind", "hypnotize", "sleep", "action" };

    // personaje -> (prefab, prefab base si es variante, altura de iconos)
    private static readonly (string character, string prefab, string variantOf, float iconHeight)[] Characters =
    {
        ("farmer", "Assets/Prefabs/Villager.prefab", null, 2.2f),
        ("milkmaid", "Assets/Prefabs/VillagerF.prefab", null, 2.1f),
        ("preacher", "Assets/Prefabs/VillagerPreacher.prefab", "Assets/Prefabs/Villager.prefab", 2.4f),
        ("boy", "Assets/Prefabs/VillagerBoy.prefab", "Assets/Prefabs/Villager.prefab", 1.75f),
        ("guard", "Assets/Prefabs/Guard.prefab", null, 2.35f),
    };

    [MenuItem("Tools/Aldeanos/Importar animaciones y aplicar a prefabs")]
    public static void BuildAll()
    {
        foreach (var c in Characters)
        {
            VillagerAnimationSet set = BuildSet(c.character, c.iconHeight);
            if (set == null) continue;
            ApplyToPrefab(set, c.prefab, c.variantOf);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[VillagerSpriteSetBuilder] Animaciones de aldeanos importadas y aplicadas.");
    }

    private static VillagerAnimationSet BuildSet(string character, float iconHeight)
    {
        string folder = $"{Root}/{character}";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogWarning($"[VillagerSpriteSetBuilder] No existe {folder}");
            return null;
        }

        ConfigureImporters(folder);

        string setPath = $"{folder}/{character}_Animations.asset";
        VillagerAnimationSet set = AssetDatabase.LoadAssetAtPath<VillagerAnimationSet>(setPath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<VillagerAnimationSet>();
            AssetDatabase.CreateAsset(set, setPath);
        }

        foreach (string anim in Anims)
        {
            Sprite[] frames = AssetDatabase.FindAssets($"{anim}_ t:Sprite", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileNameWithoutExtension(p).StartsWith(anim + "_"))
                .OrderBy(p => p)
                .Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p))
                .ToArray();
            ClipFor(set, anim).frames = frames;
        }

        // El niño y el guardia persiguen con su caminata acelerada (sus hojas de carrera no eran utilizables)
        set.run.fps = character == "boy" ? 24f : character == "guard" ? 20f : 14f;
        set.iconHeight = iconHeight;
        EditorUtility.SetDirty(set);
        return set;
    }

    private static VillagerAnimationSet.Clip ClipFor(VillagerAnimationSet set, string anim)
    {
        switch (anim)
        {
            case "idle": return set.idle;
            case "walk": return set.walk;
            case "run": return set.run;
            case "notice": return set.notice;
            case "scream": return set.scream;
            case "blind": return set.blind;
            case "hypnotize": return set.hypnotize;
            case "sleep": return set.sleep;
            default: return set.action;
        }
    }

    private static void ConfigureImporters(string folder)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = PixelsPerUnit;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.maxTextureSize = 512;

            TextureImporterSettings s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            s.spritePivot = new Vector2(0.5f, PivotFromBottom / CanvasHeight);
            s.spriteMeshType = SpriteMeshType.Tight;
            imp.SetTextureSettings(s);
            imp.SaveAndReimport();
        }
    }

    private static void ApplyToPrefab(VillagerAnimationSet set, string prefabPath, string variantOf)
    {
        GameObject root;
        bool isNew = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null;
        if (isNew && variantOf != null)
        {
            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(variantOf);
            root = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            root.name = Path.GetFileNameWithoutExtension(prefabPath);
        }
        else
        {
            root = PrefabUtility.LoadPrefabContents(prefabPath);
        }

        try
        {
            Transform visual = root.transform.Find("Visual");
            if (visual == null)
            {
                Debug.LogWarning($"[VillagerSpriteSetBuilder] {prefabPath} no tiene objeto Visual");
                return;
            }

            // Animator antiguo fuera: los fotogramas los reproduce VillagerSpriteAnimator
            Animator animator = visual.GetComponent<Animator>();
            if (animator != null) Object.DestroyImmediate(animator, true);

            VillagerSpriteAnimator spriteAnimator = visual.GetComponent<VillagerSpriteAnimator>();
            if (spriteAnimator == null) spriteAnimator = visual.gameObject.AddComponent<VillagerSpriteAnimator>();
            spriteAnimator.Animations = set;

            SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
            if (sr != null && !set.idle.IsEmpty) sr.sprite = set.idle.frames[0];
            visual.localPosition = Vector3.zero;
            visual.localScale = Vector3.one;

            // Si root es una instancia del prefab base, esto crea una variante de prefab
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            if (isNew && variantOf != null) Object.DestroyImmediate(root);
            else PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
