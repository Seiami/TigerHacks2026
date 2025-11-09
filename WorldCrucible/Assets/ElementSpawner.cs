using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ElementSpawner : MonoBehaviour, IBeginDragHandler, IDragHandler
// , IEndDragHandler 
{
    public GameObject sourceObj; 


    // Start is called once before the first execution of Update after the MonoBehaviour is created

        public void OnBeginDrag(PointerEventData eventData)
    {
        // Get selected sourceObject
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Spawn source object
        Instantiate(sourceObj, sourceObj.transform);

    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
