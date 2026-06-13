using UnityEngine;
using UnityEngine.EventSystems;

public class ClickableSquareBorderHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Border Lines")]
    public GameObject topLine;
    public GameObject bottomLine;
    public GameObject leftLine;
    public GameObject rightLine;

    private void Start()
    {
        SetBorderVisible(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetBorderVisible(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetBorderVisible(false);
    }

    private void SetBorderVisible(bool visible)
    {
        if (topLine != null) topLine.SetActive(visible);
        if (bottomLine != null) bottomLine.SetActive(visible);
        if (leftLine != null) leftLine.SetActive(visible);
        if (rightLine != null) rightLine.SetActive(visible);
    }
}