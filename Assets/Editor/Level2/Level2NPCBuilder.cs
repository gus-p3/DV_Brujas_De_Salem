using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Configura e instancia todos los Guardias, Aldeanos (con >= 3 patrones erráticos)
/// y Recursos (Pociones y Corazones) en Level2.unity (Acto 2 — Pueblo y Mercado).
/// Mantiene los prefabs originales 100% intactos mediante overrides limpios por instancia.
/// </summary>
public static class Level2NPCBuilder
{
    private const string ScenePath = "Assets/Scenes/Level2.unity";
    private const string GuardPrefabPath = "Assets/Prefabs/Guard.prefab";
    private const string VillagerPrefabPath = "Assets/Prefabs/Villager.prefab";
    private const string VillagerFPrefabPath = "Assets/Prefabs/VillagerF.prefab";
    private const string VillagerBoyPrefabPath = "Assets/Prefabs/VillagerBoy.prefab";
    private const string VillagerPreacherPrefabPath = "Assets/Prefabs/VillagerPreacher.prefab";

    private const string PotionBlindPath = "Assets/Prefabs/PotionPickup_Blind.prefab";
    private const string PotionHypnotizePath = "Assets/Prefabs/PotionPickup_Hypnotize.prefab";
    private const string PotionSleepPath = "Assets/Prefabs/PotionPickup_Sleep.prefab";
    private const string HeartPickupPath = "Assets/Prefabs/HeartPickup.prefab";

