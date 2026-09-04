using TMPro;
using UnityEngine;

/// <summary>
/// Controla el estado general de la carrera. De momento, se encarga de
/// actualizar los textos "Lap Counter 1" y "Lap Counter 2" con el número
/// de vueltas completadas por el Car 1 y el Car 2 respectivamente.
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

    [Header("Textos de vueltas (UI)")]
    [SerializeField] private TextMeshProUGUI lapCounterText1;
    [SerializeField] private TextMeshProUGUI lapCounterText2;

    private int laps1 = 0;
    private int laps2 = 0;

    // Último "checkpointsReached" visto de cada coche, para no contar la
    // misma vuelta varias veces mientras el valor no cambia.
    private int lastCheckpointsReached1 = 0;
    private int lastCheckpointsReached2 = 0;

    void Update()
    {
        UpdateLapCounter(car1, ref lastCheckpointsReached1, ref laps1, lapCounterText1);
        UpdateLapCounter(car2, ref lastCheckpointsReached2, ref laps2, lapCounterText2);
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
}
