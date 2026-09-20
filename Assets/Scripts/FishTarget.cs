using UnityEngine;

public class FishTarget : MonoBehaviour
{
    public enum FishState
    {
        Idle,
        HookedByAI,
        Contest,
        PlayerWin,
        AIWin
    }

    [Header("State")]
    public FishState currentState = FishState.Idle;

    [Header("Time")]
    [SerializeField] private float autoHookDelay = 1.5f;
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

    [Header("Visual")]
    [Range(0f, 1f)]
    [SerializeField] private float idleSaturation = 0.08f;
    [SerializeField] private float glowPower = 2f;
    [SerializeField] private bool hideAfterResult = true;
    [SerializeField] private bool debugLog = true;

    private float hookStartTime;
    private float contestTimer;
    private Renderer fishRenderer;
    private Material[] originalMaterials;
    private Material[] grayMaterials;
    private Material[] glowMaterials;

    public bool CanReceivePlayerInput => currentState == FishState.HookedByAI || currentState == FishState.Contest;
    public float PlayerGauge => playerGauge;
    public float AIGauge => aiGauge;
    public float ContestTimer => contestTimer;

    private void Start()
    {
        fishRenderer = GetComponentInChildren<Renderer>();

        if (fishRenderer == null)
        {
            Debug.LogError($"{name}: Renderer가 없어서 FishTarget을 사용할 수 없음");
            enabled = false;
            return;
        }

        MakeVisualMaterials();
        SetIdle();

        Invoke(nameof(HookByAI), autoHookDelay);
    }

    private void Update()
    {
        if (currentState == FishState.Contest)
        {
            UpdateContest();
        }
    }

    public void PressF()
    {
        if (currentState == FishState.HookedByAI)
        {
            StartContest();
            return;
        }

        if (currentState == FishState.Contest)
        {
            playerGauge += playerMashPower;

            if (debugLog)
            {
                Debug.Log($"{name}: F 연타 / Player {playerGauge:0} / AI {aiGauge:0}");
            }

            return;
        }

        if (debugLog)
        {
            Debug.Log($"{name}: 지금은 F를 눌러도 반응하지 않음");
        }
    }

    private void SetIdle()
    {
        currentState = FishState.Idle;
        playerGauge = 0f;
        aiGauge = 0f;
        contestTimer = 0f;

        fishRenderer.materials = grayMaterials;

        if (debugLog)
        {
            Debug.Log($"{name}: 회색 대기 상태");
        }
    }

    private void HookByAI()
    {
        if (currentState != FishState.Idle)
        {
            return;
        }

        currentState = FishState.HookedByAI;
        hookStartTime = Time.time;

        fishRenderer.materials = glowMaterials;

        if (debugLog)
        {
            Debug.Log($"{name}: 물고기가 빛남. 지금 F를 누르면 타이밍 판정");
        }
    }

    private void StartContest()
    {
        float reactionTime = Time.time - hookStartTime;
        string timingName;

        if (reactionTime <= perfectTime)
        {
            playerGauge = perfectBonus;
            timingName = "Perfect";
        }
        else if (reactionTime <= goodTime)
        {
            playerGauge = goodBonus;
            timingName = "Good";
        }
        else if (reactionTime <= normalTime)
        {
            playerGauge = normalBonus;
            timingName = "Normal";
        }
        else
        {
            playerGauge = lateBonus;
            timingName = "Late";
        }

        aiGauge = 0f;
        contestTimer = contestDuration;
        currentState = FishState.Contest;

        if (debugLog)
        {
            Debug.Log($"{name}: {timingName} / 반응 시간 {reactionTime:0.00}초 / 시작 보너스 {playerGauge:0}");
            Debug.Log($"{name}: AI와 연타 대결 시작");
        }
    }

    private void UpdateContest()
    {
        contestTimer -= Time.deltaTime;
        aiGauge += aiPowerPerSecond * Time.deltaTime;

        if (contestTimer <= 0f)
        {
            FinishContest();
        }
    }

    private void FinishContest()
    {
        bool playerWin = playerGauge >= aiGauge;
        currentState = playerWin ? FishState.PlayerWin : FishState.AIWin;
        fishRenderer.materials = originalMaterials;

        if (debugLog)
        {
            string result = playerWin ? "플레이어 승리, 물고기 훔치기 성공" : "AI 승리, 물고기를 빼앗지 못함";
            Debug.Log($"{name}: 대결 종료 / Player {playerGauge:0} / AI {aiGauge:0} / {result}");
        }

        if (hideAfterResult)
        {
            gameObject.SetActive(false);
        }
    }

    private void MakeVisualMaterials()
    {
        originalMaterials = fishRenderer.materials;
        grayMaterials = new Material[originalMaterials.Length];
        glowMaterials = new Material[originalMaterials.Length];

        for (int i = 0; i < originalMaterials.Length; i++)
        {
            Material original = originalMaterials[i];
            Material gray = new Material(original);
            Material glow = new Material(original);

            Color originalColor = GetMaterialColor(original);
            float grayValue = originalColor.grayscale;
            Color grayColor = new Color(grayValue, grayValue, grayValue, originalColor.a);

            SetMaterialColor(gray, Color.Lerp(grayColor, originalColor, idleSaturation));
            SetMaterialColor(glow, originalColor);

            if (glow.HasProperty("_EmissionColor"))
            {
                glow.EnableKeyword("_EMISSION");
                glow.SetColor("_EmissionColor", originalColor * glowPower);
            }

            grayMaterials[i] = gray;
            glowMaterials[i] = glow;
        }
    }

    private Color GetMaterialColor(Material material)
    {
        if (material.HasProperty("_BaseColor"))
        {
            return material.GetColor("_BaseColor");
        }

        if (material.HasProperty("_Color"))
        {
            return material.GetColor("_Color");
        }

        return Color.white;
    }

    private void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }
}