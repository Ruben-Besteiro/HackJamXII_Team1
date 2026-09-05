using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardManagement : MonoBehaviour
{
    public event Action<int, int, int, bool> OnCardChanged;

    public int GasValue = 3;
    public int TireValue = 3;
    public int ChasisValue = 3;
    
    private int indexCards = 0;
    
    [SerializeField] private SO_CardContainer CardsReference;

    [Header("Card Structure Config")] 
    [SerializeField] private TextMeshProUGUI textDescription;
    [SerializeField] private Image textImage;
    [SerializeField] private TextMeshProUGUI textTitle;
    [SerializeField] private TextMeshProUGUI textRightChoice;
    [SerializeField] private TextMeshProUGUI textLeftChoice;
    
    [Header("Values Config")]
    [SerializeField] private RectTransform parentGasValues;
    private List<Image> gasValuesArray;
    [SerializeField] private RectTransform parentTiresValues;
    private List<Image> tiresValuesArray;
    [SerializeField] private RectTransform parentChasisValues;
    private List<Image> chasisValuesArray;
    [SerializeField] private Color activateValuesColor; 
    [SerializeField] private Color deactivateValuesColor; 

    [Header("Time Card References")]
    [SerializeField] private Image imageTimeFill;

    // Canvas raíz de esta carta: se desactiva mientras dure la cuenta atrás
    // inicial para que la carta no se vea (ni sea interactuable) hasta que
    // la partida empiece de verdad.
    [SerializeField] private Canvas cardCanvas;

    // Se usa si no hay un RaceManager en la escena (por ejemplo, probando
    // este canvas de forma aislada).
    [SerializeField] private float fallbackMaxTime = 10f;

    // La TimeBar nunca dura menos que esto, para que siempre sea jugable.
    [SerializeField] private float minTimeBarDuration = 2f;

    // Punto de referencia con el que calibramos la duración de la TimeBar:
    // cuando el timer general marca "referenceGeneralTimer" segundos, la
    // TimeBar dura "referenceMaxTime" segundos (p. ej. 3:00 -> 15s).
    [SerializeField] private float referenceGeneralTimer = 180f;
    [SerializeField] private float referenceMaxTime = 15f;

    // Segundos de timer general que quedan cuando la TimeBar alcanza ya su
    // mínimo ("minTimeBarDuration"). Entre este punto y el de referencia de
    // arriba, la duración de la TimeBar baja en línea recta.
    [SerializeField] private float floorTriggerGeneralTimer = 8f;

    private float maxTime;
    private float currentTime = 0f;

    

    private void Start()
    {
        // Cada jugador necesita su propio mazo: si dos CardManagement
        // comparten el mismo SO_CardContainer (mismo asset), barajar uno
        // reordenaría también el mazo del otro. Clonamos el contenedor en
        // runtime para que el shuffle y el consumo de cartas de este
        // jugador no afecten al asset original ni a otras instancias.
        if (CardsReference != null)
            CardsReference = Instantiate(CardsReference);

        PrepareUI();
        CardsShuffle();
        UpdateUI();
        SetUpCard();
        // Time card control
        ResetTimer();

        // Si todavía estamos en la cuenta atrás, la carta no debe aparecer
        // hasta que el RaceManager marque el inicio de la partida.
        if (cardCanvas != null && RaceManager.Instance != null && !RaceManager.Instance.RaceStarted)
        {
            cardCanvas.enabled = false;
        }
    }

    private void SetUpCard()
    {
        if (CardsReference.cards.Length <= 0)
            return;

        textDescription.text = CardsReference.cards[indexCards].cardDescripton;
        textImage.sprite = CardsReference.cards[indexCards].cardImage;
        textTitle.text = CardsReference.cards[indexCards].cardTitle;
        textRightChoice.text = CardsReference.cards[indexCards].cardRightChoiceText;
        textLeftChoice.text = CardsReference.cards[indexCards].cardLeftChoiceText;
    }

    public void SetChoice(bool _isRight)
    {
        if (_isRight)
        {
            GasValue += CardsReference.cards[indexCards].cardGasValueRight;
            if (CardsReference.cards[indexCards].cardGasValueRight == 0)
                GasValue--;
            TireValue += CardsReference.cards[indexCards].cardTireValueRight;
            ChasisValue += CardsReference.cards[indexCards].cardChasisValueRight;
        }
        else
        {
            GasValue += CardsReference.cards[indexCards].cardGasValueLeft;
            if (CardsReference.cards[indexCards].cardGasValueLeft == 0)
                GasValue--;
            TireValue += CardsReference.cards[indexCards].cardTireValueLeft;
            ChasisValue += CardsReference.cards[indexCards].cardChasisValueLeft;
        }

        GasValue = Mathf.Clamp(GasValue, 0, 5);
        TireValue = Mathf.Clamp(TireValue, 0, 5);
        ChasisValue = Mathf.Clamp(ChasisValue, 0, 5);

        UpdateUI();

        NextCard();
    }

    private void PrepareUI()
    {
        gasValuesArray = new List<Image>();
        tiresValuesArray = new List<Image>();
        chasisValuesArray = new List<Image>();
        
        foreach (Transform value in parentGasValues.transform)
            gasValuesArray.Add(value.GetComponent<Image>());
        
        foreach (Transform value in parentTiresValues.transform)
            tiresValuesArray.Add(value.GetComponent<Image>());
        
        foreach (Transform value in parentChasisValues.transform)
            chasisValuesArray.Add(value.GetComponent<Image>());
    }

    private void UpdateUI()
    {
        int maxGas = gasValuesArray.Count;

        for (int i = 0; i < maxGas; i++)
        {
            bool isDeactivated = i < (maxGas - GasValue);
            gasValuesArray[i].color = isDeactivated ? deactivateValuesColor : activateValuesColor;
        }

        int maxTires = tiresValuesArray.Count;
        for (int i = 0; i < maxTires; i++)
        {
            bool isDeactivated = i < (maxTires - TireValue);
            tiresValuesArray[i].color = isDeactivated ? deactivateValuesColor : activateValuesColor;
        }

        int maxChasis = chasisValuesArray.Count;
        for (int i = 0; i < maxChasis; i++)
        {
            bool isDeactivated = i < (maxChasis - ChasisValue);
            chasisValuesArray[i].color = isDeactivated ? deactivateValuesColor : activateValuesColor;
        }
    }

    private void NextCard()
    {
        SoundManager.Instance.PlaySFX(SFX.Woosh);
        indexCards++;
        if (indexCards >= CardsReference.cards.Length)
        {
            CardsShuffle();
            indexCards = 0;
        }
        
        SetUpCard();
        bool endTimeSelection = currentTime <= 0;
        ResetTimer();
        OnCardChanged?.Invoke(GasValue, TireValue, ChasisValue, endTimeSelection);
    }

    private void CardsShuffle()
    {
        // Fisher-Yates Shuffle
        if (CardsReference == null || CardsReference.cards == null || CardsReference.cards.Length <= 1)
            return;

        for (int i = CardsReference.cards.Length - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            
            var temp = CardsReference.cards[i];
            CardsReference.cards[i] = CardsReference.cards[randomIndex];
            CardsReference.cards[randomIndex] = temp;
        }
    }

    private void ResetTimer()
    {
        maxTime = CalculateMaxTime();
        currentTime = maxTime;
        imageTimeFill.fillAmount = 1f;
    }

    /// <summary>
    /// Calcula cuánto debe durar la TimeBar en función del tiempo que le
    /// queda al RaceManager. Baja en línea recta desde
    /// (referenceGeneralTimer, referenceMaxTime) hasta
    /// (floorTriggerGeneralTimer, minTimeBarDuration), y a partir de ahí se
    /// queda fija en "minTimeBarDuration" para que siga siendo jugable.
    /// </summary>
    private float CalculateMaxTime()
    {
        if (RaceManager.Instance == null)
            return fallbackMaxTime;

        float slope = (referenceMaxTime - minTimeBarDuration) / (referenceGeneralTimer - floorTriggerGeneralTimer);
        float value = minTimeBarDuration + slope * (RaceManager.Instance.generalTimer - floorTriggerGeneralTimer);

        return Mathf.Max(minTimeBarDuration, value);
    }

    private void Update()
    {
        // Mientras dure la cuenta atrás inicial del RaceManager, la partida
        // no ha empezado todavía: no consumimos el tiempo de la carta.
        if (RaceManager.Instance != null && !RaceManager.Instance.RaceStarted)
            return;

        // La cuenta atrás acaba de terminar: ahora sí mostramos la carta.
        if (cardCanvas != null && !cardCanvas.enabled)
            cardCanvas.enabled = true;

        currentTime -= Time.deltaTime;
        currentTime = Mathf.Clamp(currentTime, 0f, maxTime);
        imageTimeFill.fillAmount = currentTime / maxTime;

        if (currentTime <= 0f)
            NextCard();
            
    }
}
