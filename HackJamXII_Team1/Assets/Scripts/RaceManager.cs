using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla el estado general de la carrera. De momento, se encarga de:
/// - Actualizar los textos "Lap Counter 1" y "Lap Counter 2" con el número
///   de vueltas completadas por el Car 1 y el Car 2 respectivamente.
/// - Llevar en "leader" el coche que más casillas ha recorrido, y poner en
///   negrita el lap counter de ese coche.
///
/// Para saber si un coche ha completado una vuelta miramos su
/// "checkpointsReached" (cuántas casillas ha recorrido en total, sin dar
/// la vuelta): si es distinto de 0 y es múltiplo del número de checkpoints
/// del circuito, es que acaba de dar una vuelta completa.
/// </summary>
public class RaceManager : MonoBehaviour
{
    [Header("Coches")]
    [SerializeField] private Car car1;
    [SerializeField] private Car car2;
    [SerializeField] private Car leader;


    // Los tiempos para que el jugador decida qué opción usar para su carta
    public float maxTimeForCards = 5;
    public float timeRemainingForCard1;
    public float timeRemainingForCard2;

    [Header("Textos de vueltas (UI)")]
    [SerializeField] private TextMeshProUGUI lapCounterText1;
    [SerializeField] private TextMeshProUGUI lapCounterText2;

    [Header("Debug: sistema de cartas")]
    [SerializeField] private TextMeshPro debugText1;
    [SerializeField] private TextMeshPro debugText2;
    [SerializeField] private Image realTimeBar1;
    [SerializeField] private Image realTimeBar2;

    private int laps1 = 0;
    private int laps2 = 0;

    // Último "checkpointsReached" visto de cada coche, para no contar la
    // misma vuelta varias veces mientras el valor no cambia.
    private int lastCheckpointsReached1 = 0;
    private int lastCheckpointsReached2 = 0;

    // Cuenta atrás hasta la próxima carta de prueba (entre 1 y 3 segundos).
    private float timeUntilNextCardTest;

    void Start()
    {
        timeUntilNextCardTest = Random.Range(1f, 3f);
    }

    void Update()
    {
        UpdateLapCounter(car1, ref lastCheckpointsReached1, ref laps1, lapCounterText1);
        UpdateLapCounter(car2, ref lastCheckpointsReached2, ref laps2, lapCounterText2);

        UpdateLeader();

        UpdateCardTestHelper();
    }

    /// <summary>
    /// Comprueba si "car" acaba de completar una vuelta nueva y, si es así,
    /// incrementa "laps" y refresca "lapCounterText" con el valor actual.
    /// </summary>
    private void UpdateLapCounter(Car car, ref int lastCheckpointsReached, ref int laps, TextMeshProUGUI lapCounterText)
    {
        if (car == null || lapCounterText == null) return;

        int checkpointsReached = car.checkpointsReached;
        int checkpointCount = car.checkpoints.Count;

        if (checkpointCount > 0 && checkpointsReached != lastCheckpointsReached)
        {
            lastCheckpointsReached = checkpointsReached;

            if (checkpointsReached != 0 && checkpointsReached % checkpointCount == 0)
            {
                laps++;
            }
        }

        lapCounterText.text = laps.ToString();
    }

    /// <summary>
    /// Actualiza "leader" con el coche que más casillas ha recorrido
    /// (mayor "checkpointsReached") y pone su lap counter en negrita,
    /// dejando el del otro coche en estilo normal.
    /// </summary>
    private void UpdateLeader()
    {
        if (car1 == null || car2 == null) return;

        if (car1.checkpointsReached > car2.checkpointsReached)
        {
            leader = car1;
        }
        else if (car2.checkpointsReached > car1.checkpointsReached)
        {
            leader = car2;
        }
        // En caso de empate se mantiene el líder anterior.

        if (lapCounterText1 != null)
        {
            lapCounterText1.fontStyle = leader == car1 ? FontStyles.Bold : FontStyles.Normal;
        }

        if (lapCounterText2 != null)
        {
            lapCounterText2.fontStyle = leader == car2 ? FontStyles.Bold : FontStyles.Normal;
        }
    }


    /// <summary>
    /// Helper de prueba para el sistema de cartas: cada cierto tiempo aleatorio
    /// (entre 1 y 3 segundos) "reparte" una carta a Car 1 o Car 2 -a modo de
    /// prueba, sin lógica real de reparto todavía-, mostrando "Prueba" en su
    /// texto de debug y copiando el "maxTimeForCards" actual a su
    /// "timeRemainingForCard" correspondiente. Mientras tanto, "maxTimeForCards"
    /// se va reduciendo un 1% cada segundo, y las barras "Real Time Bar" se
    /// rellenan según cuánto le queda a cada carta respecto a ese máximo.
    /// </summary>
    private void UpdateCardTestHelper()
    {
        maxTimeForCards *= Mathf.Pow(0.985f, Time.deltaTime);

        // Cuenta atrás de cada carta ya repartida, para que la barra se vaya
        // vaciando en vez de quedarse llena para siempre.
        timeRemainingForCard1 = Mathf.Max(0f, timeRemainingForCard1 - Time.deltaTime);
        timeRemainingForCard2 = Mathf.Max(0f, timeRemainingForCard2 - Time.deltaTime);

        timeUntilNextCardTest -= Time.deltaTime;
        if (timeUntilNextCardTest <= 0f)
        {
            timeUntilNextCardTest = Random.Range(1f, 3f);

            if (Random.value < 0.5f)
            {
                TriggerCardTest(debugText1, ref timeRemainingForCard1);
            }
            else
            {
                TriggerCardTest(debugText2, ref timeRemainingForCard2);
            }
        }

        UpdateRealTimeBar(realTimeBar1, timeRemainingForCard1);
        UpdateRealTimeBar(realTimeBar2, timeRemainingForCard2);
    }

    private void TriggerCardTest(TextMeshPro debugText, ref float timeRemainingForCard)
    {
        if (debugText != null)
        {
            debugText.text = "Prueba";
        }

        timeRemainingForCard = maxTimeForCards;
    }

    private void UpdateRealTimeBar(Image realTimeBar, float timeRemainingForCard)
    {
        if (realTimeBar == null || maxTimeForCards <= 0f) return;

        realTimeBar.fillAmount = Mathf.Clamp01(timeRemainingForCard / maxTimeForCards);
    }
}
