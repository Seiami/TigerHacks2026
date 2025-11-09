using UnityEngine;

public class CelestBodySpawnerRuntime : MonoBehaviour
{
    public GameObject celestBodyPrefab; // prefab must contain CelestBody component
    public int spawnCount = 3;
    public float spawnRadius = 10f;

    void Start()
    {
        // Determine spawn count; require an odd count so one body sits exactly at center
        int count = spawnCount;
        if (count <= 0) count = 1;
        if (count % 2 == 0)
        {
            Debug.LogWarning($"CelestBodySpawnerRuntime: spawnCount {spawnCount} is even — incrementing to {spawnCount + 1} so a body can be placed exactly at screen center.");
            count = spawnCount + 1;
        }

        // World-space center (camera center). Fallback to origin if no camera found.
        Camera cam = Camera.main;
        Vector3 center = Vector3.zero;
        if (cam != null)
        {
            center = cam.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f, cam.nearClipPlane));
            center.z = 0f;
        }

        // If spawnRadius is zero, all bodies at center
        if (spawnRadius <= 0f)
        {
            for (int i = 0; i < count; i++)
            {
                var go = Instantiate(celestBodyPrefab, center, Quaternion.identity);
                var cb = go.GetComponent<CelestBody>();
                if (cb != null) cb.RandomizeRuntime();
            }
            return;
        }

        // Compute evenly spaced positions across [-spawnRadius, spawnRadius]
        float totalWidth = spawnRadius * 2f;
        float step = (count > 1) ? (totalWidth / (count - 1)) : 0f;

        for (int i = 0; i < count; i++)
        {
            float x = center.x - spawnRadius + (i * step);
            Vector3 pos = new Vector3(x, center.y, 0f);
            var go = Instantiate(celestBodyPrefab, pos, Quaternion.identity);
            var cb = go.GetComponent<CelestBody>();
            if (cb != null) cb.RandomizeRuntime();
        }
    }
}