using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class BattleInfoPanel : MonoBehaviour
{
    [Header("ルート")]
    [SerializeField] private RectTransform panelRoot;

    [Header("革命")]
    [SerializeField] private GameObject revolutionOnObject;
    [SerializeField] private GameObject revolutionOffObject;

    [Header("1枚縛り用")]
    [SerializeField] private Image shibariSingleIcon;
    [SerializeField] private Sprite singleHeart;
    [SerializeField] private Sprite singleSpade;
    [SerializeField] private Sprite singleDiamond;
    [SerializeField] private Sprite singleClub;

    [Header("2枚縛り用")]
    [SerializeField] private Image shibariUpperIcon;
    [SerializeField] private Image shibariLowerIcon;

    [Header("2枚縛り用スプライト")]
    [SerializeField] private Sprite heartUpper;
    [SerializeField] private Sprite heartLower;
    [SerializeField] private Sprite spadeUpper;
    [SerializeField] private Sprite spadeLower;
    [SerializeField] private Sprite diamondUpper;
    [SerializeField] private Sprite diamondLower;
    [SerializeField] private Sprite clubUpper;
    [SerializeField] private Sprite clubLower;

    [Header("位置")]
    [SerializeField] private float hiddenY = 120f;
    [SerializeField] private float shownY = -20f;
    [SerializeField] private float slideSpeed = 12f;

    private bool isOpen;
    private float targetY;

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

        // 1枚
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

    public void Refresh(bool isRevolution, bool isLocked, List<Card> fieldCards)
    {
        SetRevolution(isRevolution);
        SetShibari(isLocked, fieldCards);
    }
}