using UnityEngine;

public class FloatingTitle : MonoBehaviour
{
    [Header("Floating Settings")]
    public float floatHeight = 20f;
    public float floatSpeed = 1.5f;

    private RectTransform rectTransform;
    private Vector2 startPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        startPosition = rectTransform.anchoredPosition;
    }

    private void Update()
    {
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        rectTransform.anchoredPosition = new Vector2(startPosition.x, newY);
    }
}