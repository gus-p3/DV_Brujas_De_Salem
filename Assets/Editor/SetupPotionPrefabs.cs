using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public static class SetupPotionPrefabs
{
    [MenuItem("Brujas/3. Pociones/Crear Prefabs y Configurar HUD")]
    public static void Execute()
    {
        EnsureFolder("Assets/Prefabs");

        // 1. Crear Prefabs de Proyectiles
        GameObject blindProj = CreateProjectilePrefab("PotionProjectile_Blind", PotionType.Blind, 5f, "Assets/Art/Effects/PotionProjectile_Blind.png");
        GameObject hypnoProj = CreateProjectilePrefab("PotionProjectile_Hypnotize", PotionType.Hypnotize, 4f, "Assets/Art/Effects/PotionProjectile_Hypnotize.png");
        GameObject sleepProj = CreateProjectilePrefab("PotionProjectile_Sleep", PotionType.Sleep, 6f, "Assets/Art/Effects/PotionProjectile_Sleep.png");

        // 2. Crear Prefabs de Recolectables (Pickups)
        CreatePickupPrefab("PotionPickup_Blind", PotionType.Blind, "Assets/Art/Items/PotionItem_Blind.png");
        CreatePickupPrefab("PotionPickup_Hypnotize", PotionType.Hypnotize, "Assets/Art/Items/PotionItem_Hypnotize.png");
        CreatePickupPrefab("PotionPickup_Sleep", PotionType.Sleep, "Assets/Art/Items/PotionItem_Sleep.png");

        // 3. Configurar Player y HUD en la escena activa
        ConfigurePlayerAndHUD(blindProj, hypnoProj, sleepProj);

        AssetDatabase.SaveAssets();
        Debug.Log("[SetupPotionPrefabs] ¡Prefabs de pociones creados y escena configurada con éxito!");
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }

    private static GameObject CreateProjectilePrefab(string name, PotionType type, float duration, string spritePath)
    {
        string prefabPath = $"Assets/Prefabs/{name}.prefab";
        GameObject go = new GameObject(name);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 12;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.25f;
        col.isTrigger = false;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 2.2f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        PotionProjectile proj = go.AddComponent<PotionProjectile>();
        SerializedObject so = new SerializedObject(proj);
        so.FindProperty("potionType").enumValueIndex = (int)type;
        so.FindProperty("effectDuration").floatValue = duration;
        so.FindProperty("splashRadius").floatValue = 1.6f;
        so.FindProperty("horizontalSpeed").floatValue = 7f;
        so.FindProperty("verticalSpeed").floatValue = 6f;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreatePickupPrefab(string name, PotionType type, string spritePath)
    {
        string prefabPath = $"Assets/Prefabs/{name}.prefab";
        GameObject go = new GameObject(name);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 8;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.45f;
        col.isTrigger = true;

        PotionPickup pickup = go.AddComponent<PotionPickup>();
        SerializedObject so = new SerializedObject(pickup);
        so.FindProperty("potionType").enumValueIndex = (int)type;
        so.FindProperty("amount").intValue = 1;
        so.FindProperty("maxCharges").intValue = 3;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static void ConfigurePlayerAndHUD(GameObject blindProj, GameObject hypnoProj, GameObject sleepProj)
    {
        // Configurar PotionInventory en Player
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            PotionInventory inv = player.GetComponent<PotionInventory>();
            if (inv == null) inv = player.gameObject.AddComponent<PotionInventory>();

            SerializedObject soInv = new SerializedObject(inv);
            soInv.FindProperty("blindProjectilePrefab").objectReferenceValue = blindProj;
            soInv.FindProperty("hypnotizeProjectilePrefab").objectReferenceValue = hypnoProj;
            soInv.FindProperty("sleepProjectilePrefab").objectReferenceValue = sleepProj;
            soInv.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(player.gameObject);
        }

        // Configurar HUDController
        HUDController hud = Object.FindFirstObjectByType<HUDController>();
        if (hud != null)
        {
            SerializedObject soHud = new SerializedObject(hud);

            // Buscar o crear marcos y etiquetas de tecla bajo cada texto de cargas
            TextMeshProUGUI[] chargeTexts = new TextMeshProUGUI[]
            {
                hud.blindChargesText,
                hud.hypnotizeChargesText,
                hud.sleepChargesText
            };

            Image[] frames = new Image[3];
            TextMeshProUGUI[] keyHints = new TextMeshProUGUI[3];
            string[] keys = new string[] { "[1]", "[2]", "[3]" };

            for (int i = 0; i < 3; i++)
            {
                if (chargeTexts[i] == null) continue;

                Transform parentSlot = chargeTexts[i].transform.parent;
                if (parentSlot == null) parentSlot = chargeTexts[i].transform;

                // Marco
                Transform frameTrans = parentSlot.Find("SelectionFrame");
                if (frameTrans == null)
                {
                    GameObject frameGo = new GameObject("SelectionFrame");
                    frameGo.transform.SetParent(parentSlot, false);
                    Image img = frameGo.AddComponent<Image>();
                    img.color = new Color(1f, 0.85f, 0.2f, 0.85f);
                    img.raycastTarget = false;

                    RectTransform rt = frameGo.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.offsetMin = new Vector2(-6f, -6f);
                    rt.offsetMax = new Vector2(6f, 6f);
                    frameTrans = frameGo.transform;
                }
                frames[i] = frameTrans.GetComponent<Image>();

                // Etiqueta de Tecla
                Transform keyTrans = parentSlot.Find("KeyHint");
                if (keyTrans == null)
                {
                    GameObject keyGo = new GameObject("KeyHint");
                    keyGo.transform.SetParent(parentSlot, false);
                    TextMeshProUGUI tmp = keyGo.AddComponent<TextMeshProUGUI>();
                    tmp.text = keys[i];
                    tmp.fontSize = 16f;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = new Color(1f, 1f, 1f, 0.9f);

                    RectTransform rt = keyGo.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0f);
                    rt.anchorMax = new Vector2(0.5f, 0f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -2f);
                    rt.sizeDelta = new Vector2(40f, 20f);
                    keyTrans = keyGo.transform;
                }
                keyHints[i] = keyTrans.GetComponent<TextMeshProUGUI>();
            }

            SerializedProperty framesProp = soHud.FindProperty("potionSelectionFrames");
            framesProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                framesProp.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            }

            SerializedProperty keysProp = soHud.FindProperty("potionKeyHints");
            keysProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                keysProp.GetArrayElementAtIndex(i).objectReferenceValue = keyHints[i];
            }

            soHud.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud.gameObject);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
