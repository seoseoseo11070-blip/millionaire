using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VictoryPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Image tableBackground;
    [SerializeField] private Image[] seatRankImages;

    [Header("1〜4位スプライト")]
    [SerializeField] private Sprite[] playerRankSprites;
    [SerializeField] private Sprite[] npc1RankSprites;
    [SerializeField] private Sprite[] npc2RankSprites;
    [SerializeField] private Sprite[] npc3RankSprites;

    [Header("発表タイミング")]
    [SerializeField] private float delayBetweenRanks = 1.0f;
    [SerializeField] private float delayAfterFirst = 5.0f;

    [SerializeField] private string titleSceneName = "TitleScene";

    void Awake()
    {
        HideAllRankImages();
        if (root != null)
            root.SetActive(false);
    }

    void HideAllRankImages()
    {
        if (seatRankImages == null) return;
        for (int i = 0; i < seatRankImages.Length; i++)
        {
            if (seatRankImages[i] == null) continue;
            seatRankImages[i].sprite = null;
            seatRankImages[i].enabled = false;
            seatRankImages[i].gameObject.SetActive(false);
        }
    }

    public void Show(List<int> finishOrder)
    {

        if (SfxPlayer.Instance != null)
            SfxPlayer.Instance.PlayVictoryJingle();

        if (root != null)
            root.SetActive(true);

        HideAllRankImages(); // 全部消す

        StopAllCoroutines();
        StartCoroutine(RevealRanksThenTitle(finishOrder));
    }


    private IEnumerator RevealRanksThenTitle(List<int> finishOrder)
    {
        if (finishOrder == null || finishOrder.Count == 0)
        {
            yield return new WaitForSecondsRealtime(delayAfterFirst);
            LoadTitle();
            yield break;
        }
        for (int rank = finishOrder.Count; rank >= 1; rank--)
        {
            int orderIndex = rank - 1;
            if (orderIndex < 0 || orderIndex >= finishOrder.Count) continue;

            int playerIndex = finishOrder[orderIndex];
            Sprite sp = GetRankSprite(playerIndex, rank);

            int seat = Mathf.Clamp(playerIndex, 0, seatRankImages != null ? seatRankImages.Length - 1 : 0);
            if (seatRankImages != null && seat < seatRankImages.Length && seatRankImages[seat] != null)
            {
                seatRankImages[seat].gameObject.SetActive(true);
                seatRankImages[seat].sprite = sp;
                seatRankImages[seat].enabled = sp != null;
                seatRankImages[seat].transform.SetAsLastSibling();
            }

            if (SfxPlayer.Instance != null)
                SfxPlayer.Instance.PlayVictoryRank();

            Debug.Log($"順位発表: {rank}位 → {PlayerLabel(playerIndex)}");
            if (rank > 1)
                yield return new WaitForSecondsRealtime(delayBetweenRanks);
        }

        // 5秒
        yield return new WaitForSecondsRealtime(delayAfterFirst);
        LoadTitle();
    }

    private Sprite GetRankSprite(int playerIndex, int rank)
    {
        int i = Mathf.Clamp(rank - 1, 0, 3);
        switch (playerIndex)
        {
            case 0: return GetSafe(playerRankSprites, i);
            case 1: return GetSafe(npc1RankSprites, i);
            case 2: return GetSafe(npc2RankSprites, i);
            case 3: return GetSafe(npc3RankSprites, i);
            default: return null;
        }
    }

    private Sprite GetSafe(Sprite[] arr, int i)
    {
        if (arr == null || i < 0 || i >= arr.Length) return null;
        return arr[i];
    }

    private string PlayerLabel(int playerIndex)
    {
        return playerIndex == 0 ? "あなた" : $"NPC{playerIndex}";
    }

    private void LoadTitle()
    {
        if (!string.IsNullOrEmpty(titleSceneName)
            && Application.CanStreamedLevelBeLoaded(titleSceneName))
        {
            SceneManager.LoadScene(titleSceneName);
            return;
        }

        SceneManager.LoadScene(0);
    }
}