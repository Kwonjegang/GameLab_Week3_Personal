using DG.Tweening;
using UnityEngine;

public class FishVisualController : MonoBehaviour
{
    [Header("Material")]
    [Range(0f, 1f)]
    [SerializeField] private float idleSaturation = 0.08f;
    [SerializeField] private float glowPower = 0.5f;

    [Header("Wiggle")]
    [SerializeField] private float fishWiggleScale = 0.85f;
    [SerializeField] private float fishWiggleDistance = 0.15f;
    [SerializeField] private float fishWiggleTime = 0.2f;

    private Renderer fishRenderer;
    private Transform fishMoveTarget;
    private Material[] originalMaterials;
    private Material[] grayMaterials;
    private Material[] glowMaterials;
    private Material[] winMaterials;
    private Tween fishWiggleTween;
    private LineRenderer winRing;
    private Vector3 fishBaseScale;
    private bool isInitialized;

    public void Configure(Renderer targetRenderer, Transform moveTarget, float saturation, float emissionPower, float wiggleScale, float wiggleDistance, float wiggleTime)
    {
        fishRenderer = targetRenderer;
        fishMoveTarget = moveTarget;
        idleSaturation = saturation;
        glowPower = emissionPower;
        fishWiggleScale = wiggleScale;
        fishWiggleDistance = wiggleDistance;
        fishWiggleTime = wiggleTime;
    }

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        if (fishRenderer == null)
        {
            fishRenderer = GetComponentInChildren<Renderer>();
        }

        if (fishMoveTarget == null && fishRenderer != null)
        {
            fishMoveTarget = fishRenderer.transform;
        }

        if (fishRenderer == null || fishMoveTarget == null)
        {
            return;
        }

        fishBaseScale = fishMoveTarget.localScale;
        MakeVisualMaterials();
        isInitialized = true;
    }

    public void RefreshMaterials(Renderer targetRenderer, Transform moveTarget)
    {
        if (fishWiggleTween != null) fishWiggleTween.Kill();
        fishRenderer = targetRenderer;
        fishMoveTarget = moveTarget;
        isInitialized = false;
        Initialize();
        SetIdle();
    }

    public void SetIdle()
    {
        if (!isInitialized)
        {
            Initialize();
        }

        if (fishRenderer != null && grayMaterials != null)
        {
            fishRenderer.materials = grayMaterials;
        }
    }

    public void SetGlow()
    {
        if (!isInitialized)
        {
            Initialize();
        }

        if (fishRenderer != null && glowMaterials != null)
        {
            fishRenderer.materials = glowMaterials;
        }
    }

    public void SetWinGlow()
    {
        if (!isInitialized) Initialize();
        if (fishRenderer != null && winMaterials != null) fishRenderer.materials = winMaterials;
        if (winRing == null)
        {
            GameObject ring = new GameObject("FishWinWhiteRing");
            ring.transform.SetParent(transform, false);
            winRing = ring.AddComponent<LineRenderer>();
            winRing.material = new Material(Shader.Find("Sprites/Default"));
            winRing.startColor = winRing.endColor = Color.white;
            winRing.startWidth = winRing.endWidth = 0.09f;
            winRing.useWorldSpace = true;
            winRing.loop = true;
            winRing.positionCount = 48;
        }
        winRing.enabled = true;
    }

    private void LateUpdate()
    {
        if (winRing == null || !winRing.enabled || fishRenderer == null) return;
        Camera camera = Camera.main;
        Vector3 right = camera != null ? camera.transform.right : Vector3.right;
        Vector3 up = camera != null ? camera.transform.up : Vector3.up;
        Vector3 center = fishRenderer.bounds.center;
        float radius = Mathf.Max(fishRenderer.bounds.extents.magnitude * 1.15f, 0.5f);
        for (int i = 0; i < 48; i++)
        {
            float angle = i * Mathf.PI * 2f / 48f;
            winRing.SetPosition(i, center + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius);
        }
    }

    public void RestoreOriginal()
    {
        if (winRing != null) winRing.enabled = false;
        if (fishWiggleTween != null)
        {
            fishWiggleTween.Kill();
            fishWiggleTween = null;
        }

        if (fishMoveTarget != null)
        {
            fishMoveTarget.localScale = fishBaseScale;
        }

        if (fishRenderer != null && originalMaterials != null)
        {
            fishRenderer.materials = originalMaterials;
        }
    }

    public void PlayWiggle()
    {
        if (!isInitialized)
        {
            Initialize();
        }

        if (fishMoveTarget == null)
        {
            return;
        }

        if (fishWiggleTween != null)
        {
            fishWiggleTween.Kill();
        }

        Vector3 startPosition = fishMoveTarget.position;
        Vector3 randomOffset = new Vector3(Random.Range(-fishWiggleDistance, fishWiggleDistance), Random.Range(-fishWiggleDistance, fishWiggleDistance), Random.Range(-fishWiggleDistance, fishWiggleDistance) * 0.4f);

        DG.Tweening.Sequence sequence = DOTween.Sequence();
        sequence.Append(fishMoveTarget.DOScale(fishBaseScale * fishWiggleScale, fishWiggleTime));
        sequence.Join(fishMoveTarget.DOMove(startPosition + randomOffset, fishWiggleTime));
        sequence.Append(fishMoveTarget.DOScale(fishBaseScale, fishWiggleTime));
        sequence.Join(fishMoveTarget.DOMove(startPosition, fishWiggleTime));

        fishWiggleTween = sequence;
    }

    private void MakeVisualMaterials()
    {
        originalMaterials = fishRenderer.materials;
        grayMaterials = new Material[originalMaterials.Length];
        glowMaterials = new Material[originalMaterials.Length];
        winMaterials = new Material[originalMaterials.Length];

        for (int i = 0; i < originalMaterials.Length; i++)
        {
            Material original = originalMaterials[i];
            Material gray = new Material(original);
            Material glow = new Material(original);
            Material win = new Material(original);

            Color originalColor = GetMaterialColor(original);
            float grayValue = originalColor.grayscale;
            Color grayColor = new Color(grayValue, grayValue, grayValue, originalColor.a);

            SetMaterialColor(gray, Color.Lerp(grayColor, originalColor, idleSaturation));
            SetMaterialColor(glow, originalColor);
            SetMaterialColor(win, Color.white);

            if (glow.HasProperty("_EmissionColor"))
            {
                glow.EnableKeyword("_EMISSION");
                glow.SetColor("_EmissionColor", originalColor * glowPower);
            }
            if (win.HasProperty("_EmissionColor"))
            {
                win.EnableKeyword("_EMISSION");
                win.SetColor("_EmissionColor", Color.white * 3f);
            }

            grayMaterials[i] = gray;
            glowMaterials[i] = glow;
            winMaterials[i] = win;
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
