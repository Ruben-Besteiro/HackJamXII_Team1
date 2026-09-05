using System;
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
    [SerializeField] private TextMeshProUGUI textGasValue;
    [SerializeField] private TextMeshProUGUI textTiresValue;
    [SerializeField] private TextMeshProUGUI textChasisValue;

    [Header("Time Card References")]
    [SerializeField] private Image imageTimeFill;

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
        CardsShuffle();
        UpdateUI();
        SetUpCard();
        // Time card control
        ResetTimer();
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

    private void UpdateUI()
    {
        textGasValue.text = GasValue.ToString() + "/5";
        textTiresValue.text = TireValue.ToString() + "/5";
        textChasisValue.text = ChasisValue.ToString() + "/5";
    }

    private void NextCard()
    {
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

        // Debug.Log($"[CardManagement] Nueva carta: la TimeBar tardará {maxTime:0.00}s en vaciarse.");
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
        currentTime -= Time.deltaTime;
        currentTime = Mathf.Clamp(currentTime, 0f, maxTime);
        imageTimeFill.fillAmount = currentTime / maxTime;

        if (currentTime <= 0f)
            NextCard();
            
    }
}
