using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewPlanet : MonoBehaviour {
    public List<Sprite> planetSprites;
    //randomized int that determines sprite
    private int rand;
    //changing values reflecting planet data
    private bool isInPool;
    private int mass;
    private List<string> compElements;
    private List<float> compPercents;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        rand = Random.Range(0,planetSprites.Count);
        GetComponent<SpriteRenderer>().sprite = planetSprites[rand];
    }
    
    // Update is called once per frame
    void Update(){
        if(Input.GetMouseButtonDown(0)) {
            Recolor();
        }
    }
    
    void Resize() {
        
    }

    void Recolor() {
        //GetComponent<SpriteRenderer>().color = new Color32(R, G, B, A);
        
    }
}
