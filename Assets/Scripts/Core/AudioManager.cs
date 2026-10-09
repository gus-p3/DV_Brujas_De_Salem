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

    private void Start()
    {
        float musicVol = PlayerPrefs.GetFloat(MusicVolumeParam, 0.75f);
        float sfxVol = PlayerPrefs.GetFloat(SFXVolumeParam, 0.75f);
        SetMusicVolume(musicVol);
        SetSFXVolume(sfxVol);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;
        if (musicSource.clip == clip) return;
        
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void SetMusicVolume(float volume01)
    {
        PlayerPrefs.SetFloat(MusicVolumeParam, volume01);
        float db = volume01 > 0.0001f ? Mathf.Log10(volume01) * 20f : -80f;
        if (audioMixer != null) audioMixer.SetFloat(MusicVolumeParam, db);
    }

    public void SetSFXVolume(float volume01)
    {
        PlayerPrefs.SetFloat(SFXVolumeParam, volume01);
        float db = volume01 > 0.0001f ? Mathf.Log10(volume01) * 20f : -80f;
        if (audioMixer != null) audioMixer.SetFloat(SFXVolumeParam, db);
    }
}
