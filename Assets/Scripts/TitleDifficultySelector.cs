using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public enum NpcDifficulty
{
    Weak = 0,    // 弱い
    Normal = 1,  // 普通
    Strong = 2   // 強い
}

public class TitleDifficultySelector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public const string PrefsKey = "NpcDifficulty";

    [Header("全体")]
    [SerializeField] private RectTransform panelRoot;

    [Header("表示画像")]
    [SerializeField] private Image barImage;
    [SerializeField] private Sprite spriteWeak;    // 弱い
    [SerializeField] private Sprite spriteNormal;  // 普通
    [SerializeField] private Sprite spriteStrong;  // 強い

    [Header("位置")]
    [SerializeField] private float hiddenX = 900f;
    [SerializeField] private float shownX = 0f;
    [SerializeField] private float slideSpeed = 12f;

    [Header("ホイール")]
    [SerializeField] private bool requireHover = true;
    [Header("スタートボタン（任意）")]
    [SerializeField] private Button startButton;



    private NpcDifficulty difficulty = NpcDifficulty.Weak;
    private bool isOpen = false;
    private bool isHover = false;
    private float targetX;

    void Start()
    {
        difficulty = (NpcDifficulty)PlayerPrefs.GetInt(PrefsKey, (int)NpcDifficulty.Weak);
        ApplyVisual();

        isOpen = false;
        targetX = hiddenX;
        if (panelRoot != null)
        {
            Vector2 p = panelRoot.anchoredPosition;
            p.x = hiddenX;
            panelRoot.anchoredPosition = p;
        }
        UpdateStartButton();
    }

    void Update()
    {
        if (panelRoot != null)
        {
            Vector2 p = panelRoot.anchoredPosition;
            p.x = Mathf.Lerp(p.x, targetX, Time.deltaTime * slideSpeed);
            panelRoot.anchoredPosition = p;
        }

        if (!isOpen) return;
        if (requireHover && !isHover) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) < 0.01f) return;

        if (scroll > 0)
            ChangeDifficulty(1);
        else
            ChangeDifficulty(-1);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleOpen();
    }

    public void ToggleOpen()
    {
        isOpen = !isOpen;
        targetX = isOpen ? shownX : hiddenX;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHover = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHover = false;
    }

    private void ApplyVisual()
    {
        if (barImage == null) return;
        barImage.sprite = difficulty switch
        {
            NpcDifficulty.Strong => spriteStrong,
            NpcDifficulty.Normal => spriteNormal,
            _ => spriteWeak
        };
    }

    private void SaveDifficulty()
    {
        PlayerPrefs.SetInt(PrefsKey, (int)difficulty);
        PlayerPrefs.Save();
        Debug.Log($"NPC強さ: {difficulty}");
    }

    public static NpcDifficulty LoadDifficulty()
    {
        return (NpcDifficulty)PlayerPrefs.GetInt(PrefsKey, (int)NpcDifficulty.Weak);
    }
    public static bool IsDifficultyPlayable(NpcDifficulty d)
    {
        return d == NpcDifficulty.Weak || d == NpcDifficulty.Strong;
    }

    public static bool IsCurrentDifficultyPlayable()
    {
        return IsDifficultyPlayable(LoadDifficulty());
    }

    private void UpdateStartButton()
    {
        if (startButton == null) return;
        startButton.interactable = IsDifficultyPlayable(difficulty);
    }

    private void ChangeDifficulty(int delta)
    {
        int value = (int)difficulty + delta;
        value = Mathf.Clamp(value, 0, 2);
        NpcDifficulty next = (NpcDifficulty)value;
        if (next == difficulty) return;

        difficulty = next;
        ApplyVisual();
        SaveDifficulty();
        UpdateStartButton();
    }
}