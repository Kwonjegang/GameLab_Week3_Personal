using UnityEngine;
using UnityEngine.UI;

public class FishGaugeUI : MonoBehaviour
{
    [Header("Scene UI")]
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private GameObject dimmerRoot;
    [SerializeField] private RectTransform[] dimmerPanels;
    [SerializeField] private GameObject gaugeRoot;
    [SerializeField] private RectTransform tugMarker;
    [SerializeField] private Image playerSideFill;
    [SerializeField] private Image aiSideFill;
    [SerializeField] private GameObject stressRoot;
    [SerializeField] private Text countdownText;
    [SerializeField] private GameObject gameOverRoot;
    [SerializeField] private GameObject rewardChoiceRoot;
    [SerializeField] private Text inventoryText;
    [SerializeField] private Text contestHint;
    [SerializeField] private Image playerStressFill;
    [SerializeField] private Image aiStressFill;
    [SerializeField] private float markerTravel = 210f;
    [SerializeField] private float stressBarWidth = 270f;
    [SerializeField] private float holePadding = 45f;
    private Renderer highlightedFish;

    public void Configure(RectTransform canvas, GameObject dimmer, RectTransform[] panels,
        GameObject gauge, RectTransform marker, Image playerFill, Image aiFill,
        GameObject stressPanel, Image playerStress, Image aiStress, Text countdown, GameObject gameOver,
        GameObject rewardChoices, Text inventory, float travel)
    {
        if (canvas != null) canvasRect = canvas;
        if (dimmer != null) dimmerRoot = dimmer;
        if (panels != null && panels.Length == 4) dimmerPanels = panels;
        if (gauge != null) gaugeRoot = gauge;
        if (marker != null) tugMarker = marker;
        if (playerFill != null) playerSideFill = playerFill;
        if (aiFill != null) aiSideFill = aiFill;
        if (stressPanel != null) stressRoot = stressPanel;
        if (playerStress != null) playerStressFill = playerStress;
        if (aiStress != null) aiStressFill = aiStress;
        if (countdown != null) countdownText = countdown;
        if (gameOver != null) gameOverRoot = gameOver;
        if (rewardChoices != null) rewardChoiceRoot = rewardChoices;
        if (inventory != null) inventoryText = inventory;
        markerTravel = travel;
    }

