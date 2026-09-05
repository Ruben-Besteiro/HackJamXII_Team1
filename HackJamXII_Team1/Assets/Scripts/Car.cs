using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Mueve un icono de UI (uno de los "Square") saltando de casilla en casilla
/// a lo largo del circuito, formado por todos los GameObjects cuyo nombre
/// empieza por "Checkpoint". Cada "timeBetweenCheckpoints" milisegundos, el coche se
/// teletransporta a la siguiente casilla de la lista, desplazado lateralmente
/// (izquierda o derecha) respecto al vector que va de la casilla actual a la siguiente,
/// para que varios coches en la misma casilla no queden superpuestos.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class Car : MonoBehaviour
{
    private enum Side { Left, Right }

    [Header("Cards References")] 
    [SerializeField] private CardManagement cardManager;

    [Header("Stats")]
    [SerializeField] public int checkpointsReached = 0;        // Lo lejos que hemos llegado
    [SerializeField] public float timeBetweenCheckpoints;      // La "velocidad"
    [SerializeField] public int tires = 1;       // El desgaste de los neumáticos
    [SerializeField] public int fuel = 1;        // El combustible que queda
    [SerializeField] public int chasis = 1;        // Estado del coche

    [Header("Posición en la casilla")]
    [Tooltip("A qué lado del vector (casilla actual -> siguiente casilla) se desplaza este coche.")]
    [SerializeField] private Side side = Side.Left;
    [Tooltip("Distancia en píxeles a la que se desplaza el coche respecto al centro de la casilla.")]
    [SerializeField] private float lateralOffset = 30f;

    private RectTransform rect;
    public List<RectTransform> checkpoints = new List<RectTransform>();
    private int currentCheckpoint = 0;
    private float timer = 0f;

    private void Awake()
    {
        rect = (RectTransform)transform;
        checkpoints = FindCheckpoints();

        if (checkpoints.Count > 0)
        {
            rect.anchoredPosition = GetOffsetPosition(currentCheckpoint);
        }
    }

    // TODO: Esta función no se hace en Update, se hace a cada llamada de Next Card, suscribir a evento
    private void Update()
    {
        return;
        if (checkpoints.Count == 0) return;

        timer += Time.deltaTime * 1000f; // deltaTime está en segundos, timeBetweenCheckpoints en ms
        if (timer >= timeBetweenCheckpoints)
        {
            timer = 0f;
            currentCheckpoint = (currentCheckpoint + 1) % checkpoints.Count;
            checkpointsReached++;
            rect.anchoredPosition = GetOffsetPosition(currentCheckpoint);
        }
    }

    /// <summary>
    /// Calcula la posición de la casilla "index" desplazada lateralmente según
    /// la dirección hacia la siguiente casilla del circuito y el lado configurado.
    /// </summary>
    private Vector2 GetOffsetPosition(int index)
    {
        Vector2 current = checkpoints[index].anchoredPosition;

        if (checkpoints.Count < 2) return current;

        Vector2 next = checkpoints[(index + 1) % checkpoints.Count].anchoredPosition;
        Vector2 direction = (next - current).normalized;

        if (direction == Vector2.zero) return current;

        // Perpendicular a la izquierda del vector dirección; a la derecha es la opuesta.
        Vector2 left = new Vector2(-direction.y, direction.x);
        Vector2 perpendicular = side == Side.Left ? left : -left;

        return current + perpendicular * lateralOffset;
    }

    // Coincide con "Checkpoint" o "Checkpoint (N)", pero no con el objeto
    // contenedor "Checkpoints" (que también empieza por "Checkpoint" y por
    // eso se colaba en la lista, provocando que el coche saltase a su
    // posición -el centro del circuito- al pasar del último checkpoint al primero).
    private static readonly Regex CheckpointNameRegex = new Regex(@"^Checkpoint( ?\(\d+\))?$");

    /// <summary>
    /// Busca en la escena todos los GameObjects "Checkpoint..." y los devuelve
    /// ordenados según el número que contenga su nombre (Checkpoint 1, Checkpoint 2...).
    /// </summary>
    private static List<RectTransform> FindCheckpoints()
    {
        return Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(t => CheckpointNameRegex.IsMatch(t.name))
            .OrderBy(t => ExtractCheckpointNumber(t.name))
            .ToList();
    }

    private static int ExtractCheckpointNumber(string checkpointName)
    {
        Match match = Regex.Match(checkpointName, @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }

    private void OnEnable()
    {
        cardManager.OnCardChanged += UpdateCarStatus;
    }

    private void OnDisable()
    {
        cardManager.OnCardChanged -= UpdateCarStatus;
    }

    // Update car status depends on the values 
    private void UpdateCarStatus(int currentGas, int currentTires, int currentChasis, bool endedTimer)
    {
        fuel = currentGas;
        tires = currentTires;
        chasis = currentChasis;
        
        int squareMovement = (tires == 0 || chasis == 0) ? 0 : fuel;

        if (endedTimer)
            return;
        // Movement car
        currentCheckpoint = (currentCheckpoint + squareMovement) % checkpoints.Count;
        checkpointsReached += squareMovement;
        rect.anchoredPosition = GetOffsetPosition(currentCheckpoint);

        if (currentGas == 0)
            fuel--;
    }
}
