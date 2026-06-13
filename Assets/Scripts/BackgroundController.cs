using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundController : MonoBehaviour
{
    [Header("Background Appearance")]
    public Color backgroundColor = Color.white;
    public Sprite backgroundSprite;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyBackground();
    }

    private void OnValidate()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        ApplyBackground();
    }

    private void ApplyBackground()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.color = backgroundColor;

        if (backgroundSprite != null)
        {
            spriteRenderer.sprite = backgroundSprite;
        }
    }
}