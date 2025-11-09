using UnityEngine;

/// <summary>
/// Temporary diagnostic script to debug gravity issues.
/// Attach to SolarSystemManager and check console output.
/// </summary>
public class GravityDiagnostic : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== GRAVITY DIAGNOSTIC START ===");
        
        // Check for SolarSystem
        var solarSystem = GetComponent<SolarSystem>();
        if (solarSystem == null)
        {
            Debug.LogError("SolarSystem component NOT found on this GameObject!");
        }
        else
        {
            Debug.Log($"✓ SolarSystem found. G={SolarSystem.gravitationalConstant}");
        }
        
        // Check for CelestialPathHandler
        var pathHandler = GetComponent<CelestialPathHandler>();
        if (pathHandler == null)
        {
            Debug.LogWarning("CelestialPathHandler component NOT found on this GameObject!");
        }
        else
        {
            Debug.Log($"✓ CelestialPathHandler found.");
        }
        
        // Check Physics2D settings
        Debug.Log($"Physics2D Gravity: {Physics2D.gravity}");
        if (Physics2D.gravity != Vector2.zero)
        {
            Debug.LogWarning("Physics2D gravity is not zero! Set to (0,0) for custom gravity.");
        }
        
        Debug.Log("=== GRAVITY DIAGNOSTIC END ===");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("=== RUNTIME DIAGNOSTIC ===");
            
            // Find all celestial bodies
            GameObject[] bodies = GameObject.FindGameObjectsWithTag("CelestialBodies");
            Debug.Log($"Found {bodies.Length} objects with tag 'CelestialBodies'");
            
            foreach (var body in bodies)
            {
                var cb = body.GetComponent<CelestialBody>();
                var rb = body.GetComponent<Rigidbody2D>();
                
                if (cb == null)
                {
                    Debug.LogWarning($"  {body.name}: Missing CelestialBody component!");
                }
                if (rb == null)
                {
                    Debug.LogError($"  {body.name}: Missing Rigidbody2D!");
                }
                else
                {
                    Debug.Log($"  {body.name}: mass={rb.mass}, simulated={rb.simulated}, " +
                             $"bodyType={rb.bodyType}, gravityScale={rb.gravityScale}, " +
                             $"velocity={rb.linearVelocity.magnitude:F2}");
                }
            }
            
            Debug.Log("=== END DIAGNOSTIC (Press D to run again) ===");
        }
    }
}
