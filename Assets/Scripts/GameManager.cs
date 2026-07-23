using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Card
{
    public enum SuitType { Spade, Heart, Diamond, Club, Joker }

    public int id;
    public SuitType suit;
    public int number;
    public int strength;

    public Card(int id, SuitType suit, int number, int strength)
    {
        this.id = id;
        this.suit = suit;
        this.number = number;
        this.strength = strength;
    }
}

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform handArea;
    [SerializeField] private Sprite[] cardSprites;
    [SerializeField] private PlayerHandController handController;

    [Header("NPCの設定")]
    [SerializeField] private NPCController npcController;

    [Header("4つの場の設定")]
    [SerializeField] private Transform[] fieldSlots;

    [Header("カードのサイズ調整")]
    [SerializeField] private float cardWidth = 100f;
    [SerializeField] private float cardHeight = 140f;

    [Header("場のトランプの立体感調整")]
    [Tooltip("場に出たカードの縦の潰し具合")]
    [Range(0.1f, 1.0f)]
    [SerializeField] private float fieldCardScaleY = 0.7f;

    private List<Card> deck = new List<Card>();
    private List<List<Card>> playerHands = new List<List<Card>>();

    private List<GameObject> spawnedCardObjects = new List<GameObject>();
    private List<Card> spawnedCardDatas = new List<Card>();

    private int currentFieldCardCount = 0;
    private int currentFieldCardStrength = 0;
    private bool isRevolution = false;

    private int activePlayerIndex = 0;
    private bool isWaitingForPlayerInput = false;

    void Start()
    {
        StartGame(playerCount: 4);
    }

    public void StartGame(int playerCount)
    {
        spawnedCardObjects.Clear();
        spawnedCardDatas.Clear();

        currentFieldCardCount = 0;
        currentFieldCardStrength = 0;
        isRevolution = false;

        activePlayerIndex = 0;
        isWaitingForPlayerInput = false;

        ClearAllFieldSlots();

        CreateAdvancedDeck();
        ShuffleDeck();

        playerHands.Clear();
        for (int i = 0; i < playerCount; i++)
        {
            playerHands.Add(new List<Card>());
        }

        DistributeCards(playerCount);
        DebugLogHands();
        DisplayMyHand();
    }

    private void ClearAllFieldSlots()
    {
        foreach (Transform slot in fieldSlots)
        {
            if (slot != null)
            {
                foreach (Transform child in slot) Destroy(child.gameObject);
            }
        }
    }

    private void CreateAdvancedDeck()
    {
        deck.Clear();
        int currentId = 1;

        for (int s = 0; s < 4; s++)
        {
            Card.SuitType suit = (Card.SuitType)s;
            for (int num = 1; num <= 13; num++)
            {
                int strength = num - 2;
                if (num == 1) strength = 12;
                if (num == 2) strength = 13;
                deck.Add(new Card(currentId, suit, num, strength));
                currentId++;
            }
        }
        deck.Add(new Card(53, Card.SuitType.Joker, 0, 14));
        deck.Add(new Card(54, Card.SuitType.Joker, 0, 14));
    }

    private void ShuffleDeck()
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            Card tmp = deck[i];
            deck[i] = deck[r];
            deck[r] = tmp;
        }
    }

    private void DistributeCards(int playerCount)
    {
        int currentPlayer = 0;
        while (deck.Count > 0)
        {
            Card card = deck[0];
            deck.RemoveAt(0);
            playerHands[currentPlayer].Add(card);
            currentPlayer = (currentPlayer + 1) % playerCount;
        }

        for (int i = 1; i < playerCount; i++)
        {
            playerHands[i].Sort((a, b) => b.strength.CompareTo(a.strength));
        }
    }

    private void DisplayMyHand()
    {
        foreach (Transform child in handArea)
        {
            if (child.name == "CursorArrow") continue;
            Destroy(child.gameObject);
        }
        StartCoroutine(AnimateDistributeCards());
    }

    private System.Collections.IEnumerator AnimateDistributeCards()
    {
        List<Card> myHand = playerHands[0];

        foreach (Card card in myHand)
        {
            Transform canvasTransform = handArea.parent;
            GameObject newCard = Instantiate(cardPrefab, canvasTransform);
            spawnedCardObjects.Add(newCard);
            spawnedCardDatas.Add(card);

            Image cardImage = newCard.GetComponent<Image>();
            if (cardImage == null) cardImage = newCard.GetComponentInChildren<Image>();

            if (cardImage != null)
            {
                int spriteIndex = card.id - 1;
                if (spriteIndex >= 0 && spriteIndex < cardSprites.Length && cardSprites[spriteIndex] != null)
                {
                    cardImage.sprite = cardSprites[spriteIndex];
                }
            }

            RectTransform rect = newCard.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(cardWidth, cardHeight);
                Vector3 originalScale = cardPrefab.GetComponent<RectTransform>().localScale;
                rect.localScale = originalScale;

                Vector2 targetPosition = handArea.GetComponent<RectTransform>().anchoredPosition;
                rect.anchoredPosition = new Vector2(targetPosition.x, targetPosition.y - 500f);

                float elapsed = 0f;
                float duration = 0.15f;
                Vector2 startPosition = rect.anchoredPosition;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    rect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, elapsed / duration);
                    yield return null;
                }
                rect.anchoredPosition = targetPosition;
            }

            newCard.transform.SetParent(handArea);
            yield return new WaitForSeconds(0.04f);
        }

        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(AnimateHandSort());

        StartTurnLoop();
    }
    private System.Collections.IEnumerator AnimateHandSort()
    {
        List<Card> myHand = playerHands[0];

        HorizontalLayoutGroup layoutGroup = handArea.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup != null) layoutGroup.enabled = false;

        float spacing = layoutGroup != null ? layoutGroup.spacing : -30f;
        float totalWidth = (cardWidth * spawnedCardObjects.Count) + (spacing * (spawnedCardObjects.Count - 1));
        float startX = -totalWidth / 2f + cardWidth / 2f;

        for (int i = 0; i < spawnedCardObjects.Count; i++)
        {
            RectTransform cardRect = spawnedCardObjects[i].GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);
                float posX = startX + i * (cardWidth + spacing);
                cardRect.anchoredPosition = new Vector2(posX, 0f);
            }
            spawnedCardObjects[i].transform.SetAsLastSibling();
        }

        List<Card> targetGoalOrder = new List<Card>(myHand);
        if (isRevolution)
        {
            targetGoalOrder.Sort((a, b) => a.strength.CompareTo(b.strength));
        }
        else
        {
            targetGoalOrder.Sort((a, b) => b.strength.CompareTo(a.strength));
        }

        for (int i = 0; i < spawnedCardObjects.Count; i++)
        {
            Card idealCard = targetGoalOrder[i];
            int currentPosOfIdealCard = spawnedCardDatas.IndexOf(idealCard);

            if (currentPosOfIdealCard == i) continue;

            if (currentPosOfIdealCard != -1)
            {
                GameObject cardObjA = spawnedCardObjects[i];
                Card cardDataA = spawnedCardDatas[i];

                GameObject cardObjB = spawnedCardObjects[currentPosOfIdealCard];
                Card cardDataB = spawnedCardDatas[currentPosOfIdealCard];

                spawnedCardDatas[i] = cardDataB;
                spawnedCardDatas[currentPosOfIdealCard] = cardDataA;

                myHand[i] = cardDataB;
                myHand[currentPosOfIdealCard] = cardDataA;

                Image imageA = cardObjA.GetComponent<Image>();
                if (imageA == null) imageA = cardObjA.GetComponentInChildren<Image>();

                Image imageB = cardObjB.GetComponent<Image>();
                if (imageB == null) imageB = cardObjB.GetComponentInChildren<Image>();

                if (imageA != null && imageB != null)
                {
                    Sprite spriteTmp = imageA.sprite;
                    imageA.sprite = imageB.sprite;
                    imageB.sprite = spriteTmp;
                }

                yield return new WaitForSeconds(0.2f);
            }
        }

        if (layoutGroup != null) layoutGroup.enabled = true;

        if (handController != null)
        {
            handController.SetupHand(spawnedCardObjects, this);
        }
    }

    private void StartTurnLoop()
    {
        Debug.Log($"現在の手番 プレイヤー {activePlayerIndex + 1}");

        if (activePlayerIndex == 0)
        {
            isWaitingForPlayerInput = true;
            Debug.Log("あなたの番です");

            if (handController != null)
            {
                handController.SetupHand(spawnedCardObjects, this);
            }
        }
        else
        {
            isWaitingForPlayerInput = false;
            Invoke(nameof(CallNpcAI), 1.0f);
        }
    }

    private void CallNpcAI()
    {
        if (npcController != null)
        {
            npcController.ThinkAndPlay(activePlayerIndex, playerHands[activePlayerIndex]);
        }
    }

    public bool IsMyTurn()
    {
        return (activePlayerIndex == 0 && isWaitingForPlayerInput);
    }

    public void ProcessPass()
    {
        if (activePlayerIndex == 0 && isWaitingForPlayerInput)
        {
            Debug.Log("パスしました");
            isWaitingForPlayerInput = false;
            NextTurn();
        }
    }

    public void NextTurn()
    {
        activePlayerIndex = (activePlayerIndex + 1) % 4;
        StartTurnLoop();
    }

    public void PlayCpuCards(int cpuIndex, List<Card> selectedCards)
    {
        ClearAllFieldSlots();

        int playCount = selectedCards.Count;
        for (int i = 0; i < playCount; i++)
        {
            if (i < fieldSlots.Length && fieldSlots[i] != null && cardPrefab != null)
            {
                GameObject fieldedCard = Instantiate(cardPrefab, fieldSlots[i]);
                RectTransform fieldRect = fieldedCard.GetComponent<RectTransform>();
                if (fieldRect != null)
                {
                    fieldRect.anchorMin = new Vector2(0.5f, 0.5f);
                    fieldRect.anchorMax = new Vector2(0.5f, 0.5f);
                    fieldRect.pivot = new Vector2(0.5f, 0.5f);
                    fieldRect.anchoredPosition = Vector2.zero;
                    fieldRect.sizeDelta = new Vector2(cardWidth, cardHeight);
                    fieldRect.localScale = new Vector3(1f, fieldCardScaleY, 1f);
                }

                Image cardImage = fieldedCard.GetComponent<Image>();
                if (cardImage == null) cardImage = fieldedCard.GetComponentInChildren<Image>();
                if (cardImage != null)
                {
                    int spriteIndex = selectedCards[i].id - 1;
                    if (spriteIndex >= 0 && spriteIndex < cardSprites.Length && cardSprites[spriteIndex] != null)
                    {
                        cardImage.sprite = cardSprites[spriteIndex];
                    }
                }
            }
        }

        int selectedStrength = 0;
        foreach (Card card in selectedCards)
        {
            if (card.suit != Card.SuitType.Joker)
            {
                selectedStrength = card.strength;
                break;
            }
            selectedStrength = 14;
        }

        currentFieldCardCount = selectedCards.Count;
        currentFieldCardStrength = selectedStrength;

        if (selectedCards.Count == 4)
        {
            isRevolution = !isRevolution;
            Debug.Log($"CPUによって革命が発動しました,現在の革命状態{isRevolution}");
            StartCoroutine(AnimateHandSort());
        }

        List<string> playedNames = new List<string>();
        foreach (Card card in selectedCards)
        {
            if (card.suit == Card.SuitType.Joker) playedNames.Add("Joker");
            else playedNames.Add($"{card.suit}({card.number})");
        }
        Debug.Log($"プレイヤー {cpuIndex + 1} が場にカードを出しました" + string.Join(", ", playedNames));

        NextTurn();
    }
    public bool CheckAndPlaySelectedObjects(List<GameObject> selectedObjects)
    {
        if (activePlayerIndex != 0 || !isWaitingForPlayerInput) return false;

        List<Card> myHand = playerHands[0];
        List<Card> selectedCards = new List<Card>();

        foreach (GameObject obj in selectedObjects)
        {
            int idx = spawnedCardObjects.IndexOf(obj);
            if (idx != -1)
                selectedCards.Add(spawnedCardDatas[idx]);
        }

        if (selectedCards.Count == 0) return false;

        ClearAllFieldSlots();

        for (int i = 0; i < selectedCards.Count && i < fieldSlots.Length; i++)
        {
            GameObject fieldCard = Instantiate(cardPrefab, fieldSlots[i]);
            RectTransform rt = fieldCard.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(cardWidth, cardHeight);
                rt.localScale = new Vector3(1f, fieldCardScaleY, 1f);
            }

            Image img = fieldCard.GetComponentInChildren<Image>();
            if (img != null)
            {
                int spriteIdx = selectedCards[i].id - 1;
                if (spriteIdx >= 0 && spriteIdx < cardSprites.Length)
                    img.sprite = cardSprites[spriteIdx];
            }
        }

        // 手札から削除
        foreach (GameObject obj in selectedObjects)
        {
            int idx = spawnedCardObjects.IndexOf(obj);
            if (idx != -1)
            {
                myHand.Remove(spawnedCardDatas[idx]);
                spawnedCardObjects.RemoveAt(idx);
                spawnedCardDatas.RemoveAt(idx);
                Destroy(obj);
            }
        }

        // 手札再配置
        RearrangeRemainingHand();

        currentFieldCardCount = selectedCards.Count;
        currentFieldCardStrength = selectedCards[0].suit == Card.SuitType.Joker ? 14 : selectedCards[0].strength;

        if (selectedCards.Count == 4)
        {
            isRevolution = !isRevolution;
            Debug.Log($"革命,現在{isRevolution}");
        }

        isWaitingForPlayerInput = false;
        NextTurn();

        return true;
    }

    // 新規追加メソッド
    private void RearrangeRemainingHand()
    {
        HorizontalLayoutGroup layout = handArea.GetComponent<HorizontalLayoutGroup>();
        bool hadLayout = layout != null;
        if (hadLayout) layout.enabled = false;

        float spacing = hadLayout ? layout.spacing : -30f;
        float totalW = (cardWidth * spawnedCardObjects.Count) + spacing * (spawnedCardObjects.Count - 1);
        float startX = -totalW / 2f + cardWidth / 2f;

        for (int i = 0; i < spawnedCardObjects.Count; i++)
        {
            RectTransform r = spawnedCardObjects[i].GetComponent<RectTransform>();
            if (r != null)
                r.anchoredPosition = new Vector2(startX + i * (cardWidth + spacing), 0);
        }

        if (hadLayout) layout.enabled = true;
    }
    private System.Collections.IEnumerator AnimateHandSortAndNextTurn()
    {
        yield return StartCoroutine(AnimateHandSort());
        NextTurn();
    }

    public List<Card> GetPlayerHandData(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < playerHands.Count) return playerHands[playerIndex];
        return null;
    }

    private void DebugLogHands()
    {
        for (int i = 0; i < playerHands.Count; i++)
        {
            List<string> cardNames = new List<string>();
            foreach (Card card in playerHands[i])
            {
                if (card.suit == Card.SuitType.Joker) cardNames.Add("Joker");
                else cardNames.Add($"{card.suit}({card.number})");
            }
            DebugLogHandsEnd(i, cardNames);
        }
    }

    private void DebugLogHandsEnd(int playerIndex, List<string> cardNames)
    {
        Debug.Log($"プレイヤー {playerIndex + 1} の手札 ({playerHands[playerIndex].Count}枚): " + string.Join(", ", cardNames));
    }
}
