using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

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

    [Header("Camera Look")]
    [SerializeField] private CinemachineOrbitalFollow orbitalFollow;
    [SerializeField] private float verticalLookSensitivity = 0.08f;
    [SerializeField] private float freeLookReturnSpeed = 12f;
    [SerializeField] private float minCameraPitch = -10f;
    [SerializeField] private float maxCameraPitch = 75f;

    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;
    private Animator playerAnimator;
    private bool isActionLocked;
    private float dashTime = 1f;
    private float freeLookYaw;
    private float cameraPitch;

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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (orbitalFollow == null)
        {
            orbitalFollow = FindFirstObjectByType<CinemachineOrbitalFollow>();
        }

        if (orbitalFollow != null)
        {
            cameraPitch = orbitalFollow.VerticalAxis.Value;
        }
    }
    void Update()
    {
        isGround = IsGround();
        verticalVelocity += gravity * Time.deltaTime;
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
        if (isGround)
        {
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (inputActions.Player.Jump.WasPressedThisFrame())
            {
                playerAnimator.SetTrigger("Jump");

                Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();

                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
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

        float currentSpeed = isRunning ? runSpeed : moveSpeed;

        moveDirection = move * currentSpeed;

        playerAnimator.SetBool("IsIdle", isIdle);
        playerAnimator.SetBool("IsWalking", isWalking);
        playerAnimator.SetBool("IsRunning", isRunning);
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

        float yaw = lookInput.x * lookSensitivity;

        if (orbitalFollow != null)
        {
            cameraPitch -= lookInput.y * verticalLookSensitivity;
            cameraPitch = Mathf.Clamp(cameraPitch, minCameraPitch, maxCameraPitch);

            orbitalFollow.HorizontalAxis.Value = cameraPitch;
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

    // 아래꺼 뭐가 문제인지 애니메이션 재생하려면 어떻게 했어야하는 지 알아보기

    //public void Animator()
    //{
    //if (moveDirection.y > 0)
    //    {
    //        playerAnimator.Play("StrutWalk");

    //        return;
    //    }

    //if (moveDirection.y <= 0)
    //    {
    //        moveDirection.x = 0f;
    //        playerAnimator.Play("Dwarf_Idle");

    //        return;
    //    }
    //}

}
