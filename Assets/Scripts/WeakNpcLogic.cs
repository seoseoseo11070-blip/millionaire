using System.Collections.Generic;
using System.Text;

public static class WeakNpcLogic
{
    public struct FieldState
    {
        public int cardCount;
        public int cardStrength;
        public string playType;
        public bool isRevolution;
        public bool isSingleJoker;
        public Card.SuitType? lockedSuit;
        public int kaidanMin;
        public int kaidanMax;

        public static FieldState Empty()
        {
            return new FieldState
            {
                cardCount = 0,
                cardStrength = 0,
                playType = "",
                isRevolution = false,
                isSingleJoker = false,
                lockedSuit = null,
                kaidanMin = 0,
                kaidanMax = 0
            };
        }

        public static FieldState Single(int strength, bool isJoker = false)
        {
            return new FieldState
            {
                cardCount = 1,
                cardStrength = strength,
                playType = "1枚",
                isRevolution = false,
                isSingleJoker = isJoker,
                lockedSuit = null,
                kaidanMin = 0,
                kaidanMax = 0
            };
        }

        public static FieldState Multi(int count, int strength, string playType)
        {
            return new FieldState
            {
                cardCount = count,
                cardStrength = strength,
                playType = playType,
                isRevolution = false,
                isSingleJoker = false,
                lockedSuit = null,
                kaidanMin = 0,
                kaidanMax = 0
            };
        }
    }

    public static List<Card> Decide(List<Card> hand, FieldState field, PlayValidator validator = null)
    {
        validator ??= new PlayValidator();

        if (hand == null || hand.Count == 0)
            return new List<Card>();

        if (field.isSingleJoker)
        {
            foreach (Card c in hand)
            {
                if (c.suit != Card.SuitType.Spade || c.number != 3) continue;
                var trial = new List<Card> { c };
                if (IsValid(validator, trial, field))
                    return trial;
            }
        }

        List<List<Card>> candidates = BuildCandidates(hand, field.cardCount, field.playType);
        List<List<Card>> valid = new List<List<Card>>();

        foreach (var trial in candidates)
        {
            if (field.cardCount > 0 && trial.Count != field.cardCount) continue;
            if (!IsValid(validator, trial, field)) continue;
            valid.Add(trial);
        }

        return PickWeakest(valid, validator);
    }

    public static List<Card> PickWeakest(List<List<Card>> valid, PlayValidator validator = null)
    {
        validator ??= new PlayValidator();
        List<Card> best = null;
        int bestStrength = int.MaxValue;

        foreach (var trial in valid)
        {
            int s = validator.GetPlayStrength(trial);
            if (best == null || s < bestStrength)
            {
                best = trial;
                bestStrength = s;
            }
        }
        return best ?? new List<Card>();
    }
    public static string FormatResult(List<Card> result)
    {
        if (result == null || result.Count == 0)
            return "パス";

        var sb = new StringBuilder();
        for (int i = 0; i < result.Count; i++)
        {
            if (i > 0) sb.Append(",");
            Card c = result[i];
            if (c.suit == Card.SuitType.Joker) sb.Append("Joker");
            else sb.Append($"{c.suit}({c.number})");
        }
        return sb.ToString();
    }

    static bool IsValid(PlayValidator v, List<Card> trial, FieldState field)
    {
        return v.TryValidate(
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
            out _);
    }

    static List<List<Card>> BuildCandidates(List<Card> hand, int required, string fieldType)
    {
        var candidates = new List<List<Card>>();

        bool allowSingle = required == 0 || required == 1
            || fieldType == "1枚" || string.IsNullOrEmpty(fieldType);
        if (allowSingle && (required == 0 || required == 1))
        {
            foreach (Card c in hand)
                candidates.Add(new List<Card> { c });
        }

        bool allowSame = required == 0
            || fieldType == "2枚" || fieldType == "3枚" || fieldType == "4枚"
            || string.IsNullOrEmpty(fieldType);
        if (allowSame && (required == 0 || required >= 2))
            AddSameNumber(hand, required, candidates);

        return candidates;
    }

    static void AddSameNumber(List<Card> hand, int required, List<List<Card>> candidates)
    {
        var groups = new Dictionary<int, List<Card>>();
        var jokers = new List<Card>();

        foreach (Card c in hand)
        {
            if (c.suit == Card.SuitType.Joker) { jokers.Add(c); continue; }
            if (!groups.ContainsKey(c.number)) groups[c.number] = new List<Card>();
            groups[c.number].Add(c);
        }

        int[] sizes = required > 0 ? new[] { required } : new[] { 2, 3, 4 };

        foreach (var kv in groups)
        {
            var cards = kv.Value;
            int maxUse = cards.Count + jokers.Count;
            foreach (int size in sizes)
            {
                if (size < 2 || size > 4 || size > maxUse) continue;
                var play = new List<Card>();
                int need = size;
                for (int i = 0; i < cards.Count && need > 0; i++, need--) play.Add(cards[i]);
                for (int i = 0; i < jokers.Count && need > 0; i++, need--) play.Add(jokers[i]);
                if (play.Count == size) candidates.Add(play);
            }
        }
    }
}