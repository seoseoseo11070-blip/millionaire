using System.Collections.Generic;
using UnityEngine;

public class deck : MonoBehaviour
{
    private List<Card> Deck = new List<Card>();
    void Start()
    {
        CreateAdvancedDeck();
    }
    private void CreateAdvancedDeck()
    {
        Deck.Clear();
        int currentld = 1;
        for (int s = 0; s < 4; s++)
        {
            Card.SuitType suit = (Card.SuitType)s;
            for (int num = 1; num <= 13; num++)
            {
                int strength = num - 2;
                if (num == 1) strength = 12;
                if (num == 2) strength = 13;

                Deck.Add(new Card(currentld, suit, num, strength));
                currentld++;
            }
        }
        Deck.Add(new Card(53, Card.SuitType.Joker, 0, 14));
        Deck.Add(new Card(54, Card.SuitType.Joker, 0, 14));

        Debug.Log("山札に " + Deck.Count + " 枚のカードデータが作成されました！");
    }
}
