using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PlayerHandController : MonoBehaviour
{
    [SerializeField] private RectTransform cursorArrow;

    [Header("矢印スプライト")]
    [Tooltip("通常選択用")]
    [SerializeField] private Sprite normalArrowSprite;

    [Tooltip("7渡し用")]
    [SerializeField] private Sprite sevenGiveArrowSprite;

    [Tooltip("カードの頭上から矢印までの高さ")]
    [SerializeField] private float arrowOffsetY = 0.5f;

    [Tooltip("矢印の左右微調整")]
    [SerializeField] private float arrowOffsetX = 0f;

    [SerializeField] private float arrowWidth = 50f;
    [SerializeField] private float arrowHeight = 50f;

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

    public void SetupHand(List<GameObject> spawnedCards, GameManager manager)
    {
        cardObjects = spawnedCards;
        gameManager = manager;

        if (cardObjects != null && cardObjects.Count > 0)
            currentCursorIndex = cardObjects.Count / 2;
        else
            currentCursorIndex = 0;

        selectedIndices.Clear();
        isInputEnabled = true;

        if (cursorArrow != null)
        {
            cursorArrow.anchorMin = new Vector2(0.5f, 0.5f);
            cursorArrow.anchorMax = new Vector2(0.5f, 0.5f);
            cursorArrow.pivot = new Vector2(0.5f, 0.5f);
        }

        ApplyArrowVisual();
        FocusArrowOnHandCenter();
        UpdateCardVisuals();
    }

    public void FocusArrowOnHandCenter()
    {
        if (cardObjects == null || cardObjects.Count == 0)
        {
            if (cursorArrow != null)
                cursorArrow.gameObject.SetActive(false);
            return;
        }

        currentCursorIndex = cardObjects.Count / 2;
        if (currentCursorIndex >= cardObjects.Count)
            currentCursorIndex = cardObjects.Count - 1;

        if (cursorArrow != null)
            cursorArrow.gameObject.SetActive(false);

        ApplyArrowVisual();
        UpdateCardVisuals();
        Invoke(nameof(ShowCursorAfterMove), 0.05f);
    }

    private void ShowCursorAfterMove()
    {
        if (cursorArrow != null && cardObjects != null && cardObjects.Count > 0)
        {
            ApplyArrowVisual();
            cursorArrow.gameObject.SetActive(true);
            UpdateCardVisuals();
        }
    }

    void Update()
    {
        if (!isInputEnabled || gameManager == null) return;
        if (cardObjects == null || cardObjects.Count == 0) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        bool isSeven = gameManager.IsWaitingForSevenGive();

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
        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame
            || keyboard.spaceKey.wasPressedThisFrame)
        {
            ToggleSelectCurrent();
        }

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            if (isSeven)
                TryGiveSevenCards();
            else
                TryPlayOrPassSelectedCards();
        }

        if (!isSeven && (keyboard.pKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame))
        {
            if (gameManager.IsMyTurn())
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
        if (gameManager == null) return;
        if (!gameManager.IsMyTurn()) return;

        if (selectedIndices.Count == 0)
        {
            gameManager.ProcessPass();
            return;
        }

        List<GameObject> selected = new List<GameObject>();
        selectedIndices.Sort();
        foreach (int i in selectedIndices)
        {
            if (i >= 0 && i < cardObjects.Count && cardObjects[i] != null)
                selected.Add(cardObjects[i]);
        }

        if (gameManager.CheckAndPlaySelectedObjects(selected))
        {
            selectedIndices.Clear();
        }
    }

    private void TryGiveSevenCards()
    {
        if (gameManager == null) return;
        if (!gameManager.IsWaitingForSevenGive()) return;

        List<GameObject> selected = new List<GameObject>();
        selectedIndices.Sort();
        foreach (int i in selectedIndices)
        {
            if (i >= 0 && i < cardObjects.Count && cardObjects[i] != null)
                selected.Add(cardObjects[i]);
        }

        if (gameManager.TryGiveCardsForSeven(selected))
        {
            selectedIndices.Clear();
            ApplyArrowVisual();
        }
    }
    private void ApplyArrowVisual()
    {
        if (cursorImage == null && cursorArrow != null)
            cursorImage = cursorArrow.GetComponent<Image>();

        if (cursorImage == null) return;

        bool isSeven = gameManager != null && gameManager.IsWaitingForSevenGive();

        if (isSeven && sevenGiveArrowSprite != null)
        {
            cursorImage.sprite = sevenGiveArrowSprite;
        }
        else
        {
            if (cachedNormalSprite != null)
                cursorImage.sprite = cachedNormalSprite;
        }
    }

    private void UpdateCardVisuals()
    {
        if (cardObjects == null) return;

        if (cardObjects.Count > 0 && currentCursorIndex >= cardObjects.Count)
            currentCursorIndex = cardObjects.Count - 1;
        if (currentCursorIndex < 0)
            currentCursorIndex = 0;

        ApplyArrowVisual();

        for (int i = 0; i < cardObjects.Count; i++)
        {
            GameObject cardObj = cardObjects[i];
            if (cardObj == null) continue;

            RectTransform rect = cardObj.GetComponent<RectTransform>();
            if (rect == null) continue;

            float targetY = selectedIndices.Contains(i) ? 0.5f : 0f;
            rect.localPosition = new Vector3(rect.localPosition.x, targetY, rect.localPosition.z);

            if (i == currentCursorIndex && cursorArrow != null)
            {
                if (!cursorArrow.gameObject.activeSelf)
                    cursorArrow.gameObject.SetActive(true);

                Vector3 cardWorldPos = rect.position;
                float halfHeightWorld = rect.rect.height * 0.5f * rect.lossyScale.y;

                cursorArrow.position = new Vector3(
                    cardWorldPos.x + arrowOffsetX,
                    cardWorldPos.y + halfHeightWorld + arrowOffsetY,
                    cursorArrow.position.z
                );

                cursorArrow.localScale = Vector3.one;
                cursorArrow.sizeDelta = new Vector2(arrowWidth, arrowHeight);
                cursorArrow.SetAsLastSibling();
            }
        }
    }
}