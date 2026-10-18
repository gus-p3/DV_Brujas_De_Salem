using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    private const string MusicVolumeParam = "MusicVolume";
    private const string SFXVolumeParam = "SFXVolume";

    [Header("Default Audio Clips (Provisionales generados si están vacíos)")]
    public AudioClip menuMusicClip;
    public AudioClip levelMusicClip;
    public AudioClip buttonClickClip;
    public AudioClip jumpClip;
    public AudioClip transformClip;
    public AudioClip captureClip;
    public AudioClip heartBreakClip;
    public AudioClip heartHealClip;
    public AudioClip checkpointClip;

    private Coroutine musicFadeCoroutine;
    private float targetMusicVolume = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            EnsureAudioSources();
            GenerateProvisionalClipsIfMissing();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        float musicVol = PlayerPrefs.GetFloat(MusicVolumeParam, 1.0f);
        float sfxVol = PlayerPrefs.GetFloat(SFXVolumeParam, 0.85f);
        SetMusicVolume(Mathf.Max(musicVol, 0.95f));
        SetSFXVolume(sfxVol);
    }

    private void EnsureAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }

    // ------------------------------------------------------------- Música con Fundido

    public void PlayMusic(AudioClip clip, float fadeDuration = 0.5f)
    {
        if (clip == null) return;
        if (musicSource == null) EnsureAudioSources();

        if (musicSource.clip == clip && musicSource.isPlaying) return;

        if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(FadeMusicRoutine(clip, fadeDuration));
    }

    private IEnumerator FadeMusicRoutine(AudioClip newClip, float duration)
    {
        float startVol = musicSource.volume;
        float elapsed = 0f;

        // Fade out
        if (musicSource.isPlaying && duration > 0f)
        {
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / (duration * 0.5f));
                yield return null;
            }
        }

        musicSource.volume = 0f;
        musicSource.clip = newClip;
        musicSource.loop = true;
        musicSource.Play();

        // Fade in
        elapsed = 0f;
        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetMusicVolume, elapsed / (duration * 0.5f));
            yield return null;
        }

        musicSource.volume = targetMusicVolume;
    }

    // ------------------------------------------------------------- Efectos de Sonido (SFX)

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        if (sfxSource == null) EnsureAudioSources();
        sfxSource.PlayOneShot(clip);
    }

    public void PlayButtonClickSound()
    {
        PlaySFX(buttonClickClip);
    }

    public void PlayJumpSound()
    {
        PlaySFX(jumpClip);
    }

    public void PlayTransformSound()
    {
        PlaySFX(transformClip);
    }

    public void PlayCaptureSound()
    {
        PlaySFX(captureClip);
    }

    // ------------------------------------------------------------- Volumen y Mezclador

    public void SetMusicVolume(float volume01)
    {
        targetMusicVolume = volume01;
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.volume = volume01;
        }
        else if (musicSource != null)
        {
            musicSource.volume = volume01;
        }

        PlayerPrefs.SetFloat(MusicVolumeParam, volume01);
        float db = volume01 > 0.0001f ? Mathf.Log10(volume01) * 20f : -80f;
        if (audioMixer != null) audioMixer.SetFloat(MusicVolumeParam, db);
    }

    public void SetSFXVolume(float volume01)
    {
        if (sfxSource != null) sfxSource.volume = volume01;
        PlayerPrefs.SetFloat(SFXVolumeParam, volume01);
        float db = volume01 > 0.0001f ? Mathf.Log10(volume01) * 20f : -80f;
        if (audioMixer != null) audioMixer.SetFloat(SFXVolumeParam, db);
    }

    // ------------------------------------------------------------- Sonidos Provisionales por Código

    private void GenerateProvisionalClipsIfMissing()
    {
        if (buttonClickClip == null)
            buttonClickClip = GenerateToneClip("Provisional_Click", 0.06f, 880f, 440f);

        if (jumpClip == null)
            jumpClip = GenerateToneClip("Provisional_Jump", 0.16f, 320f, 640f);

        if (transformClip == null)
            transformClip = GenerateToneClip("Provisional_Transform", 0.25f, 400f, 950f);

        if (captureClip == null)
            captureClip = GenerateToneClip("Provisional_Capture", 0.45f, 220f, 110f);

        if (menuMusicClip == null)
            menuMusicClip = GenerateToneClip("Provisional_MenuMusic", 2f, 330f, 330f, true);

        if (levelMusicClip == null)
            levelMusicClip = GenerateLevelMelodyClip("Provisional_LevelMusic");

        if (heartBreakClip == null)
            heartBreakClip = GenerateToneClip("Provisional_HeartBreak", 0.25f, 520f, 160f);

        if (heartHealClip == null)
            heartHealClip = GenerateToneClip("Provisional_HeartHeal", 0.3f, 440f, 880f);

        if (checkpointClip == null)
            checkpointClip = GenerateToneClip("Provisional_Checkpoint", 0.35f, 523f, 659f);
    }

    private AudioClip GenerateLevelMelodyClip(string clipName)
    {
        // Melodía misteriosa y temática de Salem (D menor / arpegio místico en loop)
        int sampleRate = 44100;
        float[] notes = new float[] { 293.66f, 349.23f, 440.00f, 523.25f, 440.00f, 349.23f, 392.00f, 329.63f }; // D4, F4, A4, C5, A4, F4, G4, E4
        float noteDuration = 0.5f;
        float duration = notes.Length * noteDuration; // 4.0 segundos de bucle
        int totalSamples = (int)(sampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            int noteIndex = (int)(t / noteDuration) % notes.Length;
            float noteTime = t % noteDuration;
            float freq = notes[noteIndex];

            // Tono principal suave con armónicos cálidos tipo campana / xilófono mágico
            float env = Mathf.Sin((noteTime / noteDuration) * Mathf.PI);
            float baseTone = Mathf.Sin(2f * Mathf.PI * freq * t);
            float harmonic = Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.4f;
            float subTone = Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * 0.25f;

            // Volumen global incrementado para que se escuche claro y envolvente
            samples[i] = (baseTone + harmonic + subTone) * env * 0.55f;
        }

        AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateToneClip(string clipName, float duration, float startFreq, float endFreq, bool isAmbient = false)
    {
        int sampleRate = 44100;
        int samplesCount = (int)(sampleRate * duration);
        float[] samples = new float[samplesCount];

        for (int i = 0; i < samplesCount; i++)
        {
            float t = (float)i / samplesCount;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            float time = (float)i / sampleRate;
            
            // Envelope
            float envelope = isAmbient 
                ? Mathf.Sin(t * Mathf.PI) * 0.45f 
                : Mathf.Pow(1f - t, 2f) * 0.45f;

            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * time) * envelope;
        }

        AudioClip clip = AudioClip.Create(clipName, samplesCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
