using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Construye Assets/Scenes/Level2.unity (escena base aislada del Nivel 2, Acto 2: Pueblo y Mercado).
/// - Abre Level1 de forma ADITIVA y SOLO LECTURA para replicar (clonar) cámara, luz global,
///   managers, HUD_Canvas, EventSystem y CaptureListener. Level1 se cierra SIN guardar.
/// - Instancia el prefab Witch sin overrides.
/// - Crea suelo de prueba, vacío con KillZone y raíces organizadas.
/// No modifica prefabs, scripts compartidos ni configuración global.
/// </summary>
public static class Level2SceneBuilder
{
    public const string Level2Path = "Assets/Scenes/Level2.unity";
    private const string WitchPrefabPath = "Assets/Prefabs/Witch.prefab";
    private const string CheckpointPrefabPath = "Assets/Prefabs/Checkpoint.prefab";
    private const string GroundSpritePath = "Assets/Art/Environment/Env_Ground_Tile.png";
    private const string ReportPath = "Logs/Level2_Build_Report.txt";

    [MenuItem("Brujas/Nivel 2/Construir escena base Level2")]
    public static void BuildLevel2()
    {
        var log = new StringBuilder();
        Scene l2 = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Scene l1 = EditorSceneManager.OpenScene(Level2Audit.Level1Path, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(l2);

        var roots = new Dictionary<string, Transform>();
        foreach (string r in new[] { "Managers", "Environment", "Gameplay", "NPCs", "Ingredients", "UI" })
            roots[r] = new GameObject(r).transform;

        GameObject witch = null;
        try
        {
            // ---------- 1. Clonar infraestructura de Level1 ----------
            System.Type[] infraTypes = { typeof(GameManager), typeof(AudioManager), typeof(TutorialManager),
                typeof(CheckpointManager), typeof(FrenzyController), typeof(CaptureListener),
                typeof(UnityEngine.EventSystems.EventSystem) };
            var copied = new HashSet<GameObject>();

            // Cámara principal
            Camera cam1 = l1.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).FirstOrDefault(c => c.CompareTag("MainCamera"));
            GameObject camCopy = cam1 != null ? Clone(cam1.gameObject, null, log) : null;
            if (cam1 != null) copied.Add(cam1.gameObject);

            // Luz global 2D
            foreach (Light2D l in l1.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light2D>(true)))
                if (l.lightType == Light2D.LightType.Global && copied.Add(l.gameObject)) Clone(l.gameObject, roots["Environment"], log);

            // HUD Canvas
            HUDController hud1 = l1.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<HUDController>(true)).FirstOrDefault();
            if (hud1 != null && copied.Add(hud1.gameObject)) Clone(hud1.gameObject, roots["UI"], log);

