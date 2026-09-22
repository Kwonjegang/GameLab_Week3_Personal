using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSessionController : MonoBehaviour
{
    [Header("Day Rules")]
    [SerializeField, Range(0, 23)] private int startHour = 9;
    [SerializeField, Range(1, 24)] private int endHour = 15;
    [Tooltip("현실 시간 몇 초마다 게임 시간 1시간이 지나는지 설정합니다.")]
    [SerializeField, Min(1f)] private float secondsPerGameHour = 45f;
    [SerializeField, Min(0)] private int targetScore = 20000;

    [Header("Scene")]
    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerFishProgress progress;
    [SerializeField] private FishGaugeUI fishUI;
    [SerializeField] private Text clockText;
    [SerializeField] private TitleSequenceController titleSequence;
    [SerializeField] private GameObject settlementPanel;
    [SerializeField] private Text settlementText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text gameOverText;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settlementActions;
    [SerializeField] private GameObject gameOverRetryButton;
    [SerializeField] private GameObject gameOverMainButton;
    [SerializeField] private GameObject settlementRetryButton;
    [SerializeField] private GameObject settlementMainButton;
    [SerializeField] private GameObject menuStartButton;
    [Header("Pause and Settings")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject resumeButton;
    [SerializeField] private GameObject pauseRetryButton;
    [SerializeField] private GameObject pauseMainButton;
    [SerializeField] private GameObject pauseQuitButton;
    [SerializeField] private GameObject pauseSettingsButton;
    [SerializeField] private GameObject settingsBackButton;
    [SerializeField] private GameObject mouseMinusButton;
    [SerializeField] private GameObject mousePlusButton;
    [SerializeField] private GameObject gamepadMinusButton;
    [SerializeField] private GameObject gamepadPlusButton;
    [SerializeField] private Text mouseSensitivityText;
    [SerializeField] private Text gamepadSensitivityText;

    private float elapsedRealSeconds;
    private bool clockStopped;
    private bool paused;
    private GameObject mainReturnPanel;
    private bool mainReturnClockStopped;
    private bool mainReturnPaused;

    private void Awake()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (progress == null && player != null) progress = player.GetComponent<PlayerFishProgress>();
        if (fishUI == null) fishUI = FindFirstObjectByType<FishGaugeUI>();
        if (titleSequence == null) titleSequence = GetComponent<TitleSequenceController>();
        EnsureEventSystem();
        Bind(gameOverRetryButton, Retry);
        Bind(gameOverMainButton, ShowMainMenu);
        Bind(settlementRetryButton, Retry);
        Bind(settlementMainButton, ShowMainMenu);
        Bind(menuStartButton, Retry);
        Bind(resumeButton, ResumeGame);
        Bind(pauseRetryButton, Retry);
        Bind(pauseMainButton, ShowMainMenu);
        Bind(pauseQuitButton, QuitGame);
        Bind(pauseSettingsButton, OpenSettings);
        Bind(settingsBackButton, BackToPause);
        Bind(mouseMinusButton, () => AdjustSensitivity(-0.02f, 0f));
        Bind(mousePlusButton, () => AdjustSensitivity(0.02f, 0f));
        Bind(gamepadMinusButton, () => AdjustSensitivity(0f, -0.25f));
        Bind(gamepadPlusButton, () => AdjustSensitivity(0f, 0.25f));
    }

    private void Start()
    {
        Time.timeScale = 1f;
        if (settlementPanel != null) settlementPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settlementActions != null) settlementActions.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (player != null)
            player.SetLookSensitivities(PlayerPrefs.GetFloat("MouseSensitivity", player.MouseSensitivity),
                PlayerPrefs.GetFloat("GamepadSensitivity", player.GamepadSensitivity));
        UpdateSensitivityLabels();
        UpdateClock(startHour * 60);
        if (clockText != null && titleSequence != null && titleSequence.IsShowing)
            clockText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (titleSequence != null && titleSequence.IsShowing) return;
        if (clockText != null && !clockStopped && !clockText.gameObject.activeSelf)
            clockText.gameObject.SetActive(true);
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (mainMenuPanel != null && mainMenuPanel.activeSelf) { ReturnFromMainMenu(); return; }
            TogglePause();
        }
        if (paused) return;
        if (clockStopped) return;
        elapsedRealSeconds += Time.unscaledDeltaTime;
        int minutes = startHour * 60 + Mathf.FloorToInt(elapsedRealSeconds * 60f / secondsPerGameHour);
        minutes = Mathf.Min(minutes, endHour * 60);
        UpdateClock(minutes);
        if (minutes >= endHour * 60) StartCoroutine(Settlement());
    }

    public void StopClockForGameOver()
    {
        clockStopped = true;
    }

    public void OnGameOverShown()
    {
        clockStopped = true;
        paused = false;
        if (clockText != null) clockText.gameObject.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverText != null && string.IsNullOrEmpty(gameOverText.text)) gameOverText.text = "GAME OVER";
        ShowPointer();
        Time.timeScale = 0f;
    }

    private IEnumerator Settlement()
    {
        clockStopped = true;
        if (clockText != null) clockText.gameObject.SetActive(false);
        if (player != null) player.StopForSettlement();
        ShowPointer();
        Time.timeScale = 0f;
        if (settlementPanel != null) settlementPanel.SetActive(true);
        if (settlementActions != null) settlementActions.SetActive(false);

        int count500 = progress != null ? progress.Fish500 : 0;
        int count1000 = progress != null ? progress.Fish1000 : 0;
        int count1500 = progress != null ? progress.Fish1500 : 0;
        int total = progress != null ? progress.TotalValue : 0;
        float elapsed = 0f;
        while (elapsed < 2.4f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 2.4f);
            if (settlementText != null)
                settlementText.text = BuildSettlementText(count500, count1000, count1500,
                    Mathf.RoundToInt(total * t), false);
            yield return null;
        }

        bool won = total >= targetScore;
        if (settlementText != null)
            settlementText.text = BuildSettlementText(count500, count1000, count1500, total, true)
                + (won ? "\n\nVICTORY!" : "\n\nTARGET MISSED");
        if (won)
        {
            if (settlementActions != null) settlementActions.SetActive(true);
        }
        else
        {
            yield return new WaitForSecondsRealtime(1.8f);
            if (settlementPanel != null) settlementPanel.SetActive(false);
            if (gameOverText != null) gameOverText.text = $"GAME OVER\n{total:N0} / {targetScore:N0}";
            if (fishUI != null) fishUI.ShowGameOver();
            else OnGameOverShown();
        }
    }

    private string BuildSettlementText(int count500, int count1000, int count1500, int displayedTotal, bool complete)
    {
        return $"{endHour:00}:00  DAY SETTLEMENT\n\n" +
               $"500  x {count500}  = {count500 * 500:N0}\n" +
               $"1000 x {count1000}  = {count1000 * 1000:N0}\n" +
               $"1500 x {count1500}  = {count1500 * 1500:N0}\n\n" +
               $"TOTAL  {displayedTotal:N0} / GOAL  {targetScore:N0}" +
               (complete ? "" : "\nCOUNTING...");
    }

    private void UpdateClock(int minutes)
    {
        if (clockText != null) clockText.text = $"{minutes / 60:00}:{minutes % 60:00}";
    }

    public void Retry()
    {
        paused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("FishLooper");
    }

    public void ShowMainMenu()
    {
        mainReturnPanel = pausePanel != null && pausePanel.activeSelf ? pausePanel :
            settingsPanel != null && settingsPanel.activeSelf ? settingsPanel :
            gameOverPanel != null && gameOverPanel.activeSelf ? gameOverPanel :
            settlementPanel != null && settlementPanel.activeSelf ? settlementPanel : null;
        mainReturnClockStopped = clockStopped;
        mainReturnPaused = paused;
        clockStopped = true;
        paused = false;
        if (clockText != null) clockText.gameObject.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (settlementPanel != null) settlementPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        ShowPointer();
        Time.timeScale = 0f;
    }

    private void ReturnFromMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (mainReturnPanel != null) mainReturnPanel.SetActive(true);
        clockStopped = mainReturnClockStopped;
        paused = mainReturnPaused;
        if (clockText != null && !clockStopped) clockText.gameObject.SetActive(true);
        Time.timeScale = 0f;
        ShowPointer();
    }

    private void TogglePause()
    {
        if (clockStopped) return;
        if (settingsPanel != null && settingsPanel.activeSelf) { BackToPause(); return; }
        if (paused) ResumeGame();
        else
        {
            paused = true;
            Time.timeScale = 0f;
            if (pausePanel != null) pausePanel.SetActive(true);
            ShowPointer();
        }
    }

    public void ResumeGame()
    {
        paused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenSettings()
    {
        if (!paused) return;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        UpdateSensitivityLabels();
    }

    public void BackToPause()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    private void AdjustSensitivity(float mouseDelta, float gamepadDelta)
    {
        if (player == null) return;
        player.SetLookSensitivities(player.MouseSensitivity + mouseDelta,
            player.GamepadSensitivity + gamepadDelta);
        PlayerPrefs.SetFloat("MouseSensitivity", player.MouseSensitivity);
        PlayerPrefs.SetFloat("GamepadSensitivity", player.GamepadSensitivity);
        PlayerPrefs.Save();
        UpdateSensitivityLabels();
    }

    private void UpdateSensitivityLabels()
    {
        if (player == null) return;
        if (mouseSensitivityText != null) mouseSensitivityText.text = $"MOUSE  {player.MouseSensitivity:0.00}";
        if (gamepadSensitivityText != null) gamepadSensitivityText.text = $"GAMEPAD  {player.GamepadSensitivity:0.00}";
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void ShowPointer()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static void Bind(GameObject root, UnityEngine.Events.UnityAction action)
    {
        if (root == null) return;
        Button button = root.GetComponent<Button>();
        if (button == null) button = root.AddComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();
        button.onClick.AddListener(action);
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }
}
