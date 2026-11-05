using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Level3
{
    /// <summary>
    /// Tarjeta de capítulo a pantalla completa del Nivel 3 (fundido a negro con título).
    /// - Parte 1: "Llegaste a la cabaña. Descansa…" al entrar a la Cabaña de la Bruja, y el cierre
    ///   "Fin de la Parte 1" antes de cargar la escena de la Aldea.
    /// - Parte 2: presentación "Parte 2 — La Aldea de Salem" al iniciar la escena Level3_Village.
    /// Todo se anima con tiempo no escalado, así que funciona con Time.timeScale = 0.
    /// </summary>
    [DisallowMultipleComponent]
    public class Level3ChapterCard : MonoBehaviour
    {
        private static Level3ChapterCard _instance;

        public static Level3ChapterCard Instance
        {
            get
            {
                if (_instance == null) _instance = FindFirstObjectByType<Level3ChapterCard>(FindObjectsInactive.Include);
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Presentación al iniciar la escena")]
        [SerializeField] private bool playIntroOnStart = false;
        [SerializeField] private string introKicker = "NIVEL 3  ·  PARTE 2";
        [SerializeField] private string introTitle = "La Aldea de Salem";
        [TextArea(2, 4)]
        [SerializeField] private string introSubtitle = "";
        [SerializeField] private float introHold = 2.6f;

        [Header("Tiempos")]
        [SerializeField] private float fadeDuration = 0.9f;
        [SerializeField] private float restHold = 2.2f;
        [SerializeField] private float outroHold = 2.4f;

        private CanvasGroup group;
        private TextMeshProUGUI kickerText;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI subtitleText;
        private Coroutine running;

        public bool IsPlaying => running != null;

        public void Configure(bool introOnStart, string kicker, string title, string subtitle)
        {
            playIntroOnStart = introOnStart;
            introKicker = kicker;
            introTitle = title;
            introSubtitle = subtitle;
        }

        private void Awake()
        {
            Instance = this;
            EnsureUIHierarchy();
            group.alpha = playIntroOnStart ? 1f : 0f;
            if (playIntroOnStart) SetTexts(introKicker, introTitle, introSubtitle);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Start()
        {
            if (playIntroOnStart) Play(IntroRoutine());
        }

        /// <summary>Fundido a negro, efecto de descanso (onDark), pausa y regreso al juego (onDone).</summary>
        public void PlayRest(string kicker, string title, string subtitle, Action onDark, Action onDone)
        {
            SetTexts(kicker, title, subtitle);
            Play(RestRoutine(onDark, onDone));
        }

        /// <summary>Fundido a negro con el cierre de la parte y carga de la escena siguiente.</summary>
        public void PlayOutroAndLoad(string kicker, string title, string subtitle, string sceneName, Action beforeLoad)
        {
            SetTexts(kicker, title, subtitle);
            Play(OutroRoutine(sceneName, beforeLoad));
        }

        private void Play(IEnumerator routine)
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(routine);
        }

        private IEnumerator IntroRoutine()
        {
            group.alpha = 1f;
            yield return WaitUnscaled(introHold);
            yield return Fade(1f, 0f, fadeDuration * 1.3f);
            running = null;
        }

        private IEnumerator RestRoutine(Action onDark, Action onDone)
        {
            Time.timeScale = 0f;
            yield return Fade(group.alpha, 1f, fadeDuration);
            onDark?.Invoke();
            yield return WaitUnscaled(restHold);
            yield return Fade(1f, 0f, fadeDuration);

            // Si el jugador abrió la pausa mientras tanto, la pausa decide la escala de tiempo
            if (GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing)
            {
                Time.timeScale = 1f;
            }

            running = null;
            onDone?.Invoke();
        }

        private IEnumerator OutroRoutine(string sceneName, Action beforeLoad)
        {
            Time.timeScale = 0f;
            yield return Fade(group.alpha, 1f, fadeDuration);
            yield return WaitUnscaled(outroHold);

            if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[Level3ChapterCard] La escena '{sceneName}' no está en Build Settings.");
                yield return Fade(1f, 0f, fadeDuration);
                Time.timeScale = 1f;
                running = null;
                yield break;
            }

            beforeLoad?.Invoke();
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / Mathf.Max(0.01f, duration)));
                yield return null;
            }
            group.alpha = to;
        }

        private static IEnumerator WaitUnscaled(float seconds)
        {
            float end = Time.unscaledTime + seconds;
            while (Time.unscaledTime < end) yield return null;
        }

        private void SetTexts(string kicker, string title, string subtitle)
        {
            EnsureUIHierarchy();
            kickerText.text = kicker ?? string.Empty;
            titleText.text = title ?? string.Empty;
            subtitleText.text = subtitle ?? string.Empty;
        }

        /// <summary>Crea (o recupera) el Canvas de la tarjeta. Lo usa también el generador de escena.</summary>
        public void EnsureUIHierarchy()
        {
            if (group != null && kickerText != null && titleText != null && subtitleText != null) return;

            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 450;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            RectTransform backdrop = Child(transform, "Backdrop");
            Stretch(backdrop, Vector2.zero, Vector2.one);
            Image bg = backdrop.GetComponent<Image>();
            if (bg == null) bg = backdrop.gameObject.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.02f, 0.06f, 1f);
            bg.raycastTarget = false;

            RectTransform line = Child(transform, "GoldLine");
            Stretch(line, new Vector2(0.36f, 0.475f), new Vector2(0.64f, 0.479f));
            Image lineImg = line.GetComponent<Image>();
            if (lineImg == null) lineImg = line.gameObject.AddComponent<Image>();
            lineImg.color = new Color(0.83f, 0.69f, 0.22f, 0.85f);
            lineImg.raycastTarget = false;

            kickerText = Text("Kicker", new Vector2(0.1f, 0.60f), new Vector2(0.9f, 0.68f), 34f, FontStyles.Bold, new Color(0.83f, 0.69f, 0.22f, 1f));
            titleText = Text("Title", new Vector2(0.06f, 0.49f), new Vector2(0.94f, 0.61f), 78f, FontStyles.Bold, new Color(1f, 0.93f, 0.74f, 1f));
            subtitleText = Text("Subtitle", new Vector2(0.18f, 0.30f), new Vector2(0.82f, 0.46f), 32f, FontStyles.Italic, new Color(0.86f, 0.80f, 0.92f, 1f));
        }

        private TextMeshProUGUI Text(string name, Vector2 min, Vector2 max, float size, FontStyles style, Color color)
        {
            RectTransform rt = Child(transform, name);
            Stretch(rt, min, max);
            TextMeshProUGUI tmp = rt.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static RectTransform Child(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t == null)
            {
                GameObject go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                t = go.transform;
            }
            return (RectTransform)t;
        }

        private static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
