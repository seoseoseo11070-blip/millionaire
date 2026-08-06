using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform handArea;
    [SerializeField] private Sprite[] cardSprites;
    [SerializeField] private PlayerHandController handController;

    [Header("NPC")]
    [SerializeField] private NPCController npcController;

    [Header("場スロット")]
    [SerializeField] private Transform[] fieldSlots;

    [Header("カードサイズ")]
    [SerializeField] private float cardWidth = 100f;
    [SerializeField] private float cardHeight = 140f;

    [Header("場のカードの縦潰し")]
    [Range(0.1f, 1.0f)]
    [SerializeField] private float fieldCardScaleY = 0.7f;

    private readonly PlayValidator validator = new PlayValidator();

    private List<Card> deck = new List<Card>();
    private List<List<Card>> playerHands = new List<List<Card>>();
    private List<string> roundActionLog = new List<string>();
    private List<GameObject> spawnedCardObjects = new List<GameObject>();
    private List<Card> spawnedCardDatas = new List<Card>();

    private int consecutivePassCount = 0;
    private int lastPlayPlayerIndex = -1;
    private int currentFieldCardCount = 0;
    private int currentFieldCardStrength = 0;
    private bool isRevolution = false;

    private Card.SuitType? lockedSuit = null;
    private Card.SuitType? lastPlaySuit = null;

    private string currentFieldPlayType = "";
    private bool fieldIsSingleJoker = false;
    private int currentFieldKaidanMin = 0;
    private int currentFieldKaidanMax = 0;

    private int activePlayerIndex = 0;
    private bool isWaitingForPlayerInput = false;

    private bool isWaitingForSevenGive = false;
    private int sevenGiveCount = 0;
    private int sevenGiveToIndex = 0;

    private bool isClearingField = false;

    public int GetCurrentFieldCardCount() => currentFieldCardCount;
    public int GetCurrentFieldCardStrength() => currentFieldCardStrength;
    public bool IsRevolution() => isRevolution;
    public bool IsWaitingForSevenGive() => isWaitingForSevenGive;
    public int GetSevenGiveCount() => sevenGiveCount;
    public bool IsFieldSingleJoker() => fieldIsSingleJoker;
    public string GetCurrentFieldPlayType() => currentFieldPlayType;
    public int GetCurrentFieldKaidanMin() => currentFieldKaidanMin;
    public int GetCurrentFieldKaidanMax() => currentFieldKaidanMax;

    public bool IsMyTurn()
    {
        return activePlayerIndex == 0
            && isWaitingForPlayerInput
            && !isWaitingForSevenGive
            && !isClearingField;
    }

    public List<Card> GetPlayerHandData(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < playerHands.Count)
            return playerHands[playerIndex];
        return null;
    }

    public bool TryValidatePlay(List<Card> cards, out string playType, out string error)
    {
        return validator.TryValidate(
            cards,
            currentFieldCardCount,
            currentFieldCardStrength,
            currentFieldPlayType,
            isRevolution,
            fieldIsSingleJoker,
            lockedSuit,
            currentFieldKaidanMin,
            currentFieldKaidanMax,
            out playType,
            out error);
    }

    void Start()
    {
        StartGame(4);
    }

    public void StartGame(int playerCount)
    {
        roundActionLog.Clear();
        consecutivePassCount = 0;
        lastPlayPlayerIndex = -1;
        spawnedCardObjects.Clear();
        spawnedCardDatas.Clear();

        currentFieldCardCount = 0;
        currentFieldCardStrength = 0;
        currentFieldPlayType = "";
        currentFieldKaidanMin = 0;
        currentFieldKaidanMax = 0;
        isRevolution = false;
        lockedSuit = null;
        lastPlaySuit = null;
        fieldIsSingleJoker = false;

        isWaitingForSevenGive = false;
        sevenGiveCount = 0;
        isClearingField = false;

        activePlayerIndex = 0;
        isWaitingForPlayerInput = false;

        ClearAllFieldSlots();
        CreateAdvancedDeck();
        ShuffleDeck();

        playerHands.Clear();
        for (int i = 0; i < playerCount; i++)
            playerHands.Add(new List<Card>());

        DistributeCards(playerCount);
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
            (deck[i], deck[r]) = (deck[r], deck[i]);
        }
    }

    private void DistributeCards(int playerCount)
    {
        int currentPlayer = 0;
        while (deck.Count > 0)
        {
            playerHands[currentPlayer].Add(deck[0]);
            deck.RemoveAt(0);
            currentPlayer = (currentPlayer + 1) % playerCount;
        }

        for (int i = 1; i < playerCount; i++)
            playerHands[i].Sort((a, b) => b.strength.CompareTo(a.strength));
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
            GameObject newCard = Instantiate(cardPrefab, handArea.parent);
            spawnedCardObjects.Add(newCard);
            spawnedCardDatas.Add(card);

            Image cardImage = newCard.GetComponentInChildren<Image>();
            if (cardImage != null)
            {
                int spriteIndex = card.id - 1;
                if (spriteIndex >= 0 && spriteIndex < cardSprites.Length && cardSprites[spriteIndex] != null)
                    cardImage.sprite = cardSprites[spriteIndex];
            }

            RectTransform rect = newCard.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(cardWidth, cardHeight);
                rect.localScale = cardPrefab.GetComponent<RectTransform>().localScale;

                Vector2 target = handArea.GetComponent<RectTransform>().anchoredPosition;
                Vector2 start = new Vector2(target.x, target.y - 500f);
                rect.anchoredPosition = start;

                float elapsed = 0f;
                float duration = 0.15f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    rect.anchoredPosition = Vector2.Lerp(start, target, elapsed / duration);
                    yield return null;
                }
                rect.anchoredPosition = target;
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
        float totalWidth = (cardWidth * spawnedCardObjects.Count) + (spacing * Mathf.Max(0, spawnedCardObjects.Count - 1));
        float startX = -totalWidth / 2f + cardWidth / 2f;

        for (int i = 0; i < spawnedCardObjects.Count; i++)
        {
            RectTransform cardRect = spawnedCardObjects[i].GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.anchorMin = cardRect.anchorMax = cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);
                cardRect.anchoredPosition = new Vector2(startX + i * (cardWidth + spacing), 0f);
            }
            spawnedCardObjects[i].transform.SetAsLastSibling();
        }

        List<Card> targetOrder = new List<Card>(myHand);
        if (isRevolution)
            targetOrder.Sort((a, b) => a.strength.CompareTo(b.strength));
        else
            targetOrder.Sort((a, b) => b.strength.CompareTo(a.strength));

        for (int i = 0; i < spawnedCardObjects.Count; i++)
        {
            int currentPos = spawnedCardDatas.IndexOf(targetOrder[i]);
            if (currentPos == i || currentPos == -1) continue;

            (spawnedCardDatas[i], spawnedCardDatas[currentPos]) = (spawnedCardDatas[currentPos], spawnedCardDatas[i]);
            (myHand[i], myHand[currentPos]) = (myHand[currentPos], myHand[i]);

            Image imageA = spawnedCardObjects[i].GetComponentInChildren<Image>();
            Image imageB = spawnedCardObjects[currentPos].GetComponentInChildren<Image>();
            if (imageA != null && imageB != null)
            {
                Sprite tmp = imageA.sprite;
                imageA.sprite = imageB.sprite;
                imageB.sprite = tmp;
            }

            yield return new WaitForSeconds(0.2f);
        }

        if (layoutGroup != null) layoutGroup.enabled = true;
        if (handController != null)
            handController.SetupHand(spawnedCardObjects, this);
    }

    private void RearrangeRemainingHand()
    {
        HorizontalLayoutGroup layout = handArea.GetComponent<HorizontalLayoutGroup>();
        if (layout != null) layout.enabled = false;

        float spacing = layout != null ? layout.spacing : -30f;
        float totalW = (cardWidth * spawnedCardObjects.Count) + spacing * Mathf.Max(0, spawnedCardObjects.Count - 1);
        float startX = -totalW / 2f + cardWidth / 2f;

        for (int i = 0; i < spawnedCardObjects.Count; i++)
        {
            RectTransform r = spawnedCardObjects[i].GetComponent<RectTransform>();
            if (r != null)
                r.anchoredPosition = new Vector2(startX + i * (cardWidth + spacing), 0f);
        }

        if (layout != null) layout.enabled = true;
    }

    private void StartTurnLoop()
    {
        if (isClearingField) return;

        if (activePlayerIndex == 0)
        {
            isWaitingForPlayerInput = true;
            if (handController != null)
                handController.SetupHand(spawnedCardObjects, this);
        }
        else
        {
            isWaitingForPlayerInput = false;
            if (npcController != null)
                Invoke(nameof(CallNpcAI), Random.Range(2f, 4f));
            else
                Debug.LogError("npcController が null です");
        }
    }

    private void CallNpcAI()
    {
        if (npcController == null) return;
        if (isClearingField) return;
        if (activePlayerIndex < 0 || activePlayerIndex >= playerHands.Count) return;
        npcController.ThinkAndPlay(activePlayerIndex, playerHands[activePlayerIndex]);
    }

    public void NextTurn()
    {
        if (isClearingField) return;
        activePlayerIndex = (activePlayerIndex + 1) % playerHands.Count;
        StartTurnLoop();
    }

    public void ProcessPass()
    {
        if (isWaitingForSevenGive) return;
        if (isClearingField) return;
        if (activePlayerIndex == 0 && !isWaitingForPlayerInput) return;

        string name = activePlayerIndex == 0 ? "あなた" : $"NPC{activePlayerIndex + 1}";
        string message = $"{name}はパスした";
        AddRoundAction(message);
        Debug.Log(message);

        if (activePlayerIndex == 0)
            isWaitingForPlayerInput = false;

        consecutivePassCount++;
        if (consecutivePassCount >= playerHands.Count - 1)
            ClearFieldBecauseAllPassed();

        NextTurn();
    }

    public bool CheckAndPlaySelectedObjects(List<GameObject> selectedObjects)
    {
        if (isWaitingForSevenGive) return false;
        if (isClearingField) return false;
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

        if (!TryValidatePlay(selectedCards, out string playType, out string error))
        {
            Debug.LogWarning($"ルール違反: {error}");
            return false;
        }

        PlaceCardsOnField(selectedCards);

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

        RearrangeRemainingHand();

        bool wasFieldSingleJoker = fieldIsSingleJoker;
        UpdateFieldState(selectedCards, playType, 0);
        bool playAgain = ApplySpecialEffects(selectedCards, playType, 0, wasFieldSingleJoker);

        if (isWaitingForSevenGive)
            return true;

        if (playAgain)
        {
            isWaitingForPlayerInput = false;
            return true;
        }

        isWaitingForPlayerInput = false;
        NextTurn();
        return true;
    }

    public bool TryGiveCardsForSeven(List<GameObject> selectedObjects)
    {
        if (!isWaitingForSevenGive) return false;

        if (selectedObjects == null || selectedObjects.Count != sevenGiveCount)
        {
            Debug.LogWarning($"7渡し {sevenGiveCount} 枚選んでください（今 {selectedObjects?.Count ?? 0} 枚）");
            return false;
        }

        List<Card> myHand = playerHands[0];
        List<Card> giveCards = new List<Card>();

        foreach (GameObject obj in selectedObjects)
        {
            int idx = spawnedCardObjects.IndexOf(obj);
            if (idx != -1)
                giveCards.Add(spawnedCardDatas[idx]);
        }

        if (giveCards.Count != sevenGiveCount)
        {
            Debug.LogWarning("7渡しに失敗しました");
            return false;
        }

        List<Card> toHand = playerHands[sevenGiveToIndex];
        foreach (Card c in giveCards)
        {
            myHand.Remove(c);
            toHand.Add(c);
        }

        foreach (GameObject obj in selectedObjects)
        {
            int idx = spawnedCardObjects.IndexOf(obj);
            if (idx != -1)
            {
                spawnedCardObjects.RemoveAt(idx);
                spawnedCardDatas.RemoveAt(idx);
                Destroy(obj);
            }
        }

        RearrangeRemainingHand();

        string toName = sevenGiveToIndex == 0 ? "あなた" : $"NPC{sevenGiveToIndex + 1}";
        Debug.Log($"{toName} に {sevenGiveCount}枚渡した");
        Debug.Log($"{toName} の手札: {validator.FormatCards(toHand)}");

        isWaitingForSevenGive = false;
        sevenGiveCount = 0;
        isWaitingForPlayerInput = false;

        if (handController != null)
            handController.SetupHand(spawnedCardObjects, this);

        NextTurn();
        return true;
    }

    public void PlayCpuCards(int cpuIndex, List<Card> selectedCards)
    {
        if (selectedCards == null || selectedCards.Count == 0)
        {
            NextTurn();
            return;
        }

        if (!TryValidatePlay(selectedCards, out string playType, out _))
        {
            ProcessPass();
            return;
        }

        PlaceCardsOnField(selectedCards);

        foreach (Card card in selectedCards)
            playerHands[cpuIndex].Remove(card);

        bool wasFieldSingleJoker = fieldIsSingleJoker;
        UpdateFieldState(selectedCards, playType, cpuIndex);
        bool playAgain = ApplySpecialEffects(selectedCards, playType, cpuIndex, wasFieldSingleJoker);

        if (playAgain)
            return;

        NextTurn();
    }

    private void PlaceCardsOnField(List<Card> cards)
    {
        ClearAllFieldSlots();

        for (int i = 0; i < cards.Count && i < fieldSlots.Length; i++)
        {
            if (fieldSlots[i] == null) continue;

            GameObject fieldCard = Instantiate(cardPrefab, fieldSlots[i]);
            RectTransform rt = fieldCard.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(cardWidth, cardHeight);
                rt.localScale = new Vector3(1f, fieldCardScaleY, 1f);
            }

            Image img = fieldCard.GetComponentInChildren<Image>();
            if (img != null)
            {
                int spriteIndex = cards[i].id - 1;
                if (spriteIndex >= 0 && spriteIndex < cardSprites.Length && cardSprites[spriteIndex] != null)
                    img.sprite = cardSprites[spriteIndex];
            }
        }
    }

    private void UpdateFieldState(List<Card> cards, string playType, int playerIndex)
    {
        Card.SuitType? playSuit = validator.GetCommonSuit(cards);

        if (currentFieldCardCount > 0 && playSuit.HasValue && lastPlaySuit.HasValue
            && playSuit.Value == lastPlaySuit.Value)
        {
            lockedSuit = playSuit;
            Debug.Log($"縛り{lockedSuit} のみ出せます");
        }

        currentFieldCardCount = cards.Count;
        currentFieldCardStrength = validator.GetPlayStrength(cards);
        currentFieldPlayType = playType;
        consecutivePassCount = 0;
        lastPlayPlayerIndex = playerIndex;
        lastPlaySuit = playSuit;

        if (playType == "階段")
        {
            validator.GetKaidanRange(cards, out currentFieldKaidanMin, out currentFieldKaidanMax);
            currentFieldCardStrength = currentFieldKaidanMax;
        }
        else
        {
            currentFieldKaidanMin = 0;
            currentFieldKaidanMax = 0;
        }

        fieldIsSingleJoker = (cards.Count == 1 && cards[0].suit == Card.SuitType.Joker);

        string name = playerIndex == 0 ? "あなた" : $"NPC{playerIndex + 1}";
        string message = $"{name}は{validator.FormatCards(cards)}を出した（{playType}）";
        AddRoundAction(message);

        if (playerIndex == 0)
            Debug.Log(message);
    }

    private bool ApplySpecialEffects(List<Card> cards, string playType, int playerIndex, bool wasFieldSingleJoker = false)
    {
        if (wasFieldSingleJoker
            && cards.Count == 1
            && cards[0].suit == Card.SuitType.Spade
            && cards[0].number == 3)
        {
            StartCoroutine(DelayedClearFieldAndContinue(playerIndex));
            return true;
        }

        if (playType == "4枚" && validator.IsSameNumberGroup(cards))
        {
            isRevolution = !isRevolution;
            Debug.Log($"4枚革命現在: {(isRevolution ? "革命中" : "通常")}");
        }

        if (playType == "階段" && cards.Count >= 4)
        {
            isRevolution = !isRevolution;
            Debug.Log($"階段革命現在: {(isRevolution ? "革命中" : "通常")}");
        }

        if (validator.ContainsNumber(cards, 8))
        {
            StartCoroutine(DelayedClearFieldAndContinue(playerIndex));
            return true;
        }

        int sevenCount = validator.CountSevens(cards);
        if (sevenCount <= 0) return false;

        int canGive = Mathf.Min(sevenCount, playerHands[playerIndex].Count);
        if (canGive <= 0) return false;

        int toIndex = (playerIndex + 1) % playerHands.Count;

        if (playerIndex == 0)
        {
            isWaitingForSevenGive = true;
            sevenGiveCount = canGive;
            sevenGiveToIndex = toIndex;
            isWaitingForPlayerInput = true;
            Debug.Log($"7渡し {canGive} 枚選んで Enter");
            return false;
        }

        TransferWeakestCards(playerIndex, toIndex, canGive);
        return false;
    }

    private System.Collections.IEnumerator DelayedClearFieldAndContinue(int playerIndex)
    {
        isClearingField = true;
        isWaitingForPlayerInput = false;

        yield return new WaitForSeconds(2f);

        ClearFieldAfterSpecial();
        isClearingField = false;

        if (playerIndex == 0)
        {
            activePlayerIndex = 0;
            isWaitingForPlayerInput = true;
            if (handController != null)
                handController.SetupHand(spawnedCardObjects, this);
            Debug.Log("場が流れた");
        }
        else
        {
            activePlayerIndex = playerIndex;
            if (playerIndex >= 0 && playerIndex < playerHands.Count && playerHands[playerIndex].Count > 0)
            {
                Debug.Log($"場が流れた。NPC{playerIndex + 1}の番");
                Invoke(nameof(CallNpcAI), Random.Range(1.0f, 2.0f));
            }
            else
            {
                NextTurn();
            }
        }
    }

    private void TransferWeakestCards(int fromIndex, int toIndex, int count)
    {
        List<Card> fromHand = playerHands[fromIndex];
        List<Card> toHand = playerHands[toIndex];
        count = Mathf.Min(count, fromHand.Count);
        if (count <= 0) return;

        List<Card> sorted = new List<Card>(fromHand);
        sorted.Sort((a, b) =>
            validator.GetCardStrengthValue(a).CompareTo(validator.GetCardStrengthValue(b)));

        List<Card> givenCards = new List<Card>();
        for (int i = 0; i < count; i++)
        {
            Card give = sorted[i];
            fromHand.Remove(give);
            toHand.Add(give);
            givenCards.Add(give);
        }

        string fromName = fromIndex == 0 ? "あなた" : $"NPC{fromIndex + 1}";
        string toName = toIndex == 0 ? "あなた" : $"NPC{toIndex + 1}";
        Debug.Log($"7渡し {fromName} が {toName} に {count}枚渡した");
        Debug.Log($" {toName} の手札: {validator.FormatCards(toHand)}");

        if (toIndex == 0)
            AddReceivedCardsToPlayerHand(givenCards);
    }

    private void AddReceivedCardsToPlayerHand(List<Card> receivedCards)
    {
        foreach (Card card in receivedCards)
        {
            GameObject newCard = Instantiate(cardPrefab, handArea);
            spawnedCardObjects.Add(newCard);
            spawnedCardDatas.Add(card);

            Image cardImage = newCard.GetComponentInChildren<Image>();
            if (cardImage != null)
            {
                int spriteIndex = card.id - 1;
                if (spriteIndex >= 0 && spriteIndex < cardSprites.Length && cardSprites[spriteIndex] != null)
                    cardImage.sprite = cardSprites[spriteIndex];
            }

            RectTransform rect = newCard.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(cardWidth, cardHeight);
                if (cardPrefab != null)
                {
                    RectTransform prefabRect = cardPrefab.GetComponent<RectTransform>();
                    if (prefabRect != null)
                        rect.localScale = prefabRect.localScale;
                }
            }
        }

        SortPlayerHandByStrength();
    }

    private void SortPlayerHandByStrength()
    {
        List<Card> myHand = playerHands[0];

        int pairCount = Mathf.Min(spawnedCardDatas.Count, spawnedCardObjects.Count);
        List<(Card data, GameObject obj)> pairs = new List<(Card, GameObject)>();
        for (int i = 0; i < pairCount; i++)
        {
            if (spawnedCardObjects[i] == null) continue;
            pairs.Add((spawnedCardDatas[i], spawnedCardObjects[i]));
        }

        if (isRevolution)
            pairs.Sort((a, b) => validator.GetCardStrengthValue(a.data).CompareTo(validator.GetCardStrengthValue(b.data)));
        else
            pairs.Sort((a, b) => validator.GetCardStrengthValue(b.data).CompareTo(validator.GetCardStrengthValue(a.data)));

        spawnedCardDatas.Clear();
        spawnedCardObjects.Clear();
        myHand.Clear();

        foreach (var p in pairs)
        {
            spawnedCardDatas.Add(p.data);
            spawnedCardObjects.Add(p.obj);
            myHand.Add(p.data);
            p.obj.transform.SetParent(handArea, false);
            p.obj.transform.SetAsLastSibling();
        }

        RearrangeRemainingHand();

        if (handController != null)
            handController.SetupHand(spawnedCardObjects, this);
    }

    private void ClearAllFieldSlots()
    {
        foreach (Transform slot in fieldSlots)
        {
            if (slot == null) continue;
            foreach (Transform child in slot)
                Destroy(child.gameObject);
        }
    }

    private void ClearFieldAfterSpecial()
    {
        ClearAllFieldSlots();
        currentFieldCardCount = 0;
        currentFieldCardStrength = 0;
        currentFieldPlayType = "";
        currentFieldKaidanMin = 0;
        currentFieldKaidanMax = 0;
        consecutivePassCount = 0;
        lockedSuit = null;
        lastPlaySuit = null;
        fieldIsSingleJoker = false;
    }

    private void ClearFieldBecauseAllPassed()
    {
        string summary = roundActionLog.Count == 0
            ? "（行動なし）"
            : string.Join(" / ", roundActionLog);

        Debug.Log($"場が流れた{summary}");

        ClearAllFieldSlots();
        currentFieldCardCount = 0;
        currentFieldCardStrength = 0;
        currentFieldPlayType = "";
        currentFieldKaidanMin = 0;
        currentFieldKaidanMax = 0;
        consecutivePassCount = 0;
        lastPlayPlayerIndex = -1;
        lockedSuit = null;
        lastPlaySuit = null;
        fieldIsSingleJoker = false;
        roundActionLog.Clear();
    }

    private void AddRoundAction(string message)
    {
        roundActionLog.Add(message);
    }
}