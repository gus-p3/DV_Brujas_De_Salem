using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Crea la escena de prueba de personajes animados: una instancia del rig por estado,
/// con su etiqueta, suelo y panel de rendimiento.
/// </summary>
public static class CharacterTestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Test_Personajes.unity";
    private const string WitchRigPrefab = "Assets/Prefabs/WitchRig.prefab";
    private const string GroundSprite = "Assets/Art/Environment/Env_Ground_Tile.png";
    private const float Spacing = 1.3f;

    private static readonly (RigDemoDriver.Mode mode, string label)[] Stations =
    {
        (RigDemoDriver.Mode.Idle, "Idle"),
        (RigDemoDriver.Mode.Walk, "Caminar"),
        (RigDemoDriver.Mode.Run, "Correr"),
        (RigDemoDriver.Mode.Jump, "Saltar"),
        (RigDemoDriver.Mode.Cast, "Hechizo"),
        (RigDemoDriver.Mode.Hurt, "Daño"),
        (RigDemoDriver.Mode.Die, "Morir"),
        (RigDemoDriver.Mode.Fly, "Volar"),
    };

    [MenuItem("Tools/Rig/Crear escena de prueba de personajes")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        float width = Spacing * (Stations.Length - 1);

        // Cámara
        GameObject camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 2.9f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.20f, 0.19f, 0.27f);
        camGo.transform.position = new Vector3(width * 0.5f, 1.45f, -10f);

        // Suelo
        GameObject ground = new GameObject("Ground");
        SpriteRenderer gsr = ground.AddComponent<SpriteRenderer>();
        gsr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GroundSprite);
        gsr.drawMode = SpriteDrawMode.Tiled;
        gsr.size = new Vector2(width + 8f, 1.2f);
        gsr.sortingLayerName = "Midground";
        ground.transform.position = new Vector3(width * 0.5f, -0.6f, 0f);
        if (gsr.sprite != null)
        {
            // Alinear la parte superior del tile con y = 0
            ground.transform.position = new Vector3(width * 0.5f, -gsr.bounds.extents.y + 0.05f, 0f);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WitchRigPrefab);
        for (int i = 0; i < Stations.Length; i++)
        {
            float x = i * Spacing;
            GameObject station = new GameObject($"Station_{Stations[i].label}");
            station.transform.position = new Vector3(x, 0f, 0f);

            GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab, station.transform);
            rig.name = $"WitchRig_{Stations[i].label}";
            RigDemoDriver driver = rig.AddComponent<RigDemoDriver>();
            SerializedObject so = new SerializedObject(driver);
            so.FindProperty("mode").enumValueIndex = (int)Stations[i].mode;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject label = new GameObject("Label");
            label.transform.SetParent(station.transform, false);
            label.transform.localPosition = new Vector3(0f, 2.25f, 0f);
            TextMeshPro tmp = label.AddComponent<TextMeshPro>();
            tmp.text = Stations[i].label;
            tmp.fontSize = 2.6f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.96f, 0.86f, 0.6f);
            tmp.rectTransform.sizeDelta = new Vector2(1.6f, 0.6f);
            tmp.sortingLayerID = SortingLayer.NameToID("Foreground");
        }

        new GameObject("PerfOverlay").AddComponent<PerfOverlay>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[CharacterTestSceneBuilder] Escena creada: {ScenePath}");
    }
}
