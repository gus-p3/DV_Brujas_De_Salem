using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton persistente encargado del audio global (Música y Efectos de Sonido).
/// Gestiona la música de fondo en bucle, los efectos puntuales (SFX) y el estado de silencio (Mute)
/// guardado en PlayerPrefs ("AudioMuted").
/// Incluye síntesis procedural enriquecida para todos los efectos del juego (pasos, vuelo, pociones,
/// cofres, llaves, ingredientes, puertas, escaleras, alertas de aldeanos/guardias, pergaminos y portón).
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string MutePrefKey = "AudioMuted";

    [Header("Fuentes de Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips de Música")]
    public AudioClip menuMusicClip;
    public AudioClip levelMusicClip;
    public AudioClip level2MusicClip;
    public AudioClip level3MusicClip;

    [Header("Clips de SFX Base")]
    public AudioClip buttonClickClip;
    public AudioClip jumpClip;
    public AudioClip transformClip;
    public AudioClip captureClip;
    public AudioClip heartBreakClip;
    public AudioClip heartHealClip;
    public AudioClip checkpointClip;

    [Header("Clips de SFX Extendidos (UX y Ambientación)")]
    public AudioClip footstepHumanClip;
    public AudioClip footstepCatClip;
    public AudioClip landClip;
    public AudioClip broomFlyClip;
    public AudioClip ladderStepClip;
    public AudioClip potionSelectClip;
    public AudioClip potionThrowClip;
    public AudioClip potionShatterClip;
    public AudioClip chestOpenClip;
    public AudioClip chestLockedClip;
    public AudioClip keyPickupClip;
    public AudioClip ingredientPickupClip;
    public AudioClip potionPickupClip;
    public AudioClip doorOpenClip;
    public AudioClip loreReadClip;
    public AudioClip villagerNoticeClip;
    public AudioClip villagerScreamClip;
    public AudioClip guardAlertClip;
    public AudioClip frenzyAlarmClip;
    public AudioClip gateUnlockClip;
    public AudioClip errorBuzzClip;

    private bool isMuted = false;
    public bool IsMuted => isMuted;

    private float lastFootstepTime = -10f;
    private float lastLadderStepTime = -10f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // DontDestroyOnLoad solo funciona en objetos raíz (en Level3 el AudioManager cuelga de "Managers").
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
        LoadMusicFromAssetsIfNull();
#endif

        EnsureAudioSources();
        GenerateProvisionalClipsIfMissing();
        LoadMuteState();
        LoadVolumeSettings();
    }

#if UNITY_EDITOR
    private void LoadMusicFromAssetsIfNull()
    {
        if (menuMusicClip == null)
            menuMusicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/BGM_Menu.wav");
        if (levelMusicClip == null)
            levelMusicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/BGM_Level1_Forest.wav");
        if (level2MusicClip == null)
            level2MusicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/BGM_Level2_Village.wav");
        if (level3MusicClip == null)
            level3MusicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/BGM_Level3_Church.wav");
    }
