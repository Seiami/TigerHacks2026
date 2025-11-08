using Unity.VisualScripting;
using UnityEngine;

public class SpriteGenerator : MonoBehaviour
{
    public ElementData element; // Reference to your ScriptableObject
    public int spriteWidth = 512;
    public int spriteHeight = 512;
    public float pixelsPerUnit = 100.0f;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (element != null)
        {
            GenerateAndSetSprite(element.color);
        }
        else
        {
            Debug.LogError("ColorData asset not assigned!");
        }

        EnsureSymbol();
    }

    void GenerateAndSetSprite(Color color)
    {
        Texture2D texture = new Texture2D(spriteWidth, spriteHeight, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        int w = spriteWidth;
        int h = spriteHeight;
        float cx = (w - 1) * 0.5f;
        float cy = (h - 1) * 0.5f;
        float radius = Mathf.Min(w, h) * 0.48f; // 48% to leave room for anti-aliasing
        float outlineWidth = Mathf.Max(3f, radius * 0.08f); // outline is 8% of radius
        float aaWidth = 2f; // anti-aliasing edge width in pixels

        Color[] pixels = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Outside circle
                if (dist > radius + aaWidth)
                {
                    pixels[y * w + x] = Color.clear;
                }
                // Outer edge anti-aliasing (outline color fading out)
                else if (dist > radius)
                {
                    float alpha = 1f - ((dist - radius) / aaWidth);
                    Color outlineColor = symbolColor;
                    outlineColor.a = alpha;
                    pixels[y * w + x] = outlineColor;
                }
                // Outline (same color as text)
                else if (dist > radius - outlineWidth)
                {
                    pixels[y * w + x] = symbolColor;
                }
                // Inner edge anti-aliasing (blend outline to fill)
                else if (dist > radius - outlineWidth - aaWidth)
                {
                    float t = (dist - (radius - outlineWidth - aaWidth)) / aaWidth;
                    pixels[y * w + x] = Color.Lerp(color, symbolColor, t);
                }
                // Inside circle (filled with element color)
                else
                {
                    pixels[y * w + x] = color;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        Rect rect = new Rect(0, 0, w, h);
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        Sprite newSprite = Sprite.Create(texture, rect, pivot, pixelsPerUnit);
        spriteRenderer.sprite = newSprite;
    }

    [SerializeField] private string fallbackSymbol = ""; // used if element doesn't have a symbol field
    [SerializeField] private float textSizeScale = 0.4f; // text will be 40% of sprite size
    [SerializeField] private Color symbolColor = Color.white;

    private TMPro.TextMeshPro symbolText;

    void EnsureSymbol()
    {
        if (symbolText == null)
        {
            var existing = transform.Find("Symbol");
            if (existing != null)
            {
                symbolText = existing.GetComponent<TMPro.TextMeshPro>();
            }
            else
            {
                var go = new GameObject("Symbol");
                go.transform.SetParent(transform, false);
                symbolText = go.AddComponent<TMPro.TextMeshPro>();
                // Center it over the sprite
                symbolText.alignment = TMPro.TextAlignmentOptions.Center;
                symbolText.rectTransform.sizeDelta = Vector2.zero;
                symbolText.enableAutoSizing = false;
            }

            // Ensure it renders above the sprite
            var mr = symbolText.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerID = spriteRenderer.sortingLayerID;
                mr.sortingOrder = spriteRenderer.sortingOrder + 1;
            }
        }

        string symbol = fallbackSymbol;
        // If your ElementData has a symbol field, prefer it:
        if (element != null && !string.IsNullOrEmpty(element.atomicSymbol))
        {
            symbol = element.atomicSymbol;
        }

        symbolText.text = symbol;
        // Calculate font size based on sprite size in world units
        float spriteSize = Mathf.Min(spriteWidth, spriteHeight) / pixelsPerUnit;
        symbolText.fontSize = spriteSize * textSizeScale * 10f; // scale factor for TMP
        symbolText.color = symbolColor;
        symbolText.transform.localPosition = Vector3.zero;
    }
}

