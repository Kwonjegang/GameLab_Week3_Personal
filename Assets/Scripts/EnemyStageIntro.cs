using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

public class EnemyStageIntro : MonoBehaviour
{
    [Header("Move Points")]
    [SerializeField] private Transform moveTarget;
    [SerializeField] private Transform liePoint;

    [Header("Move Setting")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float jumpMoveTime = 1.2f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float fadeTime = 1f;
    [SerializeField] private float sitTriggerRatio = 0.5f;
    [SerializeField] private string walkBoolName = "IsWalking";

    [Header("Camera Switch")]
    [SerializeField] private CinemachineCamera stageCamera;
    [SerializeField] private CinemachineCamera AIFishGetCamera;
    [SerializeField] private CinemachineCamera FishFocusCamera;

    [Header("Fishing Rod")]
    [SerializeField] private GameObject fishingRodPrefab;
    [SerializeField] private Transform fishingRodSocket;

    [Header("Fishing Line")]
    [SerializeField] private Vector3 fishingLineStartLocalPosition = new Vector3(0f, 0f, 1.5f);

    [Header("Fishing Rod Swing")]
    [SerializeField] private Vector3 rodReadyRotation = new Vector3(135f, 3.5f, 180f);
    [SerializeField] private Vector3 rodSwingRotation = new Vector3(80f, 3.5f, 160f);
    [SerializeField] private float rodSwingTime = 0.35f;

    [Header("Fish Lift")]
    [SerializeField] private FishTargetNew targetFish;
    [SerializeField] private float fishLiftDelay = 1.5f;

    private GameObject spawnedFisingRod;
    private Transform fishingLineStartPoint;
    private Renderer[] renderers;
    private Material[] materials;
    private Animator animator;
    private bool isPlaying;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        renderers = GetComponentsInChildren<Renderer>();

        List<Material> materialList = new List<Material>();

        foreach (Renderer targetRenderer in renderers)
        {
            foreach (Material material in targetRenderer.materials)
            {
                PrepareTransParentMaterial(material);
                materialList.Add(material);
            }
        }
        materials = materialList.ToArray();
        SetAlpha(0f);
    }
    public void PlayIntro()
    {
        if (isPlaying)
        {
            return;
        }
        gameObject.SetActive(true);
        StartCoroutine(IntroCoroutine());
    }
    void SpawnFishingRod()
    {
        if (spawnedFisingRod != null)
        {
            return;
        }

        if (fishingRodPrefab == null || fishingRodSocket == null)
        {
            Debug.Log("낚싯대 프리팹 또는 손 소켓이 연결되지 않았습니다.");
            return;
        }

        spawnedFisingRod = Instantiate(fishingRodPrefab, fishingRodSocket);

        spawnedFisingRod.transform.localPosition = new Vector3(1.32f, 0f, -4f);
        spawnedFisingRod.transform.localRotation = Quaternion.Euler(rodReadyRotation);
        spawnedFisingRod.transform.localScale = new Vector3(3f, 3f, 3f);

        CreateFishingLineStartPoint();
    }
    void CreateFishingLineStartPoint()
    {
        if (spawnedFisingRod == null || fishingLineStartPoint != null)
        {
            return;
        }

        GameObject linePointObject = new GameObject("FishingLineStartPoint");
        fishingLineStartPoint = linePointObject.transform;
        fishingLineStartPoint.SetParent(spawnedFisingRod.transform, false);
        fishingLineStartPoint.localPosition = fishingLineStartLocalPosition;
        fishingLineStartPoint.localRotation = Quaternion.identity;
        fishingLineStartPoint.localScale = Vector3.one;
    }
    void SetAlpha(float alpha)
    {
        foreach (Material material in materials)
        {
            if (material.HasProperty("_BaseColor"))
            {
                Color color = material.GetColor("_BaseColor");
                color.a = alpha;
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                Color color = material.GetColor("_Color");
                    color.a = alpha;
                    material.SetColor("_Color", color);
            }
        }
    }
    void PrepareTransParentMaterial(Material material)
    {
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
    }
    public void SwitchToFishFocusCamera()
    {
        CinemachineBrain brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
        if (brain != null)
        {
            StartCoroutine(RestoreBlendAfterFocus(brain, brain.DefaultBlend));
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.45f);
        }
        stageCamera.gameObject.SetActive(false);
        AIFishGetCamera.gameObject.SetActive(false);
        FishFocusCamera.gameObject.SetActive(true);
    }

    private IEnumerator RestoreBlendAfterFocus(CinemachineBrain brain, CinemachineBlendDefinition previous)
    {
        yield return new WaitForSeconds(0.5f);
        if (brain != null) brain.DefaultBlend = previous;
    }

    IEnumerator IntroCoroutine()
    {
        isPlaying = true;

        yield return StartCoroutine(FadeInCoroutine());
        yield return StartCoroutine(WalkToTargetPointCoroutine());
        yield return StartCoroutine(JumpToLiePointCoroutine());
        yield return StartCoroutine(SwingFishRodCoroutine());
    }

    IEnumerator FadeInCoroutine()
    {
        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;

            float alpha = Mathf.Clamp01(timer / fadeTime);
            SetAlpha(alpha);

            yield return null;
        }

        SetAlpha(1f);
    }
    IEnumerator WalkToTargetPointCoroutine()
    {
        if (animator != null && !string.IsNullOrEmpty(walkBoolName))
        {
            animator.SetBool(walkBoolName, true);
        }

        while (moveTarget != null && Vector3.Distance(transform.position, moveTarget.position) > 0.1f)
        {
            Vector3 targetPosition = moveTarget.position;
            targetPosition.y = transform.position.y;

            float distance = Vector3.Distance(transform.position, targetPosition);

            if (distance <= 0.1f)
            {
                break;
            }

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            Vector3 direction = targetPosition - transform.position;

            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            yield return null;
        }
        if (moveTarget != null)
        {
            Vector3 finalPosition = moveTarget.position;
            finalPosition.y = transform.position.y;
            transform.position = finalPosition;
        }

        if (animator != null && !string.IsNullOrEmpty(walkBoolName))
        {
            animator.SetBool(walkBoolName, false);
        }
    }
    IEnumerator JumpToLiePointCoroutine()
    {
        if (liePoint == null)
        {
            yield break;
        }

        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }

        Vector3 startPosition = transform.position;
        Vector3 endPosition = liePoint.position;

        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = Quaternion.Euler(0f, liePoint.eulerAngles.y, 0f);

        float timer = 0f;
        bool sitStarted = false;

        while (timer < jumpMoveTime)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / jumpMoveTime);

            Vector3 nextPosition = Vector3.Lerp(startPosition, endPosition, t);
            nextPosition.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;

            transform.position = nextPosition;
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);

            if (!sitStarted && t >= sitTriggerRatio)
            {
                sitStarted = true;

                if (animator != null)
                {
                    animator.SetTrigger("SunbedSit");
                }
            }

            yield return null;
        }

        transform.position = endPosition;
        transform.rotation = endRotation;
    }
    IEnumerator SwingFishingCoroutine()
    {
        if (spawnedFisingRod == null)
        {
            yield break;
        }

        Quaternion readyRotation = Quaternion.Euler(rodReadyRotation);
        Quaternion swingRotation = Quaternion.Euler(rodSwingRotation);

        float timer = 0f;

        while (timer < rodSwingTime)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / rodSwingTime);
            spawnedFisingRod.transform.localRotation = Quaternion.Slerp(readyRotation, swingRotation, t);
            yield return null;
        }
        timer = 0f;

        while (timer < rodSwingTime)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / rodSwingTime);
                
            spawnedFisingRod.transform.localRotation = Quaternion.Slerp(swingRotation, readyRotation, t);
            yield return null;
        }
        spawnedFisingRod.transform.localRotation = readyRotation;
    }
    IEnumerator SwingFishRodCoroutine()
    {
        yield return new WaitForSeconds(3f);

        stageCamera.gameObject.SetActive(false);
        AIFishGetCamera.gameObject.SetActive(true);

        SpawnFishingRod();

        if (animator != null)
        {
            animator.SetTrigger("Swing");
        }
        StartCoroutine(SwingFishingCoroutine());

        yield return new WaitForSeconds(fishLiftDelay);

        if (targetFish != null)
        {
            Transform lineStartPoint = fishingLineStartPoint != null ? fishingLineStartPoint : fishingRodSocket;
            targetFish.SetEnemyIntro(this);
            targetFish.StartAICatch(lineStartPoint);
        }
    }
}
    

