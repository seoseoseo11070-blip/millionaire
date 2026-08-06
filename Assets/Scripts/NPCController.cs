using UnityEngine;
using System.Collections.Generic;

public class NPCController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    public void ThinkAndPlay(int cpuIndex, List<Card> cpuHand)
    {
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
                if (c.suit == Card.SuitType.Spade && c.number == 3)
                {
                    List<Card> trial = new List<Card> { c };
                    if (gameManager.TryValidatePlay(trial, out _, out _))
                        return trial;
                }
            }
        }

        int required = gameManager.GetCurrentFieldCardCount();
        string fieldType = gameManager.GetCurrentFieldPlayType();

        List<List<Card>> candidates = new List<List<Card>>();

        if (required == 0 || required == 1)
        {
            if (required == 0 || string.IsNullOrEmpty(fieldType) || fieldType == "単体")
            {
                foreach (Card c in hand)
                    candidates.Add(new List<Card> { c });
            }
        }

        bool allowSameNumber = required == 0
            || string.IsNullOrEmpty(fieldType)
            || fieldType == "ペア"
            || fieldType == "3枚"
            || fieldType == "4枚";

        if (allowSameNumber && (required == 0 || required >= 2))
            AddSameNumberCandidates(hand, required, candidates);

        bool allowKaidan = required == 0
            || string.IsNullOrEmpty(fieldType)
            || fieldType == "階段";

        if (allowKaidan && (required == 0 || required >= 3))
            AddKaidanCandidates(hand, required, candidates);

        List<Card> best = null;
        int bestStrength = int.MaxValue;

        foreach (List<Card> trial in candidates)
        {
            if (required > 0 && trial.Count != required) continue;
            if (!gameManager.TryValidatePlay(trial, out _, out _)) continue;

            int strength = GetPlayStrength(trial);
            if (best == null || strength < bestStrength)
            {
                best = trial;
                bestStrength = strength;
            }
        }

        return best ?? new List<Card>();
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

        foreach (var kv in groups)
        {
            List<Card> cards = kv.Value;
            int maxUse = cards.Count + jokers.Count;

            int[] sizes = required > 0 ? new[] { required } : new[] { 2, 3, 4 };

            foreach (int size in sizes)
            {
                if (size < 2 || size > 4) continue;
                if (size > maxUse) continue;

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
            HashSet<int> usedStrength = new HashSet<int>();
            foreach (Card c in suitCards)
            {
                if (usedStrength.Contains(c.strength)) continue;
                usedStrength.Add(c.strength);
                unique.Add(c);
            }

            foreach (int size in sizes)
            {
                if (size < 3) continue;
                if (unique.Count + jokers.Count < size) continue;

                for (int start = 0; start < unique.Count; start++)
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
                            if (jokerUsed + gap <= jokers.Count && play.Count + gap + 1 <= size)
                            {
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
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    while (play.Count < size && jokerUsed < jokers.Count)
                    {
                        play.Add(jokers[jokerUsed]);
                        jokerUsed++;
                    }

                    if (play.Count == size)
                        candidates.Add(new List<Card>(play));
                }
            }
        }
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
            {
                if (strengths[i] > max)
                    max = strengths[i];
            }
            return max;
        }

        strengths.Sort();
        int jokersLeft = jokerCount;
        int high = strengths[0];
        for (int i = 1; i < strengths.Count; i++)
        {
            int gap = strengths[i] - strengths[i - 1] - 1;
            jokersLeft -= gap;
            high = strengths[i];
        }
        if (jokersLeft > 0)
            high += jokersLeft;
        return high;
    }

    private string FormatCards(List<Card> cards)
    {
        List<string> names = new List<string>();
        foreach (Card c in cards)
        {
            if (c.suit == Card.SuitType.Joker)
                names.Add("Joker");
            else
                names.Add($"{c.suit}({c.number})");
        }
        return string.Join(",", names);
    }
}