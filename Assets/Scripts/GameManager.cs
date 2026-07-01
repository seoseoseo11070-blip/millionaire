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

    void Start()
    {
        StartGame(playerCount: 4);
    }

    public void StartGame(int playerCount)
    {
        spawnedCardObjects.Clear();

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

        for (int i = 0; i < playerCount; i++)
        {
            playerHands[i].Sort((a, b) => a.strength.CompareTo(b.strength));
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
        if (handController != null)
        {
            handController.SetupHand(spawnedCardObjects, this);
        }
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
            Debug.Log($"プレイヤー {i + 1} の手札 ({playerHands[i].Count}枚): " + string.Join(", ", cardNames));
        }
    }
    public List<Card> GetPlayerHandData(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < playerHands.Count)
        {
            return playerHands[playerIndex];
        }
        return null;
    }
    public void RemoveCardFromData(int handIndex)
    {
        List<Card> myHand = playerHands[0];
        if (handIndex >= 0 && handIndex < myHand.Count)
        {
            Card removedCard = myHand[handIndex];

            string cardName = removedCard.suit == Card.SuitType.Joker ? "Joker" : $"{removedCard.suit}({removedCard.number})";
            Debug.Log($"場にカードを1枚出しました: {cardName}");

            myHand.RemoveAt(handIndex);
        }
    }
}
