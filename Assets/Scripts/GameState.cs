using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public static GameState Instance;

    [Header("Collected Cards")]
    public List<CardData> collectedCards = new List<CardData>();

    public bool hasCard => collectedCards.Count > 0;

    [Header("Played Card Route")]
    public CardData playedCard;
    public string routeFlag = "";
    public bool playedSpecialRoute = false;

    [Header("All Route Flags")]
    public List<string> routeFlags = new List<string>();

    [Header("Testing Only")]
    public bool addTestCardsOnStart = false;
    public CardData[] testCardsToAdd;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        AddTestCardsIfEnabled();
    }

    private void AddTestCardsIfEnabled()
    {
        if (!addTestCardsOnStart)
            return;

        if (testCardsToAdd == null || testCardsToAdd.Length == 0)
            return;

        foreach (CardData card in testCardsToAdd)
        {
            SetCollectedCard(card);
        }

        Debug.Log("Testkarten wurden hinzugefügt. Anzahl gesammelter Karten: " + collectedCards.Count);
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

    public void SetPlayedCardRoute(CardData card, string flag, bool isSpecialRoute)
    {
        if (card == null)
        {
            Debug.LogWarning("Tried to set played card route, but card was null.");
            return;
        }

        playedCard = card;
        routeFlag = flag;
        playedSpecialRoute = isSpecialRoute;

        AddRouteFlag(flag);

        Debug.Log("Played card: " + card.cardName + " | Route Flag: " + routeFlag + " | Special Route: " + playedSpecialRoute);
    }

    public void AddRouteFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag))
            return;

        if (!routeFlags.Contains(flag))
        {
            routeFlags.Add(flag);
            Debug.Log("Route Flag hinzugefügt: " + flag);
        }
    }

    public bool HasRouteFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag))
            return false;

        if (routeFlag == flag)
            return true;

        return routeFlags.Contains(flag);
    }

    public bool HasAllRouteFlags(string[] flags)
    {
        if (flags == null || flags.Length == 0)
            return true;

        foreach (string flag in flags)
        {
            if (string.IsNullOrEmpty(flag))
                continue;

            if (!HasRouteFlag(flag))
                return false;
        }

        return true;
    }

    public bool HasAnyRouteFlag(string[] flags)
    {
        if (flags == null || flags.Length == 0)
            return false;

        foreach (string flag in flags)
        {
            if (string.IsNullOrEmpty(flag))
                continue;

            if (HasRouteFlag(flag))
                return true;
        }

        return false;
    }

    public bool HasPlayedCard(CardData card)
    {
        if (card == null || playedCard == null)
            return false;

        return playedCard == card;
    }
}