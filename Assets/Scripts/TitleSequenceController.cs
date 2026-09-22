using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TitleSequenceController : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform sunbedSeat;
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private RectTransform titleText;
    [SerializeField] private GameObject startPrompt;
    [SerializeField] private Image blackOverlay;
    [SerializeField] private Text workHoursText;
    [SerializeField] private RawImage gamepadGuide;
    [SerializeField] private RawImage keyboardGuide;
    [SerializeField] private float fallHeight = 25f;
    [SerializeField] private float fallDuration = 1.15f;
    [SerializeField] private float titleDropDuration = 0.75f;
    [SerializeField] private Vector3 titleCameraOffset = new Vector3(19f, 12f, 28f);
    [SerializeField, Min(1f)] private float workHoursDuration = 4.5f;

    public bool IsShowing { get; private set; } = true;

    private enum IntroStep { Cinematic, AwaitStart, Transition, Gamepad, Keyboard, Finished }
    private IntroStep step;
    private int inputReadyFrame;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private CharacterController characterController;
    private Animator animator;
    private CinemachineBrain brain;
    private Camera mainCamera;

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (sunbedSeat == null || player == null) { IsShowing = false; return; }
        mainCamera = Camera.main;
        if (mainCamera == null) { IsShowing = false; return; }
        brain = mainCamera.GetComponent<CinemachineBrain>();
        characterController = player.GetComponent<CharacterController>();
        animator = player.GetComponent<Animator>();
        originalPosition = player.transform.position;
        originalRotation = player.transform.rotation;
        originalCameraPosition = mainCamera.transform.position;
        originalCameraRotation = mainCamera.transform.rotation;

        player.enabled = false;
        if (characterController != null) characterController.enabled = false;
        if (brain != null) brain.enabled = false;
        if (titlePanel != null) titlePanel.SetActive(true);
        if (startPrompt != null) startPrompt.SetActive(false);
        if (blackOverlay != null) { blackOverlay.gameObject.SetActive(false); SetBlackAlpha(0f); }
        if (workHoursText != null) workHoursText.gameObject.SetActive(false);
        if (gamepadGuide != null)
        {
            if (gamepadGuide.texture == null)
                gamepadGuide.texture = Resources.Load<Texture2D>("Guides/GamepadGuide");
            gamepadGuide.gameObject.SetActive(false);
        }
        if (keyboardGuide != null)
        {
            if (keyboardGuide.texture == null)
                keyboardGuide.texture = Resources.Load<Texture2D>("Guides/KeyboardGuide");
            keyboardGuide.gameObject.SetActive(false);
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StartCoroutine(PlayTitle());
    }

    private void Update()
    {
        if (Time.frameCount < inputReadyFrame || !AnyInputPressed()) return;
        switch (step)
        {
            case IntroStep.AwaitStart:
                step = IntroStep.Transition;
                StartCoroutine(ShowWorkHoursAndGuides());
                break;
            case IntroStep.Gamepad:
                if (gamepadGuide != null) gamepadGuide.gameObject.SetActive(false);
                if (keyboardGuide != null) keyboardGuide.gameObject.SetActive(true);
                step = IntroStep.Keyboard;
                inputReadyFrame = Time.frameCount + 2;
                break;
            case IntroStep.Keyboard:
                BeginGame();
                break;
        }
    }

    private IEnumerator PlayTitle()
    {
        step = IntroStep.Cinematic;
        Vector3 seat = sunbedSeat.position + Vector3.right * 2.5f;
        Vector3 cameraOffset = sunbedSeat.rotation * titleCameraOffset;
        mainCamera.transform.position = seat + cameraOffset;
        mainCamera.transform.LookAt(seat + Vector3.up * 4f);
        player.transform.SetPositionAndRotation(seat + Vector3.up * fallHeight, sunbedSeat.rotation);
        if (animator != null) animator.Play("SittingPose", 0, 0f);
        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fallDuration);
            player.transform.position = Vector3.Lerp(seat + Vector3.up * fallHeight, seat, t * t);
            yield return null;
        }
        player.transform.position = seat;
        Vector3 originalScale = player.transform.localScale;
        player.transform.localScale = Vector3.Scale(originalScale, new Vector3(1.1f, 0.85f, 1.1f));
        yield return new WaitForSecondsRealtime(0.12f);
        player.transform.localScale = originalScale;
        yield return new WaitForSecondsRealtime(0.18f);

        if (titleText != null)
        {
            Vector2 landing = new Vector2(0f, 175f);
            titleText.anchoredPosition = new Vector2(0f, 900f);
            elapsed = 0f;
            while (elapsed < titleDropDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / titleDropDuration);
                titleText.anchoredPosition = Vector2.LerpUnclamped(new Vector2(0f, 900f), landing,
                    1f - Mathf.Pow(1f - t, 3f));
                yield return null;
            }
            titleText.anchoredPosition = landing + Vector2.down * 22f;
            yield return new WaitForSecondsRealtime(0.1f);
            titleText.anchoredPosition = landing;
        }
        if (startPrompt != null) startPrompt.SetActive(true);
        step = IntroStep.AwaitStart;
        inputReadyFrame = Time.frameCount + 2;
    }

    private IEnumerator ShowWorkHoursAndGuides()
    {
        if (blackOverlay != null) blackOverlay.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < 0.65f)
        {
            elapsed += Time.unscaledDeltaTime;
            SetBlackAlpha(Mathf.Clamp01(elapsed / 0.65f));
            yield return null;
        }
        SetBlackAlpha(1f);
        if (titlePanel != null) titlePanel.SetActive(false);
        if (workHoursText != null)
        {
            workHoursText.supportRichText = true;
            workHoursText.text = "근무 시간은 오전 9시부터 오후 3시까지 입니다.\n\n"
                + "<size=38>(썬배드에 F를 누르면 미니게임을 진행할 수 있습니다.)</size>";
            workHoursText.gameObject.SetActive(true);
        }
        yield return new WaitForSecondsRealtime(workHoursDuration);
        if (workHoursText != null) workHoursText.gameObject.SetActive(false);
        if (gamepadGuide != null) gamepadGuide.gameObject.SetActive(true);
        step = IntroStep.Gamepad;
        inputReadyFrame = Time.frameCount + 2;
    }

    private void BeginGame()
    {
        step = IntroStep.Finished;
        if (keyboardGuide != null) keyboardGuide.gameObject.SetActive(false);
        if (blackOverlay != null) blackOverlay.gameObject.SetActive(false);
        player.transform.SetPositionAndRotation(originalPosition, originalRotation);
        if (animator != null) { animator.Rebind(); animator.Update(0f); }
        if (characterController != null) characterController.enabled = true;
        player.enabled = true;
        mainCamera.transform.SetPositionAndRotation(originalCameraPosition, originalCameraRotation);
        if (brain != null) brain.enabled = true;
        IsShowing = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetBlackAlpha(float alpha)
    {
        if (blackOverlay == null) return;
        Color color = blackOverlay.color;
        color.a = alpha;
        blackOverlay.color = color;
    }

    private static bool AnyInputPressed()
    {
        return (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            || (Mouse.current != null &&
                (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
            || (Gamepad.current != null &&
                (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame));
    }
}
