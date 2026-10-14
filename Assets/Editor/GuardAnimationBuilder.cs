using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Procesa y corta los spritesheets del guardia (celdas de 256x256, pivote Bottom Center),
/// genera los AnimationClips (Idle 6 fps, Walk 10 fps, Alert 8 fps) y construye
/// Guard.controller con parámetros Speed (float) e IsAlert (bool).
/// </summary>
public static class GuardAnimationBuilder
{
    private const string SpritesFolder = "Assets/Art/Characters/Guard/";
    private const string AnimationsFolder = "Assets/Animations/Guard/";
    public const string ControllerPath = AnimationsFolder + "Guard.controller";

    // Frecuencias de cuadro según GDD
    private const float IdleFps = 6f;
    private const float WalkFps = 10f;
    private const float AlertFps = 8f;

    // Parámetros del Animator
    private const string SpeedParam = "Speed";
    private const string IsAlertParam = "IsAlert";

    private const float WalkSpeedThreshold = 0.1f;

    private static readonly EditorCurveBinding SpriteBinding = new EditorCurveBinding
    {
        type = typeof(SpriteRenderer),
        path = string.Empty,
        propertyName = "m_Sprite"
    };

    [MenuItem("Brujas/2. Sprites/Cortar guardia")]
    [MenuItem("Brujas/Sprites/Cortar guardia")]
    public static void BuildGuardAssets()
    {
        if (!Directory.Exists(AnimationsFolder))
        {
            Directory.CreateDirectory(AnimationsFolder);
        }

        // 1. Cortar los 3 spritesheets con SpriteSheetSlicer
        string[] sheets = { "Guard_Idle.png", "Guard_Walk.png", "Guard_Alert.png" };
        foreach (string sheet in sheets)
        {
            string path = SpritesFolder + sheet;
            if (File.Exists(path))
            {
                SpriteSheetSlicer.SliceSheet(path, SpriteSheetSlicer.CellSize, SpriteSheetSlicer.CellSize);
            }
            else
            {
                Debug.LogError($"[GuardAnimationBuilder] No existe el archivo {path}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 2. Construir AnimationClips
        AnimationClip idle = BuildClip("Guard_Idle", IdleFps, true);
        AnimationClip walk = BuildClip("Guard_Walk", WalkFps, true);
        AnimationClip alert = BuildClip("Guard_Alert", AlertFps, true);

        // 3. Construir Guard.controller
        BuildController(idle, walk, alert);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GuardAnimationBuilder] Sprites cortados, clips y Guard.controller creados exitosamente.");
    }

    private static AnimationClip BuildClip(string sheetName, float fps, bool loop)
    {
        string sheetPath = SpritesFolder + sheetName + ".png";
        List<Sprite> sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(sheetPath)
            .OfType<Sprite>()
            .OrderBy(s => ExtractIndex(s.name))
            .ToList();

        if (sprites.Count == 0)
        {
            throw new System.InvalidOperationException($"No hay sprites cortados en {sheetPath}.");
        }

        AnimationClip clip = new AnimationClip { frameRate = fps, name = sheetName };

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Count + 1];
        for (int i = 0; i < sprites.Count; i++)
        {
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };
        }
        keys[sprites.Count] = new ObjectReferenceKeyframe
        {
            time = sprites.Count / fps,
            value = loop ? sprites[0] : sprites[sprites.Count - 1]
        };
        AnimationUtility.SetObjectReferenceCurve(clip, SpriteBinding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string clipPath = AnimationsFolder + sheetName + ".anim";
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (existing != null)
        {
            existing.frameRate = fps;
            AnimationUtility.SetObjectReferenceCurve(existing, SpriteBinding, keys);
            AnimationUtility.SetAnimationClipSettings(existing, settings);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static int ExtractIndex(string spriteName)
    {
        int underscore = spriteName.LastIndexOf('_');
        return underscore >= 0 && int.TryParse(spriteName.Substring(underscore + 1), out int index) ? index : 0;
    }

    private static void BuildController(AnimationClip idle, AnimationClip walk, AnimationClip alert)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter(SpeedParam, AnimatorControllerParameterType.Float);
        controller.AddParameter(IsAlertParam, AnimatorControllerParameterType.Bool);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idleState = machine.AddState("Idle");
        AnimatorState walkState = machine.AddState("Walk");
        AnimatorState alertState = machine.AddState("Alert");

        idleState.motion = idle;
        walkState.motion = walk;
        alertState.motion = alert;
        machine.defaultState = idleState;

        // Transición Idle -> Walk (Speed > 0.1f)
        AnimatorStateTransition idleToWalk = QuickTransition(idleState, walkState);
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, WalkSpeedThreshold, SpeedParam);

        // Transición Walk -> Idle (Speed < 0.1f)
        AnimatorStateTransition walkToIdle = QuickTransition(walkState, idleState);
        walkToIdle.AddCondition(AnimatorConditionMode.Less, WalkSpeedThreshold, SpeedParam);

        // Transición AnyState -> Alert (IsAlert == true)
        AnimatorStateTransition anyToAlert = machine.AddAnyStateTransition(alertState);
        ConfigureFast(anyToAlert);
        anyToAlert.canTransitionToSelf = false;
        anyToAlert.AddCondition(AnimatorConditionMode.If, 0f, IsAlertParam);

        // Retorno Alert -> Idle (IsAlert == false && Speed < 0.1f)
        AnimatorStateTransition alertToIdle = QuickTransition(alertState, idleState);
        alertToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, IsAlertParam);
        alertToIdle.AddCondition(AnimatorConditionMode.Less, WalkSpeedThreshold, SpeedParam);

        // Retorno Alert -> Walk (IsAlert == false && Speed > 0.1f)
        AnimatorStateTransition alertToWalk = QuickTransition(alertState, walkState);
        alertToWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, IsAlertParam);
        alertToWalk.AddCondition(AnimatorConditionMode.Greater, WalkSpeedThreshold, SpeedParam);
    }

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
