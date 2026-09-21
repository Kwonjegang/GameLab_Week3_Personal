using UnityEngine;

public class PlayerFishProgress : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)] private float stress;
    [SerializeField] private int fish500;
    [SerializeField] private int fish1000;
    [SerializeField] private int fish1500;
    [SerializeField] private FishGaugeUI ui;

    private PlayerController player;
    private bool gameOver;

    public float Stress => stress;
    public int TotalValue => fish500 * 500 + fish1000 * 1000 + fish1500 * 1500;
    public bool IsGameOver => gameOver;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        if (ui == null) ui = FindFirstObjectByType<FishGaugeUI>();
    }

    private void Start() { RefreshUI(); }

    private void Update()
    {
        if (ui == null || gameOver || player == null) return;
        if (player.IsInWater || player.isStageStarted) ui.ShowStress();
        else ui.HideStress();
    }

    public void AddFish(int value, int count = 1)
    {
        if (gameOver || count <= 0) return;
        if (value == 500) fish500 += count;
        else if (value == 1000) fish1000 += count;
        else if (value == 1500) fish1500 += count;
        RefreshUI();
    }

    public void ChangeStress(float amount)
    {
        if (gameOver) return;
        stress = Mathf.Clamp(stress + amount, 0f, 100f);
        RefreshUI();
        if (stress >= 100f)
        {
            gameOver = true;
            if (player != null)
            {
                if (player.IsInWater) player.DrownGameOver(ui);
                else player.EndStage(true, ui);
            }
        }
    }

    private void RefreshUI()
    {
        if (ui == null) return;
        ui.UpdateStress(stress, 0f);
        ui.UpdateInventory(fish500, fish1000, fish1500, TotalValue);
    }
}
