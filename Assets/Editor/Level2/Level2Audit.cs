using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Auditoría de SOLO LECTURA de Level1.unity para preparar el Nivel 2.
/// Abre Level1 de forma aditiva en memoria, vuelca jerarquía raíz, componentes y
/// overrides del prefab Witch a Temp/Level2_Audit_L1.txt y cierra la escena SIN guardar.
/// Nunca llama a SaveScene sobre Level1.
/// </summary>
public static class Level2Audit
{
    public const string Level1Path = "Assets/Scenes/Level1.unity";
    private const string OutPath = "Logs/Level2_Audit_L1.txt";

    [MenuItem("Brujas/Nivel 2/Auditar Level1 (solo lectura)")]
    public static void AuditLevel1()
    {
        var sb = new StringBuilder();
        Scene host = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Scene l1 = EditorSceneManager.OpenScene(Level1Path, OpenSceneMode.Additive);
        try
        {
            sb.AppendLine("== RAICES DE Level1 ==");
            foreach (GameObject root in l1.GetRootGameObjects())
            {
                sb.AppendLine($"[ROOT] {root.name} children={root.transform.childCount} prefab={PrefabUtility.IsAnyPrefabInstanceRoot(root)}");
                Dump(root.transform, sb, 1, root.name == "_Managers" || root.name == "Main Camera" || root.name.Contains("Witch") ? 3 : 1);
            }

            sb.AppendLine("== OBJETOS CON MANAGERS / SINGLETONS ==");
            System.Type[] types = { typeof(GameManager), typeof(AudioManager), typeof(TutorialManager), typeof(CheckpointManager),
                typeof(FrenzyController), typeof(HUDController), typeof(CaptureListener), typeof(UnityEngine.EventSystems.EventSystem),
                typeof(CameraFollow), typeof(KillZone), typeof(LevelGoal), typeof(PotionInventory), typeof(PlayerInvulnerability),
                typeof(HealthManager), typeof(BroomInventory), typeof(TransformationManager), typeof(PlayerController) };
            foreach (var t in types)
            {
                foreach (Component c in l1.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren(t, true)))
                    sb.AppendLine($"{t.Name} -> {Path(c.transform)}");
            }

            sb.AppendLine("== OVERRIDES DE INSTANCIAS DEL PREFAB Witch ==");
            foreach (PlayerController pc in l1.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlayerController>(true)))
            {
                GameObject w = PrefabUtility.GetOutermostPrefabInstanceRoot(pc.gameObject);
                if (w == null) { sb.AppendLine("Witch no es instancia de prefab"); continue; }
                sb.AppendLine($"Instancia: {w.name} fuente={AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(w))}");
                foreach (var ac in PrefabUtility.GetAddedComponents(w)) sb.AppendLine($"  + AddedComponent {ac.instanceComponent.GetType().Name} en {ac.instanceComponent.name}");
                foreach (var rc in PrefabUtility.GetRemovedComponents(w)) sb.AppendLine($"  - RemovedComponent {rc.assetComponent.GetType().Name}");
                foreach (var ag in PrefabUtility.GetAddedGameObjects(w)) sb.AppendLine($"  + AddedGameObject {ag.instanceGameObject.name}");
                foreach (var m in PrefabUtility.GetPropertyModifications(w))
                    if (m.target != null) sb.AppendLine($"  ~ {m.target.GetType().Name}({m.target.name}).{m.propertyPath} = {m.value} {(m.objectReference != null ? m.objectReference.name : "")}");
            }
        }
        finally
        {
            // Cerrar SIN guardar: Level1 en disco no se toca.
            EditorSceneManager.CloseScene(l1, true);
        }
        File.WriteAllText(OutPath, sb.ToString());
        Debug.Log("[Level2Audit] Escrito " + OutPath);
    }

    private static void Dump(Transform t, StringBuilder sb, int depth, int maxDepth)
    {
        if (depth > maxDepth) return;
        foreach (Transform ch in t)
        {
            string comps = string.Join(",", ch.GetComponents<Component>().Where(c => c != null && !(c is Transform)).Select(c => c.GetType().Name));
            sb.AppendLine($"{new string(' ', depth * 2)}- {ch.name} [{comps}] prefab={PrefabUtility.IsAnyPrefabInstanceRoot(ch.gameObject)}");
            Dump(ch, sb, depth + 1, maxDepth);
        }
    }

    public static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
}
