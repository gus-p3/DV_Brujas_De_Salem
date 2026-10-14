using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Crea el prefab Assets/Prefabs/Guard.prefab configurado con:
/// Rigidbody2D, BoxCollider2D, SpriteRenderer, Animator (Guard.controller),
/// EnemySight y PatrolRoute.
/// </summary>
public static class GuardPrefabBuilder
{
    private const string PrefabFolder = "Assets/Prefabs";
    private const string PrefabPath = PrefabFolder + "/Guard.prefab";
    private const string ControllerPath = "Assets/Animations/Guard/Guard.controller";
    private const string IdleSheetPath = "Assets/Art/Characters/Guard/Guard_Idle.png";

    [MenuItem("Brujas/Prefabs/Crear prefab Guard")]
    public static GameObject CreateGuardPrefab()
    {
        if (!Directory.Exists(PrefabFolder))
        {
            Directory.CreateDirectory(PrefabFolder);
        }

        // Asegurar que existe el tag Enemy
        EnsureTag("Enemy");

        // 1. Root GameObject
        GameObject root = new GameObject("Guard");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0) root.layer = enemyLayer;
        root.tag = "Enemy";

        // Rigidbody2D
        Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // BoxCollider2D
        BoxCollider2D col = root.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.7f, 1.7f);
        col.offset = new Vector2(0f, 0.85f);

        // 2. Child Visual
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        Sprite firstIdleSprite = AssetDatabase.LoadAllAssetRepresentationsAtPath(IdleSheetPath)
            .OfType<Sprite>()
            .FirstOrDefault();
        if (firstIdleSprite != null)
        {
            sr.sprite = firstIdleSprite;
        }
        sr.sortingLayerName = "Gameplay";
        sr.sortingOrder = 4;

        Animator anim = visual.AddComponent<Animator>();
        RuntimeAnimatorController rac = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        if (rac != null)
        {
            anim.runtimeAnimatorController = rac;
        }

        // 3. Child LanternOrigin
        GameObject lantern = new GameObject("LanternOrigin");
        lantern.transform.SetParent(root.transform, false);
        lantern.transform.localPosition = new Vector3(0.45f, 0.85f, 0f);

        // 4. EnemySight
        EnemySight sight = root.AddComponent<EnemySight>();
        SerializedObject soSight = new SerializedObject(sight);
        soSight.FindProperty("viewDistance").floatValue = 6.5f;
        soSight.FindProperty("viewAngle").floatValue = 55f;
        soSight.FindProperty("rayCount").intValue = 30;
        soSight.FindProperty("obstacleMask").intValue = 1 << LayerMask.NameToLayer("Ground");
        soSight.FindProperty("targetMask").intValue = 1 << LayerMask.NameToLayer("Player");
        soSight.FindProperty("lanternOrigin").objectReferenceValue = lantern.transform;
        soSight.FindProperty("facingReference").objectReferenceValue = visual.transform;
        soSight.FindProperty("suspicionTime").floatValue = 1.2f;
        soSight.FindProperty("lostTargetDelay").floatValue = 2.0f;
        soSight.FindProperty("captureDistance").floatValue = 1.2f;
        soSight.FindProperty("coneSortingLayer").stringValue = "Gameplay";
        soSight.FindProperty("coneSortingOrder").intValue = 3;
        soSight.ApplyModifiedPropertiesWithoutUndo();

        // 5. PatrolRoute
        PatrolRoute patrol = root.AddComponent<PatrolRoute>();
        SerializedObject soPatrol = new SerializedObject(patrol);
        soPatrol.FindProperty("walkSpeed").floatValue = 2.0f;
        soPatrol.FindProperty("chaseSpeed").floatValue = 3.6f;
        soPatrol.FindProperty("waitTimeAtPoint").floatValue = 1.5f;
        soPatrol.FindProperty("arrivalTolerance").floatValue = 0.25f;
        soPatrol.FindProperty("captureDistance").floatValue = 1.2f;
        soPatrol.FindProperty("groundMask").intValue = 1 << LayerMask.NameToLayer("Ground");
        soPatrol.FindProperty("edgeCheckOffset").floatValue = 0.45f;
        soPatrol.FindProperty("edgeCheckRayDistance").floatValue = 1.2f;
        soPatrol.FindProperty("enemySight").objectReferenceValue = sight;
        soPatrol.FindProperty("animator").objectReferenceValue = anim;
        soPatrol.FindProperty("visualTransform").objectReferenceValue = visual.transform;
        soPatrol.ApplyModifiedPropertiesWithoutUndo();

        // 6. Guardar como Prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GuardPrefabBuilder] Prefab creado exitosamente en {PrefabPath}");
        return prefab;
    }

    private static void EnsureTag(string tag)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        bool exists = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
            {
                exists = true;
                break;
            }
        }
        if (!exists)
        {
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
        }
    }
}
