using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SalemPolishFixer
{
    [MenuItem("Tools/Salem/Aplicar 4 Correcciones")]
    public static void ApplyAllFixes()
    {
        Debug.Log("[SalemPolishFixer] Iniciando aplicacion de correciones...");

        // 1. Asignar musica en AudioManager.prefab
        FixAudioManagerPrefab();

        // 2. Ajustar Guard.prefab (LanternOrigin a la mano)
        FixGuardPrefab();

        // 3. Procesar Escenas
        FixSceneLevel2();
        FixSceneLevel1();
        FixSceneLevel3();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SalemPolishFixer] ¡Todas las correcciones aplicadas exitosamente!");
    }

    private static void FixAudioManagerPrefab()
    {
        string path = "Assets/Prefabs/AudioManager.prefab";
        GameObject go = PrefabUtility.LoadPrefabContents(path);
        AudioManager am = go.GetComponent<AudioManager>();
        if (am != null)
        {
            am.menuMusicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/BGM_Menu.wav");
            am.levelMusicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/BGM_Level1_Forest.wav");
            am.level2MusicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/BGM_Level2_Village.wav");
            am.level3MusicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/BGM_Level3_Church.wav");
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Debug.Log("[SalemPolishFixer] AudioManager.prefab actualizado con los 4 temas musicales.");
        }
        PrefabUtility.UnloadPrefabContents(go);
    }

    private static void FixGuardPrefab()
    {
        string path = "Assets/Prefabs/Guard.prefab";
        GameObject go = PrefabUtility.LoadPrefabContents(path);
        Transform lantern = go.transform.Find("LanternOrigin");
        if (lantern == null)
        {
            GameObject lgo = new GameObject("LanternOrigin");
            lgo.transform.SetParent(go.transform, false);
            lantern = lgo.transform;
        }
        lantern.localPosition = new Vector3(0.45f, 1.05f, 0f);

        EnemySight sight = go.GetComponent<EnemySight>();
        if (sight != null)
        {
            SerializedObject so = new SerializedObject(sight);
            var prop = so.FindProperty("lanternOrigin");
            if (prop != null) prop.objectReferenceValue = lantern;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(go, path);
        PrefabUtility.UnloadPrefabContents(go);
        Debug.Log("[SalemPolishFixer] Guard.prefab farol fijado a altura de manos (1.05u).");
    }

    private static void FixSceneLevel2()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level2.unity", OpenSceneMode.Single);
        if (!scene.IsValid()) return;

        // Maquetar CatSlider en HUD_Canvas
        GameObject canvasGo = GameObject.Find("HUD_Canvas");
        if (canvasGo != null)
        {
            Transform cat = canvasGo.transform.Find("CatSlider");
            Transform flight = canvasGo.transform.Find("FlightSlider");
            HUDController hud = canvasGo.GetComponent<HUDController>();

            if (cat == null && flight != null)
            {
                GameObject copy = Object.Instantiate(flight.gameObject, canvasGo.transform);
                copy.name = "CatSlider";
                cat = copy.transform;
                RectTransform rt = copy.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(40f, -120f);
                    rt.sizeDelta = new Vector2(240f, 18f);
                }
            }

            if (cat != null && hud != null)
            {
                hud.catEnergySlider = cat.GetComponent<Slider>();
                cat.gameObject.SetActive(false);
            }
        }

        // Actualizar faroles de guardias en Nivel 2
        FixGuardsInActiveScene();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SalemPolishFixer] Level2.unity actualizado: CatSlider maquetado y faroles fijados a las manos.");
    }

    private static void FixSceneLevel1()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity", OpenSceneMode.Single);
        if (!scene.IsValid()) return;

        FixGuardsInActiveScene();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SalemPolishFixer] Level1.unity guardias corregidos para aluzar desde las manos.");
    }

    private static void FixSceneLevel3()
    {
        string scenePath = File.Exists("Assets/Scenes/Level3_Village.unity")
            ? "Assets/Scenes/Level3_Village.unity"
            : "Assets/Scenes/Level3.unity";

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid()) return;

        FixGuardsInActiveScene();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[SalemPolishFixer] {scenePath} guardias corregidos para aluzar desde las manos.");
    }

    private static void FixGuardsInActiveScene()
    {
        EnemySight[] sights = Object.FindObjectsByType<EnemySight>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (EnemySight es in sights)
        {
            Transform t = es.transform.Find("LanternOrigin");
            if (t == null)
            {
                GameObject lgo = new GameObject("LanternOrigin");
                lgo.transform.SetParent(es.transform, false);
                t = lgo.transform;
            }
            t.localPosition = new Vector3(0.45f, 1.05f, 0f);

            SerializedObject so = new SerializedObject(es);
            var prop = so.FindProperty("lanternOrigin");
            if (prop != null) prop.objectReferenceValue = t;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
