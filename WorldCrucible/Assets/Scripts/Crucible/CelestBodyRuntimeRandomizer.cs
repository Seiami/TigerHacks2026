using UnityEngine;

public class CelestBodyRuntimeRandomizer : MonoBehaviour
{
    public CelestBody target;

    void Start()
    {
        if (target == null)
        {
            target = Object.FindFirstObjectByType<CelestBody>();
        }

        if (target != null)
        {
            target.RandomizeRuntime();
        }
        else
        {
            Debug.LogWarning("No CelestBody found to randomize.");
        }
    }

    // Optional: call this from a UI button to randomize on demand
    public void RandomizeNow()
    {
        if (target != null) target.RandomizeRuntime();
    }
}