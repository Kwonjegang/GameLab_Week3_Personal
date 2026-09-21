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
    private void Start()
    {
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

    IEnumerator IntroCoroutine()
    {
        isPlaying = true;

        yield return StartCoroutine(FadeInCoroutine());
        yield return StartCoroutine(WalkToTargetPointCoroutine());
        yield return StartCoroutine(JumpToLiePointCoroutine());
        yield return StartCoroutine(SwingFishRobCoroutine());
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
    IEnumerator SwingFishRobCoroutine()
    {
        if (animator != null)
        {
            yield return new WaitForSeconds(3f);
            animator.SetTrigger("Swing");
        }
        yield return null;
    }
}
