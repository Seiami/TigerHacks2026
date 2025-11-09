using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(SpriteRenderer))]
public class CelestBody : MonoBehaviour
{
    // Similar to BodyData properties
    public string bodyName = "Unknown Body";
    public Sprite bodySprite;
    public List<ElementRatio> composition = new();
    public float temperature; // Kelvin
    public float radius;
    public float mass; // In terms of Earth Mass (1)

    // Reference to the closest matching template
    private BodyData closestTemplate;

    // Weights for comparison algorithm
    [Header("Comparison Weights")]
    [SerializeField] private float compositionWeight = 0.3f;
    [SerializeField] private float temperatureWeight = 0.1f;
    [SerializeField] private float massWeight = 0.5f;
    [SerializeField] private float radiusWeight = 0.05f;

    [System.Serializable]
    public struct ElementRatio
    {
        public ElementData element;
        [Range(0f, 1f)]
        public float ratio;

        public ElementRatio(ElementData element, float ratio)
        {
            this.element = element;
            this.ratio = ratio;
        }
    }

    /// <summary>
    /// Initialize the celestial body from crafting stage data
    /// </summary>
    /// <param name="elements">Dictionary of elements and their counts from the stage</param>
    /// <param name="temp">Temperature set by the user</param>
    public void Initialize(Dictionary<ElementData, int> elements, float temp)
    {
        temperature = temp;

        // Calculate composition ratios
        int totalElements = elements.Values.Sum();
        composition.Clear();

        foreach (var kvp in elements)
        {
            float ratio = (float)kvp.Value / totalElements;
            composition.Add(new ElementRatio(kvp.Key, ratio));
        }

        // Sort composition by ratio (descending) for easier comparison
        composition = composition.OrderByDescending(e => e.ratio).ToList();

        // Calculate estimated mass and radius based on composition
        CalculatePhysicalProperties();

        // Find the closest matching template
        closestTemplate = FindClosestBodyTemplate();

        // Apply visual properties from the template if found
        if (closestTemplate != null)
        {
            bodySprite = closestTemplate.bodySprite;
            bodyName = $"{closestTemplate.bodyName}-like Body";

            // Apply the sprite and recolor based on composition
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && bodySprite != null)
            {
                spriteRenderer.sprite = bodySprite;
                spriteRenderer.color = CalculateCompositionColor();
            }
        }
    }

    /// <summary>
    /// Calculate the weighted average color based on element composition
    /// </summary>
    /// <returns>Blended color representing the composition</returns>
    private Color CalculateCompositionColor()
    {
        Color blendedColor = Color.black;
        float totalRatio = 0f;

        foreach (var comp in composition)
        {
            if (comp.element != null)
            {
                // Add weighted color contribution
                blendedColor += comp.element.color * comp.ratio;
                totalRatio += comp.ratio;
            }
        }

        // Normalize if needed (should be close to 1.0 already)
        if (totalRatio > 0f)
        {
            blendedColor /= totalRatio;
        }

        // Ensure alpha is 1
        blendedColor.a = 1f;

        return blendedColor;
    }

    /// <summary>
    /// Calculate physical properties based on composition
    /// Uses element molar mass as a weight factor
    /// </summary>
    private void CalculatePhysicalProperties()
    {
        // Calculate weighted average mass based on element composition
        float weightedMass = 0f;
        foreach (var comp in composition)
        {
            if (comp.element != null)
            {
                weightedMass += comp.element.molarMass * comp.ratio;
            }
        }

        // Normalize to Earth mass scale (rough approximation)
        mass = weightedMass / 50f; // Arbitrary scaling factor

        // Radius correlates with mass (rough cube root relationship)
        radius = Mathf.Pow(mass, 1f / 3f);
    }

    /// <summary>
    /// Find the BodyData template that most closely matches this celestial body
    /// Uses weighted comparison of composition, temperature, mass, and radius
    /// </summary>
    /// <returns>The closest matching BodyData template</returns>
    private BodyData FindClosestBodyTemplate()
    {
        // Load all BodyData templates from Resources
        BodyData[] templates = Resources.LoadAll<BodyData>("CelestialBodies");

        if (templates.Length == 0)
        {
            Debug.LogWarning("No BodyData templates found in Resources/CelestialBodies");
            return null;
        }

        BodyData closest = null;
        float lowestScore = float.MaxValue;

        foreach (var template in templates)
        {
            // Skip templates where generated mass is more than 1000x the template's mass
            if (template.mass > 0 && mass > 1000f * template.mass)
                continue;

            float score = CalculateDifferenceScore(template);

            if (score < lowestScore)
            {
                lowestScore = score;
                closest = template;
            }
        }

        Debug.Log($"Closest template: {closest?.bodyName} with difference score: {lowestScore:F3}");
        return closest;
    }

    /// <summary>
    /// Calculate a weighted difference score between this body and a template
    /// Lower score means closer match
    /// </summary>
    private float CalculateDifferenceScore(BodyData template)
    {
        float compositionDiff = CalculateCompositionDifference(template);
        float temperatureDiff = CalculateTemperatureDifference(template);
        float massDiff = CalculateMassDifference(template);
        float radiusDiff = CalculateRadiusDifference(template);

        // Weighted sum of differences
        return (compositionDiff * compositionWeight) +
               (temperatureDiff * temperatureWeight) +
               (massDiff * massWeight) +
               (radiusDiff * radiusWeight);
    }

    /// <summary>
    /// Calculate composition difference using element-by-element comparison
    /// </summary>
    private float CalculateCompositionDifference(BodyData template)
    {
        float totalDifference = 0f;

        // Create dictionaries for easier lookup
        Dictionary<ElementData, float> myComp = composition
            .Where(e => e.element != null)
            .ToDictionary(e => e.element, e => e.ratio);

        Dictionary<ElementData, float> templateComp = template.composition
            .Where(e => e.element != null)
            .ToDictionary(e => e.element, e => e.ratio);

        // Get all unique elements from both compositions
        HashSet<ElementData> allElements = new HashSet<ElementData>(myComp.Keys);
        allElements.UnionWith(templateComp.Keys);

        // Calculate difference for each element
        foreach (var element in allElements)
        {
            float myRatio = myComp.ContainsKey(element) ? myComp[element] : 0f;
            float templateRatio = templateComp.ContainsKey(element) ? templateComp[element] : 0f;
            totalDifference += Mathf.Abs(myRatio - templateRatio);
        }

        return totalDifference;
    }

    /// <summary>
    /// Calculate normalized temperature difference
    /// </summary>
    private float CalculateTemperatureDifference(BodyData template)
    {
        if (temperature <= 0 || template.temperature <= 0)
            return 0f;

        // Normalize by the larger temperature to get a 0-1 range
        float maxTemp = Mathf.Max(temperature, template.temperature);
        return Mathf.Abs(temperature - template.temperature) / maxTemp;
    }

    /// <summary>
    /// Calculate normalized mass difference
    /// </summary>
    private float CalculateMassDifference(BodyData template)
    {
        if (mass <= 0 || template.mass <= 0)
            return 0f;

        float maxMass = Mathf.Max(mass, template.mass);
        return Mathf.Abs(mass - template.mass) / maxMass;
    }

    /// <summary>
    /// Calculate normalized radius difference
    /// </summary>
    private float CalculateRadiusDifference(BodyData template)
    {
        if (radius <= 0 || template.radius <= 0)
            return 0f;

        float maxRadius = Mathf.Max(radius, template.radius);
        return Mathf.Abs(radius - template.radius) / maxRadius;
    }

    /// <summary>
    /// Get the closest matching template (read-only access)
    /// </summary>
    public BodyData GetClosestTemplate()
    {
        return closestTemplate;
    }

    /// <summary>
    /// Get a formatted description of this celestial body
    /// </summary>
    public string GetDescription()
    {
        string desc = $"Body: {bodyName}\n";
        desc += $"Temperature: {temperature:F1} K\n";
        desc += $"Mass: {mass:F2} Earth masses\n";
        desc += $"Radius: {radius:F2} Earth radii\n";
        desc += "\nComposition:\n";

        foreach (var comp in composition)
        {
            if (comp.element != null)
            {
                desc += $"  {comp.element.elementName}: {comp.ratio * 100:F1}%\n";
            }
        }

        if (closestTemplate != null)
        {
            desc += $"\nMost similar to: {closestTemplate.bodyName}";
        }

        return desc;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only helper to randomize this body's properties & composition for testing.
    /// </summary>
    public void RandomizeTestBody()
    {
        // Random temperature (Kelvin)
        temperature = Random.Range(1f, 5000f);

        // Gather ElementData assets via AssetDatabase (editor-only) and delegate to shared routine
        string[] elementGuids = AssetDatabase.FindAssets("t:ElementData");
        List<ElementData> allElements = new List<ElementData>();
        foreach (var guid in elementGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var element = AssetDatabase.LoadAssetAtPath<ElementData>(path);
            if (element != null)
                allElements.Add(element);
        }

        RandomizeInternal(allElements);


    // Override radius and mass with specified random ranges, making high mass rare
    float massT = Random.value; // 0..1
    mass = Mathf.Lerp(0.0001f, 166515f, Mathf.Pow(massT, 5f)); // 5th power for extreme rarity
    radius = Random.Range(0.0001f, 54500f);

        // Re-apply to renderer if present
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && bodySprite != null)
        {
            spriteRenderer.sprite = bodySprite;
            spriteRenderer.color = CalculateCompositionColor();
        }
    }
