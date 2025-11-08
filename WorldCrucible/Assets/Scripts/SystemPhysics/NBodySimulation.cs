using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Tutorial: https://www.youtube.com/watch?v=7axImc1sxa0&t=268s
// Tutorial: https://www.youtube.com/watch?v=kUXskc76ud8&t=162s

public class NBodySimulation : MonoBehaviour
{
    CelestialBody[] bodies;
    static NBodySimulation instance;


    void Awake()
    {
        bodies = FindObjectsByType<CelestialBody>(FindObjectsSortMode.None);

        Time.fixedDeltaTime = Universe.physicsTimeStep;
    }

    void FixedUpdate()
    {
        /*
        for (int i = 0; i < bodies.Length; i++)
        {
            Vector2 acceleration = CalculateAcceleration(bodies[i].Position, bodies[i]);
            bodies[i].UpdateVelocity(acceleration, Universe.physicsTimeStep);
        }
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].UpdatePosition(Universe.physicsTimeStep);
        }
        */
    }

    public static Vector2 CalculateAcceleration(Vector2 point, CelestialBody ignorebody = null)
    {
        Vector2 acceleration = Vector2.zero;
        foreach (var body in Instance.bodies)
        {
            if (body != ignorebody)
            {
                float sqrDst = (body.Position - point).sqrMagnitude;
                Vector2 forceDir = (body.Position - point).normalized;
                acceleration += forceDir * Universe.gravitationalConstant * body.GetComponent<Rigidbody2D>().mass / sqrDst;
            }
        }
        return acceleration;
    }

    


    public static CelestialBody[] Bodies
    {
        get
        {
            return Instance.bodies;
        }
    }

    static NBodySimulation Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<NBodySimulation>();
            }
            return instance;
        }
    }
}
