using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class BattleInfoPanel : MonoBehaviour
{
    [Header("ルート")]
    [SerializeField] private RectTransform panelRoot;

    [Header("革命パネル")]
    [SerializeField] private GameObject revolutionOnObject;
    [SerializeField] private GameObject revolutionOffObject;

    [Header("1枚縛り用")]
    [SerializeField] private Image shibariSingleIcon;
    [SerializeField] private Sprite singleHeart;
    [SerializeField] private Sprite singleSpade;
    [SerializeField] private Sprite singleDiamond;
    [SerializeField] private Sprite singleClub;

    [Header("2枚縛り")]
    [SerializeField] private Image shibariUpperIcon;
    [SerializeField] private Image shibariLowerIcon;

    [Header("2枚縛り用")]
    [SerializeField] private Sprite heartUpper;
    [SerializeField] private Sprite heartLower;
    [SerializeField] private Sprite spadeUpper;
    [SerializeField] private Sprite spadeLower;
    [SerializeField] private Sprite diamondUpper;
    [SerializeField] private Sprite diamondLower;
    [SerializeField] private Sprite clubUpper;
    [SerializeField] private Sprite clubLower;

    [Header("手札枚数")]
    [SerializeField] private Sprite[] digitSprites = new Sprite[10];

    [Header("数字親")]
    [SerializeField] private RectTransform digitParent;

    [Header("1の位の座標(0=プレイヤー）")]
    [SerializeField]
    private Vector2[] onesPositions = new Vector2[]
    {
        new Vector2(40f, -30f),
        new Vector2(80f, -30f),
        new Vector2(40f, -60f),
        new Vector2(80f, -60f),
    };

    [Header("10の位の座標")]
    [SerializeField]
    private Vector2[] tensPositions = new Vector2[]
    {
        new Vector2(20f, -30f),
        new Vector2(60f, -30f),
        new Vector2(20f, -60f),
        new Vector2(60f, -60f),
    };

    [Header("数字サイズ")]
    [SerializeField] private Vector2 digitDisplaySize = new Vector2(16.832f, 16.832f);

    [Header("パネル開閉位置")]
    [SerializeField] private float hiddenY = 120f;
    [SerializeField] private float shownY = -20f;
    [SerializeField] private float slideSpeed = 12f;

    private bool isOpen;
    private float targetY;

    private Image[] onesImages;
    private Image[] tensImages;

    void Awake()
    {
        isOpen = false;
        targetY = hiddenY;

        if (panelRoot != null)
        {
            Vector2 p = panelRoot.anchoredPosition;
            p.y = hiddenY;
            panelRoot.anchoredPosition = p;
        }

        SetRevolution(false);
        ClearShibari();
        EnsureDigitImages();
    }

    void Update()
    {
        if (panelRoot != null)
        {
            Vector2 p = panelRoot.anchoredPosition;
            p.y = Mathf.Lerp(p.y, targetY, Time.deltaTime * slideSpeed);
            panelRoot.anchoredPosition = p;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            ToggleOpen();
    }

    public void ToggleOpen()
    {
        isOpen = !isOpen;
        targetY = isOpen ? shownY : hiddenY;
    }

    public void SetRevolution(bool isRevolution)
    {
        if (revolutionOnObject != null)
            revolutionOnObject.SetActive(isRevolution);
        if (revolutionOffObject != null)
            revolutionOffObject.SetActive(!isRevolution);
    }

    void ClearShibari()
    {
        if (shibariSingleIcon != null) shibariSingleIcon.gameObject.SetActive(false);
        if (shibariUpperIcon != null) shibariUpperIcon.gameObject.SetActive(false);
        if (shibariLowerIcon != null) shibariLowerIcon.gameObject.SetActive(false);
    }

    public void SetShibari(bool locked, List<Card> fieldCards)
    {
        ClearShibari();
        if (!locked || fieldCards == null || fieldCards.Count == 0) return;

        if (fieldCards.Count == 2)
        {
            Sprite upper = GetUpperSprite(fieldCards[0].suit);
            Sprite lower = GetLowerSprite(fieldCards[1].suit);

            if (shibariUpperIcon != null && upper != null)
            {
                shibariUpperIcon.sprite = upper;
                shibariUpperIcon.gameObject.SetActive(true);
            }
            if (shibariLowerIcon != null && lower != null)
            {
                shibariLowerIcon.sprite = lower;
                shibariLowerIcon.gameObject.SetActive(true);
            }
            return;
        }

        Card.SuitType suit = fieldCards[0].suit;
        if (suit == Card.SuitType.Joker) return;

        Sprite single = GetSingleSprite(suit);
        if (shibariSingleIcon != null && single != null)
        {
            shibariSingleIcon.sprite = single;
            shibariSingleIcon.gameObject.SetActive(true);
        }
    }

    Sprite GetSingleSprite(Card.SuitType suit)
    {
        return suit switch
        {
            Card.SuitType.Heart => singleHeart,
            Card.SuitType.Spade => singleSpade,
            Card.SuitType.Diamond => singleDiamond,
            Card.SuitType.Club => singleClub,
            _ => null
        };
    }

    Sprite GetUpperSprite(Card.SuitType suit)
    {
        return suit switch
        {
            Card.SuitType.Heart => heartUpper,
            Card.SuitType.Spade => spadeUpper,
            Card.SuitType.Diamond => diamondUpper,
            Card.SuitType.Club => clubUpper,
            _ => null
        };
    }

    Sprite GetLowerSprite(Card.SuitType suit)
    {
        return suit switch
        {
            Card.SuitType.Heart => heartLower,
            Card.SuitType.Spade => spadeLower,
            Card.SuitType.Diamond => diamondLower,
            Card.SuitType.Club => clubLower,
            _ => null
        };
    }

    void EnsureDigitImages()
    {
        if (digitSprites == null || digitSprites.Length < 10)
        {
            Debug.LogWarning("digitSprites が不足している");
            return;
        }

        RectTransform parent = digitParent != null ? digitParent : panelRoot;
        if (parent == null)
        {
            Debug.LogWarning("digitParent / panelRoot がありません");
            return;
        }

        const int seatCount = 4;

        bool needCreate = onesImages == null
            || onesImages.Length != seatCount
            || tensImages == null
            || tensImages.Length != seatCount;

        if (!needCreate)
        {
            for (int i = 0; i < seatCount; i++)
            {
                if (onesImages[i] == null || tensImages[i] == null)
                {
                    needCreate = true;
                    break;
                }
            }
        }

        if (!needCreate) return;

        if (onesImages != null)
        {
            for (int i = 0; i < onesImages.Length; i++)
            {
                if (onesImages[i] != null) Destroy(onesImages[i].gameObject);
                if (tensImages != null && i < tensImages.Length && tensImages[i] != null)
                    Destroy(tensImages[i].gameObject);
            }
        }

        onesImages = new Image[seatCount];
        tensImages = new Image[seatCount];

        for (int i = 0; i < seatCount; i++)
        {
            Vector2 onesPos = (onesPositions != null && i < onesPositions.Length)
                ? onesPositions[i]
                : new Vector2(40f + i * 40f, -30f);

            Vector2 tensPos = (tensPositions != null && i < tensPositions.Length)
                ? tensPositions[i]
                : onesPos + new Vector2(-20f, 0f);

            tensImages[i] = CreateDigitImage(parent, $"DigitTens_{i}", tensPos);
            onesImages[i] = CreateDigitImage(parent, $"DigitOnes_{i}", onesPos);
        }

        BringDigitsToFront();
    }

    Image CreateDigitImage(RectTransform parent, string name, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = digitDisplaySize;
        rt.anchoredPosition = anchoredPos;
        rt.localScale = Vector3.one;

        Image img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.color = Color.white;
        img.preserveAspect = true;

        if (digitSprites != null && digitSprites.Length > 0 && digitSprites[0] != null)
            img.sprite = digitSprites[0];

        return img;
    }

    void BringDigitsToFront()
    {
        if (tensImages != null)
        {
            foreach (var img in tensImages)
                if (img != null) img.transform.SetAsLastSibling();
        }
        if (onesImages != null)
        {
            foreach (var img in onesImages)
                if (img != null) img.transform.SetAsLastSibling();
        }
    }
    public void SetHandCounts(int[] counts)
    {
        EnsureDigitImages();
        if (onesImages == null || tensImages == null) return;
        if (digitSprites == null || digitSprites.Length < 10) return;

        for (int i = 0; i < onesImages.Length; i++)
        {
            int count = 0;
            if (counts != null && i < counts.Length)
                count = Mathf.Max(0, counts[i]);

            int tens = count / 10;
            int ones = count % 10;

            if (tensImages[i] != null)
            {
                tensImages[i].sprite = digitSprites[Mathf.Clamp(tens, 0, 9)];
                tensImages[i].preserveAspect = true;
                tensImages[i].color = Color.white;
                tensImages[i].rectTransform.sizeDelta = digitDisplaySize;
                if (tensPositions != null && i < tensPositions.Length)
                    tensImages[i].rectTransform.anchoredPosition = tensPositions[i];
                tensImages[i].gameObject.SetActive(true);
            }

            if (onesImages[i] != null)
            {
                onesImages[i].sprite = digitSprites[Mathf.Clamp(ones, 0, 9)];
                onesImages[i].preserveAspect = true;
                onesImages[i].color = Color.white;
                onesImages[i].rectTransform.sizeDelta = digitDisplaySize;
                if (onesPositions != null && i < onesPositions.Length)
                    onesImages[i].rectTransform.anchoredPosition = onesPositions[i];
                onesImages[i].gameObject.SetActive(true);
            }
        }

        BringDigitsToFront();
    }

    public void Refresh(bool isRevolution, bool isLocked, List<Card> fieldCards)
    {
        SetRevolution(isRevolution);
        SetShibari(isLocked, fieldCards);
    }
}