using UnityEngine;
using System.Collections.Generic;


// Tutorial: https://www.youtube.com/watch?v=7axImc1sxa0&t=268s

[ExecuteInEditMode]
[RequireComponent (typeof (Rigidbody2D))]
public class CelestialBody : GravityObject
{
    public Vector2 initialVelocity;
    public bool useInitialVelocity = false;
    //public string bodyName = "Unnamed";
    Transform meshHolder;

    public Vector2 velocity { get; private set; }
    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        velocity = initialVelocity;
    }

    // Update all velocities of all  at once in scene
    /* 
    public void UpdateVelocity(CelestialBody[] allBodies, float timeStep)
     {
         foreach (var otherBody in allBodies)
         {
             if (otherBody != this)
             {
                 float sqrDst = (otherBody.GetComponent<Rigidbody2D>().position - GetComponent<Rigidbody2D>().position).sqrMagnitude; // Squared Distance
                 Vector2 forceDir = (otherBody.GetComponent<Rigidbody2D>().position - GetComponent<Rigidbody2D>().position).normalized; // Normalized direction (angle)

                 Vector2 acceleration = forceDir * Universe.gravitationalConstant * otherBody.mass / sqrDst; // G formula slotted into F = MA for Acceleration
                 velocity += acceleration * timeStep;
             }
         }
     }
     */

    // Update velocity itself
    /*
    public void UpdateVelocity(Vector2 acceleration, float timeStep)
    {
        velocity += acceleration * timeStep;
    }
    

    // Updaye position itself
    public void UpdatePosition(float timeStep)
    {
        rb.MovePosition(rb.position + velocity * timeStep);
    }
    */

    /* void OnValidate()
    {
        //mass = surfaceGravity * radius * radius / Universe.gravitationalConstant;
        // TODO: Mesh holder should be a child under the body that renders/has the mesh representation. Implement later
        // meshHolder.localScale = Vector2.one * radius;
        // meshHolder = transform.GetChild(0); 
        // GameObject.name = bodyName;
    }
    */

    public Rigidbody2D Rigidbody2D
    {
        get
        {
            return rb;
        }
    }

    public UnityEngine.Vector2 Position
    {
        get
        {
            return rb.position;
        }
    }
}
