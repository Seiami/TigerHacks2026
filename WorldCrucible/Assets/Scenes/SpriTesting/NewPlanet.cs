using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewPlanet : MonoBehaviour {
    public List<Sprite> planetSprites;
    //changing values reflecting planet data
    public int heat;
    private int prevHeat;
    public int mass;
    private int prevMass;
    public List<ElementData> compElements;
    private List<ElementData> prevElements;
    public List<float> compPercents;
    private List<float> prevPercents;
    
    void Start() {
        heat = 0;
        prevHeat = 0;
        mass = 0;
        prevMass = 0;
        //selects random sprite to use for rest of lifetime
        GetComponent<SpriteRenderer>().sprite = planetSprites[Random.Range(0,planetSprites.Count)];
        transform.localScale = new Vector3(0,0,0);
    }

    void Update(){
        if(mass != prevMass) {
            Resize();
        }
        if(ListCompElement(compElements, prevElements, compElements.Count, prevElements.Count) || ListCompFloat(compPercents, prevPercents, compPercents.Count, prevPercents.Count) || heat != prevHeat) {
            Recolor();
        }
        //if(Input.GetMouseButton(0)) {
        //    Recolor();
        //    Resize();
        //}
    }

    bool ListCompElement(List<ElementData> lista, List<ElementData> listb, int lena, int lenb) {
        if(lena != lenb) {
            return true;
        }
        for(int i=0; i<lena; i++) {
            if(lista[i] != listb[i]) {
                return true; 
            }
        }
        return false;
    }

    bool ListCompFloat(List<float> lista, List<float> listb, int lena, int lenb) {
        if(lena != lenb) {
            return true;
        }
        for(int i=0; i<lena; i++) {
            if(lista[i] != listb[i]) {
                return true; 
            }
        }
        return false;
    }
    
    void Resize() {
        float size;
        if(mass>1000.0f) {
            size = 1;
        }
        size = Mathf.Log(mass, 1000.0f);
        transform.localScale = new Vector3 (size,size,0);
    }

    void Recolor() {
        int len = compElements.Count;
        float Rval=0.0f, Gval=0.0f, Bval=0.0f;
        Color elementColor;
        float perc;
        for(int i=0; i<len; i++) {
            elementColor = compElements[i].color;
            perc = compPercents[i];
            Rval += elementColor[0] * perc;
            Gval += elementColor[1] * perc;
            Bval += elementColor[2] * perc;
        }
        GetComponent<SpriteRenderer>().color = new Color(Rval, Gval, Bval);
    }
}
