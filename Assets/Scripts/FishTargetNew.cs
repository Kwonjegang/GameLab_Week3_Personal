using UnityEngine;
using UnityEngine.UI;

public partial class FishTargetNew : MonoBehaviour
{
    public enum FishState
    {
        Idle,
        HookedByAI,
        HookedByPlayer,
        Contest,
        PlayerWin,
        AIWin
    }

    [Header("State")]
    public FishState currentState = FishState.Idle;

    [Header("Time")]
    [SerializeField] private float contestDuration = 4f;

    [Header("Timing Bonus")]
    [SerializeField] private float perfectTime = 0.25f;
    [SerializeField] private float goodTime = 0.85f;
    [SerializeField] private float normalTime = 1.25f;
    [SerializeField] private float perfectBonus = 70f;
    [SerializeField] private float goodBonus = 35f;
    [SerializeField] private float normalBonus = 15f;
    [SerializeField] private float lateBonus = 5f;

    [Header("Contest Gauge")]
    [SerializeField] private float playerGauge;
    [SerializeField] private float aiGauge;
    [SerializeField] private float playerMashPower = 5f;
    [SerializeField] private float aiPowerPerSecond = 18f;
    [SerializeField] private float maxGauge = 100f;

    [Header("AI Catch")]
    [SerializeField] private EnemyStageIntro enemyIntro;
    [SerializeField] private Transform fishMoveTarget;
    [SerializeField] private Transform fishFocusPoint;
    [SerializeField] private Vector3 fishLineOffset = Vector3.zero;
    [SerializeField] private float liftTime = 1f;
    [SerializeField] private float liftDistance = 5f;
    [SerializeField] private float liftAngle = 60f;
    [SerializeField] private float glowDuration = 3f;

    [Header("Player Fishing")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private GameObject playerFishingRod;
    [SerializeField] private Transform playerRodSocket;
    [SerializeField] private Transform playerLineStartPoint;
    [SerializeField] private float playerHookDelay = 0.8f;
    [SerializeField] private float fishFocusDelay = 1f;

    [Header("Player Fishing Rod Swing")]
    [SerializeField] private Vector3 playerRodSwingRotation = new Vector3(20f, 0f, 0f);
    [SerializeField] private float playerRodSwingTime = 0.25f;

    [Header("Line Visual")]
    [SerializeField] private LineRenderer aiFishingLine;
    [SerializeField] private LineRenderer playerFishingLine;
    [SerializeField] private Material fishingLineMaterial;
    [SerializeField] private Color fishingLineColor = new Color(1f, 0.95f, 0.7f, 1f);
    [SerializeField] private float fishingLineWidth = 0.02f;

    [Header("Gauge UI")]
    [SerializeField] private GameObject gaugeRoot;
    [SerializeField] private Slider playerGaugeSlider;
    [SerializeField] private Slider aiGaugeSlider;

    [Header("Fish Visual")]
    [Range(0f, 1f)]
    [SerializeField] private float idleSaturation = 0.08f;
    [SerializeField] private float glowPower = 2f;
    [SerializeField] private float fishWiggleScale = 1.18f;
    [SerializeField] private float fishWiggleDistance = 0.35f;
    [SerializeField] private float fishWiggleTime = 0.14f;
    [SerializeField] private bool hideAfterResult = true;
    [SerializeField] private bool debugLog = true;

    private float hookStartTime;
    private float contestTimer;
    private bool isInitialized;
    private Renderer fishRenderer;
    private Transform aiLineStartPoint;
    private Coroutine aiCatchCoroutine;
    private Coroutine hookByPlayerCoroutine;
    private Coroutine playerRodSwingCoroutine;
    private float playerHookReactionTime = -1f;
    private bool isFishFocusReady;

    private FishLineController lineController;
    private FishGaugeUI gaugeUI;
    private FishVisualController visualController;

    public bool CanReceivePlayerInput => currentState == FishState.HookedByAI || currentState == FishState.Contest;
    public float PlayerGauge => playerGauge;
    public float AIGauge => aiGauge;
    public float ContestTimer => contestTimer;

    private void Start()
    {
        InitializeFish();
    }

    private void Update()
    {
        if (!isInitialized)
        {
            InitializeFish();
        }

        if (!isInitialized)
        {
            return;
        }

        if (currentState == FishState.Contest)
        {
            UpdateContest();
        }

        UpdateFishingLines();
    }

    private void LateUpdate()
    {
        if (playerFishingRod != null && playerFishingRod.activeSelf && playerRodSocket != null)
        {
            Transform rod = playerFishingRod.transform;
            rod.position = playerRodSocket.position;
            Vector3 direction = GetFishPosition() - rod.position;
            if (direction.sqrMagnitude > 0.01f) rod.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }

    public void StartAICatch(Transform startPoint)
    {
        gameObject.SetActive(true);
        InitializeFish();

        if (!isInitialized)
        {
            return;
        }

        aiLineStartPoint = startPoint;

        if (aiCatchCoroutine != null)
        {
            StopCoroutine(aiCatchCoroutine);
        }

        aiCatchCoroutine = StartCoroutine(AICatchCoroutine());
    }

    public void SetEnemyIntro(EnemyStageIntro intro)
    {
        enemyIntro = intro;
    }

    public void PressF()
    {
        if (currentState == FishState.HookedByAI)
        {
            StartPlayerHook();
            return;
        }

        if (currentState == FishState.Contest)
        {
            AddPlayerMashGauge();
        }
    }

    private void StartPlayerHook()
    {
        if (hookByPlayerCoroutine != null)
        {
            return;
        }

        playerHookReactionTime = Time.time - hookStartTime;
        gaugeUI.ClearArrival();
        MoveToFishFocusCamera();
        hookByPlayerCoroutine = StartCoroutine(HookByPlayerCoroutine());
    }

    private void AddPlayerMashGauge()
    {
        playerGauge = Mathf.Clamp(playerGauge + playerMashPower, 0f, maxGauge);
        visualController.PlayWiggle();
        gaugeUI.UpdateGauge(playerGauge, aiGauge);

        if (debugLog)
        {
            Debug.Log($"{name}: F 연타 / Player {playerGauge:0} / AI {aiGauge:0}");
        }
    }

    private void UpdateFishingLines()
    {
        if (currentState == FishState.HookedByAI || currentState == FishState.HookedByPlayer || currentState == FishState.Contest)
        {
            lineController.UpdateAILine(aiLineStartPoint, GetFishLinePosition());
        }

        if (currentState == FishState.HookedByPlayer || currentState == FishState.Contest)
        {
            lineController.UpdatePlayerLine(playerLineStartPoint, GetFishLinePosition());
        }
    }
}
