using UnityEngine;
using UnityEngine.UI;

public class FishGaugeUI : MonoBehaviour
{
    [SerializeField] private GameObject gaugeRoot;
    [SerializeField] private Slider playerGaugeSlider;
    [SerializeField] private Slider aiGaugeSlider;
    [SerializeField] private float maxGauge = 100f;
    private Canvas canvas;
    private GameObject dimmer;

    public void Configure(GameObject root, Slider playerSlider, Slider aiSlider, float gaugeMaxValue)
    {
        gaugeRoot = root != null ? root : gaugeRoot;
        playerGaugeSlider = playerSlider != null ? playerSlider : playerGaugeSlider;
        aiGaugeSlider = aiSlider != null ? aiSlider : aiGaugeSlider;
        maxGauge = gaugeMaxValue;
    }

    public void Initialize()
    {
        if (gaugeRoot == null || playerGaugeSlider == null || aiGaugeSlider == null) BuildUI();
        UpdateGauge(0f, 0f);
    }

    private void BuildUI()
    {
        GameObject canvasObject = new GameObject("FishContestCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        dimmer = MakeImage("FishArrivalDim", canvasObject.transform, new Color(0.01f, 0.02f, 0.05f, 0.48f));
        RectTransform dimRect = dimmer.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = dimRect.offsetMax = Vector2.zero;
        dimmer.SetActive(false);
        gaugeRoot = MakeImage("FishContestGauge", canvasObject.transform, new Color(0.025f, 0.055f, 0.1f, 0.92f));
        RectTransform panel = gaugeRoot.GetComponent<RectTransform>();
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(0f, -65f);
        panel.sizeDelta = new Vector2(620f, 170f);
        MakeText(panel, "MASH F  /  FIRST TO 100 WINS THE FISH", new Vector2(0f, -23f), 24, Color.white, 600f);
        MakeText(panel, "PLAYER", new Vector2(-245f, -72f), 20, new Color(0.2f, 0.85f, 1f), 105f);
        MakeText(panel, "AI", new Vector2(-245f, -122f), 20, new Color(1f, 0.45f, 0.42f), 105f);
        playerGaugeSlider = MakeGauge(panel, -72f, new Color(0.18f, 0.78f, 1f));
        aiGaugeSlider = MakeGauge(panel, -122f, new Color(1f, 0.38f, 0.36f));
        gaugeRoot.SetActive(false);
    }

    private static GameObject MakeImage(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return obj;
    }

    private static void MakeText(Transform parent, string value, Vector2 position, int size, Color color, float width)
    {
        GameObject obj = new GameObject(value, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(width, 34f);
        Text label = obj.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = value;
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.raycastTarget = false;
    }

    private static Slider MakeGauge(Transform parent, float y, Color color)
    {
        GameObject root = new GameObject("Gauge", typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(57f, y);
        rect.sizeDelta = new Vector2(460f, 25f);
        GameObject track = MakeImage("Track", root.transform, new Color(0.12f, 0.18f, 0.24f));
        GameObject fill = MakeImage("Fill", track.transform, color);
        foreach (GameObject obj in new[] { track, fill })
        {
            RectTransform r = obj.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }
        Slider slider = root.GetComponent<Slider>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.interactable = false;
        return slider;
    }

    public void ShowArrival() { if (dimmer != null) dimmer.SetActive(true); }
    public void ClearArrival() { if (dimmer != null) dimmer.SetActive(false); }
    public void Show(float playerGauge, float aiGauge)
    {
        if (gaugeRoot != null) gaugeRoot.SetActive(true);
        UpdateGauge(playerGauge, aiGauge);
    }
    public void Hide()
    {
        if (gaugeRoot != null) gaugeRoot.SetActive(false);
        ClearArrival();
    }
    public void UpdateGauge(float playerGauge, float aiGauge)
    {
        if (playerGaugeSlider != null)
        {
            playerGaugeSlider.maxValue = maxGauge;
            playerGaugeSlider.value = playerGauge;
        }
        if (aiGaugeSlider != null)
        {
            aiGaugeSlider.maxValue = maxGauge;
            aiGaugeSlider.value = aiGauge;
        }
    }
    private void OnDestroy() { if (canvas != null) Destroy(canvas.gameObject); }
}
