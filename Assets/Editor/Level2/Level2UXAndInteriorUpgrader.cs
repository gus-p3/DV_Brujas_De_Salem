#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Level2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Aplica la actualización integral de Nivel 2:
/// 1) Reimportación de sprites de interiores (cortes arquitectónicos completos RoomCutaway_House1..4),
///    vigas estructurales, arco de sótano felino, llaves, ingredientes y puertas, más animaciones fluidas de aldeanos.
/// 2) Asignación de los nuevos clips de sonido (.wav) al AudioManager.
/// 3) Sustitución de las cuadrículas de bloques Wall_r_c por cortes arquitectónicos coloniales completos
///    y composición con props auténticos de Cainos (TX Village Props + TX Chest Animation).
/// 4) Sustitución del 100 % de barriles, cajas y cofres (exteriores e interiores) por los diseños de la librería Cainos.
/// 5) Retos obligatorios de habilidades (Vuelo F en Botica, Gato C en Posada, Poción E en Mansión)
///    y limpieza de estado de cofres/pociones para que todas las pociones obtenidas se sumen al inventario lanzable.
/// </summary>
public static class Level2UXAndInteriorUpgrader
{
    private const string LEVEL2_SCENE_PATH = "Assets/Scenes/Level2.unity";
    private const string CAINOS_PROPS_PATH = "Assets/Cainos/Pixel Art Platformer - Village Props/Texture/TX Village Props.png";
    private const string CAINOS_CHEST_PATH = "Assets/Cainos/Pixel Art Platformer - Village Props/Texture/TX Chest Animation.png";
    private const string CAINOS_PREFAB_DIR = "Assets/Cainos/Pixel Art Platformer - Village Props/Prefab/";
    private const string LIT_MAT_PATH = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";

