using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public static GameState Instance;

    [Header("Collected Cards")]
    public List<CardData> collectedCards = new List<CardData>();

    public bool hasCard => collectedCards.Count > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetCollectedCard(CardData card)
    {
        if (card == null)
            return;

        if (!collectedCards.Contains(card))
        {
            collectedCards.Add(card);
        }
        else
        {
            Debug.Log("Diese Karte wurde bereits eingesammelt: " + card.cardName);
        }
    }

    

    public bool HasCollectedCard(CardData card)
    {
        if (card == null)
            return false;

        return collectedCards.Contains(card);
    }

    public int GetCollectedCardValue()
    {
        if (collectedCards.Count == 0)
            return -1;

        return collectedCards[collectedCards.Count - 1].cardValue;
    }

    public int GetCollectedCardValue(int index)
    {
        if (index < 0 || index >= collectedCards.Count)
            return -1;

        return collectedCards[index].cardValue;
    }
}