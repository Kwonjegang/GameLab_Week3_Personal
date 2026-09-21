using UnityEngine;

public class FishSteelController : MonoBehaviour
{
    [Header("Steal Input")]
    [SerializeField] private float stealRange = 30f;
    [SerializeField] private bool debugLog = true;

    private InputSystem_Actions inputActions;
    private PlayerController playerController;
    private UnderwaterFishSpawner underwaterSpawner;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        playerController = GetComponent<PlayerController>();
        underwaterSpawner = FindFirstObjectByType<UnderwaterFishSpawner>();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (!inputActions.Player.Attack.WasPressedThisFrame()) return;
        if (playerController.IsGameOver) return;
        if (playerController.IsInWater && !playerController.isStageStarted)
        {
            if (underwaterSpawner != null) underwaterSpawner.TryParry();
            return;
        }
        if (playerController.isStageStarted) TryPressF();
    }

    private void TryPressF()
    {
        FishTargetNew fish = FindNearestFish();

        if (fish == null)
        {
            if (debugLog)
            {
                Debug.Log("F 입력: 반응 가능한 물고기가 범위 안에 없음");
            }

            return;
        }

        fish.PressF();
    }

    private FishTargetNew FindNearestFish()
    {
        FishTargetNew[] fishes = FindObjectsByType<FishTargetNew>(FindObjectsSortMode.None);

        FishTargetNew nearestFish = null;
        float nearestDistance = stealRange;

        foreach (FishTargetNew fish in fishes)
        {
            if (fish == null || !fish.CanReceivePlayerInput)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, fish.transform.position);

            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearestFish = fish;
            }
        }

        return nearestFish;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stealRange);
    }
}
