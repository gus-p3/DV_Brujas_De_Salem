using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Añade a Level1 dos zonas de reto que combinan lo aprendido en el tutorial (Tools > Nivel > Construir zonas de reto):
/// - Zona 6B "Reto del sigilo": maizal vigilado, túnel largo de dos troncos con guardia en el techo
///   (pone a prueba el límite de tiempo del gato) y aldeanos con pociones.
/// - Zona 8B "Último reto": guardias cruzados, maizal, plataformas y aldeanos antes del vuelo final.
/// Las piezas se clonan de las ya existentes en el nivel, así que heredan su arte y su configuración.
/// </summary>
public static class ChallengeZoneBuilder
{
    private const string Zone6B = "Zone_6B_RetoSigilo";
    private const string Zone8B = "Zone_8B_UltimoReto";

    // Espacio que se abre para cada zona y punto donde se abre (coordenadas del nivel antes de insertar).
    // 8B se abre tras el último aldeano de la zona 8 y deja libre el final del segundo maizal (Z8BMargin).
    private const float Z8BInsertAt = 262f, Z8BLength = 66f, Z8BMargin = 8f;
    private const float Z6BInsertAt = 192f, Z6BLength = 72f;

    [MenuItem("Tools/Nivel/Construir zonas de reto")]
    public static void Build()
    {
        if (GameObject.Find(Zone6B) != null || GameObject.Find(Zone8B) != null)
        {
            Debug.LogWarning("[ChallengeZoneBuilder] Las zonas de reto ya existen.");
            return;
        }

        // Primero la de más a la derecha para no desplazar el punto de la otra
        LevelLayoutTools.InsertSpace(Z8BInsertAt, Z8BLength);
        LevelLayoutTools.InsertSpace(Z6BInsertAt, Z6BLength);
        Physics2D.SyncTransforms();

        BuildZone6B(Z6BInsertAt);
        BuildZone8B(Z8BInsertAt + Z6BLength + Z8BMargin);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[ChallengeZoneBuilder] Zonas de reto construidas.");
    }

    // ------------------------------------------------------------------ Zona 6B

    private static void BuildZone6B(float x0)
    {
        Transform zone = NewZone(Zone6B, "Zone_6_GatoTunel");

        Sign(zone, x0 + 3f, "Reto del sigilo: el gato se cansa pronto. Escóndete en el maizal, recupera fuerzas como bruja y cruza el túnel largo de una vez.");

        // Maizal vigilado por un guardia que lo atraviesa
        Clone(GameObject.Find("Cornfield_Z3_A"), zone, new Vector3(x0 + 12f, 0f, 0f), "Cornfield_6B");
        Guard(zone, "Guard_6B_Maizal", GameObject.Find("Guard_Z3"), x0 + 6f, x0 + 22f, 0.17f);

        // Túnel largo: dos troncos huecos seguidos (26 u) con un techo continuo
        float t0 = x0 + 24.5f, segment = 13f;
        Transform back = GameObject.Find("Tunnel_Visual_Back").transform;
        Transform front = GameObject.Find("Tunnel_Visual_Front").transform;
        for (int i = 0; i < 2; i++)
        {
            float cx = t0 + segment * (i + 0.5f);
            TunnelPiece(back, zone, cx, segment, "Tunnel_6B_Back_" + i);
            TunnelPiece(front, zone, cx, segment, "Tunnel_6B_Front_" + i);
        }
        GameObject ceilingSrc = GameObject.Find("Tunnel_Solid_Ceiling");
        GameObject ceiling = Clone(ceilingSrc, zone, ceilingSrc.transform.position, "Tunnel_6B_Ceiling");
        StretchBox(ceiling.GetComponent<BoxCollider2D>(), t0, t0 + segment * 2f);
        Clone(GameObject.Find("CatOnlyPassage_Entry"), zone, new Vector3(t0 - 0.3f, 1f, 0f), "CatOnlyPassage_6B_Entry");
        Clone(GameObject.Find("CatOnlyPassage_Exit"), zone, new Vector3(t0 + segment * 2f + 0.3f, 1f, 0f), "CatOnlyPassage_6B_Exit");

        // Techo transitable: dos troncos sobre el túnel y un guardia que lo recorre
        GameObject roofSrc = GameObject.Find("Platform_Tunnel_Roof");
        float roofTop = roofSrc.GetComponent<BoxCollider2D>().bounds.max.y;
        for (int i = 0; i < 2; i++)
        {
            GameObject roof = Clone(roofSrc, zone, roofSrc.transform.position, "Tunnel_6B_Roof_" + i);
            FitWidth(roof, t0 + segment * (i + 0.5f), segment + 0.4f);
        }
        Guard(zone, "Guard_6B_Techo", GameObject.Find("Guard_Z6_Roof"), t0 + 1.5f, t0 + segment * 2f - 1.5f, roofTop + 0.02f);
        Prefab(zone, "HeartPickup", new Vector3(t0 + segment, 0.6f, 0f), "HeartPickup_6B");

        // Salida: aldeanos con pociones a mano y checkpoint
        float v0 = t0 + segment * 2f + 3f;
        Prefab(zone, "PotionPickup_Sleep", new Vector3(v0 + 1f, 0.6f, 0f), "PotionPickup_6B_Sleep");
        Prefab(zone, "Villager", new Vector3(v0 + 5f, 0.17f, 0f), "Villager_6B_1");
        Prefab(zone, "PotionPickup_Hypnotize", new Vector3(v0 + 8.5f, 0.6f, 0f), "PotionPickup_6B_Hypnotize");
        Prefab(zone, "VillagerF", new Vector3(v0 + 12f, 0.17f, 0f), "Villager_6B_2");
        Prefab(zone, "Checkpoint", new Vector3(x0 + Z6BLength - 3f, 0.17f, 0f), "Checkpoint_6B");
    }

