using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform handArea;
    [SerializeField] private Sprite[] cardSprites;
    [SerializeField] private PlayerHandController handController;

    [Header("NPC演出")]
    [SerializeField] private NPCThinkingEffect thinkingEffect;

    [Header("NPC手札の裏表示")]
    [SerializeField] private NpcHandView npcHandView;

    [Header("対戦情報UI")]
    [SerializeField] private BattleInfoPanel battleInfoPanel;

    [Header("勝利演出")]
    [SerializeField] private VictoryPanel victoryPanel;

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

    [Header("プレイヤー上がり後の高速プレイ")]
    [SerializeField] private float fastPlayDelay = 0.1f;

    private readonly PlayValidator validator = new PlayValidator();

    private List<Card> deck = new List<Card>();
    private List<List<Card>> playerHands = new List<List<Card>>();
    private List<string> roundActionLog = new List<string>();
    private List<GameObject> spawnedCardObjects = new List<GameObject>();
    private List<Card> spawnedCardDatas = new List<Card>();
    private List<Card> playedHistory = new List<Card>();
    private List<Card> currentFieldCards = new List<Card>();

    private HashSet<Card.SuitType> lockedSuits = new HashSet<Card.SuitType>();
    private List<Card.SuitType> lockedSuitOrder = new List<Card.SuitType>();

    private int consecutivePassCount;
    private int lastPlayPlayerIndex = -1;
    private int currentFieldCardCount;
    private int currentFieldCardStrength;
    private bool isRevolution;

    private Card.SuitType? lastPlaySuit;
    private string currentFieldPlayType = "";
    private bool fieldIsSingleJoker;
    private int currentFieldKaidanMin;
    private int currentFieldKaidanMax;

    private int activePlayerIndex;
    private bool isWaitingForPlayerInput;
    private bool isWaitingForSevenGive;
    private int sevenGiveCount;
    private int sevenGiveToIndex;
    private bool isClearingField;

    private bool[] hasFinished;
    private List<int> finishOrder = new List<int>();
    private bool isResolvingRanks;
    private bool gameEnded;

    public int GetCurrentFieldCardCount() => currentFieldCardCount;
    public int GetCurrentFieldCardStrength() => currentFieldCardStrength;
    public bool IsRevolution() => isRevolution;
    public bool IsWaitingForSevenGive() => isWaitingForSevenGive;
    public int GetSevenGiveCount() => sevenGiveCount;
    public bool IsFieldSingleJoker() => fieldIsSingleJoker;
    public string GetCurrentFieldPlayType() => currentFieldPlayType;
    public int GetCurrentFieldKaidanMin() => currentFieldKaidanMin;
    public int GetCurrentFieldKaidanMax() => currentFieldKaidanMax;
    public Card.SuitType? GetLastPlaySuit() => lastPlaySuit;
    public IReadOnlyList<Card> GetPlayedHistory() => playedHistory;
    public int GetPlayerCount() => playerHands.Count;

    public bool IsSuitLocked() => lockedSuits != null && lockedSuits.Count > 0;

    public bool IsSuitAllowed(Card.SuitType suit)
    {
        if (lockedSuits == null || lockedSuits.Count == 0) return true;
        if (suit == Card.SuitType.Joker) return true;
        return lockedSuits.Contains(suit);
    }

    public Card.SuitType? GetLockedSuit()
    {
        if (lockedSuitOrder != null && lockedSuitOrder.Count > 0)
            return lockedSuitOrder[0];
        if (lockedSuits == null || lockedSuits.Count == 0) return null;
        foreach (var s in lockedSuits) return s;
        return null;
    }

    public List<Card.SuitType> GetLockedSuitList()
    {
        return new List<Card.SuitType>(lockedSuitOrder);
    }

    public List<Card> GetCurrentFieldCards()
    {
        return new List<Card>(currentFieldCards);
    }

    public int GetHandCount(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerHands.Count) return 0;
        return playerHands[playerIndex].Count;
    }

    public bool HasPlayerFinished(int playerIndex)
    {
        if (hasFinished == null || playerIndex < 0 || playerIndex >= hasFinished.Length)
            return false;
        return hasFinished[playerIndex];
    }

    public bool IsMyTurn()
    {
        return activePlayerIndex == 0
            && isWaitingForPlayerInput
            && !isWaitingForSevenGive
            && !isClearingField
            && !gameEnded
            && !isResolvingRanks;
    }

    public List<Card> GetPlayerHandData(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < playerHands.Count)
            return playerHands[playerIndex];
        return null;
    }

    public bool TryValidatePlay(List<Card> cards, out string playType, out string error)
    {
        Card.SuitType? suitForValidator = null;
        if (lockedSuits != null && lockedSuits.Count == 1)
            suitForValidator = GetLockedSuit();

        if (!validator.TryValidate(
            cards,
            currentFieldCardCount,
            currentFieldCardStrength,
            currentFieldPlayType,
            isRevolution,
            fieldIsSingleJoker,
            suitForValidator,
            currentFieldKaidanMin,
            currentFieldKaidanMax,
            out playType,
            out error))
        {
            return false;
        }

        if (IsSuitLocked())
        {
            foreach (Card c in cards)
            {
                if (c.suit == Card.SuitType.Joker) continue;
                if (!IsSuitAllowed(c.suit))
                {
                    error = "縛り";
                    playType = "";
                    return false;
                }
            }
        }

        return true;
    }

    void Start()
    {
        StartGame(4);
    }

    public void StartGame(int playerCount)
    {
        playedHistory.Clear();
        roundActionLog.Clear();
        consecutivePassCount = 0;
        lastPlayPlayerIndex = -1;
        spawnedCardObjects.Clear();
        spawnedCardDatas.Clear();
        currentFieldCards.Clear();
        lockedSuits.Clear();
        lockedSuitOrder.Clear();
        finishOrder.Clear();

        currentFieldCardCount = 0;
        currentFieldCardStrength = 0;
        currentFieldPlayType = "";
        currentFieldKaidanMin = 0;
        currentFieldKaidanMax = 0;
        isRevolution = false;
        lastPlaySuit = null;
        fieldIsSingleJoker = false;

        isWaitingForSevenGive = false;
        sevenGiveCount = 0;
        isClearingField = false;
        activePlayerIndex = 0;
        isWaitingForPlayerInput = false;
        isResolvingRanks = false;
        gameEnded = false;

        hasFinished = new bool[playerCount];

        ClearAllFieldSlots();
        CreateAdvancedDeck();
        ShuffleDeck();

        playerHands.Clear();
        for (int i = 0; i < playerCount; i++)
            playerHands.Add(new List<Card>());

        DistributeCards(playerCount);
        DisplayMyHand();
        RefreshBattleInfoUI();
    }

    private string PlayerName(int index)
    {
        if (index == 0) return "あなた";
        return $"NPC{index}";
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

        int[] dealCounts = new int[playerHands.Count];
        int[] finalCounts = new int[playerHands.Count];
        for (int i = 0; i < playerHands.Count; i++)
        {
            dealCounts[i] = 0;
            finalCounts[i] = playerHands[i].Count;
        }

        if (battleInfoPanel != null)
            battleInfoPanel.SetHandCounts(dealCounts);

        int playerDealt = 0;
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

            playerDealt++;
            dealCounts[0] = playerDealt;
            float ratio = myHand.Count > 0 ? (float)playerDealt / myHand.Count : 1f;
            for (int p = 1; p < dealCounts.Length; p++)
                dealCounts[p] = Mathf.RoundToInt(finalCounts[p] * ratio);

            if (battleInfoPanel != null)
                battleInfoPanel.SetHandCounts(dealCounts);
            if (npcHandView != null)
                npcHandView.SetCountsFromPlayerHands(dealCounts);

            yield return new WaitForSeconds(0.04f);
        }

        if (battleInfoPanel != null)
            battleInfoPanel.SetHandCounts(finalCounts);

        RefreshNpcHandViews();

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
        float totalWidth = (cardWidth * spawnedCardObjects.Count)
            + (spacing * Mathf.Max(0, spawnedCardObjects.Count - 1));
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

            (spawnedCardDatas[i], spawnedCardDatas[currentPos]) =
                (spawnedCardDatas[currentPos], spawnedCardDatas[i]);
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
        float totalW = (cardWidth * spawnedCardObjects.Count)
            + spacing * Mathf.Max(0, spawnedCardObjects.Count - 1);
        float startX = -totalW / 2f + cardWidth / 2f;

        for (int i = 0; i < spawnedCardObjects.Count; i++)
        {
            RectTransform r = spawnedCardObjects[i].GetComponent<RectTransform>();
            if (r != null)
                r.anchoredPosition = new Vector2(startX + i * (cardWidth + spacing), 0f);
        }

        if (layout != null) layout.enabled = true;
    }

    private void RefreshNpcHandViews()
    {
        if (npcHandView == null) return;

        int[] counts = new int[playerHands.Count];
        for (int i = 0; i < playerHands.Count; i++)
            counts[i] = HasPlayerFinished(i) ? 0 : playerHands[i].Count;

        npcHandView.SetCountsFromPlayerHands(counts);
    }

    private void RefreshHandCountsOnly()
    {
        if (battleInfoPanel != null)
        {
            int[] counts = new int[playerHands.Count];
            for (int i = 0; i < playerHands.Count; i++)
                counts[i] = HasPlayerFinished(i) ? 0 : playerHands[i].Count;
            battleInfoPanel.SetHandCounts(counts);
        }
        RefreshNpcHandViews();
    }

    private void RefreshBattleInfoUI()
    {
        if (battleInfoPanel != null)
            battleInfoPanel.Refresh(isRevolution, GetLockedSuitList());

        RefreshHandCountsOnly();
    }

    private void StartTurnLoop()
    {
        if (gameEnded || isResolvingRanks || isClearingField) return;

        if (HasPlayerFinished(activePlayerIndex))
        {
            if (thinkingEffect != null)
                thinkingEffect.HideAll();
            if (npcHandView != null)
                npcHandView.SetThinkingPlayer(0);
            NextTurn();
            return;
        }

        if (thinkingEffect != null)
            thinkingEffect.SetThinkingPlayer(activePlayerIndex);

        if (npcHandView != null)
            npcHandView.SetThinkingPlayer(activePlayerIndex);

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
        if (gameEnded || isResolvingRanks) return;
        if (npcController == null) return;
        if (isClearingField) return;
        if (activePlayerIndex < 0 || activePlayerIndex >= playerHands.Count) return;

        if (HasPlayerFinished(activePlayerIndex))
        {
            if (thinkingEffect != null)
                thinkingEffect.HideAll();
            if (npcHandView != null)
                npcHandView.SetThinkingPlayer(0);
            NextTurn();
            return;
        }

        npcController.ThinkAndPlay(activePlayerIndex, playerHands[activePlayerIndex]);
    }

    public void NextTurn()
    {
        if (gameEnded || isResolvingRanks || isClearingField) return;

        int count = playerHands.Count;
        for (int i = 0; i < count; i++)
        {
            activePlayerIndex = (activePlayerIndex + 1) % count;
            if (!HasPlayerFinished(activePlayerIndex))
                break;
        }

        StartTurnLoop();
    }

    public void ProcessPass()
    {
        if (gameEnded || isResolvingRanks) return;
        if (isWaitingForSevenGive) return;
        if (isClearingField) return;
        if (activePlayerIndex == 0 && !isWaitingForPlayerInput) return;

        string message = $"{PlayerName(activePlayerIndex)}はパスした";
        AddRoundAction(message);
        Debug.Log(message);

        if (activePlayerIndex == 0)
            isWaitingForPlayerInput = false;

        consecutivePassCount++;
        int alive = CountAlivePlayers();
        if (alive > 1 && consecutivePassCount >= alive - 1)
            ClearFieldBecauseAllPassed();

        NextTurn();
    }

    private int CountAlivePlayers()
    {
        int n = 0;
        for (int i = 0; i < playerHands.Count; i++)
        {
            if (!HasPlayerFinished(i))
                n++;
        }
        return n;
    }

    public bool CheckAndPlaySelectedObjects(List<GameObject> selectedObjects)
    {
        if (gameEnded || isResolvingRanks) return false;
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

        CheckFinish(0);
        RefreshHandCountsOnly();

        if (gameEnded || isResolvingRanks)
            return true;

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
        Debug.Log($"{PlayerName(sevenGiveToIndex)} に {sevenGiveCount}枚渡した");

        isWaitingForSevenGive = false;
        sevenGiveCount = 0;
        isWaitingForPlayerInput = false;

        if (handController != null)
            handController.SetupHand(spawnedCardObjects, this);

        CheckFinish(0);
        RefreshHandCountsOnly();
        NextTurn();
        return true;
    }

    public void PlayCpuCards(int cpuIndex, List<Card> selectedCards)
    {
        if (gameEnded) return;

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

        CheckFinish(cpuIndex);
        RefreshHandCountsOnly();

        if (HasPlayerFinished(cpuIndex) && thinkingEffect != null)
            thinkingEffect.HideAll();

        if (gameEnded || isResolvingRanks)
            return;

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
        List<Card> previousField = new List<Card>(currentFieldCards);

        if (previousField.Count > 0)
        {
            if (previousField.Count == 2 && cards.Count == 2)
            {
                if (IsTwoCardShibariMatch(previousField, cards))
                {
                    lockedSuits.Clear();
                    lockedSuitOrder.Clear();
                    foreach (Card c in previousField)
                    {
                        if (c.suit == Card.SuitType.Joker) continue;
                        if (lockedSuits.Add(c.suit))
                            lockedSuitOrder.Add(c.suit);
                    }
                    Debug.Log($"2枚縛り発動: {string.Join(",", lockedSuitOrder)}");
                }
            }
            else if (previousField.Count == 1 && cards.Count == 1)
            {
                if (previousField[0].suit != Card.SuitType.Joker
                    && cards[0].suit != Card.SuitType.Joker
                    && previousField[0].suit == cards[0].suit)
                {
                    lockedSuits.Clear();
                    lockedSuitOrder.Clear();
                    lockedSuits.Add(cards[0].suit);
                    lockedSuitOrder.Add(cards[0].suit);
                    Debug.Log($"1枚縛り発動: {cards[0].suit}");
                }
            }
        }

        currentFieldCards = new List<Card>(cards);
        currentFieldCardCount = cards.Count;
        currentFieldCardStrength = validator.GetPlayStrength(cards);
        currentFieldPlayType = playType;
        consecutivePassCount = 0;
        lastPlayPlayerIndex = playerIndex;
        lastPlaySuit = validator.GetCommonSuit(cards);

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

        fieldIsSingleJoker = cards.Count == 1 && cards[0].suit == Card.SuitType.Joker;

        string message = $"{PlayerName(playerIndex)}は{validator.FormatCards(cards)}を出した（{playType})";
        AddRoundAction(message);
        if (playerIndex == 0)
            Debug.Log(message);

        foreach (Card c in cards)
            playedHistory.Add(c);

        RefreshBattleInfoUI();
    }

    private bool IsTwoCardShibariMatch(List<Card> field, List<Card> play)
    {
        if (field == null || play == null || field.Count != 2 || play.Count != 2)
            return false;

        var fieldSuits = new List<Card.SuitType>();
        foreach (Card c in field)
        {
            if (c.suit != Card.SuitType.Joker)
                fieldSuits.Add(c.suit);
        }
        if (fieldSuits.Count == 0) return false;

        var playSuits = new List<Card.SuitType>();
        int playJokers = 0;
        foreach (Card c in play)
        {
            if (c.suit == Card.SuitType.Joker) playJokers++;
            else playSuits.Add(c.suit);
        }

        foreach (var s in playSuits)
        {
            if (!fieldSuits.Contains(s))
                return false;
        }

        var remain = new List<Card.SuitType>(fieldSuits);
        foreach (var s in playSuits)
        {
            int idx = remain.IndexOf(s);
            if (idx < 0) return false;
            remain.RemoveAt(idx);
        }

        return remain.Count <= playJokers;
    }

    private bool ApplySpecialEffects(List<Card> cards, string playType, int playerIndex, bool wasFieldSingleJoker)
    {
        if (wasFieldSingleJoker
            && cards.Count == 1
            && cards[0].suit == Card.SuitType.Spade
            && cards[0].number == 3)
        {
            if (!isResolvingRanks)
                StartCoroutine(DelayedClearFieldAndContinue(playerIndex));
            else
                ClearFieldAfterSpecial();
            return !isResolvingRanks;
        }

        if (playType == "4枚" && validator.IsSameNumberGroup(cards))
        {
            isRevolution = !isRevolution;
            Debug.Log($"革命 現在: {(isRevolution ? "革命中" : "通常")}");
            RefreshBattleInfoUI();
        }

        if (playType == "階段" && cards.Count >= 4)
        {
            isRevolution = !isRevolution;
            Debug.Log($"階段革命 現在: {(isRevolution ? "革命中" : "通常")}");
            RefreshBattleInfoUI();
        }

        if (validator.ContainsNumber(cards, 8))
        {
            if (!isResolvingRanks)
                StartCoroutine(DelayedClearFieldAndContinue(playerIndex));
            else
                ClearFieldAfterSpecial();
            return !isResolvingRanks;
        }

        int sevenCount = validator.CountSevens(cards);
        if (sevenCount <= 0) return false;

        int canGive = Mathf.Min(sevenCount, playerHands[playerIndex].Count);
        if (canGive <= 0) return false;

        int toIndex = FindNextAliveIndex(playerIndex);
        if (isResolvingRanks)
        {
            TransferWeakestCards(playerIndex, toIndex, canGive);
            return false;
        }

        if (playerIndex == 0)
        {
            isWaitingForSevenGive = true;
            sevenGiveCount = canGive;
            sevenGiveToIndex = toIndex;
            isWaitingForPlayerInput = true;
            Debug.Log($"7渡し {canGive} 枚選んで Enter");
            if (handController != null)
                handController.FocusArrowOnHandCenter();
            return false;
        }

        TransferWeakestCards(playerIndex, toIndex, canGive);
        return false;
    }

    private int FindNextAliveIndex(int fromIndex)
    {
        int count = playerHands.Count;
        for (int i = 1; i <= count; i++)
        {
            int idx = (fromIndex + i) % count;
            if (!HasPlayerFinished(idx))
                return idx;
        }
        return (fromIndex + 1) % count;
    }

    private System.Collections.IEnumerator DelayedClearFieldAndContinue(int playerIndex)
    {
        isClearingField = true;
        isWaitingForPlayerInput = false;

        if (thinkingEffect != null)
            thinkingEffect.HideAll();
        if (npcHandView != null)
            npcHandView.SetThinkingPlayer(0);

        yield return new WaitForSeconds(2f);

        ClearFieldAfterSpecial();
        isClearingField = false;

        if (gameEnded || isResolvingRanks)
            yield break;

        if (HasPlayerFinished(playerIndex))
        {
            NextTurn();
            yield break;
        }

        if (playerIndex == 0)
        {
            activePlayerIndex = 0;
            isWaitingForPlayerInput = true;
            if (handController != null)
                handController.SetupHand(spawnedCardObjects, this);
            if (thinkingEffect != null)
                thinkingEffect.SetThinkingPlayer(0);
            if (npcHandView != null)
                npcHandView.SetThinkingPlayer(0);
            Debug.Log("場が流れた");
        }
        else
        {
            activePlayerIndex = playerIndex;
            if (thinkingEffect != null)
                thinkingEffect.SetThinkingPlayer(playerIndex);
            if (npcHandView != null)
                npcHandView.SetThinkingPlayer(playerIndex);
            Debug.Log($"場が流れた。{PlayerName(playerIndex)}の番");
            Invoke(nameof(CallNpcAI), Random.Range(1.0f, 2.0f));
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

        Debug.Log($"7渡し {PlayerName(fromIndex)} → {PlayerName(toIndex)} に {count}枚");

        if (toIndex == 0 && !isResolvingRanks)
            AddReceivedCardsToPlayerHand(givenCards);

        RefreshHandCountsOnly();
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
                RectTransform prefabRect = cardPrefab.GetComponent<RectTransform>();
                if (prefabRect != null)
                    rect.localScale = prefabRect.localScale;
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
            pairs.Sort((a, b) => validator.GetCardStrengthValue(a.data)
                .CompareTo(validator.GetCardStrengthValue(b.data)));
        else
            pairs.Sort((a, b) => validator.GetCardStrengthValue(b.data)
                .CompareTo(validator.GetCardStrengthValue(a.data)));

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
        lastPlaySuit = null;
        fieldIsSingleJoker = false;
        lockedSuits.Clear();
        lockedSuitOrder.Clear();
        currentFieldCards.Clear();
        RefreshBattleInfoUI();
    }

    private void ClearFieldBecauseAllPassed()
    {
        string summary = roundActionLog.Count == 0
            ? "（行動なし）"
            : string.Join(" / ", roundActionLog);

        Debug.Log($"場が流れた {summary}");

        ClearFieldAfterSpecial();
        lastPlayPlayerIndex = -1;
        roundActionLog.Clear();
    }

    private void AddRoundAction(string message)
    {
        roundActionLog.Add(message);
    }

    private void CheckFinish(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerHands.Count) return;
        if (HasPlayerFinished(playerIndex)) return;
        if (playerHands[playerIndex].Count > 0) return;

        hasFinished[playerIndex] = true;
        if (!finishOrder.Contains(playerIndex))
            finishOrder.Add(playerIndex);

        Debug.Log($"{PlayerName(playerIndex)}が上がった（{finishOrder.Count}位）");

        if (thinkingEffect != null && activePlayerIndex == playerIndex)
            thinkingEffect.HideAll();
        if (npcHandView != null && activePlayerIndex == playerIndex)
            npcHandView.SetThinkingPlayer(0);

        RefreshHandCountsOnly();
        if (finishOrder.Count >= playerHands.Count - 1)
        {
            for (int i = 0; i < playerHands.Count; i++)
            {
                if (!finishOrder.Contains(i))
                {
                    hasFinished[i] = true;
                    finishOrder.Add(i);
                    Debug.Log($"{PlayerName(i)}は最下位");
                }
            }
            EndGameShowVictory();
            return;
        }

        if (finishOrder.Count >= playerHands.Count)
        {
            EndGameShowVictory();
            return;
        }
        if (playerIndex == 0 && finishOrder.Count == 1 && !isResolvingRanks && !gameEnded)
            StartCoroutine(ResolveRemainingRanksFast());
    }

    private System.Collections.IEnumerator ResolveRemainingRanksFast()
    {
        isResolvingRanks = true;
        isWaitingForPlayerInput = false;
        CancelInvoke(nameof(CallNpcAI));

        if (thinkingEffect != null)
            thinkingEffect.HideAll();
        if (npcHandView != null)
            npcHandView.SetThinkingPlayer(0);

        int safety = 0;
        const int maxSteps = 400;

        while (CountAlivePlayers() > 0 && safety < maxSteps && !gameEnded)
        {
            safety++;

            int tries = 0;
            while (HasPlayerFinished(activePlayerIndex) && tries < playerHands.Count)
            {
                activePlayerIndex = (activePlayerIndex + 1) % playerHands.Count;
                tries++;
            }

            if (CountAlivePlayers() <= 0)
                break;

            if (activePlayerIndex == 0)
            {
                if (HasPlayerFinished(0))
                {
                    activePlayerIndex = (activePlayerIndex + 1) % playerHands.Count;
                    continue;
                }
                break;
            }

            List<Card> hand = playerHands[activePlayerIndex];
            if (hand == null || hand.Count == 0)
            {
                CheckFinish(activePlayerIndex);
                yield return null;
                continue;
            }
            yield return new WaitForSeconds(fastPlayDelay);

            if (gameEnded) yield break;

            List<Card> toPlay = null;
            if (npcController != null)
                toPlay = npcController.DecideCards(hand);
            else
                toPlay = FindAnyValidPlay(hand);

            if (toPlay != null && toPlay.Count > 0)
            {
                Debug.Log($"高速({difficultyLog()}): {PlayerName(activePlayerIndex)} → {validator.FormatCards(toPlay)}");
                FastPlayCards(activePlayerIndex, toPlay);
            }
            else
            {
                Debug.Log($"高速: {PlayerName(activePlayerIndex)} はパス");
                FastPass(activePlayerIndex);
            }

            RefreshNpcHandViews();
            RefreshHandCountsOnly();

            if (finishOrder.Count >= playerHands.Count)
                break;
        }

        isResolvingRanks = false;
        EndGameShowVictory();
    }
    private string difficultyLog()
    {
        return "選択レベル";
    }
    private List<Card> FindAnyValidPlay(List<Card> hand)
    {
        if (hand == null || hand.Count == 0) return null;

        List<Card> sorted = new List<Card>(hand);
        sorted.Sort((a, b) =>
            validator.GetCardStrengthValue(a).CompareTo(validator.GetCardStrengthValue(b)));

        foreach (Card c in sorted)
        {
            var trial = new List<Card> { c };
            if (TryValidatePlay(trial, out _, out _))
                return trial;
        }

        for (int i = 0; i < sorted.Count; i++)
        {
            for (int j = i + 1; j < sorted.Count; j++)
            {
                bool okPair = sorted[i].suit == Card.SuitType.Joker
                    || sorted[j].suit == Card.SuitType.Joker
                    || sorted[i].number == sorted[j].number;

                if (!okPair) continue;

                var trial = new List<Card> { sorted[i], sorted[j] };
                if (TryValidatePlay(trial, out _, out _))
                    return trial;
            }
        }

        return null;
    }

    private void FastPlayCards(int cpuIndex, List<Card> selectedCards)
    {
        if (selectedCards == null || selectedCards.Count == 0)
        {
            FastPass(cpuIndex);
            return;
        }

        if (!TryValidatePlay(selectedCards, out string playType, out _))
        {
            FastPass(cpuIndex);
            return;
        }

        PlaceCardsOnField(selectedCards);

        foreach (Card card in selectedCards)
            playerHands[cpuIndex].Remove(card);

        bool wasFieldSingleJoker = fieldIsSingleJoker;
        UpdateFieldState(selectedCards, playType, cpuIndex);
        bool playAgain = ApplySpecialEffects(selectedCards, playType, cpuIndex, wasFieldSingleJoker);

        CheckFinish(cpuIndex);
        RefreshHandCountsOnly();

        if (gameEnded) return;

        if (playAgain)
            return;

        int count = playerHands.Count;
        for (int i = 0; i < count; i++)
        {
            activePlayerIndex = (activePlayerIndex + 1) % count;
            if (!HasPlayerFinished(activePlayerIndex))
                break;
        }
    }

    private void FastPass(int playerIndex)
    {
        string message = $"{PlayerName(playerIndex)}はパスした";
        AddRoundAction(message);

        consecutivePassCount++;
        int alive = CountAlivePlayers();
        if (alive > 1 && consecutivePassCount >= alive - 1)
            ClearFieldBecauseAllPassed();

        int count = playerHands.Count;
        for (int i = 0; i < count; i++)
        {
            activePlayerIndex = (activePlayerIndex + 1) % count;
            if (!HasPlayerFinished(activePlayerIndex))
                break;
        }
    }
    private void EndGameShowVictory()
    {
        if (gameEnded) return;
        gameEnded = true;

        isWaitingForPlayerInput = false;
        isResolvingRanks = false;
        CancelInvoke(nameof(CallNpcAI));

        if (thinkingEffect != null) thinkingEffect.HideAll();
        if (npcHandView != null) npcHandView.SetThinkingPlayer(0);

        for (int i = 0; i < playerHands.Count; i++)
        {
            if (!finishOrder.Contains(i))
                finishOrder.Add(i);
        }

        ClearAllFieldSlots();
        currentFieldCards.Clear();

        int last = finishOrder[finishOrder.Count - 1];
        if (last >= 0 && last < playerHands.Count)
            playerHands[last].Clear();

        if (last == 0)
        {
            foreach (var go in spawnedCardObjects)
                if (go != null) Destroy(go);
            spawnedCardObjects.Clear();
            spawnedCardDatas.Clear();
        }

        RefreshNpcHandViews();
        RefreshHandCountsOnly();

        Debug.Log("===== 最終順位 =====");
        for (int r = 0; r < finishOrder.Count; r++)
            Debug.Log($"{r + 1}位: {PlayerName(finishOrder[r])}");

        if (victoryPanel == null)
        {
            Debug.LogError("victoryPanel が null Inspector で設定");
            StartCoroutine(FallbackReturnToTitle());
            return;
        }

        victoryPanel.Show(new List<int>(finishOrder));
    }

    private System.Collections.IEnumerator FallbackReturnToTitle()
    {
        yield return new WaitForSecondsRealtime(5f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}