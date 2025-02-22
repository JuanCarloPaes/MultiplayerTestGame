using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class MenuButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public RectTransform buttonTransform; // The main button transform
    public Image backgroundSprite; // The sprite that appears behind the button
    public float hoverScale = 1.1f; // Scale factor on hover
    public float animationDuration = 0.2f; // Duration of the animation

    private Vector3 originalScale;
    private Color originalColor;

    void Start()
    {
        if (buttonTransform == null)
            buttonTransform = GetComponent<RectTransform>();

        if (backgroundSprite != null)
        {
            originalColor = backgroundSprite.color;
            backgroundSprite.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0); // Hide initially
        }

        originalScale = buttonTransform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Scale up the button
        buttonTransform.DOScale(hoverScale, animationDuration).SetEase(Ease.OutBack);

        // Animate the background sprite appearing (fade in + slight movement)
        if (backgroundSprite != null)
        {
            backgroundSprite.transform.localPosition = new Vector3(-10, 0, 0); // Adjust position to slightly offset
            backgroundSprite.transform.DOLocalMoveX(0, animationDuration).SetEase(Ease.OutBack);
            backgroundSprite.DOFade(1f, animationDuration);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Reset the button scale
        buttonTransform.DOScale(originalScale, animationDuration).SetEase(Ease.InBack);

        // Fade out the background sprite
        if (backgroundSprite != null)
        {
            backgroundSprite.DOFade(0, animationDuration);
        }
    }
}