    [MenuItem("Brujas/Nivel 2/Configurar NPCs y Recursos")]
    public static void BuildNPCsAndPickups()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Cargar prefabs
        GameObject guardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GuardPrefabPath);
        GameObject villagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VillagerPrefabPath);
        GameObject villagerFPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VillagerFPrefabPath);
        GameObject villagerBoyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VillagerBoyPrefabPath);
        GameObject villagerPreacherPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VillagerPreacherPrefabPath);

        GameObject potBlindPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PotionBlindPath);
        GameObject potHypnoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PotionHypnotizePath);
        GameObject potSleepPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PotionSleepPath);
        GameObject heartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeartPickupPath);

        // 1. Organizar raíz NPCs
        Transform npcsRoot = GameObject.Find("NPCs")?.transform;
        if (npcsRoot == null) npcsRoot = new GameObject("NPCs").transform;

        // Limpiar contenido previo de NPCs
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform ch in npcsRoot) toDestroy.Add(ch.gameObject);
        foreach (var go in toDestroy) Object.DestroyImmediate(go);

        // 2. Organizar raíz Pickups bajo Gameplay
        Transform gameplayRoot = GameObject.Find("Gameplay")?.transform;
        if (gameplayRoot == null) gameplayRoot = new GameObject("Gameplay").transform;

        Transform pickupsRoot = gameplayRoot.Find("Pickups");
        if (pickupsRoot != null) Object.DestroyImmediate(pickupsRoot.gameObject);
        pickupsRoot = new GameObject("Pickups").transform;
        pickupsRoot.SetParent(gameplayRoot, false);

        int groundLayer = LayerMask.NameToLayer("Ground");
        LayerMask groundMask = 1 << groundLayer;

        // =========================================================================
        // ZONA 1: Entrada al Pueblo (Sin enemigos, recursos de calibración)
        // =========================================================================
        Transform z1Npc = CreateSubGroup(npcsRoot, "Zona_1");
        // Recursos Z1
        InstantiatePickup(pickupsRoot, potSleepPrefab, "Potion_Sleep_Z1", new Vector3(13f, 2.7f, 0f));
        InstantiatePickup(pickupsRoot, heartPrefab, "Heart_Z1", new Vector3(22f, 1.4f, 0f));

        // =========================================================================
        // ZONA 2: Calle de Puestos Bajos (1 Guardia + 2 Aldeanos)
        // =========================================================================
        Transform z2Npc = CreateSubGroup(npcsRoot, "Zona_2");
        // Guardia Z2: Patrulla entre puestos (x = 33 a 51)
        Transform[] wpZ2 = CreateWaypoints(z2Npc, "Waypoints_Z2", new Vector2(33f, 0.17f), new Vector2(51f, 0.17f));
        CreateGuard(z2Npc, guardPrefab, "Guard_Z2", new Vector3(38f, 0.17f, 0f), wpZ2, PatrolMode.PingPong, groundMask);

        // Aldeano Z2_1 (Masculino): Patrón WalkAndReturn cerca del callejón (x = 42)
        CreateVillager(z2Npc, villagerPrefab, "Villager_Z2_1", new Vector3(42f, 0.17f, 0f), WanderPattern.WalkAndReturn, 3.5f);

        // Aldeano Z2_2 (Femenino): Patrón LookAround vigilando puesto 3 (x = 54)
        CreateVillager(z2Npc, villagerFPrefab, "Villager_Z2_2", new Vector3(54f, 0.17f, 0f), WanderPattern.LookAround, 2.5f);

        // Recurso Z2
        InstantiatePickup(pickupsRoot, potBlindPrefab, "Potion_Blind_Z2", new Vector3(45f, 4.15f, 0f));

        // =========================================================================
        // ZONA 3: Plaza Central y Brecha Canal (1 Guardia + 3 Aldeanos de 3 variantes)
        // =========================================================================
        Transform z3Npc = CreateSubGroup(npcsRoot, "Zona_3");

        // Guardia Z3: Patrulla lado derecho de la plaza (x = 83 a 92)
        Transform[] wpZ3 = CreateWaypoints(z3Npc, "Waypoints_Z3", new Vector2(83f, 0.17f), new Vector2(92f, 0.17f));
        CreateGuard(z3Npc, guardPrefab, "Guard_Z3_1", new Vector3(85f, 0.17f, 0f), wpZ3, PatrolMode.PingPong, groundMask);

        // Aldeano Niño: Patrón ShortStops en la entrada de la plaza (x = 66)
        CreateVillager(z3Npc, villagerBoyPrefab, "Villager_Z3_1_Boy", new Vector3(66f, 0.17f, 0f), WanderPattern.ShortStops, 3.0f);

        // Aldeano Adulto: Patrón WalkAndReturn antes de la brecha (x = 72)
        CreateVillager(z3Npc, villagerPrefab, "Villager_Z3_2", new Vector3(72f, 0.17f, 0f), WanderPattern.WalkAndReturn, 2.5f);

        // Aldeano Predicador: Patrón LookAround observando el mercado (x = 88)
        CreateVillager(z3Npc, villagerPreacherPrefab, "Villager_Z3_3_Preacher", new Vector3(88f, 0.17f, 0f), WanderPattern.LookAround, 3.0f);

        // Recursos Z3
        InstantiatePickup(pickupsRoot, potBlindPrefab, "Potion_Blind_Z3", new Vector3(65f, 1.7f, 0f));
        InstantiatePickup(pickupsRoot, heartPrefab, "Heart_Z3", new Vector3(87f, 4.55f, 0f));

        // =========================================================================
        // ZONA 4: Callejón Estrecho y Pasaje Felino (1 Guardia en tejado + 1 Aldeana)
        // =========================================================================
        Transform z4Npc = CreateSubGroup(npcsRoot, "Zona_4");

        // Guardia Z4: Patrulla sobre el tejado del túnel (x = 106 a 114, y = 4.8)
        Transform[] wpZ4 = CreateWaypoints(z4Npc, "Waypoints_Z4_Roof", new Vector2(106f, 4.8f), new Vector2(114f, 4.8f));
        CreateGuard(z4Npc, guardPrefab, "Guard_Z4_Roof", new Vector3(107f, 4.8f, 0f), wpZ4, PatrolMode.PingPong, groundMask);

        // Aldeana Z4: Patrón LookAround a la salida del túnel (x = 120)
        CreateVillager(z4Npc, villagerFPrefab, "Villager_Z4_Exit", new Vector3(120f, 0.17f, 0f), WanderPattern.LookAround, 2.5f);

        // Recursos Z4
        InstantiatePickup(pickupsRoot, potSleepPrefab, "Potion_Sleep_Z4", new Vector3(101f, 3.4f, 0f));

        // =========================================================================
        // ZONA 5: Gran Mercado Techado (2 Guardias [Calle + Altura] + 4 Aldeanos mezclados)
        // =========================================================================
        Transform z5Npc = CreateSubGroup(npcsRoot, "Zona_5");

        // Guardia Callejero: Patrulla calle baja (x = 132 a 144)
        Transform[] wpZ5Street = CreateWaypoints(z5Npc, "Waypoints_Z5_Street", new Vector2(132f, 0.17f), new Vector2(144f, 0.17f));
        CreateGuard(z5Npc, guardPrefab, "Guard_Z5_Street", new Vector3(135f, 0.17f, 0f), wpZ5Street, PatrolMode.PingPong, groundMask);

        // Guardia Elevado: Patrulla circuito sobre toldos y techos (x = 146 a 158, y = 4.7)
        Transform[] wpZ5Upper = CreateWaypoints(z5Npc, "Waypoints_Z5_Upper", new Vector2(146f, 4.7f), new Vector2(158f, 4.7f));
        CreateGuard(z5Npc, guardPrefab, "Guard_Z5_Upper", new Vector3(150f, 4.7f, 0f), wpZ5Upper, PatrolMode.Loop, groundMask);

        // 4 Aldeanos con los 3 patrones representados:
        CreateVillager(z5Npc, villagerPrefab, "Villager_Z5_1", new Vector3(134f, 0.17f, 0f), WanderPattern.WalkAndReturn, 3.5f);
        CreateVillager(z5Npc, villagerFPrefab, "Villager_Z5_2_Female", new Vector3(140f, 0.17f, 0f), WanderPattern.ShortStops, 2.8f);
        CreateVillager(z5Npc, villagerBoyPrefab, "Villager_Z5_3_Boy", new Vector3(147f, 0.17f, 0f), WanderPattern.LookAround, 3.2f);
        CreateVillager(z5Npc, villagerPreacherPrefab, "Villager_Z5_4_Preacher", new Vector3(156f, 0.17f, 0f), WanderPattern.WalkAndReturn, 3.0f);

        // Recursos Z5
        InstantiatePickup(pickupsRoot, potHypnoPrefab, "Potion_Hypno_Z5", new Vector3(138f, 4.15f, 0f));
        InstantiatePickup(pickupsRoot, heartPrefab, "Heart_Z5", new Vector3(150f, 0.5f, 0f));

        // =========================================================================
        // ZONA 6: Salida del Mercado y Portón (1 Guardia Final + 1 Aldeano)
        // =========================================================================
        Transform z6Npc = CreateSubGroup(npcsRoot, "Zona_6");

        // Guardia Z6: Patrulla frente al portón (x = 174 a 180)
        Transform[] wpZ6 = CreateWaypoints(z6Npc, "Waypoints_Z6", new Vector2(174f, 0.17f), new Vector2(180f, 0.17f));
        CreateGuard(z6Npc, guardPrefab, "Guard_Z6", new Vector3(176f, 0.17f, 0f), wpZ6, PatrolMode.PingPong, groundMask);

        // Aldeano Z6: Patrón ShortStops nervioso cerca del portón
        CreateVillager(z6Npc, villagerPrefab, "Villager_Z6", new Vector3(171f, 0.17f, 0f), WanderPattern.ShortStops, 2.5f);

        // Recurso Z6
        InstantiatePickup(pickupsRoot, heartPrefab, "Heart_Z6", new Vector3(173.5f, 1.4f, 0f));

        // 3. Conectar CaptureListener si está en escena
        CaptureListener cl = Object.FindFirstObjectByType<CaptureListener>();
        if (cl != null) cl.ConnectEnemies();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Level2NPCBuilder] ¡Guardias, Aldeanos y Recursos configurados exitosamente en Level2.unity!");
    }

    private static Transform CreateSubGroup(Transform parent, string name)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        return g.transform;
    }

    private static Transform[] CreateWaypoints(Transform parent, string containerName, Vector2 a, Vector2 b)
    {
        GameObject c = new GameObject(containerName);
        c.transform.SetParent(parent, false);

        GameObject wpA = new GameObject("WP_A");
        wpA.transform.SetParent(c.transform, false);
        wpA.transform.position = a;

        GameObject wpB = new GameObject("WP_B");
        wpB.transform.SetParent(c.transform, false);
        wpB.transform.position = b;

        return new Transform[] { wpA.transform, wpB.transform };
    }

    private static void CreateGuard(Transform parent, GameObject prefab, string name, Vector3 pos, Transform[] waypoints, PatrolMode mode, LayerMask groundMask)
    {
        if (prefab == null) return;
        GameObject guard = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
        guard.name = name;
        guard.transform.SetParent(parent, false);
        guard.transform.position = pos;

        PatrolRoute pr = guard.GetComponent<PatrolRoute>();
        if (pr != null)
        {
            var so = new SerializedObject(pr);
            so.FindProperty("patrolMode").enumValueIndex = (int)mode;
            so.FindProperty("groundMask").intValue = groundMask.value;

            SerializedProperty wpProp = so.FindProperty("waypoints");
            wpProp.ClearArray();
            if (waypoints != null)
            {
                for (int i = 0; i < waypoints.Length; i++)
                {
                    wpProp.InsertArrayElementAtIndex(i);
                    wpProp.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i];
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void CreateVillager(Transform parent, GameObject prefab, string name, Vector3 pos, WanderPattern pattern, float wanderRadius)
    {
        if (prefab == null) return;
        GameObject v = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
        v.name = name;
        v.transform.SetParent(parent, false);
        v.transform.position = pos;

        VillagerAI ai = v.GetComponent<VillagerAI>();
        if (ai != null)
        {
            var so = new SerializedObject(ai);
            var radProp = so.FindProperty("maxWanderRadius");
            if (radProp != null) radProp.floatValue = wanderRadius;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Componente por instancia de configuración de patrón en Level 2
        VillagerEncounterConfig config = v.GetComponent<VillagerEncounterConfig>();
        if (config == null) config = v.AddComponent<VillagerEncounterConfig>();
        config.AssignedPattern = pattern;
    }

    private static void InstantiatePickup(Transform parent, GameObject prefab, string name, Vector3 pos)
    {
        if (prefab == null) return;
        GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
        p.name = name;
        p.transform.SetParent(parent, false);
        p.transform.position = pos;
    }
}
