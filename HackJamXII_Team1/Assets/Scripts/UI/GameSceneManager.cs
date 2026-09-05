using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance;

    [Header("UI")]
    [SerializeField] private Image fadePanel;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.5f;

    private string mainScene;
    private string secondaryScene;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        fadePanel.gameObject.SetActive(false);
        fadePanel.color = new Color(0, 0, 0, 0);
    }

    /* SCENE LOAD */

    public void LoadScene(string sceneName, SceneTransition transition)
    {
        //SoundManager_OLD.Instance.StopMusic();

        if (transition == SceneTransition.Instant)
        {
            SceneManager.LoadScene(sceneName);
            mainScene = sceneName;
            return;
        }

        StartCoroutine(LoadSceneFade(sceneName));
    }

    public IEnumerator LoadSceneFade(string sceneName)
    {
        yield return FadeIn();

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
            yield return null;

        op.allowSceneActivation = true;
        yield return null;

        mainScene = sceneName;
        yield return FadeOut();
    }

    /* SECONDARY SCENE */

    public void LoadSecondaryScene(string sceneName, SceneTransition transition)
    {
        if (!string.IsNullOrEmpty(secondaryScene))
            return;

        StartCoroutine(LoadSecondary(sceneName, transition));
    }

    private IEnumerator LoadSecondary(string sceneName, SceneTransition transition)
    {
        if (transition == SceneTransition.FadeBlack)
            yield return FadeIn();

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!op.isDone)
            yield return null;

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        secondaryScene = sceneName;

        if (transition == SceneTransition.FadeBlack)
            yield return FadeOut();
    }

    public void UnloadSecondaryScene(SceneTransition transition)
    {
        if (string.IsNullOrEmpty(secondaryScene))
            return;

        StartCoroutine(UnloadSecondary(transition));
    }

    private IEnumerator UnloadSecondary(SceneTransition transition)
    {
        if (transition == SceneTransition.FadeBlack)
            yield return FadeIn();

        AsyncOperation op = SceneManager.UnloadSceneAsync(secondaryScene);
        while (!op.isDone)
            yield return null;

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(mainScene));
        secondaryScene = null;

        if (transition == SceneTransition.FadeBlack)
            yield return FadeOut();
    }

    /* UI TWEENS (corutinas propias, sin DOTween) */

    private IEnumerator FadeIn()
    {
        if (fadePanel == null) yield break;

        fadePanel.gameObject.SetActive(true);
        yield return FadeTo(1f);
    }

    private IEnumerator FadeOut()
    {
        if (fadePanel == null) yield break;

        yield return FadeTo(0f);
        if (fadePanel != null) fadePanel.gameObject.SetActive(false);
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (fadePanel == null) yield break;

        Color color = fadePanel.color;
        float startAlpha = color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            // El panel puede destruirse a mitad de fade si pertenece a una escena
            // que se descarga (p. ej. no está bajo el propio GameSceneManager).
            if (fadePanel == null) yield break;

            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / fadeDuration));
            fadePanel.color = color;
            yield return null;
        }

        if (fadePanel == null) yield break;

        color.a = targetAlpha;
        fadePanel.color = color;
    }
}

public enum SceneTransition
{
    Instant,
    FadeBlack
}
