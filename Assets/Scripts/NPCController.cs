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
            Debug.Log($"NPC{cpuIndex + 1}は{FormatCards(cardsToPlay)}を出した");
            gameManager.PlayCpuCards(cpuIndex, cardsToPlay);
        }
        else
        {
            gameManager.ProcessPass();
        }
    }

    private List<Card> FindBestPlayableCards(List<Card> hand)
    {
        if (gameManager.IsFieldSingleJoker())
        {
            foreach (Card c in hand)
            {
                if (c.suit != Card.SuitType.Spade || c.number != 3) continue;
                List<Card> trial = new List<Card> { c };
                if (gameManager.TryValidatePlay(trial, out _, out _))
                    return trial;
            }
        }

        int required = gameManager.GetCurrentFieldCardCount();
        string fieldType = gameManager.GetCurrentFieldPlayType();

        List<List<Card>> candidates = new List<List<Card>>();

        bool allowSingle = required == 0 || required == 1
            || fieldType == "1枚" || string.IsNullOrEmpty(fieldType);
        if (allowSingle && (required == 0 || required == 1))
        {
            foreach (Card c in hand)
                candidates.Add(new List<Card> { c });
        }

        bool allowSameNumber = required == 0
            || fieldType == "2枚" || fieldType == "3枚" || fieldType == "4枚"
            || string.IsNullOrEmpty(fieldType);
        if (allowSameNumber && (required == 0 || required >= 2))
            AddSameNumberCandidates(hand, required, candidates);

        bool allowKaidan = required == 0
            || fieldType == "階段" || string.IsNullOrEmpty(fieldType);
        if (allowKaidan && (required == 0 || required >= 3))
            AddKaidanCandidates(hand, required, candidates);

        List<List<Card>> valid = new List<List<Card>>();
        foreach (List<Card> trial in candidates)
        {
            if (required > 0 && trial.Count != required) continue;
            if (!gameManager.TryValidatePlay(trial, out _, out _)) continue;
            valid.Add(trial);
        }

        if (valid.Count == 0)
            return new List<Card>();

        switch (difficulty)
        {
            case NpcDifficulty.Normal:
                return PickMedium(valid);
            case NpcDifficulty.Strong:
                return PickStrong(valid);
            default:
                return PickWeakest(valid);
        }
    }

    // 弱い
    private List<Card> PickWeakest(List<List<Card>> valid)
    {
        List<Card> best = null;
        int bestStrength = int.MaxValue;

        foreach (List<Card> trial in valid)
        {
            int strength = GetPlayStrength(trial);
            if (best == null || strength < bestStrength)
            {
                best = trial;
                bestStrength = strength;
            }
        }
        return best ?? new List<Card>();
    }

    // 普通
    private List<Card> PickMedium(List<List<Card>> valid)
    {
        Debug.Log("普通のNPCはまだ作ってない");
        return PickWeakest(valid);
    }

    // 強い
    private List<Card> PickStrong(List<List<Card>> valid)
    {
        if (valid == null || valid.Count == 0)
            return new List<Card>();

        int myIndex = lastCpuIndex;
        List<Card> hand = gameManager.GetPlayerHandData(myIndex) ?? new List<Card>();
        int handCount = hand.Count;
        bool fieldEmpty = gameManager.GetCurrentFieldCardCount() == 0;

        if (handCount <= 2)
            return PickWeakest(valid);

        if (gameManager.IsFieldSingleJoker())
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

    // ----- 1枚札分析 -----
    private class SingleAnalysis
    {
        public List<Card> weakJunk = new List<Card>();
        public List<Card> highSingles = new List<Card>();
        public bool OnlyHighSinglesLeft;
    }

    private SingleAnalysis AnalyzeSingles(List<Card> hand)
    {
        SingleAnalysis a = new SingleAnalysis();
        Dictionary<int, int> countByNum = new Dictionary<int, int>();

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

            if (IsHighSingle(c))
                a.highSingles.Add(c);
            else
                a.weakJunk.Add(c);
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

            bool harass = nextCount <= 4;
            bool needDump = singles.weakJunk.Count >= sevenCount;
            if (harass || needDump)
                return trial;
        }
        return null;
    }

    private List<Card> TryShibariControl(List<List<Card>> valid, List<Card> hand, bool fieldEmpty)
    {
        if (fieldEmpty) return null;

        bool alreadyLocked = gameManager.IsSuitLocked();
        Card.SuitType? lastSuit = gameManager.GetLastPlaySuit();

        if (alreadyLocked)
        {
            Card.SuitType? lockSuit = gameManager.GetLockedSuit();
            List<List<Card>> lockedPlays = new List<List<Card>>();
            foreach (var trial in valid)
            {
                if (IsPlayAllSuitOrJoker(trial, lockSuit))
                    lockedPlays.Add(trial);
            }
            if (lockedPlays.Count > 0)
                return PickWeakestAvoidProtect(lockedPlays);
            return null;
        }

        if (!lastSuit.HasValue) return null;

        List<List<Card>> shibariPlays = new List<List<Card>>();
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
            List<List<Card>> pairs = FilterByCount(valid, 2);
            if (pairs.Count > 0)
                return PickWeakestAvoidProtect(pairs);

            List<List<Card>> triples = FilterByCount(valid, 3);
            if (triples.Count > 0)
                return PickWeakestAvoidProtect(triples);
        }

        List<List<Card>> junkPlays = new List<List<Card>>();
        foreach (var trial in valid)
        {
            if (PlayHasProtect(trial) && handCount > 5) continue;
            if (trial.Count == 1 && IsHighSingle(trial[0]) && handCount > 4) continue;
            junkPlays.Add(trial);
        }

        if (junkPlays.Count > 0)
            return PickWeakest(junkPlays);

        return PickWeakestAvoidProtect(valid);
    }

    private List<Card> PickStrongResponse(List<List<Card>> valid, int handCount)
    {
        List<List<Card>> safe = new List<List<Card>>();
        foreach (var trial in valid)
        {
            if (handCount > 5 && PlayHasProtect(trial)) continue;
            safe.Add(trial);
        }

        if (safe.Count > 0)
            return PickWeakest(safe);

        return PickWeakest(valid);
    }

    private List<List<Card>> FilterByCount(List<List<Card>> valid, int count)
    {
        List<List<Card>> list = new List<List<Card>>();
        foreach (var t in valid)
            if (t.Count == count) list.Add(t);
        return list;
    }

    private List<Card> PickWeakestAvoidProtect(List<List<Card>> valid)
    {
        List<List<Card>> safe = new List<List<Card>>();
        foreach (var t in valid)
            if (!PlayHasProtect(t)) safe.Add(t);

        if (safe.Count > 0)
            return PickWeakest(safe);
        return PickWeakest(valid);
    }
    private void AddSameNumberCandidates(List<Card> hand, int required, List<List<Card>> candidates)
    {
        Dictionary<int, List<Card>> groups = new Dictionary<int, List<Card>>();
        List<Card> jokers = new List<Card>();

        foreach (Card c in hand)
        {
            if (c.suit == Card.SuitType.Joker)
            {
                jokers.Add(c);
                continue;
            }
            if (!groups.ContainsKey(c.number))
                groups[c.number] = new List<Card>();
            groups[c.number].Add(c);
        }

        int[] sizes = required > 0 ? new[] { required } : new[] { 2, 3, 4 };

        foreach (var kv in groups)
        {
            List<Card> cards = kv.Value;
            int maxUse = cards.Count + jokers.Count;

            foreach (int size in sizes)
            {
                if (size < 2 || size > 4 || size > maxUse) continue;

                List<Card> play = new List<Card>();
                int need = size;
                for (int i = 0; i < cards.Count && need > 0; i++, need--)
                    play.Add(cards[i]);
                for (int i = 0; i < jokers.Count && need > 0; i++, need--)
                    play.Add(jokers[i]);

                if (play.Count == size)
                    candidates.Add(play);
            }
        }

        if (jokers.Count >= 2 && (required == 0 || required == 2))
            candidates.Add(new List<Card> { jokers[0], jokers[1] });
    }

    private void AddKaidanCandidates(List<Card> hand, int required, List<List<Card>> candidates)
    {
        Dictionary<Card.SuitType, List<Card>> bySuit = new Dictionary<Card.SuitType, List<Card>>();
        List<Card> jokers = new List<Card>();

        foreach (Card c in hand)
        {
            if (c.suit == Card.SuitType.Joker)
            {
                jokers.Add(c);
                continue;
            }
            if (!bySuit.ContainsKey(c.suit))
                bySuit[c.suit] = new List<Card>();
            bySuit[c.suit].Add(c);
        }

        int[] sizes = required > 0 ? new[] { required } : new[] { 3, 4, 5, 6 };

        foreach (var kv in bySuit)
        {
            List<Card> suitCards = new List<Card>(kv.Value);
            suitCards.Sort((a, b) => a.strength.CompareTo(b.strength));

            List<Card> unique = new List<Card>();
            HashSet<int> used = new HashSet<int>();
            foreach (Card c in suitCards)
            {
                if (!used.Add(c.strength)) continue;
                unique.Add(c);
            }

            foreach (int size in sizes)
            {
                if (size < 3 || unique.Count + jokers.Count < size) continue;
                for (int start = 0; start < unique.Count; start++)
                {
                    List<Card> play = BuildKaidanFrom(unique, jokers, start, size);
                    if (play != null)
                        candidates.Add(play);
                }
            }
        }
    }

    private List<Card> BuildKaidanFrom(List<Card> unique, List<Card> jokers, int start, int size)
    {
        List<Card> play = new List<Card> { unique[start] };
        int lastStr = unique[start].strength;
        int jokerUsed = 0;
        int i = start + 1;

        while (play.Count < size)
        {
            if (i < unique.Count && unique[i].strength == lastStr + 1)
            {
                play.Add(unique[i]);
                lastStr = unique[i].strength;
                i++;
            }
            else if (jokerUsed < jokers.Count)
            {
                play.Add(jokers[jokerUsed]);
                jokerUsed++;
                lastStr++;
            }
            else if (i < unique.Count && unique[i].strength > lastStr + 1)
            {
                int gap = unique[i].strength - lastStr - 1;
                if (jokerUsed + gap > jokers.Count || play.Count + gap + 1 > size)
                    return null;

                for (int g = 0; g < gap; g++)
                {
                    play.Add(jokers[jokerUsed]);
                    jokerUsed++;
                    lastStr++;
                }
                play.Add(unique[i]);
                lastStr = unique[i].strength;
                i++;
            }
            else break;
        }

        while (play.Count < size && jokerUsed < jokers.Count)
        {
            play.Add(jokers[jokerUsed]);
            jokerUsed++;
        }

        return play.Count == size ? play : null;
    }

    private int GetPlayStrength(List<Card> cards)
    {
        int jokerCount = 0;
        List<int> strengths = new List<int>();

        foreach (Card c in cards)
        {
            if (c.suit == Card.SuitType.Joker)
            {
                jokerCount++;
                continue;
            }
            strengths.Add(c.strength);
        }

        if (strengths.Count == 0)
            return 14;

        bool sameNumber = true;
        for (int i = 1; i < strengths.Count; i++)
        {
            if (strengths[i] != strengths[0])
            {
                sameNumber = false;
                break;
            }
        }

        if (sameNumber)
        {
            int max = strengths[0];
            for (int i = 1; i < strengths.Count; i++)
                if (strengths[i] > max) max = strengths[i];
            return max;
        }

        strengths.Sort();
        int high = strengths[strengths.Count - 1];
        int jokersLeft = jokerCount;
        for (int i = 0; i < strengths.Count - 1; i++)
            jokersLeft -= strengths[i + 1] - strengths[i] - 1;
        if (jokersLeft > 0)
            high += jokersLeft;
        return high;
    }

    private string FormatCards(List<Card> cards)
    {
        List<string> names = new List<string>();
        foreach (Card c in cards)
        {
            if (c.suit == Card.SuitType.Joker) names.Add("Joker");
            else names.Add($"{c.suit}({c.number})");
        }
        return string.Join(",", names);
    }
}