using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpriteGenerator : MonoBehaviour
{
    [Header("Element Configuration")]
    public ElementData element;
    
    [Header("Sprite Settings")]
    public int spriteSize = 512;
    public float pixelsPerUnit = 100f;
    public float outlineWidth = 0.05f; // 5% of radius
    
    [Header("Text Settings")]
    public float textSizeScale = 0.25f; // Reduced from 0.45f
    public TMP_FontAsset fontAsset;
    
    private SpriteRenderer spriteRenderer;
    private TextMeshPro symbolText;

    void Start()
    {
        if (element != null)
        {
            GenerateElementSprite();
        }
        else
        {
            Debug.LogWarning($"No ElementData assigned to SpriteGenerator on {gameObject.name}");
        }
    }

    public void GenerateElementSprite()
    {
        if (element == null)
        {
            Debug.LogError("Cannot generate sprite: No ElementData assigned!");
            return;
        }

        // Ensure we have a sprite renderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Generate the circular sprite with outline and baked text
        Sprite sprite = CreateCircleSprite(element.color);
        spriteRenderer.sprite = sprite;

        Debug.Log($"Generated sprite for {element.elementName} ({element.atomicSymbol})");
    }

    private Sprite CreateCircleSprite(Color circleColor)
    {
        Texture2D texture = new Texture2D(spriteSize, spriteSize);
        Color[] pixels = new Color[spriteSize * spriteSize];

        float center = spriteSize / 2f;
        float radius = spriteSize / 2f - 2; // Leave 2 pixels padding
        float outlineThickness = radius * outlineWidth;
        float innerRadius = radius - outlineThickness;

        // Determine a contrasting color for the outline and text
        Color contrastColor = GetContrastColor(circleColor);

        for (int y = 0; y < spriteSize; y++)
        {
            for (int x = 0; x < spriteSize; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                int index = y * spriteSize + x;

                if (distance <= radius)
                {
                    if (distance >= innerRadius)
                    {
                        // Outline area - ensure full opacity
                        Color outlineColor = contrastColor;
                        outlineColor.a = 1f;
                        pixels[index] = outlineColor;
                    }
                    else
                    {
                        // Inner circle area - ensure full opacity
                        Color fillColor = circleColor;
                        fillColor.a = 1f;
                        pixels[index] = fillColor;
                    }
                }
                else
                {
                    // Outside circle - transparent
                    pixels[index] = new Color(0, 0, 0, 0);
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        // Draw the text onto the texture
        DrawTextOnTexture(texture, element.atomicSymbol, contrastColor);

        // Create sprite from texture
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, spriteSize, spriteSize),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit
        );

        return sprite;
    }

    private void DrawTextOnTexture(Texture2D texture, string text, Color textColor)
    {
        // Create a temporary texture for text rendering
        Texture2D tempTexture = new Texture2D(spriteSize, spriteSize, TextureFormat.ARGB32, false);
        Color[] clearPixels = new Color[spriteSize * spriteSize];
        for (int i = 0; i < clearPixels.Length; i++)
        {
            clearPixels[i] = new Color(0, 0, 0, 0);
        }
        tempTexture.SetPixels(clearPixels);
        tempTexture.Apply();

        // Use GUIStyle to render text
        RenderTexture renderTexture = RenderTexture.GetTemporary(spriteSize, spriteSize, 0, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;

        GL.Clear(true, true, new Color(0, 0, 0, 0));

        // Create a GUIStyle for text
        GUIStyle style = new GUIStyle();
        style.fontSize = Mathf.RoundToInt(spriteSize * textSizeScale);
        style.normal.textColor = textColor;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontStyle = FontStyle.Bold;

        // Begin GUI rendering
        GUI.matrix = Matrix4x4.identity;
        
        // Draw the text
        Rect textRect = new Rect(0, 0, spriteSize, spriteSize);
        
        // We need to use BeginGUI/EndGUI context
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, spriteSize, spriteSize, 0);
        
        // Manually render using GL
        Graphics.DrawTexture(new Rect(0, 0, spriteSize, spriteSize), Texture2D.blackTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, new Color(0, 0, 0, 0));
        
        GL.PopMatrix();

        // Read back the render texture
        Texture2D textTexture = new Texture2D(spriteSize, spriteSize, TextureFormat.ARGB32, false);
        textTexture.ReadPixels(new Rect(0, 0, spriteSize, spriteSize), 0, 0);
        textTexture.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);

        // Composite text onto the original texture using simple font rendering
        // Since Unity's GUI text rendering is complex, let's use a simpler bitmap font approach
        DrawSimpleText(texture, text, textColor);

        DestroyImmediate(tempTexture);
        DestroyImmediate(textTexture);
    }

    private void DrawSimpleText(Texture2D texture, string text, Color textColor)
    {
        // Calculate text size and position
        int fontSize = Mathf.RoundToInt(spriteSize * textSizeScale);
        int centerX = spriteSize / 2;
        int centerY = spriteSize / 2;

        // Calculate the maximum width (80% of the circle diameter to have padding)
        float circleRadius = spriteSize / 2f - 2;
        float maxTextWidth = circleRadius * 2f * 0.8f;

        // For each character, we'll draw a simple representation
        // Use a system sans-serif font
        Font arialFont = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
        if (arialFont == null) arialFont = Font.CreateDynamicFontFromOSFont("Helvetica", fontSize);
        if (arialFont == null) arialFont = Font.CreateDynamicFontFromOSFont("Sans-Serif", fontSize);

        // Request characters at initial size
        arialFont.RequestCharactersInTexture(text, fontSize, FontStyle.Bold);

        // Calculate initial text width
        float totalWidth = 0;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        
        foreach (char c in text)
        {
            CharacterInfo charInfo;
            if (arialFont.GetCharacterInfo(c, out charInfo, fontSize, FontStyle.Bold))
            {
                totalWidth += charInfo.advance;
                minY = Mathf.Min(minY, charInfo.minY);
                maxY = Mathf.Max(maxY, charInfo.maxY);
            }
        }

        // If text is too wide, scale down the font size
        if (totalWidth > maxTextWidth)
        {
            float scaleFactor = maxTextWidth / totalWidth;
            fontSize = Mathf.RoundToInt(fontSize * scaleFactor);
            
            // Re-request characters at new size
            arialFont.RequestCharactersInTexture(text, fontSize, FontStyle.Bold);
            
            // Recalculate width and height at new size
            totalWidth = 0;
            minY = float.MaxValue;
            maxY = float.MinValue;
            
            foreach (char c in text)
            {
                CharacterInfo charInfo;
                if (arialFont.GetCharacterInfo(c, out charInfo, fontSize, FontStyle.Bold))
                {
                    totalWidth += charInfo.advance;
                    minY = Mathf.Min(minY, charInfo.minY);
                    maxY = Mathf.Max(maxY, charInfo.maxY);
                }
            }
        }

        Color[] pixels = texture.GetPixels();
        
        float textCenterOffsetY = (maxY + minY) / 2f;

        float currentX = centerX - totalWidth / 2f;

        // Draw each character
        foreach (char c in text)
        {
            CharacterInfo charInfo;
            if (arialFont.GetCharacterInfo(c, out charInfo, fontSize, FontStyle.Bold))
            {
                // Get the character's UV coordinates from the font texture
                Texture2D fontTexture = arialFont.material.mainTexture as Texture2D;
                if (fontTexture != null)
                {
                    // Calculate character position - properly centered vertically
                    int charX = Mathf.RoundToInt(currentX + charInfo.minX);
                    int charY = Mathf.RoundToInt(centerY - charInfo.minY + textCenterOffsetY);
                    
                    int charWidth = charInfo.glyphWidth;
                    int charHeight = charInfo.glyphHeight;

                    // Sample from font texture and composite onto our texture
                    for (int y = 0; y < charHeight; y++)
                    {
                        for (int x = 0; x < charWidth; x++)
                        {
                            int targetX = charX + x;
                            int targetY = charY - y;

                            if (targetX >= 0 && targetX < spriteSize && targetY >= 0 && targetY < spriteSize)
                            {
                                // Sample from font texture (flip UV Y to correct upside-down text)
                                float uvX = charInfo.uvBottomLeft.x + (charInfo.uvBottomRight.x - charInfo.uvBottomLeft.x) * (x / (float)charWidth);
                                float uvY = charInfo.uvTopLeft.y - (charInfo.uvTopLeft.y - charInfo.uvBottomLeft.y) * (y / (float)charHeight);

                                Color fontPixel = fontTexture.GetPixelBilinear(uvX, uvY);
                                
                                // Use alpha from font as text opacity
                                if (fontPixel.a > 0.1f)
                                {
                                    int pixelIndex = targetY * spriteSize + targetX;
                                    if (pixelIndex >= 0 && pixelIndex < pixels.Length)
                                    {
                                        // Blend text color with background
                                        Color bgColor = pixels[pixelIndex];
                                        float alpha = fontPixel.a;
                                        pixels[pixelIndex] = Color.Lerp(bgColor, textColor, alpha);
                                    }
                                }
                            }
                        }
                    }
                }

                currentX += charInfo.advance;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
    }

    private Color GetContrastColor(Color backgroundColor)
    {
        // Calculate perceived brightness using weighted RGB values
        float brightness = (backgroundColor.r * 0.299f + 
                          backgroundColor.g * 0.587f + 
                          backgroundColor.b * 0.114f);
        
        // Return white for dark backgrounds, black for light backgrounds
        return brightness > 0.5f ? Color.black : Color.white;
    }

#if UNITY_EDITOR
    [ContextMenu("Generate Sprite Now")]
    public void GenerateSpriteInEditor()
    {
        GenerateElementSprite();
    }

    [ContextMenu("Save Sprite to Asset")]
    public void SaveSpriteAsAsset()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Debug.LogError("No sprite to save! Generate a sprite first.");
            return;
        }

        if (element == null)
        {
            Debug.LogError("No ElementData assigned!");
            return;
        }

        // Save the texture
        string path = $"Assets/Sprites/{element.atomicSymbol}_Icon.png";
        Texture2D texture = spriteRenderer.sprite.texture;
        byte[] bytes = texture.EncodeToPNG();
        
        System.IO.Directory.CreateDirectory("Assets/Sprites");
        System.IO.File.WriteAllBytes(path, bytes);
        
        AssetDatabase.Refresh();
        
        // Configure the texture import settings
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.ImportAsset(path);
        }

        // Load the created sprite and assign it to the element
        Sprite savedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (savedSprite != null)
        {
            element.icon = savedSprite;
            EditorUtility.SetDirty(element);
            AssetDatabase.SaveAssets();
            Debug.Log($"Sprite saved to {path} and assigned to {element.elementName}");
        }
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(SpriteGenerator))]
public class SpriteGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        SpriteGenerator generator = (SpriteGenerator)target;
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Generate Sprite", GUILayout.Height(30)))
        {
            generator.GenerateElementSprite();
        }
        
        if (GUILayout.Button("Save Sprite to Asset", GUILayout.Height(30)))
        {
            generator.SaveSpriteAsAsset();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Generate All Element Sprites", GUILayout.Height(40)))
        {
            GenerateAllElementSprites();
        }
    }
    
    private void GenerateAllElementSprites()
    {
        // Find all ElementData assets
        string[] guids = AssetDatabase.FindAssets("t:ElementData");
        
        if (guids.Length == 0)
        {
            Debug.LogWarning("No ElementData assets found!");
            return;
        }
        
        Debug.Log($"Found {guids.Length} element(s). Generating sprites...");
        
        // Create a temporary GameObject for generation
        GameObject tempObj = new GameObject("TempSpriteGenerator");
        SpriteGenerator tempGenerator = tempObj.AddComponent<SpriteGenerator>();
        
        // Copy settings from the current generator
        SpriteGenerator currentGenerator = (SpriteGenerator)target;
        tempGenerator.spriteSize = currentGenerator.spriteSize;
        tempGenerator.pixelsPerUnit = currentGenerator.pixelsPerUnit;
        tempGenerator.outlineWidth = currentGenerator.outlineWidth;
        tempGenerator.textSizeScale = currentGenerator.textSizeScale;
        tempGenerator.fontAsset = currentGenerator.fontAsset;
        
        int successCount = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ElementData elementData = AssetDatabase.LoadAssetAtPath<ElementData>(path);
            
            if (elementData != null)
            {
                tempGenerator.element = elementData;
                tempGenerator.GenerateElementSprite();
                tempGenerator.SaveSpriteAsAsset();
                successCount++;
            }
        }
        
        DestroyImmediate(tempObj);
        
        Debug.Log($"Successfully generated {successCount} element sprite(s)!");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif