using UnityEngine;

public class CelestialBodySpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] celestialBodyPrefabs; // Assign your prefabs
    
    [Header("UI References")]
    public KeyCode[] spawnKeys; // E.g., 1, 2, 3 for different bodies
    
    private GameObject pendingBody;

    void LateUpdate()
    {
        // Clear reference once the body is released
        if (pendingBody != null)
        {
            CelestialBody cb = pendingBody.GetComponent<CelestialBody>();
            if (cb != null && !cb.isBeingPlaced)
            {
                pendingBody = null; // Body is now live, clear our reference
            }
        }
    }

    void Update()
    {
        // Spawn body on key press
        for (int i = 0; i < spawnKeys.Length && i < celestialBodyPrefabs.Length; i++)
        {
            if (Input.GetKeyDown(spawnKeys[i]))
            {
                SpawnBody(i);
            }
        }
    }

    void SpawnBody(int prefabIndex)
    {
        // Only destroy pending body if it's still being placed (not yet released)
        if (pendingBody != null)
        {
            CelestialBody pendingCB = pendingBody.GetComponent<CelestialBody>();
            if (pendingCB != null && pendingCB.isBeingPlaced)
            {
                Destroy(pendingBody);
            }
        }
        
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        
        pendingBody = Instantiate(celestialBodyPrefabs[prefabIndex], mousePos, Quaternion.identity);
        
        CelestialBody body = pendingBody.GetComponent<CelestialBody>();
        if (body != null)
        {
            body.isBeingPlaced = true;
        }
    }
}