using System.Collections.Generic;
using System.Text;
using UnityEngine;
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
        public static FieldState FromGameManager(GameManager gm)
        {
            return new FieldState
            {
                cardCount = gm.GetCurrentFieldCardCount(),
                cardStrength = gm.GetCurrentFieldCardStrength(),
                playType = gm.GetCurrentFieldPlayType() ?? "",
                isRevolution = gm.IsRevolution(),
                isSingleJoker = gm.IsFieldSingleJoker(),
                lockedSuit = gm.GetLockedSuit(),
                kaidanMin = gm.GetCurrentFieldKaidanMin(),
                kaidanMax = gm.GetCurrentFieldKaidanMax()
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
            AddSameNumberCandidates(hand, required, candidates);

        bool allowKaidan = required == 0
            || fieldType == "階段" || string.IsNullOrEmpty(fieldType);
        if (allowKaidan && (required == 0 || required >= 3))
            AddKaidanCandidates(hand, required, candidates);

        return candidates;
    }

    static void AddSameNumberCandidates(List<Card> hand, int required, List<List<Card>> candidates)
    {
        var groups = new Dictionary<int, List<Card>>();
        var jokers = new List<Card>();

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

                var play = new List<Card>();
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

    static void AddKaidanCandidates(List<Card> hand, int required, List<List<Card>> candidates)
    {
        var bySuit = new Dictionary<Card.SuitType, List<Card>>();
        var jokers = new List<Card>();

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

            var unique = new List<Card>();
            var used = new HashSet<int>();
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

    static List<Card> BuildKaidanFrom(List<Card> unique, List<Card> jokers, int start, int size)
    {
        var play = new List<Card> { unique[start] };
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

    public static string FormatCardJa(Card c)
    {
        if (c == null) return "?";
        if (c.suit == Card.SuitType.Joker) return "ジョーカー";

        string suit = c.suit switch
        {
            Card.SuitType.Spade => "スペード",
            Card.SuitType.Heart => "ハート",
            Card.SuitType.Diamond => "ダイヤ",
            Card.SuitType.Club => "クラブ",
            _ => c.suit.ToString()
        };
        string num = c.number switch
        {
            1 => "A",
            11 => "J",
            12 => "Q",
            13 => "K",
            _ => c.number.ToString()
        };
        return $"{suit}の{num}";
    }

    public static string FormatCardsJa(List<Card> cards)
    {
        if (cards == null || cards.Count == 0) return "なし";
        var sb = new StringBuilder();
        foreach (Card c in cards)
            sb.Append(FormatCardJa(c));
        return sb.ToString();
    }

    public static string FormatResult(List<Card> result)
    {
        if (result == null || result.Count == 0) return "パス";
        return FormatCardsJa(result);
    }

    public static string FormatFieldJa(FieldState field, List<Card> fieldCards = null)
    {
        string board;
        if (fieldCards != null && fieldCards.Count > 0)
            board = FormatCardsJa(fieldCards);
        else if (field.cardCount <= 0)
            board = "なし";
        else if (field.isSingleJoker)
            board = "ジョーカー";
        else
            board = $"{field.playType}(強さ{field.cardStrength})";

        string shibari = field.lockedSuit.HasValue
            ? SuitJa(field.lockedSuit.Value)
            : "なし";
        string revolution = field.isRevolution ? "革命中" : "通常";

        return $"場{board} 縛り{shibari} 革命{revolution}";
    }

    static string SuitJa(Card.SuitType suit)
    {
        return suit switch
        {
            Card.SuitType.Spade => "スペード",
            Card.SuitType.Heart => "ハート",
            Card.SuitType.Diamond => "ダイヤ",
            Card.SuitType.Club => "クラブ",
            _ => suit.ToString()
        };
    }

    public static void LogDecision(
        FieldState field,
        List<Card> hand,
        List<Card> result,
        List<Card> fieldCards = null)
    {
        string fieldPart = FormatFieldJa(field, fieldCards);
        string handPart = FormatCardsJa(hand);
        string playPart = FormatResult(result);
        Debug.Log($"{fieldPart} NPC手札{handPart} NPCが出したカード{playPart}");
    }
}