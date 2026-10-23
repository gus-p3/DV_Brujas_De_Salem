using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Herramientas de maquetación de niveles de desplazamiento lateral:
/// - Insertar espacio horizontal en una X (todo lo que está a la derecha se recorre).
/// - Escalar un tramo [x0, x1] (separa los objetos del tramo y recorre lo que sigue).
/// Las unidades que se mueven son los hijos directos de las zonas (_Zones/Zone_N) y de las raíces de
/// disparadores; los colliders de suelo largos se estiran en lugar de moverse, y se actualizan la zona
/// de muerte y los límites de la cámara. Todo queda registrado en Undo.
/// </summary>
public static class LevelLayoutTools
{
    private static readonly string[] UnitRoots = { "_Zones", "TutorialTriggers" };
    private const float LongColliderWidth = 6f;

    /// <summary>Inserta 'amount' unidades de espacio en 'atX'.</summary>
    public static void InsertSpace(float atX, float amount)
    {
        Apply(x => x >= atX ? x + amount : x, $"Insertar {amount} u en x={atX}");
    }

    /// <summary>Separa el tramo [x0, x1] multiplicando sus distancias por 'scale'; lo posterior se recorre.</summary>
    public static void ScaleRange(float x0, float x1, float scale)
    {
        float extra = (x1 - x0) * (scale - 1f);
        Apply(x => x <= x0 ? x : x <= x1 ? x0 + (x - x0) * scale : x + extra, $"Escalar tramo {x0}..{x1} x{scale}");
    }

    private static void Apply(Func<float, float> map, string undoName)
    {
        Undo.SetCurrentGroupName(undoName);
        // Los límites de los colliders movidos en el editor solo se actualizan al sincronizar la física
        Physics2D.SyncTransforms();
        int ground = LayerMask.NameToLayer("Ground");

        foreach (Transform unit in CollectUnits())
        {
            // Solo se estiran los suelos puros (sin arte propio). Las piezas con sprite, aunque sean largas
            // (techos, troncos), se mueven enteras para que el collider no se separe del dibujo.
            BoxCollider2D box = unit.GetComponent<BoxCollider2D>();
            if (box != null && IsPureGround(box, ground))
            {
                StretchCollider(box, map);
                continue;
            }
            // Se desplaza según dónde está la pieza de verdad (su collider o su dibujo), no según su transform,
            // que puede estar lejos si el collider tiene offset
            float center = CenterX(unit);
            Undo.RecordObject(unit, undoName);
            unit.position += new Vector3(map(center) - center, 0f, 0f);
        }

        // Suelos largos que no cuelgan de una zona (p. ej. World/Ground)
        foreach (BoxCollider2D box in UnityEngine.Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
        {
            if (!IsPureGround(box, ground) || IsUnit(box.transform)) continue;
            StretchCollider(box, map);
        }

        // Zona de muerte: se estira como un suelo
        GameObject kill = GameObject.Find("KillZone");
        if (kill != null && kill.TryGetComponent(out BoxCollider2D killBox)) StretchCollider(killBox, map);

        // Límite derecho de la cámara
        CameraFollow cam = UnityEngine.Object.FindFirstObjectByType<CameraFollow>();
        if (cam != null)
        {
            SerializedObject so = new SerializedObject(cam);
            SerializedProperty max = so.FindProperty("maxLimit");
            Vector2 v = max.vector2Value;
            v.x = map(v.x);
            max.vector2Value = v;
            so.ApplyModifiedProperties();
        }
    }

    /// <summary>Suelo largo sin arte propio (o con arte generado por GroundStrip): se estira en lugar de moverse.</summary>
    private static bool IsPureGround(BoxCollider2D box, int groundLayer)
    {
        if (box.isTrigger || box.gameObject.layer != groundLayer || box.bounds.size.x < LongColliderWidth) return false;
        return box.GetComponent<GroundStrip>() != null || box.GetComponentInChildren<SpriteRenderer>() == null;
    }

    private static float CenterX(Transform unit)
    {
        Collider2D col = unit.GetComponent<Collider2D>();
        if (col != null) return col.bounds.center.x;
        Renderer r = unit.GetComponentInChildren<Renderer>();
        return r != null ? r.bounds.center.x : unit.position.x;
    }

    private static void StretchCollider(BoxCollider2D box, Func<float, float> map)
    {
        Undo.RecordObject(box, "Estirar collider");
        Undo.RecordObject(box.transform, "Estirar collider");
        Bounds b = box.bounds;
        float newMin = map(b.min.x), newMax = map(b.max.x);
        float scaleX = box.transform.lossyScale.x;
        Vector2 size = box.size;
        size.x = (newMax - newMin) / Mathf.Abs(scaleX);
        box.size = size;
        // Recolocar el centro del collider en mundo moviendo su offset
        float centerWorld = (newMin + newMax) * 0.5f;
        Vector2 offset = box.offset;
        offset.x = (centerWorld - box.transform.position.x) / scaleX;
        box.offset = offset;
        GroundStrip strip = box.GetComponent<GroundStrip>();
        if (strip != null) strip.Rebuild();
    }

    private static IEnumerable<Transform> CollectUnits()
    {
        foreach (string rootName in UnitRoots)
        {
            GameObject root = GameObject.Find(rootName);
            if (root == null) continue;
            foreach (Transform zoneOrUnit in root.transform)
            {
                if (rootName == "_Zones")
                {
                    foreach (Transform unit in zoneOrUnit) yield return unit;
                }
                else
                {
                    yield return zoneOrUnit;
                }
            }
        }
    }

    private static bool IsUnit(Transform t)
    {
        foreach (Transform u in CollectUnits()) if (u == t) return true;
        return false;
    }
}
