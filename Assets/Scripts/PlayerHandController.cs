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

        //  矢印キー左
        if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            currentCursorIndex--;
            if (currentCursorIndex < 0) currentCursorIndex = cardObjects.Count - 1;
            UpdateVisuals();
        }

        //  矢印キー右
        if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            currentCursorIndex++;
            if (currentCursorIndex >= cardObjects.Count) currentCursorIndex = 0;
            UpdateVisuals();
        }

        // スペースキー
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
                    selectedIndices.RemoveAt(0);
                }
                selectedIndices.Add(currentCursorIndex);
            }
            UpdateVisuals();
            LogSelectedCards();
        }

        // ■ Enterキー
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

        List<int> sortedIndices = new List<int>(selectedIndices);
        sortedIndices.Sort((a, b) => b.CompareTo(a));

        bool canPlay = gameManager.CheckAndPlayCards(sortedIndices);

        if (canPlay)
        {

            selectedIndices.Clear();

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
                Debug.Log("上がりです");
            }
        }
        else
        {
            Debug.LogWarning("そのカードの組み合わせは場に出せません");
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        selectedIndices.RemoveAll(index => index < 0 || index >= cardObjects.Count);

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
                    Vector3 cardWroldPos = rect.position;
                    float halfHeightWorld = rect.rect.height * 0.5f * rect.lossyScale.y;
                    cursorArrow.position = new Vector3(cardWroldPos.x, cardWroldPos.y + halfHeightWorld + arrowOffsetY, cursorArrow.position.z);
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

