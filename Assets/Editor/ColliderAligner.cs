using UnityEditor;
using UnityEngine;

/// <summary>
/// Utilidad de edición para alinear colliders de plataformas y suelo
/// con las superficies visuales exactas de los sprites.
/// </summary>
public static class ColliderAligner
{
    [MenuItem("Brujas/5. Nivel/Alinear colliders con el arte")]
    public static void AlignAllColliders()
    {
        // 1. Alinear plataformas
        Transform platformsRoot = GameObject.Find("World/Platforms")?.transform;
        if (platformsRoot != null)
        {
            int count = 0;
            for (int i = 0; i < platformsRoot.childCount; i++)
            {
                Transform plat = platformsRoot.GetChild(i);
                BoxCollider2D col = plat.GetComponent<BoxCollider2D>();
                SpriteRenderer sr = plat.GetComponent<SpriteRenderer>();
                if (col != null && sr != null && sr.sprite != null)
                {
                    Undo.RecordObject(col, "Alinear Collider Plataforma");
                    float w = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
                    float targetTop = 0.27f;
                    float colThickness = 0.35f;
                    col.size = new Vector2(w * 0.92f, colThickness);
                    col.offset = new Vector2(0f, targetTop - (colThickness * 0.5f));
                    count++;
                }
            }
            Debug.Log($"[ColliderAligner] Se alinearon los colliders de {count} plataformas.");
        }

        // 2. Alinear suelo
        GameObject groundObj = GameObject.Find("World/Ground");
        if (groundObj != null)
        {
            BoxCollider2D gCol = groundObj.GetComponent<BoxCollider2D>();
            if (gCol != null)
            {
                Undo.RecordObject(gCol, "Alinear Collider Suelo");
                gCol.size = new Vector2(115f, 4.17f);
                gCol.offset = new Vector2(0f, 0.085f);
                Debug.Log("[ColliderAligner] Se alineó el BoxCollider2D del suelo con la línea de césped en y = +0.17.");
            }
        }

        // 3. Alinear letreros y cámara si están presentes
        GameObject sign1 = GameObject.Find("World/Tutorials/Tutorial_Sign_1");
        if (sign1 != null)
        {
            Undo.RecordObject(sign1.transform, "Mover Tutorial_Sign_1");
            sign1.transform.position = new Vector3(4f, 1.2f, 0f);
            SpriteRenderer sr = sign1.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Undo.RecordObject(sr, "Cambiar sorting layer sign");
                sr.sortingLayerName = "Midground";
                sr.sortingOrder = 1;
            }
        }
        GameObject sign2 = GameObject.Find("World/Tutorials/Tutorial_Sign_2");
        if (sign2 != null)
        {
            SpriteRenderer sr = sign2.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Undo.RecordObject(sr, "Cambiar sorting layer sign");
                sr.sortingLayerName = "Midground";
                sr.sortingOrder = 1;
            }
        }

        // 4. Alinear Maizales (agregar CornFoliageFade si falta)
        GameObject cf1 = GameObject.Find("World/Cornfields/Cornfield_Zone1");
        if (cf1 != null && cf1.GetComponent<CornFoliageFade>() == null)
        {
            Undo.AddComponent<CornFoliageFade>(cf1);
        }
        GameObject cf3 = GameObject.Find("World/Cornfields/Cornfield_Zone3");
        if (cf3 != null && cf3.GetComponent<CornFoliageFade>() == null)
        {
            Undo.AddComponent<CornFoliageFade>(cf3);
        }

        // 5. Alinear cámara
        Camera cam = Camera.main;
        if (cam != null)
        {
            CameraFollow cf = cam.GetComponent<CameraFollow>();
            if (cf != null)
            {
                SerializedObject so = new SerializedObject(cf);
                so.FindProperty("offset").vector3Value = new Vector3(0f, 1.2f, -10f);
                so.FindProperty("minLimit").vector2Value = new Vector2(-5f, 0.17f);
                so.FindProperty("maxLimit").vector2Value = new Vector2(110f, 15f);
                so.ApplyModifiedProperties();
            }
        }

        // 6. Reconstruir HUD con nuevo diseño
        UISetup.BuildHUD();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene()
        );
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[ColliderAligner] Escena actualizada y guardada con éxito.");
    }
}
