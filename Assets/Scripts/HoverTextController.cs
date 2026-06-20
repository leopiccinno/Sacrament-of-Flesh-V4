using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonHoverText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("Hover Text")]
    public GameObject hoverTextObject;
    public TextMeshProUGUI hoverText;

    [Header("Settings")]
    public string textToShow = "Button";
    public Vector2 offset = new Vector2(0f, 15f);

    private RectTransform hoverTextRect;
    private RectTransform canvasRect;
    private Canvas canvas;

    private bool isHovering = false;

    private static ButtonHoverText currentOwner;

    private void Awake()
    {
        if (hoverTextObject != null)
            hoverTextRect = hoverTextObject.GetComponent<RectTransform>();

        canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();

        HideHoverText();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverTextObject == null || hoverText == null)
            return;

        isHovering = true;
        currentOwner = this;

        hoverText.text = textToShow;
        hoverTextObject.SetActive(true);

        MoveText(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;

        if (currentOwner == this)
        {
            HideHoverText();
        }
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (isHovering && currentOwner == this)
        {
            MoveText(eventData);
        }
    }

    private void MoveText(PointerEventData eventData)
    {
        if (hoverTextRect == null || canvasRect == null || canvas == null)
            return;

        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPoint
        );

        hoverTextRect.anchoredPosition = localPoint + offset;
    }

    private void HideHoverText()
    {
        isHovering = false;

        if (hoverTextObject != null)
        {
            hoverTextObject.SetActive(false);
        }

        if (currentOwner == this)
        {
            currentOwner = null;
        }
    }

    private void OnDisable()
    {
        if (currentOwner == this)
        {
            HideHoverText();
        }
    }

    private void OnDestroy()
    {
        if (currentOwner == this)
        {
            HideHoverText();
        }
    }
}