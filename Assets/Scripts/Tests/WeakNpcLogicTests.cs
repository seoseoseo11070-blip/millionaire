using System.Collections.Generic;
using System.Security.AccessControl;
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

    // strength: 3→1, 4→2, 5→3, 6→4, 7→5, 8→6, 9→7, 10→8,
    //           J→9, Q→10, K→11, A→12, 2→13, Joker→14


    static Card MakeCard(Card.SuitType suit, int number, int strength, int id = 0)
    {
        return new Card(id, suit, number, strength);
    }

    void RunAndAssert(
        string caseName,
        WeakNpcLogic.FieldState field,
        List<Card> fieldCards,
        List<Card> hand,
        List<Card> expected)
    {
        List<Card> result = WeakNpcLogic.Decide(hand, field, validator);
        WeakNpcLogic.LogDecision(field, hand, result, fieldCards);

        string actualText = WeakNpcLogic.FormatResult(result);
        string expectedText = (expected == null || expected.Count == 0)
            ? "パス"
            : WeakNpcLogic.FormatCardsJa(expected);

        Debug.Log($"[{caseName}] 予想:{expectedText} / 結果:{actualText}");

        if (expected == null || expected.Count == 0)
        {
            Assert.AreEqual(0, result.Count, $"[{caseName}] パス予想なのに出した: {actualText}");
            return;
        }

        Assert.AreEqual(expected.Count, result.Count, $"[{caseName}] 枚数が違う 予想:{expectedText} 結果:{actualText}");
        var expectedNumbers = new List<int>();
        var actualNumbers = new List<int>();
        foreach (var c in expected) expectedNumbers.Add(c.number);
        foreach (var c in result) actualNumbers.Add(c.number);
        expectedNumbers.Sort();
        actualNumbers.Sort();
        CollectionAssert.AreEqual(expectedNumbers, actualNumbers, $"[{caseName}] 予想:{expectedText} 結果:{actualText}");
    }

    [Test]
    public void 場が空()
    {
        var fieldCards = new List<Card>();
        var field = WeakNpcLogic.FieldState.Empty();

        var hand = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 3, 1),
            MakeCard(Card.SuitType.Heart, 5, 3),
        };

        var expected = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 3, 1),
        };

        RunAndAssert(nameof(場が空), field, fieldCards, hand, expected);
    }

    [Test]
    public void 場が1枚()
    {
        var fieldCards = new List<Card>
        {
            MakeCard(Card.SuitType.Diamond, 4, 2),
        };
        var field = new WeakNpcLogic.FieldState
        {
            cardCount = 1,
            cardStrength = 2,
            playType = "1枚",
            isRevolution = false,
            isSingleJoker = false,
            lockedSuit = null,
            kaidanMin = 0,
            kaidanMax = 0,
        };

        var hand = new List<Card>
        {
            MakeCard(Card.SuitType.Diamond, 3, 1),
            MakeCard(Card.SuitType.Spade, 5, 3),
            MakeCard(Card.SuitType.Heart, 9, 7),
        };
        var expected = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 5, 3),
        };

        RunAndAssert(nameof(場が1枚), field, fieldCards, hand, expected);
    }

    [Test]
    public void 場が2枚()
    {
        var fieldCards = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 4, 2),
            MakeCard(Card.SuitType.Heart, 4, 2),
        };
        var field = new WeakNpcLogic.FieldState
        {
            cardCount = 2,
            cardStrength = 2,
            playType = "2枚",
            isRevolution = false,
            isSingleJoker = false,
            lockedSuit = null,
            kaidanMin = 0,
            kaidanMax = 0,
        };

        var hand = new List<Card>
        {
            MakeCard(Card.SuitType.Club, 3, 1),
            MakeCard(Card.SuitType.Spade, 5, 3),
            MakeCard(Card.SuitType.Heart, 5, 3),
            MakeCard(Card.SuitType.Diamond, 9, 7),
        };

        var expected = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 5, 3),
            MakeCard(Card.SuitType.Heart, 5, 3),
        };

        RunAndAssert(nameof(場が2枚), field, fieldCards, hand, expected);
    }

    [Test]
    public void 場が3枚()
    {
        var fieldCards = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 4, 2),
            MakeCard(Card.SuitType.Heart, 4, 2),
            MakeCard(Card.SuitType.Club, 4, 2),
        };
        var field = new WeakNpcLogic.FieldState
        {
            cardCount = 3,
            cardStrength = 2,
            playType = "3枚",
            isRevolution = false,
            isSingleJoker = false,
            lockedSuit = null,
            kaidanMin = 0,
            kaidanMax = 0,
        };

        var hand = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 5, 3),
            MakeCard(Card.SuitType.Heart, 5, 3),
            MakeCard(Card.SuitType.Diamond, 5, 3),
            MakeCard(Card.SuitType.Club, 9, 7),
        };

        var expected = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 5, 3),
            MakeCard(Card.SuitType.Heart, 5, 3),
            MakeCard(Card.SuitType.Diamond, 5, 3),
        };

        RunAndAssert(nameof(場が3枚), field, fieldCards, hand, expected);
    }
    [Test]
    public void 場が4枚()
    {
        var fieldCards = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 3, 1),
            MakeCard(Card.SuitType.Heart, 3, 1),
            MakeCard(Card.SuitType.Diamond, 3, 1),
            MakeCard(Card.SuitType.Club, 3, 1),
        };
        var field = new WeakNpcLogic.FieldState
        {
            cardCount = 4,
            cardStrength = 1,
            playType = "4枚",
            isRevolution = false,
            isSingleJoker = false,
            lockedSuit = null,
            kaidanMin = 0,
            kaidanMax = 0,
        };

        var hand = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 4, 2),
            MakeCard(Card.SuitType.Heart, 4, 2),
            MakeCard(Card.SuitType.Diamond, 4, 2),
            MakeCard(Card.SuitType.Club, 4, 2),
        };

        var expected = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 4, 2),
            MakeCard(Card.SuitType.Heart, 4, 2),
            MakeCard(Card.SuitType.Diamond, 4, 2),
            MakeCard(Card.SuitType.Club, 4, 2),
        };

        RunAndAssert(nameof(場が4枚), field, fieldCards, hand, expected);
    }

    [Test]
    public void 革命中_場が1枚()
    {
        var fieldCards = new List<Card>
        {
            MakeCard(Card.SuitType.Heart, 5, 3),
        };
        var field = new WeakNpcLogic.FieldState
        {
            cardCount = 1,
            cardStrength = 3,
            playType = "1枚",
            isRevolution = true,
            isSingleJoker = false,
            lockedSuit = null,
            kaidanMin = 0,
            kaidanMax = 0,
        };

        var hand = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 3, 1),
            MakeCard(Card.SuitType.Heart, 4, 2),
            MakeCard(Card.SuitType.Club, 9, 7),
        };

        var expected = new List<Card>
        {
            MakeCard(Card.SuitType.Heart, 4, 2),
        };

        RunAndAssert(nameof(革命中_場が1枚), field, fieldCards, hand, expected);
    }
    [Test]
    public void 革命中_場が2枚()
    {
        var fieldCards = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 6, 4),
            MakeCard(Card.SuitType.Heart, 6, 4),
        };
        var field = new WeakNpcLogic.FieldState
        {
            cardCount = 2,
            cardStrength = 4,
            playType = "2枚",
            isRevolution = true,
            isSingleJoker = false,
            lockedSuit = null,
            kaidanMin = 0,
            kaidanMax = 0,
        };

        var hand = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 4, 2),
            MakeCard(Card.SuitType.Heart, 4, 2),
            MakeCard(Card.SuitType.Club, 9, 7),
        };

        var expected = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 4, 2),
            MakeCard(Card.SuitType.Heart, 4, 2),
        };

        RunAndAssert(nameof(革命中_場が2枚), field, fieldCards, hand, expected);
    }

    [Test]
    public void 革命中_場が3枚()
    {
        var fieldCards = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 7, 5),
            MakeCard(Card.SuitType.Heart, 7, 5),
            MakeCard(Card.SuitType.Diamond, 7, 5),
        };
        var field = new WeakNpcLogic.FieldState
        {
            cardCount = 3,
            cardStrength = 5,
            playType = "3枚",
            isRevolution = true,
            isSingleJoker = false,
            lockedSuit = null,
            kaidanMin = 0,
            kaidanMax = 0,
        };

        var hand = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 4, 2),
            MakeCard(Card.SuitType.Heart, 4, 2),
            MakeCard(Card.SuitType.Club, 4, 2),
        };

        var expected = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 4, 2),
            MakeCard(Card.SuitType.Heart, 4, 2),
            MakeCard(Card.SuitType.Club, 4, 2),
        };

        RunAndAssert(nameof(革命中_場が3枚), field, fieldCards, hand, expected);
    }
    [Test]
    public void 革命中_場が4枚()
    {
        var fieldCards = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 8, 6),
            MakeCard(Card.SuitType.Heart, 8, 6),
            MakeCard(Card.SuitType.Diamond, 8, 6),
            MakeCard(Card.SuitType.Club, 8, 6),
        };
        var field = new WeakNpcLogic.FieldState
        {
            cardCount = 4,
            cardStrength = 6,
            playType = "4枚",
            isRevolution = true,
            isSingleJoker = false,
            lockedSuit = null,
            kaidanMin = 0,
            kaidanMax = 0,
        };

        var hand = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 3, 1),
            MakeCard(Card.SuitType.Heart, 3, 1),
            MakeCard(Card.SuitType.Diamond, 3, 1),
            MakeCard(Card.SuitType.Club, 3, 1),
        };

        var expected = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 3, 1),
            MakeCard(Card.SuitType.Heart, 3, 1),
            MakeCard(Card.SuitType.Diamond, 3, 1),
            MakeCard(Card.SuitType.Club, 3, 1),
        };

        RunAndAssert(nameof(革命中_場が4枚), field, fieldCards, hand, expected);
    }

    [Test]
    public void 場が1枚_勝てなければパス()
    {
        var fieldCards = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 2, 13),
        };
        var field = new WeakNpcLogic.FieldState
        {
            cardCount = 1,
            cardStrength = 13,
            playType = "1枚",
            isRevolution = false,
            isSingleJoker = false,
            lockedSuit = null,
            kaidanMin = 0,
            kaidanMax = 0,
        };

        var hand = new List<Card>
        {
            MakeCard(Card.SuitType.Heart, 3, 1),
            MakeCard(Card.SuitType.Club, 5, 3),
        };

        var expected = new List<Card>();

        RunAndAssert(nameof(場が1枚_勝てなければパス), field, fieldCards, hand, expected);
    }
}