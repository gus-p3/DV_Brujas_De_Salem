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
    public int Lives = 3;
    public int Score = 0;
    public float TimeElapsed = 0f;

    [Header("Spell Charges")]
    public int BlindCharges = 0;
    public int HypnotizeCharges = 0;
    public int SleepCharges = 0;

    // Eventos para el HUD
    public event Action OnStatsUpdated;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (CurrentState == GameState.Playing)
        {
            TimeElapsed += Time.deltaTime;
            
            // Alternar pausa con Escape
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

    public void StartLevel()
    {
        Lives = 3;
        Score = 0;
        TimeElapsed = 0f;
        SetState(GameState.Playing);
        OnStatsUpdated?.Invoke();
    }

    public void PauseGame()
    {
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        StartLevel();
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
        
        // Asumiendo muerte instantánea por hoguera según rúbrica "La hoguera te alcanzó..."
        SetState(GameState.GameOver);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SetState(GameState.Playing);
        SceneManager.LoadScene("MainMenu");
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
