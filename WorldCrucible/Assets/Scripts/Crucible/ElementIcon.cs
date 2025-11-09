using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(CanvasGroup))]
public class ElementIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Element Info")]
    public ElementData elementData; // assign in inspector

    private Image image;
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Transform originalParent;

    private void Awake()
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();

        if (elementData != null)
            UpdateIconVisual();
    }

    private void UpdateIconVisual()
    {
        // Set the sprite and optionally a color tint
        image.sprite = elementData.icon;
        image.preserveAspect = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        transform.SetParent(canvas.transform, true); // move to root canvas for smooth dragging
        canvasGroup.blocksRaycasts = false; // allow drop zones to detect this
        canvasGroup.alpha = 0.8f; // slightly transparent for feedback
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // If dropped nowhere (no new parent assigned), return to original
        if (transform.parent == canvas.transform)
        {
            transform.SetParent(originalParent, false);
        }
    }
}