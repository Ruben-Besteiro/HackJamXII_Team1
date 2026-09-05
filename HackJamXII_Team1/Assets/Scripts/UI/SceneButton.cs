using UnityEngine;

public class SceneButton : MonoBehaviour
{
    [SerializeField] private SceneTransition transitionType;
    [SerializeField] private SFX sfx;

    public void LoadScene(string LevelName)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(sfx);

        GameSceneManager.Instance.LoadScene(LevelName, transitionType);
    }
}
