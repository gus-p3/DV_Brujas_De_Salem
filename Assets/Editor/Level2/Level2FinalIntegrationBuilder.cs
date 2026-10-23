using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Level2;

namespace Level2.Editor
{
    /// <summary>
    /// Constructor de integración final para Level2.unity (Prompt 5 de 5).
    /// Ensambla en la escena:
    /// 1. Level2Bootstrap en _MANAGERS para sanitización de estado y corrección de E-04.
    /// 2. Level2Goal en Goal_Exit_Marker (x=182) para disparar la condición de victoria.
    /// 3. Level2Complete_Canvas con la pantalla completa de estadísticas y navegación segura (Acto 3 / Menú / Reintentar).
    /// 4. Ocultamiento del LevelCompletePanel genérico clonado en HUDController para evitar superposiciones.
    /// </summary>
    public static class Level2FinalIntegrationBuilder
    {
        private const string ScenePath = "Assets/Scenes/Level2.unity";

        [MenuItem("Brujas/Nivel 2/Integración Final (Prompt 5)")]
        public static void IntegrateFinalLevel2()
        {
            Debug.Log("[Level2FinalIntegrationBuilder] Iniciando integración final de Level2.unity...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Level2FinalIntegrationBuilder] No se pudo abrir {ScenePath}");
                return;
            }

            // 1. Integrar Level2Bootstrap
            IntegrateBootstrap();

            // 2. Integrar UI de Victoria (Level2Complete_Canvas)
            Level2CompleteScreen completeScreen = IntegrateCompleteScreen();

            // 3. Integrar Level2Goal
            IntegrateGoal(completeScreen);

