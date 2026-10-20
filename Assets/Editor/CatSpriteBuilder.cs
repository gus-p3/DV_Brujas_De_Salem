using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Importa los fotogramas del gato (Assets/Art/Characters/CatSprites, exportados por ArtSource/Cat/export_cat.py),
/// reescribe sus clips y completa Cat.controller con salto (subida, caída, aterrizaje) y sentarse.
/// También ajusta Witch.prefab para que el gato use los fotogramas a escala real.
/// </summary>
public static class CatSpriteBuilder
{
    private const string SpritesDir = "Assets/Art/Characters/CatSprites";
    private const string AnimDir = "Assets/Animations/Cat";
    private const string ControllerPath = AnimDir + "/Cat.controller";
    private const float PixelsPerUnit = 128f;
    private const float PivotY = 8f / 200f;

    // animación -> (clip, fps, bucle)
    private static readonly (string anim, string clip, float fps, bool loop)[] Clips =
    {
        ("idle", "Cat_Idle", 8f, true),
        ("walk", "Cat_Walk", 12f, true),
        ("sneak", "Cat_Sneak", 10f, true),
        ("rise", "Cat_Rise", 10f, false),
        ("fall", "Cat_Fall", 8f, true),
        ("land", "Cat_Land", 14f, false),
        ("sit", "Cat_Sit", 7f, false),
    };

    [MenuItem("Tools/Gato/Importar animaciones del gato")]
    public static void Build()
    {
        ConfigureImporters();
        foreach (var c in Clips) WriteClip(c.anim, c.clip, c.fps, c.loop);
        WireController();
        AdjustWitchPrefab();
        AssetDatabase.SaveAssets();
        Debug.Log("[CatSpriteBuilder] Animaciones del gato importadas.");
    }

    private static void ConfigureImporters()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesDir }))
        {
            TextureImporter imp = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
            if (imp == null) continue;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = PixelsPerUnit;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Compressed;
            TextureImporterSettings s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            s.spritePivot = new Vector2(0.5f, PivotY);
            s.spriteMeshType = SpriteMeshType.Tight;
            imp.SetTextureSettings(s);
            imp.SaveAndReimport();
        }
    }

    private static void WriteClip(string anim, string clipName, float fps, bool loop)
    {
        Sprite[] frames = Directory.GetFiles(SpritesDir, anim + "_*.png")
            .OrderBy(p => p)
            .Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p.Replace('\\', '/')))
            .Where(s => s != null)
            .ToArray();
        if (frames.Length == 0) return;

        string path = $"{AnimDir}/{clipName}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.ClearCurves();
        clip.frameRate = fps;

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        // Clave final para que el último fotograma dure lo mismo que los demás
        keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[frames.Length - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
    }

    private static void WireController()
    {
        AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (ctrl.parameters.All(p => p.name != "IsSitting")) ctrl.AddParameter("IsSitting", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState idle = Find(sm, "Idle");

        // Estados nuevos (se recrean para que la herramienta pueda ejecutarse varias veces)
        foreach (string n in new[] { "Rise", "Fall", "Land", "Sit" })
        {
            AnimatorState old = Find(sm, n);
            if (old != null) sm.RemoveState(old);
        }
        foreach (AnimatorStateTransition t in sm.anyStateTransitions.ToArray()) sm.RemoveAnyStateTransition(t);

        AnimatorState rise = sm.AddState("Rise", new Vector3(500, 0, 0));
        rise.motion = Clip("Cat_Rise");
        AnimatorState fall = sm.AddState("Fall", new Vector3(500, 80, 0));
        fall.motion = Clip("Cat_Fall");
        AnimatorState land = sm.AddState("Land", new Vector3(500, 160, 0));
        land.motion = Clip("Cat_Land");
        AnimatorState sit = sm.AddState("Sit", new Vector3(250, 240, 0));
        sit.motion = Clip("Cat_Sit");

        AnimatorStateTransition toRise = sm.AddAnyStateTransition(rise);
        Instant(toRise);
        toRise.canTransitionToSelf = false;
        toRise.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
        toRise.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VerticalVelocity");

        AnimatorStateTransition toFall = sm.AddAnyStateTransition(fall);
        Instant(toFall);
        toFall.canTransitionToSelf = false;
        toFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
        toFall.AddCondition(AnimatorConditionMode.Less, -0.5f, "VerticalVelocity");

        AnimatorStateTransition riseLand = rise.AddTransition(land);
        Instant(riseLand);
        riseLand.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
        AnimatorStateTransition fallLand = fall.AddTransition(land);
        Instant(fallLand);
        fallLand.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");

        AnimatorStateTransition landIdle = land.AddTransition(idle);
        landIdle.hasExitTime = true;
        landIdle.exitTime = 1f;
        landIdle.duration = 0f;

        AnimatorStateTransition idleSit = idle.AddTransition(sit);
        Instant(idleSit);
        idleSit.AddCondition(AnimatorConditionMode.If, 0, "IsSitting");
        AnimatorStateTransition sitIdle = sit.AddTransition(idle);
        Instant(sitIdle);
        sitIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsSitting");

        EditorUtility.SetDirty(ctrl);
    }

    private static void AdjustWitchPrefab()
    {
        const string witchPath = "Assets/Prefabs/Witch.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(witchPath);
        try
        {
            TransformationManager tm = root.GetComponent<TransformationManager>();
            // Los fotogramas nuevos ya vienen a escala real con el pivote en las patas
            tm.catVisualScale = 1f;
            tm.catVisualOffsetY = 0f;
            if (root.GetComponent<CatAnimationDriver>() == null) root.AddComponent<CatAnimationDriver>();
            PrefabUtility.SaveAsPrefabAsset(root, witchPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static AnimatorState Find(AnimatorStateMachine sm, string stateName)
    {
        return sm.states.Select(s => s.state).FirstOrDefault(s => s.name == stateName);
    }

    private static AnimationClip Clip(string clipName)
    {
        return AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimDir}/{clipName}.anim");
    }

    private static void Instant(AnimatorStateTransition t)
    {
        t.hasExitTime = false;
        t.duration = 0f;
    }
}
