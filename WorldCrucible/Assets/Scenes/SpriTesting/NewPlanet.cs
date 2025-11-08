using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class NewPlanet : MonoBehaviour
{
    
    private int rand;
    
    
    public List<Sprite> planetSprites;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rand = Random.Range(0,planetSprites.Count);
        GetComponent<SpriteRenderer>().sprite = planetSprites[rand];
    }
    
    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0)) {
            Recolor((byte)Random.Range(0,255), (byte)Random.Range(0,255), (byte)Random.Range(0,255));
        }
    }
    
    void Recolor(byte Rval, byte Bval, byte Aval)
    {
        GetComponent<SpriteRenderer>().color = new Color32(Rval, Bval, Aval, 255);
    }
}
