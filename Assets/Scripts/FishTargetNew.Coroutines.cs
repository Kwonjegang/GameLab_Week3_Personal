using System.Collections;
using UnityEngine;

public partial class FishTargetNew
{
    private IEnumerator HookByPlayerCoroutine()
    {
        currentState = FishState.HookedByPlayer;

        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger("Swing");
            playerAnimator.SetTrigger("Swing");
        }

        if (playerFishingRod != null)
        {
            playerFishingRod.SetActive(true);

            if (playerRodSwingCoroutine != null)
            {
                StopCoroutine(playerRodSwingCoroutine);
            }

            playerRodSwingCoroutine = StartCoroutine(PlayerRodSwingCoroutine());
        }

        lineController.UpdateAILine(aiLineStartPoint, GetFishLinePosition());
        lineController.UpdatePlayerLine(playerLineStartPoint, GetFishLinePosition());

        yield return new WaitForSeconds(playerHookDelay);
        gaugeUI.ClearArrival();
        MoveToFishFocusCamera();
        yield return new WaitForSeconds(fishFocusDelay);
        StartContest();

        hookByPlayerCoroutine = null;
    }

    private IEnumerator PlayerRodSwingCoroutine()
    {
        if (playerFishingRod == null)
        {
            yield break;
        }

        Transform swingingPart = playerFishingRod.transform.childCount > 0 ? playerFishingRod.transform.GetChild(0) : playerFishingRod.transform;
        Quaternion readyRotation = swingingPart.localRotation;
        Quaternion swingRotation = readyRotation * Quaternion.Euler(playerRodSwingRotation);

        float timer = 0f;

        while (timer < playerRodSwingTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / playerRodSwingTime);
            swingingPart.localRotation = Quaternion.Slerp(readyRotation, swingRotation, t);
            yield return null;
        }

        timer = 0f;

        while (timer < playerRodSwingTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / playerRodSwingTime);
            swingingPart.localRotation = Quaternion.Slerp(swingRotation, readyRotation, t);
            yield return null;
        }

        swingingPart.localRotation = readyRotation;
        playerRodSwingCoroutine = null;
    }

    private IEnumerator AICatchCoroutine()
    {
        SetIdle();

        Vector3 startPosition = GetFishPosition();
        Vector3 horizontalDirection = Vector3.forward;

        if (aiLineStartPoint != null)
        {
            horizontalDirection = aiLineStartPoint.position - startPosition;
            horizontalDirection.y = 0f;

            if (horizontalDirection.sqrMagnitude > 0.01f)
            {
                horizontalDirection.Normalize();
            }
            else
            {
                horizontalDirection = Vector3.forward;
            }
        }

        float angle = liftAngle * Mathf.Deg2Rad;
        Vector3 liftDirection = horizontalDirection * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle);
        Vector3 endPosition = startPosition + liftDirection.normalized * liftDistance;

        float countdownStep = Random.Range(countdownStepRange.x, countdownStepRange.y);
        float flightDuration = countdownStep * 3f;
        float timer = 0f;
        int shownNumber = 0;

        while (timer < flightDuration)
        {
            timer += Time.deltaTime;
            int number = Mathf.Clamp(3 - Mathf.FloorToInt(timer / countdownStep), 1, 3);
            if (number != shownNumber)
            {
                shownNumber = number;
                gaugeUI.ShowCountdown(number);
            }
            float t = Mathf.Clamp01(timer / flightDuration);
            SetFishPosition(Vector3.Lerp(startPosition, endPosition, t));
            lineController.UpdateAILine(aiLineStartPoint, GetFishLinePosition());
            yield return null;
        }

        SetFishPosition(endPosition);
        gaugeUI.HideCountdown();
        HookByAI();

        timer = 0f;

        while (timer < Mathf.Min(glowDuration, goodTime) && currentState == FishState.HookedByAI)
        {
            timer += Time.deltaTime;
            lineController.UpdateAILine(aiLineStartPoint, GetFishLinePosition());
            yield return null;
        }

        if (currentState == FishState.HookedByAI)
        {
            contestBalance = -maxGauge;
            FinishContest();
        }

        aiCatchCoroutine = null;
    }

    private IEnumerator MoveFishToWinner(bool playerWin)
    {
        Transform winner = playerWin ? playerAnimator != null ? playerAnimator.transform : null : aiLineStartPoint;
        if (winner == null) winner = playerWin ? playerLineStartPoint : aiLineStartPoint;
        Vector3 start = GetFishPosition();
        Vector3 destination = winner != null ? winner.position + Vector3.up * 2f : start;
        visualController.SetWinGlow();
        float elapsed = 0f;
        const float duration = 0.65f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetFishPosition(Vector3.Lerp(start, destination, t) + Vector3.up * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }
        SetFishPosition(destination);
        visualController.RestoreOriginal();
        if (playerWin && (playerProgress == null || !playerProgress.IsGameOver))
        {
            int reward = Random.Range(0, 3);
            int value = reward == 0 ? 500 : reward == 1 ? 1000 : 1500;
            int count = reward == 2 ? 2 : 3;
            if (playerProgress != null)
            {
                playerProgress.AddFish(value, count);
                playerProgress.ChangeStress(-35f);
            }
            gaugeUI.ShowRewardResult(value, count);
            yield return new WaitForSeconds(1.1f);
            gaugeUI.HideRewardResult();
        }
        if (roundsCompleted >= 4 || (playerProgress != null && playerProgress.IsGameOver))
        {
            stageComplete = true;
            gaugeUI.HideRewardResult();
            if (playerFishingRod != null) playerFishingRod.SetActive(false);
            if (enemyIntro != null) enemyIntro.ResetStage();
            PlayerController player = playerAnimator != null ? playerAnimator.GetComponent<PlayerController>() : null;
            if (player != null && (playerProgress == null || !playerProgress.IsGameOver)) player.EndStage(false, gaugeUI);
            if (hideAfterResult) gameObject.SetActive(false);
            yield break;
        }
        yield return new WaitForSeconds(nextRoundDelay);
        if (playerFishingRod != null) playerFishingRod.SetActive(false);
        if (enemyIntro != null) yield return enemyIntro.WaitForAIFishGetCamera();
        SetFishPosition(initialFishPosition);
        if (enemyIntro != null) yield return enemyIntro.ReplayFishingSwingAndWait();
        StartAICatch(aiLineStartPoint);
    }
}
