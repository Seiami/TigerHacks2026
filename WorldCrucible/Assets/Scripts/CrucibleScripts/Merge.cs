using UnityEngine;
using System;
// using System.Collitions.Generic;
// using System.Collitions;

public class Merge : MonoBehaviour
{
    int ID;
    public GameObject MergedObject;
    Transform Block1;
    Transform Block2;
    public float Distance; // The conditional distance until elements start moving towards 
    public float MergeSpeed; // how fast the elements will move together


    public float MergeThreshold = 0.5f;   // Distance at which merging occurs
    public float AttractionConstant = 1f; // Strength of attraction (like gravity)
    public float MaxAcceleration = 10f;   // To prevent excessive speeds

    Rigidbody2D rb;

    float mass;


    bool CanMerge;  // Determines if the elements can merge
    void Start() {
        ID = GetInstanceID();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update() {
        // if(Input.GetKeyDown(KeyCode.Space)) Debug.Log("hello!");;
    }

    private void FixedUpdate() {
        MoveTowards();
    }
    
    public void MoveTowards() {
        if (CanMerge){
            GetComponent<Transform>().position = Vector2.MoveTowards(Block1.position, Block2.position, MergeSpeed);
            if(Vector2.Distance(Block1.position, Block2.position) < Distance)
            {
                if(ID< Block2.gameObject.GetComponent<Merge>().ID) {return;}
                Debug.Log($"Sending Message From {gameObject.name} with ID: {ID}");
                GameObject o = Instantiate(MergedObject, transform.position, Quaternion.identity) as GameObject;
                Destroy(Block2.gameObject);
                Destroy(gameObject);
                
            }
        }
    }
    private void OnCollisionEnter2D(Collision2D collision) {
        if(collision.gameObject.CompareTag("MergeBlock")) {
            if(collision.gameObject.GetComponent<SpriteRenderer>().color == GetComponent<SpriteRenderer>().color ) {
                Block1 = transform;
                Block2 = collision.transform;
                CanMerge = true;
                Destroy(collision.gameObject.GetComponent<Rigidbody2D>());
                Destroy(GetComponent<Rigidbody2D>());
            }
        }
    }
}
