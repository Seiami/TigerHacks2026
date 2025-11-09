using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// Runtime UI for selecting CelestBody by clicking and randomizing it.
/// Creates a simple Canvas if none exists and provides buttons for: Randomize Selected, Randomize All.
/// </summary>
public class CelestBodyRuntimeUI : MonoBehaviour
{
    public Camera uiCamera; // optional; defaults to Camera.main
    public Font uiFont;

    // UI elements
    Canvas runtimeCanvas;
    Text selectedText;
    Text descriptionTextLeft;
    Text descriptionTextRight;
    Button randomizeSelectedButton;
    Button randomizeAllButton;

    CelestBody selectedBody;
    CelestBody firstBody;

    void Start()
    {
        if (uiCamera == null) uiCamera = Camera.main;
        CreateUI();

        // Cache the first CelestBody in the scene and update description
        var arr = Object.FindObjectsByType<CelestBody>(FindObjectsSortMode.None);
        firstBody = (arr != null && arr.Length > 0) ? arr[0] : null;
        RefreshFirstBodyDescription();
    }

    void Update()
    {
        // Left-click to select a CelestBody in world space
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mp = Input.mousePosition;
            Vector3 world = uiCamera.ScreenToWorldPoint(mp);
            Vector2 pw = new Vector2(world.x, world.y);
            RaycastHit2D hit = Physics2D.Raycast(pw, Vector2.zero);
            if (hit.collider != null)
            {
                var cb = hit.collider.GetComponent<CelestBody>();
                if (cb != null)
                {
                    SelectBody(cb);
                }
            }
        }
    }

    void SelectBody(CelestBody cb)
    {
        selectedBody = cb;
        if (selectedText != null)
            selectedText.text = $"Selected: {cb.gameObject.name}";
    }

    void CreateUI()
    {
        // Try to find an existing Canvas
        runtimeCanvas = FindFirstObjectByType<Canvas>();
        if (runtimeCanvas == null)
        {
            GameObject cgo = new GameObject("RuntimeCanvas");
            runtimeCanvas = cgo.AddComponent<Canvas>();
            runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
        }

        // Create panel
        GameObject panel = new GameObject("RuntimePanel");
        panel.transform.SetParent(runtimeCanvas.transform, false);
        var img = panel.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.4f);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(10f, -10f);
    rt.sizeDelta = new Vector2(220f, 110f);

        // Selected text
        GameObject tgo = new GameObject("SelectedText");
        tgo.transform.SetParent(panel.transform, false);
        selectedText = tgo.AddComponent<Text>();
        selectedText.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
        selectedText.fontSize = 14;
        selectedText.alignment = TextAnchor.UpperLeft;
        RectTransform trt = selectedText.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0f, 1f);
        trt.anchoredPosition = new Vector2(8f, -8f);
    trt.sizeDelta = new Vector2(320f, 32f);
        selectedText.text = "Selected: (none)";

        // Randomize Selected button
        randomizeSelectedButton = CreateButton(panel.transform, "Randomize Selected", new Vector2(8f, -48f));
        randomizeSelectedButton.onClick.AddListener(() => {
            if (selectedBody != null) selectedBody.RandomizeRuntime();
            // refresh description for the first body in case it was randomized
            RefreshFirstBodyDescription();
        });

        // Randomize All button
        randomizeAllButton = CreateButton(panel.transform, "Randomize All", new Vector2(8f, -78f));
        randomizeAllButton.onClick.AddListener(() => {
            var all = Object.FindObjectsByType<CelestBody>(FindObjectsSortMode.None);
            foreach (var c in all) c.RandomizeRuntime();
            // update cached first body and refresh description
            var arr2 = Object.FindObjectsByType<CelestBody>(FindObjectsSortMode.None);
            firstBody = (arr2 != null && arr2.Length > 0) ? arr2[0] : null;
            RefreshFirstBodyDescription();
        });

    // Create a top-centered description panel (center top of screen)
    GameObject topPanel = new GameObject("TopDescriptionPanel");
    topPanel.transform.SetParent(runtimeCanvas.transform, false);
    var topImg = topPanel.AddComponent<Image>();
    topImg.color = new Color(0f, 0f, 0f, 0.6f);
    RectTransform tprt = topPanel.GetComponent<RectTransform>();
    tprt.anchorMin = new Vector2(0.5f, 1f);
    tprt.anchorMax = new Vector2(0.5f, 1f);
    tprt.pivot = new Vector2(0.5f, 1f);
    tprt.anchoredPosition = new Vector2(0f, -10f);
    tprt.sizeDelta = new Vector2(640f, 140f);

    // Two-column description text inside top panel; both auto-resize to fit their half
    // Left column
    GameObject leftGo = new GameObject("FirstBodyDescriptionLeft");
    leftGo.transform.SetParent(topPanel.transform, false);
    descriptionTextLeft = leftGo.AddComponent<Text>();
    descriptionTextLeft.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
    descriptionTextLeft.fontSize = 16;
    descriptionTextLeft.alignment = TextAnchor.UpperLeft;
    descriptionTextLeft.horizontalOverflow = HorizontalWrapMode.Wrap;
    descriptionTextLeft.verticalOverflow = VerticalWrapMode.Overflow;
    descriptionTextLeft.resizeTextForBestFit = true;
    descriptionTextLeft.resizeTextMinSize = 10;
    descriptionTextLeft.resizeTextMaxSize = 20;
    RectTransform lrt = descriptionTextLeft.GetComponent<RectTransform>();
    lrt.anchorMin = new Vector2(0f, 0f);
    lrt.anchorMax = new Vector2(0.5f, 1f);
    lrt.pivot = new Vector2(0f, 0.5f);
    lrt.anchoredPosition = new Vector2(8f, -10f);
    lrt.offsetMin = new Vector2(8f, 8f);
    lrt.offsetMax = new Vector2(-4f, -8f);
    descriptionTextLeft.text = "First body: (none)";

    // Right column
    GameObject rightGo = new GameObject("FirstBodyDescriptionRight");
    rightGo.transform.SetParent(topPanel.transform, false);
    descriptionTextRight = rightGo.AddComponent<Text>();
    descriptionTextRight.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
    descriptionTextRight.fontSize = 16;
    descriptionTextRight.alignment = TextAnchor.UpperLeft;
    descriptionTextRight.horizontalOverflow = HorizontalWrapMode.Wrap;
    descriptionTextRight.verticalOverflow = VerticalWrapMode.Overflow;
    descriptionTextRight.resizeTextForBestFit = true;
    descriptionTextRight.resizeTextMinSize = 10;
    descriptionTextRight.resizeTextMaxSize = 20;
    RectTransform rrt = descriptionTextRight.GetComponent<RectTransform>();
    rrt.anchorMin = new Vector2(0.5f, 0f);
    rrt.anchorMax = new Vector2(1f, 1f);
    rrt.pivot = new Vector2(1f, 0.5f);
    rrt.anchoredPosition = new Vector2(-8f, -10f);
    rrt.offsetMin = new Vector2(4f, 8f);
    rrt.offsetMax = new Vector2(-8f, -8f);
    descriptionTextRight.text = "";
    }

    void RefreshFirstBodyDescription()
    {
        if (descriptionTextLeft == null || descriptionTextRight == null) return;

        if (firstBody == null)
        {
            descriptionTextLeft.text = "First body: (none)";
            descriptionTextRight.text = "";
            return;
        }

        string desc = firstBody.GetDescription() ?? string.Empty;
        var lines = desc.Split(new[] { '\n' }, System.StringSplitOptions.None);
        int mid = (lines.Length + 1) / 2;
        string left = string.Join("\n", lines.Take(mid).ToArray());
        string right = string.Join("\n", lines.Skip(mid).ToArray());

        descriptionTextLeft.text = left;
        descriptionTextRight.text = right;
    }

    Button CreateButton(Transform parent, string label, Vector2 anchored)
    {
        GameObject bgo = new GameObject(label);
        bgo.transform.SetParent(parent, false);
        var btn = bgo.AddComponent<Button>();
        var img = bgo.AddComponent<Image>();
        img.color = new Color(0.8f, 0.8f, 0.8f, 1f);

        RectTransform brt = bgo.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 1f);
        brt.anchorMax = new Vector2(0f, 1f);
        brt.pivot = new Vector2(0f, 1f);
        brt.anchoredPosition = anchored;
        brt.sizeDelta = new Vector2(204f, 24f);

        GameObject lgo = new GameObject("Label");
        lgo.transform.SetParent(bgo.transform, false);
        var txt = lgo.AddComponent<Text>();
        txt.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.black;
        txt.fontSize = 14;
        RectTransform lrt = txt.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0f, 0f);
        lrt.anchorMax = new Vector2(1f, 1f);
        lrt.sizeDelta = Vector2.zero;

        return btn;
    }
}
