using UnityEngine;

public partial class FishTargetNew
{
    private void InitializeFish()
    {
        if (isInitialized)
        {
            return;
        }

        fishRenderer = GetComponentInChildren<Renderer>();

        if (fishRenderer == null)
        {
            Debug.LogError($"{name}: Renderer가 없어서 FishTargetNew를 사용할 수 없음");
            enabled = false;
            return;
        }

        if (fishMoveTarget == null)
        {
            fishMoveTarget = fishRenderer.transform;
        }

        if (!PrepareHelperControllers())
        {
            enabled = false;
            return;
        }

        lineController.Configure(aiFishingLine, playerFishingLine, fishingLineMaterial, fishingLineColor, fishingLineWidth);
        lineController.Initialize();

        gaugeUI.Configure(gaugeRoot, playerGaugeSlider, aiGaugeSlider, maxGauge);
        gaugeUI.Initialize();
        gaugeUI.Hide();

        visualController.Configure(fishRenderer, fishMoveTarget, idleSaturation, glowPower, fishWiggleScale, fishWiggleDistance, fishWiggleTime);
        visualController.Initialize();

        if (playerFishingRod != null)
        {
            if (playerRodSocket != null)
            {
                playerFishingRod.transform.SetParent(playerRodSocket, false);
                playerFishingRod.transform.localPosition = Vector3.zero;
                playerFishingRod.transform.localRotation = Quaternion.identity;
                playerFishingRod.transform.localScale = Vector3.one;
                Transform model = playerFishingRod.transform.childCount > 0 ? playerFishingRod.transform.GetChild(0) : null;
                if (model != null)
                {
                    model.localPosition = Vector3.zero;
                    model.localScale = Vector3.one * 1.8f;
                }
                if (playerLineStartPoint != null)
                    playerLineStartPoint.localPosition = new Vector3(0f, 0f, 3f);
            }
            playerFishingRod.SetActive(false);
        }

        SetIdle();
        isInitialized = true;
    }

    private bool PrepareHelperControllers()
    {
        lineController = GetComponent<FishLineController>();
        gaugeUI = GetComponent<FishGaugeUI>();
        visualController = GetComponent<FishVisualController>();

        if (lineController == null || gaugeUI == null || visualController == null)
        {
            Debug.LogError($"{name}: FishLineController, FishGaugeUI, FishVisualController가 같은 오브젝트에 필요함");
            return false;
        }

        return true;
    }
}
