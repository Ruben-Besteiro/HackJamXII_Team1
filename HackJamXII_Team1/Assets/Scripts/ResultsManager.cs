using System.Collections;
using UnityEngine;

/// <summary>
/// Controla la escena "Results". Espera medio segundo, consulta al
/// "RaceManager" (que sobrevive de la escena "Sample" gracias a su
/// "DontDestroyOnLoad") quién ha ganado la carrera y desplaza la cámara
/// en el eje X, con un lerp, hasta dejarla alineada con el "Cylinder" del
/// coche ganador -Car 1 corresponde a "Cylinder 1" y Car 2 a "Cylinder 2"-,
/// de forma que su eje azul (Z, hacia delante) coincida con el del ganador.
/// </summary>
public class ResultsManager : MonoBehaviour
{
    [Header("Tiempo de espera antes de mover la cámara")]
    [SerializeField] private float delayBeforeReveal = 1;

    [Header("Cámara")]
    [SerializeField] private Transform resultsCamera;
    [SerializeField] private float cameraMoveSpeed = 2;

    [Header("Podios (Cylinder 1 = Car 1, Cylinder 2 = Car 2)")]
    [SerializeField] private Transform cylinder1;
    [SerializeField] private Transform cylinder2;

    private void Start()
    {
        StartCoroutine(ShowResults());
    }

    private IEnumerator ShowResults()
    {
        SoundManager.Instance.StopMusic();
        yield return new WaitForSeconds(delayBeforeReveal);

        SoundManager.Instance.PlaySFX(SFX.Results);
        Transform winnerCylinder = GetWinnerCylinder();
        if (resultsCamera == null || winnerCylinder == null) yield break;

        yield return MoveCameraTo(winnerCylinder.position.x);
    }

    /// <summary>
    /// Car 1 -> Cylinder 1, Car 2 -> Cylinder 2. Si no hay RaceManager
    /// (por ejemplo, si se abre esta escena suelta para probarla) se
    /// usa Cylinder 1 por defecto.
    /// </summary>
    private Transform GetWinnerCylinder()
    {
        if (RaceManager.Instance == null) return cylinder1;

        return RaceManager.Instance.WinnerCarNumber == 2 ? cylinder2 : cylinder1;
    }

    /// <summary>
    /// Desplaza "resultsCamera" a izquierda o derecha (según toque) con un
    /// lerp en el eje X hasta "targetX" -la X del Cylinder ganador-,
    /// manteniendo su altura y profundidad, para que su eje azul quede
    /// alineado con el del ganador.
    /// </summary>
    private IEnumerator MoveCameraTo(float targetX)
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
