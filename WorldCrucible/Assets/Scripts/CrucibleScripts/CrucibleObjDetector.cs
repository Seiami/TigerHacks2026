using UnityEngine;

public class CrucibleObjDetector : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void OnTriggerEnter2D(Collider2D collision) {
        if(gameObject.GetComponent<CircleCollider2D>().IsTouching(collision.gameObject.GetComponent<BoxCollider2D>()))
        {
            Debug.Log($"{collision.gameObject.name} is on the crucible"); 
        }
    }
}
