using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewPlanet : MonoBehaviour {
    public List<Sprite> planetSprites;
    //randomized int that determines sprite
    private int rand;
    //changing values reflecting planet data
    public int mass;
    public List<string> compElements;
    public List<float> compPercents;
    
    void Start() {
        //selects random sprite to use for rest of lifetime
        rand = Random.Range(0,planetSprites.Count);
        GetComponent<SpriteRenderer>().sprite = planetSprites[rand];
    }

    void Update(){
        if(Input.GetMouseButtonDown(0)) {
            Recolor();
        }
    }
    
    void Resize() {
        
    }

    void Recolor() {
        GetComponent<SpriteRenderer>().color = new Color32((byte)Random.Range(0,255), (byte)Random.Range(0,255), (byte)Random.Range(0,255), (byte)255);
    }
}
