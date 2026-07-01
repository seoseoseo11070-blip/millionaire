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

    private List<Card> deck = new List<Card>();
    private List<List<Card>> playerHands = new List<List<Card>>();

    private List<GameObject> spawnedCardObjects = new List<GameObject>();
    private List<Card> spawnedCardDatas = new List<Card>();

    private int currentFieldCardCount = 0;
    private int currentFieldCardStrength = 0;
    private bool isRevolution = false;

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
        yield return StartCoroutine(AnimateMultiShuffleAndSortMyHand());

        if (handController != null)
        {
            handController.SetupHand(spawnedCardObjects, this);
        }
    }
    // 
    private System.Collections.IEnumerator AnimateMultiShuffleAndSortMyHand()
    {
        List<Card> myHand = playerHands[0];

        HorizontalLayoutGroup layoutGroup = handArea.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup != null) layoutGroup.enabled = false;

        RectTransform handAreaRect = handArea.GetComponent<RectTransform>();
        float spacing = layoutGroup != null ? layoutGroup.spacing : -30f;
        float cardWidth = cardPrefab.GetComponent<RectTransform>().rect.width;
        float totalWidth = (cardWidth * spawnedCardObjects.Count) + (spacing * (spawnedCardObjects.Count - 1));
        float startX = -totalWidth / 2f + cardWidth / 2f;

        int shuffleCount = 4;
        for (int step = 0; step < shuffleCount; step++)
        {
            for (int i = spawnedCardObjects.Count - 1; i > 0; i--)
            {
                int r = Random.Range(0, i + 1);

                GameObject tmpObj = spawnedCardObjects[i];
                spawnedCardObjects[i] = spawnedCardObjects[r];
                spawnedCardObjects[r] = tmpObj;

                Card tmpData = spawnedCardDatas[i];
                spawnedCardDatas[i] = spawnedCardDatas[r];
                spawnedCardDatas[r] = tmpData;
            }

            for (int i = 0; i < spawnedCardObjects.Count; i++)
            {
                RectTransform cardRect = spawnedCardObjects[i].GetComponent<RectTransform>();
                if (cardRect != null)
                {
                    cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                    cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                    cardRect.pivot = new Vector2(0.5f, 0.5f);
                    float posX = startX + i * (cardWidth + spacing);
                    cardRect.anchoredPosition = new Vector2(posX, 0f);
                }
                spawnedCardObjects[i].transform.SetAsLastSibling();
            }

            yield return new WaitForSeconds(0.1f);
        }

        List<Vector2> startPositions = new List<Vector2>();
        foreach (GameObject obj in spawnedCardObjects)
        {
            startPositions.Add(obj.GetComponent<RectTransform>().anchoredPosition);
        }
        yield return new WaitForSeconds(0.3f);

        myHand.Sort((a, b) => b.strength.CompareTo(a.strength));

        List<GameObject> sortedObjects = new List<GameObject>();
        foreach (Card card in myHand)
        {
            int dataIndex = spawnedCardDatas.IndexOf(card);
            if (dataIndex != -1)
            {
                GameObject cardObj = spawnedCardObjects[dataIndex];
                sortedObjects.Add(cardObj);
                cardObj.transform.SetAsLastSibling();
            }
        }

        List<Vector2> targetPositions = new List<Vector2>();
        for (int i = 0; i < sortedObjects.Count; i++)
        {
            float posX = startX + i * (cardWidth + spacing);
            targetPositions.Add(new Vector2(posX, 0f));
        }

        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < sortedObjects.Count; i++)
            {
                int originalIndex = spawnedCardObjects.IndexOf(sortedObjects[i]);
                if (originalIndex != -1)
                {
                    sortedObjects[i].GetComponent<RectTransform>().anchoredPosition =
                        Vector2.Lerp(startPositions[originalIndex], targetPositions[i], t);
                }
            }
            yield return null;
        }

        for (int i = 0; i < sortedObjects.Count; i++)
        {
            sortedObjects[i].GetComponent<RectTransform>().anchoredPosition = targetPositions[i];
        }

        spawnedCardObjects = sortedObjects;
        spawnedCardDatas = new List<Card>(myHand);

        if (layoutGroup != null) layoutGroup.enabled = true;
    }

    public void ProcessPass()
    {
        Debug.Log("➡パス");
    }

    public bool CheckAndPlayCards(List<int> indices)
    {
        List<Card> myHand = playerHands[0];
        List<Card> selectedCards = new List<Card>();

        foreach (int index in indices)
        {
            selectedCards.Add(myHand[index]);
        }

        bool isPair = false;
        bool isKaidan = false;

        int targetNumber = -1;
        bool pairValid = true;
        foreach (Card card in selectedCards)
        {
            if (card.suit != Card.SuitType.Joker)
            {
                if (targetNumber == -1) targetNumber = card.number;
                else if (card.number != targetNumber) pairValid = false;
            }
        }
        if (pairValid) isPair = true;

        if (selectedCards.Count == 3 || selectedCards.Count == 4)
        {
            Card.SuitType kaidanSuit = Card.SuitType.Joker;
            List<int> strengths = new List<int>();
            int jokerCount = 0;

            foreach (Card card in selectedCards)
            {
                if (card.suit == Card.SuitType.Joker)
                {
                    jokerCount++;
                }
                else
                {
                    kaidanSuit = card.suit;
                    strengths.Add(card.strength);
                }
            }

            // マークがすべて統一されているか
            bool suitMatch = true;
            foreach (Card card in selectedCards)
            {
                if (card.suit != Card.SuitType.Joker && card.suit != kaidanSuit) suitMatch = false;
            }

            if (suitMatch)
            {
                strengths.Sort(); // 弱い順に並び替え

                if (jokerCount == 0)
                {
                    bool continuous = true;
                    for (int i = 0; i < strengths.Count - 1; i++)
                    {
                        if (strengths[i + 1] != strengths[i] + 1) continuous = false;
                    }
                    if (continuous) isKaidan = true;
                }
                else if (jokerCount == 1)
                {
                    int totalGap = 0;
                    for (int i = 0; i < strengths.Count - 1; i++)
                    {
                        totalGap += (strengths[i + 1] - strengths[i] - 1);
                    }
                    if (totalGap <= 1) isKaidan = true;
                }
            }
        }

        if (!isPair && !isKaidan)
        {
            return false;
        }

        List<GameObject> objectsToDestroy = new List<GameObject>();
        foreach (int index in indices)
        {
            objectsToDestroy.Add(spawnedCardObjects[index]);
            myHand.RemoveAt(index);
            spawnedCardObjects.RemoveAt(index);
            spawnedCardDatas.RemoveAt(index);
        }

        foreach (GameObject obj in objectsToDestroy)
        {
            Destroy(obj);
        }

        // 強さ判定の基準点を算出
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

        if (isPair && selectedCards.Count == 4)
        {
            isRevolution = !isRevolution;
            Debug.Log($"革命: {isRevolution}");
        }
        else if (isKaidan)
        {
            if (selectedCards.Count == 3)
            {
                Debug.Log($"階段");
            }
            else if (selectedCards.Count == 4)
            {
                isRevolution = !isRevolution;
                Debug.Log($"階段革命: {isRevolution}");
            }
        }
        List<string> playedNames = new List<string>();
        foreach (Card card in selectedCards)
        {
            if (card.suit == Card.SuitType.Joker) playedNames.Add("Joker");
            else playedNames.Add($"{card.suit}({card.number})");
        }
        string modeType = isKaidan ? "階段" : "ペア";
        Debug.Log($"場にカードを {selectedCards.Count} 枚({modeType})出しました！: " + string.Join(", ", playedNames));
        Debug.Log($"革命中: {isRevolution} ");

        return true;
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
