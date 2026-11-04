using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NpcHandView : MonoBehaviour
{
    [System.Serializable]
    public class NpcSlot
    {
        [Tooltip("0=NPC1, 1=NPC2, 2=NPC3")]
        public int npcIndex;

        public RectTransform container;
        public Sprite cardBackSprite;

        [Header("1枚目の位置")]
        public Vector2 startPos = Vector2.zero;

        [Header("2枚目以降")]
        public Vector2 spacing = new Vector2(12f, -12f);

        [Header("カード1枚のサイズ")]
        public Vector2 cardSize = new Vector2(40f, 56f);

        [Tooltip("0以外")]
        public int maxVisible = 12;
    }

    [SerializeField] private NpcSlot[] slots = new NpcSlot[3];

    [Header("思考演出")]
    [SerializeField] private float thinkRaiseY = 18f;
    [SerializeField] private float raiseSpeed = 12f;

    [Tooltip("NPCが考える秒数")]
    [SerializeField] private float thinkDuration = 3.0f;

    [Tooltip("1周")]
    [SerializeField] private bool roundTripAsOneLap = true;
    private readonly Dictionary<int, int> actualCounts = new Dictionary<int, int>();
    private int thinkingNpcIndex = -1;

    private class CardVisual
    {
        public RectTransform rt;
        public Vector2 basePos;
        public float raiseY;
    }

    private readonly Dictionary<int, List<CardVisual>> visuals = new Dictionary<int, List<CardVisual>>();
    private Coroutine thinkCo;

    private readonly HashSet<int> thinkSelectedVisual = new HashSet<int>();
    private int scanIndex = -1;

    void Update()
    {
        foreach (var kv in visuals)
        {
            foreach (var cv in kv.Value)
            {
                if (cv.rt == null) continue;
                Vector2 target = cv.basePos + new Vector2(0f, cv.raiseY);
                cv.rt.anchoredPosition = Vector2.Lerp(
                    cv.rt.anchoredPosition,
                    target,
                    Time.deltaTime * raiseSpeed);
            }
        }
    }

    public int ToVisualCount(int npcIndex, int actualCount)
    {
        if (actualCount <= 0) return 0;

        if (npcIndex == 1)
            return Mathf.Min(11, actualCount); // NPC2
        else
            return Mathf.Min(6, actualCount);  // NPC1, NPC3
    }

    public void SetActualCount(int npcIndex, int actualCount)
    {
        actualCounts[npcIndex] = Mathf.Max(0, actualCount);
        Rebuild(npcIndex);
    }



    public void SetCountsFromPlayerHands(int[] allPlayerCounts)
    {
        if (allPlayerCounts == null) return;

        for (int playerIndex = 1; playerIndex < allPlayerCounts.Length; playerIndex++)
        {
            int npcIndex = playerIndex - 1;
            SetActualCount(npcIndex, allPlayerCounts[playerIndex]);
        }

        BringAllToFront();
    }

    public void SetThinkingPlayer(int playerIndex)
    {
        SetThinkingPlayer(playerIndex, 1);
    }
    public bool IsThinking { get; private set; }


    void ResetAllRaise()
    {
        foreach (var kv in visuals)
            foreach (var cv in kv.Value)
                cv.raiseY = 0f;
    }

    public void SetThinkingPlayer(int playerIndex, int fieldCardCount)
    {
        if (thinkCo != null)
        {
            StopCoroutine(thinkCo);
            thinkCo = null;
        }

        ResetAllRaise();
        thinkSelectedVisual.Clear();
        scanIndex = -1;

        if (playerIndex <= 0)
        {
            thinkingNpcIndex = -1;
            IsThinking = false;
            return;
        }

        thinkingNpcIndex = playerIndex - 1;
        IsThinking = true;

        int pickCount = Mathf.Max(1, fieldCardCount);
        thinkCo = StartCoroutine(ThinkScanLoop(thinkingNpcIndex, pickCount));
    }

    IEnumerator ThinkScanLoop(int npcIndex, int pickCount)
    {

        float waited = 0f;
        while ((!visuals.ContainsKey(npcIndex) || visuals[npcIndex].Count == 0) && waited < 2f)
        {
            if (thinkingNpcIndex != npcIndex)
            {
                IsThinking = false;
                thinkCo = null;
                yield break;
            }
            waited += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (!visuals.ContainsKey(npcIndex) || visuals[npcIndex].Count == 0)
        {
            IsThinking = false;
            thinkCo = null;
            yield break;
        }

        var list = visuals[npcIndex];
        int n = list.Count;

        if (n <= 1)
        {
            ResetAllRaise();
            thinkSelectedVisual.Clear();
            scanIndex = -1;
            IsThinking = false;
            thinkCo = null;
            yield break;
        }

        thinkSelectedVisual.Clear();
        bool usePlaySelect = (n > 3);

        if (usePlaySelect)
        {
            int take = Mathf.Min(Mathf.Max(1, pickCount), n);
            var pool = new List<int>();
            for (int i = 0; i < n; i++)
                pool.Add(i);

            for (int i = 0; i < take; i++)
            {
                int p = Random.Range(0, pool.Count);
                thinkSelectedVisual.Add(pool[p]);
                pool.RemoveAt(p);
            }
        }

        var visitedPlay = new HashSet<int>();

        bool fromLeft = Random.value < 0.5f;
        int start = fromLeft ? 0 : n - 1;
        int end = fromLeft ? n - 1 : 0;
        int step = fromLeft ? 1 : -1;
        scanIndex = start;

        int stepsPerLap = Mathf.Max(1, (n - 1) * 2);
        float interval = Mathf.Clamp(thinkDuration / stepsPerLap, 0.12f, 0.8f);

        int safety = 0;
        int maxScanSteps = stepsPerLap * 2 + n;

        while (thinkingNpcIndex == npcIndex && safety < maxScanSteps)
        {
            safety++;
            list = visuals[npcIndex];
            if (list == null || list.Count == 0) break;

            n = list.Count;
            if (n <= 1) break;

            scanIndex = Mathf.Clamp(scanIndex, 0, n - 1);
            if (usePlaySelect && thinkSelectedVisual.Contains(scanIndex))
                visitedPlay.Add(scanIndex);

            ApplyScanRaise(list, scanIndex, visitedPlay);

            yield return new WaitForSeconds(interval);

            if (thinkingNpcIndex != npcIndex)
            {
                ResetAllRaise();
                thinkSelectedVisual.Clear();
                scanIndex = -1;
                IsThinking = false;
                thinkCo = null;
                yield break;
            }

            if (usePlaySelect)
            {
                if (visitedPlay.Count >= thinkSelectedVisual.Count && thinkSelectedVisual.Count > 0)
                    break;
            }
            else
            {
                if (safety >= stepsPerLap)
                    break;
            }

            if (scanIndex == end)
            {
                step = -step;
                int tmp = start;
                start = end;
                end = tmp;
            }
            scanIndex += step;
        }

        if (thinkingNpcIndex != npcIndex)
        {
            ResetAllRaise();
            thinkSelectedVisual.Clear();
            scanIndex = -1;
            IsThinking = false;
            thinkCo = null;
            yield break;
        }

        // 演出終了
        list = visuals.ContainsKey(npcIndex) ? visuals[npcIndex] : null;
        if (list != null)
        {
            for (int i = 0; i < list.Count; i++)
            {
                bool keepPlay = thinkSelectedVisual.Contains(i) && visitedPlay.Contains(i);
                list[i].raiseY = keepPlay ? thinkRaiseY : 0f;
            }
        }

        scanIndex = -1;
        IsThinking = false;
        thinkCo = null;
    }

    void ApplyScanRaise(List<CardVisual> list, int currentScan, HashSet<int> visitedPlay)
    {
        for (int i = 0; i < list.Count; i++)
        {
            bool isScan = (i == currentScan);
            bool keepPlay = thinkSelectedVisual.Contains(i) && visitedPlay.Contains(i);

            if (isScan || keepPlay)
                list[i].raiseY = thinkRaiseY;
            else
                list[i].raiseY = 0f;
        }
    }


    void Rebuild(int npcIndex)
    {
        NpcSlot slot = FindSlot(npcIndex);
        if (slot == null)
        {
            Debug.LogWarning($"NpcHandView: slotなし index={npcIndex}");
            return;
        }
        if (slot.container == null)
        {
            Debug.LogWarning($"NpcHandView: Container未設定 index={npcIndex}");
            return;
        }
        if (slot.cardBackSprite == null)
        {
            Debug.LogWarning($"NpcHandView: Sprite未設定 index={npcIndex}");
            return;
        }

        if (!slot.container.gameObject.activeSelf)
            slot.container.gameObject.SetActive(true);

        slot.container.SetAsLastSibling();

        int actual = actualCounts.ContainsKey(npcIndex) ? actualCounts[npcIndex] : 0;
        int visualCount = ToVisualCount(npcIndex, actual);
        visualCount = Mathf.Min(visualCount, Mathf.Max(0, slot.maxVisible));

        ClearChildren(slot.container);

        var list = new List<CardVisual>();
        visuals[npcIndex] = list;

        for (int i = 0; i < visualCount; i++)
        {
            GameObject go = new GameObject($"Card_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(slot.container, false);
            go.layer = slot.container.gameObject.layer;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = slot.cardSize;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;

            Vector2 pos = slot.startPos + slot.spacing * i;
            rt.anchoredPosition = pos;

            Image img = go.GetComponent<Image>();
            img.sprite = slot.cardBackSprite;
            img.color = Color.white;
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.enabled = true;
            img.maskable = false;

            go.transform.SetAsLastSibling();

            list.Add(new CardVisual { rt = rt, basePos = pos, raiseY = 0f });
        }
    }

    void ClearChildren(Transform parent)
    {
        var children = new List<GameObject>();
        for (int i = 0; i < parent.childCount; i++)
            children.Add(parent.GetChild(i).gameObject);

        foreach (var go in children)
        {
            go.transform.SetParent(null);
            Destroy(go);
        }
    }

    void BringAllToFront()
    {
        transform.SetAsLastSibling();
        if (slots == null) return;
        foreach (var slot in slots)
        {
            if (slot == null || slot.container == null) continue;
            slot.container.SetAsLastSibling();
        }
    }

    NpcSlot FindSlot(int npcIndex)
    {
        if (slots == null) return null;
        foreach (var s in slots)
            if (s != null && s.npcIndex == npcIndex)
                return s;
        return null;
    }
}