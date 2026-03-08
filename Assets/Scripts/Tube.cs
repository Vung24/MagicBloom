using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
public class Tube : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private float selectLiftY = 60f;
    [SerializeField] private float selectAnimDuration = 0.12f;
    [Header("Pour Animation")]
    [SerializeField] private float pourLiftY = 0f;
    [SerializeField] private float pourSideOffset = 0f;
    [SerializeField] private float pourMoveDuration = 0.85f;
    [SerializeField] private float pourRotateDuration = 0.4f;
    [SerializeField] private float pourSegmentDuration = 0.5f;
    [SerializeField] private float pourTiltAngle = 88f;
    [SerializeField] private float pourMouthGapY = 130f;
    [Header("Pour Stream")]
    [SerializeField] private bool enablePourStream = true;
    [SerializeField] private float pourStreamWidth = 18f;
    [SerializeField] private Vector3 mouthLocalOffset = new Vector3(0f, 200f, 0f);

    public List<LiquidSegment> liquidSegments;
    public Transform container;
    private Coroutine moveCoroutine;
    private Vector3 baseLocalPosition;
    private bool isSelected;
    public bool IsAnimating { get; private set; }
    private RectTransform streamRect;
    private Image streamImage;

    private void Start()
    {
        baseLocalPosition = transform.localPosition;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsAnimating)
        {
            return;
        }
        EnsureList();
        GamePlayManager.Instance?.HandleTubeClick(this);
    }

    public void SetSelected(bool selected)
    {
        if (IsAnimating)
        {
            return;
        }

        if (selected == isSelected)
        {
            return;
        }

        isSelected = selected;
        if (selected)
        {
            baseLocalPosition = transform.localPosition;
            AudioManager.Instance?.PlayClick();
        }

        Vector3 target = selected
            ? baseLocalPosition + new Vector3(0f, selectLiftY, 0f)
            : baseLocalPosition;

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        moveCoroutine = StartCoroutine(AnimateLocalPos(target));
    }

    public int GetLiquidCount()
    {
        EnsureList();
        liquidSegments.RemoveAll(x => x == null);
        return liquidSegments.Count;
    }

    public LiquidSegment GetTopSegment()
    {
        if (GetLiquidCount() == 0)
        {
            return null;
        }
        return liquidSegments.OrderBy(x => x.transform.localPosition.y).Last();
    }

    public int GetTopSameColorCount(char colorKey)
    {
        int count = 0;
        var ordered = liquidSegments
            .Where(x => x != null)
            .OrderByDescending(x => x.transform.localPosition.y);

        foreach (var segment in ordered)
        {
            if (segment.ColorKey != colorKey)
            {
                break;
            }
            count++;
        }
        return count;
    }

    public LiquidSegment PopTopSegment()
    {
        LiquidSegment top = GetTopSegment();
        if (top == null)
        {
            return null;
        }
        liquidSegments.Remove(top);
        return top;
    }

    public void PushSegment(LiquidSegment segment)
    {
        EnsureList();
        segment.transform.SetParent(container, false);
        segment.transform.localRotation = Quaternion.identity;
        liquidSegments.Add(segment);
    }

    public void RebuildPositions()
    {
        EnsureList();
        liquidSegments.RemoveAll(x => x == null);
        liquidSegments = liquidSegments.OrderBy(x => x.transform.localPosition.y).ToList();
        for (int i = 0; i < liquidSegments.Count; i++)
        {
            Transform segmentTransform = liquidSegments[i].transform;
            segmentTransform.localPosition = GameConstant.firstLiquidPos + GameConstant.liquidSegmentSize * i;
            segmentTransform.localRotation = Quaternion.identity;
            segmentTransform.SetSiblingIndex(liquidSegments.Count - 1 - i);
        }
    }

    private void EnsureList()
    {
        if (liquidSegments == null)
        {
            liquidSegments = new List<LiquidSegment>();
        }
    }

    private IEnumerator AnimateLocalPos(Vector3 targetLocalPos)
    {
        Vector3 start = transform.localPosition;
        float duration = Mathf.Max(0.01f, selectAnimDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            transform.localPosition = Vector3.LerpUnclamped(start, targetLocalPos, t);
            yield return null;
        }

        transform.localPosition = targetLocalPos;
        moveCoroutine = null;
    }

    public IEnumerator AnimatePourTo(Tube target, int moveCount)
    {
        if (target == null || moveCount <= 0 || IsAnimating)
        {
            yield break;
        }

        IsAnimating = true;
        int originalSiblingIndex = transform.GetSiblingIndex();
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        try
        {
            HideStream();
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }

            isSelected = false;
            transform.localPosition = baseLocalPosition;
            transform.SetAsLastSibling();

            float direction = Mathf.Sign(target.transform.position.x - transform.position.x);
            if (Mathf.Approximately(direction, 0f))
            {
                direction = 1f;
            }

            float tiltZ = direction > 0f ? -pourTiltAngle : pourTiltAngle;
            Vector3 targetMouth = target.GetMouthWorldPosition();
            Quaternion pourRotation = Quaternion.Euler(0f, 0f, tiltZ);
            Vector3 desiredSourceMouth = new Vector3(targetMouth.x, targetMouth.y + pourMouthGapY, targetMouth.z);
            Vector3 pivotPosition = desiredSourceMouth - (pourRotation * mouthLocalOffset);
            Vector3 sidePosition = pivotPosition + new Vector3(-direction * pourSideOffset, pourLiftY, 0f);

            yield return MoveTransform(transform, startPosition, sidePosition, pourMoveDuration);
            yield return RotateTransform(transform, startRotation, pourRotation, pourRotateDuration);

            bool hasPouredAny = false;
            for (int i = 0; i < moveCount; i++)
            {
                LiquidSegment sourceTop = GetTopSegment();
                if (sourceTop == null)
                {
                    break;
                }
                hasPouredAny = true;

                int targetIndex = target.GetLiquidCount();
                LiquidSegment incoming = Instantiate(sourceTop, target.container);
                incoming.transform.localRotation = Quaternion.identity;
                if (sourceTop.Data != null)
                {
                    incoming.SetColor(sourceTop.Data);
                }

                Vector3 sourceStartLocalPos = sourceTop.transform.localPosition;
                Vector3 sourceStartLocalScale = sourceTop.transform.localScale;
                Vector3 targetFinalLocalPos = GameConstant.firstLiquidPos + GameConstant.liquidSegmentSize * targetIndex;
                Vector3 incomingBaseScale = incoming.transform.localScale;
                incoming.transform.localPosition = targetFinalLocalPos - new Vector3(0f, GameConstant.liquidSegmentSize.y * 0.5f, 0f);
                incoming.transform.localScale = new Vector3(incomingBaseScale.x, 0f, incomingBaseScale.z);

                ShowStream(sourceTop.Color);
                float elapsed = 0f;
                float safeDuration = Mathf.Max(0.01f, pourSegmentDuration);
                while (elapsed < safeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = EaseOutCubic(Mathf.Clamp01(elapsed / safeDuration));
                    float sourceScaleRatio = 1f - t;
                    float targetScaleRatio = t;
                    float halfHeight = GameConstant.liquidSegmentSize.y * 0.5f;

                    sourceTop.transform.localScale = new Vector3(
                        sourceStartLocalScale.x,
                        sourceStartLocalScale.y * sourceScaleRatio,
                        sourceStartLocalScale.z
                    );
                    sourceTop.transform.localPosition = new Vector3(
                        sourceStartLocalPos.x,
                        sourceStartLocalPos.y - halfHeight * (1f - sourceScaleRatio),
                        sourceStartLocalPos.z
                    );

                    incoming.transform.localScale = new Vector3(
                        incomingBaseScale.x,
                        incomingBaseScale.y * targetScaleRatio,
                        incomingBaseScale.z
                    );
                    incoming.transform.localPosition = new Vector3(
                        targetFinalLocalPos.x,
                        targetFinalLocalPos.y - halfHeight * (1f - targetScaleRatio),
                        targetFinalLocalPos.z
                    );

                    Vector3 sourceMouth = GetMouthWorldPosition();
                    Vector3 targetSurface = GetSegmentTopWorld(incoming);
                    Vector3 streamStart = new Vector3(sourceMouth.x, sourceMouth.y, sourceMouth.z);
                    Vector3 streamEnd = new Vector3(sourceMouth.x, targetSurface.y, sourceMouth.z);
                    UpdateStream(streamStart, streamEnd);
                    yield return null;
                }

                liquidSegments.Remove(sourceTop);
                Destroy(sourceTop.gameObject);
                incoming.transform.localScale = incomingBaseScale;
                incoming.transform.localPosition = targetFinalLocalPos;
                target.PushSegment(incoming);
            }
            HideStream();
            if (hasPouredAny)
            {
                RebuildPositions();
                target.RebuildPositions();
            }

            yield return RotateTransform(transform, transform.rotation, startRotation, pourRotateDuration);
            yield return MoveTransform(transform, transform.position, startPosition, pourMoveDuration);
        }
        finally
        {
            transform.SetSiblingIndex(originalSiblingIndex);
            transform.rotation = startRotation;
            transform.localPosition = baseLocalPosition;
            HideStream();
            IsAnimating = false;
        }
    }

    public Vector3 GetMouthWorldPosition()
    {
        return transform.TransformPoint(mouthLocalOffset);
    }

    private IEnumerator MoveTransform(Transform target, Vector3 from, Vector3 to, float duration)
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / safeDuration));
            target.position = Vector3.LerpUnclamped(from, to, t);
            yield return null;
        }
        target.position = to;
    }

    private IEnumerator RotateTransform(Transform target, Quaternion from, Quaternion to, float duration)
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / safeDuration));
            target.rotation = Quaternion.SlerpUnclamped(from, to, t);
            yield return null;
        }
        target.rotation = to;
    }

    private void EnsureStreamVisual()
    {
        if (!enablePourStream || streamRect != null)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        GameObject streamObj = new GameObject("PourStream", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        streamRect = streamObj.GetComponent<RectTransform>();
        streamImage = streamObj.GetComponent<Image>();
        streamObj.transform.SetParent(canvas.transform, true);
        streamRect.pivot = new Vector2(0.5f, 0f);
        streamRect.anchorMin = new Vector2(0.5f, 0.5f);
        streamRect.anchorMax = new Vector2(0.5f, 0.5f);
        streamImage.raycastTarget = false;
        streamObj.SetActive(false);
    }

    private void ShowStream(Color color)
    {
        if (!enablePourStream)
        {
            return;
        }

        EnsureStreamVisual();
        if (streamRect == null || streamImage == null)
        {
            return;
        }

        Color tint = color;
        tint.a = 0.9f;
        streamImage.color = tint;
        streamRect.gameObject.SetActive(true);
    }

    private void HideStream()
    {
        if (streamRect != null)
        {
            streamRect.gameObject.SetActive(false);
        }
    }

    private void UpdateStream(Vector3 worldStart, Vector3 worldEnd)
    {
        if (streamRect == null)
        {
            return;
        }

        Vector3 delta = worldEnd - worldStart;
        float length = delta.magnitude;
        if (length <= 0.001f)
        {
            streamRect.sizeDelta = new Vector2(pourStreamWidth, 0f);
            streamRect.position = worldStart;
            return;
        }

        streamRect.position = worldStart;
        streamRect.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
        streamRect.sizeDelta = new Vector2(pourStreamWidth, length);
    }

    private Vector3 GetSegmentTopWorld(LiquidSegment segment)
    {
        if (segment == null)
        {
            return transform.position;
        }

        float halfHeight = GameConstant.liquidSegmentSize.y * 0.5f;
        return segment.transform.TransformPoint(new Vector3(0f, halfHeight, 0f));
    }

    private static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
