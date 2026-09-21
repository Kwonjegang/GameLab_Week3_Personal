using UnityEngine;
using UnityEngine.Rendering;

public class FishLineController : MonoBehaviour
{
    [Header("Line Renderers")]
    [SerializeField] private LineRenderer aiFishingLine;
    [SerializeField] private LineRenderer playerFishingLine;

    [Header("Line Visual")]
    [SerializeField] private Material fishingLineMaterial;
    [SerializeField] private Color fishingLineColor = new Color(1f, 0.95f, 0.7f, 1f);
    [SerializeField] private float fishingLineWidth = 0.02f;

    public void Configure(LineRenderer aiLine, LineRenderer playerLine, Material lineMaterial, Color lineColor, float lineWidth)
    {
        aiFishingLine = aiLine != null ? aiLine : aiFishingLine;
        playerFishingLine = playerLine != null ? playerLine : playerFishingLine;
        fishingLineMaterial = lineMaterial != null ? lineMaterial : fishingLineMaterial;
        fishingLineColor = lineColor;
        fishingLineWidth = lineWidth;
    }

    public void Initialize()
    {
        PrepareLineRenderer(aiFishingLine);
        PrepareLineRenderer(playerFishingLine);
        HideAll();
    }

    public void UpdateAILine(Transform startPoint, Vector3 fishLinePosition)
    {
        UpdateLine(aiFishingLine, startPoint, fishLinePosition);
    }

    public void UpdatePlayerLine(Transform startPoint, Vector3 fishLinePosition)
    {
        UpdateLine(playerFishingLine, startPoint, fishLinePosition);
    }

    public void HideAll()
    {
        if (aiFishingLine != null)
        {
            aiFishingLine.enabled = false;
        }

        if (playerFishingLine != null)
        {
            playerFishingLine.enabled = false;
        }
    }

    private void UpdateLine(LineRenderer targetLine, Transform startPoint, Vector3 fishLinePosition)
    {
        if (targetLine == null || startPoint == null)
        {
            return;
        }

        ApplyLineVisual(targetLine);
        targetLine.enabled = true;
        targetLine.positionCount = 2;
        targetLine.SetPosition(0, startPoint.position);
        targetLine.SetPosition(1, fishLinePosition);
    }

    private void PrepareLineRenderer(LineRenderer targetLine)
    {
        if (targetLine == null)
        {
            return;
        }

        targetLine.useWorldSpace = true;
        targetLine.positionCount = 2;
        targetLine.numCapVertices = 2;
        targetLine.numCornerVertices = 2;
        targetLine.textureMode = LineTextureMode.Stretch;
        targetLine.shadowCastingMode = ShadowCastingMode.Off;
        targetLine.receiveShadows = false;

        if (fishingLineMaterial != null)
        {
            targetLine.material = fishingLineMaterial;
        }
        else if (targetLine.sharedMaterial == null)
        {
            Shader lineShader = Shader.Find("Universal Render Pipeline/Unlit");

            if (lineShader == null)
            {
                lineShader = Shader.Find("Sprites/Default");
            }

            if (lineShader != null)
            {
                targetLine.material = new Material(lineShader);
            }
        }

        ApplyLineVisual(targetLine);
    }

    private void ApplyLineVisual(LineRenderer targetLine)
    {
        if (targetLine == null)
        {
            return;
        }

        targetLine.startWidth = fishingLineWidth;
        targetLine.endWidth = fishingLineWidth;
        targetLine.startColor = fishingLineColor;
        targetLine.endColor = fishingLineColor;
    }
}
