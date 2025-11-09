using UnityEngine;

public class SolarSystem : MonoBehaviour
{
    public enum GravityInteractionMode { AllPairs, CentralMass, FilterByMass }
    [Header("Gravity Settings")]
    [Tooltip("Higher = stronger gravity. Tune with masses and scale.")]
    public static float gravitationalConstant = 10f; // was const; now tunable in Inspector
    GameObject[] bodies;
    [SerializeField] bool IsEllipticalOrbit = false;
    
    [Header("Simulation Tuning")]
    [Tooltip("Bodies closer than this distance won't interact (prevents singularities)")]
    [SerializeField] float minDistance = 0.5f; // Increased from 0.05 for better stability
    [Tooltip("Softening parameter to smooth gravity at close range")]
    [SerializeField] float softening = 0.2f;   // Increased from 0.02 for better stability
    [Tooltip("Refresh body list every N physics frames")]
    [SerializeField] int refreshInterval = 10;  // frames between refresh checks

    [Header("Safety Limits")]
    [Tooltip("Clamp body speed each frame (0 = no clamp)")]
    [SerializeField] float maxSpeed = 250f;

    [Tooltip("Cap the maximum gravitational force magnitude per pair (0 = no cap)")]
    [SerializeField] float maxForcePerPair = 0f;

    [Header("Interaction Mode")]
    [Tooltip("AllPairs = everyone attracts everyone. CentralMass = only central bodies influence others (configurable). FilterByMass = skip small-small interactions.")]
    [SerializeField] GravityInteractionMode interactionMode = GravityInteractionMode.AllPairs;
    [Tooltip("Tag that marks central attractors (used in CentralMass mode).")]
    [SerializeField] string centralTag = "Star";
    [Tooltip("Mass >= this value counts as central (used along with tag) in CentralMass mode.")]
    [SerializeField] float centralMassThreshold = 100f;
    [Tooltip("In CentralMass mode: central bodies exert gravity on others.")]
    [SerializeField] bool centralExertsOnOthers = true;
    [Tooltip("In CentralMass mode: non-central bodies exert gravity on central bodies.")]
    [SerializeField] bool othersExertOnCentral = false;
    [Tooltip("In CentralMass mode: central bodies attract each other.")]
    [SerializeField] bool centralToCentralAttract = false;
    [Tooltip("In CentralMass mode: non-central bodies attract each other.")]
    [SerializeField] bool nonCentralMutualAttraction = false;
    [Tooltip("In FilterByMass mode: interactions between two bodies with mass < smallMassThreshold are skipped.")]
    [SerializeField] float smallMassThreshold = 10f;

    int refreshCounter = 0;

    // Cross-frame diagnostics
    System.Collections.Generic.Dictionary<int, Vector2> lastVelocity = new System.Collections.Generic.Dictionary<int, Vector2>();
    System.Collections.Generic.Dictionary<int, Vector2> lastPredictedDv = new System.Collections.Generic.Dictionary<int, Vector2>();
    System.Collections.Generic.HashSet<int> clampedLastFrame = new System.Collections.Generic.HashSet<int>();

    void Start()
    {
        // Ensure we use standard physics stepping
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;

        // Proactively disable any legacy gravity/throw scripts that may inject forces
        DisableLegacyGravityScripts();

        RefreshBodies();
        // Don't set initial velocity here - let user do it
    }

    void FixedUpdate()
    {
        // Refresh bodies list when new ones are added
        GameObject[] currentBodies = GameObject.FindGameObjectsWithTag("CelestialBodies");
        if (currentBodies.Length != bodies.Length || (++refreshCounter >= refreshInterval))
        {
            RefreshBodies();
            refreshCounter = 0;
        }

        // Evaluate residuals from last frame (after physics has integrated)
        EvaluateResiduals();

        // Apply gravity each physics step
        Gravity();
    }

    void RefreshBodies()
    {
        bodies = GameObject.FindGameObjectsWithTag("CelestialBodies");
        // Sanitize newly found bodies (disable legacy components on them)
        for (int i = 0; i < bodies.Length; i++)
        {
            if (bodies[i] == null) continue;
            DisableLegacyScriptsOnObject(bodies[i]);
        }
    }

