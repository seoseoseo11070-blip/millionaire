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

    static Card MakeCard(Card.SuitType suit, int number, int strength, int id = 0)
    {
        return new Card(id, suit, number, strength);
    }


    [Test]
    public void 場が空_最弱の1枚を出す()
    {
        var field = WeakNpcLogic.FieldState.Empty();
        var hand = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 3, 1),
            MakeCard(Card.SuitType.Heart, 2, 13),
        };

        List<Card> result = WeakNpcLogic.Decide(hand, field, validator);
        string played = WeakNpcLogic.FormatResult(result);
        Debug.Log($"[場が空_最弱の1枚を出す] NPCの出し: {played}");

        Assert.AreEqual(1, result.Count, $"実際: {played}");
        Assert.AreEqual(3, result[0].number, $"実際: {played}");
        Assert.AreEqual(Card.SuitType.Spade, result[0].suit, $"実際: {played}");
    }


    [Test]
    public void 場に単体_勝てる最弱を出す()
    {
        var field = WeakNpcLogic.FieldState.Single(strength: 1);
        var hand = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 4, 2),
            MakeCard(Card.SuitType.Heart, 5, 3),
            MakeCard(Card.SuitType.Club, 2, 13),
        };

        List<Card> result = WeakNpcLogic.Decide(hand, field, validator);
        string played = WeakNpcLogic.FormatResult(result);
        Debug.Log($"[場に単体_勝てる最弱を出す] NPCの出し: {played}");

        Assert.AreEqual(1, result.Count, $"実際: {played}");
        Assert.AreEqual(4, result[0].number, $"実際: {played}");
    }

    [Test]
    public void 場に単体_勝てなければパス()
    {
        var field = WeakNpcLogic.FieldState.Single(strength: 13);
        var hand = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 3, 1),
            MakeCard(Card.SuitType.Heart, 4, 2),
        };

        List<Card> result = WeakNpcLogic.Decide(hand, field, validator);
        string played = WeakNpcLogic.FormatResult(result);
        Debug.Log($"[場に単体_勝てなければパス] NPCの出し: {played}");

        Assert.AreEqual(0, result.Count, $"実際: {played}");
        Assert.AreEqual("パス", played);
    }


    [Test]
    public void 場がジョーカー_スペ3を出す()
    {
        var field = WeakNpcLogic.FieldState.Single(strength: 14, isJoker: true);
        var hand = new List<Card>
        {
            MakeCard(Card.SuitType.Heart, 2, 13),
            MakeCard(Card.SuitType.Spade, 3, 1),
        };

        List<Card> result = WeakNpcLogic.Decide(hand, field, validator);
        string played = WeakNpcLogic.FormatResult(result);
        Debug.Log($"[場がジョーカー_スペ3を出す] NPCの出し: {played}");

        Assert.AreEqual(1, result.Count, $"実際: {played}");
        Assert.AreEqual(Card.SuitType.Spade, result[0].suit, $"実際: {played}");
        Assert.AreEqual(3, result[0].number, $"実際: {played}");
    }


    [Test]
    public void 場がペア_より強いペアを出す()
    {
        var field = WeakNpcLogic.FieldState.Multi(2, strength: 2, playType: "2枚");
        var hand = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 5, 3),
            MakeCard(Card.SuitType.Heart, 5, 3),
            MakeCard(Card.SuitType.Club, 3, 1),
        };

        List<Card> result = WeakNpcLogic.Decide(hand, field, validator);
        string played = WeakNpcLogic.FormatResult(result);
        Debug.Log($"[場がペア_より強いペアを出す] NPCの出し: {played}");

        Assert.AreEqual(2, result.Count, $"実際: {played}");
        Assert.AreEqual(5, result[0].number, $"実際: {played}");
        Assert.AreEqual(5, result[1].number, $"実際: {played}");
    }


    [Test]
    public void ひな形_場と手札を書き換えて使う()
    {
        var field = WeakNpcLogic.FieldState.Empty();
        var hand = new List<Card>
        {
            MakeCard(Card.SuitType.Spade, 3, 1),
            MakeCard(Card.SuitType.Heart, 4, 2),
        };

        List<Card> result = WeakNpcLogic.Decide(hand, field, validator);
        string played = WeakNpcLogic.FormatResult(result);
        Debug.Log($"[ひな形] NPCの出し: {played}");

        Assert.AreEqual(1, result.Count, $"実際: {played}");
        Assert.AreEqual(3, result[0].number, $"実際: {played}");
    }
}