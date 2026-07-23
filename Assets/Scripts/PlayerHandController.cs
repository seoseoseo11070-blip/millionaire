using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PlayerHandController : MonoBehaviour
{
    [SerializeField] private RectTransform cursorArrow;

    [Tooltip("カードから矢印までの高さ")]
    [SerializeField] private float arrowOffsetY = 0.5f;

    [Tooltip("矢印の位置調整")]
    [SerializeField] private float arrowOffsetX = 0f;

    [Tooltip("矢印の横幅")]
    [SerializeField] private float arrowWidth = 50f;
    [Tooltip("矢印の縦幅")]
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
        {
            currentCursorIndex = cardObjects.Count / 2;
        }
        else
        {
            currentCursorIndex = 0;
        }

        selectedIndices.Clear();
        isInputEnabled = true;

        if (cursorArrow != null)
        {
            cursorArrow.anchorMin = new Vector2(0.5f, 0.5f);
            cursorArrow.anchorMax = new Vector2(0.5f, 0.5f);
            cursorArrow.pivot = new Vector2(0.5f, 0.5f);
            cursorArrow.gameObject.SetActive(cardObjects.Count > 0);
        }
        UpdateVisuals();
    }

    void Update()
    {
        if (!isInputEnabled || cardObjects.Count == 0) return;

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
            currentCursorIndex = Mathf.Clamp(currentCursorIndex, 0, cardObjects.Count - 1);
            if (selectedIndices.Contains(currentCursorIndex))
            {
                selectedIndices.Remove(currentCursorIndex);
            }
            else
            {
                if (selectedIndices.Count >= 4)
                {
                    selectedIndices.RemoveAt(0);
                }
                selectedIndices.Add(currentCursorIndex);
            }
            UpdateVisuals();
            LogSelectedCards();
        }

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            TryPlayOrPassSelectedCards();
        }
    }

    private void LogSelectedCards()
    {
        if (gameManager == null) return;

        List<string> cardNames = new List<string>();
        List<Card> myHand = gameManager.GetPlayerHandData(0);

        foreach (int index in selectedIndices)
        {
            if (myHand != null && index >= 0 && index < myHand.Count)
            {
                Card card = myHand[index];
                if (card.suit == Card.SuitType.Joker) cardNames.Add("Joker");
                else cardNames.Add($"{card.suit}({card.number})");
            }
        }

        if (cardNames.Count > 0) Debug.Log($"【現在選択中のカード一覧（計 {cardNames.Count} 枚）】: " + string.Join(" → ", cardNames));
        else Debug.Log("【現在選択中のカード一覧】: なし");
    }

    private void TryPlayOrPassSelectedCards()
    {
        if (gameManager == null) return;

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
        {
            if (index < cardObjects.Count)
                selectedObjects.Add(cardObjects[index]);
        }

        bool canPlay = gameManager.CheckAndPlaySelectedObjects(selectedObjects);

        if (canPlay)
        {
            foreach (GameObject obj in selectedObjects)
            {
                cardObjects.Remove(obj);
            }
            selectedIndices.Clear();
            if (cardObjects.Count > 0)
            {
                currentCursorIndex = cardObjects.Count / 2;

                if (cursorArrow != null)
                {
                    cursorArrow.gameObject.SetActive(false);
                }
            }
            else
            {
                currentCursorIndex = 0;
                if (cursorArrow != null)
                    cursorArrow.gameObject.SetActive(false);
            }

            UpdateVisuals();
            Invoke(nameof(ShowCursorAfterMove), 0.05f);
        }
        else
        {
            Debug.LogWarning("そのカードの組み合わせは場に出せません");
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
        if (cardObjects.Count > 0)
        {
            if (currentCursorIndex < 0 || currentCursorIndex >= cardObjects.Count)
            {
                currentCursorIndex = cardObjects.Count / 2;
            }
        }
        else
        {
            currentCursorIndex = 0;
            if (cursorArrow != null) cursorArrow.gameObject.SetActive(false);
        }

        selectedIndices.RemoveAll(index => index < 0 || index >= cardObjects.Count);

        if (cardObjects.Count > 0 && currentCursorIndex >= cardObjects.Count)
        {
            currentCursorIndex = cardObjects.Count - 1;
        }

        for (int i = 0; i < cardObjects.Count; i++)
        {
            GameObject cardObj = cardObjects[i];
            if (cardObj == null) continue;

            RectTransform rect = cardObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                float targetY = selectedIndices.Contains(i) ? 0.5f : 0f;
                rect.localPosition = new Vector3(rect.localPosition.x, targetY, rect.localPosition.z);

                if (i == currentCursorIndex && cursorArrow != null)
                {
                    if (!cursorArrow.gameObject.activeSelf) cursorArrow.gameObject.SetActive(true);

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
        }

        if (cursorArrow != null)
        {
            cursorArrow.SetAsLastSibling();
        }
    }
}

