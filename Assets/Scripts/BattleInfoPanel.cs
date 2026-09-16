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

    [Header("縛りアイコン")]
    [SerializeField] private Image shibariIcon;
    [SerializeField] private Sprite spriteHeart;
    [SerializeField] private Sprite spriteSpade;
    [SerializeField] private Sprite spriteDiamond;
    [SerializeField] private Sprite spriteClub;

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
        SetShibari(null);
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
        {
            ToggleOpen();
        }
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

    public void SetShibari(Card.SuitType? suit)
    {
        if (shibariIcon == null) return;

        if (!suit.HasValue || suit.Value == Card.SuitType.Joker)
        {
            shibariIcon.gameObject.SetActive(false);
            return;
        }

        Sprite sp = suit.Value switch
        {
            Card.SuitType.Heart => spriteHeart,
            Card.SuitType.Spade => spriteSpade,
            Card.SuitType.Diamond => spriteDiamond,
            Card.SuitType.Club => spriteClub,
            _ => null
        };

        if (sp == null)
        {
            shibariIcon.gameObject.SetActive(false);
            return;
        }

        shibariIcon.sprite = sp;
        shibariIcon.gameObject.SetActive(true);
    }

    public void Refresh(bool isRevolution, Card.SuitType? lockedSuit)
    {
        SetRevolution(isRevolution);
        SetShibari(lockedSuit);
    }
}