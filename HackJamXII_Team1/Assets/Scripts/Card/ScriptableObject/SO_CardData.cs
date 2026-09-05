using UnityEngine;

public enum CardCategory { Basic, Rare, Gift}

[CreateAssetMenu(fileName = "SO_CardData", menuName = "Scriptable Objects/SO_CardData")]
public class SO_CardData : ScriptableObject
{
    public string cardDescripton;
    public Sprite cardImage;
    public string cardTitle;
    public string cardRightChoiceText;
    public string cardLeftChoiceText;
    public CardCategory cardCategory;
    [Header("Card Values Right")]
    public int cardGasValueRight;
    public int cardTireValueRight;
    public int cardChasisValueRight;
    [Header("Card Values Left")]
    public int cardGasValueLeft;
    public int cardTireValueLeft;
    public int cardChasisValueLeft;
}
