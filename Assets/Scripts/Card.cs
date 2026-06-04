using UnityEngine;

public class Card : MonoBehaviour
{
    public enum SuitType
    {
        Spade,
        Heart,
        Diamond,
        Club,
        Joker
    }
    public int id;
    public SuitType suit;
    public int number;
    public int strength;

    public Card(int id, SuitType suit, int number, int strength)
    {
        this.id = id;
        this.suit = suit;
        this.number = number;
        this.strength = strength;
    }

}
