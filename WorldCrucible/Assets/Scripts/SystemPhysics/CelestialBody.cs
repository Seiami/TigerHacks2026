using UnityEngine;
using System.Collections;

// Note: Removed [ExecuteInEditMode] as it interferes with runtime coroutines and isn't needed for gameplay
[RequireComponent(typeof(Rigidbody2D))]
public class CelestialBody : GravityObject
{
    public Vector2 initialVelocity;
    public bool useInitialVelocity = false;
    
    // NEW: Drag-to-spawn state
    [Header("Spawn Settings")]
    public bool isBeingPlaced = true; // Start true for newly spawned bodies
    
    private Vector2 velocity;
    private Rigidbody2D rb;
    private bool hasBeenReleased = false;
    private Collider2D col;
    // renamed to avoid duplicate declarations from previous version
    private Vector2 pendingVelocityInternal = Vector2.zero;
    private bool applyVelocityFlag = false;
    private bool enableColliderFlag = false;
    private Vector2 pendingVelocity = Vector2.zero;
    // private bool applyVelocityNextFrame = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        velocity = initialVelocity;
        
        // Cache collider
        col = GetComponent<Collider2D>();

        // Ensure rigidbody is set up correctly for gravity simulation
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic; // Start as kinematic while placing
            rb.gravityScale = 0f; // We handle gravity manually
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        // Disable collider while placing to avoid any accidental contacts
        if (isBeingPlaced && col != null)
        {
            col.enabled = false;
        }
    }

    void Update()
    {
        // Allow dragging while being placed
        if (isBeingPlaced && !hasBeenReleased)
        {
            FollowMouse();
        }
    }
    
    void FixedUpdate()
    {
        // Apply pending velocity after physics initialization
        if (applyVelocityFlag && rb != null)
        {
            rb.linearVelocity = pendingVelocityInternal;
            Debug.Log($"[CelestialBody] Applied velocity: {pendingVelocityInternal.magnitude:F2}");
            applyVelocityFlag = false;
        }

        // Enable collider after velocity is applied to avoid initial separation impulses
        if (enableColliderFlag && col != null)
        {
            col.enabled = true;
            enableColliderFlag = false;
        }
    }

    void FollowMouse()
    {
        if (Camera.main == null) return;
        
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        
        // Move using Rigidbody position to avoid physics velocity calculation
        if (rb != null)
        {
            // Use MovePosition while kinematic to avoid unintended velocity injection
            rb.MovePosition(mousePos);
            // Do NOT touch linearVelocity during placement; keep it at zero from Awake configuration
            rb.angularVelocity = 0f;
        }
        else
        {
            transform.position = mousePos;
        }
        
        // On mouse button down, start velocity visualization
        if (Input.GetMouseButtonDown(0))
        {
            if (CelestialPathHandler.Instance != null)
            {
                CelestialPathHandler.Instance.StartVisualizingPath(this);
            }
        }
        
        // On mouse button up, release the body
        if (Input.GetMouseButtonUp(0))
        {
            ReleaseCelestialBody();
        }
    }

    void ReleaseCelestialBody()
    {
        hasBeenReleased = true;
        isBeingPlaced = false;
        
        // Get the desired velocity FIRST (before changing bodyType)
        Vector2 desiredVelocity = Vector2.zero;
        
        if (CelestialPathHandler.Instance != null)
        {
            CelestialPathHandler.Instance.StopVisualizingPath();
            // The path handler has set initialVelocity via SetInitialVelocity
            desiredVelocity = initialVelocity;
        }
        else if (useInitialVelocity)
        {
            // Fallback: use preset initialVelocity
            desiredVelocity = initialVelocity;
        }

        // Optional: If Shift is held, snap to circular orbit around nearest central body
    // Use newer API to avoid obsolete warning
    var solarSystem = Object.FindFirstObjectByType<SolarSystem>();
        if (solarSystem != null && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            var central = solarSystem.FindNearestCentralBody(rb);
            if (central != null)
            {
                var circV = solarSystem.ComputeCircularOrbitVelocity(rb, central, 1f);
                if (circV != Vector2.zero)
                {
                    desiredVelocity = circV;
                    Debug.Log($"[CelestialBody] Shift-held: snapping to circular orbit around '{central.gameObject.name}' at speed {circV.magnitude:F2}");
                }
            }
        }
        
    Debug.Log($"[CelestialBody] Release: Switching to Dynamic, will apply velocity {desiredVelocity.magnitude:F2} next frame");
        
        // Now switch to Dynamic and schedule velocity application
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic; // Enable physics
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            
            // Schedule velocity to be applied in next FixedUpdate
            pendingVelocityInternal = desiredVelocity;
            applyVelocityFlag = true;

            // Enable collider next FixedUpdate after velocity application
            enableColliderFlag = true;
        }
    }
    
    public void SetInitialVelocity(Vector2 vel)
    {
        initialVelocity = vel;
        // Defer actual velocity application until release sequence sets body Dynamic
        // (CelestialBody.ReleaseCelestialBody schedules pendingVelocityInternal)
    }

    public Rigidbody2D Rigidbody2D => rb;
    public Vector2 Position => rb.position;
    public Vector2 Velocity => rb.linearVelocity;
}