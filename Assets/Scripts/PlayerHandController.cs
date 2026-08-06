using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHandController : MonoBehaviour
{
    [SerializeField] private RectTransform cursorArrow;

    [Tooltip("カードの頭上から矢印までの高さ")]
    [SerializeField] private float arrowOffsetY = 0.5f;

    [Tooltip("矢印の左右の位置微調整")]
    [SerializeField] private float arrowOffsetX = 0f;

    [SerializeField] private float arrowWidth = 50f;
    [SerializeField] private float arrowHeight = 50f;

    private List<GameObject> cardObjects = new List<GameObject>();
    private List<int> selectedIndices = new List<int>();
    private int currentCursorIndex = 0;
    private bool isInputEnabled = false;
    private GameManager gameManager;

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
            cursorArrow.gameObject.SetActive(cardObjects != null && cardObjects.Count > 0);
        }

        UpdateVisuals();
    }

    void Update()
    {
        if (!isInputEnabled || cardObjects == null || cardObjects.Count == 0) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            currentCursorIndex--;
            if (currentCursorIndex < 0) currentCursorIndex = cardObjects.Count - 1;
            UpdateVisuals();
        }

        if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            currentCursorIndex++;
            if (currentCursorIndex >= cardObjects.Count) currentCursorIndex = 0;
            UpdateVisuals();
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            ToggleSelection(currentCursorIndex);
            UpdateVisuals();
        }

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            TryPlayOrPassSelectedCards();
        }
    }

    private void ToggleSelection(int index)
    {
        index = Mathf.Clamp(index, 0, cardObjects.Count - 1);

        if (selectedIndices.Contains(index))
        {
            selectedIndices.Remove(index);
            return;
        }

        int maxSelect = 4;
        if (gameManager != null && gameManager.IsWaitingForSevenGive())
            maxSelect = gameManager.GetSevenGiveCount();

        if (selectedIndices.Count >= maxSelect)
            selectedIndices.RemoveAt(0);

        selectedIndices.Add(index);
    }

    private void TryPlayOrPassSelectedCards()
    {
        if (gameManager == null) return;

        if (gameManager.IsWaitingForSevenGive())
        {
            List<GameObject> giveObjects = new List<GameObject>();
            foreach (int index in selectedIndices)
            {
                if (index >= 0 && index < cardObjects.Count)
                    giveObjects.Add(cardObjects[index]);
            }

            bool ok = gameManager.TryGiveCardsForSeven(giveObjects);
            if (ok)
            {
                foreach (GameObject obj in giveObjects)
                    cardObjects.Remove(obj);

                selectedIndices.Clear();

                if (cardObjects.Count > 0)
                    currentCursorIndex = cardObjects.Count / 2;
                else
                {
                    currentCursorIndex = 0;
                    if (cursorArrow != null) cursorArrow.gameObject.SetActive(false);
                }

                if (cursorArrow != null) cursorArrow.gameObject.SetActive(false);
                UpdateVisuals();
                if (cardObjects.Count > 0)
                    Invoke(nameof(ShowCursorAfterMove), 0.05f);
            }
            else
            {
                UpdateVisuals();
            }
            return;
        }

        if (!gameManager.IsMyTurn())
        {
            Debug.LogWarning("あなたの番ではありません");
            return;
        }

        if (selectedIndices.Count == 0)
        {
            gameManager.ProcessPass();
            return;
        }

        selectedIndices.RemoveAll(index => index < 0 || index >= cardObjects.Count);
        if (selectedIndices.Count == 0)
        {
            gameManager.ProcessPass();
            return;
        }

        List<GameObject> selectedObjects = new List<GameObject>();
        foreach (int index in selectedIndices)
            selectedObjects.Add(cardObjects[index]);

        bool canPlay = gameManager.CheckAndPlaySelectedObjects(selectedObjects);

        if (canPlay)
        {
            foreach (GameObject obj in selectedObjects)
                cardObjects.Remove(obj);

            selectedIndices.Clear();

            if (gameManager.IsWaitingForSevenGive())
            {
                if (cardObjects.Count > 0)
                    currentCursorIndex = cardObjects.Count / 2;
                UpdateVisuals();
                return;
            }

            if (cardObjects.Count > 0)
            {
                currentCursorIndex = cardObjects.Count / 2;
                if (cursorArrow != null) cursorArrow.gameObject.SetActive(false);
                UpdateVisuals();
                Invoke(nameof(ShowCursorAfterMove), 0.05f);
            }
            else
            {
                currentCursorIndex = 0;
                if (cursorArrow != null) cursorArrow.gameObject.SetActive(false);
            }
        }

        UpdateVisuals();
    }

    private void ShowCursorAfterMove()
    {
        if (cursorArrow != null && cardObjects.Count > 0)
        {
            cursorArrow.gameObject.SetActive(true);
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        if (cardObjects == null) return;

        if (cardObjects.Count > 0)
        {
            if (currentCursorIndex < 0 || currentCursorIndex >= cardObjects.Count)
                currentCursorIndex = cardObjects.Count / 2;
        }
        else
        {
            currentCursorIndex = 0;
            if (cursorArrow != null) cursorArrow.gameObject.SetActive(false);
            return;
        }

        selectedIndices.RemoveAll(index => index < 0 || index >= cardObjects.Count);

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
            }
        }

        if (cursorArrow != null)
            cursorArrow.SetAsLastSibling();
    }
}