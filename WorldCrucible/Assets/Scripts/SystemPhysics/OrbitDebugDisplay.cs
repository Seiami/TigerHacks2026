using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class OrbitDebugDisplay : MonoBehaviour
{
    [Header("Simulation Settings")]
    public int numSteps = 1000;
    public float timeStep = 0.1f;
    public bool usePhysicsTimeStep = false;

    [Header("Reference Frame")]
    public bool relativeToBody = true;
    public CelestialBody centralBody;

    [Header("Drawing Settings")]
    public float lineWidth = 0.02f;
    public bool useThickLines = false;
    public bool liveMode = true;

    private Dictionary<CelestialBody, List<Vector3>> liveTrails = new();

    void Start()
    {
        if (!Application.isPlaying)
            HideOrbits();
    }

    void Update()
    {
        if (!Application.isPlaying)
            DrawPredictiveOrbits();
    }

    void FixedUpdate()
{
    if (Application.isPlaying)
    {
        if (liveMode)
            DrawLiveOrbits();      // optional: keep if you want trails
        else
            DrawPredictiveOrbits(); // static predictive orbits during play
    }
}

    // =============================================================
    // 🛰 1. PREDICTIVE ORBIT SIMULATION (Edit Mode)
    // =============================================================
    void DrawPredictiveOrbits()
    {
        CelestialBody[] bodies = FindObjectsByType<CelestialBody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (bodies.Length < 2)
            return;

        if (relativeToBody && centralBody == null)
        {
            Debug.LogWarning("OrbitDebugDisplay: 'relativeToBody' enabled but no centralBody assigned.");
            relativeToBody = false;
        }

        // Initialize virtual copies
        VirtualBody[] virtualBodies = new VirtualBody[bodies.Length];
        Vector3[][] drawPoints = new Vector3[bodies.Length][];
        int referenceIndex = 0;
        Vector2 referenceInitialPos = Vector2.zero;

        for (int i = 0; i < bodies.Length; i++)
        {
            virtualBodies[i] = new VirtualBody(bodies[i]);
            drawPoints[i] = new Vector3[numSteps];

            if (relativeToBody && bodies[i] == centralBody)
            {
                referenceIndex = i;
                referenceInitialPos = virtualBodies[i].position;
            }
        }

        // Simulate forward
        for (int step = 0; step < numSteps; step++)
        {
            for (int i = 0; i < virtualBodies.Length; i++)
            {
                CelestialBody ignore = bodies[i];
                Vector2 accel = CalculateAcceleration(virtualBodies[i].position, ignore);
                virtualBodies[i].velocity += accel * timeStep;
            }

            Vector2 refPos = relativeToBody ? virtualBodies[referenceIndex].position : Vector2.zero;

            for (int i = 0; i < virtualBodies.Length; i++)
            {
                virtualBodies[i].position += virtualBodies[i].velocity * timeStep;

                Vector2 pos = virtualBodies[i].position;
                if (relativeToBody)
                {
                    Vector2 offset = refPos - referenceInitialPos;
                    pos -= offset;
                    if (i == referenceIndex)
                        pos = referenceInitialPos;
                }

                drawPoints[i][step] = new Vector3(pos.x, pos.y, 0);
            }
        }

        // Draw predicted orbits
        for (int i = 0; i < bodies.Length; i++)
        {
            CelestialBody body = bodies[i];
            if (body == null) continue;

            Color color = GetBodyColor(body);
            LineRenderer lr = body.GetComponentInChildren<LineRenderer>();

            if (useThickLines && lr)
            {
                lr.enabled = true;
                lr.positionCount = drawPoints[i].Length;
                lr.SetPositions(drawPoints[i]);
                lr.startColor = lr.endColor = color;
                lr.widthMultiplier = lineWidth;
            }
            else
            {
                for (int s = 0; s < drawPoints[i].Length - 1; s++)
                    Debug.DrawLine(drawPoints[i][s], drawPoints[i][s + 1], color);
                if (lr) lr.enabled = false;
            }
        }
    }

    // =============================================================
    // ☄️ 2. LIVE ORBIT TRAILS (Runtime)
    // =============================================================
    void DrawLiveOrbits()
    {
        CelestialBody[] bodies =FindObjectsByType<CelestialBody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (bodies.Length == 0)
            return;

        foreach (var body in bodies)
        {
            if (!liveTrails.ContainsKey(body))
                liveTrails[body] = new List<Vector3>();
        }

        Vector2 refPos = (relativeToBody && centralBody != null) ? centralBody.transform.position : Vector2.zero;

        foreach (var body in bodies)
        {
            if (body == null || !body.gameObject.activeInHierarchy)
                continue;

            Vector2 pos2D = body.transform.position;
            if (relativeToBody && centralBody != null)
                pos2D -= refPos;

            var trail = liveTrails[body];
            trail.Add(new Vector3(pos2D.x, pos2D.y, 0));

            if (trail.Count > numSteps)
                trail.RemoveAt(0);

            Color color = GetBodyColor(body);
            LineRenderer lr = body.GetComponentInChildren<LineRenderer>();

            if (lr)
            {
                lr.enabled = true;
                lr.positionCount = trail.Count;
                lr.SetPositions(trail.ToArray());
                lr.startColor = lr.endColor = color;
                lr.widthMultiplier = lineWidth;
            }
            else
            {
                for (int i = 1; i < trail.Count; i++)
                    Debug.DrawLine(trail[i - 1], trail[i], color);
            }
        }
    }

    // =============================================================
    // 🧮 Utilities
    // =============================================================
    public static Vector2 CalculateAcceleration(Vector2 point, CelestialBody ignoreBody = null)
{
    Vector2 acceleration = Vector2.zero;

    var bodies = FindObjectsByType<CelestialBody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    foreach (var body in bodies)
    {
        if (body == null || body == ignoreBody)
            continue;

        Rigidbody2D rb = body.GetComponent<Rigidbody2D>();
        if (rb == null)
            continue;

        Vector2 dir = body.transform.position - (Vector3)point;
        float sqrDst = dir.sqrMagnitude;
        if (sqrDst < 1e-6f)
            continue;

        dir.Normalize();
        acceleration += dir * SolarSystem.gravitationalConstant * rb.mass / sqrDst;
    }

    return acceleration;
}
    
    Color GetBodyColor(CelestialBody body)
    {
        var rend = body.GetComponentInChildren<MeshRenderer>();
        return rend ? rend.sharedMaterial.color : Color.white;
    }

    void HideOrbits()
    {
        foreach (var body in FindObjectsByType<CelestialBody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var lr = body.GetComponentInChildren<LineRenderer>();
            if (lr) lr.positionCount = 0;
        }
    }

    void OnValidate()
    {
        if (usePhysicsTimeStep)
            timeStep = Time.fixedDeltaTime;
    }

    // =============================================================
    // 📦 VirtualBody (for Predictive Mode)
    // =============================================================
    class VirtualBody
    {
        public Vector2 position;
        public Vector2 velocity;
        public float mass;

        public VirtualBody(CelestialBody body)
        {
            if (body == null) return;

            position = body.transform.position;
            Rigidbody2D rb = body.GetComponent<Rigidbody2D>();

            mass = (rb != null && rb.mass > 0f) ? rb.mass : 1f;

            if (body.useInitialVelocity)
                velocity = body.initialVelocity;
            else if (rb != null)
                velocity = rb.linearVelocity;
            else
                velocity = Vector2.zero;
        }
    }
}