#endif

    /// <summary>
    /// Runtime-safe randomization of this body's composition and visual appearance.
    /// Requires ElementData assets to be placed under a Resources folder (e.g. Resources/Elements).
    /// Call this from Play mode to randomize at runtime.
    /// </summary>
    /// <summary>
    /// Runtime randomization entry point.
    /// Randomizes composition, and also temperature and optional mass/radius tweak.
    /// Requires ElementData assets to be placed under a Resources folder (e.g. Resources/Elements).
    /// </summary>
    /// <param name="minTemp">Minimum temperature in Kelvin</param>
    /// <param name="maxTemp">Maximum temperature in Kelvin</param>
    /// <param name="randomizeMassScale">If true, applies a small random scale to mass (and recomputes radius)</param>
    public void RandomizeRuntime(float minTemp = 100f, float maxTemp = 5000f, bool randomizeMassScale = true)
    {
        // Load ElementData assets from Resources/Elements (adjust path if you store them elsewhere)
        ElementData[] loaded = Resources.LoadAll<ElementData>("Elements");
        List<ElementData> allElements = new List<ElementData>(loaded);

        if (allElements == null || allElements.Count == 0)
        {
            Debug.LogWarning("RandomizeRuntime: No ElementData found in Resources/Elements. Place ElementData assets under a Resources folder.");
            return;
        }

        // Randomize temperature first
        temperature = Random.Range(minTemp, maxTemp);

        // Delegate to shared randomizer for composition & visual
        RandomizeInternal(allElements);


    // Override radius and mass with specified random ranges, making high mass rare
    float massT = Random.value; // 0..1
    mass = Mathf.Lerp(0.001f, 166515f, Mathf.Pow(massT, 5f)); // 5th power for extreme rarity
    radius = Random.Range(0.0001f, 54500f);

        // Apply to renderer if present
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && bodySprite != null)
        {
            spriteRenderer.sprite = bodySprite;
            spriteRenderer.color = CalculateCompositionColor();
        }
    }

    /// <summary>
    /// Core randomization logic shared between editor-only and runtime entry points.
    /// </summary>
    private void RandomizeInternal(List<ElementData> allElements)
    {
        if (allElements == null || allElements.Count == 0)
        {
            Debug.LogWarning("RandomizeInternal: No element data provided.");
            return;
        }

        // Decide how many distinct elements to use (at least 1)
        int elementCount = Random.Range(2, Mathf.Min(4, allElements.Count + 1));
        // Shuffle list
        allElements = allElements.OrderBy(_ => Random.value).ToList();

        composition.Clear();
        float sumWeights = 0f;
        List<(ElementData el, float w)> tempWeights = new();

        for (int i = 0; i < elementCount; i++)
        {
            float w = Random.Range(0.05f, 1f); // ensure minimal presence
            tempWeights.Add((allElements[i], w));
            sumWeights += w;
        }

        // Normalize to ratios summing ~1
        foreach (var (el, w) in tempWeights)
        {
            composition.Add(new ElementRatio(el, w / sumWeights));
        }

        // Sort for consistency
        composition = composition.OrderByDescending(c => c.ratio).ToList();

        // Recalculate physical props
        CalculatePhysicalProperties();

        // Find closest template & apply
        closestTemplate = FindClosestBodyTemplate();
        if (closestTemplate != null)
        {
            bodySprite = closestTemplate.bodySprite;
            bodyName = $"{closestTemplate.bodyName}-like Body";
        }

        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && bodySprite != null)
        {
            spriteRenderer.sprite = bodySprite;
            spriteRenderer.color = CalculateCompositionColor();
        }

        Debug.Log($"Randomized CelestBody (runtime) -> Temp: {temperature:F1}K, Elements: {composition.Count}, Closest: {closestTemplate?.bodyName ?? "None"}");
    }
}
