using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Herramientas de construcción de niveles (Tools > Nivel):
/// - Reconstruir suelo: pone un GroundStrip en cada collider largo de suelo y genera su arte alineado.
/// - Anclar objetos al suelo: añade GroundAnchor a props, objetos y maizales, y los apoya en la superficie.
/// Pensadas para reutilizarse al crear niveles nuevos con las mismas piezas.
/// </summary>
public static class LevelTools
{
    private const string TilePath = "Assets/Art/Environment/Env_Ground_Tile.png";
    private const string FringePath = "Assets/Art/Environment/Env_Ground_Fringe.png";

    // Hundimiento (u) por tipo de objeto: los postes se clavan, los objetos se posan en el pasto
    private const float SinkPost = 0.15f;
    private const float SinkItem = 0.03f;
    private const float SinkCornStalk = 0.3f;
    private const float SinkCornCluster = 0.35f;

    private static readonly string[] AnchoredPrefabs =
    {
        "Assets/Prefabs/Checkpoint.prefab",
        "Assets/Prefabs/Collectible_Branch.prefab",
        "Assets/Prefabs/Collectible_Rope.prefab",
        "Assets/Prefabs/Collectible_Straw.prefab",
        "Assets/Prefabs/HeartPickup.prefab",
        "Assets/Prefabs/PotionPickup_Blind.prefab",
        "Assets/Prefabs/PotionPickup_Hypnotize.prefab",
        "Assets/Prefabs/PotionPickup_Sleep.prefab",
    };

    [MenuItem("Tools/Nivel/Reconstruir suelo")]
    public static void RebuildGround()
    {
        ConfigureGroundSprites();
        Sprite tile = AssetDatabase.LoadAssetAtPath<Sprite>(TilePath);
        Sprite fringe = AssetDatabase.LoadAssetAtPath<Sprite>(FringePath);
        int ground = LayerMask.NameToLayer("Ground");
        Physics2D.SyncTransforms();

        foreach (BoxCollider2D box in Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
        {
            // Suelos: colliders largos de la capa Ground sin sprite propio (las plataformas ya traen su arte)
            if (box.isTrigger || box.gameObject.layer != ground || box.bounds.size.x < 6f) continue;
            if (box.GetComponent<SpriteRenderer>() != null || !box.name.StartsWith("Ground")) continue;

            // Baldosas antiguas colocadas a mano
            List<GameObject> old = new List<GameObject>();
            foreach (Transform child in box.transform)
                if (child.name.StartsWith("Tile_")) old.Add(child.gameObject);
            foreach (GameObject go in old) Undo.DestroyObjectImmediate(go);

            GroundStrip strip = box.GetComponent<GroundStrip>();
            if (strip == null) strip = Undo.AddComponent<GroundStrip>(box.gameObject);
            strip.Configure(tile, fringe);
            strip.Rebuild();
            Debug.Log($"[LevelTools] Suelo reconstruido: {box.name} ({box.bounds.min.x:F1}..{box.bounds.max.x:F1})");
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Nivel/Anclar objetos al suelo")]
    public static void AnchorAll()
    {
        // 1) Prefabs de objetos y checkpoints
        foreach (string path in AnchoredPrefabs)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            GroundAnchor anchor = root.GetComponent<GroundAnchor>();
            if (anchor == null) anchor = root.AddComponent<GroundAnchor>();
            anchor.Sink = path.Contains("Checkpoint") ? SinkPost : SinkItem;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // 2) Objetos de escena: letreros, meta y tallos de maíz
        foreach (WorldSign sign in Object.FindObjectsByType<WorldSign>(FindObjectsSortMode.None))
            EnsureAnchor(sign.gameObject, SinkPost);
        foreach (LevelGoal goal in Object.FindObjectsByType<LevelGoal>(FindObjectsSortMode.None))
            foreach (SpriteRenderer sr in goal.GetComponentsInChildren<SpriteRenderer>())
                EnsureAnchor(sr.gameObject, SinkPost);
        foreach (CornFoliageFade field in Object.FindObjectsByType<CornFoliageFade>(FindObjectsSortMode.None))
            foreach (SpriteRenderer sr in field.GetComponentsInChildren<SpriteRenderer>())
                EnsureAnchor(sr.gameObject, sr.name.StartsWith("Cluster") ? SinkCornCluster : SinkCornStalk);

        // 3) Apoyar todo (las instancias de prefab heredan el componente recién añadido)
        int snapped = 0;
        foreach (GroundAnchor anchor in Object.FindObjectsByType<GroundAnchor>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(anchor.transform, "Anclar al suelo");
            if (anchor.Snap()) snapped++;
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[LevelTools] {snapped} objetos apoyados en el suelo.");
    }

    private static void EnsureAnchor(GameObject go, float sink)
    {
        GroundAnchor anchor = go.GetComponent<GroundAnchor>();
        if (anchor == null) anchor = Undo.AddComponent<GroundAnchor>(go);
        anchor.Sink = sink;
        EditorUtility.SetDirty(anchor);
    }

    private static void ConfigureGroundSprites()
    {
        foreach (string path in new[] { TilePath, FringePath })
        {
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 64f;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.mipmapEnabled = false;
            TextureImporterSettings s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect; // necesario para el modo mosaico
            s.spriteAlignment = (int)SpriteAlignment.Center;
            imp.SetTextureSettings(s);
            imp.SaveAndReimport();
        }
    }
}
