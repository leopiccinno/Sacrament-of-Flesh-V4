using UnityEngine;
using UnityEngine.UI;

public class RandomCardPickup : MonoBehaviour
{
    [Header("Possible Cards")]
    public CardData[] possibleCards;

    [Header("Scene Card UI")]
    public Image smallCardImage;

    [Header("Big Card UI")]
    public GameObject bigCardPanel;
    public Image bigCardImage;

    [Header("HUD Icon")]
    public GameObject cardIconHUD;
    public Image cardIconHUDImage;

    [Header("Dialogue After Pickup")]
    public IntroDialogueController dialogueManager;
    public bool continueDialogueAfterPickup = true;

    [Header("Runtime Info")]
    public CardData selectedCard;
    public bool playerHasCard = false;

    private bool isInspectingCard = false;

    private void Start()
    {
        ChooseRandomCard();

        if (bigCardPanel != null)
            bigCardPanel.SetActive(false);

        if (cardIconHUD != null)
            cardIconHUD.SetActive(false);
    }

    private void ChooseRandomCard()
    {
        if (possibleCards == null || possibleCards.Length == 0)
        {
            Debug.LogWarning("No possible cards assigned to RandomCardPickup.");
            return;
        }

        int randomIndex = Random.Range(0, possibleCards.Length);
        selectedCard = possibleCards[randomIndex];

        if (smallCardImage != null && selectedCard != null)
        {
            smallCardImage.sprite = selectedCard.smallSprite;
            smallCardImage.preserveAspect = true;
        }
    }

    public void OnCardClicked()
    {
        if (playerHasCard)
            return;

        if (selectedCard == null)
        {
            Debug.LogWarning("No card was selected.");
            return;
        }

        isInspectingCard = true;

        if (bigCardPanel != null)
            bigCardPanel.SetActive(true);

        if (bigCardImage != null)
        {
            bigCardImage.sprite = selectedCard.bigSprite;
            bigCardImage.preserveAspect = true;
        }
    }

    private void Update()
    {
        if (isInspectingCard && Input.GetKeyDown(KeyCode.Return))
        {
            CollectCard();
        }
    }

    private void CollectCard()
{
    isInspectingCard = false;
    playerHasCard = true;

    if (bigCardPanel != null)
        bigCardPanel.SetActive(false);

    if (GameState.Instance != null)
    {
        GameState.Instance.SetCollectedCard(selectedCard);
    }

    if (PersistentCardHUD.Instance != null)
    {
        PersistentCardHUD.Instance.RefreshCardIcon();
    }

    gameObject.SetActive(false);

    if (continueDialogueAfterPickup && dialogueManager != null)
    {
        dialogueManager.StartAfterNoteDialogue();
    }
}


    public bool HasCard()
    {
        return playerHasCard;
    }

    public int GetCardValue()
    {
        if (selectedCard == null)
            return -1;

        return selectedCard.cardValue;
    }
}