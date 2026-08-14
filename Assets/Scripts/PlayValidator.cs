using System.Collections.Generic;

public class PlayValidator
{
    public bool IsValidCombination(List<Card> cards, out string playType)
    {
        playType = "";
        int n = cards.Count;

        if (n == 1)
        {
            playType = "1枚";
            return true;
        }

        if (IsSameNumberGroup(cards))
        {
            if (n == 2) { playType = "2枚"; return true; }
            if (n == 3) { playType = "3枚"; return true; }
            if (n == 4) { playType = "4枚"; return true; }
            return false;
        }

        if (n >= 3 && IsKaidan(cards))
        {
            playType = "階段";
            return true;
        }

        return false;
    }

    public bool IsSameNumberGroup(List<Card> cards)
    {
        int? number = null;
        foreach (Card c in cards)
        {
            if (c.suit == Card.SuitType.Joker) continue;
            if (number == null) number = c.number;
            else if (c.number != number.Value) return false;
        }
        return true;
    }

    public bool IsKaidan(List<Card> cards)
    {
        Card.SuitType? suit = null;
        List<int> strengths = new List<int>();
        int jokerCount = 0;

        foreach (Card c in cards)
        {
            if (c.suit == Card.SuitType.Joker)
            {
                jokerCount++;
                continue;
            }
            if (suit == null) suit = c.suit;
            else if (c.suit != suit.Value) return false;
            strengths.Add(c.strength);
        }

        if (suit == null || strengths.Count < 2) return false;

        strengths.Sort();

        for (int i = 0; i < strengths.Count - 1; i++)
        {
            if (strengths[i] == strengths[i + 1]) return false;
        }

        int gaps = 0;
        for (int i = 0; i < strengths.Count - 1; i++)
        {
            int diff = strengths[i + 1] - strengths[i];
            if (diff <= 0) return false;
            gaps += diff - 1;
        }
        return gaps <= jokerCount;
    }

    public void GetKaidanRange(List<Card> cards, out int minStr, out int maxStr)
    {
        minStr = 0;
        maxStr = 0;

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
        {
            minStr = 14;
            maxStr = 14;
            return;
        }

        strengths.Sort();
        int jokersLeft = jokerCount;

        for (int i = 0; i < strengths.Count - 1; i++)
        {
            int gap = strengths[i + 1] - strengths[i] - 1;
            jokersLeft -= gap;
        }

        minStr = strengths[0];
        maxStr = strengths[strengths.Count - 1];

        if (jokersLeft > 0)
            maxStr += jokersLeft;
    }

    public int GetPlayStrength(List<Card> cards)
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

        if (IsSameNumberGroup(cards))
        {
            int max = strengths[0];
            for (int i = 1; i < strengths.Count; i++)
                if (strengths[i] > max) max = strengths[i];
            return max;
        }

        strengths.Sort();
        int jokersLeft = jokerCount;
        int high = strengths[strengths.Count - 1];

        for (int i = 0; i < strengths.Count - 1; i++)
        {
            int gap = strengths[i + 1] - strengths[i] - 1;
            jokersLeft -= gap;
        }

        if (jokersLeft > 0)
            high += jokersLeft;

        return high;
    }

    public Card.SuitType? GetCommonSuit(List<Card> cards)
    {
        Card.SuitType? suit = null;
        foreach (Card c in cards)
        {
            if (c.suit == Card.SuitType.Joker) continue;
            if (suit == null) suit = c.suit;
            else if (c.suit != suit.Value) return null;
        }
        return suit;
    }

    public bool ContainsNumber(List<Card> cards, int number)
    {
        foreach (Card c in cards)
            if (c.number == number) return true;
        return false;
    }

    public int CountSevens(List<Card> cards)
    {
        int count = 0;
        foreach (Card c in cards)
            if (c.number == 7) count++;
        return count;
    }

    public int GetCardStrengthValue(Card c)
    {
        return c.suit == Card.SuitType.Joker ? 14 : c.strength;
    }

    public string FormatCards(List<Card> cards)
    {
        List<string> names = new List<string>();
        foreach (Card c in cards)
        {
            if (c.suit == Card.SuitType.Joker) names.Add("Joker");
            else names.Add($"{c.suit}({c.number})");
        }
        return string.Join(",", names);
    }

    public bool TryValidate(
        List<Card> cards,
        int currentFieldCardCount,
        int currentFieldCardStrength,
        string currentFieldPlayType,
        bool isRevolution,
        bool fieldIsSingleJoker,
        Card.SuitType? lockedSuit,
        int currentFieldKaidanMin,
        int currentFieldKaidanMax,
        out string playType,
        out string error)
    {
        playType = "";
        error = "";

        if (cards == null || cards.Count == 0)
        {
            error = "カードが選ばれていません";
            return false;
        }

        if (!IsValidCombination(cards, out playType))
        {
            error = "その組み合わせは出せません";
            return false;
        }

        if (currentFieldCardCount == 0)
            return true;

        if (cards.Count != currentFieldCardCount)
        {
            error = $"場は{currentFieldCardCount}枚です。同じ枚数で出してください";
            return false;
        }

        if (!string.IsNullOrEmpty(currentFieldPlayType) && playType != currentFieldPlayType)
        {
            error = $"場は「{currentFieldPlayType}」です。「{playType}」では出せません";
            return false;
        }

        if (cards.Count == 1 && currentFieldCardCount == 1 && fieldIsSingleJoker)
        {
            Card playCard = cards[0];
            if (playCard.suit == Card.SuitType.Spade && playCard.number == 3)
                return true;
        }

        if (lockedSuit.HasValue)
        {
            foreach (Card c in cards)
            {
                if (c.suit == Card.SuitType.Joker) continue;
                if (c.suit != lockedSuit.Value)
                {
                    error = $"縛り{lockedSuit.Value} しか出せません";
                    return false;
                }
            }
        }

        if (currentFieldPlayType == "階段" && playType == "階段")
        {
            GetKaidanRange(cards, out int playMin, out int playMax);

            bool kaidanBeats = isRevolution
                ? playMax < currentFieldKaidanMin
                : playMin > currentFieldKaidanMax;

            if (!kaidanBeats)
            {
                error = isRevolution
                    ? $"革命中{currentFieldKaidanMin}より下の階段を出してください"
                    : $"{currentFieldKaidanMax + 1}以上";
                return false;
            }
            return true;
        }

        int playStrength = GetPlayStrength(cards);
        bool beats = isRevolution
            ? playStrength < currentFieldCardStrength
            : playStrength > currentFieldCardStrength;

        if (!beats)
        {
            error = "場のカードより強くありません";
            return false;
        }

        return true;
    }
}