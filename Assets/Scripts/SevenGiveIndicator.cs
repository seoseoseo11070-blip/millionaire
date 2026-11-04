using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class SevenGiveIndicator : MonoBehaviour
{
    [Header("点滅用 Image")]
    [SerializeField] private Image markerImage;

    [Header("送った素材")]
    [SerializeField] private Sprite markerSprite;

    [Header("NPC1の位置")]
    [SerializeField] private RectTransform npc1Anchor;

    [Header("点滅")]
    [SerializeField] private float blinkInterval = 0.35f;
    [SerializeField] private float minAlpha = 0.25f;
    [SerializeField] private float maxAlpha = 1f;

    private Coroutine blinkCo;

    void Awake()
    {
        Hide();
    }

    public void Show()
    {
        if (markerImage == null)
        {
            Debug.LogWarning("SevenGiveIndicator: markerImage 未設定");
            return;
        }

        if (markerSprite != null)
            markerImage.sprite = markerSprite;

        if (npc1Anchor != null)
            markerImage.rectTransform.position = npc1Anchor.position;
        else
            Debug.LogWarning("SevenGiveIndicator: npc1Anchor 未設定");

        markerImage.gameObject.SetActive(true);
        markerImage.enabled = true;
        markerImage.color = Color.white;
        markerImage.transform.SetAsLastSibling();

        if (blinkCo != null)
            StopCoroutine(blinkCo);
        blinkCo = StartCoroutine(BlinkLoop());
    }

    public void Hide()
    {
        if (blinkCo != null)
        {
            StopCoroutine(blinkCo);
            blinkCo = null;
        }

        if (markerImage != null)
        {
            Color c = markerImage.color;
            c.a = 1f;
            markerImage.color = c;
            markerImage.gameObject.SetActive(false);
        }
    }

    IEnumerator BlinkLoop()
    {
        bool on = true;
        while (true)
        {
            if (markerImage == null) yield break;

            Color c = markerImage.color;
            c.a = on ? maxAlpha : minAlpha;
            markerImage.color = c;
            on = !on;

            yield return new WaitForSeconds(blinkInterval);
        }
    }
}