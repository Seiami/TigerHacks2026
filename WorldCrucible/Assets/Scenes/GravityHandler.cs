using UnityEngine;
// System.Collections.Generic.List;
// //  reference https://youtu.be/o3rt2FaMwBs?si=4e5TcTXtYOIzXOPm
// public class GravityHandler : MonoBehaviour
// {
//     [SerializeField] float g = 1f;
//     static float G;

//     public static List<Rigidbody2D> attractors = new List<Rigidbdoy2D>();
//     public static List<Rigidbody2D> attractees = new List<Rigidbdoy2D>();
//     public static bool isSimulatingLive = true;

//     // Start is called once before the first execution of Update after the MonoBehaviour is created
//     void Start()
//     {
        
//     }
//     // Update is called every interval
//     void FixedUpdate()
//     {
//         G = G;
//         if(isSimulatingLive) SimulateGravities();
//     }
//     public static void SimulateGravities()
//     {
//         foreach(Rigidbody2D attractor in attractors)
//         {
//             foreach(Rigidbody2D attractee in attractees)
//             {
//                 if(attractor!=attractees) GravityForce(attractor, attractee);
//             }
//         }
//     }

//     public static void GravityForce(Rigidbody2D attractor, Rigidbody2D target){
//         float massProduct = attractor.mass*target.mass*GravityForce;

//         Vector2 difference = attractor.position - target.position;
//         float distance = difference.magnitude;

//         // force = GravityForce * ((m1*m2)/r^2)

//         float unScaledforceMagnitude = massProduct/Mathf.Pow(distance,2);
//         float forceMagnitude = GravityForce*unScaledforceMagnitude;

//         Vector2 forceDirection = difference.normalized;

//         vector2 forceVector = forceDirection * forceMagnitude;
//         target.AddForce(forceVector);
//     }
    
    

// }
