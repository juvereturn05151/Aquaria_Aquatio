/*
SoundManager owns prototype-level audio playback for music, looping ambience,
and one-shot sound effects. It does not decide when gameplay sounds should
occur; gameplay systems should call it when their state changes.
*/

using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioSource loopAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("UI")]
    [SerializeField] private AudioClip buttonClickSound;

    [Header("Exploration")]
    [SerializeField] private AudioClip signalSound;
    [SerializeField] private AudioClip encounterAvailableSound;

    [Header("Encounter")]
    [SerializeField] private AudioClip creatureFoundSound;
    [SerializeField] private AudioClip moveCloserSound;
    [SerializeField] private AudioClip encounterSuccessSound;

    [Header("Music")]
    [SerializeField] private AudioClip explorationMusic;
    [SerializeField] private AudioClip encounterMusic;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = Mathf.Clamp01(value);
            ApplyVolumeSettings();
        }
    }

    public float MusicVolume
    {
        get => musicVolume;
        set
        {
            musicVolume = Mathf.Clamp01(value);
            ApplyVolumeSettings();
        }
    }

    public float SFXVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            ApplyVolumeSettings();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ConfigureAudioSources();
        ApplyVolumeSettings();
    }

    private void OnValidate()
    {
        masterVolume = Mathf.Clamp01(masterVolume);
        musicVolume = Mathf.Clamp01(musicVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);
        ConfigureAudioSources();
        ApplyVolumeSettings();
    }

    public void PlaySFX(AudioClip clip)
    {
        PlaySFX(clip, 1f);
    }

    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (clip == null || sfxAudioSource == null)
        {
            return;
        }

        sfxAudioSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(
            clip,
            position,
            Mathf.Clamp01(volume) * masterVolume * sfxVolume
        );
    }

    public void PlayLoop(AudioClip clip)
    {
        if (clip == null || loopAudioSource == null)
        {
            return;
        }

        if (loopAudioSource.clip == clip && loopAudioSource.isPlaying)
        {
            return;
        }

        loopAudioSource.clip = clip;
        loopAudioSource.loop = true;
        loopAudioSource.Play();
    }

    public void StopLoop()
    {
        if (loopAudioSource != null)
        {
            loopAudioSource.Stop();
            loopAudioSource.clip = null;
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicAudioSource == null)
        {
            return;
        }

        if (musicAudioSource.clip == clip && musicAudioSource.isPlaying)
        {
            return;
        }

        musicAudioSource.clip = clip;
        musicAudioSource.loop = true;
        musicAudioSource.Play();
    }

    public void StopMusic()
    {
        if (musicAudioSource != null)
        {
            musicAudioSource.Stop();
            musicAudioSource.clip = null;
        }
    }

    public void PlayButtonClick()
    {
        PlaySFX(buttonClickSound);
    }

    public void PlayExplorationSignal()
    {
        PlaySFX(signalSound);
    }

    public void PlayEncounterAvailable()
    {
        PlaySFX(encounterAvailableSound);
    }

    public void PlayCreatureFound()
    {
        PlaySFX(creatureFoundSound);
    }

    public void PlayMoveCloser()
    {
        PlaySFX(moveCloserSound);
    }

    public void PlayEncounterSuccess()
    {
        PlaySFX(encounterSuccessSound);
    }

    public void PlayExplorationMusic()
    {
        PlayMusic(explorationMusic);
    }

    public void PlayEncounterMusic()
    {
        PlayMusic(encounterMusic);
    }

    private void ConfigureAudioSources()
    {
        ConfigureAudioSource(musicAudioSource, true);
        ConfigureAudioSource(loopAudioSource, true);
        ConfigureAudioSource(sfxAudioSource, false);
    }

    private void ConfigureAudioSource(AudioSource source, bool loop)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
    }

    private void ApplyVolumeSettings()
    {
        if (musicAudioSource != null)
        {
            musicAudioSource.volume = masterVolume * musicVolume;
        }

        if (loopAudioSource != null)
        {
            loopAudioSource.volume = masterVolume * sfxVolume;
        }

        if (sfxAudioSource != null)
        {
            sfxAudioSource.volume = masterVolume * sfxVolume;
        }
    }
}
