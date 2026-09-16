using UnityEngine;
using System.Collections.Generic;

public class NPCController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    private NpcDifficulty difficulty;
    private int lastCpuIndex = 1;

    void Start()
    {
        difficulty = TitleDifficultySelector.LoadDifficulty();
        Debug.Log($"NPC難易度: {difficulty}");
    }

    public void ThinkAndPlay(int cpuIndex, List<Card> cpuHand)
    {
        lastCpuIndex = cpuIndex;

        if (cpuHand == null || cpuHand.Count == 0)
        {
            gameManager.NextTurn();
            return;
        }

        List<Card> cardsToPlay = FindBestPlayableCards(cpuHand);

        if (cardsToPlay.Count > 0)
        {
            Debug.Log($"NPC{cpuIndex}は{FormatCards(cardsToPlay)}を出した");
            gameManager.PlayCpuCards(cpuIndex, cardsToPlay);
        }
        else
        {
            gameManager.ProcessPass();
        }
    }

    private List<Card> FindBestPlayableCards(List<Card> hand)
    {
        var field = WeakNpcLogic.FieldState.FromGameManager(gameManager);

        switch (difficulty)
        {
            case NpcDifficulty.Normal:
                return PickMedium(hand, field);
            case NpcDifficulty.Strong:
                return PickStrong(hand, field);
            default:
                return WeakNpcLogic.Decide(hand, field);
        }
    }

    private List<Card> PickMedium(List<Card> hand, WeakNpcLogic.FieldState field)
    {
        Debug.Log("普通のNPCはまだ作ってない");
        return WeakNpcLogic.Decide(hand, field);
    }

    //強いNPC
    private List<Card> PickStrong(List<Card> hand, WeakNpcLogic.FieldState field)
    {
        List<List<Card>> valid = BuildValidPlays(hand, field);
        if (valid.Count == 0)
            return new List<Card>();

        int myIndex = lastCpuIndex;
        int handCount = hand.Count;
        bool fieldEmpty = field.cardCount == 0;

        if (handCount <= 2)
            return WeakNpcLogic.PickWeakest(valid);

        if (field.isSingleJoker)
        {
            foreach (var t in valid)
            {
                if (t.Count == 1 && t[0].suit == Card.SuitType.Spade && t[0].number == 3)
                    return t;
            }
        }

        SingleAnalysis singles = AnalyzeSingles(hand);

        List<Card> seven = TrySevenDump(valid, hand, myIndex, singles);
        if (seven != null) return seven;

        List<Card> shibari = TryShibariControl(valid, hand, fieldEmpty);
        if (shibari != null) return shibari;

        List<Card> eight = TryEightControl(valid, handCount, fieldEmpty);
        if (eight != null) return eight;

        if (fieldEmpty)
            return PickStrongLead(valid, hand, singles, handCount);

        return PickStrongResponse(valid, handCount);
    }

    private List<List<Card>> BuildValidPlays(List<Card> hand, WeakNpcLogic.FieldState field)
    {
        var validator = new PlayValidator();
        var all = new List<List<Card>>();

        // 1枚
        if (field.cardCount == 0 || field.cardCount == 1
            || field.playType == "1枚" || string.IsNullOrEmpty(field.playType))
        {
            if (field.cardCount == 0 || field.cardCount == 1)
            {
                foreach (Card c in hand)
                    all.Add(new List<Card> { c });
            }
        }
        CollectValidated(hand, field, validator, all);
        return all;
    }

    private void CollectValidated(
        List<Card> hand,
        WeakNpcLogic.FieldState field,
        PlayValidator validator,
        List<List<Card>> dst)
    {
        var candidates = new List<List<Card>>();

        int required = field.cardCount;
        string fieldType = field.playType;

        bool allowSingle = required == 0 || required == 1
            || fieldType == "1枚" || string.IsNullOrEmpty(fieldType);
        if (allowSingle && (required == 0 || required == 1))
        {
            foreach (Card c in hand)
                candidates.Add(new List<Card> { c });
        }
        var seen = new HashSet<string>();
        foreach (var trial in candidates)
        {
            if (required > 0 && trial.Count != required) continue;
            if (!validator.TryValidate(
                trial,
                field.cardCount,
                field.cardStrength,
                field.playType,
                field.isRevolution,
                field.isSingleJoker,
                field.lockedSuit,
                field.kaidanMin,
                field.kaidanMax,
                out _,
                out _))
                continue;

            string key = FormatCards(trial);
            if (!seen.Add(key)) continue;
            dst.Add(trial);
        }

        List<Card> weakPlay = WeakNpcLogic.Decide(hand, field, validator);
        if (weakPlay.Count > 0)
        {
            string key = FormatCards(weakPlay);
            if (seen.Add(key))
                dst.Add(weakPlay);
        }
    }

    private class SingleAnalysis
    {
        public List<Card> weakJunk = new List<Card>();
        public List<Card> highSingles = new List<Card>();
        public bool OnlyHighSinglesLeft;
    }

    private SingleAnalysis AnalyzeSingles(List<Card> hand)
    {
        var a = new SingleAnalysis();
        var countByNum = new Dictionary<int, int>();

        foreach (Card c in hand)
        {
            if (c.suit == Card.SuitType.Joker) continue;
            if (!countByNum.ContainsKey(c.number)) countByNum[c.number] = 0;
            countByNum[c.number]++;
        }

        foreach (Card c in hand)
        {
            if (c.suit == Card.SuitType.Joker)
            {
                a.highSingles.Add(c);
                continue;
            }
            if (countByNum.TryGetValue(c.number, out int n) && n >= 2)
                continue;

            if (IsHighSingle(c)) a.highSingles.Add(c);
            else a.weakJunk.Add(c);
        }

        a.OnlyHighSinglesLeft = a.weakJunk.Count == 0 && a.highSingles.Count > 0;
        return a;
    }

    private bool IsHighSingle(Card c)
    {
        if (c.suit == Card.SuitType.Joker) return true;
        int n = c.number;
        return n == 1 || n == 2 || n == 7 || n == 8 || n == 11 || n == 12 || n == 13;
    }

    private bool IsProtectCard(Card c)
    {
        if (c.suit == Card.SuitType.Joker) return true;
        int n = c.number;
        return n == 2 || n == 3 || n == 7 || n == 8;
    }

    private bool PlayHasProtect(List<Card> play)
    {
        foreach (Card c in play)
            if (IsProtectCard(c)) return true;
        return false;
    }

    private bool PlayHasNumber(List<Card> play, int number)
    {
        foreach (Card c in play)
            if (c.number == number) return true;
        return false;
    }

    private List<Card> TrySevenDump(List<List<Card>> valid, List<Card> hand, int myIndex, SingleAnalysis singles)
    {
        if (singles.weakJunk.Count == 0) return null;

        int next = (myIndex + 1) % gameManager.GetPlayerCount();
        int nextCount = gameManager.GetHandCount(next);

        foreach (var trial in valid)
        {
            if (!PlayHasNumber(trial, 7)) continue;
            int sevenCount = 0;
            foreach (Card c in trial)
                if (c.number == 7) sevenCount++;

            if (nextCount <= 4 || singles.weakJunk.Count >= sevenCount)
                return trial;
        }
        return null;
    }

    private List<Card> TryShibariControl(List<List<Card>> valid, List<Card> hand, bool fieldEmpty)
    {
        if (fieldEmpty) return null;

        if (gameManager.IsSuitLocked())
        {
            var lockSuit = gameManager.GetLockedSuit();
            var lockedPlays = new List<List<Card>>();
            foreach (var trial in valid)
            {
                if (IsPlayAllSuitOrJoker(trial, lockSuit))
                    lockedPlays.Add(trial);
            }
            if (lockedPlays.Count > 0)
                return PickWeakestAvoidProtect(lockedPlays);
            return null;
        }

        var lastSuit = gameManager.GetLastPlaySuit();
        if (!lastSuit.HasValue) return null;

        var shibariPlays = new List<List<Card>>();
        foreach (var trial in valid)
        {
            if (IsPlayAllSuitOrJoker(trial, lastSuit))
                shibariPlays.Add(trial);
        }
        if (shibariPlays.Count > 0)
            return PickWeakestAvoidProtect(shibariPlays);
        return null;
    }

    private bool IsPlayAllSuitOrJoker(List<Card> play, Card.SuitType? suit)
    {
        if (!suit.HasValue) return false;
        foreach (Card c in play)
        {
            if (c.suit == Card.SuitType.Joker) continue;
            if (c.suit != suit.Value) return false;
        }
        return true;
    }

    private List<Card> TryEightControl(List<List<Card>> valid, int handCount, bool fieldEmpty)
    {
        if (fieldEmpty && handCount > 6) return null;
        foreach (var trial in valid)
        {
            if (!PlayHasNumber(trial, 8)) continue;
            if (!fieldEmpty || handCount <= 6)
                return trial;
        }
        return null;
    }

    private List<Card> PickStrongLead(List<List<Card>> valid, List<Card> hand, SingleAnalysis singles, int handCount)
    {
        if (singles.OnlyHighSinglesLeft)
        {
            var pairs = FilterByCount(valid, 2);
            if (pairs.Count > 0) return PickWeakestAvoidProtect(pairs);
            var triples = FilterByCount(valid, 3);
            if (triples.Count > 0) return PickWeakestAvoidProtect(triples);
        }

        var junkPlays = new List<List<Card>>();
        foreach (var trial in valid)
        {
            if (PlayHasProtect(trial) && handCount > 5) continue;
            if (trial.Count == 1 && IsHighSingle(trial[0]) && handCount > 4) continue;
            junkPlays.Add(trial);
        }
        if (junkPlays.Count > 0)
            return WeakNpcLogic.PickWeakest(junkPlays);
        return PickWeakestAvoidProtect(valid);
    }

    private List<Card> PickStrongResponse(List<List<Card>> valid, int handCount)
    {
        var safe = new List<List<Card>>();
        foreach (var trial in valid)
        {
            if (handCount > 5 && PlayHasProtect(trial)) continue;
            safe.Add(trial);
        }
        if (safe.Count > 0)
            return WeakNpcLogic.PickWeakest(safe);
        return WeakNpcLogic.PickWeakest(valid);
    }

    private List<List<Card>> FilterByCount(List<List<Card>> valid, int count)
    {
        var list = new List<List<Card>>();
        foreach (var t in valid)
            if (t.Count == count) list.Add(t);
        return list;
    }

    private List<Card> PickWeakestAvoidProtect(List<List<Card>> valid)
    {
        var safe = new List<List<Card>>();
        foreach (var t in valid)
            if (!PlayHasProtect(t)) safe.Add(t);
        if (safe.Count > 0)
            return WeakNpcLogic.PickWeakest(safe);
        return WeakNpcLogic.PickWeakest(valid);
    }

    private string FormatCards(List<Card> cards)
    {
        var names = new List<string>();
        foreach (Card c in cards)
        {
            if (c.suit == Card.SuitType.Joker) names.Add("Joker");
            else names.Add($"{c.suit}({c.number})");
        }
        return string.Join(",", names);
    }
}