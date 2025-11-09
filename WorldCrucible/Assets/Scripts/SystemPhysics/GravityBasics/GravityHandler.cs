using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityHandler : MonoBehaviour
{
    [SerializeField] float g = 1f;
    static float G;
    //in physical universe every body would be both attractor and attractee
    public static List<Rigidbody2D> attractors = new List<Rigidbody2D>();
    public static List<Rigidbody2D> attractees = new List<Rigidbody2D>();
    public static bool isSimulatingLive = true;
    void FixedUpdate()
    {
        G=g;//in case g is changed in editor
        if(isSimulatingLive)//PathHandler changes this
        SimulateGravities();
    }
    public static void SimulateGravities()
    {
        foreach(Rigidbody2D attractor in attractors)
        {
            foreach(Rigidbody2D attractee in attractees)
            {
                if(attractor!=attractee)
                AddGravityForce(attractor,attractee);
            }
        }
    }

    public static void AddGravityForce(Rigidbody2D attractor, Rigidbody2D target)
    {
        // Standard Newtonian gravity: F = G * (m1 * m2) / r^2
        float massProduct = attractor.mass * target.mass; // do NOT multiply by G here

        Vector3 difference = attractor.position - target.position;
        float distance = difference.magnitude;

        // Prevent singularities / extremely large forces when distance ~ 0
        const float minDistance = 0.01f;
        float safeDistance = Mathf.Max(distance, minDistance);

        float unScaledforceMagnitude = massProduct / (safeDistance * safeDistance);
        float forceMagnitude = G * unScaledforceMagnitude;

        Vector3 forceDirection = difference.normalized;
        Vector3 forceVector = forceDirection * forceMagnitude;

        // Defensive: ensure finite force
        if (float.IsNaN(forceVector.x) || float.IsNaN(forceVector.y) || float.IsInfinity(forceVector.x) || float.IsInfinity(forceVector.y))
            return;

        target.AddForce(forceVector);
    }
}
