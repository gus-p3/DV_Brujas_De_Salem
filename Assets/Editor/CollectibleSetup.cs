using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Crea los prefabs de los coleccionables (rama, paja, cuerda),
/// actualiza el HUD con el contador de escoba y el banner de mensajes,
/// y coloca los coleccionables en la escena Level1.unity.
/// </summary>
public static class CollectibleSetup
{
    private const string PrefabsFolder = "Assets/Prefabs";
    private const string BranchPath = "Assets/Art/Environment/Item_Branch.png";
    private const string StrawPath = "Assets/Art/Environment/Item_Straw.png";
    private const string RopePath = "Assets/Art/Environment/Item_Rope.png";

    [MenuItem("Brujas/Nivel/Configurar recoleccion y escoba en Level 1")]
    public static void Setup()
    {
        if (!Directory.Exists(PrefabsFolder)) Directory.CreateDirectory(PrefabsFolder);

        // 1. Crear prefabs coleccionables
        Sprite branchSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BranchPath);
        Sprite strawSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StrawPath);
        Sprite ropeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RopePath);

        GameObject branchPrefab = CreateCollectiblePrefab("Collectible_Branch", BroomPart.Branch, branchSprite, 100);
        GameObject strawPrefab = CreateCollectiblePrefab("Collectible_Straw", BroomPart.Straw, strawSprite, 100);
        GameObject ropePrefab = CreateCollectiblePrefab("Collectible_Rope", BroomPart.Rope, ropeSprite, 100);

        // 2. Abrir Level1.unity
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity", OpenSceneMode.Single);

        // 3. Añadir BroomInventory a la bruja en escena y en su prefab
        GameObject witch = GameObject.Find("Witch");
        if (witch != null)
        {
            if (witch.GetComponent<BroomInventory>() == null)
            {
                witch.AddComponent<BroomInventory>();
            }
        }

        GameObject witchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Witch.prefab");
        if (witchPrefab != null && witchPrefab.GetComponent<BroomInventory>() == null)
        {
            GameObject wInstance = (GameObject)PrefabUtility.InstantiatePrefab(witchPrefab);
            wInstance.AddComponent<BroomInventory>();
            PrefabUtility.SaveAsPrefabAsset(wInstance, "Assets/Prefabs/Witch.prefab");
            Object.DestroyImmediate(wInstance);
        }

        // 4. Configurar HUD en Level1: MessageBanner y BroomHUD
        SetupHUD(branchSprite, strawSprite, ropeSprite);

        // 5. Instanciar coleccionables en marcadores
        PlaceCollectibleAtSpawn("Item_Spawn_Branch", branchPrefab);
        PlaceCollectibleAtSpawn("Item_Spawn_Straw", strawPrefab);
        PlaceCollectibleAtSpawn("Item_Spawn_Rope", ropePrefab);

        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CollectibleSetup] Recolección, escoba y HUD configurados exitosamente en Level1.");
    }

    private static GameObject CreateCollectiblePrefab(string name, BroomPart part, Sprite sprite, int score)
    {
        string path = $"{PrefabsFolder}/{name}.prefab";
        GameObject go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("Collectible") >= 0 ? LayerMask.NameToLayer("Collectible") : 0;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = 3;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.8f, 0.8f);

        Collectible c = go.AddComponent<Collectible>();
        SerializedObject so = new SerializedObject(c);
        so.FindProperty("part").enumValueIndex = (int)part;
        so.FindProperty("scoreValue").intValue = score;
        so.FindProperty("bobAmplitude").floatValue = 0.15f;
        so.FindProperty("bobSpeed").floatValue = 3.5f;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static void PlaceCollectibleAtSpawn(string spawnName, GameObject prefab)
    {
        GameObject spawn = GameObject.Find(spawnName);
        if (spawn != null && prefab != null)
        {
            // Limpiar hijos existentes en el marcador
            for (int i = spawn.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(spawn.transform.GetChild(i).gameObject);
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = prefab.name;
            instance.transform.SetParent(spawn.transform, false);
            instance.transform.localPosition = Vector3.zero;
        }
    }

    private static void SetupHUD(Sprite branchSprite, Sprite strawSprite, Sprite ropeSprite)
    {
        HUDController hud = Object.FindFirstObjectByType<HUDController>();
        if (hud == null) return;

        Canvas canvas = hud.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // 1. Message Banner
        Transform existingBanner = canvas.transform.Find("MessageBanner");
        GameObject bannerObj;
        if (existingBanner != null)
        {
            bannerObj = existingBanner.gameObject;
        }
        else
        {
            bannerObj = new GameObject("MessageBanner");
            bannerObj.transform.SetParent(canvas.transform, false);

            RectTransform rt = bannerObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 60f);
            rt.sizeDelta = new Vector2(600f, 60f);

            TextMeshProUGUI tmp = bannerObj.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 28f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.95f, 0.6f, 1f); // Dorado cálido
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = new Color32(20, 10, 30, 255);
            bannerObj.SetActive(false);
        }

        hud.messageBannerText = bannerObj.GetComponent<TextMeshProUGUI>();

        // 2. Broom HUD Container (en la esquina superior, bajo o junto a las vidas/stats)
        Transform existingBroomHUD = canvas.transform.Find("BroomHUD");
        GameObject broomHUDObj;
        if (existingBroomHUD != null)
        {
            broomHUDObj = existingBroomHUD.gameObject;
        }
        else
        {
            broomHUDObj = new GameObject("BroomHUD");
            broomHUDObj.transform.SetParent(canvas.transform, false);

            RectTransform rt = broomHUDObj.AddComponent<RectTransform>();
            // Anclado arriba a la derecha, debajo del puntaje/tiempo
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20f, -80f);
            rt.sizeDelta = new Vector2(180f, 45f);

            // Contador de texto
            GameObject txtObj = new GameObject("CounterText");
            txtObj.transform.SetParent(broomHUDObj.transform, false);
            RectTransform txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = new Vector2(0f, 0.5f);
            txtRt.anchorMax = new Vector2(0f, 0.5f);
            txtRt.pivot = new Vector2(0f, 0.5f);
            txtRt.anchoredPosition = new Vector2(0f, 0f);
            txtRt.sizeDelta = new Vector2(90f, 35f);

            TextMeshProUGUI cTmp = txtObj.AddComponent<TextMeshProUGUI>();
            cTmp.fontSize = 18f;
            cTmp.alignment = TextAlignmentOptions.Left;
            cTmp.text = "Escoba: 0/3";
            cTmp.color = new Color(0.95f, 0.92f, 0.8f);

            // Iconos (Rama, Paja, Cuerda)
            Image imgBranch = CreateIcon(broomHUDObj, "Icon_Branch", branchSprite, new Vector2(95f, 0f));
            Image imgStraw = CreateIcon(broomHUDObj, "Icon_Straw", strawSprite, new Vector2(125f, 0f));
            Image imgRope = CreateIcon(broomHUDObj, "Icon_Rope", ropeSprite, new Vector2(155f, 0f));

            BroomHUD bh = broomHUDObj.AddComponent<BroomHUD>();
            SerializedObject soBh = new SerializedObject(bh);
            soBh.FindProperty("counterText").objectReferenceValue = cTmp;
            soBh.FindProperty("branchIcon").objectReferenceValue = imgBranch;
            soBh.FindProperty("strawIcon").objectReferenceValue = imgStraw;
            soBh.FindProperty("ropeIcon").objectReferenceValue = imgRope;
            soBh.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(hud);
    }

    private static Image CreateIcon(GameObject parent, string name, Sprite sprite, Vector2 pos)
    {
        GameObject iconObj = new GameObject(name);
        iconObj.transform.SetParent(parent.transform, false);

        RectTransform rt = iconObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(26f, 26f);

        Image img = iconObj.AddComponent<Image>();
        img.sprite = sprite;
        img.color = new Color(1f, 1f, 1f, 0.25f);
        return img;
    }
}
