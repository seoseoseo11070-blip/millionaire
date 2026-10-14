using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VictoryPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite tableSprite;

    [Tooltip("0=プレイヤー, 1=NPC1, 2=NPC2, 3=NPC3")]
    [SerializeField] private Image[] seatRankImages = new Image[4];

    [SerializeField] private Sprite[] playerRankSprites = new Sprite[4];
    [SerializeField] private Sprite[] npc1RankSprites = new Sprite[4];
    [SerializeField] private Sprite[] npc2RankSprites = new Sprite[4];
    [SerializeField] private Sprite[] npc3RankSprites = new Sprite[4];

    [Header("タイトルへ")]
    [SerializeField] private float returnSeconds = 5f;
    [Tooltip("Build Settings のシーン名。空なら Index を使う")]
    [SerializeField] private string titleSceneName = "";
    [SerializeField] private int titleSceneIndex = 0;

    void Awake()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void Show(List<int> finishOrder)
    {
        Debug.Log($"[Victory] Show 呼び出し count={finishOrder?.Count}");
        GameObject panel = root != null ? root : gameObject;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();

        Canvas canvas = panel.GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = panel.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = true;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 999;
        }

        if (backgroundImage != null)
        {
            if (tableSprite != null)
                backgroundImage.sprite = tableSprite;
            backgroundImage.enabled = true;
            backgroundImage.color = Color.white;
            backgroundImage.gameObject.SetActive(true);
        }

        int[] rankByPlayer = new int[4];
        if (finishOrder != null)
        {
            for (int r = 0; r < finishOrder.Count && r < 4; r++)
            {
                int p = finishOrder[r];
                if (p >= 0 && p < 4)
                {
                    rankByPlayer[p] = r + 1;
                    Debug.Log($"[Victory] {r + 1}位 = index {p}");
                }
            }
        }

        for (int seat = 0; seat < 4; seat++)
        {
            if (seatRankImages == null || seat >= seatRankImages.Length || seatRankImages[seat] == null)
            {
                Debug.LogWarning($"[Victory] seatRankImages[{seat}] なし");
                continue;
            }

            int rank = rankByPlayer[seat];
            Sprite sp = GetRankSprite(seat, rank);
            if (sp != null)
            {
                seatRankImages[seat].sprite = sp;
                seatRankImages[seat].enabled = true;
                seatRankImages[seat].color = Color.white;
                seatRankImages[seat].gameObject.SetActive(true);
            }
            else
            {
                seatRankImages[seat].gameObject.SetActive(false);
                Debug.LogWarning($"[Victory] 席{seat} の {rank}位スプライトなし");
            }
        }

        StopAllCoroutines();
        StartCoroutine(ReturnToTitleAfterDelay());
    }

    Sprite GetRankSprite(int playerIndex, int rank1to4)
    {
        if (rank1to4 < 1 || rank1to4 > 4) return null;
        int i = rank1to4 - 1;
        Sprite[] arr = playerIndex switch
        {
            0 => playerRankSprites,
            1 => npc1RankSprites,
            2 => npc2RankSprites,
            3 => npc3RankSprites,
            _ => null
        };
        if (arr == null || i >= arr.Length) return null;
        return arr[i];
    }

    IEnumerator ReturnToTitleAfterDelay()
    {
        Debug.Log($"[Victory] {returnSeconds}秒後にタイトルへ");
        yield return new WaitForSecondsRealtime(returnSeconds);
        if (!string.IsNullOrEmpty(titleSceneName))
        {
            try
            {
                SceneManager.LoadScene(titleSceneName);
                yield break;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Victory] 読み込み失敗: {e.Message}");
            }
        }

        Debug.Log($"[Victory] LoadScene index={titleSceneIndex}");
        SceneManager.LoadScene(titleSceneIndex);
    }
}