    // ------------------------------------------------------------------ Zona 8B

    private static void BuildZone8B(float x0)
    {
        Transform zone = NewZone(Zone8B, "Zone_8_RetoCombinado");

        Sign(zone, x0 + 3f, "Último reto: guardias y aldeanos vigilan el camino a la aldea. Combina el gato, las pociones y el vuelo.");

        Guard(zone, "Guard_8B_1", GameObject.Find("Guard_Z3"), x0 + 7f, x0 + 20f, 0.17f);
        Clone(GameObject.Find("Cornfield_Z3_B"), zone, new Vector3(x0 + 15f, 0f, 0f), "Cornfield_8B");
        Prefab(zone, "PotionPickup_Blind", new Vector3(x0 + 23f, 0.6f, 0f), "PotionPickup_8B_Blind");

        // Ruta alta opcional sobre el segundo guardia
        GameObject p1Src = GameObject.Find("Platform_Z8_1");
        GameObject p1 = Clone(p1Src, zone, p1Src.transform.position, "Platform_8B_1");
        MoveTo(p1, x0 + 27f);
        GameObject p2Src = GameObject.Find("Platform_Z8_2");
        GameObject p2 = Clone(p2Src, zone, p2Src.transform.position, "Platform_8B_2");
        MoveTo(p2, x0 + 35f);
        float p1Top = p1.GetComponent<BoxCollider2D>().bounds.max.y;
        Prefab(zone, "HeartPickup", new Vector3(x0 + 27f, p1Top + 0.4f, 0f), "HeartPickup_8B");
        Guard(zone, "Guard_8B_2", GameObject.Find("Guard_Z3"), x0 + 25f, x0 + 41f, 0.17f);
        Prefab(zone, "PotionPickup_Sleep", new Vector3(x0 + 39f, 0.6f, 0f), "PotionPickup_8B_Sleep");

        Prefab(zone, "Villager", new Vector3(x0 + 46f, 0.17f, 0f), "Villager_8B_1");
        Prefab(zone, "VillagerF", new Vector3(x0 + 52f, 0.17f, 0f), "Villager_8B_2");
    }

    // ------------------------------------------------------------------ utilidades

    private static Transform NewZone(string zoneName, string afterSibling)
    {
        Transform zones = GameObject.Find("_Zones").transform;
        GameObject go = new GameObject(zoneName);
        Undo.RegisterCreatedObjectUndo(go, zoneName);
        go.transform.SetParent(zones, false);
        Transform after = zones.Find(afterSibling);
        if (after != null) go.transform.SetSiblingIndex(after.GetSiblingIndex() + 1);
        return go.transform;
    }

    private static GameObject Clone(GameObject src, Transform parent, Vector3 position, string cloneName)
    {
        GameObject go;
        GameObject asset = PrefabUtility.GetCorrespondingObjectFromSource(src);
        if (asset != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            // Copiar los ajustes de la instancia de origen (rango de visión, velocidad, etc.)
            Component[] from = src.GetComponents<Component>();
            Component[] to = go.GetComponents<Component>();
            for (int i = 0; i < from.Length && i < to.Length; i++)
                if (from[i] != null && to[i] != null && from[i].GetType() == to[i].GetType() && !(from[i] is Transform))
                {
                    EditorUtility.CopySerialized(from[i], to[i]);
                    RemapReferences(to[i], src.transform, go.transform);
                }
            go.transform.localScale = src.transform.localScale;
        }
        else
        {
            go = Object.Instantiate(src, parent);
        }
        Undo.RegisterCreatedObjectUndo(go, cloneName);
        go.name = cloneName;
        go.transform.position = position;
        return go;
    }

