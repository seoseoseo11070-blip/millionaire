using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PlayerHandController : MonoBehaviour
{
    [SerializeField] private RectTransform cursorArrow;

    [Header("矢印スプライト")]
    [SerializeField] private Sprite normalArrowSprite;
    [SerializeField] private Sprite sevenGiveArrowSprite;

    [SerializeField] private float arrowOffsetY = 0.5f;
    [SerializeField] private float arrowOffsetX = 0f;
    [SerializeField] private float arrowWidth = 50f;
    [SerializeField] private float arrowHeight = 50f;

    [Tooltip("選択中")]
    [SerializeField] private float selectRaiseY = 0.5f;

    private List<GameObject> cardObjects = new List<GameObject>();
    private List<int> selectedIndices = new List<int>();
    private int currentCursorIndex = 0;
    private bool isInputEnabled = false;
    private GameManager gameManager;

    private Image cursorImage;
    private Sprite cachedNormalSprite;

    void Awake()
    {
        if (cursorArrow != null)
            cursorImage = cursorArrow.GetComponent<Image>();

        if (normalArrowSprite != null)
            cachedNormalSprite = normalArrowSprite;
        else if (cursorImage != null)
            cachedNormalSprite = cursorImage.sprite;
    }
    public void SetupHand(List<GameObject> spawnedCards, GameManager manager, bool focusCenter = false)
    {
        cardObjects = spawnedCards ?? new List<GameObject>();
        gameManager = manager;

        selectedIndices.Clear();
        isInputEnabled = true;

        if (focusCenter && cardObjects.Count > 0)
            currentCursorIndex = cardObjects.Count / 2;
        else
            ClampCursorIndex();

        if (cursorArrow != null)
        {
            cursorArrow.anchorMin = new Vector2(0.5f, 0.5f);
            cursorArrow.anchorMax = new Vector2(0.5f, 0.5f);
            cursorArrow.pivot = new Vector2(0.5f, 0.5f);
        }

        ResetAllCardHeights();
        ApplyArrowVisual();
        RefreshCursorAfterHandChanged();
    }
    public void FocusArrowOnHandCenter()
    {
        if (cardObjects == null || cardObjects.Count == 0)
        {
            HideArrow();
            return;
        }

        currentCursorIndex = cardObjects.Count / 2;
        selectedIndices.Clear();
        ResetAllCardHeights();
        ApplyArrowVisual();
        SnapArrowToCurrentCard(hideFirst: true);
    }

    public void RefreshCursorAfterHandChanged()
    {
        selectedIndices.Clear();
        ClampCursorIndex();
        ResetAllCardHeights();
        ApplyArrowVisual();

        if (cardObjects == null || cardObjects.Count == 0)
        {
            HideArrow();
            return;
        }

        StopCoroutine(nameof(SnapArrowEndOfFrame));
        StartCoroutine(SnapArrowEndOfFrame());
    }

    private System.Collections.IEnumerator SnapArrowEndOfFrame()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        ClampCursorIndex();
        ResetAllCardHeights();

        if (cursorArrow != null)
            cursorArrow.gameObject.SetActive(true);

        PlaceArrowOnIndex(currentCursorIndex);

        if (cursorArrow != null)
            cursorArrow.SetAsLastSibling();
    }

    private void ClampCursorIndex()
    {
        if (cardObjects == null || cardObjects.Count == 0)
        {
            currentCursorIndex = 0;
            return;
        }

        if (currentCursorIndex >= cardObjects.Count)
            currentCursorIndex = cardObjects.Count - 1;
        if (currentCursorIndex < 0)
            currentCursorIndex = 0;
    }

    private void ResetAllCardHeights()
    {
        if (cardObjects == null) return;

        for (int i = 0; i < cardObjects.Count; i++)
        {
            if (cardObjects[i] == null) continue;
            RectTransform rect = cardObjects[i].GetComponent<RectTransform>();
            if (rect == null) continue;

            Vector3 lp = rect.localPosition;
            lp.y = 0f;
            rect.localPosition = lp;
        }
    }

    private void HideArrow()
    {
        if (cursorArrow != null)
            cursorArrow.gameObject.SetActive(false);
    }

    private void ShowCursorAfterMove()
    {
        if (cursorArrow == null) return;
        if (cardObjects == null || cardObjects.Count == 0)
        {
            HideArrow();
            return;
        }

        ClampCursorIndex();
        PlaceArrowOnIndex(currentCursorIndex);
        cursorArrow.gameObject.SetActive(true);
        cursorArrow.SetAsLastSibling();
    }
    private void SnapArrowToCurrentCard(bool hideFirst)
    {
        CancelInvoke(nameof(ShowCursorAfterMove));

        if (cardObjects == null || cardObjects.Count == 0)
        {
            HideArrow();
            return;
        }

        ClampCursorIndex();

        if (hideFirst && cursorArrow != null)
            cursorArrow.gameObject.SetActive(false);

        Invoke(nameof(ShowCursorAfterMove), 0.05f);
    }

    private void PlaceArrowOnIndex(int index)
    {
        if (cursorArrow == null) return;
        if (cardObjects == null || index < 0 || index >= cardObjects.Count) return;
        if (cardObjects[index] == null) return;

        RectTransform rect = cardObjects[index].GetComponent<RectTransform>();
        if (rect == null) return;

        Canvas.ForceUpdateCanvases();

        Vector3 cardWorldPos = rect.position;
        float halfHeightWorld = rect.rect.height * 0.5f * rect.lossyScale.y;

        cursorArrow.position = new Vector3(
            cardWorldPos.x + arrowOffsetX,
            cardWorldPos.y + halfHeightWorld + arrowOffsetY,
            cursorArrow.position.z
        );

        cursorArrow.localScale = Vector3.one;
        cursorArrow.sizeDelta = new Vector2(arrowWidth, arrowHeight);
    }
    private void ApplyArrowVisual()
    {
        if (cursorImage == null && cursorArrow != null)
            cursorImage = cursorArrow.GetComponent<Image>();
        if (cursorImage == null) return;

        bool isSeven = gameManager != null && gameManager.IsWaitingForSevenGive();

        if (isSeven && sevenGiveArrowSprite != null)
            cursorImage.sprite = sevenGiveArrowSprite;
        else if (cachedNormalSprite != null)
            cursorImage.sprite = cachedNormalSprite;
    }

    void Update()
    {
        if (!isInputEnabled || gameManager == null) return;
        if (cardObjects == null || cardObjects.Count == 0) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        bool isSeven = gameManager.IsWaitingForSevenGive();
        bool myTurn = gameManager.IsMyTurn();

        if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
        {
            currentCursorIndex--;
            if (currentCursorIndex < 0)
                currentCursorIndex = cardObjects.Count - 1;
            UpdateCardVisuals();
        }

        if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
        {
            currentCursorIndex++;
            if (currentCursorIndex >= cardObjects.Count)
                currentCursorIndex = 0;
            UpdateCardVisuals();
        }

        // 選択
        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame
            || keyboard.spaceKey.wasPressedThisFrame)
        {
            ToggleSelectCurrent();
        }

        // 自分の番と7渡し中だけ
        if (!isSeven && !myTurn)
            return;

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            if (isSeven)
                TryGiveSevenCards();
            else
                TryPlayOrPassSelectedCards();
        }

        if (!isSeven && (keyboard.pKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame))
        {
            if (myTurn)
                gameManager.ProcessPass();
        }
    }

    private void ToggleSelectCurrent()
    {
        if (currentCursorIndex < 0 || currentCursorIndex >= cardObjects.Count) return;

        if (selectedIndices.Contains(currentCursorIndex))
            selectedIndices.Remove(currentCursorIndex);
        else
            selectedIndices.Add(currentCursorIndex);

        UpdateCardVisuals();
    }

    private void TryPlayOrPassSelectedCards()
    {
        if (gameManager == null || !gameManager.IsMyTurn()) return;

        if (selectedIndices.Count == 0)
        {
            gameManager.ProcessPass();
            return;
        }

        List<GameObject> selected = new List<GameObject>();
        List<int> removed = new List<int>(selectedIndices);
        removed.Sort();

        foreach (int i in removed)
        {
            if (i >= 0 && i < cardObjects.Count && cardObjects[i] != null)
                selected.Add(cardObjects[i]);
        }

        int keepIndex = currentCursorIndex;

        foreach (int ri in removed)
        {
            if (ri < keepIndex)
                keepIndex--;
        }

        if (gameManager.CheckAndPlaySelectedObjects(selected))
        {
            selectedIndices.Clear();
            currentCursorIndex = keepIndex;
        }
    }

    private void TryGiveSevenCards()
    {
        if (gameManager == null || !gameManager.IsWaitingForSevenGive()) return;

        List<GameObject> selected = new List<GameObject>();
        List<int> removed = new List<int>(selectedIndices);
        removed.Sort();

        foreach (int i in removed)
        {
            if (i >= 0 && i < cardObjects.Count && cardObjects[i] != null)
                selected.Add(cardObjects[i]);
        }
        int keepIndex = currentCursorIndex;
        foreach (int ri in removed)
        {
            if (ri < keepIndex)
                keepIndex--;
        }

        if (gameManager.TryGiveCardsForSeven(selected))
        {
            selectedIndices.Clear();
            currentCursorIndex = keepIndex;
            ApplyArrowVisual();
        }
    }

    private void UpdateCardVisuals()
    {
        if (cardObjects == null) return;

        ClampCursorIndex();
        ApplyArrowVisual();

        for (int i = 0; i < cardObjects.Count; i++)
        {
            GameObject cardObj = cardObjects[i];
            if (cardObj == null) continue;

            RectTransform rect = cardObj.GetComponent<RectTransform>();
            if (rect == null) continue;

            float targetY = selectedIndices.Contains(i) ? selectRaiseY : 0f;
            Vector3 lp = rect.localPosition;
            lp.y = targetY;
            rect.localPosition = lp;
        }

        if (cursorArrow != null && cardObjects.Count > 0)
        {
            PlaceArrowOnIndex(currentCursorIndex);
            if (!cursorArrow.gameObject.activeSelf)
                cursorArrow.gameObject.SetActive(true);
            cursorArrow.SetAsLastSibling();
        }
    }
}