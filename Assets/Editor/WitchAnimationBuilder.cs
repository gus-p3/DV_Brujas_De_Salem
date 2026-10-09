using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Crea los AnimationClips frame-by-frame de la bruja (curva de sprites sobre SpriteRenderer.sprite)
/// y el AnimatorController Witch.controller. No usa huesos ni el paquete 2D Animation.
/// </summary>
public static class WitchAnimationBuilder
{
    private const string SpritesFolder = "Assets/Art/Characters/Witch/";
    private const string AnimationsFolder = "Assets/Animations/Witch/";
    public const string ControllerPath = AnimationsFolder + "Witch.controller";

    // Cuadros por segundo de cada animación
    private const float IdleFps = 6f;
    private const float WalkFps = 10f;
    private const float JumpFps = 8f;
    private const float FlyFps = 8f;

    // Parámetros del Animator
    private const string SpeedParam = "Speed";
    private const string IsGroundedParam = "IsGrounded";
    private const string VerticalVelocityParam = "VerticalVelocity";
    private const string IsFlyingParam = "IsFlying";

    // Umbral de velocidad horizontal para pasar entre Idle y Walk
    private const float WalkSpeedThreshold = 0.1f;

    private static readonly EditorCurveBinding SpriteBinding = new EditorCurveBinding
    {
        type = typeof(UnityEngine.SpriteRenderer),
        path = string.Empty,
        propertyName = "m_Sprite"
    };

    [MenuItem("Brujas/Animaciones/Crear clips y Animator de la bruja")]
    public static void Build()
    {
        AssetDatabase.Refresh();

        AnimationClip idle = BuildClip("Witch_Idle", IdleFps, true);
        AnimationClip walk = BuildClip("Witch_Walk", WalkFps, true);
        AnimationClip jump = BuildClip("Witch_Jump", JumpFps, false);
        AnimationClip fly = BuildClip("Witch_Fly", FlyFps, true);

        BuildController(idle, walk, jump, fly);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[WitchAnimationBuilder] Clips y Witch.controller creados.");
    }

    private static AnimationClip BuildClip(string sheetName, float fps, bool loop)
    {
        List<Sprite> sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(SpritesFolder + sheetName + ".png")
            .OfType<Sprite>()
            .OrderBy(s => ExtractIndex(s.name))
            .ToList();

        if (sprites.Count == 0)
        {
            throw new System.InvalidOperationException($"No hay sprites cortados en {sheetName}.png. Ejecuta primero el SpriteSheetSlicer.");
        }

        AnimationClip clip = new AnimationClip { frameRate = fps, name = sheetName };

        // Un fotograma clave por cuadro, más uno final que fija la duración total del clip
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Count + 1];
        for (int i = 0; i < sprites.Count; i++)
        {
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };
        }
        keys[sprites.Count] = new ObjectReferenceKeyframe
        {
            time = sprites.Count / fps,
            // En bucle vuelve al primer cuadro; sin bucle mantiene el último
            value = loop ? sprites[0] : sprites[sprites.Count - 1]
        };
        AnimationUtility.SetObjectReferenceCurve(clip, SpriteBinding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string path = AnimationsFolder + sheetName + ".anim";
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null)
        {
            existing.frameRate = fps;
            AnimationUtility.SetObjectReferenceCurve(existing, SpriteBinding, keys);
            AnimationUtility.SetAnimationClipSettings(existing, settings);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static int ExtractIndex(string spriteName)
    {
        int underscore = spriteName.LastIndexOf('_');
        return underscore >= 0 && int.TryParse(spriteName.Substring(underscore + 1), out int index) ? index : 0;
    }

    private static void BuildController(AnimationClip idle, AnimationClip walk, AnimationClip jump, AnimationClip fly)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter(SpeedParam, AnimatorControllerParameterType.Float);
        controller.AddParameter(IsGroundedParam, AnimatorControllerParameterType.Bool);
        controller.AddParameter(VerticalVelocityParam, AnimatorControllerParameterType.Float);
        controller.AddParameter(IsFlyingParam, AnimatorControllerParameterType.Bool);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idleState = machine.AddState("Idle");
        AnimatorState walkState = machine.AddState("Walk");
        AnimatorState jumpState = machine.AddState("Jump");
        AnimatorState flyState = machine.AddState("Fly");
        idleState.motion = idle;
        walkState.motion = walk;
        jumpState.motion = jump;
        flyState.motion = fly;
        machine.defaultState = idleState;

        // Idle <-> Walk según la velocidad horizontal
        AnimatorStateTransition idleToWalk = QuickTransition(idleState, walkState);
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, WalkSpeedThreshold, SpeedParam);
        idleToWalk.AddCondition(AnimatorConditionMode.If, 0f, IsGroundedParam);

        AnimatorStateTransition walkToIdle = QuickTransition(walkState, idleState);
        walkToIdle.AddCondition(AnimatorConditionMode.Less, WalkSpeedThreshold, SpeedParam);

        // Cualquier estado -> Jump si no toca el suelo (y no está volando)
        AnimatorStateTransition anyToJump = machine.AddAnyStateTransition(jumpState);
        ConfigureFast(anyToJump);
        anyToJump.canTransitionToSelf = false;
        anyToJump.AddCondition(AnimatorConditionMode.IfNot, 0f, IsGroundedParam);
        anyToJump.AddCondition(AnimatorConditionMode.IfNot, 0f, IsFlyingParam);

        // Jump -> Idle / Walk al tocar el suelo
        AnimatorStateTransition jumpToIdle = QuickTransition(jumpState, idleState);
        jumpToIdle.AddCondition(AnimatorConditionMode.If, 0f, IsGroundedParam);
        jumpToIdle.AddCondition(AnimatorConditionMode.Less, WalkSpeedThreshold, SpeedParam);

        AnimatorStateTransition jumpToWalk = QuickTransition(jumpState, walkState);
        jumpToWalk.AddCondition(AnimatorConditionMode.If, 0f, IsGroundedParam);
        jumpToWalk.AddCondition(AnimatorConditionMode.Greater, WalkSpeedThreshold, SpeedParam);

        // Cualquier estado <-> Fly según IsFlying
        AnimatorStateTransition anyToFly = machine.AddAnyStateTransition(flyState);
        ConfigureFast(anyToFly);
        anyToFly.canTransitionToSelf = false;
        anyToFly.AddCondition(AnimatorConditionMode.If, 0f, IsFlyingParam);

        // Al dejar de volar en el suelo vuelve a Idle; si deja de volar en el aire, "Cualquier estado -> Jump" toma el control
        AnimatorStateTransition flyToIdle = QuickTransition(flyState, idleState);
        flyToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, IsFlyingParam);
        flyToIdle.AddCondition(AnimatorConditionMode.If, 0f, IsGroundedParam);
    }

    /// <summary>Transición de respuesta rápida: sin Has Exit Time y sin mezcla.</summary>
    private static AnimatorStateTransition QuickTransition(AnimatorState from, AnimatorState to)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        ConfigureFast(transition);
        return transition;
    }

    private static void ConfigureFast(AnimatorStateTransition transition)
    {
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = 0f;
        transition.exitTime = 0f;
        transition.offset = 0f;
    }
}
