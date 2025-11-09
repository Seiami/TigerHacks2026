using UnityEngine;
using UnityEngine.UI;
// using System.Collitions.Generic;

public class TabController : MonoBehaviour
{
    public Image[] tabImages;
    public GameObject[] pages;

    // public Color unSelected = new Color(32)(159, 90, 253, 1);
    // private Color Selected = new Color(114, 14, 255, 1);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ActivateTab(0);
    }

    // Update is called once per frame
    public void ActivateTab(int tabNum)
    {
        for(int i = 0; i< pages.Length; i++)
        {
            pages[i].SetActive(false);
            // tabImages[i].color = new Color(32)( 200, 240, 113, 1);
            tabImages[i].color = Color.grey;
        }
        pages[tabNum].SetActive(true);
        // tabImages[tabNum].color = Selected;
        tabImages[tabNum].color = Color.white;


    }
}