    public void Initialize()
    {
        if (canvasRect != null)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (Text label in canvasRect.GetComponentsInChildren<Text>(true))
                if (label.font == null) label.font = font;
        }
        UpdateGauge(0f, 100f);
        UpdateStress(0f, 0f);
        Hide();
        HideCountdown();
        if (stressRoot != null) stressRoot.SetActive(false);
        HideGameOver();
        HideRewardChoices();
    }

    public void ShowCountdown(int number)
    {
        if (countdownText == null) return;
        countdownText.gameObject.SetActive(true);
        countdownText.text = number.ToString();
        countdownText.color = new Color(1f, 0.9f, 0.42f);
        countdownText.rectTransform.localScale = Vector3.one * 1.25f;
    }

    public void ShowParryCue()
    {
        if (countdownText == null) return;
        countdownText.gameObject.SetActive(true);
        countdownText.text = "F!";
        countdownText.color = new Color(0.2f, 0.95f, 1f);
        countdownText.rectTransform.localScale = Vector3.one * 1.4f;
    }

    public void ShowParryResult(string result)
    {
        if (countdownText == null) return;
        countdownText.gameObject.SetActive(true);
        countdownText.text = result;
        countdownText.color = Color.white;
        countdownText.rectTransform.localScale = Vector3.one;
    }

    public void HideCountdown()
    {
        if (countdownText != null) countdownText.gameObject.SetActive(false);
    }

    public void ShowStress()
    {
        if (stressRoot != null) stressRoot.SetActive(true);
    }

    public void HideStress()
    {
        if (stressRoot != null) stressRoot.SetActive(false);
    }

    public void ShowGameOver()
    {
        if (gameOverRoot != null) gameOverRoot.SetActive(true);
    }

    public void HideGameOver()
    {
        if (gameOverRoot != null) gameOverRoot.SetActive(false);
    }

    public void ShowRewardChoices()
    {
        if (rewardChoiceRoot != null) rewardChoiceRoot.SetActive(true);
    }

    public void HideRewardChoices()
    {
        if (rewardChoiceRoot != null) rewardChoiceRoot.SetActive(false);
    }

    public void UpdateInventory(int fish500, int fish1000, int fish1500, int totalValue)
    {
        if (inventoryText != null)
            inventoryText.text = $"500 x{fish500}   1000 x{fish1000}   1500 x{fish1500}    TOTAL {totalValue}";
    }

    public void ShowArrival(Renderer fish)
    {
        highlightedFish = fish;
        if (dimmerRoot != null) dimmerRoot.SetActive(true);
        UpdateDimmer();
    }

    public void ClearArrival()
    {
        highlightedFish = null;
        if (dimmerRoot != null) dimmerRoot.SetActive(false);
    }

    private void LateUpdate()
    {
        if (highlightedFish != null && dimmerRoot != null && dimmerRoot.activeSelf) UpdateDimmer();
    }

    private void UpdateDimmer()
    {
        if (canvasRect == null || dimmerPanels == null || dimmerPanels.Length != 4 || highlightedFish == null) return;
        Camera camera = Camera.main;
        if (camera == null) return;
        Vector3 screen = camera.WorldToScreenPoint(highlightedFish.bounds.center);
        if (screen.z <= 0f) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
        float width = canvasRect.rect.width;
        float height = canvasRect.rect.height;
        float x = Mathf.Clamp(local.x + width * 0.5f, 0f, width);
        float y = Mathf.Clamp(local.y + height * 0.5f, 0f, height);
        Vector3 edge = camera.WorldToScreenPoint(highlightedFish.bounds.center + camera.transform.up * highlightedFish.bounds.extents.magnitude);
        float screenRadius = Vector2.Distance(new Vector2(screen.x, screen.y), new Vector2(edge.x, edge.y));
        float radius = Mathf.Clamp(screenRadius * width / Mathf.Max(1f, Screen.width) + holePadding, 75f, 300f);
        float left = Mathf.Clamp(x - radius, 0f, width);
        float right = Mathf.Clamp(x + radius, 0f, width);
        float bottom = Mathf.Clamp(y - radius, 0f, height);
        float top = Mathf.Clamp(y + radius, 0f, height);
        SetPanel(dimmerPanels[0], 0f, 0f, left, height);
        SetPanel(dimmerPanels[1], right, 0f, width - right, height);
        SetPanel(dimmerPanels[2], left, 0f, right - left, bottom);
        SetPanel(dimmerPanels[3], left, top, right - left, height - top);
    }

    private static void SetPanel(RectTransform panel, float x, float y, float width, float height)
    {
        if (panel == null) return;
        panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.zero;
        panel.anchoredPosition = new Vector2(x, y);
        panel.sizeDelta = new Vector2(width, height);
    }

    public void Show(float balance, float maximum, int round = 0)
    {
        if (gaugeRoot != null) gaugeRoot.SetActive(true);
        if (contestHint != null && round > 0) contestHint.text = $"ROUND {round}/4   F 연타!";
        UpdateGauge(balance, maximum);
    }

    public void Hide()
    {
        if (gaugeRoot != null) gaugeRoot.SetActive(false);
        ClearArrival();
        HideCountdown();
    }

    public void UpdateGauge(float balance, float maximum)
    {
        if (tugMarker == null) return;
        float normalized = Mathf.Clamp(balance / Mathf.Max(1f, maximum), -1f, 1f);
        Vector2 position = tugMarker.anchoredPosition;
        position.x = normalized * markerTravel;
        tugMarker.anchoredPosition = position;
        SetSideWidth(playerSideFill, Mathf.Max(0f, normalized) * markerTravel);
        SetSideWidth(aiSideFill, Mathf.Max(0f, -normalized) * markerTravel);
    }

    private static void SetSideWidth(Image image, float width)
    {
        if (image == null) return;
        Vector2 size = image.rectTransform.sizeDelta;
        size.x = width;
        image.rectTransform.sizeDelta = size;
    }

    public void UpdateStress(float playerStress, float aiStress)
    {
        SetStressWidth(playerStressFill, playerStress);
        SetStressWidth(aiStressFill, aiStress);
    }

    private void SetStressWidth(Image image, float stress)
    {
        if (image == null) return;
        Vector2 size = image.rectTransform.sizeDelta;
        size.x = stressBarWidth * Mathf.Clamp01(stress / 100f);
        image.rectTransform.sizeDelta = size;
    }
}
