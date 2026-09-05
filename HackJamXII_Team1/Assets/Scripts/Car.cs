using System;
using System.Collections;
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

    [Tooltip("Segundos de espera entre cada casilla al avanzar varias de golpe.")]
    [SerializeField] private float stepCooldown = 0.2f;

    [Header("Posición en la casilla")]
    [Tooltip("A qué lado del vector (casilla actual -> siguiente casilla) se desplaza este coche.")]
    [SerializeField] private Side side = Side.Left;
    [Tooltip("Distancia en píxeles a la que se desplaza el coche respecto al centro de la casilla.")]
    [SerializeField] private float lateralOffset = 30f;

    private RectTransform rect;
    public List<RectTransform> checkpoints = new List<RectTransform>();
    private int currentCheckpoint = 0;
    private float timer = 0f;

    // Movimiento pendiente: en vez de teletransportarse a la casilla final,
    // el coche avanza de una en una con "stepCooldown" segundos entre saltos.
    private int pendingSteps = 0;
    private Coroutine moveCoroutine;

    // AudioSource propio para el sonido de motor: así cada coche puede tener
    // su propio "Engine" sonando en loop mientras se mueve, sin pisar el de
    // otro coche que se esté moviendo a la vez.
    private AudioSource engineAudioSource;

    private void Awake()
    {
        rect = (RectTransform)transform;
        checkpoints = FindCheckpoints();

        if (checkpoints.Count > 0)
        {
            rect.anchoredPosition = GetOffsetPosition(currentCheckpoint);
        }

        engineAudioSource = gameObject.AddComponent<AudioSource>();
        engineAudioSource.playOnAwake = false;
        engineAudioSource.loop = true;
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

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        pendingSteps = 0;
        StopEngineSound();
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

        // Movement car: en vez de saltar directamente a la casilla final,
        // se encola el avance y se consume de una casilla en una cada
        // "stepCooldown" segundos.
        if (squareMovement > 0)
        {
            pendingSteps += squareMovement;

            if (moveCoroutine == null)
            {
                moveCoroutine = StartCoroutine(MoveStepByStep());
            }
        }
    }

    /// <summary>
    /// Consume "pendingSteps" de una casilla en una, desplazándose
    /// físicamente (interpolando la posición) desde la casilla actual a la
    /// siguiente a lo largo de "stepCooldown" segundos, en vez de
    /// teletransportarse directamente a la casilla final.
    /// </summary>
    private IEnumerator MoveStepByStep()
    {
        StartEngineSound();

        while (pendingSteps > 0 && checkpoints.Count > 0)
        {
            pendingSteps--;

            int nextCheckpoint = (currentCheckpoint + 1) % checkpoints.Count;
            Vector2 startPos = rect.anchoredPosition;
            Vector2 endPos = GetOffsetPosition(nextCheckpoint);

            float elapsed = 0f;
            while (elapsed < stepCooldown)
            {
                elapsed += Time.deltaTime;
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, Mathf.Clamp01(elapsed / stepCooldown));
                yield return null;
            }

            rect.anchoredPosition = endPos;
            currentCheckpoint = nextCheckpoint;
            checkpointsReached++;
        }

        StopEngineSound();
        moveCoroutine = null;
    }

    /// <summary>
    /// Arranca (o retoma) el sonido de motor en loop para este coche. Si ya
    /// estaba sonando (p. ej. porque llegaron más "pendingSteps" mientras el
    /// coche seguía en marcha) no hace nada.
    /// </summary>
    private void StartEngineSound()
    {
        if (engineAudioSource == null || SoundManager.Instance == null) return;
        if (engineAudioSource.isPlaying) return;

        AudioClip clip = SoundManager.Instance.GetSFXClip(SFX.Engine);
        if (clip == null) return;

        engineAudioSource.clip = clip;
        engineAudioSource.volume = SoundManager.Instance.SfxVolume;
        engineAudioSource.Play();
    }

    private void StopEngineSound()
    {
        if (engineAudioSource != null)
        {
            engineAudioSource.Stop();
        }
    }
}
