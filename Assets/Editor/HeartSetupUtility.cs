using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Utilidad de Editor para crear el prefab HeartPickup y configurar HeartsUI en el Canvas de Level1.
/// </summary>
public static class HeartSetupUtility
{
    private const string PrefabFolder = "Assets/Prefabs";
    private const string HeartPrefabPath = "Assets/Prefabs/HeartPickup.prefab";

    [MenuItem("Brujas/3. Nivel/Configurar HeartsUI y Prefab HeartPickup")]
    public static void SetupHeartsUIAndPrefab()
    {
        CreateHeartPickupPrefab();
        SetupHeartsUIInScene();
    }

    public static void CreateHeartPickupPrefab()
    {
        if (!Directory.Exists(PrefabFolder))
        {
            Directory.CreateDirectory(PrefabFolder);
        }

        Sprite heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Item_Heart.png");
        if (heartSprite == null)
        {
            Debug.LogError("[HeartSetupUtility] No se encontró el sprite Assets/Art/Environment/Item_Heart.png");
            return;
        }

        GameObject tempObj = new GameObject("HeartPickup");
        SpriteRenderer sr = tempObj.AddComponent<SpriteRenderer>();
        sr.sprite = heartSprite;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 10;

        CircleCollider2D col = tempObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.45f;

        tempObj.AddComponent<HeartPickup>();

        PrefabUtility.SaveAsPrefabAsset(tempObj, HeartPrefabPath);
        Object.DestroyImmediate(tempObj);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[HeartSetupUtility] Prefab creado en {HeartPrefabPath}");
    }

    public static void SetupHeartsUIInScene()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[HeartSetupUtility] No se encontró Canvas en la escena.");
            return;
        }

        // Cargar sprites
        Sprite fullSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/ui_icon_heart_full.png");
        Sprite emptySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/ui_icon_heart_empty.png");
        Sprite[] breakSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/UI/Heart_Break.png")
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();
        Sprite[] healSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/UI/Heart_Heal.png")
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        // Buscar o crear HeartsUI bajo el Canvas
        HeartsUI heartsUI = canvas.GetComponentInChildren<HeartsUI>(true);
        if (heartsUI == null)
        {
            GameObject heartsObj = new GameObject("HeartsUI", typeof(RectTransform));
            heartsObj.transform.SetParent(canvas.transform, false);
            heartsUI = heartsObj.AddComponent<HeartsUI>();
        }

        heartsUI.SetFrames(fullSprite, emptySprite, breakSprites, healSprites);

        // Desactivar o limpiar los heartIcons viejos de HUDController para no duplicar
        HUDController hud = Object.FindFirstObjectByType<HUDController>();
        if (hud != null && hud.heartIcons != null)
        {
            foreach (var oldIcon in hud.heartIcons)
            {
                if (oldIcon != null)
                {
                    oldIcon.gameObject.SetActive(false);
                }
            }
        }

        EditorUtility.SetDirty(heartsUI.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[HeartSetupUtility] HeartsUI configurado en escena con éxito.");
    }
}
