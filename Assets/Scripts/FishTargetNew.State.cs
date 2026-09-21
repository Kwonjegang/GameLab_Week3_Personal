using UnityEngine;

public partial class FishTargetNew
{
    private void SetIdle()
    {
        currentState = FishState.Idle;
        isFishFocusReady = false;
        playerHookReactionTime = -1f;
        playerGauge = 0f;
        aiGauge = 0f;
        contestTimer = 0f;

        visualController.SetIdle();
        lineController.HideAll();
        gaugeUI.Hide();
    }

    private void HookByAI()
    {
        if (currentState != FishState.Idle)
        {
            return;
        }

        currentState = FishState.HookedByAI;
        isFishFocusReady = false;
        hookStartTime = Time.time;
        visualController.SetGlow();
        gaugeUI.ShowArrival();

        if (debugLog)
        {
            Debug.Log($"{name}: 물고기가 빛남. 지금 F를 누르면 타이밍 판정");
        }
    }

    private void StartContest()
    {
        float reactionTime = playerHookReactionTime >= 0f ? playerHookReactionTime : Time.time - hookStartTime;
        playerHookReactionTime = -1f;

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

        gaugeUI.ClearArrival();
        MoveToFishFocusCamera();
        gaugeUI.Show(playerGauge, aiGauge);

        if (debugLog)
        {
            Debug.Log($"{name}: {timingName} / 반응 시간 {reactionTime:0.00}초 / 시작 보너스 {playerGauge:0}");
        }
    }

    private void UpdateContest()
    {
        contestTimer -= Time.deltaTime;
        aiGauge = Mathf.Clamp(aiGauge + aiPowerPerSecond * Time.deltaTime, 0f, maxGauge);

        gaugeUI.UpdateGauge(playerGauge, aiGauge);

        if (contestTimer <= 0f || playerGauge >= maxGauge || aiGauge >= maxGauge)
        {
            FinishContest();
        }
    }

    private void FinishContest()
    {
        bool playerWin = playerGauge >= aiGauge;
        currentState = playerWin ? FishState.PlayerWin : FishState.AIWin;

        visualController.RestoreOriginal();
        lineController.HideAll();
        gaugeUI.Hide();

        if (debugLog)
        {
            string result = playerWin ? "플레이어 승리, 물고기 훔치기 성공" : "AI 승리, 물고기를 빼앗지 못함";
            Debug.Log($"{name}: 대결 종료 / Player {playerGauge:0} / AI {aiGauge:0} / {result}");
        }

        StartCoroutine(MoveFishToWinner(playerWin));
    }

    private Vector3 GetFishPosition()
    {
        return fishMoveTarget != null ? fishMoveTarget.position : transform.position;
    }

    private void SetFishPosition(Vector3 position)
    {
        if (fishMoveTarget != null)
        {
            fishMoveTarget.position = position;
        }
        else
        {
            transform.position = position;
        }
    }

    private Vector3 GetFishLinePosition()
    {
        return GetFishPosition() + fishLineOffset;
    }

    private void MoveToFishFocusCamera()
    {
        if (isFishFocusReady)
        {
            return;
        }

        if (enemyIntro != null)
        {
            enemyIntro.SwitchToFishFocusCamera();
        }

        if (fishFocusPoint != null)
        {
            SetFishPosition(fishFocusPoint.position);
            lineController.UpdateAILine(aiLineStartPoint, GetFishLinePosition());
            lineController.UpdatePlayerLine(playerLineStartPoint, GetFishLinePosition());
        }

        isFishFocusReady = true;
    }
}
