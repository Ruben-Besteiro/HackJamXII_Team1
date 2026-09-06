using System;
using System.Collections;
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
    private Coroutine musicFadeCoroutine;

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
            case "Menu":
                PlayMusic(Music.Menu);
                break;
            case "Sample":
                PlayMusic(Music.Game);
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

    // Fundidos de música pensados para sincronizarse con el fundido a negro
    // del GameSceneManager durante los cambios de escena.
    public void FadeMusicOut(float duration) => FadeMusicTo(0f, duration);
    public void FadeMusicIn(float duration) => FadeMusicTo(musicVolume, duration);

    private void FadeMusicTo(float targetVolume, float duration)
    {
        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);

        musicFadeCoroutine = StartCoroutine(FadeMusicRoutine(targetVolume, duration));
    }

    private IEnumerator FadeMusicRoutine(float targetVolume, float duration)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    public void PlaySFX(SFX sfx)
    {
        if (sfxDict.TryGetValue(sfx, out AudioClip clip) && clip != null)
            sfxSource.PlayOneShot(clip);
    }

    // Volumen actual de los efectos de sonido, para que quien reproduzca un
    // SFX por su cuenta (p. ej. un AudioSource en loop en cada coche) pueda
    // mantenerse en línea con el volumen general configurado aquí.
    public float SfxVolume => sfxVolume;

    public AudioClip GetSFXClip(SFX sfx)
    {
        sfxDict.TryGetValue(sfx, out AudioClip clip);
        return clip;
    }
}

public enum Music
{
    Menu,
    Game,
}

public enum SFX
{
    Yes,
    Woosh,
    Engine,
    Results
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
