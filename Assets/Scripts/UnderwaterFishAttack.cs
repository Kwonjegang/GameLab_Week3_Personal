using System.Collections;
using UnityEngine;

public class UnderwaterFishAttack : MonoBehaviour
{
    private static readonly float[] Speeds = { 19f, 25f, 32f };
    private static readonly int[] Values = { 500, 1000, 1500 };
    private PlayerController player;
    private PlayerFishProgress progress;
    private UnderwaterFishSpawner spawner;
    private Renderer[] renderers;
    private Vector3 modelForwardEuler;
    [SerializeField, Range(0.05f, 1f)] private float parryTimeScale = 0.35f;
    [SerializeField] private float parrySlowSeconds = 0.38f;
    private bool timeSlowed;
    private float previousTimeScale;
    private float speed;
    private int value;
    private bool glowing;
    private bool finished;
    private bool charging;
    private Vector3 baseScale;
    private float waterHeight;
    private FishGaugeUI ui;

    public void Initialize(PlayerController target, PlayerFishProgress owner, UnderwaterFishSpawner source,
        int type, float surfaceY, Vector3 forwardOffset)
    {
        player = target;
        progress = owner;
        spawner = source;
        speed = Speeds[Mathf.Clamp(type, 0, Speeds.Length - 1)];
        value = Values[Mathf.Clamp(type, 0, Values.Length - 1)];
        waterHeight = surfaceY;
        modelForwardEuler = forwardOffset;
        ui = FindFirstObjectByType<FishGaugeUI>();
        baseScale = transform.localScale;
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void Update()
    {
        if (player == null || finished) return;
        if (!player.IsInWater || progress.IsGameOver) { Finish(); return; }
        Vector3 target = AttackTarget();
        Vector3 toTarget = target - transform.position;
        if (!charging)
        {
            if (toTarget.magnitude > 65f) { Finish(); return; }
            if (toTarget.magnitude <= 55f)
            {
                charging = true;
            }
            else return;
        }
        Vector3 direction = toTarget.normalized;
        Quaternion facing = Quaternion.LookRotation(direction) * Quaternion.Euler(modelForwardEuler);
        transform.rotation = facing;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        float remaining = Vector3.Distance(transform.position, target);
        if (!glowing && remaining <= speed * 0.45f && remaining > 3.5f) SetGlow(true);
        if (Vector3.Distance(transform.position, target) < 3.5f)
        {
            progress.ChangeStress(10f);
            Finish();
        }
    }

    public bool TryParry()
    {
        if (!glowing || finished || player == null) return false;
        if (Vector3.Distance(transform.position, AttackTarget()) > 18f) return false;
        finished = true;
        RestoreTimeScale();
        if (ui != null) ui.ShowParryResult("PARRY!");
        StartCoroutine(StunAndAbsorb());
        return true;
    }

    private IEnumerator StunAndAbsorb()
    {
        SetGlow(false);
        float stunnedTime = 0f;
        while (stunnedTime < 0.22f)
        {
            stunnedTime += Time.deltaTime;
            transform.Rotate(0f, 0f, 300f * Time.deltaTime, Space.Self);
            yield return null;
        }
        Vector3 start = transform.position;
        float elapsed = 0f;
        while (elapsed < 0.45f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.45f);
            Vector3 target = AttackTarget();
            transform.position = Vector3.Lerp(start, target, t * t);
            transform.localScale = baseScale * Mathf.Lerp(1f, 0.1f, t);
            yield return null;
        }
        progress.AddFish(value);
        Finish();
    }

    private void SetGlow(bool enabled)
    {
        glowing = enabled;
        if (enabled)
        {
            if (ui != null) ui.ShowParryCue();
            if (!timeSlowed) StartCoroutine(ParrySlowMotion());
        }
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].materials;
            foreach (Material material in materials)
            {
                if (!material.HasProperty("_EmissionColor")) continue;
                if (enabled)
                {
                    material.EnableKeyword("_EMISSION");
                    Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.white;
                    material.SetColor("_EmissionColor", color * 1.6f);
                }
                else material.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    private Vector3 AttackTarget()
    {
        Vector3 target = player.transform.position + Vector3.up;
        target.y = Mathf.Min(target.y, waterHeight - 1f);
        return target;
    }

    private IEnumerator ParrySlowMotion()
    {
        previousTimeScale = Time.timeScale;
        timeSlowed = true;
        Time.timeScale = Mathf.Max(0.05f, previousTimeScale * parryTimeScale);
        yield return new WaitForSecondsRealtime(parrySlowSeconds);
        RestoreTimeScale();
    }

    private void RestoreTimeScale()
    {
        if (!timeSlowed) return;
        Time.timeScale = previousTimeScale;
        timeSlowed = false;
    }

    private void Finish()
    {
        RestoreTimeScale();
        if (ui != null) ui.HideCountdown();
        if (spawner != null) spawner.FishFinished(this);
        Destroy(gameObject);
    }

    private void OnDestroy() { RestoreTimeScale(); }
}
