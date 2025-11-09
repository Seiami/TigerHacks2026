using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(CanvasGroup))]
public class ElementIcon : MonoBehaviour, 
// IBeginDragHandler, IDragHandler, IEndDragHandler, 
IPointerClickHandler
{
    [Header("Element Info")]
    public ElementData elementData; // assign in inspector

    private Image image;
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Vector3 originalPosition;
    private Vector2 cursorPos;

    bool clicked = false;
    public GameObject elementCursorPrefab;
    public GameObject elementCursor;

    private static ElementIcon currentlySelected;

    private GameObject floatingImageObject;
    private Image floatingImage;
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

    void Start()
    {
        originalPosition = rectTransform.position;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentlySelected != null && currentlySelected != this)
        {
            currentlySelected.Deselect();
        }
        // if (elementCursor != null ) Destroy(elementCursor);
        clicked = !clicked;
         if (clicked)
        {
            currentlySelected = this;
            OnSelected();
        }else
        {
            Deselect();
        }

        canvasGroup.blocksRaycasts = false; // allow drop zones to detect this

    }

    private void OnSelected()
    {
        canvasGroup.blocksRaycasts = false;

        // ✅ Create a new floating image (UI only)
        floatingImageObject = new GameObject("FloatingImage");
        floatingImageObject.transform.SetParent(canvas.transform, false);

        floatingImage = floatingImageObject.AddComponent<Image>();
        floatingImage.sprite = image.sprite;
        floatingImage.preserveAspect = true;
        floatingImage.raycastTarget = false; // don't block clicks

        RectTransform floatRect = floatingImageObject.GetComponent<RectTransform>();
        floatRect.sizeDelta = rectTransform.sizeDelta;
    }
    private void Deselect()
    {
        clicked = false;
        canvasGroup.blocksRaycasts = true;

        // Destroy floating image
        if (floatingImageObject != null)
            Destroy(floatingImageObject);

        // Move icon back (optional)
        // rectTransform.position = originalPosition;
    }

    void Update()
    {
        if (clicked && floatingImageObject != null)
        {
            cursorPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            floatingImageObject.transform.position = new Vector2(cursorPos.x, cursorPos.y);
            // Vector2 mouseScreenPosition = Input.mousePosition;
            // sprite.transform.position = mouseScreenPosition;
        } 
        // else{
        //     elementCursor.transform.position = originalPosition.position;
        // }
    }


}


    // public void OnBeginDrag(PointerEventData eventData)
    // {
    //     originalParent = transform.parent;
    //     transform.SetParent(canvas.transform, true); // move to root canvas for smooth dragging
    //     canvasGroup.blocksRaycasts = false; // allow drop zones to detect this
    //     canvasGroup.alpha = 0.8f; // slightly transparent for feedback
    // }

    // public void OnDrag(PointerEventData eventData)
    // {
    //     rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    // }

    // public void OnEndDrag(PointerEventData eventData)
    // {
    //     canvasGroup.blocksRaycasts = true;
    //     canvasGroup.alpha = 1f;

    //     // If dropped nowhere (no new parent assigned), return to original
    //     if (transform.parent == canvas.transform)
    //     {
    //         transform.SetParent(originalParent, false);
    //     }
    // }
// }