using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Object = UnityEngine.Object;

public class Car : MonoBehaviour
{
    [Header("Cards References")]
    [SerializeField] private CardManagement cardManager;

    [Header("Stats")]
    [SerializeField] public int checkpointsReached = 0;        // Lo lejos que hemos llegado
    [SerializeField] public int tires = 1;       // El desgaste de los neumáticos
    [SerializeField] public int fuel = 1;        // El combustible que queda
    [SerializeField] public int chasis = 1;        // Estado del coche

    [Tooltip("Segundos de espera entre cada casilla al avanzar varias de golpe.")]
    [SerializeField] private float stepCooldown = 0.2f;

    [Header("Posición en la casilla")]
    [Tooltip("Distancia (con signo) a la que se desplaza este coche respecto al centro de la casilla, perpendicular al vector casilla actual -> siguiente casilla. El signo decide el lado: p. ej. 1 para un coche y -1 para el otro los coloca a cada lado del circuito.")]
    [SerializeField] private float lateralOffset = 1f;
    [Tooltip("Normal del plano en el que se apoya el circuito, en LOCAL respecto a cada checkpoint (se transforma con la rotación real del checkpoint, así que sigue la inclinación del Canvas World Space aunque no sea el plano XY de mundo puro). Por defecto Vector3.forward, el eje 'hacia la cámara' del checkpoint sin rotar.")]
    [SerializeField] private Vector3 trackPlaneNormal = Vector3.forward;

    private Transform carTransform;
    public List<Transform> checkpoints = new List<Transform>();
    private int currentCheckpoint = 0;

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
        carTransform = transform;
        checkpoints = FindCheckpoints();

        if (checkpoints.Count > 0)
        {
            carTransform.position = GetOffsetPosition(currentCheckpoint);
            FaceDirection(GetDirectionToNext(currentCheckpoint), currentCheckpoint);
        }

        engineAudioSource = gameObject.AddComponent<AudioSource>();
        engineAudioSource.playOnAwake = false;
        engineAudioSource.loop = true;
    }

    /// <summary>
    /// Dirección normalizada desde la casilla "index" hacia la siguiente
    /// casilla del circuito. Devuelve Vector3.zero si no se puede calcular
    /// (menos de 2 checkpoints, o casillas coincidentes).
    /// </summary>
    private Vector3 GetDirectionToNext(int index)
    {
        if (checkpoints.Count < 2) return Vector3.zero;

        Vector3 current = checkpoints[index].position;
        Vector3 next = checkpoints[(index + 1) % checkpoints.Count].position;

        return (next - current).normalized;
    }

    /// <summary>
    /// Normal del plano del circuito en la casilla "index", ya en espacio de
    /// mundo. "trackPlaneNormal" se define en LOCAL respecto al checkpoint
    /// (no es un vector de mundo fijo): el Canvas World Space de los
    /// checkpoints puede estar inclinado (p. ej. para dar una vista en
    /// perspectiva), así que hay que rotarlo con la orientación real de cada
    /// checkpoint en vez de asumir siempre Vector3.forward "de mundo". Usar
    /// el vector fijo aquí era lo que hacía que los coches acabasen panza
    /// arriba: su "arriba" no coincidía con la normal real, inclinada, del
    /// circuito.
    /// </summary>
    private Vector3 GetPlaneNormal(int index)
    {
        return checkpoints[index].TransformDirection(trackPlaneNormal);
    }

    /// <summary>
    /// Calcula la posición de la casilla "index" desplazada lateralmente según
    /// la dirección hacia la siguiente casilla del circuito y "lateralOffset".
    /// </summary>
    private Vector3 GetOffsetPosition(int index)
    {
        Vector3 current = checkpoints[index].position;
        Vector3 direction = GetDirectionToNext(index);

        if (direction == Vector3.zero) return current;

        // Perpendicular al vector dirección, sobre el plano del circuito
        // (definido por "trackPlaneNormal"). El signo de "lateralOffset"
        // decide a qué lado del circuito se desplaza este coche.
        Vector3 perpendicular = Vector3.Cross(GetPlaneNormal(index), direction);

        return current + perpendicular * lateralOffset;
    }

    /// <summary>
    /// Orienta el coche para que mire hacia "direction", con la normal del
    /// plano del circuito en "index" como eje "arriba". No hace nada si la
    /// dirección es Vector3.zero (p. ej. un único checkpoint).
    /// </summary>
    private void FaceDirection(Vector3 direction, int index)
    {
        if (direction == Vector3.zero) return;

        carTransform.rotation = Quaternion.LookRotation(direction, GetPlaneNormal(index));
    }

    // Coincide con "Checkpoint", "Checkpoint 1" (el modelo 3D duplica el
    // checkpoint base con un número) y con el sufijo "(N)" que añade Unity
    // al duplicar ("Checkpoint 1 (20)"...), pero no con el objeto contenedor
    // "Checkpoints" (que también empieza por "Checkpoint" y por eso se
    // colaba en la lista, provocando que el coche saltase a su posición
    // -el centro del circuito- al pasar del último checkpoint al primero).
    private static readonly Regex CheckpointNameRegex = new Regex(@"^Checkpoint(?: \d+)?( ?\(\d+\))?$");

    // El número que decide el ORDEN del circuito es el que va entre
    // paréntesis (el sufijo de duplicado de Unity); el que pueda ir pegado
    // a "Checkpoint" (p. ej. el "1" de "Checkpoint 1") no cuenta para el
    // orden, así que se busca específicamente al final del nombre.
    private static readonly Regex CheckpointOrderRegex = new Regex(@"\((\d+)\)$");

    /// <summary>
    /// Busca en la escena todos los GameObjects "Checkpoint..." y los devuelve
    /// ordenados según el número que contenga su nombre (Checkpoint 1, Checkpoint 2...).
    /// </summary>
    private static List<Transform> FindCheckpoints()
    {
        return Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(t => CheckpointNameRegex.IsMatch(t.name))
            .OrderBy(t => ExtractCheckpointNumber(t.name))
            .ToList();
    }

    private static int ExtractCheckpointNumber(string checkpointName)
    {
        Match match = CheckpointOrderRegex.Match(checkpointName);
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
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
            Vector3 startPos = carTransform.position;
            Vector3 endPos = GetOffsetPosition(nextCheckpoint);

            Quaternion startRot = carTransform.rotation;
            Vector3 travelDirection = GetDirectionToNext(currentCheckpoint);
            Quaternion endRot = travelDirection != Vector3.zero
                ? Quaternion.LookRotation(travelDirection, GetPlaneNormal(currentCheckpoint))
                : startRot;

            float elapsed = 0f;
            while (elapsed < stepCooldown)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / stepCooldown);
                carTransform.position = Vector3.Lerp(startPos, endPos, t);
                carTransform.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }

            carTransform.position = endPos;
            carTransform.rotation = endRot;
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