            // 4. Desactivar panel de completado genérico si existe en HUDController
            SanitizeClonedHUD();

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Level2FinalIntegrationBuilder] ¡Integración final de Level2 completada con éxito! (Guardado: {saved})");
        }

        private static void IntegrateBootstrap()
        {
            GameObject managersRoot = GameObject.Find("Managers") ?? GameObject.Find("_MANAGERS");
            if (managersRoot == null)
            {
                managersRoot = new GameObject("Managers");
            }

            Transform existingBoot = managersRoot.transform.Find("Level2_Bootstrap");
            GameObject bootObj = existingBoot != null ? existingBoot.gameObject : new GameObject("Level2_Bootstrap");
            bootObj.transform.SetParent(managersRoot.transform, false);

            Level2Bootstrap boot = bootObj.GetComponent<Level2Bootstrap>();
            if (boot == null)
            {
                boot = bootObj.AddComponent<Level2Bootstrap>();
            }

            Debug.Log("[Level2FinalIntegrationBuilder] Level2Bootstrap configurado en Managers/Level2_Bootstrap.");
        }

        private static void IntegrateGoal(Level2CompleteScreen completeScreen)
        {
            GameObject goalObj = GameObject.Find("Goal_Exit_Marker");
            if (goalObj == null)
            {
                GameObject gameplayRoot = GameObject.Find("_GAMEPLAY");
                goalObj = new GameObject("Goal_Exit_Marker");
                if (gameplayRoot != null)
                {
                    goalObj.transform.SetParent(gameplayRoot.transform, false);
                }
                goalObj.transform.position = new Vector3(182f, 2.0f, 0f);
            }

            BoxCollider2D boxCol = goalObj.GetComponent<BoxCollider2D>();
            if (boxCol == null)
            {
                boxCol = goalObj.AddComponent<BoxCollider2D>();
            }
            boxCol.isTrigger = true;
            boxCol.size = new Vector2(3.0f, 4.5f);

            Level2Goal goal = goalObj.GetComponent<Level2Goal>();
            if (goal == null)
            {
                goal = goalObj.AddComponent<Level2Goal>();
            }

            if (completeScreen != null)
            {
                goal.SetCompleteScreen(completeScreen);
            }

            Debug.Log("[Level2FinalIntegrationBuilder] Level2Goal configurado en Goal_Exit_Marker.");
        }

        private static Level2CompleteScreen IntegrateCompleteScreen()
        {
            GameObject uiRoot = GameObject.Find("UI") ?? GameObject.Find("_UI");
            if (uiRoot == null)
            {
                uiRoot = new GameObject("UI");
            }

            // Buscar si ya existe Level2Complete_Canvas
            Transform existingCanvas = uiRoot.transform.Find("Level2Complete_Canvas");
            GameObject canvasObj = existingCanvas != null ? existingCanvas.gameObject : new GameObject("Level2Complete_Canvas");
            canvasObj.transform.SetParent(uiRoot.transform, false);

            Canvas canvas = canvasObj.GetComponent<Canvas>();
            if (canvas == null) canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            GraphicRaycaster raycaster = canvasObj.GetComponent<GraphicRaycaster>();
            if (raycaster == null) canvasObj.AddComponent<GraphicRaycaster>();

            Level2CompleteScreen screen = canvasObj.GetComponent<Level2CompleteScreen>();
            if (screen == null) screen = canvasObj.AddComponent<Level2CompleteScreen>();

            // Panel Principal
            Transform existingPanel = canvasObj.transform.Find("Panel_LevelComplete");
            GameObject panelObj = existingPanel != null ? existingPanel.gameObject : new GameObject("Panel_LevelComplete");
            panelObj.transform.SetParent(canvasObj.transform, false);

            RectTransform panelRt = panelObj.GetComponent<RectTransform>();
            if (panelRt == null) panelRt = panelObj.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(760, 520);
            panelRt.anchoredPosition = Vector2.zero;

            Image panelBg = panelObj.GetComponent<Image>();
            if (panelBg == null) panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.06f, 0.05f, 0.08f, 0.94f);

            // Borde estético dorado
            Transform existingBorder = panelObj.transform.Find("Border");
            GameObject borderObj = existingBorder != null ? existingBorder.gameObject : new GameObject("Border");
            borderObj.transform.SetParent(panelObj.transform, false);
            RectTransform borderRt = borderObj.GetComponent<RectTransform>();
            if (borderRt == null) borderRt = borderObj.AddComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = new Vector2(-12, -12);
            borderRt.anchoredPosition = Vector2.zero;
            Image borderImg = borderObj.GetComponent<Image>();
            if (borderImg == null) borderImg = borderObj.AddComponent<Image>();
            borderImg.color = new Color(0.85f, 0.70f, 0.25f, 0.40f);

            // Título
            TextMeshProUGUI titleTmp = CreateTMPText(panelObj.transform, "TitleText",
                "¡MERCADO DE SALEM SUPERADO!", 32, FontStyles.Bold,
                new Color(1f, 0.84f, 0.0f, 1f),
                new Vector2(0f, 195f), new Vector2(700, 50));

            // Subtítulo
            TextMeshProUGUI subtitleTmp = CreateTMPText(panelObj.transform, "SubtitleText",
                "Has recolectado los ingredientes requeridos para el aquelarre colonial.\n<color=#D4AF37>(Acto 3: La Iglesia — Próximamente)</color>",
                17, FontStyles.Normal,
                new Color(0.9f, 0.9f, 0.9f, 1f),
                new Vector2(0f, 130f), new Vector2(700, 55));

            // Caja de Estadísticas
            Transform existingStatsBox = panelObj.transform.Find("StatsBox");
            GameObject statsBoxObj = existingStatsBox != null ? existingStatsBox.gameObject : new GameObject("StatsBox");
            statsBoxObj.transform.SetParent(panelObj.transform, false);
            RectTransform sbRt = statsBoxObj.GetComponent<RectTransform>();
            if (sbRt == null) sbRt = statsBoxObj.AddComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0.5f, 0.5f);
            sbRt.anchorMax = new Vector2(0.5f, 0.5f);
            sbRt.sizeDelta = new Vector2(640, 150);
            sbRt.anchoredPosition = new Vector2(0f, 10f);
            Image sbImg = statsBoxObj.GetComponent<Image>();
            if (sbImg == null) sbImg = statsBoxObj.AddComponent<Image>();
            sbImg.color = new Color(0.12f, 0.10f, 0.16f, 0.85f);

            TextMeshProUGUI ingrTmp = CreateTMPText(statsBoxObj.transform, "IngrText",
                "Ingredientes: 8 / 8", 20, FontStyles.Normal,
                new Color(0.85f, 0.95f, 0.85f, 1f),
                new Vector2(0f, 38f), new Vector2(600, 32));

            TextMeshProUGUI timeTmp = CreateTMPText(statsBoxObj.transform, "TimeText",
                "Tiempo: 00:00", 20, FontStyles.Normal,
                new Color(0.85f, 0.95f, 0.85f, 1f),
                new Vector2(0f, 2f), new Vector2(600, 32));

            TextMeshProUGUI scoreTmp = CreateTMPText(statsBoxObj.transform, "ScoreText",
                "Puntuación: 0 pts", 23, FontStyles.Bold,
                new Color(1f, 0.85f, 0.25f, 1f),
                new Vector2(0f, -38f), new Vector2(600, 34));

            // Botones de acción
            Button btnContinue = CreateButton(panelObj.transform, "Btn_Continue",
                "Continuar (Próximamente: Acto 3)",
                new Vector2(0f, -100f), new Vector2(400, 42),
                new Color(0.20f, 0.45f, 0.25f, 1f));

            Button btnRetry = CreateButton(panelObj.transform, "Btn_Retry",
                "Reintentar Nivel 2",
                new Vector2(-150f, -160f), new Vector2(250, 42),
                new Color(0.25f, 0.35f, 0.55f, 1f));

            Button btnMenu = CreateButton(panelObj.transform, "Btn_Menu",
                "Menú Principal",
                new Vector2(150f, -160f), new Vector2(250, 42),
                new Color(0.45f, 0.20f, 0.20f, 1f));

            // Configurar referencias en el componente
            screen.SetReferences(panelObj, titleTmp, subtitleTmp, ingrTmp, timeTmp, scoreTmp,
                btnContinue, btnRetry, btnMenu);

            // Ocultar panel por defecto para que no se vea al iniciar el nivel
            panelObj.SetActive(false);

            Debug.Log("[Level2FinalIntegrationBuilder] Level2Complete_Canvas y Level2CompleteScreen construidos e integrados.");
            return screen;
        }

        private static TextMeshProUGUI CreateTMPText(Transform parent, string name, string text,
            float fontSize, FontStyles style, Color color, Vector2 pos, Vector2 size)
        {
            Transform existing = parent.Find(name);
            GameObject obj = existing != null ? existing.gameObject : new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt == null) rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private static Button CreateButton(Transform parent, string name, string label,
            Vector2 pos, Vector2 size, Color bgColor)
        {
            Transform existing = parent.Find(name);
            GameObject obj = existing != null ? existing.gameObject : new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt == null) rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            Image img = obj.GetComponent<Image>();
            if (img == null) img = obj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = obj.GetComponent<Button>();
            if (btn == null) btn = obj.AddComponent<Button>();

            ColorBlock cb = btn.colors;
            cb.normalColor = bgColor;
            cb.highlightedColor = bgColor * 1.25f;
            cb.pressedColor = bgColor * 0.8f;
            btn.colors = cb;

            // Texto del botón
            CreateTMPText(obj.transform, "Text", label, 17, FontStyles.Bold,
                Color.white, Vector2.zero, size);

            return btn;
        }

        private static void SanitizeClonedHUD()
        {
            HUDController hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            if (hud != null && hud.levelCompletePanel != null)
            {
                hud.levelCompletePanel.gameObject.SetActive(false);
                Debug.Log("[Level2FinalIntegrationBuilder] LevelCompletePanel genérico clonado desactivado para dar prioridad a Level2CompleteScreen.");
            }
        }
    }
}