#endif

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    private void EnsureAudioSources()
    {
        if (musicSource == null)
        {
            Transform mTrans = transform.Find("MusicSource");
            GameObject mObj = mTrans != null ? mTrans.gameObject : new GameObject("MusicSource");
            mObj.transform.SetParent(transform);
            musicSource = mObj.GetComponent<AudioSource>();
            if (musicSource == null) musicSource = mObj.AddComponent<AudioSource>();
        }

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = 0.45f;

        if (sfxSource == null)
        {
            Transform sTrans = transform.Find("SFXSource");
            GameObject sObj = sTrans != null ? sTrans.gameObject : new GameObject("SFXSource");
            sObj.transform.SetParent(transform);
            sfxSource = sObj.GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = sObj.AddComponent<AudioSource>();
        }

        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = 0.78f;
    }

    private void LoadMuteState()
    {
        isMuted = PlayerPrefs.GetInt(MutePrefKey, 0) == 1;
        ApplyMuteState();
    }

    public void ToggleMute()
    {
        SetMute(!isMuted);
    }

    public void SetMute(bool mute)
    {
        isMuted = mute;
        PlayerPrefs.SetInt(MutePrefKey, isMuted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMuteState();
    }

    private void ApplyMuteState()
    {
        AudioListener.volume = isMuted ? 0f : 1f;
        if (musicSource != null) musicSource.mute = isMuted;
        if (sfxSource != null) sfxSource.mute = isMuted;
    }

    private void LoadVolumeSettings()
    {
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 1.0f);
        if (musicSource != null) musicSource.volume = musicVol * 0.45f;
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
        if (sfxSource != null) sfxSource.volume = sfxVol * 0.82f;
    }

    public void PlayMusicForScene(string sceneName)
    {
        if (sceneName == "MainMenu")
        {
            PlayMusic(menuMusicClip);
        }
        else if (sceneName == "Level2")
        {
            PlayMusic(level2MusicClip != null ? level2MusicClip : levelMusicClip);
        }
        else if (sceneName.StartsWith("Level3"))
        {
            PlayMusic(level3MusicClip != null ? level3MusicClip : levelMusicClip);
        }
        else if (sceneName.StartsWith("Level") || sceneName.StartsWith("Test"))
        {
            PlayMusic(levelMusicClip);
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (sfxSource == null || clip == null || isMuted) return;
        sfxSource.PlayOneShot(clip, Mathf.Clamp(volumeScale, 0.05f, 1.5f));
    }

    // --- Métodos de conveniencia para eventos del juego ---
    public void PlayButtonClickSound() => PlaySFX(buttonClickClip, 0.85f);
    public void PlayJumpSound() => PlaySFX(jumpClip, 0.75f);
    public void PlayTransformSound() => PlaySFX(transformClip, 0.9f);
    public void PlayCaptureSound() => PlaySFX(captureClip, 1.0f);
    public void PlayHeartBreakSound() => PlaySFX(heartBreakClip, 0.95f);
    public void PlayHeartHealSound() => PlaySFX(heartHealClip, 0.95f);
    public void PlayCheckpointSound() => PlaySFX(checkpointClip, 0.9f);

    public void PlayFootstepSound(bool isCat = false)
    {
        float minInterval = isCat ? 0.22f : 0.30f;
        if (Time.unscaledTime - lastFootstepTime < minInterval) return;
        lastFootstepTime = Time.unscaledTime;
        PlaySFX(isCat ? footstepCatClip : footstepHumanClip, isCat ? 0.38f : 0.48f);
    }

    public void PlayLandSound() => PlaySFX(landClip, 0.55f);
    public void PlayBroomFlySound() => PlaySFX(broomFlyClip, 0.85f);

    public void PlayLadderStepSound()
    {
        if (Time.unscaledTime - lastLadderStepTime < 0.24f) return;
        lastLadderStepTime = Time.unscaledTime;
        PlaySFX(ladderStepClip, 0.52f);
    }

    public void PlayPotionSelectSound() => PlaySFX(potionSelectClip, 0.65f);
    public void PlayPotionThrowSound() => PlaySFX(potionThrowClip, 0.85f);
    public void PlayPotionShatterSound() => PlaySFX(potionShatterClip, 0.92f);
    public void PlayChestOpenSound() => PlaySFX(chestOpenClip, 1.0f);
    public void PlayLockedSound() => PlaySFX(chestLockedClip, 0.85f);
    public void PlayKeyPickupSound() => PlaySFX(keyPickupClip, 1.0f);
    public void PlayIngredientPickupSound() => PlaySFX(ingredientPickupClip, 0.95f);
    public void PlayPotionPickupSound() => PlaySFX(potionPickupClip, 0.95f);
    public void PlayDoorOpenSound() => PlaySFX(doorOpenClip, 0.88f);
    public void PlayLoreReadSound() => PlaySFX(loreReadClip, 0.85f);
    public void PlayVillagerNoticeSound() => PlaySFX(villagerNoticeClip, 0.85f);
    public void PlayVillagerScreamSound() => PlaySFX(villagerScreamClip, 0.95f);
    public void PlayGuardAlertSound() => PlaySFX(guardAlertClip, 0.95f);
    public void PlayFrenzyAlarmSound() => PlaySFX(frenzyAlarmClip, 1.0f);
    public void PlayGateUnlockSound() => PlaySFX(gateUnlockClip, 1.0f);
    public void PlayErrorSound() => PlaySFX(errorBuzzClip, 0.7f);

    public void SetMusicVolume(float volume)
    {
        float v = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("MusicVolume", v);
        if (musicSource != null) musicSource.volume = v * 0.35f;
    }

    public void SetSFXVolume(float volume)
    {
        float v = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("SFXVolume", v);
        if (sfxSource != null) sfxSource.volume = v;
    }

    // --- Generación Procedural de Audio de Respaldo ---
    private void GenerateProvisionalClipsIfMissing()
    {
        if (buttonClickClip == null)
            buttonClickClip = CreateToneClip("SFX_ButtonClick", 0.08f, t =>
                Mathf.Sin(2f * Mathf.PI * 660f * t) * Mathf.Exp(-t * 38f) * 0.45f +
                Mathf.Sin(2f * Mathf.PI * 1320f * t) * Mathf.Exp(-t * 55f) * 0.25f);

        if (jumpClip == null)
            jumpClip = CreateToneClip("SFX_Jump", 0.22f, t =>
            {
                float freq = Mathf.Lerp(240f, 580f, Mathf.Sqrt(t / 0.22f));
                float env = Mathf.Sin(Mathf.PI * (t / 0.22f));
                return (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.35f + PseudoNoise(t * 900f) * 0.08f) * env;
            });

        if (transformClip == null)
            transformClip = CreateToneClip("SFX_Transform", 0.34f, t =>
            {
                float f1 = Mathf.Lerp(330f, 660f, t / 0.34f);
                float f2 = Mathf.Lerp(495f, 990f, t / 0.34f);
                float env = (1f - t / 0.34f);
                return (Mathf.Sin(2f * Mathf.PI * f1 * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * f2 * t)) * 0.28f * env;
            });

        if (captureClip == null)
            captureClip = CreateToneClip("SFX_Capture", 0.55f, t =>
            {
                float freq = t < 0.22f ? 293.66f : 196.0f;
                float bell = Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * Mathf.Exp(-t * 6f) * 0.25f;
                return (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.4f + bell) * (1f - t / 0.55f);
            });

        if (heartBreakClip == null)
            heartBreakClip = CreateToneClip("SFX_HeartBreak", 0.38f, t =>
            {
                float freq = Mathf.Lerp(420f, 130f, t / 0.38f);
                float crackle = PseudoNoise(t * 2400f) * Mathf.Exp(-t * 14f) * 0.25f;
                return (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.38f + crackle) * (1f - t / 0.38f);
            });

        if (heartHealClip == null)
            heartHealClip = CreateToneClip("SFX_HeartHeal", 0.45f, t =>
            {
                float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f };
                int idx = Mathf.Clamp((int)(t / 0.11f), 0, notes.Length - 1);
                float localT = t - idx * 0.11f;
                float f = notes[idx];
                return (Mathf.Sin(2f * Mathf.PI * f * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * f * 2f * t))
                       * Mathf.Exp(-localT * 10f) * 0.35f;
            });

        if (checkpointClip == null)
            checkpointClip = CreateToneClip("SFX_Checkpoint", 0.48f, t =>
            {
                float f = t < 0.16f ? 440f : (t < 0.32f ? 554.37f : 659.25f);
                float shimmer = Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.2f;
                return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.35f + shimmer) * (1f - t / 0.48f);
            });

        // --- Nuevos sonidos detallados ---
        if (footstepHumanClip == null)
            footstepHumanClip = CreateToneClip("SFX_FootstepHuman", 0.075f, t =>
            {
                float thud = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(155f, 75f, t / 0.075f) * t) * Mathf.Exp(-t * 42f);
                float grit = PseudoNoise(t * 1800f) * Mathf.Exp(-t * 55f) * 0.35f;
                return (thud * 0.45f + grit) * 0.6f;
            });

        if (footstepCatClip == null)
            footstepCatClip = CreateToneClip("SFX_FootstepCat", 0.055f, t =>
            {
                float pad = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(260f, 140f, t / 0.055f) * t) * Mathf.Exp(-t * 58f);
                return pad * 0.35f;
            });

        if (landClip == null)
            landClip = CreateToneClip("SFX_Land", 0.14f, t =>
            {
                float f = Mathf.Lerp(140f, 52f, t / 0.14f);
                return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.5f + PseudoNoise(t * 1100f) * 0.15f) * Mathf.Exp(-t * 22f);
            });

        if (broomFlyClip == null)
            broomFlyClip = CreateToneClip("SFX_BroomFly", 0.36f, t =>
            {
                float whoosh = PseudoNoise(t * 1400f) * Mathf.Sin(Mathf.PI * (t / 0.36f)) * 0.22f;
                float magic = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(380f, 760f, t / 0.36f) * t) * (1f - t / 0.36f) * 0.28f;
                return whoosh + magic;
            });

        if (ladderStepClip == null)
            ladderStepClip = CreateToneClip("SFX_LadderStep", 0.07f, t =>
            {
                float wood = Mathf.Sin(2f * Mathf.PI * 290f * t) * Mathf.Exp(-t * 48f) * 0.4f +
                             Mathf.Sin(2f * Mathf.PI * 510f * t) * Mathf.Exp(-t * 65f) * 0.2f;
                return wood;
            });

        if (potionSelectClip == null)
            potionSelectClip = CreateToneClip("SFX_PotionSelect", 0.09f, t =>
            {
                float glass = Mathf.Sin(2f * Mathf.PI * 1180f * t) * Mathf.Exp(-t * 40f) * 0.32f +
                              Mathf.Sin(2f * Mathf.PI * 1760f * t) * Mathf.Exp(-t * 55f) * 0.18f;
                return glass;
            });

        if (potionThrowClip == null)
            potionThrowClip = CreateToneClip("SFX_PotionThrow", 0.20f, t =>
            {
                float freq = Mathf.Lerp(520f, 240f, t / 0.20f);
                float whoosh = PseudoNoise(t * 1600f) * Mathf.Sin(Mathf.PI * (t / 0.20f)) * 0.2f;
                return (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.32f + whoosh) * (1f - t / 0.20f);
            });

        if (potionShatterClip == null)
            potionShatterClip = CreateToneClip("SFX_PotionShatter", 0.35f, t =>
            {
                float glass1 = Mathf.Sin(2f * Mathf.PI * 1480f * t) * Mathf.Exp(-t * 25f) * 0.22f;
                float glass2 = Mathf.Sin(2f * Mathf.PI * 2120f * t) * Mathf.Exp(-t * 32f) * 0.18f;
                float burst = PseudoNoise(t * 3200f) * Mathf.Exp(-t * 16f) * 0.35f;
                float magic = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(620f, 310f, t / 0.35f) * t) * (1f - t / 0.35f) * 0.25f;
                return glass1 + glass2 + burst + magic;
            });

        if (chestOpenClip == null)
            chestOpenClip = CreateToneClip("SFX_ChestOpen", 0.62f, t =>
            {
                if (t < 0.14f)
                {
                    // Crujido de madera y bisagra
                    float creak = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(160f, 240f, t / 0.14f) * t) * 0.35f;
                    return creak;
                }
                // Fanfarria de tesoro (Sol4 - Si4 - Re5 - Sol5)
                float[] notes = { 392.00f, 493.88f, 587.33f, 783.99f };
                float mt = t - 0.14f;
                int idx = Mathf.Clamp((int)(mt / 0.115f), 0, notes.Length - 1);
                float f = notes[idx];
                float env = 1f - (mt / 0.48f);
                return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.38f + Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.18f) * env;
            });

        if (chestLockedClip == null)
            chestLockedClip = CreateToneClip("SFX_ChestLocked", 0.22f, t =>
            {
                float clank = (t < 0.09f || (t > 0.11f && t < 0.19f)) ? 1f : 0.15f;
                return (Mathf.Sin(2f * Mathf.PI * 195f * t) * 0.35f + PseudoNoise(t * 2100f) * 0.25f) * clank * (1f - t / 0.22f);
            });

        if (keyPickupClip == null)
            keyPickupClip = CreateToneClip("SFX_KeyPickup", 0.46f, t =>
            {
                // Tintineo metálico de llave dorada (La5 -> Mi6 -> La6)
                float[] notes = { 880f, 1318.5f, 1760f };
                int idx = Mathf.Clamp((int)(t / 0.14f), 0, notes.Length - 1);
                float f = notes[idx];
                float localT = t - idx * 0.14f;
                return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.36f + Mathf.Sin(2f * Mathf.PI * f * 2.5f * t) * 0.18f)
                       * Mathf.Exp(-localT * 9f);
            });

        if (ingredientPickupClip == null)
            ingredientPickupClip = CreateToneClip("SFX_IngredientPickup", 0.42f, t =>
            {
                // Arpegio místico herbal (Mi5 -> Sol#5 -> Si5 -> Mi6)
                float[] notes = { 659.25f, 830.61f, 987.77f, 1318.51f };
                int idx = Mathf.Clamp((int)(t / 0.10f), 0, notes.Length - 1);
                float f = notes[idx];
                return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.36f + Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.14f)
                       * (1f - t / 0.42f);
            });

        if (potionPickupClip == null)
            potionPickupClip = CreateToneClip("SFX_PotionPickup", 0.35f, t =>
            {
                float f = t < 0.12f ? Mathf.Lerp(320f, 640f, t / 0.12f) : 987.77f;
                return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.36f + Mathf.Sin(2f * Mathf.PI * f * 1.5f * t) * 0.14f)
                       * (1f - t / 0.35f);
            });

        if (doorOpenClip == null)
            doorOpenClip = CreateToneClip("SFX_DoorOpen", 0.30f, t =>
            {
                float latch = t < 0.06f ? Mathf.Sin(2f * Mathf.PI * 420f * t) * 0.4f : 0f;
                float creak = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(130f, 195f, t / 0.30f) * t) * (1f - t / 0.30f) * 0.32f;
                return latch + creak;
            });

        if (loreReadClip == null)
            loreReadClip = CreateToneClip("SFX_LoreRead", 0.26f, t =>
            {
                float rustle = PseudoNoise(t * 2600f) * Mathf.Sin(Mathf.PI * (t / 0.26f)) * 0.24f;
                float chime = Mathf.Sin(2f * Mathf.PI * 740f * t) * Mathf.Exp(-t * 10f) * 0.20f;
                return rustle + chime;
            });

        if (villagerNoticeClip == null)
            villagerNoticeClip = CreateToneClip("SFX_VillagerNotice", 0.22f, t =>
            {
                float f = t < 0.09f ? 520f : 780f;
                return Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-(t % 0.09f) * 16f) * 0.38f;
            });

        if (villagerScreamClip == null)
            villagerScreamClip = CreateToneClip("SFX_VillagerScream", 0.48f, t =>
            {
                float vibrato = Mathf.Sin(2f * Mathf.PI * 18f * t) * 35f;
                float f = Mathf.Lerp(680f, 460f, t / 0.48f) + vibrato;
                return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.36f + Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.16f)
                       * (1f - t / 0.48f);
            });

        if (guardAlertClip == null)
            guardAlertClip = CreateToneClip("SFX_GuardAlert", 0.38f, t =>
            {
                float f = t < 0.15f ? 220f : 330f;
                return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.4f + Mathf.Sin(2f * Mathf.PI * f * 1.5f * t) * 0.25f)
                       * (1f - t / 0.38f);
            });

        if (frenzyAlarmClip == null)
            frenzyAlarmClip = CreateToneClip("SFX_FrenzyAlarm", 0.75f, t =>
            {
                float bellFreq = ((int)(t / 0.18f) % 2 == 0) ? 587.33f : 440f;
                return (Mathf.Sin(2f * Mathf.PI * bellFreq * t) * 0.42f +
                        Mathf.Sin(2f * Mathf.PI * bellFreq * 2.4f * t) * 0.2f) * (1f - t / 0.75f);
            });

        if (gateUnlockClip == null)
            gateUnlockClip = CreateToneClip("SFX_GateUnlock", 0.85f, t =>
            {
                float[] chord = { 220f, 277.18f, 329.63f, 440f, 659.25f };
                float sum = 0f;
                for (int i = 0; i < chord.Length; i++)
                {
                    if (t >= i * 0.08f)
                        sum += Mathf.Sin(2f * Mathf.PI * chord[i] * t) * 0.16f;
                }
                return sum * (1f - t / 0.85f);
            });

        if (errorBuzzClip == null)
            errorBuzzClip = CreateToneClip("SFX_ErrorBuzz", 0.18f, t =>
            {
                float f = t < 0.09f ? 180f : 135f;
                return Mathf.Sin(2f * Mathf.PI * f * t) * (1f - t / 0.18f) * 0.38f;
            });

        if (menuMusicClip == null)
        {
            float[] notes = { 220f, 261.63f, 329.63f, 293.66f, 246.94f, 261.63f, 196f, 220f };
            menuMusicClip = CreateMelodyClip("BGM_Menu", notes, 0.55f, 0.16f, 110f);
        }

        if (levelMusicClip == null)
        {
            // Tema colonial nocturno del bosque (Nivel 1)
            float[] notes =
            {
                164.81f, 196.00f, 246.94f, 220.00f, 196.00f, 185.00f, 164.81f, 146.83f,
                164.81f, 220.00f, 261.63f, 246.94f, 196.00f, 220.00f, 185.00f, 164.81f
            };
            levelMusicClip = CreateMelodyClip("BGM_Level", notes, 0.45f, 0.22f, 82.41f);
        }

        if (level2MusicClip == null)
        {
            // Tema de infiltración en la aldea y mercado de Salem (Nivel 2)
            float[] notesVillage =
            {
                220.00f, 246.94f, 261.63f, 329.63f, 293.66f, 261.63f, 246.94f, 220.00f,
                196.00f, 220.00f, 246.94f, 293.66f, 261.63f, 220.00f, 207.65f, 220.00f
            };
            level2MusicClip = CreateMelodyClip("BGM_Level2", notesVillage, 0.38f, 0.22f, 110.00f);
        }

        if (level3MusicClip == null)
        {
            // Tema de la catedral y rito del caldero (Nivel 3)
            float[] notesChurch =
            {
                146.83f, 174.61f, 220.00f, 207.65f, 174.61f, 164.81f, 146.83f, 130.81f,
                146.83f, 196.00f, 220.00f, 261.63f, 220.00f, 174.61f, 155.56f, 146.83f
            };
            level3MusicClip = CreateMelodyClip("BGM_Level3", notesChurch, 0.50f, 0.24f, 73.42f);
        }
    }

    private static float PseudoNoise(float x)
    {
        return Mathf.Sin(x * 1.7f) * 0.5f + Mathf.Sin(x * 3.1f + 1.3f) * 0.3f + Mathf.Sin(x * 7.3f + 2.7f) * 0.2f;
    }

    private AudioClip CreateToneClip(string clipName, float duration, System.Func<float, float> waveFunc)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(sampleRate * duration));
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            samples[i] = Mathf.Clamp(waveFunc(t), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateMelodyClip(string clipName, float[] notes, float noteDuration, float amplitude, float droneFreq)
    {
        int sampleRate = 44100;
        float totalDuration = notes.Length * noteDuration;
        int sampleCount = Mathf.CeilToInt(sampleRate * totalDuration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            int noteIndex = Mathf.Min((int)(t / noteDuration), notes.Length - 1);
            float noteTime = t - (noteIndex * noteDuration);
            float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(noteTime / noteDuration));
            float freq = notes[noteIndex];

            float lead = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.65f +
                         Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * Mathf.Exp(-noteTime * 5f) * 0.25f;
            float bass = Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * 0.35f +
                         Mathf.Sin(2f * Mathf.PI * droneFreq * t) * 0.15f;

            samples[i] = Mathf.Clamp((lead * env + bass * 0.6f) * amplitude, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
