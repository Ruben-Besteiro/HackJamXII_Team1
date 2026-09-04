using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Mueve un icono de UI (uno de los "Square") saltando de casilla en casilla
/// a lo largo del circuito, formado por todos los GameObjects cuyo nombre
/// empieza por "Checkpoint". Cada "timeBetweenCheckpoints" milisegundos, el coche se
/// teletransporta a la siguiente casilla de la lista.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class Car : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Tiempo en milisegundos que espera el coche antes de saltar a la siguiente casilla.")]
    [SerializeField] private float timeBetweenCheckpoints;

    private RectTransform rect;
    private List<RectTransform> checkpoints = new List<RectTransform>();
    private int currentCheckpoint = 0;
    private float timer = 0f;

    private void Awake()
    {
        rect = (RectTransform)transform;
        checkpoints = FindCheckpoints();

        if (checkpoints.Count > 0)
        {
            rect.anchoredPosition = checkpoints[currentCheckpoint].anchoredPosition;
        }
    }

    private void Update()
    {
        if (checkpoints.Count == 0) return;

        timer += Time.deltaTime * 1000f; // deltaTime está en segundos, timeBetweenCheckpoints en ms
        if (timer >= timeBetweenCheckpoints)
        {
            timer = 0f;
            currentCheckpoint = (currentCheckpoint + 1) % checkpoints.Count;
            rect.anchoredPosition = checkpoints[currentCheckpoint].anchoredPosition;
        }
    }

    /// <summary>
    /// Busca en la escena todos los GameObjects "Checkpoint..." y los devuelve
    /// ordenados según el número que contenga su nombre (Checkpoint 1, Checkpoint 2...).
    /// </summary>
    private static List<RectTransform> FindCheckpoints()
    {
        return Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(t => t.name.StartsWith("Checkpoint"))
            .OrderBy(t => ExtractCheckpointNumber(t.name))
            .ToList();
    }

    private static int ExtractCheckpointNumber(string checkpointName)
    {
        Match match = Regex.Match(checkpointName, @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }
}
