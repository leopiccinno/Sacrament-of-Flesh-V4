using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PersistentCardHUD : MonoBehaviour
{
    public static PersistentCardHUD Instance;

    [Header("Card Icon UI")]
    public GameObject cardIconObject;
    public Image cardIconImage;

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
        if (GameState.Instance == null)
        {
            HideCardIcon();
            return;
        }

        if (!GameState.Instance.hasCard || GameState.Instance.collectedCard == null)
        {
            HideCardIcon();
            return;
        }

        ShowCardIcon(GameState.Instance.collectedCard);
    }

    private void ShowCardIcon(CardData card)
    {
        if (cardIconObject != null)
            cardIconObject.SetActive(true);

        if (cardIconImage != null)
        {
            cardIconImage.sprite = card.smallSprite;
            cardIconImage.preserveAspect = true;
        }
    }

    private void HideCardIcon()
    {
        if (cardIconObject != null)
            cardIconObject.SetActive(false);
    }
}