    void DisableLegacyGravityScripts()
    {
        int disabledCount = 0;

        // Disable legacy GravityHandler if present
        var legacyHandlers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in legacyHandlers)
        {
            if (mb == null) continue;
            var typeName = mb.GetType().Name;
            if (typeName == "GravityHandler" || typeName == "Graviton" || typeName == "Throwable" ||
                typeName == "PathHandler" || typeName == "CircleOrbit" || typeName == "GridController")
            {
                if (mb.enabled)
                {
                    mb.enabled = false;
                    disabledCount++;
                    Debug.LogWarning($"[SolarSystem] Disabled legacy script: {typeName} on {mb.gameObject.name}");
                }
            }
        }

        if (disabledCount > 0)
        {
            Debug.Log($"[SolarSystem] Disabled {disabledCount} legacy gravity-related components to prevent double-forces.");
        }
    }

    void DisableLegacyScriptsOnObject(GameObject go)
    {
        if (go == null) return;
        var mbs = go.GetComponents<MonoBehaviour>();
        foreach (var mb in mbs)
        {
            if (mb == null) continue;
            var typeName = mb.GetType().Name;
            if (typeName == "GravityHandler" || typeName == "Graviton" || typeName == "Throwable" ||
                typeName == "PathHandler" || typeName == "CircleOrbit" || typeName == "GridController")
            {
                if (mb.enabled)
                {
                    mb.enabled = false;
                    Debug.LogWarning($"[SolarSystem] Disabled legacy script on body: {typeName} on {go.name}");
                }
            }
        }
    }

    void SetInitialVelocity()
    {
        foreach (GameObject a in bodies)
        {
            Vector2 totalVelocity = Vector2.zero;

            var cbA = a.GetComponent<CelestialBody>();
            Rigidbody2D rbA = a.GetComponent<Rigidbody2D>();
            if (cbA != null && cbA.useInitialVelocity == true)
            {
                rbA.linearVelocity = cbA.initialVelocity;
            }

            foreach (GameObject b in bodies)
            {
                if (!a.Equals(b))
                {
                    Rigidbody2D rbB = b.GetComponent<Rigidbody2D>();
                    float m2 = rbB.mass;

                    Vector2 direction = ((Vector2)b.transform.position - (Vector2)a.transform.position);
                    float r = direction.magnitude;
                    direction.Normalize();

                    Vector2 perpendicular = new Vector2(-direction.y, direction.x);
                    float speed;

                    if (IsEllipticalOrbit)
                    {
                        // Eliptic orbit = G * M  ( 2 / r + 1 / a) where G is the gravitational constant, M is the mass of the central object, r is the distance between the two bodies
                        // and a is the length of the semi major axis (!!! NOT GAMEOBJECT a !!!)
                        speed = Mathf.Sqrt((gravitationalConstant * m2) * ((2 / r) - (1 / (r * 1.5f))));
                    }
                    else
                    {
                        //Circular Orbit = ((G * M) / r)^0.5, where G = gravitational constant, M is the mass of the central object and r is the distance between the two objects
                        //We ignore the mass of the orbiting object when the orbiting object's mass is negligible, like the mass of the earth vs. mass of the sun
                        speed = Mathf.Sqrt((gravitationalConstant * m2) / r);
                    }
                    totalVelocity += perpendicular * speed;
                }
            }
            rbA.linearVelocity = totalVelocity;
        }
    }

    void Gravity()
    {
        // Build a compact list of active rigidbodies (only Dynamic bodies, skip Kinematic/Static)
        var activeBodies = new System.Collections.Generic.List<Rigidbody2D>(bodies.Length);
        for (int i = 0; i < bodies.Length; i++)
        {
            if (bodies[i] == null) continue;
            var rb = bodies[i].GetComponent<Rigidbody2D>();
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
                activeBodies.Add(rb);
        }

        int n = activeBodies.Count;
        if (n < 2) return;

        bool debugLog = Time.frameCount % 60 == 0;
        if (debugLog)
        {
            Debug.Log($"[SolarSystem] Applying gravity to {n} bodies. G={gravitationalConstant}");
        }

        float minDist2 = minDistance * minDistance;

        var prevVel = new Vector2[n];
        var forceAccum = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            prevVel[i] = activeBodies[i].linearVelocity;
            forceAccum[i] = Vector2.zero;
        }

        for (int i = 0; i < n - 1; i++)
        {
            var rbA = activeBodies[i];
            Vector2 posA = rbA.position;
            float m1 = rbA.mass;
            bool isCentralA = IsCentral(rbA.gameObject, m1);

            for (int j = i + 1; j < n; j++)
            {
                var rbB = activeBodies[j];
                Vector2 delta = rbB.position - posA;
                float r2 = delta.sqrMagnitude;
                if (r2 < minDist2)
                    continue;

                float softenedR2 = r2 + softening * softening;
                float invR = 1f / Mathf.Sqrt(softenedR2);
                Vector2 dir = delta * invR;

                float m2 = rbB.mass;
                bool isCentralB = IsCentral(rbB.gameObject, m2);

                float forceMag = gravitationalConstant * (m1 * m2) / softenedR2;
                if (maxForcePerPair > 0f && forceMag > maxForcePerPair)
                {
                    if (debugLog && i == 0 && j == 1)
                        Debug.Log($"  Force capped: original={forceMag:F1} -> cap={maxForcePerPair:F1}");
                    forceMag = maxForcePerPair;
                }
                Vector2 force = dir * forceMag;

                if (debugLog && i == 0 && j == 1)
                {
                    Debug.Log($"  Body pair: m1={m1}, m2={m2}, dist={Mathf.Sqrt(r2):F2}, force={force.magnitude:F3}");
                }

                if (interactionMode == GravityInteractionMode.AllPairs)
                {
                    forceAccum[i] += force;
                    forceAccum[j] -= force;
                }
                else if (interactionMode == GravityInteractionMode.CentralMass)
                {
                    if (isCentralA && !isCentralB)
                    {
                        if (centralExertsOnOthers) forceAccum[j] -= force;
                        if (othersExertOnCentral) forceAccum[i] += force;
                    }
                    else if (isCentralB && !isCentralA)
                    {
                        if (centralExertsOnOthers) forceAccum[i] += force;
                        if (othersExertOnCentral) forceAccum[j] -= force;
                    }
                    else if (isCentralA && isCentralB)
                    {
                        if (centralToCentralAttract)
                        {
                            forceAccum[i] += force;
                            forceAccum[j] -= force;
                        }
                    }
                    else
                    {
                        if (nonCentralMutualAttraction)
                        {
                            forceAccum[i] += force;
                            forceAccum[j] -= force;
                        }
                    }
                }
                else if (interactionMode == GravityInteractionMode.FilterByMass)
                {
                    bool bothSmall = (m1 < smallMassThreshold) && (m2 < smallMassThreshold);
                    if (!bothSmall)
                    {
                        forceAccum[i] += force;
                        forceAccum[j] -= force;
                    }
                }
            }
        }

        for (int i = 0; i < n; i++)
        {
            if (forceAccum[i] != Vector2.zero)
            {
                activeBodies[i].AddForce(forceAccum[i]);
            }
        }

        clampedLastFrame.Clear();
        if (maxSpeed > 0f)
        {
            for (int i = 0; i < n; i++)
            {
                var rb = activeBodies[i];
                float v = rb.linearVelocity.magnitude;
                if (v > maxSpeed)
                {
                    Debug.LogWarning($"[SolarSystem] Speed clamp on {rb.gameObject.name}: {v:F2} -> {maxSpeed:F2}");
                    rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
                    clampedLastFrame.Add(rb.GetInstanceID());
                }
            }
        }

        float dt = Time.fixedDeltaTime;
        for (int i = 0; i < n; i++)
        {
            var rb = activeBodies[i];
            int id = rb.GetInstanceID();
            lastVelocity[id] = rb.linearVelocity;
            lastPredictedDv[id] = (forceAccum[i] / rb.mass) * dt;
        }
    }

    bool IsCentral(GameObject go, float mass)
    {
        if (interactionMode != GravityInteractionMode.CentralMass) return false;
        if (go != null && go.CompareTag(centralTag)) return true;
        if (mass >= centralMassThreshold) return true;
        return false;
    }

        // Utilities and accessors
        public float GetGravitationalConstant() => gravitationalConstant;
        public GravityInteractionMode GetInteractionMode() => interactionMode;
        public string GetCentralTag() => centralTag;
        public float GetCentralMassThreshold() => centralMassThreshold;
        public bool GetCentralExertsOnOthers() => centralExertsOnOthers;
        public bool GetOthersExertOnCentral() => othersExertOnCentral;
        public bool GetCentralToCentralAttract() => centralToCentralAttract;
        public bool GetNonCentralMutualAttraction() => nonCentralMutualAttraction;
        public float GetSmallMassThreshold() => smallMassThreshold;

        // Compute the ideal circular orbit velocity for 'orbiting' around 'central'
        public Vector2 ComputeCircularOrbitVelocity(Rigidbody2D orbiting, Rigidbody2D central, float directionSign = 1f)
        {
            if (orbiting == null || central == null) return Vector2.zero;
            Vector2 r = orbiting.position - central.position;
            float dist = r.magnitude;
            if (dist <= 1e-4f) return Vector2.zero;
            Vector2 ur = r / dist;
            Vector2 ut = new Vector2(-ur.y, ur.x) * Mathf.Sign(directionSign);
            // v = sqrt(G*M / r)
            float v = Mathf.Sqrt(Mathf.Max(0.0f, gravitationalConstant * central.mass / dist));
            return ut * v;
        }

        // Find nearest central body relative to 'orbiting'
        public Rigidbody2D FindNearestCentralBody(Rigidbody2D orbiting)
        {
            if (orbiting == null || bodies == null) return null;
            Rigidbody2D best = null;
            float bestD2 = float.PositiveInfinity;
            for (int i = 0; i < bodies.Length; i++)
            {
                var go = bodies[i];
                if (go == null) continue;
                var rb = go.GetComponent<Rigidbody2D>();
                if (rb == null || rb == orbiting) continue;
                if (!IsCentral(go, rb.mass)) continue;
                float d2 = (rb.position - orbiting.position).sqrMagnitude;
                if (d2 < bestD2)
                {
                    bestD2 = d2;
                    best = rb;
                }
            }
            return best;
        }

    void EvaluateResiduals()
    {
        // Compare actual delta-V this frame to last frame's predicted
        const float impulseWarnThreshold = 20f; // units/s of unexplained delta per frame
        var activeBodies = new System.Collections.Generic.List<Rigidbody2D>(bodies.Length);
        for (int i = 0; i < bodies.Length; i++)
        {
            if (bodies[i] == null) continue;
            var rb = bodies[i].GetComponent<Rigidbody2D>();
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
                activeBodies.Add(rb);
        }

        foreach (var rb in activeBodies)
        {
            int id = rb.GetInstanceID();
            if (!lastVelocity.ContainsKey(id) || !lastPredictedDv.ContainsKey(id))
                continue;

            // actual dv since last frame
            Vector2 prev = lastVelocity[id];
            Vector2 actualDv = rb.linearVelocity - prev;
            Vector2 predictedDv = lastPredictedDv[id];
            Vector2 residual = actualDv - predictedDv;
            float rmag = residual.magnitude;

            if (rmag > impulseWarnThreshold)
            {
                bool wasClamped = clampedLastFrame.Contains(id);
                string reason = wasClamped ? "(likely due to speed clamp/collision saturation)" : "(external impulse suspected)";
                Debug.LogWarning($"[SolarSystem] Delta-V residual on {rb.gameObject.name}: residual dV={rmag:F2} actual={actualDv.magnitude:F2} predicted={predictedDv.magnitude:F2} {reason}");
            }
        }
    }
}
