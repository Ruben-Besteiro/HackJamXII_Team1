using System.Collections;
using UnityEngine;

// Escena de resultados: Anuncia al ganador
public class ResultsManager : MonoBehaviour
{
    [SerializeField] private float delayBeforeReveal = 1;

    [Header("Cámara")]
    [SerializeField] private Transform resultsCamera;
    [SerializeField] private float cameraMoveSpeed = 2;

    [SerializeField] private Transform car1;
    [SerializeField] private Transform car2;

    [Header("Botón volver")]
    [SerializeField] private GameObject backButton;
    [SerializeField] private float backButtonDelay = 5f;

    private void Start()
    {
        StartCoroutine(ShowResults());
        StartCoroutine(ShowBackButton());
    }

    private IEnumerator ShowBackButton()
    {
        if (backButton == null) yield break;

        backButton.SetActive(false);
        yield return new WaitForSeconds(backButtonDelay);
        backButton.SetActive(true);
    }

    private IEnumerator ShowResults()
    {
        SoundManager.Instance.StopMusic();
        yield return new WaitForSeconds(delayBeforeReveal);

        SoundManager.Instance.PlaySFX(SFX.Results);
        Transform winner = GetWinner();
        if (resultsCamera == null || winner == null) yield break;

        yield return MoveCameraToWinner(winner.position.x);
    }

    private Transform GetWinner()
    {
        if (RaceManager.Instance == null) return car1;

        return RaceManager.Instance.WinnerCarNumber == 2 ? car2 : car1;
    }

    private IEnumerator MoveCameraToWinner(float targetX)
    {
        Vector3 startPos = resultsCamera.position;
        Vector3 endPos = new Vector3(targetX, startPos.y, startPos.z);

        float distance = Mathf.Abs(endPos.x - startPos.x);
        float duration = cameraMoveSpeed > 0f ? distance / cameraMoveSpeed : 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            resultsCamera.position = Vector3.Lerp(startPos, endPos, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        resultsCamera.position = endPos;
    }
}
