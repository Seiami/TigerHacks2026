using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolPartical : MonoBehaviour
{
    public bool isInPool;
    public bool isOnScreen;
    //public List<Sprite> meteorSprites;

    //variable for randomness of spawning position
    private float heat;
    //variable for the point the object moves towards
    private Vector3 target;

    void Start()
    {
        transform.position = new Vector3(100,100,0);
        isInPool = true;
        isOnScreen = false;
        //selects random 
        //GetComponent<SpriteRenderer>().sprite = meteorSprites[Random.Range(0,meteorSprites.Count)];
    }

    // Update is called once per frame
    void Update()
    {
        //handles 'spawning'
        if(isInPool == false) {
            Resize();
            Recolor();
            Vector2 mousePosition = Input.mousePosition;
            transform.position = new Vector3(mousePosition[0] + Random.Range(0.0f,heat),mousePosition[1] + Random.Range(0.0f, heat),0);
            isInPool = true;
            isOnScreen = true;
        }
        //handles movement and return to pool
        if(isOnScreen == true) {
            
        }
    }

    void Resize(){
        //transform.localScale = new Vector3(, , 0);
    }

    void Recolor(){
        //GetComponent<SpriteRenderer>().color = ;
    }
}
