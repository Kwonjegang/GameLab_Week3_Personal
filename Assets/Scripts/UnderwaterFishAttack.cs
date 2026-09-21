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
    private Vector3 attackDirection;
    private float speed;
    private int value;
    private bool glowing;
    private bool finished;
    private bool charging;
    private Vector3 baseScale;
    private float waterHeight;
    private FishGaugeUI ui;

    public void Initialize(PlayerController target, PlayerFishProgress owner, UnderwaterFishSpawner source, int type, float surfaceY)
    {
        player = target;
        progress = owner;
        spawner = source;
        speed = Speeds[Mathf.Clamp(type, 0, Speeds.Length - 1)];
        value = Values[Mathf.Clamp(type, 0, Values.Length - 1)];
        waterHeight = surfaceY;
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
                attackDirection = toTarget.normalized;
                transform.rotation = Quaternion.LookRotation(attackDirection);
            }
            return;
        }
        // Direction is captured once: the rush stays straight even if the player dodges.
        transform.position += attackDirection * speed * Time.deltaTime;
        float remaining = Vector3.Dot(target - transform.position, attackDirection);
        if (!glowing && remaining <= speed * 0.45f && remaining > 0f) SetGlow(true);
        if (Vector3.Distance(transform.position, target) < 3.5f)
        {
            progress.ChangeStress(10f);
            Finish();
        }
        else if (remaining < -6f) Finish();
    }

    public bool TryParry()
    {
        if (!glowing || finished || player == null) return false;
        if (Vector3.Distance(transform.position, AttackTarget()) > 18f) return false;
        finished = true;
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
        if (enabled && ui != null) ui.ShowParryCue();
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

    private void Finish()
    {
        if (ui != null) ui.HideCountdown();
        if (spawner != null) spawner.FishFinished(this);
        Destroy(gameObject);
    }
}
