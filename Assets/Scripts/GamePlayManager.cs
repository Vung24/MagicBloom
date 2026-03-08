using System.Collections;
using UnityEngine;
using System.Linq;

public class GamePlayManager : MonoBehaviour
{
    private const int MaxSegments = 4;
    public static GamePlayManager Instance { get; private set; }
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private GameObject gameplayRoot;
    [SerializeField] private float winDelay = 1.5f;
    private Tube selectedTube;
    private bool hasWon;
    private bool hasLost;
    private bool isBusy;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
    }

    public void HandleTubeClick(Tube clickedTube)
    {
        if (clickedTube == null || hasWon || hasLost || isBusy || clickedTube.IsAnimating)
            return;

        if (selectedTube == null)
        {
            if (clickedTube.GetLiquidCount() > 0)
            {
                selectedTube = clickedTube;
                selectedTube.SetSelected(true);
            }
            return;
        }

        if (selectedTube == clickedTube)
        {
            selectedTube.SetSelected(false);
            selectedTube = null;
            return;
        }

        Tube source = selectedTube;
        source.SetSelected(false);
        selectedTube = null;
        StartCoroutine(Pour(source, clickedTube));
    }
    private void CheckTubeFull(Tube tube)
    {
        if (hasWon) return;
        if (IsSolvedTube(tube) && tube.GetLiquidCount() == MaxSegments)
            AudioManager.Instance?.PlayWaterFull();
    }
    private IEnumerator Pour(Tube source, Tube target)
    {
        if (source == null || target == null || source == target || source.IsAnimating || target.IsAnimating)
        {
            yield break;
        }

        if (source.GetLiquidCount() == 0 || target.GetLiquidCount() >= MaxSegments)
        {
            yield break;
        }

        LiquidSegment sourceTop = source.GetTopSegment();
        LiquidSegment targetTop = target.GetTopSegment();
        if (sourceTop == null)
        {
            yield break;
        }

        bool validTarget = targetTop == null || IsSameLiquid(sourceTop, targetTop);
        if (!validTarget)
        {
            yield break;
        }

        int sameColorCount = GetTopSameLiquidCount(source, sourceTop);
        int availableSlots = MaxSegments - target.GetLiquidCount();
        int moveCount = Mathf.Min(sameColorCount, availableSlots);
        if (moveCount <= 0)
        {
            yield break;
        }

        isBusy = true;
        try
        {
            yield return source.AnimatePourTo(target, moveCount);
        }
        finally
        {
            isBusy = false;
        }
        CheckTubeFull(target);
        CheckGameWin();
        CheckGameLose();
    }

    private int GetTopSameLiquidCount(Tube source, LiquidSegment sourceTop)
    {
        if (source == null || sourceTop == null || source.liquidSegments == null)
        {
            return 0;
        }

        int count = 0;
        var ordered = source.liquidSegments
            .Where(x => x != null)
            .OrderByDescending(x => x.transform.localPosition.y);

        foreach (var segment in ordered)
        {
            if (!IsSameLiquid(segment, sourceTop))
            {
                break;
            }
            count++;
        }

        return count;
    }

    private bool IsSameLiquid(LiquidSegment a, LiquidSegment b)
    {
        if (a == null || b == null)
        {
            return false;
        }

        if (a.ColorKey != '\0' && b.ColorKey != '\0')
        {
            return a.ColorKey == b.ColorKey;
        }

        return Mathf.Abs(a.Color.r - b.Color.r) < 0.02f &&
               Mathf.Abs(a.Color.g - b.Color.g) < 0.02f &&
               Mathf.Abs(a.Color.b - b.Color.b) < 0.02f &&
               Mathf.Abs(a.Color.a - b.Color.a) < 0.02f;
    }

    private bool IsFinal()
    {
        Tube[] allTubes = FindObjectsOfType<Tube>();
        if (allTubes.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < allTubes.Length; i++)
        {
            if (!IsSolvedTube(allTubes[i]))
            {
                return false;
            }
        }
        return true;
    }

    private bool IsWin()
    {
        if (hasWon)
        {
            return false;
        }
        return IsFinal();
    }

    private void CheckGameWin()
    {
        if (!IsWin())
        {
            return;
        }

        hasWon = true;
        StartCoroutine(ShowVictoryAfterDelay());
    }

    private IEnumerator ShowVictoryAfterDelay()
    {
        if (winDelay > 0f)
        {
            yield return new WaitForSeconds(winDelay);
        }
        AudioManager.Instance?.PlayWin();
        HideGameplay();

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }
    }
    private void CheckGameLose()
    {
        if (hasWon || hasLost) return;
        if (HasAnyValidMove()) return;

        hasLost = true;
        StartCoroutine(ShowLoseAfterDelay());
    }
    private bool HasAnyValidMove()
    {
        Tube[] allTubes = FindObjectsOfType<Tube>();

        foreach (Tube source in allTubes)
        {
            if (source.GetLiquidCount() == 0) continue;

            LiquidSegment sourceTop = source.GetTopSegment();
            if (sourceTop == null) continue;

            int sameCount = GetTopSameLiquidCount(source, sourceTop);

            foreach (Tube target in allTubes)
            {
                if (source == target) continue;
                if (target.GetLiquidCount() >= MaxSegments) continue;

                if (target.GetLiquidCount() == 0)
                {
                    bool sourceIsPure = source.GetLiquidCount() == sameCount;
                    if (!sourceIsPure) return true;
                    continue;
                }

                LiquidSegment targetTop = target.GetTopSegment();
                if (targetTop == null) continue;

                if (!IsSameLiquid(sourceTop, targetTop)) continue;

                int availableSlots = MaxSegments - target.GetLiquidCount();
                if (availableSlots <= 0) continue;

                return true;
            }
        }
        return false;
    }
    private IEnumerator ShowLoseAfterDelay()
    {
        yield return new WaitForSeconds(winDelay);

        HideGameplay();

        if (losePanel != null)
            losePanel.SetActive(true);
    }
    private void HideGameplay()
    {
        if (gameplayRoot != null)
        {
            gameplayRoot.SetActive(false);
            return;
        }

        SpawnManager spawnManager = FindObjectOfType<SpawnManager>();
        if (spawnManager != null)
        {
            spawnManager.gameObject.SetActive(false);
        }
    }

    private bool IsSolvedTube(Tube tube)
    {
        int count = tube.GetLiquidCount();
        if (count == 0)
        {
            return true;
        }

        if (count != MaxSegments)
        {
            return false;
        }

        char firstKey = tube.liquidSegments[0].ColorKey;
        return tube.liquidSegments.All(segment => segment != null && segment.ColorKey == firstKey);
    }

}
