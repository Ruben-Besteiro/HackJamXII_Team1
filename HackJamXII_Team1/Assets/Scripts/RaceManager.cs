using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    public static RaceManager Instance { get; private set; }

    [Header("Cuenta atrás")]
    [SerializeField] private float countdownDuration = 3f;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private GameObject countdownCanvas;

    private float countdownTimeRemaining;

    // Se pone a "true" en cuanto termina la cuenta atrás. Mientras sea
    // "false" la carrera no avanza (ni el tiempo general ni las cartas).
    public bool RaceStarted { get; private set; }

    [Header("Coches")]
    [SerializeField] private Car car1;
    [SerializeField] private Car car2;
    private Car leader;

    /// <summary>
    /// Número del coche ganador (1 o 2). Se fija una única vez, en el
    /// momento exacto en que termina la carrera (ver "RefreshGeneralTimeBar"),
    /// comparando "checkpointsReached" de cada coche. Debe calcularse ahí y
    /// no más tarde: "car1"/"car2" viven en la escena "Sample" y se destruyen
    /// en cuanto esta se descarga, así que para cuando "ResultsManager" (ya
    /// en la escena "Results") pregunta por el ganador, esas referencias ya
    /// no serían válidas.
    /// </summary>
    public int WinnerCarNumber { get; private set; } = 1;

    [Header("Tiempo")]
    [SerializeField] private Image generalTimeBar;
    public float generalTimer = 180f;

    // Valor de "generalTimer" al empezar la carrera, usado como referencia
    // para calcular qué fracción de la barra hay que rellenar.
    private float initialGeneralTimer;

    // Los tiempos para que el jugador decida qué opción usar para su carta
    public float maxTimeForCards;
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

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        timeUntilNextCardTest = Random.Range(1f, 3f);

        initialGeneralTimer = generalTimer;
        RefreshGeneralTimeBar();

        countdownTimeRemaining = countdownDuration;
        RaceStarted = false;
        UpdateCountdownText();
    }

    void Update()
    {
        if (!RaceStarted)
        {
            UpdateCountdown();
            return;
        }

        UpdateGeneralTimer();

        UpdateLapCounter(car1, ref lastCheckpointsReached1, ref laps1, lapCounterText1);
        UpdateLapCounter(car2, ref lastCheckpointsReached2, ref laps2, lapCounterText2);

        UpdateLeader();

        //UpdateCardTestHelper();
    }

    /// <summary>
    /// Cuenta atrás previa a la carrera: resta "Time.deltaTime" a
    /// "countdownTimeRemaining" y refresca "countdownText" con los segundos
    /// que quedan (sin decimales). Al llegar a 0 oculta "countdownCanvas" y
    /// marca "RaceStarted" para que la partida empiece de verdad.
    /// </summary>
    private void UpdateCountdown()
    {
        countdownTimeRemaining = Mathf.Max(0f, countdownTimeRemaining - Time.deltaTime);
        UpdateCountdownText();

        if (countdownTimeRemaining <= 0f)
        {
            RaceStarted = true;

            if (countdownCanvas != null)
            {
                countdownCanvas.SetActive(false);
            }
        }
    }

    private void UpdateCountdownText()
    {
        if (countdownText == null) return;

        countdownText.text = Mathf.CeilToInt(countdownTimeRemaining).ToString();
    }

    /// <summary>
    /// Mientras quede tiempo, resta "Time.deltaTime" a "generalTimer" y
    /// refresca "generalTimeBar" con el nuevo valor, fotograma a fotograma
    /// para que la barra baje de forma continua en vez de "a saltitos".
    /// </summary>
    private void UpdateGeneralTimer()
    {
        if (generalTimer <= 0) return;

        generalTimer = Mathf.Max(0f, generalTimer - Time.deltaTime);
        RefreshGeneralTimeBar();
    }

    /// <summary>
    /// Actualiza "generalTimeBar" para que su "fillAmount" refleje qué
    /// fracción del tiempo total de partida queda en "generalTimer"
    /// (1 = tiempo completo, 0 = se ha agotado).
    /// </summary>
    private void RefreshGeneralTimeBar()
    {
        if (generalTimeBar == null || initialGeneralTimer <= 0) return;

        generalTimeBar.fillAmount = generalTimer / initialGeneralTimer;

        if (generalTimeBar.fillAmount <= 0f)
        {
            // Hay que fijar el ganador aquí, antes de cambiar de escena:
            // "car1"/"car2" pertenecen a "Sample" y se destruyen en cuanto
            // se descarga, así que ya no se podrían consultar desde "Results".
            WinnerCarNumber = (car1 != null && car2 != null && car2.checkpointsReached > car1.checkpointsReached) ? 2 : 1;
            GameSceneManager.Instance.LoadScene("Results", SceneTransition.FadeBlack);
        }
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


    /*/// <summary>
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
    }*/
}
