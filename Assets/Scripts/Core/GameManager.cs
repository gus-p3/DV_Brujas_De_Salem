using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
    Paused,
    LevelComplete,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }
    public event Action<GameState> OnStateChanged;

    [Header("Player Stats")]
    [SerializeField] private int maxHearts = 3;
    private int currentHearts = 3;

    public int MaxHearts => maxHearts;
    public int CurrentHearts => currentHearts;

    public int Lives
    {
        get => currentHearts;
        set => currentHearts = Mathf.Clamp(value, 0, maxHearts);
    }
    public int Score = 0;
    public float TimeElapsed = 0f;

    [Header("Spell Charges")]
    public int BlindCharges = 0;
    public int HypnotizeCharges = 0;
    public int SleepCharges = 0;

    // Eventos para el HUD y el sistema de corazones
    public event Action OnStatsUpdated;
    public event Action<int, int> OnHeartsChanged; // (actual, maximo)
    public event Action OnHeartLost;
    public event Action OnHeartGained;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
        if (scene.name == "MainMenu" || scene.buildIndex == 0)
        {
            SetState(GameState.Playing);
            if (AudioManager.Instance != null && AudioManager.Instance.menuMusicClip != null)
            {
                AudioManager.Instance.PlayMusic(AudioManager.Instance.menuMusicClip);
            }
        }
        else
        {
            StartLevel();
        }
    }

    private void Start()
    {
        string activeScene = SceneManager.GetActiveScene().name;
        if (activeScene != "MainMenu" && SceneManager.GetActiveScene().buildIndex != 0)
        {
            StartLevel();
        }
    }

    private void Update()
    {
        if (CurrentState == GameState.Playing)
        {
            TimeElapsed += Time.deltaTime;
            
            // Alternar pausa con Escape (ignorado si un cuadro de tutorial está abierto)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                PauseGame();
            }
        }
        else if (CurrentState == GameState.Paused)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ResumeGame();
            }
        }
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;
        
        switch (CurrentState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                break;
            case GameState.Paused:
            case GameState.LevelComplete:
            case GameState.GameOver:
                Time.timeScale = 0f;
                break;
        }

        OnStateChanged?.Invoke(CurrentState);
    }

    public void AddScore(int amount)
    {
        Score += amount;
        OnStatsUpdated?.Invoke();
    }

    private float levelStartRealTime = -10f;

    public bool LoseHeart(int amount = 1, string source = "")
    {
        if (CurrentState != GameState.Playing) return false;

        // Protección inicial al cargar/iniciar el nivel para evitar daño espurio en el arranque
        if (Time.unscaledTime - levelStartRealTime < 1.0f && source != "Test")
        {
            return false;
        }

        PlayerInvulnerability inv = UnityEngine.Object.FindFirstObjectByType<PlayerInvulnerability>();
        if (inv != null && inv.IsInvulnerable)
        {
            Debug.Log($"[HealthManager] Lesionar ignorado por invulnerabilidad activa (Fuente: {source}).");
            return false;
        }

        currentHearts = Mathf.Max(0, currentHearts - amount);
        Debug.Log($"[HealthManager] Lesionar aplicado ({source}). Corazones restantes: {currentHearts}/{maxHearts}");

        OnHeartsChanged?.Invoke(currentHearts, maxHearts);
        OnHeartLost?.Invoke();
        OnStatsUpdated?.Invoke();

        if (currentHearts <= 0)
        {
            if (inv != null) inv.CancelInvulnerability();
            TriggerGameOver();
        }
        else if (inv != null)
        {
            inv.TriggerInvulnerability();
        }

        return true;
    }

    /// <summary>Alias de recibir daño para compatibilidad con el sistema de salud.</summary>
    public bool Lesionar(int amount = 1, string source = "Daño") => LoseHeart(amount, source);

    public bool GainHeart(int amount = 1)
    {
        if (currentHearts >= maxHearts) return false;

        currentHearts = Mathf.Min(maxHearts, currentHearts + amount);
        Debug.Log($"[GameManager] Corazón recuperado. Corazones actuales: {currentHearts}/{maxHearts}");

        OnHeartsChanged?.Invoke(currentHearts, maxHearts);
        OnHeartGained?.Invoke();
        OnStatsUpdated?.Invoke();
        return true;
    }

    public void TakeDamage(int amount = 1)
    {
        LoseHeart(amount, "Daño");
    }

    public void AddSpellCharges(int blind, int hypnotize, int sleep)
    {
        BlindCharges += blind;
        HypnotizeCharges += hypnotize;
        SleepCharges += sleep;
        OnStatsUpdated?.Invoke();

        // Si la ranura seleccionada actualmente no tiene cargas, seleccionar automáticamente la poción recién recogida
        PotionInventory inv = UnityEngine.Object.FindFirstObjectByType<PotionInventory>();
        if (inv != null)
        {
            int sel = (int)inv.SelectedPotion;
            int currentSelCharges = sel == 0 ? BlindCharges : (sel == 1 ? HypnotizeCharges : SleepCharges);
            if (currentSelCharges <= 0)
            {
                if (blind > 0) inv.SelectPotion(PotionType.Blind);
                else if (hypnotize > 0) inv.SelectPotion(PotionType.Hypnotize);
                else if (sleep > 0) inv.SelectPotion(PotionType.Sleep);
            }
        }
    }

    public bool TryConsumeSpell(int spellType)
    {
        // 0: Cegar, 1: Hipnotizar, 2: Dormir
        bool success = false;
        if (spellType == 0 && BlindCharges > 0) { BlindCharges--; success = true; }
        else if (spellType == 1 && HypnotizeCharges > 0) { HypnotizeCharges--; success = true; }
        else if (spellType == 2 && SleepCharges > 0) { SleepCharges--; success = true; }

        if (success) OnStatsUpdated?.Invoke();
        return success;
    }

    public void StartLevel()
    {
        levelStartRealTime = Time.unscaledTime;
        currentHearts = maxHearts;
        Score = 0;
        TimeElapsed = 0f;
        SetState(GameState.Playing);
        if (FrenzyController.Instance != null)
        {
            FrenzyController.Instance.StopFrenzy();
        }
        OnHeartsChanged?.Invoke(currentHearts, maxHearts);
        OnStatsUpdated?.Invoke();

        if (AudioManager.Instance != null && AudioManager.Instance.levelMusicClip != null)
        {
            AudioManager.Instance.PlayMusic(AudioManager.Instance.levelMusicClip);
        }
    }

    public void PauseGame()
    {
        // Ignorar si un tutorial contextual está desplegado
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsOpen)
        {
            return;
        }

        if (CurrentState == GameState.Playing)
            SetState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused)
            SetState(GameState.Playing);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        int activeIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(activeIndex);
    }

    public void CompleteLevel()
    {
        if (CurrentState != GameState.Playing) return;

        SaveSystem.SaveLevelProgress(SceneManager.GetActiveScene().buildIndex, Score);
        SetState(GameState.LevelComplete);
    }

    public void TriggerGameOver()
    {
        if (CurrentState != GameState.Playing) return;
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCaptureSound();
        }

        SetState(GameState.GameOver);

        GameOverPanel goPanel = UnityEngine.Object.FindFirstObjectByType<GameOverPanel>(FindObjectsInactive.Include);
        if (goPanel != null)
        {
            goPanel.gameObject.SetActive(true);
            goPanel.HandleStateChanged(GameState.GameOver);
        }
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SetState(GameState.Playing);
        if (AudioManager.Instance != null && AudioManager.Instance.menuMusicClip != null)
        {
            AudioManager.Instance.PlayMusic(AudioManager.Instance.menuMusicClip);
        }
        SceneManager.LoadScene(0);
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        int nextScene = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextScene < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextScene);
            StartLevel();
        }
        else
        {
            LoadMainMenu();
        }
    }
}
