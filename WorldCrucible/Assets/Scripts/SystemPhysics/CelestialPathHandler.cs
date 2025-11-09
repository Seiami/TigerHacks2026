using System.Collections.Generic;
using UnityEngine;

public class CelestialPathHandler : MonoBehaviour
{
    public static CelestialPathHandler Instance;
    
    [Header("Path Visualization Settings")]
    [Tooltip("Number of prediction steps to simulate")]
    public int simulationSteps = 500;
    [Tooltip("Time step for each prediction step")]
    public float simulationTimeStep = 0.02f;
    public Color pathColor = Color.cyan;
    
    [Header("Velocity Control")]
    [Tooltip("Scales drag distance to velocity. LOWER = slower, more orbital. HIGHER = faster, escape velocity. Try 0.001-0.01 for large worlds, 0.05-0.2 for small worlds.")]
    [Range(0f, 2f)] public float throwToVelocityScale = 0.1f;
    
    [Tooltip("Maximum initial velocity (clamps extreme drags). Set to 0 for no limit.")]
    [Range(0f, 500f)] public float maxInitialVelocity = 200f;
    
    private GameObject cloneObject;
    private CelestialBody currentBody;
    private LineRenderer pathLine;
    private Vector3 dragStartPosition;
    private SolarSystem solarSystem;
    private Camera mainCam;

    void Awake()
    {
        Instance = this;
        solarSystem = GetComponent<SolarSystem>();
        mainCam = Camera.main;
    }

    public void StartVisualizingPath(CelestialBody body)
    {
        currentBody = body;
        // Capture the CURRENT position when drag starts (not when body was spawned)
        dragStartPosition = body.transform.position;
        
        Debug.Log($"[PathHandler] Starting path visualization from position: {dragStartPosition}");
        
        // Create visualization line if needed
        if (pathLine == null)
        {
            GameObject lineObj = new GameObject("PathVisualization");
            pathLine = lineObj.AddComponent<LineRenderer>();
            pathLine.startWidth = 0.05f;
            pathLine.endWidth = 0.05f;
            pathLine.material = new Material(Shader.Find("Sprites/Default"));
            pathLine.startColor = pathColor;
            pathLine.endColor = pathColor;
        }
        
        pathLine.enabled = true;
    }

    void Update()
    {
        if (currentBody != null && currentBody.isBeingPlaced)
        {
            VisualizePath();
        }
    }

    void VisualizePath()
    {
        if (Input.GetMouseButton(0))
        {
            if (mainCam == null) { mainCam = Camera.main; if (mainCam == null) return; }
            Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            
            Vector2 throwVector = CalculateThrowVector(mousePos);
            SimulatePath(currentBody, throwVector);
        }
        else
        {
            // Hide path when not dragging
            if (pathLine != null)
            {
                pathLine.enabled = false;
            }
        }
    }

    Vector2 CalculateThrowVector(Vector3 mousePos)
    {
        Vector2 distance = mousePos - dragStartPosition;
        return -distance; // Pull-back mechanic
    }

    void SimulatePath(CelestialBody body, Vector2 initialDragVector)
    {
        List<Vector3> pathPoints = new List<Vector3>();
        
        // Create virtual simulation state
        VirtualBody[] virtualBodies = CreateVirtualBodies();
        int bodyIndex = FindBodyIndex(body);
        
        // Apply initial velocity to our body (scaled drag vector only, ignore accumulated velocity)
        Vector2 velDelta = initialDragVector * throwToVelocityScale;
        virtualBodies[bodyIndex].velocity = velDelta; // Use ONLY the throw vector for prediction
        
        // Simulate forward
        for (int step = 0; step < simulationSteps; step++)
        {
            // Calculate accelerations
            for (int i = 0; i < virtualBodies.Length; i++)
            {
                Vector2 acceleration = CalculateAcceleration(virtualBodies[i].position, i, virtualBodies);
                virtualBodies[i].velocity += acceleration * simulationTimeStep;
            }
            
            // Update positions
            for (int i = 0; i < virtualBodies.Length; i++)
            {
                virtualBodies[i].position += virtualBodies[i].velocity * simulationTimeStep;
            }
            
            // Store path point for our body
            pathPoints.Add(new Vector3(
                virtualBodies[bodyIndex].position.x,
                virtualBodies[bodyIndex].position.y,
                0
            ));
        }
        
        // Draw path
        pathLine.positionCount = pathPoints.Count;
        pathLine.SetPositions(pathPoints.ToArray());
    }

    VirtualBody[] CreateVirtualBodies()
    {
        CelestialBody[] allBodies = FindObjectsByType<CelestialBody>(FindObjectsSortMode.None);
        VirtualBody[] virtuals = new VirtualBody[allBodies.Length];
        
        for (int i = 0; i < allBodies.Length; i++)
        {
            var rb = allBodies[i].Rigidbody2D;
            virtuals[i] = new VirtualBody
            {
                position = allBodies[i].transform.position,
                velocity = Vector2.zero, // Always start prediction from zero velocity
                mass = rb != null ? rb.mass : 1f
            };
        }
        
        return virtuals;
    }

    int FindBodyIndex(CelestialBody body)
    {
        CelestialBody[] allBodies = FindObjectsByType<CelestialBody>(FindObjectsSortMode.None);
        for (int i = 0; i < allBodies.Length; i++)
        {
            if (allBodies[i] == body) return i;
        }
        return 0;
    }

    Vector2 CalculateAcceleration(Vector2 point, int ignoreIndex, VirtualBody[] bodies)
    {
        Vector2 acceleration = Vector2.zero;
        
        for (int i = 0; i < bodies.Length; i++)
        {
            if (i == ignoreIndex) continue;
            
            Vector2 direction = bodies[i].position - point;
            float sqrDistance = direction.sqrMagnitude;
            
            if (sqrDistance < 0.01f) continue; // Avoid division by zero
            
            direction.Normalize();
            float forceMagnitude = SolarSystem.gravitationalConstant * bodies[i].mass / sqrDistance;
            acceleration += direction * forceMagnitude;
        }
        
        return acceleration;
    }

    public void StopVisualizingPath()
    {
        if (currentBody != null)
        {
            if (mainCam == null) { mainCam = Camera.main; }
            if (mainCam == null)
            {
                // No camera: just finalize without changing velocity
                currentBody = null;
            }
            else
            {
                Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = 0;
                Vector2 throwVector = CalculateThrowVector(mousePos);
                // Use ONLY the throw vector (not adding to existing velocity which is polluted by dragging)
                Vector2 newVelocity = throwVector * throwToVelocityScale;
                
                // Clamp to maximum velocity if set
                if (maxInitialVelocity > 0 && newVelocity.magnitude > maxInitialVelocity)
                {
                    newVelocity = newVelocity.normalized * maxInitialVelocity;
                    Debug.LogWarning($"[PathHandler] Velocity clamped from {(throwVector * throwToVelocityScale).magnitude:F2} to {maxInitialVelocity}");
                }
                
                // Debug log to see what velocity we're setting
                Debug.Log($"[PathHandler] Setting velocity: {newVelocity.magnitude:F2} (drag: {throwVector.magnitude:F2}, scale: {throwToVelocityScale})");
                
                currentBody.SetInitialVelocity(newVelocity);
                currentBody = null;
            }
        }
        
        if (pathLine != null)
        {
            pathLine.enabled = false;
        }
    }

    class VirtualBody
    {
        public Vector2 position;
        public Vector2 velocity;
        public float mass;
    }
}