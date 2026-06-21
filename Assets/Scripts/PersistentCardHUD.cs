using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PersistentCardHUD : MonoBehaviour
{
    public static PersistentCardHUD Instance;

    [Header("Card Icon UI")]
    public Transform iconContainer;
    public GameObject cardIconPrefab;

    private List<GameObject> spawnedIcons = new List<GameObject>();

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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        RefreshCardIcon();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshCardIcon();
    }

    public void RefreshCardIcon()
{
    ClearIcons();

    if (GameState.Instance == null)
    {
        Debug.LogWarning("GameState.Instance ist null, kann Karten-Icons nicht aktualisieren.");
        return;
    }

    foreach (CardData card in GameState.Instance.collectedCards)
    {
        SpawnIcon(card);
    }
}

private void SpawnIcon(CardData card)
{
    if (cardIconPrefab == null)
    {
        Debug.LogWarning("cardIconPrefab ist nicht zugewiesen in PersistentCardHUD.");
        return;
    }

    if (iconContainer == null)
    {
        Debug.LogWarning("iconContainer ist nicht zugewiesen in PersistentCardHUD.");
        return;
    }

    GameObject icon = Instantiate(cardIconPrefab, iconContainer);
    Image iconImage = icon.GetComponent<Image>();

    if (iconImage != null)
    {
        iconImage.sprite = card.smallSprite;
        iconImage.preserveAspect = true;
    }
    else
    {
        Debug.LogWarning("cardIconPrefab hat keine Image-Komponente auf dem Root-Objekt.");
    }

    spawnedIcons.Add(icon);
}

    private void ClearIcons()
    {
        foreach (GameObject icon in spawnedIcons)
        {
            if (icon != null)
                Destroy(icon);
        }

        spawnedIcons.Clear();
    }
}