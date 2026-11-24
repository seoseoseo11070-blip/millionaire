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
    public List<Card> DecideCards(List<Card> hand)
    {
        if (hand == null || hand.Count == 0)
            return new List<Card>();

        List<Card> result = FindBestPlayableCards(hand);
        return result ?? new List<Card>();
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
    //普通のNPC
    private List<Card> PickMedium(List<Card> hand, WeakNpcLogic.FieldState field)
    {
        List<List<Card>> valid = BuildValidPlays(hand, field);
        if (valid.Count == 0)
            return new List<Card>();

        int handCount = hand.Count;
        var prefer = new List<List<Card>>();
        var fallback = new List<List<Card>>();

        foreach (var trial in valid)
        {
            if (handCount > 6 && PlayHasProtect(trial))
                fallback.Add(trial);
            else
                prefer.Add(trial);
        }

        var pool = prefer.Count > 0 ? prefer : (fallback.Count > 0 ? fallback : valid);
        pool.Sort((a, b) => GetPlayStrengthSimple(a).CompareTo(GetPlayStrengthSimple(b)));
        return pool[pool.Count / 2];
    }

    private int GetPlayStrengthSimple(List<Card> cards)
    {
        int max = 0;
        foreach (Card c in cards)
        {
            int s = c.suit == Card.SuitType.Joker ? 14 : c.strength;
            if (s > max) max = s;
        }
        return max;
    }
    //強いNPC
    private List<Card> PickStrong(List<Card> hand, WeakNpcLogic.FieldState field)
    {
        List<List<Card>> valid = BuildValidPlays(hand, field);
        if (valid.Count == 0)
            return new List<Card>(); // パス

        int handCount = hand.Count;
        bool fieldEmpty = field.cardCount == 0;

        // ジョーカー単スペ3
        if (field.isSingleJoker)
        {
            foreach (var t in valid)
            {
                if (t.Count == 1 && t[0].suit == Card.SuitType.Spade && t[0].number == 3)
                    return t;
            }
        }

        if (handCount <= 2)
            return WeakNpcLogic.PickWeakest(valid);

        int nextCount = GetNextOpponentHandCount();
        int minOpponent = GetMinOpponentHandCount();
        int strongLeft = EstimateStrongCardsLeft(hand);

        List<Card> best = null;
        int bestScore = int.MinValue;

        foreach (var trial in valid)
        {
            int score = ScoreStrongPlay(trial, hand, field, handCount, fieldEmpty, nextCount, minOpponent, strongLeft);
            if (score > bestScore)
            {
                bestScore = score;
                best = trial;
            }
        }

        if (!fieldEmpty && bestScore < -15)
            return new List<Card>();

        return best ?? new List<Card>();
    }

    private int ScoreStrongPlay(
        List<Card> trial,
        List<Card> hand,
        WeakNpcLogic.FieldState field,
        int handCount,
        bool fieldEmpty,
        int nextCount,
        int minOpponent,
        int strongLeft)
    {
        int score = 0;
        int str = GetPlayStrengthSimple(trial);
        int remain = handCount - trial.Count;
        bool has8 = PlayHasNumber(trial, 8);
        bool has7 = PlayHasNumber(trial, 7);
        bool hasSpade3 = trial.Count == 1
            && trial[0].suit == Card.SuitType.Spade && trial[0].number == 3;
        bool hasJoker = false;
        foreach (Card c in trial)
            if (c.suit == Card.SuitType.Joker) hasJoker = true;

        score += trial.Count * 8;
        if (remain == 0) score += 500;
        if (remain == 1) score += 120;
        if (remain == 2) score += 60;
        if (remain == 3) score += 25;
        if (minOpponent <= 3 && remain <= 5)
            score += 30;
        score += (14 - str) * 2;

        if (fieldEmpty)
        {
            score += (12 - str) * 3;
            if (has8 && handCount > 6) score -= 80;
            if (has7 && handCount > 5) score -= 50;
            if (hasSpade3 && handCount > 4) score -= 70;
            if (hasJoker && handCount > 5) score -= 40;
            if (trial.Count >= 3) score += 25;
            if (trial.Count >= 4) score += 35;
        }
        else
        {
            score += (18 - str) * 2;
            if (str >= 12 && handCount > 5) score -= 25;
        }
        if (has8)
        {
            if (remain <= 3) score += 90;
            else if (handCount <= 6) score += 55;
            else if (!fieldEmpty && nextCount <= 4) score += 40;
            else if (fieldEmpty) score -= 70;
            else score -= 10;
        }
        if (has7)
        {
            if (nextCount <= 3) score += 100;
            else if (nextCount <= 5 && handCount <= 8) score += 55;
            else if (remain <= 2) score += 40;
            else if (handCount > 8) score -= 45;
            else score -= 15;
        }

        if (field.isSingleJoker && hasSpade3)
            score += 400;

        if (hasSpade3 && !field.isSingleJoker)
        {
            if (handCount <= 3) score += 10;
            else score -= 55;
        }

        if (hasJoker && !fieldEmpty && str >= field.cardStrength)
        {
            if (handCount <= 4) score += 20;
            else score -= 30;
        }

        if (trial.Count >= 4)
        {
            if (handCount <= 10) score += 50;
            if (remain <= 4) score += 40;
            if (handCount > 12) score -= 20;
        }

        if (gameManager.IsSuitLocked())
            score += 12;

        var lastSuit = gameManager.GetLastPlaySuit();
        if (!fieldEmpty && lastSuit.HasValue && !gameManager.IsSuitLocked())
        {
            if (IsPlayAllSuitOrJoker(trial, lastSuit))
                score += 28;
        }

        if (strongLeft >= 3 && str >= 11 && handCount > 5)
            score -= 20;

        if (handCount > 6 && PlayHasProtect(trial))
            score -= 18;

        return score;
    }

    private int GetNextOpponentHandCount()
    {
        int players = gameManager.GetPlayerCount();
        for (int i = 1; i < players; i++)
        {
            int idx = (lastCpuIndex + i) % players;
            if (gameManager.HasPlayerFinished(idx)) continue;
            return gameManager.GetHandCount(idx);
        }
        return 99;
    }

    private int GetMinOpponentHandCount()
    {
        int min = 99;
        int players = gameManager.GetPlayerCount();
        for (int i = 0; i < players; i++)
        {
            if (i == lastCpuIndex) continue;
            if (gameManager.HasPlayerFinished(i)) continue;
            int c = gameManager.GetHandCount(i);
            if (c < min) min = c;
        }
        return min;
    }

    private int EstimateStrongCardsLeft(List<Card> myHand)
    {
        int totalStrong = 4 * 6;
        int seen = 0;
        var hist = gameManager.GetPlayedHistory();
        if (hist != null)
        {
            foreach (Card c in hist)
            {
                if (c.suit == Card.SuitType.Joker) { seen++; continue; }
                int n = c.number;
                if (n == 1 || n == 2 || n == 8 || n == 11 || n == 12 || n == 13)
                    seen++;
            }
        }
        foreach (Card c in myHand)
        {
            if (c.suit == Card.SuitType.Joker) continue;
            int n = c.number;
            if (n == 1 || n == 2 || n == 8 || n == 11 || n == 12 || n == 13)
                seen++;
        }
        return Mathf.Max(0, totalStrong - seen);
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

    private List<List<Card>> BuildValidPlays(List<Card> hand, WeakNpcLogic.FieldState field)
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

        bool allowSame = required == 0 || required >= 2
            || fieldType == "2枚" || fieldType == "3枚" || fieldType == "4枚"
            || string.IsNullOrEmpty(fieldType);
        if (allowSame)
            AddSameNumberCandidates(hand, required, candidates);

        bool allowKaidan = required == 0 || required >= 3
            || fieldType == "階段" || string.IsNullOrEmpty(fieldType);
        if (allowKaidan)
            AddKaidanCandidates(hand, required, candidates);

        var valid = new List<List<Card>>();
        var seen = new HashSet<string>();

        foreach (var trial in candidates)
        {
            if (required > 0 && trial.Count != required) continue;
            if (!gameManager.TryValidatePlay(trial, out _, out _)) continue;
            string key = FormatCards(trial);
            if (!seen.Add(key)) continue;
            valid.Add(trial);
        }

        if (valid.Count == 0)
        {
            List<Card> weak = WeakNpcLogic.Decide(hand, field);
            if (weak != null && weak.Count > 0)
                valid.Add(weak);
        }

        return valid;
    }

    private void AddKaidanCandidates(List<Card> hand, int required, List<List<Card>> dst)
    {
        var byStr = new SortedDictionary<int, List<Card>>();
        var jokers = new List<Card>();

        foreach (Card c in hand)
        {
            if (c.suit == Card.SuitType.Joker)
            {
                jokers.Add(c);
                continue;
            }
            int s = c.strength;
            if (!byStr.ContainsKey(s)) byStr[s] = new List<Card>();
            byStr[s].Add(c);
        }

        var strengths = new List<int>(byStr.Keys);
        strengths.Sort();

        int[] lens = required >= 3 ? new[] { required } : new[] { 3, 4, 5, 6 };

        foreach (int len in lens)
        {
            if (len < 3) continue;
            for (int start = 0; start < strengths.Count; start++)
            {
                var trial = new List<Card>();
                int jokersLeft = jokers.Count;
                int expect = strengths[start];
                int need = len;

                int idx = start;
                while (need > 0)
                {
                    if (idx < strengths.Count && strengths[idx] == expect)
                    {
                        trial.Add(byStr[strengths[idx]][0]);
                        idx++;
                        expect++;
                        need--;
                    }
                    else if (jokersLeft > 0)
                    {
                        trial.Add(jokers[jokers.Count - jokersLeft]);
                        jokersLeft--;
                        expect++;
                        need--;
                    }
                    else break;
                }

                if (need == 0 && trial.Count == len)
                    dst.Add(new List<Card>(trial));
            }
        }
    }
    private void AddSameNumberCandidates(List<Card> hand, int required, List<List<Card>> dst)
    {
        var byNum = new Dictionary<int, List<Card>>();
        var jokers = new List<Card>();

        foreach (Card c in hand)
        {
            if (c.suit == Card.SuitType.Joker)
            {
                jokers.Add(c);
                continue;
            }
            if (!byNum.ContainsKey(c.number))
                byNum[c.number] = new List<Card>();
            byNum[c.number].Add(c);
        }

        int[] sizes = required >= 2 ? new[] { required } : new[] { 2, 3, 4 };

        foreach (var kv in byNum)
        {
            List<Card> group = kv.Value;
            foreach (int size in sizes)
            {
                if (size < 2 || size > 4) continue;
                if (group.Count + jokers.Count < size) continue;

                var trial = new List<Card>();
                for (int i = 0; i < group.Count && trial.Count < size; i++)
                    trial.Add(group[i]);
                for (int j = 0; j < jokers.Count && trial.Count < size; j++)
                    trial.Add(jokers[j]);

                if (trial.Count == size)
                    dst.Add(trial);
            }
        }
    }


    private class SingleAnalysis
    {
        public List<Card> weakJunk = new List<Card>();
        public List<Card> highSingles = new List<Card>();
        public bool OnlyHighSinglesLeft;
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