using UnityEngine;

public class CrucibleObjDetector : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    Transform origin;
    bool CanMerge;
    private void OnTriggerEnter2D(Collider2D collision) {
        if(gameObject.GetComponent<CircleCollider2D>().IsTouching(collision.gameObject.GetComponent<BoxCollider2D>()))
        {
            Debug.Log($"{collision.gameObject.name} is on the crucible"); 
            // if(collision.gameObject.GetComponent<SpriteRenderer>().color == GetComponent<SpriteRenderer>().color ) {
            Debug.Log("attempting to mege");

            origin = collision.transform;
            CanMerge = true;
            Destroy(collision.gameObject);
            Destroy(GetComponent<Rigidbody2D>());

        }
    }
}