    [MenuItem("Brujas/Nivel 2/Aplicar Mejoras UX, Sonido, Interiores y Retos")]
    public static void ApplyAllUpgrades()
    {
        // 1. Refrescar y configurar importadores de texturas y audios modificados
        AssetDatabase.Refresh();
        ForceReimportUpdatedSprites();
        VillagerSpriteSetBuilder.BuildAll();
        UpdateIngredientPrefabsSprites();

        // 2. Abrir Level2.unity
        Scene active = SceneManager.GetActiveScene();
        if (active.path != LEVEL2_SCENE_PATH)
        {
            EditorSceneManager.OpenScene(LEVEL2_SCENE_PATH, OpenSceneMode.Single);
        }
        Physics2D.SyncTransforms();

        // 3. Conectar nuevos efectos de sonido al AudioManager
        WireAudioClipsToAudioManager();

        // 4. Actualizar FPS y fluidez en todos los VillagerSpriteAnimator de la escena
        UpgradeSceneVillagerAnimators();

        // 5. Refrescar sprites de puertas e iconos de ingredientes en el HUD
        RefreshDoorSprites();
        RefreshIngredientHUDSprites();

        // 6. Configurar los 3 retos obligatorios de habilidades (Vuelo F, Gato C, Poción E)
        ConfigureMandatoryAbilityChallenges();

        // 7. Reemplazar bloques interiores por cortes arquitectónicos coloniales + props de Cainos
        RebuildAllFourHouseInteriorsCohesive();

        // 8. Reemplazar TODOS los barriles, cajas y cofres (exteriores e interiores) por la librería de Cainos
        SwapAllBarrelsCratesAndChestsToCainos();

        // 9. Mejorar visibilidad de llaves, limpiar estado de sesión/cofres/pociones y alinear al suelo
        UpgradeKeysVisibilityAndGrounding();
        RefreshDoorSprites(); // Re-alinear todas las puertas al piso después de reconstruir las vigas interiores
        ResetAllChestsAndPotionStateForCleanPlay();

        Physics2D.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), LEVEL2_SCENE_PATH);
        AssetDatabase.SaveAssets();

        Debug.Log("[Level2UXAndInteriorUpgrader] ¡Cortes arquitectónicos coloniales, barriles/cofres Cainos, retos y pociones aplicados con éxito!");
    }

    private static void ForceReimportUpdatedSprites()
    {
        (string path, float ppu, FilterMode filter, SpriteMeshType meshType)[] spriteConfigs =
        {
            ("Assets/Art/Environment/Level2/RoomCutaway_House1_Apothecary.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/RoomCutaway_House2_Tavern.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/RoomCutaway_House3_Abandoned.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/RoomCutaway_House4_Manor.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Interior_Cellar_CatArch.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Interior_Rafter_Loft.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Interior_Floor_Planks.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Placeholder_Roof_Platform.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Placeholder_Market_Canopy_CatTunnel.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Interior_Wall_Timber.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Interior_Wall_Stone.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Interior_Fireplace.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Interior_Bookshelf.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Interior_Table_Chairs.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Interior_Bed.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Interior_Wardrobe.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Interior_Cauldron_Rack.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Interior_Window_Shutters.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Interior_Chandelier.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Placeholder_Market_Barrel.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Placeholder_Market_Crate.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Interactive_Chest_Closed.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Interactive_Chest_Open.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Interactive_Key.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Placeholder_Ingredient_Mushroom.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Placeholder_Ingredient_Spider.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Placeholder_Ingredient_WitheredFlower.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Placeholder_Ingredient_Root.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Village_Door_Closed.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Village_Door_Open.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect),
            ("Assets/Art/Environment/Level2/Village_Door_Locked.png", 64f, FilterMode.Bilinear, SpriteMeshType.FullRect)
        };

        foreach (var cfg in spriteConfigs)
        {
            if (!System.IO.File.Exists(cfg.path)) continue;
            TextureImporter ti = AssetImporter.GetAtPath(cfg.path) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = cfg.ppu;
                ti.filterMode = cfg.filter;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.alphaIsTransparency = true;
                TextureImporterSettings settings = new TextureImporterSettings();
                ti.ReadTextureSettings(settings);
                settings.spriteMeshType = cfg.meshType;
                ti.SetTextureSettings(settings);
                ti.SaveAndReimport();
            }
            else
            {
                AssetDatabase.ImportAsset(cfg.path, ImportAssetOptions.ForceUpdate);
            }
        }
    }

    private static void UpdateIngredientPrefabsSprites()
    {
        (string prefabPath, string spritePath)[] map =
        {
            ("Assets/Prefabs/Level2/Ingredient_Mushroom.prefab", "Assets/Art/Environment/Level2/Placeholder_Ingredient_Mushroom.png"),
            ("Assets/Prefabs/Level2/Ingredient_Spider.prefab", "Assets/Art/Environment/Level2/Placeholder_Ingredient_Spider.png"),
            ("Assets/Prefabs/Level2/Ingredient_WitheredFlower.prefab", "Assets/Art/Environment/Level2/Placeholder_Ingredient_WitheredFlower.png"),
            ("Assets/Prefabs/Level2/Ingredient_Root.prefab", "Assets/Art/Environment/Level2/Placeholder_Ingredient_Root.png")
        };

        foreach (var item in map)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(item.prefabPath);
            Sprite sp = LoadSpriteFromAsset(item.spritePath);
            if (prefab != null && sp != null)
            {
                SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = sp;
                    EditorUtility.SetDirty(prefab);
                }
            }
        }
    }

    private static void RefreshIngredientHUDSprites()
    {
        Sprite mSp = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Placeholder_Ingredient_Mushroom.png");
        Sprite sSp = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Placeholder_Ingredient_Spider.png");
        Sprite fSp = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Placeholder_Ingredient_WitheredFlower.png");
        Sprite rSp = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Placeholder_Ingredient_Root.png");

        IngredientHUD hud = Object.FindFirstObjectByType<IngredientHUD>(FindObjectsInactive.Include);
        if (hud != null)
        {
            SerializedObject so = new SerializedObject(hud);
            if (so.FindProperty("mushroomSprite") != null && mSp != null) so.FindProperty("mushroomSprite").objectReferenceValue = mSp;
            if (so.FindProperty("spiderSprite") != null && sSp != null) so.FindProperty("spiderSprite").objectReferenceValue = sSp;
            if (so.FindProperty("flowerSprite") != null && fSp != null) so.FindProperty("flowerSprite").objectReferenceValue = fSp;
            if (so.FindProperty("rootSprite") != null && rSp != null) so.FindProperty("rootSprite").objectReferenceValue = rSp;
            so.ApplyModifiedPropertiesWithoutUndo();

            SetSlotIconSprite(so.FindProperty("mushroomIconTransform")?.objectReferenceValue as RectTransform, mSp);
            SetSlotIconSprite(so.FindProperty("spiderIconTransform")?.objectReferenceValue as RectTransform, sSp);
            SetSlotIconSprite(so.FindProperty("flowerIconTransform")?.objectReferenceValue as RectTransform, fSp);
            SetSlotIconSprite(so.FindProperty("rootIconTransform")?.objectReferenceValue as RectTransform, rSp);
            EditorUtility.SetDirty(hud);
        }

        TutorialTrigger[] tuts = Object.FindObjectsByType<TutorialTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TutorialTrigger t in tuts)
        {
            if (t != null && t.TipId == "tip_ingredientes_mercado" && mSp != null)
            {
                SerializedObject tSo = new SerializedObject(t);
                if (tSo.FindProperty("tipIcon") != null)
                {
                    tSo.FindProperty("tipIcon").objectReferenceValue = mSp;
                    tSo.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(t);
                }
            }
        }
    }

    private static void SetSlotIconSprite(RectTransform rt, Sprite sp)
    {
        if (rt == null || sp == null) return;
        UnityEngine.UI.Image img = rt.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
        {
            img.sprite = sp;
            img.preserveAspect = true;
            img.color = Color.white;
            EditorUtility.SetDirty(img);
        }
    }

    private static void WireAudioClipsToAudioManager()
    {
        AudioManager am = Object.FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
        if (am == null) return;

        SerializedObject so = new SerializedObject(am);
        (string fieldName, string wavPath)[] bindings =
        {
            ("footstepHumanClip", "Assets/Audio/SFX/SFX_FootstepHuman.wav"),
            ("footstepCatClip", "Assets/Audio/SFX/SFX_FootstepCat.wav"),
            ("broomFlyClip", "Assets/Audio/SFX/SFX_BroomFly.wav"),
            ("potionThrowClip", "Assets/Audio/SFX/SFX_PotionThrow.wav"),
            ("potionShatterClip", "Assets/Audio/SFX/SFX_PotionShatter.wav"),
            ("chestOpenClip", "Assets/Audio/SFX/SFX_ChestOpen.wav"),
            ("chestLockedClip", "Assets/Audio/SFX/SFX_ChestLocked.wav"),
            ("keyPickupClip", "Assets/Audio/SFX/SFX_KeyPickup.wav"),
            ("ingredientPickupClip", "Assets/Audio/SFX/SFX_IngredientPickup.wav"),
            ("potionPickupClip", "Assets/Audio/SFX/SFX_PotionPickup.wav"),
            ("doorOpenClip", "Assets/Audio/SFX/SFX_DoorOpen.wav"),
            ("loreReadClip", "Assets/Audio/SFX/SFX_LoreRead.wav"),
            ("villagerNoticeClip", "Assets/Audio/SFX/SFX_VillagerNotice.wav"),
            ("villagerScreamClip", "Assets/Audio/SFX/SFX_VillagerScream.wav"),
            ("guardAlertClip", "Assets/Audio/SFX/SFX_GuardAlert.wav"),
            ("frenzyAlarmClip", "Assets/Audio/SFX/SFX_FrenzyAlarm.wav"),
            ("gateUnlockClip", "Assets/Audio/SFX/SFX_GateUnlock.wav")
        };

        foreach (var b in bindings)
        {
            if (System.IO.File.Exists(b.wavPath))
            {
                AssetDatabase.ImportAsset(b.wavPath, ImportAssetOptions.ForceUpdate);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(b.wavPath);
                SerializedProperty prop = so.FindProperty(b.fieldName);
                if (prop != null && clip != null)
                {
                    prop.objectReferenceValue = clip;
                }
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(am);
    }

    private static void UpgradeSceneVillagerAnimators()
    {
        VillagerSpriteAnimator[] anims = Object.FindObjectsByType<VillagerSpriteAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (VillagerSpriteAnimator anim in anims)
        {
            if (anim == null) continue;
            SerializedObject so = new SerializedObject(anim);
            SetFloatIfPresent(so, "idleFps", 6.5f);
            SetFloatIfPresent(so, "walkFps", 14.5f);
            SetFloatIfPresent(so, "runFps", 20.0f);
            SetFloatIfPresent(so, "statusFps", 8.5f);
            SetBoolIfPresent(so, "enableSecondaryMotion", true);
            SetBoolIfPresent(so, "enableSmoothTurn", true);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(anim);
        }
    }

    private static void SetFloatIfPresent(SerializedObject so, string name, float val)
    {
        SerializedProperty p = so.FindProperty(name);
        if (p != null) p.floatValue = val;
    }

    private static void SetBoolIfPresent(SerializedObject so, string name, bool val)
    {
        SerializedProperty p = so.FindProperty(name);
        if (p != null) p.boolValue = val;
    }

    private static void RefreshDoorSprites()
    {
        Sprite doorClosed = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Village_Door_Closed.png");
        Sprite doorOpen = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Village_Door_Open.png");
        Sprite doorLocked = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Village_Door_Locked.png");

        HouseDoor[] doors = Object.FindObjectsByType<HouseDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (HouseDoor d in doors)
        {
            if (d == null) continue;
            SerializedObject so = new SerializedObject(d);

            if (d.name == "Door_Ext_House3_Roof")
            {
                if (so.FindProperty("permanentlyBarred") != null) so.FindProperty("permanentlyBarred").boolValue = false;
                if (so.FindProperty("isLocked") != null) so.FindProperty("isLocked").boolValue = false;
                if (so.FindProperty("actionLabel") != null) so.FindProperty("actionLabel").stringValue = "Entrar a la Casa del Tejedor (2º Piso)";
            }
            else if (d.name == "Door_Ext_House3_Ground")
            {
                if (so.FindProperty("permanentlyBarred") != null) so.FindProperty("permanentlyBarred").boolValue = false;
                if (so.FindProperty("isLocked") != null) so.FindProperty("isLocked").boolValue = false;
                if (so.FindProperty("actionLabel") != null) so.FindProperty("actionLabel").stringValue = "Entrar a la Casa del Tejedor";
                Level2DoorIndicator ind = d.GetComponent<Level2DoorIndicator>();
                if (ind != null) ind.Configure(false, 0.25f);
            }
            else if (d.name == "Door_Ext_Z3_Quarantine" || d.name == "Door_Ext_Z6_Sanctum")
            {
                d.gameObject.SetActive(false);
                EditorUtility.SetDirty(d.gameObject);
                continue;
            }

            if (doorClosed != null && so.FindProperty("closedSprite") != null)
                so.FindProperty("closedSprite").objectReferenceValue = doorClosed;
            if (doorOpen != null && so.FindProperty("openSprite") != null)
                so.FindProperty("openSprite").objectReferenceValue = doorOpen;
            if (doorLocked != null && so.FindProperty("lockedSprite") != null)
                so.FindProperty("lockedSprite").objectReferenceValue = doorLocked;
            so.ApplyModifiedPropertiesWithoutUndo();

            SpriteRenderer sr = d.GetComponent<SpriteRenderer>();
            if (sr != null && doorClosed != null)
            {
                sr.sprite = doorClosed;
                Physics2D.SyncTransforms();
                float surfY = Level2CompleteFixer.FindSurfaceBelow(d.transform.position.x, sr.bounds.min.y + 0.8f);
                float delta = surfY - sr.bounds.min.y;
                d.transform.position += new Vector3(0f, delta, 0f);
                Physics2D.SyncTransforms();
            }
            EditorUtility.SetDirty(d);
        }
    }

    private static void ConfigureMandatoryAbilityChallenges()
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        Dictionary<string, Sprite> cainos = LoadCainosSprites();
        Sprite loftSprite = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Interior_Rafter_Loft.png");
        Sprite catArchSprite = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Interior_Cellar_CatArch.png");
        Sprite plankSprite = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Interior_Floor_Planks.png");
        Sprite timberSprite = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Interior_Wall_Timber.png");
        Sprite closedChestSp = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Interactive_Chest_Closed.png");
        Sprite openChestSp = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Interactive_Chest_Open.png");
        Sprite flightIconSp = LoadSpriteFromAsset("Assets/Art/UI/Icons/Icon_FlightOnly.png");
        Sprite catIconSp = LoadSpriteFromAsset("Assets/Art/UI/Icons/Icon_CatOnly.png");

        // =====================================================================
        // RETO 1 (CASA 1 - BOTICA): VUELO EN ESCOBA OBLIGATORIO [F]
        // =====================================================================
        GameObject h1 = GameObject.Find("Environment/Interiors/Interior_House1_Apothecary");
        if (h1 != null)
        {
            RaiseInteriorCeilingForFlightLoft(h1.transform, 18f, 28.5f, 39.6f, 16f, timberSprite);

            Transform oldLoft = h1.transform.Find("Challenge_FlightLoft_H1");
            if (oldLoft != null) Object.DestroyImmediate(oldLoft.gameObject);

            GameObject loftGO = new GameObject("Challenge_FlightLoft_H1");
            loftGO.transform.SetParent(h1.transform, true);
            loftGO.layer = groundLayer;
            float loftTopY = 35.85f;
            float loftH = 0.40f;
            loftGO.transform.position = new Vector3(19.8f, loftTopY - loftH * 0.5f, 0f);

            SpriteRenderer lsr = loftGO.AddComponent<SpriteRenderer>();
            lsr.sprite = loftSprite != null ? loftSprite : plankSprite;
            lsr.drawMode = SpriteDrawMode.Simple;
            lsr.sortingLayerName = "Gameplay";
            lsr.sortingOrder = 1;

            BoxCollider2D lcol = loftGO.AddComponent<BoxCollider2D>();
            lcol.size = new Vector2(3.8f, loftH);
            lcol.isTrigger = false;
            lcol.usedByEffector = true;

            PlatformEffector2D leff = loftGO.AddComponent<PlatformEffector2D>();
            leff.useOneWay = true;
            leff.useOneWayGrouping = true;
            leff.surfaceArc = 170f;

            // Cadenas y poleas de Cainos que sostienen el altillo desde las vigas del tejado
            Sprite chainSp = GetCainos(cainos, "Chain 01", null);
            Sprite pulleySp = GetCainos(cainos, "Elevator  Pully", null);
            foreach (float cx in new[] { -1.35f, 1.35f })
            {
                if (chainSp != null)
                {
                    for (int ci = 0; ci < 3; ci++)
                    {
                        GameObject chGO = new GameObject($"LoftChain_{(cx < 0 ? "L" : "R")}_{ci}");
                        chGO.transform.SetParent(loftGO.transform, false);
                        chGO.transform.localPosition = new Vector3(cx, 0.55f + ci * 0.95f, 0f);
                        SpriteRenderer chSr = chGO.AddComponent<SpriteRenderer>();
                        chSr.sprite = chainSp;
                        chSr.sortingLayerName = "Gameplay";
                        chSr.sortingOrder = 0;
                    }
                }
                if (pulleySp != null)
                {
                    GameObject pGO = new GameObject($"LoftPulley_{(cx < 0 ? "L" : "R")}");
                    pGO.transform.SetParent(loftGO.transform, false);
                    pGO.transform.localPosition = new Vector3(cx, 3.25f, 0f);
                    SpriteRenderer pSr = pGO.AddComponent<SpriteRenderer>();
                    pSr.sprite = pulleySp;
                    pSr.sortingLayerName = "Gameplay";
                    pSr.sortingOrder = 1;
                }
            }

            // Icono e indicador de Vuelo Obligatorio [F]
            GameObject hintGO = new GameObject("FlightHint_Loft_H1");
            hintGO.transform.SetParent(loftGO.transform, false);
            hintGO.transform.localPosition = new Vector3(-1.55f, -0.55f, 0f);
            BoxCollider2D hintCol = hintGO.AddComponent<BoxCollider2D>();
            hintCol.isTrigger = true;
            hintCol.size = new Vector2(3.5f, 3.5f);
            SpriteRenderer hsr = hintGO.AddComponent<SpriteRenderer>();
            if (flightIconSp != null)
            {
                hsr.sprite = flightIconSp;
                hsr.sortingLayerName = "Gameplay";
                hsr.sortingOrder = 5;
            }
            hintGO.AddComponent<FlightOnlyHint>();

            Physics2D.SyncTransforms();

            Transform chest1Tr = h1.transform.Find("Chest_Int_House1_Upper");
            if (chest1Tr != null)
            {
                chest1Tr.position = new Vector3(19.8f, loftTopY + 0.5f, 0f);
                SnapSpriteBottomToY(chest1Tr.gameObject, loftTopY);
                EditorUtility.SetDirty(chest1Tr.gameObject);
            }
        }

        // =====================================================================
        // RETO 2 (CASA 2 - SÓTANO DE LA POSADA): FORMA DE GATO OBLIGATORIA [C]
        // =====================================================================
        GameObject h2 = GameObject.Find("Environment/Interiors/Interior_House2_Tavern");
        if (h2 != null)
        {
            // Eliminar los peldaños amontonados en el centro del sótano (la escalera de mano Ladder_Int_House2_CellarToF1 ya conecta limpiamente)
            Transform oldSteps2 = h2.transform.Find("Cellar_WalkableSteps");
            if (oldSteps2 != null) Object.DestroyImmediate(oldSteps2.gameObject);
            Transform oldCrateStep = h2.transform.Find("Cellar_Crate_Step");
            if (oldCrateStep != null) Object.DestroyImmediate(oldCrateStep.gameObject);

            Transform oldTunnel = h2.transform.Find("Challenge_CatTunnel_TavernCellar");
            if (oldTunnel != null) Object.DestroyImmediate(oldTunnel.gameObject);

            GameObject tunnelGO = new GameObject("Challenge_CatTunnel_TavernCellar");
            tunnelGO.transform.SetParent(h2.transform, true);
            tunnelGO.layer = groundLayer;

            float tunnelBottomY = 25.65f;
            float tunnelTopY = 28.55f;
            float tunnelH = tunnelTopY - tunnelBottomY;
            float tunnelW = 2.1f;
            tunnelGO.transform.position = new Vector3(46.6f, tunnelBottomY + tunnelH * 0.5f, 0f);

            SpriteRenderer tsr = tunnelGO.AddComponent<SpriteRenderer>();
            tsr.sprite = catArchSprite;
            tsr.drawMode = SpriteDrawMode.Simple;
            tsr.color = Color.white;
            tsr.sortingLayerName = "Gameplay";
            tsr.sortingOrder = 1;

            BoxCollider2D tcol = tunnelGO.AddComponent<BoxCollider2D>();
            tcol.size = new Vector2(tunnelW, tunnelH);
            tcol.isTrigger = false;

            GameObject catHintGO = new GameObject("CatPassageHint_TavernCellar");
            catHintGO.transform.SetParent(tunnelGO.transform, false);
            catHintGO.transform.localPosition = new Vector3(0f, -tunnelH * 0.5f - 0.42f, 0f);
            BoxCollider2D catTrig = catHintGO.AddComponent<BoxCollider2D>();
            catTrig.isTrigger = true;
            catTrig.size = new Vector2(2.6f, 0.9f);
            CatOnlyPassage cop = catHintGO.AddComponent<CatOnlyPassage>();
            if (catIconSp != null)
            {
                SerializedObject soCop = new SerializedObject(cop);
                if (soCop.FindProperty("catIconSprite") != null)
                {
                    soCop.FindProperty("catIconSprite").objectReferenceValue = catIconSp;
                    soCop.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            Transform b1 = h2.transform.Find("Cellar_Barrel_1");
            if (b1 != null)
            {
                BoxCollider2D bc1 = b1.GetComponent<BoxCollider2D>();
                if (bc1 != null) Object.DestroyImmediate(bc1);
                b1.position = new Vector3(44.2f, 25.25f, 0f);
                SnapSpriteBottomToY(b1.gameObject, 24.75f);
            }
            Transform b2 = h2.transform.Find("Cellar_Barrel_2");
            if (b2 != null)
            {
                BoxCollider2D bc2 = b2.GetComponent<BoxCollider2D>();
                if (bc2 != null) Object.DestroyImmediate(bc2);
                b2.position = new Vector3(50.2f, 25.25f, 0f);
                SnapSpriteBottomToY(b2.gameObject, 24.75f);
            }
        }

        // =====================================================================
        // RETO 3 (CASA 4 - CRIPTA DE LA MANSIÓN): ARROJAR POCIÓN OBLIGATORIO [1/2/3 + E]
        // =====================================================================
        GameObject h4 = GameObject.Find("Environment/Interiors/Interior_House4_Manor");
        if (h4 != null)
        {
            // Eliminar peldaños y cajas flotantes en el centro de la cripta (Ladder_Int_House4_CellarToF1 conecta limpiamente)
            Transform oldSteps4 = h4.transform.Find("Cellar_WalkableSteps");
            if (oldSteps4 != null) Object.DestroyImmediate(oldSteps4.gameObject);
            foreach (string floatingCrate in new[] { "Manor_Cellar_Crate1", "Manor_Cellar_Crate2" })
            {
                Transform fc = h4.transform.Find(floatingCrate);
                if (fc != null) Object.DestroyImmediate(fc.gameObject);
            }

            Transform oldAlchChest = h4.transform.Find("Chest_Int_House4_CellarAlch");
            if (oldAlchChest != null) Object.DestroyImmediate(oldAlchChest.gameObject);

            if (closedChestSp != null && openChestSp != null)
            {
                GameObject chestGO = new GameObject("Chest_Int_House4_CellarAlch");
                chestGO.transform.SetParent(h4.transform, true);
                chestGO.transform.localScale = Vector3.one;
                chestGO.transform.position = new Vector3(144.8f, 25.25f, 0f);

                SpriteRenderer csr = chestGO.AddComponent<SpriteRenderer>();
                csr.sprite = closedChestSp;
                csr.sortingLayerName = "Gameplay";
                csr.sortingOrder = 2;
                SnapSpriteBottomToY(chestGO, 24.75f);

                BoxCollider2D ctrig = chestGO.AddComponent<BoxCollider2D>();
                ctrig.isTrigger = true;
                ctrig.size = new Vector2(1.45f, 1.35f);
                ctrig.offset = new Vector2(0f, 0.25f);

                Level2Chest l2c = chestGO.AddComponent<Level2Chest>();
                l2c.ConfigureChest(
                    "chest_int_house4_cellar_alch",
                    "Arcón Alquímico de la Cripta",
                    closedChestSp,
                    openChestSp,
                    new List<ChestContentEntry>
                    {
                        new ChestContentEntry { itemKind = ChestItemKind.Potion, potionType = PotionType.Sleep, amount = 1 },
                        new ChestContentEntry { itemKind = ChestItemKind.Potion, potionType = PotionType.Blind, amount = 1 }
                    });

                Level2DoorIndicator ind = chestGO.AddComponent<Level2DoorIndicator>();
                ind.Configure(false, 1.05f);
                chestGO.AddComponent<Level2CatPickupGuard>();
                EditorUtility.SetDirty(chestGO);
            }

            Transform oldSentinel = h4.transform.Find("Challenge_PotionSentinel_ManorCrypt");
            if (oldSentinel != null) Object.DestroyImmediate(oldSentinel.gameObject);

            GameObject sentinelGO = new GameObject("Challenge_PotionSentinel_ManorCrypt");
            sentinelGO.transform.SetParent(h4.transform, true);
            sentinelGO.layer = 0;
            sentinelGO.transform.position = new Vector3(152.8f, 24.75f, 0f);

            SpriteRenderer ssr = sentinelGO.AddComponent<SpriteRenderer>();
            ssr.sortingLayerName = "Gameplay";
            ssr.sortingOrder = 4;

            VillagerAnimationSet guardSet = AssetDatabase.LoadAssetAtPath<VillagerAnimationSet>("Assets/Art/Characters/Villagers/guard/guard_Animations.asset");
            if (guardSet == null)
            {
                guardSet = AssetDatabase.LoadAssetAtPath<VillagerAnimationSet>("Assets/Art/Characters/Villagers/guard_AnimSet.asset");
            }
            if (guardSet != null && guardSet.idle != null && !guardSet.idle.IsEmpty)
            {
                ssr.sprite = guardSet.idle.frames[0];
            }
            SnapSpriteBottomToY(sentinelGO, 24.75f);

            VillagerSpriteAnimator vsa = sentinelGO.AddComponent<VillagerSpriteAnimator>();
            if (guardSet != null)
            {
                vsa.SetAnimationSet(guardSet);
            }

            BoxCollider2D barrierCol = sentinelGO.AddComponent<BoxCollider2D>();
            barrierCol.isTrigger = false;
            float centerOffsetY = (26.60f - sentinelGO.transform.position.y);
            barrierCol.offset = new Vector2(0f, centerOffsetY);
            barrierCol.size = new Vector2(1.15f, 3.65f);

            GameObject wardGO = new GameObject("ArcaneWardVisual");
            wardGO.transform.SetParent(sentinelGO.transform, false);
            wardGO.transform.localPosition = new Vector3(-0.35f, 0.85f, 0f);
            SpriteRenderer wsr = wardGO.AddComponent<SpriteRenderer>();
            wsr.sprite = CreateVerticalWardSprite();
            wsr.color = new Color(1.0f, 0.32f, 0.22f, 0.65f);
            wsr.sortingLayerName = "Gameplay";
            wsr.sortingOrder = 5;

            ArcaneSentinelObstacle sentinel = sentinelGO.AddComponent<ArcaneSentinelObstacle>();
            sentinel.Configure(
                "Centinela de la Cripta",
                barrierCol,
                null,
                ssr,
                guardSet,
                wsr,
                null,
                null,
                null);
            EditorUtility.SetDirty(sentinelGO);
        }
    }

    /// <summary>
    /// Reconstruye la estética de las 4 casas interiores para eliminar el aspecto "creado por bloques":
    /// - Oculta los mosaicos cuadrados Wall_r_c y los puentes superpuestos LadderTopBridge_*.
    /// - Coloca un único corte arquitectónico colonial completo (RoomCutaway_House1..4) por casa.
    /// - Viste las vigas de suelo con escuadras y pilares de madera de Cainos (Platform L/R 01, Stall Tent Pillar).
    /// - Amuebla cada estancia con composiciones detalladas de Cainos (mesas, sillas, calderos, ollas, barriles,
    ///   cajas, sacos, armeros, estandartes y antorchas/pebeteros con fuego).
    /// </summary>
    private static void RebuildAllFourHouseInteriorsCohesive()
    {
        Dictionary<string, Sprite> cainos = LoadCainosSprites();
        Sprite plankSp = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Interior_Floor_Planks.png");
        Sprite cauldronSp = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Interior_Cauldron_Rack.png");
        Sprite shelfSp = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Interior_Bookshelf.png");
        Sprite fireplaceSp = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Interior_Fireplace.png");
        Sprite tableSp = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Interior_Table_Chairs.png");
        Sprite chandelierSp = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Interior_Chandelier.png");
        Sprite windowSp = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Interior_Window_Shutters.png");

        Sprite cainosBarrel = GetCainos(cainos, "Barrel", null);
        Sprite cainosCrateL = GetCainos(cainos, "Crate Large", null);
        Sprite cainosCrateS = GetCainos(cainos, "Crate Small", null);
        Sprite cainosPot1 = GetCainos(cainos, "Pot 01", null);
        Sprite cainosPot2 = GetCainos(cainos, "Pot 02", null);
        Sprite cainosPot3 = GetCainos(cainos, "Pot 03", null);
        Sprite cainosCauldron = GetCainos(cainos, "Cauldron", null);
        Sprite cainosBasket = GetCainos(cainos, "Basket", null);
        Sprite cainosGrain = GetCainos(cainos, "Grain Box", null);
        Sprite cainosBag1 = GetCainos(cainos, "Gunny Bag 01", null);
        Sprite cainosBag2 = GetCainos(cainos, "Gunny Bag 02", null);
        Sprite cainosBanner = GetCainos(cainos, "Banner", null);
        Sprite cainosWeaponRack = GetCainos(cainos, "Weapon Rack", null);
        Sprite cainosBench = GetCainos(cainos, "Log Bench", null);
        Sprite cainosLogs = GetCainos(cainos, "Wood Logs", null);

        // --- CASA 1: BOTICA DE HERBOLARIA (x=10..26, y=28.5..39.6) ---
        GameObject h1 = GameObject.Find("Environment/Interiors/Interior_House1_Apothecary");
        if (h1 != null)
        {
            ApplyFullRoomCutawayAndBeams(
                h1.transform,
                "Assets/Art/Environment/Level2/RoomCutaway_House1_Apothecary.png",
                new Vector3(18.0f, 34.05f, 0f),
                plankSp,
                cainos);

            Transform decor1 = RecreateDecorRoot(h1.transform, "Cainos_Interior_Decor");
            // Planta Baja (y=28.75): Puerta en x=12.5, Lore en x=18.0, Escalera en x=22.0
            SpawnGroundedDecor(decor1, "Cainos_Apothecary_CauldronBig", new Vector2(15.2f, 28.75f), cainosCauldron, 1.35f, -1);
            SpawnGroundedDecor(decor1, "Cainos_Apothecary_AlchRack", new Vector2(20.0f, 28.75f), cauldronSp, 1.20f, -1);
            SpawnWallDecor(decor1, "Cainos_Apothecary_WindowLeft", new Vector3(15.2f, 31.0f, 0f), windowSp, 1.15f, -2);
            SpawnWallDecor(decor1, "Cainos_Apothecary_WindowRight", new Vector3(20.0f, 31.0f, 0f), windowSp, 1.15f, -2);
            // 1er Piso (y=32.75): Puerta balcón en x=12.5, Escalera en x=22.0
            SpawnGroundedDecor(decor1, "Cainos_Apothecary_F2_Shelf", new Vector2(15.0f, 32.75f), shelfSp, 1.20f, -1);
            SpawnGroundedDecor(decor1, "Cainos_Apothecary_F2_Table", new Vector2(18.2f, 32.75f), tableSp, 1.15f, -1);
            SpawnCainosTorch(decor1, "Torch_Apothecary_F1", new Vector2(18.0f, 30.8f));
            SpawnCainosTorch(decor1, "Torch_Apothecary_Loft", new Vector2(17.2f, 36.6f));
            SpawnWarmCandleLight(decor1, "Light_Apothecary_CauldronGlow", new Vector3(15.2f, 29.8f, 0f), new Color(0.45f, 0.95f, 0.55f, 1f), 5.0f, 1.10f);
        }

        // --- CASA 2: POSADA DEL CUERVO (x=39..57, y=24.5..36.8) ---
        GameObject h2 = GameObject.Find("Environment/Interiors/Interior_House2_Tavern");
        if (h2 != null)
        {
            ApplyFullRoomCutawayAndBeams(
                h2.transform,
                "Assets/Art/Environment/Level2/RoomCutaway_House2_Tavern.png",
                new Vector3(48.0f, 30.65f, 0f),
                plankSp,
                cainos);

            Transform decor2 = RecreateDecorRoot(h2.transform, "Cainos_Interior_Decor");
            // Sótano (y=24.75): Llave en x=42, Arco Gato en x=46.6, Escalera en x=53.0
            SpawnGroundedDecor(decor2, "Cainos_Tavern_Cellar_Bag1", new Vector2(40.3f, 24.75f), cainosBag1, 1.1f, -1);
            SpawnCainosTorch(decor2, "Torch_Tavern_CellarWest", new Vector2(42.0f, 26.8f));
            SpawnCainosTorch(decor2, "Torch_Tavern_CellarEast", new Vector2(51.2f, 26.8f));
            // 1er Piso (y=28.75): Puerta en x=41.5, Escalera en x=44.2, Lore en x=50.5, Escalera sótano en x=53.0
            SpawnGroundedDecor(decor2, "Cainos_Tavern_F1_Table", new Vector2(47.2f, 28.75f), tableSp, 1.20f, -1);
            SpawnGroundedDecor(decor2, "Cainos_Tavern_F1_Barrel", new Vector2(55.2f, 28.75f), cainosBarrel, 1.10f, -1);
            SpawnWallDecor(decor2, "Cainos_Tavern_F1_Window", new Vector3(47.2f, 31.0f, 0f), windowSp, 1.15f, -2);
            // 2º Piso (y=32.75): Escalera en x=44.2, Cofre en x=51.5, Puerta balcón en x=54.2
            SpawnGroundedDecor(decor2, "Cainos_Tavern_F2_Table", new Vector2(48.0f, 32.75f), tableSp, 1.20f, -1);
            SpawnWallDecor(decor2, "Cainos_Tavern_F2_Window", new Vector3(48.0f, 35.0f, 0f), windowSp, 1.15f, -2);
            SpawnWarmCandleLight(decor2, "Light_Tavern_HearthFire", new Vector3(48.2f, 30.2f, 0f), new Color(1.0f, 0.72f, 0.35f, 1f), 6.0f, 1.15f);
            SpawnWarmCandleLight(decor2, "Light_Tavern_CellarKeySpot", new Vector3(42.0f, 26.2f, 0f), new Color(1.0f, 0.85f, 0.38f, 1f), 5.0f, 1.20f);
        }

        // --- CASA 3: CASA ABANDONADA DEL TEJEDOR (x=98..116, y=28.5..40.8) ---
        GameObject h3 = GameObject.Find("Environment/Interiors/Interior_House3_Abandoned");
        if (h3 != null)
        {
            Transform oldFloatCrate3 = h3.transform.Find("Abandoned_Crate_1");
            if (oldFloatCrate3 != null) Object.DestroyImmediate(oldFloatCrate3.gameObject);

            ApplyFullRoomCutawayAndBeams(
                h3.transform,
                "Assets/Art/Environment/Level2/RoomCutaway_House3_Abandoned.png",
                new Vector3(107.0f, 34.65f, 0f),
                plankSp,
                cainos);

            Transform decor3 = RecreateDecorRoot(h3.transform, "Cainos_Interior_Decor");
            // 1er Piso (y=28.75): Puerta en x=100.5, Escalera en x=112.5
            SpawnGroundedDecor(decor3, "Cainos_Abandoned_F1_Shelf", new Vector2(104.2f, 28.75f), shelfSp, 1.20f, -1);
            SpawnGroundedDecor(decor3, "Cainos_Abandoned_F1_Table", new Vector2(108.2f, 28.75f), tableSp, 1.20f, -1);
            SpawnWallDecor(decor3, "Cainos_Abandoned_F1_Window", new Vector3(108.2f, 31.0f, 0f), windowSp, 1.15f, -2);
            // 2º Piso (y=32.75): Escalera izq en x=103.0, Escalera der en x=112.5, Puerta tejado en x=114.2
            SpawnGroundedDecor(decor3, "Cainos_Abandoned_F2_Table", new Vector2(106.2f, 32.75f), tableSp, 1.15f, -1);
            SpawnGroundedDecor(decor3, "Cainos_Abandoned_F2_Alch", new Vector2(109.8f, 32.75f), cauldronSp, 1.15f, -1);
            // Ático (y=36.75): Escalera en x=103.0, Lore en x=108.2, Cofre en x=112.2
            SpawnGroundedDecor(decor3, "Cainos_Abandoned_Attic_Shelf", new Vector2(105.5f, 36.75f), shelfSp, 1.15f, -1);
            SpawnCainosTorch(decor3, "Torch_Abandoned_Attic", new Vector2(109.8f, 38.6f));
            SpawnWarmCandleLight(decor3, "Light_Abandoned_AtticChestSpot", new Vector3(112.2f, 38.2f, 0f), new Color(0.90f, 0.78f, 1.0f, 1f), 5.5f, 1.15f);
        }

        // --- CASA 4: MANSIÓN DEL MAGISTRADO (x=138..158, y=24.5..37.2) ---
        GameObject h4 = GameObject.Find("Environment/Interiors/Interior_House4_Manor");
        if (h4 != null)
        {
            ApplyFullRoomCutawayAndBeams(
                h4.transform,
                "Assets/Art/Environment/Level2/RoomCutaway_House4_Manor.png",
                new Vector3(148.0f, 30.85f, 0f),
                plankSp,
                cainos);

            Transform decor4 = RecreateDecorRoot(h4.transform, "Cainos_Interior_Decor");
            // Cripta (y=24.75): Escalera en x=141.5, Cofre Alquímico en x=144.8, Lore en x=148.0, Centinela en x=152.8, Llave en x=155.5
            SpawnGroundedDecor(decor4, "Cainos_Manor_Crypt_WeaponRack", new Vector2(139.4f, 24.75f), cainosWeaponRack, 1.05f, -1);
            SpawnCainosTorch(decor4, "Torch_Manor_CryptWest", new Vector2(144.8f, 26.8f));
            SpawnCainosTorch(decor4, "Torch_Manor_CryptEast", new Vector2(154.8f, 26.8f));
            // Gran Salón 1er Piso (y=28.75): Escalera en x=141.5, Puerta en x=143.5, Escalera der en x=153.5
            SpawnGroundedDecor(decor4, "Cainos_Manor_F1_GrandShelf", new Vector2(146.5f, 28.75f), shelfSp, 1.20f, -1);
            SpawnGroundedDecor(decor4, "Cainos_Manor_F1_StudyTable", new Vector2(150.2f, 28.75f), tableSp, 1.20f, -1);
            SpawnWallDecor(decor4, "Cainos_Manor_F1_WindowR", new Vector3(150.2f, 31.0f, 0f), windowSp, 1.15f, -2);
            // Biblioteca 2º Piso (y=32.75): Cofre Real en x=142.8 (Mezzanine y=34.5), Puerta balcón en x=150.0, Escalera en x=153.5
            SpawnGroundedDecor(decor4, "Cainos_Manor_F2_Shelf", new Vector2(146.5f, 32.75f), shelfSp, 1.20f, -1);
            SpawnGroundedDecor(decor4, "Cainos_Manor_F2_WeaponRack", new Vector2(156.2f, 32.75f), cainosWeaponRack, 1.05f, -1);
            SpawnWarmCandleLight(decor4, "Light_Manor_CryptKeySpot", new Vector3(155.5f, 26.2f, 0f), new Color(1.0f, 0.85f, 0.35f, 1f), 5.5f, 1.25f);
            SpawnWarmCandleLight(decor4, "Light_Manor_HearthGlow", new Vector3(148.0f, 30.2f, 0f), new Color(1.0f, 0.68f, 0.28f, 1f), 6.0f, 1.15f);
        }
    }

    private static void ApplyFullRoomCutawayAndBeams(
        Transform interiorRoot,
        string cutawayAssetPath,
        Vector3 cutawayWorldCenter,
        Sprite plankSprite,
        Dictionary<string, Sprite> cainos)
    {
        // 1. Limpiar mobiliario antiguo duplicado (Prop_*) y configurar el 100 % de los pisos físicos con su piso visual exacto
        List<GameObject> legacyPropsToDestroy = new List<GameObject>();
        foreach (Transform child in interiorRoot)
        {
            if (child == null) continue;
            if (child.name.StartsWith("Prop_"))
            {
                legacyPropsToDestroy.Add(child.gameObject);
            }
        }
        foreach (GameObject oldProp in legacyPropsToDestroy)
        {
            Object.DestroyImmediate(oldProp);
        }

        foreach (SpriteRenderer sr in interiorRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null) continue;
            string n = sr.gameObject.name;
            if (n.StartsWith("Wall_") || n == "Ceiling_Solid" || n == "LeftWall_Solid" || n == "RightWall_Solid")
            {
                sr.enabled = false;
                EditorUtility.SetDirty(sr);
            }
            else if ((n == "Bottom_Floor_Solid" || n.Contains("Slab") || n.Contains("Landing") || n == "Library_Mezzanine" || n.StartsWith("LadderTopBridge_")) && plankSprite != null)
            {
                BoxCollider2D col = sr.GetComponent<BoxCollider2D>();
                sr.enabled = true;
                sr.sprite = plankSprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                if (col != null)
                {
                    col.offset = Vector2.zero;
                    sr.size = new Vector2(col.size.x, col.size.y);
                }
                sr.color = Color.white;
                sr.sortingLayerName = "Gameplay";
                sr.sortingOrder = 1;
                EditorUtility.SetDirty(sr);
            }
        }

        // 2. Crear o actualizar el corte arquitectónico completo (RoomCutaway_Backdrop)
        Sprite cutawaySp = LoadSpriteFromAsset(cutawayAssetPath);
        if (cutawaySp != null)
        {
            Transform oldCutaway = interiorRoot.Find("RoomCutaway_Backdrop");
            if (oldCutaway != null) Object.DestroyImmediate(oldCutaway.gameObject);

            GameObject backdropGO = new GameObject("RoomCutaway_Backdrop");
            backdropGO.transform.SetParent(interiorRoot, true);
            backdropGO.transform.position = cutawayWorldCenter;
            backdropGO.transform.localScale = Vector3.one;

            SpriteRenderer bsr = backdropGO.AddComponent<SpriteRenderer>();
            bsr.sprite = cutawaySp;
            bsr.drawMode = SpriteDrawMode.Simple;
            bsr.color = Color.white;
            bsr.sortingLayerName = "Midground";
            bsr.sortingOrder = 10;

            Material litMat = AssetDatabase.LoadAssetAtPath<Material>(LIT_MAT_PATH);
            if (litMat != null) bsr.sharedMaterial = litMat;
        }

        // 3. Eliminar escuadras flotantes antiguas bajo las losas (las vigas completas ya apoyan directamente en la estructura)
        Transform oldBrackets = interiorRoot.Find("Cainos_Slab_Brackets");
        if (oldBrackets != null) Object.DestroyImmediate(oldBrackets.gameObject);
    }

    /// <summary>
    /// Sustituye TODOS los barriles, cajas y cofres de Level2.unity (exteriores, puestos, sótanos e interiores)
    /// por los diseños auténticos de la librería Cainos (TX Village Props.png y TX Chest Animation.png).
    /// </summary>
    private static void SwapAllBarrelsCratesAndChestsToCainos()
    {
        Dictionary<string, Sprite> cainos = LoadCainosSprites();
        Sprite barrelSp = GetCainos(cainos, "Barrel", null);
        Sprite crateLargeSp = GetCainos(cainos, "Crate Large", null);
        Sprite crateSmallSp = GetCainos(cainos, "Crate Small", null);

        // 1. Reemplazar cualquier barril o caja en toda la escena (Exterior + Interiores)
        SpriteRenderer[] allRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (SpriteRenderer sr in allRenderers)
        {
            if (sr == null || sr.gameObject == null) continue;
            string goName = sr.gameObject.name;
            string spName = sr.sprite != null ? sr.sprite.name : "";

            if (barrelSp != null && (goName.StartsWith("Barrel") || goName.Contains("Cellar_Barrel") || spName.Contains("Placeholder_Market_Barrel") || spName.Contains("Env_Barrel")))
            {
                sr.sprite = barrelSp;
                sr.drawMode = SpriteDrawMode.Simple;
                BoxCollider2D col = sr.GetComponent<BoxCollider2D>();
                if (col != null && !col.isTrigger)
                {
                    col.size = barrelSp.bounds.size;
                    col.offset = barrelSp.bounds.center;
                }
                Physics2D.SyncTransforms();
                float surfY = Level2CompleteFixer.FindBestSurfaceBelow(sr.bounds.center.x, sr.bounds.min.y + 0.8f);
                SnapSpriteBottomToY(sr.gameObject, surfY);
                EditorUtility.SetDirty(sr.gameObject);
            }
            else if (crateLargeSp != null && (goName.StartsWith("Crates_Stack") || goName.Contains("Cellar_Crate") || goName.Contains("Abandoned_Crate") || spName.Contains("Placeholder_Market_Crate") || spName.Contains("Env_Crate")))
            {
                Sprite chosenCrate = goName.Contains("Small") ? (crateSmallSp != null ? crateSmallSp : crateLargeSp) : crateLargeSp;
                sr.sprite = chosenCrate;
                sr.drawMode = SpriteDrawMode.Simple;
                BoxCollider2D col = sr.GetComponent<BoxCollider2D>();
                if (col != null && !col.isTrigger)
                {
                    col.size = chosenCrate.bounds.size;
                    col.offset = chosenCrate.bounds.center;
                }
                Physics2D.SyncTransforms();
                float surfY = Level2CompleteFixer.FindBestSurfaceBelow(sr.bounds.center.x, sr.bounds.min.y + 0.8f);
                SnapSpriteBottomToY(sr.gameObject, surfY);
                EditorUtility.SetDirty(sr.gameObject);
            }
        }

        // 2. Reemplazar TODOS los cofres Level2Chest y TreasureChest por los cofres animados de Cainos (TX Chest Animation.png)
        Sprite[] chestAtlasSprites = AssetDatabase.LoadAllAssetsAtPath(CAINOS_CHEST_PATH).OfType<Sprite>().ToArray();
        if (chestAtlasSprites.Length > 0)
        {
            Level2Chest[] allChests = Object.FindObjectsByType<Level2Chest>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Level2Chest chest in allChests)
            {
                if (chest == null) continue;
                string kind = chest.name switch
                {
                    "Chest_Int_House4_Royal" => "Golden",
                    "Chest_Int_House2_Locked" => "Iron",
                    "Chest_Fly_Z3_CanalPerch" or "Chest_Z5_MarketPlaza" or "Chest_Int_House1_Upper" or "Chest_Int_House4_CellarAlch" => "Silver",
                    _ => "Wooden",
                };

                Sprite[] frames = Enumerable.Range(0, 7)
                    .Select(i => chestAtlasSprites.FirstOrDefault(sp => sp.name == $"TX Chest Animation {kind} {i}"))
                    .Where(sp => sp != null)
                    .ToArray();

                if (frames.Length == 7)
                {
                    SpriteRenderer sr = chest.GetComponent<SpriteRenderer>();
                    SerializedObject so = new SerializedObject(chest);
                    so.FindProperty("closedSprite").objectReferenceValue = frames[0];
                    so.FindProperty("openSprite").objectReferenceValue = frames[6];
                    SerializedProperty list = so.FindProperty("openFrames");
                    if (list != null)
                    {
                        list.arraySize = frames.Length;
                        for (int i = 0; i < frames.Length; i++)
                        {
                            list.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
                        }
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();

                    if (sr != null)
                    {
                        sr.sprite = frames[0];
                        Physics2D.SyncTransforms();
                        float surfY = Level2CompleteFixer.FindBestSurfaceBelow(chest.transform.position.x, sr.bounds.min.y + 0.8f);
                        SnapSpriteBottomToY(chest.gameObject, surfY);
                        BoxCollider2D col = chest.GetComponent<BoxCollider2D>();
                        if (col != null)
                        {
                            col.isTrigger = true;
                            col.size = new Vector2(1.45f, 1.35f);
                            col.offset = new Vector2(0f, 0.45f);
                        }
                    }
                    EditorUtility.SetDirty(chest.gameObject);
                }
            }
        }
    }

    private static void ResetAllChestsAndPotionStateForCleanPlay()
    {
        // Limpiar cualquier sesión previa en PlayerPrefs para que todos los cofres entreguen sus pociones e ingredientes al jugar
        PlayerPrefs.DeleteKey("L2_Session_Active");
        if (IngredientInventory.Instance != null)
        {
            IngredientInventory.Instance.ResetProgress();
        }
        if (KeyInventory.Instance != null)
        {
            KeyInventory.Instance.ResetExplorationProgress();
        }

        Level2Chest[] allChests = Object.FindObjectsByType<Level2Chest>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Level2Chest c in allChests)
        {
            if (c == null) continue;
            c.ResetChestState();
            EditorUtility.SetDirty(c);
        }

        GameManager gm = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        if (gm != null)
        {
            gm.BlindCharges = 0;
            gm.HypnotizeCharges = 0;
            gm.SleepCharges = 0;
            gm.Score = 0;
            gm.Lives = gm.MaxHearts;
            EditorUtility.SetDirty(gm);
        }
    }

    private static void UpgradeKeysVisibilityAndGrounding()
    {
        Sprite keySprite = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Interactive_Key.png");
        KeyItem[] keys = Object.FindObjectsByType<KeyItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (KeyItem k in keys)
        {
            if (k == null) continue;
            k.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

            SpriteRenderer sr = k.GetComponent<SpriteRenderer>();
            if (sr != null && keySprite != null)
            {
                sr.sprite = keySprite;
                sr.color = Color.white;
                sr.sortingLayerName = "Gameplay";
                sr.sortingOrder = 6;
            }

            if (k.GetComponent<Level2CatPickupGuard>() == null)
            {
                k.gameObject.AddComponent<Level2CatPickupGuard>();
            }
            if (k.GetComponent<PickupGlint>() == null)
            {
                k.gameObject.AddComponent<PickupGlint>();
            }

            float surfY = Level2CompleteFixer.FindBestSurfaceBelow(k.transform.position.x, k.transform.position.y + 0.8f);
            SnapSpriteBottomToY(k.gameObject, surfY);
            EditorUtility.SetDirty(k.gameObject);
        }

        // Despejar el extremo derecho de Alley_LowCeiling (Zona 4) para que Chest_Z4_CatTunnelExit (x = 115.2)
        // quede con techo libre y permita volver a forma humana tras cruzar el pasaje felino
        GameObject alleyCeiling = GameObject.Find("Alley_LowCeiling") ?? GameObject.Find("Environment/Zone_4_CallejonGato/CatPassage_MarketAlley/Alley_LowCeiling");
        if (alleyCeiling != null)
        {
            alleyCeiling.transform.position = new Vector3(108.8f, alleyCeiling.transform.position.y, 0f);
            BoxCollider2D acol = alleyCeiling.GetComponent<BoxCollider2D>();
            if (acol != null) acol.size = new Vector2(9.6f, acol.size.y);
            SpriteRenderer asr = alleyCeiling.GetComponent<SpriteRenderer>();
            if (asr != null && asr.drawMode == SpriteDrawMode.Tiled) asr.size = new Vector2(9.6f, asr.size.y);
            Physics2D.SyncTransforms();
            EditorUtility.SetDirty(alleyCeiling);
        }

        // Actualizar el sprite del cobertizo del callejón del gato (Canopy_0..3) alineado con el tope físico de Alley_LowCeiling (y=1.55)
        Sprite canopySp = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Placeholder_Roof_Platform.png");
        if (canopySp == null) canopySp = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Placeholder_Market_Canopy_CatTunnel.png");
        GameObject catPassageRoot = GameObject.Find("Environment/Zone_4_CallejonGato/CatPassage_MarketAlley");
        if (catPassageRoot != null && canopySp != null)
        {
            float ceilingTopY = 1.55f;
            if (alleyCeiling != null && alleyCeiling.GetComponent<BoxCollider2D>() != null)
            {
                ceilingTopY = alleyCeiling.GetComponent<BoxCollider2D>().bounds.max.y;
            }
            foreach (SpriteRenderer csr in catPassageRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (csr != null && csr.gameObject.name.StartsWith("Canopy_"))
                {
                    csr.sprite = canopySp;
                    csr.drawMode = SpriteDrawMode.Tiled;
                    csr.size = new Vector2(2.4f, 0.36f);
                    csr.color = Color.white;
                    csr.transform.position = new Vector3(csr.transform.position.x, ceilingTopY - 0.18f, 0f);
                    EditorUtility.SetDirty(csr);
                }
            }
        }

        // Igualar el ancho visual (SpriteRenderer) al ancho físico (BoxCollider2D) en las 4 plataformas elevadas exteriores usando un sprite FullRect limpio
        Sprite extRoofPlankSp = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Placeholder_Roof_Platform.png");
        if (extRoofPlankSp == null) extRoofPlankSp = LoadSpriteFromAsset("Assets/Art/Environment/Level2/Interior_Floor_Planks.png");
        string[] extRoofs =
        {
            "Environment/Zone_4_CallejonGato/Roof_Z4_OverAlley",
            "Environment/Zone_5_GranMercado/Roof_Z5_Low_1",
            "Environment/Zone_5_GranMercado/Roof_Z5_Mid_2",
            "Environment/Zone_5_GranMercado/Roof_Z5_HighWalk"
        };
        foreach (string rPath in extRoofs)
        {
            GameObject rGo = GameObject.Find(rPath);
            if (rGo == null) continue;
            BoxCollider2D rCol = rGo.GetComponent<BoxCollider2D>();
            SpriteRenderer rSr = rGo.GetComponent<SpriteRenderer>();
            if (rCol != null && rSr != null)
            {
                if (extRoofPlankSp != null) rSr.sprite = extRoofPlankSp;
                rSr.drawMode = SpriteDrawMode.Tiled;
                rSr.size = new Vector2(rCol.size.x, 0.42f);
                EditorUtility.SetDirty(rSr);
            }
            Transform sway = rGo.transform.Find("Awning_Sway_Visual");
            if (sway != null)
            {
                sway.localScale = Vector3.one;
                EditorUtility.SetDirty(sway);
            }
        }

        // Limpiar cualquier WorldLootPopup residual de pruebas anteriores y reactivar Environment/Parallax
        GameObject[] allSceneObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject g in allSceneObjects)
        {
            if (g == null) continue;
            if (g.name.Contains("WorldLootPopup"))
            {
                Object.DestroyImmediate(g);
            }
            else if (g.name.StartsWith("Sign_House"))
            {
                g.SetActive(false);
                EditorUtility.SetDirty(g);
            }
        }

        GameObject envRoot = GameObject.Find("Environment");
        if (envRoot != null)
        {
            Transform parallax = envRoot.transform.Find("Parallax");
            if (parallax != null) parallax.gameObject.SetActive(true);
        }

        // Asegurar que la Bruja tenga vuelo habilitado y parámetros ágiles para alcanzar todos los cofres elevados
        GameObject witch = GameObject.Find("Witch");
        if (witch != null)
        {
            witch.transform.position = new Vector3(1.5f, 0.17f, 0f);
            PlayerController pc = witch.GetComponent<PlayerController>();
            if (pc != null)
            {
                SerializedObject soPc = new SerializedObject(pc);
                if (soPc.FindProperty("canFly") != null) soPc.FindProperty("canFly").boolValue = true;
                if (soPc.FindProperty("flySpeed") != null) soPc.FindProperty("flySpeed").floatValue = 6.5f;
                if (soPc.FindProperty("flyHorizontalSpeed") != null) soPc.FindProperty("flyHorizontalSpeed").floatValue = 6.8f;
                if (soPc.FindProperty("maxFlightDuration") != null) soPc.FindProperty("maxFlightDuration").floatValue = 7.5f;
                if (soPc.FindProperty("energyRechargePerSecond") != null) soPc.FindProperty("energyRechargePerSecond").floatValue = 1.25f;
                soPc.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(pc);
            }
            EditorUtility.SetDirty(witch);
        }

        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(1.5f, 1.5f, -10f);
        }

        Level2GroundingTool.AlignMenu();
    }

    private static void RaiseInteriorCeilingForFlightLoft(Transform interiorRoot, float centerX, float bottomFloorY, float newCeilingY, float width, Sprite timberWall)
    {
        foreach (string cName in new[] { "Ceiling_Solid", "Ceiling" })
        {
            Transform ceilingTr = interiorRoot.Find(cName);
            if (ceilingTr != null)
            {
                ceilingTr.position = new Vector3(ceilingTr.position.x, newCeilingY, 0f);
                EditorUtility.SetDirty(ceilingTr.gameObject);
            }
        }

        InteriorZone zone = interiorRoot.GetComponent<InteriorZone>();
        if (zone != null)
        {
            zone.Configure(
                zone.InteriorId,
                zone.HouseDisplayName,
                zone.FloorsCount,
                new Vector2(centerX - width * 0.5f - 1.2f, bottomFloorY - 1.2f),
                new Vector2(centerX + width * 0.5f + 1.2f, newCeilingY + 1.2f));
            EditorUtility.SetDirty(zone);
        }

        float totalH = newCeilingY - bottomFloorY;
        float midY = bottomFloorY + totalH * 0.5f;
        foreach (string wallName in new[] { "LeftWall_Solid", "RightWall_Solid", "Wall_Left", "Wall_Right" })
        {
            Transform wTr = interiorRoot.Find(wallName);
            if (wTr != null)
            {
                wTr.position = new Vector3(wTr.position.x, midY, 0f);
                BoxCollider2D bc = wTr.GetComponent<BoxCollider2D>();
                if (bc != null)
                {
                    bc.size = new Vector2(bc.size.x, totalH + 0.8f);
                }
                EditorUtility.SetDirty(wTr.gameObject);
            }
        }
    }

    /// <summary>
    /// Convierte los peldaños del sótano (Cellar_WalkableSteps) en una escalera de madera colonial de Cainos
    /// usando "Obstacle Platform X32" en cada peldaño, tramos diagonales "Stairs X48" y postes verticales.
    /// </summary>
    private static void AdjustCellarStepsRange(Transform interiorRoot, float startX, float endX, Dictionary<string, Sprite> cainos)
    {
        Transform stepsRoot = interiorRoot.Find("Cellar_WalkableSteps");
        if (stepsRoot == null) return;

        Sprite stepPlatformSp = GetCainos(cainos, "Obstacle Platform X32", null);
        Sprite pillarSp = GetCainos(cainos, "Ladder 02 Pillar B", null);
        Sprite stairsDiagSp = GetCainos(cainos, "Stairs X48", null);

        // Limpiar adornos previos bajo los peldaños
        Transform oldDecor = stepsRoot.Find("Cainos_Stair_Structure");
        if (oldDecor != null) Object.DestroyImmediate(oldDecor.gameObject);
        GameObject stairStruct = new GameObject("Cainos_Stair_Structure");
        stairStruct.transform.SetParent(stepsRoot, false);

        int stepCount = 8;
        for (int i = 1; i <= stepCount; i++)
        {
            Transform step = stepsRoot.Find($"CellarStep_{i}");
            if (step == null) continue;
            float t = (float)i / stepCount;
            float stepX = Mathf.Lerp(startX, endX, t);
            float stepTopY = 24.75f + i * 0.50f;
            float stepH = 0.24f;
            step.position = new Vector3(stepX, stepTopY - stepH * 0.5f, 0f);

            SpriteRenderer sr = step.GetComponent<SpriteRenderer>();
            if (sr != null && stepPlatformSp != null)
            {
                sr.sprite = stepPlatformSp;
                sr.drawMode = SpriteDrawMode.Simple;
                sr.color = Color.white;
                sr.sortingLayerName = "Gameplay";
                sr.sortingOrder = 0;
            }

            // Poste vertical de soporte bajo los peldaños pares
            if (pillarSp != null && (i % 2 == 0))
            {
                GameObject post = new GameObject($"StairPost_{i}");
                post.transform.SetParent(stairStruct.transform, true);
                post.transform.position = new Vector3(stepX, 24.75f + (stepTopY - 24.75f) * 0.5f, 0f);
                post.transform.localScale = new Vector3(1f, (stepTopY - 24.75f) / 1.1f, 1f);
                SpriteRenderer psr = post.AddComponent<SpriteRenderer>();
                psr.sprite = pillarSp;
                psr.sortingLayerName = "Gameplay";
                psr.sortingOrder = -1;
            }

            EditorUtility.SetDirty(step.gameObject);
        }

        if (stairsDiagSp != null)
        {
            for (int s = 0; s < 2; s++)
            {
                float sx = Mathf.Lerp(startX, endX, s == 0 ? 0.30f : 0.72f);
                float sy = 24.75f + (s == 0 ? 1.15f : 2.65f);
                GameObject diag = new GameObject($"Cainos_StairsDiag_{s}");
                diag.transform.SetParent(stairStruct.transform, true);
                diag.transform.position = new Vector3(sx, sy, 0f);
                diag.transform.localScale = new Vector3(endX >= startX ? -1f : 1f, 1f, 1f);
                SpriteRenderer dsr = diag.AddComponent<SpriteRenderer>();
                dsr.sprite = stairsDiagSp;
                dsr.sortingLayerName = "Gameplay";
                dsr.sortingOrder = -1;
            }
        }
    }

    private static Transform RecreateDecorRoot(Transform parent, string name)
    {
        Transform old = parent.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        return go.transform;
    }

    private static void SpawnGroundedDecor(Transform parent, string name, Vector2 groundPos, Sprite sprite, float scale, int sortingOrder)
    {
        if (sprite == null) return;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        go.transform.position = new Vector3(groundPos.x, groundPos.y + 0.5f, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.84f, 0.80f, 0.76f, 1f);
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = sortingOrder;

        Material litMat = AssetDatabase.LoadAssetAtPath<Material>(LIT_MAT_PATH);
        if (litMat != null) sr.sharedMaterial = litMat;

        SnapSpriteBottomToY(go, groundPos.y);
    }

    private static void SpawnWallDecor(Transform parent, string name, Vector3 worldPos, Sprite sprite, float scale, int sortingOrder)
    {
        if (sprite == null) return;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        go.transform.position = worldPos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.84f, 0.80f, 0.76f, 1f);
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = sortingOrder;

        Material litMat = AssetDatabase.LoadAssetAtPath<Material>(LIT_MAT_PATH);
        if (litMat != null) sr.sharedMaterial = litMat;
    }

    private static void SpawnCainosTorch(Transform parent, string name, Vector2 pos)
    {
        GameObject torchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CAINOS_PREFAB_DIR + "PF Village Props - Torch.prefab");
        if (torchPrefab == null) return;

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(torchPrefab);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        Material lit = AssetDatabase.LoadAssetAtPath<Material>(LIT_MAT_PATH);
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            r.sortingLayerName = "Gameplay";
            r.sortingOrder = r is SpriteRenderer ? 0 : 1;
            if (r is SpriteRenderer && lit != null) r.sharedMaterial = lit;
        }
    }

    private static void SpawnWarmCandleLight(Transform parent, string name, Vector3 worldPos, Color color, float radius, float intensity)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = worldPos;

        Light2D light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = radius * 0.25f;
        light.intensity = intensity;
        light.falloffIntensity = 0.55f;
    }

    private static Dictionary<string, Sprite> LoadCainosSprites()
    {
        Dictionary<string, Sprite> dict = new Dictionary<string, Sprite>();
        Object[] all = AssetDatabase.LoadAllAssetsAtPath(CAINOS_PROPS_PATH);
        if (all != null)
        {
            foreach (Object o in all)
            {
                if (o is Sprite sp && !dict.ContainsKey(sp.name))
                {
                    dict[sp.name] = sp;
                }
            }
        }
        return dict;
    }

    private static Sprite GetCainos(Dictionary<string, Sprite> dict, string key, Sprite fallback)
    {
        if (dict != null)
        {
            if (dict.TryGetValue(key, out Sprite sp) && sp != null) return sp;
            if (dict.TryGetValue("TX Village Props - " + key, out Sprite spPrefixed) && spPrefixed != null) return spPrefixed;
        }
        return fallback;
    }

    private static Sprite LoadSpriteFromAsset(string path)
    {
        Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp != null) return sp;
        Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
        if (all != null)
        {
            foreach (Object o in all)
            {
                if (o is Sprite s) return s;
            }
        }
        return null;
    }

    private static void SnapSpriteBottomToY(GameObject go, float targetY)
    {
        if (go == null) return;
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            go.transform.position = new Vector3(go.transform.position.x, targetY, go.transform.position.z);
            return;
        }
        Physics2D.SyncTransforms();
        if (Level2GroundingTool.TryOpaqueBottom(sr, out float opaqueBottom))
        {
            float deltaOpaque = targetY - opaqueBottom;
            go.transform.position += new Vector3(0f, deltaOpaque, 0f);
            return;
        }
        float currentBottom = sr.bounds.min.y;
        float delta = targetY - currentBottom;
        go.transform.position += new Vector3(0f, delta, 0f);
    }

    private static Sprite CreateVerticalWardSprite()
    {
        int w = 24, h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < h; y++)
        {
            float edgeY = Mathf.Sin((float)y / (h - 1) * Mathf.PI);
            for (int x = 0; x < w; x++)
            {
                float distX = Mathf.Abs(x - (w - 1) * 0.5f) / (w * 0.5f);
                float a = Mathf.Clamp01(1f - distX) * edgeY;
                float rune = (y % 16 < 3 && distX < 0.45f) ? 1.0f : 0.45f;
                tex.SetPixel(x, y, new Color(1f, 0.72f, 0.35f, a * rune));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
    }
}
#endif
