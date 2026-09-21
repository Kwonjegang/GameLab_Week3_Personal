using UnityEngine;

public class UnderwaterFishSpawner : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerFishProgress progress;
    [SerializeField] private GameObject[] fishPrefabs;
    [SerializeField] private Transform waterSurface;
    [SerializeField] private Vector2 spawnDelay = new Vector2(1.5f, 3f);
    [SerializeField] private Vector2 spawnDistance = new Vector2(30f, 48f);
    [SerializeField] private float underwaterDepth = 7f;
    [SerializeField] private Vector3 fishHeadForwardOffset;

    private UnderwaterFishAttack activeFish;
    private float nextSpawnTime;

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (player != null && progress == null) progress = player.GetComponent<PlayerFishProgress>();
        nextSpawnTime = Time.time + Random.Range(spawnDelay.x, spawnDelay.y);
    }

    private void Update()
    {
        if (player == null || progress == null || progress.IsGameOver) return;
        if (!player.IsInWater || player.isStageStarted)
        {
            if (activeFish != null) Destroy(activeFish.gameObject);
            nextSpawnTime = Time.time + Random.Range(spawnDelay.x, spawnDelay.y);
            return;
        }
        if (activeFish != null || Time.time < nextSpawnTime || fishPrefabs == null || fishPrefabs.Length == 0) return;

        int index = Random.Range(0, fishPrefabs.Length);
        if (fishPrefabs[index] == null) return;
        Vector2 direction = Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude < 0.01f) direction = Vector2.right;
        float distance = Random.Range(spawnDistance.x, spawnDistance.y);
        Vector3 position = player.transform.position + new Vector3(direction.x * distance, 0f, direction.y * distance);
        if (waterSurface != null) position.y = Mathf.Min(player.transform.position.y + 2f, waterSurface.position.y - underwaterDepth);
        Renderer surface = waterSurface != null ? waterSurface.GetComponent<Renderer>() : null;
        if (surface != null)
        {
            position.x = Mathf.Clamp(position.x, surface.bounds.min.x + 3f, surface.bounds.max.x - 3f);
            position.z = Mathf.Clamp(position.z, surface.bounds.min.z + 3f, surface.bounds.max.z - 3f);
        }
        GameObject fish = Instantiate(fishPrefabs[index], position, Quaternion.identity);
        fish.transform.localScale = fishPrefabs[index].transform.localScale * 3f;
        activeFish = fish.AddComponent<UnderwaterFishAttack>();
        activeFish.Initialize(player, progress, this, index,
            waterSurface != null ? waterSurface.position.y : player.transform.position.y + 2f,
            fishHeadForwardOffset);
    }

    public bool TryParry()
    {
        return activeFish != null && activeFish.TryParry();
    }

    public void FishFinished(UnderwaterFishAttack fish)
    {
        if (activeFish == fish) activeFish = null;
        nextSpawnTime = Time.time + Random.Range(spawnDelay.x, spawnDelay.y);
    }
}
