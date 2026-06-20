using UnityEngine;

[CreateAssetMenu(fileName = "NewCardData", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Card Info")]
    public string cardName;

    [TextArea(2, 5)]
    public string description;

    [Header("Card Value")]
    public int cardValue;

    [Header("Sprites")]
    public Sprite smallSprite;
    public Sprite bigSprite;
}