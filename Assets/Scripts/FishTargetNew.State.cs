using UnityEngine;

public partial class FishTargetNew
{
    private void SetIdle()
    {
        currentState = FishState.Idle;
        isFishFocusReady = false;
        playerHookReactionTime = -1f;
        contestBalance = 0f;

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
        gaugeUI.ShowArrival(fishRenderer);
        gaugeUI.ShowParryCue();

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
            contestBalance = perfectBonus;
            timingName = "Perfect";
        }
        else if (reactionTime <= goodTime)
        {
            contestBalance = goodBonus;
            timingName = "Good";
        }
        else
        {
            contestBalance = goodBonus;
            timingName = "Good";
        }

        currentState = FishState.Contest;

        gaugeUI.ClearArrival();
        gaugeUI.HideCountdown();
        MoveToFishFocusCamera();
        gaugeUI.Show(contestBalance, maxGauge);

        if (debugLog)
        {
            Debug.Log($"{name}: {timingName} / 반응 시간 {reactionTime:0.00}초 / 시작 위치 {contestBalance:0}");
        }
    }

    private void UpdateContest()
    {
        contestBalance = Mathf.Clamp(contestBalance - aiPowerPerSecond * Time.deltaTime, -maxGauge, maxGauge);
        gaugeUI.UpdateGauge(contestBalance, maxGauge);
        if (contestBalance <= -maxGauge || contestBalance >= maxGauge) FinishContest();
    }

    private void FinishContest()
    {
        bool playerWin = contestBalance >= maxGauge;
        currentState = playerWin ? FishState.PlayerWin : FishState.AIWin;
        if (playerWin) aiStress = Mathf.Min(100f, aiStress + 25f);
        else playerStress = Mathf.Min(100f, playerStress + 25f);
        gaugeUI.UpdateStress(playerStress, aiStress);
        CharacterDamageFlash.Play(playerWin ? enemyIntro != null ? enemyIntro.transform : null :
            playerAnimator != null ? playerAnimator.transform : null);

        visualController.RestoreOriginal();
        lineController.HideAll();
        gaugeUI.Hide();

        if (debugLog)
        {
            string result = playerWin ? "플레이어 승리, 물고기 훔치기 성공" : "AI 승리, 물고기를 빼앗지 못함";
            Debug.Log($"{name}: 대결 종료 / 줄다리기 {contestBalance:0} / Player Stress {playerStress:0}% / AI Stress {aiStress:0}% / {result}");
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
