using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Genera el prefab de la bruja y la escena de prueba Test_Bruja (suelo, 3 plataformas, bruja,
/// guardia estático con EnemySight y cámara con CameraFollow). Es idempotente.
/// </summary>
public static class TestSceneBuilder
{
    private const string PlaceholderSquarePath = "Assets/Art/Environment/Placeholder_Square.png";
    private const string WitchPrefabPath = "Assets/Prefabs/Witch.prefab";
    private const string NoFrictionPath = "Assets/Prefabs/Witch_NoFriction.physicsMaterial2D";
    private const string ScenePath = "Assets/Scenes/Test_Bruja.unity";
    private const string IdleSheetPath = "Assets/Art/Characters/Witch/Witch_Idle.png";

    // Tamaño del cuadrado de relleno (px). Con PPU 64 equivale a 1 unidad de mundo.
    private const int SquareSizePixels = 64;

    // Bruja
    private const float WitchVisualScale = 0.45f;          // 256 px a PPU 64 = 4 u; con 0.45 la bruja mide ~1,7 u
    private static readonly Vector2 WitchColliderSize = new Vector2(0.6f, 1.6f);
    private static readonly Vector2 WitchColliderOffset = new Vector2(0f, 0.8f);
    private const float WitchGravityScale = 3f;
    private const float WitchDetectionHeight = 0.9f;
    private static readonly Vector3 WitchSpawn = new Vector3(-12f, 0.05f, 0f);

    // Nivel
    private static readonly Color GroundColor = new Color(0.50f, 0.33f, 0.18f);
    private static readonly Color PlatformColor = new Color(0.72f, 0.50f, 0.25f);
    private static readonly Color BackgroundColor = new Color(0.96f, 0.88f, 0.70f);
    private static readonly Color GuardColor = new Color(0.35f, 0.30f, 0.50f);
    private static readonly Color LanternColor = new Color(1f, 0.85f, 0.3f);
    private const float GroundWidth = 40f;
    private const float GroundThickness = 1f;
    private const float PlatformThickness = 0.4f;
    private const float PlatformWidth = 4f;
    private const float CameraSize = 5.5f;
    private static readonly Vector3 GuardPosition = new Vector3(12f, 0.8f, 0f);

    [MenuItem("Brujas/Escena/Crear prefab de la bruja y Test_Bruja")]
    public static void Build()
    {
        ProjectSetup.Configure();
        Sprite square = EnsurePlaceholderSquare();
        PhysicsMaterial2D noFriction = EnsureNoFrictionMaterial();
        GameObject witchPrefab = BuildWitchPrefab(noFriction);
        BuildScene(square, witchPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TestSceneBuilder] Prefab Witch y escena Test_Bruja creados.");
    }

    // ----------------------------------------------------------------- Recursos

    private static Sprite EnsurePlaceholderSquare()
    {
        if (!File.Exists(PlaceholderSquarePath))
        {
            Texture2D texture = new Texture2D(SquareSizePixels, SquareSizePixels, TextureFormat.RGBA32, false);
            Color32[] pixels = Enumerable.Repeat(new Color32(255, 255, 255, 255), SquareSizePixels * SquareSizePixels).ToArray();
            texture.SetPixels32(pixels);
            File.WriteAllBytes(PlaceholderSquarePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(PlaceholderSquarePath, ImportAssetOptions.ForceUpdate);
        }

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(PlaceholderSquarePath);
        SpriteImportDefaults.Apply(importer);
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSquarePath);
    }

    private static PhysicsMaterial2D EnsureNoFrictionMaterial()
    {
        PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);
        if (material == null)
        {
            material = new PhysicsMaterial2D("Witch_NoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(material, NoFrictionPath);
        }
        return material;
    }

    // ------------------------------------------------------------------- Prefab

    private static GameObject BuildWitchPrefab(PhysicsMaterial2D noFriction)
    {
        GameObject root = new GameObject("Witch");
        root.tag = ProjectSetup.PlayerTag;
        root.layer = LayerMask.NameToLayer(ProjectSetup.PlayerLayer);

        Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = WitchGravityScale;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        BoxCollider2D box = root.AddComponent<BoxCollider2D>();
        box.size = WitchColliderSize;
        box.offset = WitchColliderOffset;
        box.sharedMaterial = noFriction;

        // Visual: SpriteRenderer + Animator en un hijo escalado (el collider queda independiente del arte)
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = Vector3.one * WitchVisualScale;
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAllAssetRepresentationsAtPath(IdleSheetPath).OfType<Sprite>()
            .OrderBy(s => s.name).FirstOrDefault();
        Animator animator = visual.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(WitchAnimationBuilder.ControllerPath);

        GameObject detection = new GameObject("DetectionPoint");
        detection.transform.SetParent(root.transform, false);
        detection.transform.localPosition = new Vector3(0f, WitchDetectionHeight, 0f);

        PlayerController controller = root.AddComponent<PlayerController>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("groundMask").intValue = 1 << LayerMask.NameToLayer(ProjectSetup.GroundLayer);
        so.FindProperty("noFrictionMaterial").objectReferenceValue = noFriction;
        so.FindProperty("animator").objectReferenceValue = animator;
        so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
        so.FindProperty("detectionPoint").objectReferenceValue = detection.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, WitchPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ------------------------------------------------------------------- Escena

    private static void BuildScene(Sprite square, GameObject witchPrefab)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Luz global (en URP 2D los sprites Lit necesitan al menos una luz)
        GameObject lightObject = new GameObject("Global Light 2D");
        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.color = new Color(1f, 0.96f, 0.88f);
        light.intensity = 1f;

        // Suelo
        CreateBlock("Ground", square, GroundColor, new Vector2(0f, -GroundThickness * 0.5f), new Vector2(GroundWidth, GroundThickness));

        // Tres plataformas a distintas alturas (superficies a 2.2, 4.4 y 6.6 u)
        CreateBlock("Platform_Low", square, PlatformColor, new Vector2(-6f, 2.2f - PlatformThickness * 0.5f), new Vector2(PlatformWidth, PlatformThickness));
        CreateBlock("Platform_Mid", square, PlatformColor, new Vector2(0f, 4.4f - PlatformThickness * 0.5f), new Vector2(PlatformWidth, PlatformThickness));
        CreateBlock("Platform_High", square, PlatformColor, new Vector2(6f, 6.6f - PlatformThickness * 0.5f), new Vector2(PlatformWidth, PlatformThickness));

        // Bruja (con CanFly activo solo en esta escena de pruebas)
        GameObject witch = (GameObject)PrefabUtility.InstantiatePrefab(witchPrefab);
        witch.transform.position = WitchSpawn;
        PlayerController controller = witch.GetComponent<PlayerController>();
        SerializedObject witchSo = new SerializedObject(controller);
        witchSo.FindProperty("canFly").boolValue = true;
        witchSo.ApplyModifiedPropertiesWithoutUndo();

        BuildGuard(square);
        BuildCamera(witch.transform);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    private static GameObject CreateBlock(string name, Sprite square, Color color, Vector2 position, Vector2 size)
    {
        GameObject block = new GameObject(name);
        block.layer = LayerMask.NameToLayer(ProjectSetup.GroundLayer);
        block.transform.position = position;
        block.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
        renderer.sprite = square;
        renderer.color = color;
        block.AddComponent<BoxCollider2D>();
        return block;
    }

    private static void BuildGuard(Sprite square)
    {
        // Raíz con escala X negativa: el guardia mira a la izquierda (hacia la bruja)
        GameObject guard = new GameObject("Guard_Test");
        guard.layer = LayerMask.NameToLayer(ProjectSetup.EnemyLayer);
        guard.transform.position = GuardPosition;
        guard.transform.localScale = new Vector3(-1f, 1f, 1f);

        GameObject body = new GameObject("Body");
        body.layer = guard.layer;
        body.transform.SetParent(guard.transform, false);
        body.transform.localScale = new Vector3(0.9f, 1.6f, 1f);
        SpriteRenderer bodyRenderer = body.AddComponent<SpriteRenderer>();
        bodyRenderer.sprite = square;
        bodyRenderer.color = GuardColor;

        GameObject lantern = new GameObject("Lantern");
        lantern.layer = guard.layer;
        lantern.transform.SetParent(guard.transform, false);
        lantern.transform.localPosition = new Vector3(0.55f, 0.1f, 0f);
        lantern.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
        SpriteRenderer lanternRenderer = lantern.AddComponent<SpriteRenderer>();
        lanternRenderer.sprite = square;
        lanternRenderer.color = LanternColor;
        lanternRenderer.sortingOrder = 1;

        EnemySight sight = guard.AddComponent<EnemySight>();
        SerializedObject so = new SerializedObject(sight);
        so.FindProperty("obstacleMask").intValue = 1 << LayerMask.NameToLayer(ProjectSetup.GroundLayer);
        so.FindProperty("targetMask").intValue = 1 << LayerMask.NameToLayer(ProjectSetup.PlayerLayer);
        so.FindProperty("lanternOrigin").objectReferenceValue = lantern.transform;
        so.FindProperty("facingReference").objectReferenceValue = guard.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildCamera(Transform target)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = CameraSize;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = BackgroundColor;
        camera.transform.position = new Vector3(WitchSpawn.x, 2.5f, -10f);
        cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraObject.AddComponent<AudioListener>();

        CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
        SerializedObject so = new SerializedObject(follow);
        so.FindProperty("target").objectReferenceValue = target;
        so.FindProperty("useLimits").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
