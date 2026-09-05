using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [SerializeField] private List<MusicEntry> musicEntries;
    [SerializeField] private List<SFXEntry> sfxEntries;

    [Header("Volumen")]
    [Range(0, 1)][SerializeField] private float musicVolume = 0.5f;
    [Range(0, 1)][SerializeField] private float sfxVolume = 1;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private Dictionary<Music, AudioClip> musicDict;
    private Dictionary<SFX, AudioClip> sfxDict;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.GetComponents<AudioSource>()[0];
        sfxSource = gameObject.GetComponents<AudioSource>()[1];
        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;

        musicDict = new Dictionary<Music, AudioClip>();
        foreach (var entry in musicEntries)
            musicDict[entry.track] = entry.clip;

        sfxDict = new Dictionary<SFX, AudioClip>();
        foreach (var entry in sfxEntries)
            sfxDict[entry.sfx] = entry.clip;
    }

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

    private void PlayMusicForScene(string sceneName)
    {
        switch (sceneName)
        {
            case "MainMenu":
                PlayMusic(Music.MainMenu);
                break;
            case "SampleScene":
                PlayMusic(Music.SampleScene);
                break;
        }
    }

    public void PlayMusic(Music track)
    {
        if (!musicDict.TryGetValue(track, out AudioClip clip) || clip == null) return;
        if (clip == musicSource.clip && musicSource.isPlaying) return;
        StopMusic();
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void StopMusic() => musicSource.Stop();
    public void PauseMusic() => musicSource.Pause();
    public void ResumeMusic() => musicSource.UnPause();

    public void PlaySFX(SFX sfx)
    {
        if (sfxDict.TryGetValue(sfx, out AudioClip clip) && clip != null)
            sfxSource.PlayOneShot(clip);
    }
}

public enum Music
{
    MainMenu,
    SampleScene
}

public enum SFX
{
    Yes,
    No,
    TalkPlayer,
    TalkNpc1,
    Footsteps,
    Pause
}

[Serializable]
public struct MusicEntry
{
    public Music track;
    public AudioClip clip;
}

[Serializable]
public struct SFXEntry
{
    public SFX sfx;
    public AudioClip clip;
}
