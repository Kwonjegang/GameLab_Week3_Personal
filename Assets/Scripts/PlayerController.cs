using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float runSpeed = 30f;
    public float dashSpeed = 15f;
    public float jumpHeight = 5f;
    public float verticalVelocity;
    public float gravity = -8.8f;
    public bool isMoveAble = true;
    public ParticleSystem punchEffect;

    [Header("OnGrounded")]
    [Tooltip("캐릭터가 땅 위에 있는지 체크")]
    public bool isGround = true;
    [Header("Can CameraMove Up")]
    [Tooltip("카메라 상단 이동 한계값")]
    public float TopClamp = 90.0f;
    [Header("Can CameraMove Down")]
    [Tooltip("카메라 하단 이동 한계값")]
    public float BottomClamp = -90.0f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.35f;
    [SerializeField] private float groundCheckDistance = 0.15f;

    [Header("Look & Rotate")]
    [SerializeField] private float lookSensitivity = 0.15f;
    [SerializeField] private float gamepadLookSensitivity = 2.5f;

    [Header("Camera Look")]
    [SerializeField] private CinemachineOrbitalFollow orbitalFollow;
    [SerializeField] private float verticalLookSensitivity = 0.08f;
    [SerializeField] private float freeLookReturnSpeed = 12f;
    [SerializeField] private float minCameraPitch = -10f;
    [SerializeField] private float maxCameraPitch = 75f;
    [SerializeField] private Transform playerVisual;
    [SerializeField] private float backwardCameraDistance = 2.5f;

    [Header("Water")]
    [SerializeField] private Transform waterSurface;
    [SerializeField] private float waterDoubleTapWindow = 0.35f;
    [SerializeField] private float waterEscapeJumpHeight = 40f;
    [SerializeField] private float waterSinkSpeed = 3f;

    [Header("Sunbed")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private Transform liePoint;
    [SerializeField] private float interactionRange = 5f;
    [SerializeField] private float sunbedMoveTime = 1.2f;
    [SerializeField] private float sunbedJumpHeight = 2f;
    [SerializeField] private float poseSwitchRatio = 0.4f;

    [SerializeField] private CinemachineCamera followCamera;
    [SerializeField] private CinemachineCamera stageCamera;
    [SerializeField] private EnemyStageIntro enemyIntro;

    private bool isUsingSunbed;
    public bool isStageStarted;

    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;
    private Animator playerAnimator;
    private bool isActionLocked;
    private float dashTime = 1f;
    private float freeLookYaw;
    private float cameraPitch;
    private float verticalSensitivityRatio;
    private float baseOrbitRadius;
    private float backwardBlend;
    private Quaternion visualReadyRotation;
    private bool isBackwardRunning;
    private bool isInWater;
    private bool isGameOver;
    private float lastWaterJumpPress = -10f;
    private float lastWaterBurstTime = -10f;
    private Vector3 preStagePosition;
    private Quaternion preStageRotation;
    private bool stageEnding;
    private Transform stunIcon;

    public bool IsInWater => isInWater;
    public bool IsGameOver => isGameOver;
    public float MouseSensitivity => lookSensitivity;
    public float GamepadSensitivity => gamepadLookSensitivity;

    public void SetLookSensitivities(float mouse, float gamepad)
    {
        lookSensitivity = Mathf.Clamp(mouse, 0.02f, 1f);
        gamepadLookSensitivity = Mathf.Clamp(gamepad, 0.2f, 8f);
    }

    InputSystem_Actions inputActions;
    Coroutine dashCoroutine;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }
    private void OnEnable()
    {
        inputActions.Enable();
    }
    private void OnDisable()
    {
        inputActions.Disable();
    }
    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerAnimator = GetComponent<Animator>();
        if (playerVisual != null) visualReadyRotation = playerVisual.localRotation;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (orbitalFollow == null)
        {
            orbitalFollow = FindFirstObjectByType<CinemachineOrbitalFollow>();
        }

        if (orbitalFollow != null)
        {
            cameraPitch = orbitalFollow.VerticalAxis.Value;
            baseOrbitRadius = orbitalFollow.Radius;
        }
        verticalSensitivityRatio = verticalLookSensitivity / Mathf.Max(0.01f, lookSensitivity);

        if (followCamera != null)
        {
            followCamera.gameObject.SetActive(true);
        }

        if (stageCamera != null)
        {
            stageCamera.gameObject.SetActive(false);
        }
    }
    void Update()
    {
        if (Time.timeScale == 0f) return;
        if (isGameOver) return;
        if (isUsingSunbed)
        {
            return;
        }

        CheckSunbedInteraction();

        if (isUsingSunbed)
        {
            return;
        }

        UpdateWaterState();
        isGround = IsGround();

        verticalVelocity += gravity * Time.deltaTime;
        if (isInWater && verticalVelocity < -waterSinkSpeed) verticalVelocity = -waterSinkSpeed;
        moveDirection.y = verticalVelocity;
        controller.Move(moveDirection * Time.deltaTime);

        if (isActionLocked)
        {
            return;
        }

        RotatePlayer();
        Dash();
        JumpCheck();
        Move();
    }
    public void JumpCheck()
    {
        bool jumpPressed = inputActions.Player.Jump.WasPressedThisFrame();
        if (isInWater)
        {
            if (jumpPressed)
            {
                if (Time.time - lastWaterJumpPress <= waterDoubleTapWindow &&
                    Time.time - lastWaterBurstTime > 1f)
                {
                    lastWaterBurstTime = Time.time;
                    lastWaterJumpPress = -10f;
                    verticalVelocity = Mathf.Sqrt(waterEscapeJumpHeight * -2f * gravity);
                    playerAnimator.SetTrigger("Jump");
                }
                else lastWaterJumpPress = Time.time;
            }
            return;
        }
        if (isGround)
        {
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (jumpPressed)
            {
                playerAnimator.SetTrigger("Jump");

                Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();

                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
    }

    private void UpdateWaterState()
    {
        bool wasInWater = isInWater;
        Renderer surfaceRenderer = waterSurface != null ? waterSurface.GetComponent<Renderer>() : null;
        if (surfaceRenderer == null) isInWater = false;
        else
        {
            Bounds bounds = surfaceRenderer.bounds;
            Vector3 position = transform.position;
            isInWater = position.x >= bounds.min.x && position.x <= bounds.max.x &&
                position.z >= bounds.min.z && position.z <= bounds.max.z &&
                position.y <= waterSurface.position.y + 1f;
        }
        if (isInWater != wasInWater)
        {
            playerAnimator.SetBool("IsSwimming", isInWater);
            lastWaterJumpPress = -10f;
        }
    }
    bool IsGround()
    {
        Vector3 spherePosition = transform.position + (Vector3.down * groundCheckDistance);
        return Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer);
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Vector3 spherePosition = transform.position + (Vector3.down * groundCheckDistance);
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
    }
    public void Move()
    {
        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        bool hasMoveInput = moveInput.sqrMagnitude > 0.01f;
        bool sprintPressed = inputActions.Player.Sprint.IsPressed();

        bool isIdle = !hasMoveInput;
        bool isWalking = hasMoveInput && !sprintPressed;
        bool isRunning = hasMoveInput && sprintPressed;
        isBackwardRunning = isRunning && moveInput.y < -0.1f;

        float currentSpeed = isRunning ? runSpeed : moveSpeed;

        moveDirection = move * currentSpeed;

        playerAnimator.SetBool("IsIdle", isIdle);
        playerAnimator.SetBool("IsWalking", isWalking);
        playerAnimator.SetBool("IsRunning", isRunning);
    }

    private void LateUpdate()
    {
        if (stunIcon != null && Camera.main != null) stunIcon.rotation = Camera.main.transform.rotation;
        if (isGameOver || isUsingSunbed) return;
        backwardBlend = Mathf.MoveTowards(backwardBlend, isBackwardRunning ? 1f : 0f, Time.deltaTime * 3.5f);
        if (playerVisual != null)
            playerVisual.localRotation = Quaternion.Slerp(visualReadyRotation,
                visualReadyRotation * Quaternion.Euler(0f, 180f, 0f), backwardBlend);
        if (orbitalFollow != null)
            orbitalFollow.Radius = Mathf.Lerp(orbitalFollow.Radius,
                baseOrbitRadius + backwardCameraDistance * backwardBlend,
                1f - Mathf.Exp(-5f * Time.deltaTime));
    }
    public void Dash()
    {
        if (dashCoroutine != null)
        {
            return;
        }

        if (inputActions.Player.Dash.WasPressedThisFrame() && isGround)
        {
            dashCoroutine = StartCoroutine(DashCoroutine());
        }
    }
    public void RotatePlayer()
    {
        Vector2 lookInput = inputActions.Player.Look.ReadValue<Vector2>();

        bool isFreeLook = inputActions.Player.FreeLook.IsPressed();

        bool gamepad = inputActions.Player.Look.activeControl != null &&
            inputActions.Player.Look.activeControl.device is Gamepad;
        float multiplier = gamepad ? gamepadLookSensitivity * 60f * Time.deltaTime : lookSensitivity;
        float yaw = lookInput.x * multiplier;

        if (orbitalFollow != null)
        {
            cameraPitch -= lookInput.y * multiplier * verticalSensitivityRatio;
            cameraPitch = Mathf.Clamp(cameraPitch, minCameraPitch, maxCameraPitch);

            orbitalFollow.VerticalAxis.Value = cameraPitch;
        }
        if (isFreeLook)
        {
            freeLookYaw += yaw;

            if (orbitalFollow != null)
            {
                orbitalFollow.HorizontalAxis.Value = freeLookYaw;
            }

            return;
        }

        transform.Rotate(0f, yaw, 0f);

        if (orbitalFollow != null)
        {
            freeLookYaw = Mathf.LerpAngle(freeLookYaw, 0f, Time.deltaTime * freeLookReturnSpeed);
            orbitalFollow.HorizontalAxis.Value = freeLookYaw;
        }
    }
    void CheckSunbedInteraction()
    {
        if (!inputActions.Player.Interact.WasPressedThisFrame() && !inputActions.Player.Interact.WasPerformedThisFrame())
        {
            return;
        }

        if (isActionLocked || dashCoroutine != null ||interactionPoint == null || liePoint == null || followCamera == null || stageCamera == null)
        {
            return;
        }

        Vector3 playerPosition = transform.position;
        Vector3 interactionPosition = interactionPoint.position;

        playerPosition.y = 0f;
        interactionPosition.y = 0f;

        float distance = Vector3.Distance(playerPosition, interactionPosition);

        if (distance > interactionRange)
        {
            Debug.Log($"선베드 상호작용 실패: 거리가 너무 멉니다. 현재 거리 {distance:F2}, 필요 거리 {interactionRange:F2}");
            return;
        }

        StartCoroutine(SunbedCoroutine());
    }

    IEnumerator SunbedCoroutine()
    {
        preStagePosition = transform.position;
        preStageRotation = transform.rotation;
        isUsingSunbed = true;
        isActionLocked = true;

        moveDirection = Vector3.zero;
        verticalVelocity = 0f;

        playerAnimator.SetBool("IsIdle", false);
        playerAnimator.SetBool("IsWalking", false);
        playerAnimator.SetBool("IsRunning", false);

        playerAnimator.ResetTrigger("Jump");
        playerAnimator.ResetTrigger("Dash");
        playerAnimator.ResetTrigger("StandUp");
        playerAnimator.ResetTrigger("SunbedSit");

        followCamera.gameObject.SetActive(false);
        stageCamera.gameObject.SetActive(true);

        controller.enabled = false;

        Vector3 startPosition = transform.position;
        Vector3 endPosition = liePoint.position;
        endPosition.x += 2.5f;

        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = Quaternion.Euler(0f, liePoint.eulerAngles.y, 0f);

        playerAnimator.SetTrigger("Jump");

        float timer = 0f;
        bool poseStarted = false;

        while (timer < sunbedMoveTime)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / sunbedMoveTime);

            Vector3 nextPosition = Vector3.Lerp(startPosition, endPosition, t);
            nextPosition.y += Mathf.Sin(t * Mathf.PI) * sunbedJumpHeight;

            transform.position = nextPosition;
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);

            if (!poseStarted && t >= poseSwitchRatio)
            {
                poseStarted = true;
                playerAnimator.SetTrigger("SunbedSit");
            }

            yield return null;
        }

        transform.position = endPosition;
        transform.rotation = endRotation;

        isStageStarted = true;

        if (enemyIntro != null)
        {
            enemyIntro.PlayIntro();
        }
    }

    public void EndStage(bool gameOver, FishGaugeUI ui)
    {
        if (stageEnding || isGameOver) return;
        stageEnding = true;
        StartCoroutine(EndStageCoroutine(gameOver, ui));
    }

    public void DrownGameOver(FishGaugeUI ui)
    {
        if (isGameOver) return;
        StartCoroutine(DrownGameOverCoroutine(ui));
    }

    private IEnumerator DrownGameOverCoroutine(FishGaugeUI ui)
    {
        isGameOver = true;
        isActionLocked = true;
        moveDirection = Vector3.zero;
        verticalVelocity = 0f;
        playerAnimator.SetBool("IsSwimming", false);
        playerAnimator.CrossFade("ZombieStumbling", 0.08f, 0, 0f);
        ShowStunIcon();
        yield return new WaitForSeconds(0.75f);
        controller.enabled = false;
        float elapsed = 0f;
        while (elapsed < 2f)
        {
            elapsed += Time.deltaTime;
            transform.position += Vector3.down * 4f * Time.deltaTime;
            yield return null;
        }
        if (ui != null) ui.ShowGameOver();
    }

    private void OnDestroy()
    {
        if (stunIcon != null) Destroy(stunIcon.gameObject);
    }

    private void ShowStunIcon()
    {
        if (stunIcon != null) return;
        GameObject icon = new GameObject("StunIcon");
        stunIcon = icon.transform;
        stunIcon.SetParent(transform, false);
        stunIcon.localPosition = Vector3.up * 11f;
        TextMesh label = icon.AddComponent<TextMesh>();
        label.text = "Zz";
        label.fontSize = 72;
        label.characterSize = 0.18f;
        label.anchor = TextAnchor.MiddleCenter;
        label.color = new Color(1f, 0.9f, 0.35f);
    }

    private IEnumerator EndStageCoroutine(bool gameOver, FishGaugeUI ui)
    {
        if (gameOver && enemyIntro != null) enemyIntro.ResetStage();
        isStageStarted = false;
        isUsingSunbed = false;
        isActionLocked = gameOver;
        moveDirection = Vector3.zero;
        verticalVelocity = 0f;
        isBackwardRunning = false;
        backwardBlend = 0f;
        if (playerVisual != null) playerVisual.localRotation = visualReadyRotation;
        if (orbitalFollow != null) orbitalFollow.Radius = baseOrbitRadius;
        playerAnimator.SetBool("IsSwimming", false);
        playerAnimator.Rebind();
        playerAnimator.Update(0f);
        controller.enabled = false;
        transform.SetPositionAndRotation(preStagePosition, preStageRotation);
        controller.enabled = true;
        stageCamera.gameObject.SetActive(false);
        if (followCamera != null) followCamera.gameObject.SetActive(true);
        if (gameOver)
        {
            isGameOver = true;
            ShowStunIcon();
            playerAnimator.CrossFade("ZombieStumbling", 0.08f, 0, 0f);
            yield return new WaitForSeconds(1.6f);
            if (ui != null) ui.ShowGameOver();
        }
    }

    public void StopForSettlement()
    {
        StopAllCoroutines();
        if (isStageStarted || isUsingSunbed)
        {
            if (enemyIntro != null) enemyIntro.ResetStage();
            controller.enabled = false;
            transform.SetPositionAndRotation(preStagePosition, preStageRotation);
            controller.enabled = true;
            if (stageCamera != null) stageCamera.gameObject.SetActive(false);
            if (followCamera != null) followCamera.gameObject.SetActive(true);
        }
        isStageStarted = false;
        isUsingSunbed = false;
        isActionLocked = true;
        isGameOver = true;
        moveDirection = Vector3.zero;
        verticalVelocity = 0f;
        if (orbitalFollow != null) orbitalFollow.Radius = baseOrbitRadius;
    }
    IEnumerator DashCoroutine()
    {
        if (isGround)
        {
            isActionLocked = true;

            playerAnimator.SetBool("IsIdle", false);
            playerAnimator.SetBool("IsWalking", false);
            playerAnimator.SetBool("IsRunning", false);
            playerAnimator.SetTrigger("Dash");

            float timer = 0f;

            Vector3 dashDirection = transform.forward;
            
            dashDirection.y = 0f;
            dashDirection.Normalize();

            verticalVelocity = Mathf.Sqrt(jumpHeight * 8f);

            while (timer < dashTime)
            {
                timer += Time.deltaTime;

                verticalVelocity += gravity * Time.deltaTime;

                Vector3 dashMove = dashDirection * dashSpeed;
                dashMove.y = verticalVelocity;

                controller.Move(dashMove * Time.deltaTime * 4f);

                moveDirection = Vector3.zero;

                yield return null;
            }

            yield return new WaitForSeconds(2.5f);
          
            playerAnimator.SetTrigger("StandUp");
            moveDirection = Vector3.zero;

            yield return new WaitForSeconds(2f);

            isActionLocked = false;

            dashCoroutine = null;
        }
    }
}