            // Managers
            foreach (var t in infraTypes)
            {
                foreach (Component c in l1.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren(t, true)))
                {
                    if (copied.Contains(c.gameObject) || IsUnderCopied(c.transform, copied)) continue;
                    copied.Add(c.gameObject);
                    Clone(c.gameObject, t == typeof(UnityEngine.EventSystems.EventSystem) ? roots["UI"] : roots["Managers"], log);
                }
            }

            // ---------- 2. Bruja (prefab sin overrides) ----------
            GameObject witchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WitchPrefabPath);
            witch = (GameObject)PrefabUtility.InstantiatePrefab(witchPrefab, l2);
            witch.name = "Witch";
            witch.transform.SetParent(roots["Gameplay"], false);
            witch.transform.position = new Vector3(0f, 0.05f, 0f);
            log.AppendLine("Witch instanciada desde " + WitchPrefabPath);

            // Paridad con Level1: la instancia de Witch en Level1 tiene componentes AÑADIDOS en escena
            // (PotionInventory, PlayerInvulnerability) que no están en el prefab. Se replican con los
            // mismos valores serializados; no se toca ningún valor de movimiento/forma del prefab.
            PlayerController pc1 = l1.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerController>(true)).FirstOrDefault();
            GameObject w1 = pc1 != null ? PrefabUtility.GetOutermostPrefabInstanceRoot(pc1.gameObject) : null;
            if (w1 != null)
            {
                foreach (var ac in PrefabUtility.GetAddedComponents(w1))
                {
                    Component srcComp = ac.instanceComponent;
                    Component dst = witch.AddComponent(srcComp.GetType());
                    EditorUtility.CopySerialized(srcComp, dst);
                    log.AppendLine($"Witch: replicado componente añadido de Level1 {srcComp.GetType().Name}");
                }
            }

            // ---------- 3. Limpiar referencias cruzadas a Level1 ----------
            foreach (GameObject g in l2.GetRootGameObjects())
                foreach (MonoBehaviour mb in g.GetComponentsInChildren<MonoBehaviour>(true))
                    FixCrossSceneRefs(mb, l1, witch, log);

            if (camCopy != null)
            {
                CameraFollow follow = camCopy.GetComponent<CameraFollow>();
                if (follow != null)
                {
                    follow.SetTarget(witch.transform);
                    var so = new SerializedObject(follow);
                    var max = so.FindProperty("maxLimit");
                    if (max != null) max.vector2Value = new Vector2(60f, max.vector2Value.y);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                camCopy.transform.position = new Vector3(0f, camCopy.transform.position.y, camCopy.transform.position.z);
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(l1, true); // sin guardar
        }

        // ---------- 4. Suelo de prueba + vacío con KillZone ----------
        int groundLayer = LayerMask.NameToLayer("Ground");
        Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GroundSpritePath);
        Transform env = roots["Environment"];
        CreateGround(env, "Ground_Test_A", -5f, 30f, groundLayer, groundSprite);   // x -5..25
        CreateGround(env, "Ground_Test_B", 29f, 30f, groundLayer, groundSprite);   // x 29..59 (vacío 25..29)

        GameObject kz = new GameObject("KillZone_TestGap");
        kz.transform.SetParent(roots["Gameplay"], false);
        kz.transform.position = new Vector3(27f, -6f, 0f);
        var kzCol = kz.AddComponent<BoxCollider2D>();
        kzCol.isTrigger = true;
        kzCol.size = new Vector2(4f, 2f);
        kz.AddComponent<KillZone>();

        GameObject cpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CheckpointPrefabPath);
        if (cpPrefab != null)
        {
            GameObject cp = (GameObject)PrefabUtility.InstantiatePrefab(cpPrefab, l2);
            cp.name = "Checkpoint_L2_Start";
            cp.transform.SetParent(roots["Gameplay"], false);
            cp.transform.position = new Vector3(3f, 0.17f, 0f);
        }

        foreach (string z in new[] { "Zone_1", "Zone_2", "Zone_3", "Zone_4", "Zone_5", "Zone_6" })
            new GameObject(z).transform.SetParent(env, false);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(l2, Level2Path);
        File.WriteAllText(ReportPath, log.ToString());
        Debug.Log("[Level2SceneBuilder] Level2 guardada. Informe: " + ReportPath + "\n" + log);
    }

    private static GameObject Clone(GameObject src, Transform parent, StringBuilder log)
    {
        GameObject c;
        if (PrefabUtility.IsAnyPrefabInstanceRoot(src))
        {
            // Mantener el vínculo con el prefab (sin modificarlo) y replicar los overrides de la instancia de Level1.
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(src);
            c = (GameObject)PrefabUtility.InstantiatePrefab(source, SceneManager.GetActiveScene());
            PrefabUtility.SetPropertyModifications(c, PrefabUtility.GetPropertyModifications(src));
            foreach (var ac in PrefabUtility.GetAddedComponents(src))
                log.AppendLine($"  AVISO: componente añadido no replicado {ac.instanceComponent.GetType().Name}");
        }
        else
        {
            c = Object.Instantiate(src);
        }
        c.name = src.name;
        if (parent != null) c.transform.SetParent(parent, true);
        log.AppendLine($"Clonado {Level2Audit.Path(src.transform)} (prefabInstance={PrefabUtility.IsPartOfPrefabInstance(src)})");
        return c;
    }

    private static bool IsUnderCopied(Transform t, HashSet<GameObject> copied)
    {
        for (Transform p = t.parent; p != null; p = p.parent) if (copied.Contains(p.gameObject)) return true;
        return false;
    }

    private static void FixCrossSceneRefs(MonoBehaviour mb, Scene l1, GameObject witch, StringBuilder log)
    {
        if (mb == null) return;
        var so = new SerializedObject(mb);
        var it = so.GetIterator();
        bool changed = false;
        while (it.Next(true))
        {
            if (it.propertyType != SerializedPropertyType.ObjectReference || it.objectReferenceValue == null) continue;
            GameObject go = it.objectReferenceValue as GameObject ?? (it.objectReferenceValue as Component)?.gameObject;
            if (go == null || go.scene != l1) continue;
            Object replacement = null;
            if (PrefabUtility.GetOutermostPrefabInstanceRoot(go) is GameObject r && r.GetComponent<PlayerController>() != null)
            {
                replacement = it.objectReferenceValue is GameObject ? (Object)witch
                    : witch.GetComponent(it.objectReferenceValue.GetType());
            }
            log.AppendLine($"RefCruzada {mb.GetType().Name}.{it.propertyPath} -> {go.name} => {(replacement != null ? "Witch(L2)" : "null")}");
            it.objectReferenceValue = replacement;
            changed = true;
        }
        if (changed) so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateGround(Transform parent, string name, float startX, float length, int layer, Sprite sprite)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.layer = layer;
        g.transform.position = new Vector3(startX + length * 0.5f, -2f, 0f);
        var col = g.AddComponent<BoxCollider2D>();
        col.size = new Vector2(length, 4.17f);
        col.offset = new Vector2(0f, 0.085f);
        if (sprite == null) return;
        float w = sprite.rect.width / sprite.pixelsPerUnit;
        int n = Mathf.CeilToInt(length / w);
        for (int i = 0; i < n; i++)
        {
            var t = new GameObject("Tile_" + i);
            t.transform.SetParent(g.transform, false);
            t.transform.localPosition = new Vector3(-length * 0.5f + w * 0.5f + i * w, 1f, 0f);
            var sr = t.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Gameplay";
        }
    }
}
