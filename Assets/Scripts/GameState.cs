using UnityEngine;

public class GameState : MonoBehaviour
{
    public static GameState Instance;

    [Header("Collected Card")]
    public bool hasCard = false;
    public CardData collectedCard;

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
        collectedCard = card;
        hasCard = card != null;
    }

    public int GetCollectedCardValue()
    {
        if (collectedCard == null)
            return -1;

        return collectedCard.cardValue;
    }
}