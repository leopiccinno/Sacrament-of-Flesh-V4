using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonHoverText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("Hover Text")]
    public GameObject hoverTextObject;
    public TextMeshProUGUI hoverText;

    [Header("Settings")]
    public string textToShow = "Hallway";
    public Vector2 offset = new Vector2(20f, -20f);

    private RectTransform hoverTextRect;
    private RectTransform canvasRect;
    private Canvas canvas;

    private bool isHovering = false;

    private void Awake()
    {
        hoverTextRect = hoverTextObject.GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas.GetComponent<RectTransform>();

        hoverTextObject.SetActive(false);
        hoverText.text = textToShow;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        hoverText.text = textToShow;
        hoverTextObject.SetActive(true);
        MoveText(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        hoverTextObject.SetActive(false);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (isHovering)
        {
            MoveText(eventData);
        }
    }

    private void MoveText(PointerEventData eventData)
    {
        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPoint
        );

        hoverTextRect.anchoredPosition = localPoint + offset;
    }
}