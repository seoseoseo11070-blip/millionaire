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
    [SerializeField] private float thinkInterval = 0.35f;

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
        if (thinkCo != null)
        {
            StopCoroutine(thinkCo);
            thinkCo = null;
        }

        ResetAllRaise();

        if (playerIndex <= 0)
        {
            thinkingNpcIndex = -1;
            return;
        }

        thinkingNpcIndex = playerIndex - 1;
        thinkCo = StartCoroutine(ThinkRaiseLoop(thinkingNpcIndex));
    }

    void ResetAllRaise()
    {
        foreach (var kv in visuals)
            foreach (var cv in kv.Value)
                cv.raiseY = 0f;
    }

    IEnumerator ThinkRaiseLoop(int npcIndex)
    {
        while (thinkingNpcIndex == npcIndex)
        {
            if (!visuals.ContainsKey(npcIndex) || visuals[npcIndex].Count == 0)
            {
                yield return new WaitForSeconds(thinkInterval);
                continue;
            }

            var list = visuals[npcIndex];

            foreach (var cv in list)
                cv.raiseY = 0f;

            int n = Mathf.Min(list.Count, Random.Range(1, 3));
            var indices = new List<int>();
            for (int i = 0; i < list.Count; i++)
                indices.Add(i);

            for (int i = 0; i < n; i++)
            {
                int pick = Random.Range(0, indices.Count);
                int idx = indices[pick];
                indices.RemoveAt(pick);
                list[idx].raiseY = thinkRaiseY;
            }

            yield return new WaitForSeconds(thinkInterval);
        }

        ResetAllRaise();
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