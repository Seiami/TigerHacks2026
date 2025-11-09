using UnityEngine;
using UnityEngine.EventSystems;

// Tutorial: https://www.youtube.com/watch?v=BGr-7GZJNXg

// Reference https://youtu.be/wlBJ0yZOYfM?si=HZ7nDxyiXhDNFLH1
public class Slot : MonoBehaviour, IDropHandler
{
    public GameObject currentItem; 


    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("OnDrop");
        if (eventData.pointerDrag != null)
        {
            eventData.pointerDrag.GetComponent<RectTransform>().anchoredPosition = GetComponent<RectTransform>().anchoredPosition;
        }
    }
}