    /// <summary>
    /// CopySerialized copia también las referencias a objetos del original (su Visual, su cono, sus iconos):
    /// se redirigen al objeto equivalente dentro del clon para que cada pieza controle lo suyo.
    /// </summary>
    public static void RemapReferences(Component component, Transform srcRoot, Transform dstRoot)
    {
        SerializedObject so = new SerializedObject(component);
        SerializedProperty it = so.GetIterator();
        bool changed = false;
        while (it.Next(true))
        {
            if (it.propertyType != SerializedPropertyType.ObjectReference || it.objectReferenceValue == null) continue;
            Transform refT = it.objectReferenceValue is Component c ? c.transform
                : it.objectReferenceValue is GameObject g ? g.transform : null;
            if (refT == null || !refT.IsChildOf(srcRoot)) continue;
            string path = AnimationUtility.CalculateTransformPath(refT, srcRoot);
            Transform target = path.Length == 0 ? dstRoot : dstRoot.Find(path);
            if (target == null) continue;
            Object replacement = it.objectReferenceValue is GameObject ? target.gameObject
                : (Object)target.GetComponent(it.objectReferenceValue.GetType());
            if (replacement == null) continue;
            it.objectReferenceValue = replacement;
            changed = true;
        }
        if (changed) so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject Prefab(Transform parent, string prefabName, Vector3 position, string instanceName)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/{prefabName}.prefab");
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        Undo.RegisterCreatedObjectUndo(go, instanceName);
        go.name = instanceName;
        go.transform.position = position;
        return go;
    }

    private static void Sign(Transform zone, float x, string message)
    {
        GameObject sign = Clone(GameObject.Find("Sign_Z3_Escondite"), zone, new Vector3(x, 0.17f, 0f), "Sign_" + zone.name);
        SerializedObject so = new SerializedObject(sign.GetComponent<WorldSign>());
        so.FindProperty("message").stringValue = message;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Guard(Transform zone, string guardName, GameObject template, float xa, float xb, float y)
    {
        GameObject g = Clone(template, zone, new Vector3((xa + xb) * 0.5f, y, 0f), guardName);
        List<Object> points = new List<Object>();
        foreach ((float x, string suffix) in new[] { (xa, "WpA"), (xb, "WpB") })
        {
            GameObject wp = new GameObject($"{guardName}_{suffix}");
            Undo.RegisterCreatedObjectUndo(wp, guardName);
            wp.transform.SetParent(zone, false);
            wp.transform.position = new Vector3(x, y, 0f);
            points.Add(wp.transform);
        }
        SerializedObject so = new SerializedObject(g.GetComponent<PatrolRoute>());
        SerializedProperty arr = so.FindProperty("waypoints");
        arr.arraySize = points.Count;
        for (int i = 0; i < points.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void TunnelPiece(Transform src, Transform zone, float centerX, float width, string pieceName)
    {
        GameObject go = Clone(src.gameObject, zone, src.position, pieceName);
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        float native = sr.sprite.bounds.size.x;
        go.transform.localScale = new Vector3(width / native, src.lossyScale.y, 1f);
        go.transform.position = new Vector3(centerX, src.position.y, 0f);
    }

    private static void StretchBox(BoxCollider2D box, float xMin, float xMax)
    {
        Transform t = box.transform;
        t.position = new Vector3((xMin + xMax) * 0.5f, t.position.y, t.position.z);
        box.offset = new Vector2(0f, box.offset.y);
        box.size = new Vector2((xMax - xMin) / Mathf.Abs(t.lossyScale.x), box.size.y);
    }

    private static void FitWidth(GameObject go, float centerX, float width)
    {
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        Vector3 s = go.transform.localScale;
        float current = sr.bounds.size.x;
        go.transform.localScale = new Vector3(s.x * width / current, s.y, s.z);
        Physics2D.SyncTransforms();
        go.transform.position += new Vector3(centerX - sr.bounds.center.x, 0f, 0f);
    }

    private static void MoveTo(GameObject go, float centerX)
    {
        Renderer r = go.GetComponent<Renderer>();
        go.transform.position = new Vector3(go.transform.position.x + centerX - r.bounds.center.x, go.transform.position.y, 0f);
    }
}
