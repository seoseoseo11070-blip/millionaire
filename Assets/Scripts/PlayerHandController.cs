using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PlayerHandController : MonoBehaviour
{
    [SerializeField] private RectTransform cursorArrow;

    [Tooltip("カードから矢印までの高さ")]
    [SerializeField] private float arrowOffsetY = 0.5f;

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
        currentCursorIndex = 0;
        selectedIndices.Clear();
        isInputEnabled = true;

        if (cursorArrow != null)
        {
            cursorArrow.gameObject.SetActive(cardObjects.Count > 0);
        }
        UpdateVisuals();
    }

    void Update()
    {
        if (!isInputEnabled || cardObjects.Count == 0) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 矢印キー左
        if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            currentCursorIndex--;
            if (currentCursorIndex < 0) currentCursorIndex = cardObjects.Count - 1;
            UpdateVisuals();
        }

        // ▶ 矢印キー右
        if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            currentCursorIndex++;
            if (currentCursorIndex >= cardObjects.Count) currentCursorIndex = 0;
            UpdateVisuals();
        }

        //  スペースキー（選択・解除）
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            if (selectedIndices.Contains(currentCursorIndex))
            {
                selectedIndices.Remove(currentCursorIndex);
            }
            else
            {
                if (selectedIndices.Count >= 4)
                {
                    int oldestIndex = selectedIndices[0];
                    selectedIndices.RemoveAt(0);
                    Debug.Log($"枚数制限を超えたため、({oldestIndex + 1}枚目)を解除しました。");
                }

                selectedIndices.Add(currentCursorIndex);
            }

            UpdateVisuals();

            LogSelectedCards();
        }

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            PlayCardAtCursor();
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

        if (cardNames.Count > 0)
        {
            Debug.Log($"【現在選択中のカード一覧（計 {cardNames.Count} 枚）】: " + string.Join(" → ", cardNames));
        }
        else
        {
            Debug.Log("【現在選択中のカード一覧】: なし");
        }
    }

    private void PlayCardAtCursor()
    {
        if (currentCursorIndex < 0 || currentCursorIndex >= cardObjects.Count) return;

        GameObject targetCardObj = cardObjects[currentCursorIndex];
        cardObjects.RemoveAt(currentCursorIndex);
        Destroy(targetCardObj);

        if (gameManager != null)
        {
            gameManager.RemoveCardFromData(currentCursorIndex);
        }

        selectedIndices.Remove(currentCursorIndex);

        if (cardObjects.Count > 0)
        {
            if (currentCursorIndex >= cardObjects.Count)
            {
                currentCursorIndex = cardObjects.Count - 1;
            }
        }
        else
        {
            currentCursorIndex = 0;
            if (cursorArrow != null) cursorArrow.gameObject.SetActive(false);
            Debug.Log("上がり");
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
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
                    float arrowY = targetY + arrowOffsetY;
                    cursorArrow.localPosition = new Vector3(rect.localPosition.x, arrowY, cursorArrow.localPosition.z);
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

