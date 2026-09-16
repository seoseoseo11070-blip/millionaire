using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class WeakNpcLogicTests
{
    PlayValidator validator;

    [SetUp]
    public void SetUp()
    {
        validator = new PlayValidator();
    }
    // strength: 3→1, 4→2, 5→3, 6→4, 7→5, 8→6, 9→7, 10→8, J→9, Q→10, K→11, A→12, 2→13, Joker→14
    static Card MakeCard(Card.SuitType suit, int number, int strength, int id = 0)
    {
        return new Card(id, suit, number, strength);
    }

    [Test]
    public void 弱いNPC_場と手札()
    {
        var fieldCards = new List<Card>
        {
            MakeCard(Card.SuitType.Diamond, 4, 2),
        };

        // 場の状況
        var field = new WeakNpcLogic.FieldState
        {
            cardCount = 1,              // 場の枚数（0=空）
            cardStrength = 2,           // 場の強さ
            playType = "1枚",           // "", "1枚", "2枚", "3枚", "4枚", "階段"
            isRevolution = false,       // 革命中なら true
            isSingleJoker = false,      // 場がジョーカー1枚なら true
            lockedSuit = null,          // 縛りなし=null  例: Card.SuitType.Spade
            kaidanMin = 0,
            kaidanMax = 0,
        };

        var hand = new List<Card>
        {
            MakeCard(Card.SuitType.Diamond, 3, 1),
            MakeCard(Card.SuitType.Spade, 4, 2),
            MakeCard(Card.SuitType.Spade, 5, 3),
            MakeCard(Card.SuitType.Heart, 6, 4),
            MakeCard(Card.SuitType.Heart, 9, 7),
        };
        List<Card> result = WeakNpcLogic.Decide(hand, field, validator);
        WeakNpcLogic.LogDecision(field, hand, result, fieldCards);
        Assert.IsNotNull(result);
    }
}