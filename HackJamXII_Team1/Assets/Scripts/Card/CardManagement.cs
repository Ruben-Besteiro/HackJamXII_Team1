using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardManagement : MonoBehaviour
{

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

    

    private void Start()
    {
        CardsShuffle();
        UpdateUI();
        SetUpCard();
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
        Debug.Log("Check choice");

        if (_isRight)
        {
            GasValue += CardsReference.cards[indexCards].cardGasValueRight;
            TireValue += CardsReference.cards[indexCards].cardTireValueRight;
            ChasisValue += CardsReference.cards[indexCards].cardChasisValueRight;
        }
        else
        {
            GasValue += CardsReference.cards[indexCards].cardGasValueLeft;
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
}
