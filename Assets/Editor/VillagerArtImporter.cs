using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

public static class VillagerArtImporter
{
    [MenuItem("Brujas/2. Sprites/Cortar aldeanos")]
    public static void SliceAndSetupVillagers()
    {
        string baseDir = "Assets/Art/Characters/Villager";
        if (!AssetDatabase.IsValidFolder(baseDir))
        {
            AssetDatabase.CreateFolder("Assets/Art/Characters", "Villager");
        }

        string[] sheets = new string[]
        {
            "Villager_Idle.png",
            "Villager_Walk.png",
            "Villager_Scream.png",
            "Villager_Run.png",
            "VillagerF_Idle.png",
            "VillagerF_Walk.png",
            "VillagerF_Scream.png",
            "VillagerF_Run.png"
        };

        foreach (string file in sheets)
        {
            string path = $"{baseDir}/{file}";
            SliceSheet(path, 256, 256, 4);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Crear animaciones y AnimatorController
        AnimatorController controller = CreateVillagerController(baseDir);

        // Crear Prefabs de Aldeanos
        CreateVillagerPrefab("Villager", controller, $"{baseDir}/Villager_Idle.png");
        CreateVillagerPrefab("VillagerF", controller, $"{baseDir}/VillagerF_Idle.png");

        AssetDatabase.SaveAssets();
        Debug.Log("[VillagerArtImporter] ¡Sprites de aldeanos cortados, controlador y prefabs creados!");
    }

    private static void SliceSheet(string assetPath, int frameWidth, int frameHeight, int frameCount)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 64;
        importer.filterMode = FilterMode.Bilinear;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;

        string baseName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        SpriteMetaData[] metaData = new SpriteMetaData[frameCount];

        for (int i = 0; i < frameCount; i++)
        {
            metaData[i] = new SpriteMetaData
            {
                name = $"{baseName}_{i}",
                rect = new Rect(i * frameWidth, 0, frameWidth, frameHeight),
                alignment = (int)SpriteAlignment.BottomCenter,
                pivot = new Vector2(0.5f, 0f)
            };
        }

        importer.spritesheet = metaData;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    private static AnimatorController CreateVillagerController(string baseDir)
    {
        string controllerPath = $"{baseDir}/Villager.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        // Parámetros
        EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "IsScreaming", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "IsFrenzy", AnimatorControllerParameterType.Bool);

        // Clips de Animación
        AnimationClip idleClip = CreateOrGetClip(baseDir, "Villager_Idle", 6f);
        AnimationClip walkClip = CreateOrGetClip(baseDir, "Villager_Walk", 8f);
        AnimationClip screamClip = CreateOrGetClip(baseDir, "Villager_Scream", 10f);
        AnimationClip runClip = CreateOrGetClip(baseDir, "Villager_Run", 12f);

        // Configurar Grafo de Estados
        var rootStateMachine = controller.layers[0].stateMachine;

        // Limpiar estados previos si existían
        var states = rootStateMachine.states;
        AnimatorState idleState = null;
        AnimatorState walkState = null;
        AnimatorState screamState = null;
        AnimatorState runState = null;

        foreach (var s in states)
        {
            if (s.state.name == "Idle") idleState = s.state;
            else if (s.state.name == "Walk") walkState = s.state;
            else if (s.state.name == "Scream") screamState = s.state;
            else if (s.state.name == "Run") runState = s.state;
        }

        if (idleState == null) idleState = rootStateMachine.AddState("Idle");
        if (walkState == null) walkState = rootStateMachine.AddState("Walk");
        if (screamState == null) screamState = rootStateMachine.AddState("Scream");
        if (runState == null) runState = rootStateMachine.AddState("Run");

        idleState.motion = idleClip;
        walkState.motion = walkClip;
        screamState.motion = screamClip;
        runState.motion = runClip;

        rootStateMachine.defaultState = idleState;

        // Limpiar transiciones existentes para recrearlas limpiamente
        idleState.transitions = new AnimatorStateTransition[0];
        walkState.transitions = new AnimatorStateTransition[0];
        screamState.transitions = new AnimatorStateTransition[0];
        runState.transitions = new AnimatorStateTransition[0];

        // Transiciones: Idle <-> Walk
        var toWalk = idleState.AddTransition(walkState);
        toWalk.hasExitTime = false;
        toWalk.duration = 0.1f;
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var toIdle = walkState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.1f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        // Transiciones a Scream (Alerta)
        var idleToScream = idleState.AddTransition(screamState);
        idleToScream.hasExitTime = false;
        idleToScream.duration = 0.05f;
        idleToScream.AddCondition(AnimatorConditionMode.If, 0, "IsScreaming");

        var walkToScream = walkState.AddTransition(screamState);
        walkToScream.hasExitTime = false;
        walkToScream.duration = 0.05f;
        walkToScream.AddCondition(AnimatorConditionMode.If, 0, "IsScreaming");

        var screamToIdle = screamState.AddTransition(idleState);
        screamToIdle.hasExitTime = false;
        screamToIdle.duration = 0.1f;
        screamToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsScreaming");

        // Transiciones a Run (Frenesí)
        var idleToRun = idleState.AddTransition(runState);
        idleToRun.hasExitTime = false;
        idleToRun.duration = 0.05f;
        idleToRun.AddCondition(AnimatorConditionMode.If, 0, "IsFrenzy");

        var walkToRun = walkState.AddTransition(runState);
        walkToRun.hasExitTime = false;
        walkToRun.duration = 0.05f;
        walkToRun.AddCondition(AnimatorConditionMode.If, 0, "IsFrenzy");

        var screamToRun = screamState.AddTransition(runState);
        screamToRun.hasExitTime = false;
        screamToRun.duration = 0.05f;
        screamToRun.AddCondition(AnimatorConditionMode.If, 0, "IsFrenzy");

        var runToIdle = runState.AddTransition(idleState);
        runToIdle.hasExitTime = false;
        runToIdle.duration = 0.1f;
        runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsFrenzy");

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in controller.parameters)
        {
            if (p.name == name) return;
        }
        controller.AddParameter(name, type);
    }

    private static AnimationClip CreateOrGetClip(string baseDir, string name, float frameRate)
    {
        string clipPath = $"{baseDir}/{name}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = name };
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        string sheetPath = $"{baseDir}/{name}.png";
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
        List<Sprite> sprites = new List<Sprite>();
        foreach (Object obj in subAssets)
        {
            if (obj is Sprite s) sprites.Add(s);
        }
        sprites.Sort((a, b) => string.Compare(a.name, b.name));

        if (sprites.Count > 0)
        {
            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "Visual",
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
            float interval = 1f / frameRate;
            for (int i = 0; i < sprites.Count; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i * interval,
                    value = sprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void CreateVillagerPrefab(string name, AnimatorController controller, string spritePath)
    {
        string prefabPath = $"Assets/Prefabs/{name}.prefab";
        GameObject root = new GameObject(name);

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0) root.layer = enemyLayer;

        // Físicas
        Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = root.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 2.1f);
        col.offset = new Vector2(0f, 1.05f);

        // Visual child
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        foreach (Object o in assets)
        {
            if (o is Sprite s)
            {
                sr.sprite = s;
                break;
            }
        }
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 4;

        // Animator
        Animator anim = visual.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        // Guardar Prefab
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
    }
}
