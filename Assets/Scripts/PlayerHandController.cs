using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PlayerHandController : MonoBehaviour
{
    [SerializeField] private RectTransform cursorArrow;

    [Tooltip("カードの頭上から矢印までの高さ（自由に数値を調整してください）")]
    [SerializeField] private float arrowOffsetY = 0.5f;

    [Tooltip("矢印の横幅(Width)")]
    [SerializeField] private float arrowWidth = 50f;
    [Tooltip("矢印の縦幅(Height)")]
    [SerializeField] private float arrowHeight = 50f;

    private List<GameObject> cardObjects = new List<GameObject>();
    private HashSet<int> selectedIndices = new HashSet<int>();
    private int currentCursorIndex = 0;
    private bool isInputEnabled = false;

    public void SetupHand(List<GameObject> spawnedCards)
    {
        cardObjects = spawnedCards;
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
            if (selectedIndices.Contains(currentCursorIndex))
            {
                selectedIndices.Remove(currentCursorIndex);
            }
            else
            {
                selectedIndices.Add(currentCursorIndex);
            }
            UpdateVisuals();
        }
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
                float targetY = selectedIndices.Contains(i) ? 40f : 0f;
                rect.localPosition = new Vector3(rect.localPosition.x, targetY, rect.localPosition.z);

                if (i == currentCursorIndex && cursorArrow != null)
                {
                    float arrowY = targetY + arrowOffsetY;
                    cursorArrow.localPosition = new Vector3(rect.localPosition.x, arrowY, cursorArrow.localPosition.z);

                    cursorArrow.sizeDelta = new Vector2(arrowWidth, arrowHeight);

                    Debug.Log($"【位置確認】選択中のカード({i + 1}枚目)の座標: X={rect.localPosition.x:F1}, Y={rect.localPosition.y:F1} | " +
                              $"矢印の座標: X={cursorArrow.localPosition.x:F1}, Y={cursorArrow.localPosition.y:F1} | " +
                              $"矢印サイズ: {arrowWidth} x {arrowHeight}");
                }
            }
        }

        if (cursorArrow != null)
        {
            cursorArrow.SetAsLastSibling();
        }
    }
}

