using UnityEngine;

public class SolarSystem : MonoBehaviour
{

    public const float gravitationalConstant = 100f; //0.0001f
    GameObject[] bodies;
    [SerializeField] bool IsEllipticalOrbit = false;

    void Awake()
    {
        bodies = GameObject.FindGameObjectsWithTag("CelestialBodies");

        SetInitialVelocity();
    }

    private void FixedUpdate()
    {
        Gravity();
    }

    void SetInitialVelocity()
    {
        foreach (GameObject a in bodies)
        {
            Rigidbody2D rbA = a.GetComponent<Rigidbody2D>();
            foreach (GameObject b in bodies)
            {
                if(!a.Equals(b))
                {
                    Rigidbody2D rbB = b.GetComponent<Rigidbody2D>();
                    float m2 = rbB.mass;
                    Vector2 direction = ((Vector2)b.transform.position - (Vector2)a.transform.position);
                    float r = direction.magnitude;
                    direction.Normalize();

                    Vector2 perpendicular = new Vector2(-direction.y, direction.x);

                    if (IsEllipticalOrbit)
                    {
                        // Eliptic orbit = G * M  ( 2 / r + 1 / a) where G is the gravitational constant, M is the mass of the central object, r is the distance between the two bodies
                        // and a is the length of the semi major axis (!!! NOT GAMEOBJECT a !!!)
                        rbA.linearVelocity+= perpendicular * Mathf.Sqrt((gravitationalConstant * m2) * ((2 / r) - (1 / (r * 1.5f))));
                    }
                    else
                    {
                        //Circular Orbit = ((G * M) / r)^0.5, where G = gravitational constant, M is the mass of the central object and r is the distance between the two objects
                        //We ignore the mass of the orbiting object when the orbiting object's mass is negligible, like the mass of the earth vs. mass of the sun
                        rbA.linearVelocity += perpendicular * Mathf.Sqrt((gravitationalConstant * m2) / r);
                    }
                }
            }
        }
    }

    void Gravity()
    {
        foreach (GameObject a in bodies)
        {
            Rigidbody2D rbA = a.GetComponent<Rigidbody2D>();
            foreach (GameObject b in bodies)
            {
                if (!a.Equals(b))
                {
                    Rigidbody2D rbB = b.GetComponent<Rigidbody2D>();
                    float m1 = rbA.mass;
                    float m2 = rbB.mass;
                    Vector2 direction = ((Vector2)b.transform.position - (Vector2)a.transform.position);
                    float r = direction.magnitude;
                    direction.Normalize();

                    rbA.AddForce(direction * (Universe.gravitationalConstant * (m1 * m2) / (r * r)));
                }
            }
        }
    }
}
