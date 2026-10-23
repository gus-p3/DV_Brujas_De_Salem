using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Distribuye el HUD de juego para 1920x1080 con márgenes y tamaños legibles (Tools > Nivel > Maquetar HUD):
/// arriba a la izquierda los corazones, arriba al centro el tiempo y la barra de vuelo, arriba a la derecha
/// el puntaje y las piezas de la escoba, y abajo a la izquierda la barra de pociones (icono, cantidad y tecla).
/// </summary>
public static class HudLayoutBuilder
{
    private const float Margin = 40f;

    [MenuItem("Tools/Nivel/Maquetar HUD")]
    public static void Apply()
    {
        GameObject canvasGo = GameObject.Find("HUD_Canvas");
        if (canvasGo == null)
        {
            Debug.LogWarning("[HudLayoutBuilder] No hay HUD_Canvas en la escena.");
            return;
        }
        Transform canvas = canvasGo.transform;

        // Corazones (HeartsUI los genera en tiempo de ejecución con estos valores)
        HeartsUI hearts = canvas.GetComponentInChildren<HeartsUI>(true);
        if (hearts != null)
        {
            SerializedObject so = new SerializedObject(hearts);
            so.FindProperty("heartSize").vector2Value = new Vector2(64f, 64f);
            so.FindProperty("spacing").floatValue = 14f;
            so.FindProperty("margin").vector2Value = new Vector2(Margin, Margin);
            so.FindProperty("shakeMagnitude").floatValue = 7f;
            so.ApplyModifiedProperties();
            Place(hearts.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(Margin, -Margin), new Vector2(240f, 64f));
        }
        Place(canvas.Find("FrenzyAlarmIcon"), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(Margin + 3 * 78f + 12f, -Margin - 4f), new Vector2(56f, 56f));

        // Barra de energía del gato bajo los corazones (copia de la barra de vuelo)
        Transform cat = canvas.Find("CatSlider");
        Transform flight = canvas.Find("FlightSlider");
        if (cat == null && flight != null)
        {
            GameObject copy = Object.Instantiate(flight.gameObject, canvas);
            copy.name = "CatSlider";
            Undo.RegisterCreatedObjectUndo(copy, "HUD");
            cat = copy.transform;
            HUDController hud = canvas.GetComponent<HUDController>();
            if (hud != null)
            {
                Undo.RecordObject(hud, "HUD");
                hud.catEnergySlider = copy.GetComponent<UnityEngine.UI.Slider>();
            }
        }
        if (cat != null)
        {
            Place(cat, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(Margin, -Margin - 80f), new Vector2(240f, 18f));
            cat.gameObject.SetActive(false);
        }

        // Tiempo y barra de vuelo
        Text(canvas.Find("TimeText"), 60f);
        Place(canvas.Find("TimeText"), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -Margin + 12f), new Vector2(320f, 76f));
        Place(canvas.Find("FlightSlider"), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(460f, 26f));

        // Puntaje y escoba
        Text(canvas.Find("ScoreText"), 60f);
        Place(canvas.Find("ScoreText"), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-Margin, -Margin + 12f), new Vector2(320f, 76f));
        Transform broom = canvas.Find("BroomHUD");
        if (broom != null)
        {
            Place(broom, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-Margin, -120f), new Vector2(360f, 56f));
            Transform counter = broom.Find("CounterText");
            Text(counter, 30f);
            Place(counter, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(200f, 50f));
            string[] icons = { "Icon_Branch", "Icon_Straw", "Icon_Rope" };
            for (int i = 0; i < icons.Length; i++)
                Place(broom.Find(icons[i]), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(222f + i * 48f, 0f), new Vector2(42f, 42f));
        }

        // Barra de pociones
        string[] flasks = { "Flask_Cegar", "Flask_Hipnotizar", "Flask_Dormir" };
        for (int i = 0; i < flasks.Length; i++)
        {
            Transform flask = canvas.Find(flasks[i]);
            if (flask == null) continue;
            Place(flask, Vector2.zero, Vector2.zero, new Vector2(Margin + i * 190f, Margin + 34f), new Vector2(96f, 96f));
            Transform count = flask.Find("Count");
            if (count != null)
            {
                // Cantidad a la derecha del frasco, sin tocarlo
                Place(count, new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(80f, 56f));
                TMP_Text t = count.GetComponent<TMP_Text>();
                if (t != null)
                {
                    Undo.RecordObject(t, "HUD");
                    t.fontSize = 38f;
                    t.alignment = TextAlignmentOptions.Left;
                    t.textWrappingMode = TextWrappingModes.NoWrap;
                }
            }
            Transform key = flask.Find("KeyHint");
            if (key != null)
            {
                // Tecla debajo del frasco
                Place(key, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(60f, 30f));
                Text(key, 24f);
            }
        }

        // Banner central un poco más arriba para no tapar a la bruja
        Place(canvas.Find("MessageBanner"), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(1300f, 120f));

        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        Debug.Log("[HudLayoutBuilder] HUD maquetado.");
    }

    private static void Place(Transform t, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        if (t == null) return;
        RectTransform rt = t as RectTransform;
        if (rt == null) return;
        Undo.RecordObject(rt, "HUD");
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Text(Transform t, float size)
    {
        if (t == null) return;
        TMP_Text text = t.GetComponent<TMP_Text>();
        if (text == null) return;
        Undo.RecordObject(text, "HUD");
        text.fontSize = size;
    }
}
