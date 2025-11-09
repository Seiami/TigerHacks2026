using UnityEngine;

    // Reference https://youtu.be/wlBJ0yZOYfM?si=vJTR7SZYD9pnzAnV

public class InventoryController : MonoBehaviour
{
    public GameObject panel;
    public GameObject slotPrefab;
    public int slotCount;
    public GameObject[] itemPrefabs;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i< slotCount; i++)
        {
            Slot slot = Instantiate(slotPrefab, panel.transform).GetComponent<Slot>();
            if (i<itemPrefabs.Length)
            {
                GameObject item = Instantiate(itemPrefabs[i], slot.transform);
                item.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                slot.currentItem = item;
            }
        }
    }
}
