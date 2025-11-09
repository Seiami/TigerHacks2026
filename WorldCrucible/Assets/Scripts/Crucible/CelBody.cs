using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime celestial body instance. Holds live physical properties (mass, radius, temperature, composition)
/// and derives a reference BodyData (template) that best matches its current composition + mass.
/// Similar idea to ElementIcon vs ElementData: BodyData is the authored asset; CelBody is the scene instance.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class CelBody : MonoBehaviour
{
    [Header("Live Properties")]
    [Tooltip("Mass in Earth masses (1 = Earth)."), Min(0f)] public float mass = 1f;
    [Tooltip("Radius in Earth radii (1 = Earth)."), Min(0f)] public float radius = 1f;
    [Tooltip("Surface / effective temperature in Kelvin.")] public float temperature = 288f;

    [Header("Composition (ratios sum ~1)")]
    public List<BodyData.ElementRatio> composition = new();

    [Header("Derived Template")]
    [Tooltip("Closest BodyData ScriptableObject chosen automatically unless locked.")] public BodyData bodyData;
    [Tooltip("If true, auto-matching will not overwrite bodyData.")] public bool lockBodyData = false;

    [Header("Matching Weights")]
    [Tooltip("Weight of mass difference in matching score.")] public float weightMass = 1f;
    [Tooltip("Weight of radius difference in matching score.")] public float weightRadius = 0.5f;
    [Tooltip("Weight of temperature difference in matching score.")] public float weightTemp = 0.25f;
    [Tooltip("Weight of composition difference in matching score.")] public float weightComposition = 2f;

    [Header("Catalog")]
    [Tooltip("If empty, will attempt Resources.LoadAll<BodyData>().")] public List<BodyData> catalog = new();
    [Tooltip("Recompute template on Start.")] public bool autoMatchOnStart = true;
    [Tooltip("Recompute template on Validate (editor changes)." )] public bool autoMatchOnValidate = true;

    [Header("Rigidbody Sync")]
    [Tooltip("Scale Rigidbody2D.mass from Earth mass units using this multiplier.")] public float physicsMassMultiplier = 1f; // tune for gameplay scale
    [Tooltip("Auto-apply mass to Rigidbody2D each frame (else call SyncPhysics)." )] public bool continuousPhysicsSync = false;

    private Rigidbody2D rb;
    private bool catalogLoaded = false;

    [Header("Crafting (Elements → Body)")]
    [Tooltip("If true, mass/composition are driven from craft inputs.")] public bool craftedMode = false;
    [Tooltip("Mass contributed per element (in Earth masses units for consistency).")]
    public List<CraftEntry> craftElements = new();
    [Tooltip("Density multiplier for radius estimate. 1 = baseline Earth-like density.")]
    public float densityFactor = 1f;

    [Serializable]
    public struct CraftEntry
    {
        public ElementData element;
        [Min(0f)] public float massContribution; // in Earth masses
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f; // We'll rely on custom gravity system
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
        EnsureCatalog();
    }

    void Start()
    {
        if (autoMatchOnStart && !lockBodyData)
        {
            MatchClosestBodyData();
        }
        SyncPhysics();
    }

    void Update()
    {
        if (continuousPhysicsSync)
        {
            SyncPhysics();
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        NormalizeComposition();
        if (craftedMode)
        {
            RecalculateFromCraft();
        }
        if (autoMatchOnValidate && !lockBodyData)
        {
            EnsureCatalog();
            MatchClosestBodyData();
        }
        SyncPhysics();
    }
#endif

    /// <summary>
    /// Ensure catalog list is populated if empty by loading all BodyData from Resources.
    /// </summary>
    void EnsureCatalog()
    {
        if (catalogLoaded) return;
        if (catalog == null) catalog = new List<BodyData>();
        if (catalog.Count == 0)
        {
            var loaded = Resources.LoadAll<BodyData>(string.Empty);
            catalog.AddRange(loaded);
        }
        catalogLoaded = true;
    }

    /// <summary>
    /// Normalize composition ratios to sum to 1 (if they don't already) without changing relative proportions.
    /// </summary>
    public void NormalizeComposition()
    {
        float sum = 0f;
        for (int i = 0; i < composition.Count; i++) sum += composition[i].ratio;
        if (sum <= 0f) return;
        for (int i = 0; i < composition.Count; i++)
        {
            var er = composition[i];
            er.ratio = er.ratio / sum;
            composition[i] = er;
        }
    }

    // ---------- Crafting API ----------
    [ContextMenu("Craft/Begin (clear + crafted mode)")]
    public void BeginCraft()
    {
        craftedMode = true;
        ClearCraft();
    }

    [ContextMenu("Craft/Clear")] 
    public void ClearCraft()
    {
        craftElements.Clear();
        mass = 0f;
        composition.Clear();
    }

    public void AddElementMass(ElementData element, float massEarthUnits)
    {
        if (element == null || massEarthUnits <= 0f) return;
        // Update craft list
        int idx = craftElements.FindIndex(e => e.element == element);
        if (idx >= 0)
        {
            var ce = craftElements[idx];
            ce.massContribution += massEarthUnits;
            craftElements[idx] = ce;
        }
        else
        {
            craftElements.Add(new CraftEntry { element = element, massContribution = massEarthUnits });
        }
        RecalculateFromCraft();
    }

    public void SetTemperatureFromCraft(float kelvin)
    {
        temperature = Mathf.Max(0f, kelvin);
    }

    public void RecalculateFromCraft()
    {
        // Mass is sum of contributions
        float newMass = 0f;
        foreach (var ce in craftElements) newMass += Mathf.Max(0f, ce.massContribution);
        mass = newMass;

        // Build composition ratios from contributions
        composition.Clear();
        if (newMass > 0f)
        {
            foreach (var ce in craftElements)
            {
                if (ce.element == null || ce.massContribution <= 0f) continue;
                composition.Add(new BodyData.ElementRatio
                {
                    element = ce.element,
                    ratio = Mathf.Clamp01(ce.massContribution / newMass)
                });
            }
            NormalizeComposition();
        }

        // Estimate radius from mass and density factor (very rough: radius ~ mass^(1/3) / density^(1/3))
        radius = EstimateRadiusFromMass(mass, densityFactor);

        if (!lockBodyData)
        {
            MatchClosestBodyData();
        }
        SyncPhysics();
    }

    public float EstimateRadiusFromMass(float massEarthUnits, float densityMultiplier)
    {
        massEarthUnits = Mathf.Max(0f, massEarthUnits);
        densityMultiplier = Mathf.Max(0.01f, densityMultiplier);
        float r = Mathf.Pow(massEarthUnits / densityMultiplier, 1f / 3f);
        // Earth baseline: if mass=1 and density=1 => radius ~1
        return r;
    }

    [ContextMenu("Craft/Finalize (lock + sync)")]
    public void FinalizeCraft()
    {
        craftedMode = false; // stop auto overrides unless desired
        NormalizeComposition();
        if (!lockBodyData) MatchClosestBodyData();
        SyncPhysics();
    }

    /// <summary>
    /// Compute a matching score (lower is better) between current live properties and a catalog BodyData.
    /// </summary>
    float ComputeScore(BodyData candidate)
    {
        if (candidate == null) return float.PositiveInfinity;
        float dMass = Mathf.Abs(candidate.mass - mass);
        float dRadius = Mathf.Abs(candidate.radius - radius);
        float dTemp = Mathf.Abs(candidate.temperature - temperature);
        float dComp = CompositionDifference(candidate);
        return dMass * weightMass + dRadius * weightRadius + dTemp * weightTemp + dComp * weightComposition;
    }

    /// <summary>
    /// Composition difference as sum of absolute ratio differences per element (missing elements counted fully).
    /// </summary>
    float CompositionDifference(BodyData candidate)
    {
        if (candidate == null) return 9999f;
        // Build dictionary of current
        var currentMap = new Dictionary<ElementData, float>();
        foreach (var er in composition)
        {
            if (er.element == null) continue;
            currentMap[ er.element ] = er.ratio;
        }
        float diff = 0f;
        foreach (var cer in candidate.composition)
        {
            if (cer.element == null) continue;
            float cur = currentMap.TryGetValue(cer.element, out var val) ? val : 0f;
            diff += Mathf.Abs(cer.ratio - cur);
            currentMap.Remove(cer.element);
        }
        // Any remaining elements only in current get added
        foreach (var kvp in currentMap)
        {
            diff += Mathf.Abs(kvp.Value - 0f);
        }
        return diff;
    }

    /// <summary>
    /// Select closest BodyData from catalog based on weighted differences.
    /// </summary>
    [ContextMenu("Match Closest BodyData")]
    public void MatchClosestBodyData()
    {
        EnsureCatalog();
        float bestScore = float.PositiveInfinity;
        BodyData best = null;
        for (int i = 0; i < catalog.Count; i++)
        {
            var cand = catalog[i];
            float score = ComputeScore(cand);
            if (score < bestScore)
            {
                bestScore = score;
                best = cand;
            }
        }
        if (best != null)
        {
            bodyData = best;
        }
    }

    /// <summary>
    /// Copy properties FROM current bodyData INTO live fields (if you want to re-template).
    /// </summary>
    [ContextMenu("Apply BodyData To Live Properties")]
    public void ApplyBodyDataToLive()
    {
        if (bodyData == null) return;
        mass = bodyData.mass;
        radius = bodyData.radius;
        temperature = bodyData.temperature;
        composition = new List<BodyData.ElementRatio>(bodyData.composition);
        NormalizeComposition();
        SyncPhysics();
    }

    /// <summary>
    /// Sync physics mass to Rigidbody2D using multiplier for scale.
    /// </summary>
    public void SyncPhysics()
    {
        if (rb == null) return;
        rb.mass = Mathf.Max(0.0001f, mass * physicsMassMultiplier);
    }
}
