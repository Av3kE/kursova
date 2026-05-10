using System.Collections.Generic;
using System.Linq;

namespace CardGame;

public class Player : IPlayer
{
    public string Name { get; }
    public List<Card> Hand { get; private set; }
    public int TotalScore { get; set; }
    public bool HasEmptyHand => Hand.Count == 0;

    public Player(string name)
    {
        Name = name;
        Hand = new List<Card>();
        TotalScore = 0;
    }

    public virtual void Action(GameEngine engine) { }

    public void DrawCard(Card card) => Hand.Add(card);
    public void ClearHand() => Hand.Clear();
    public int CalculateHandScore() => Hand.Sum(card => card.Points);

    public bool PlayCard(Card card)
    {
        return Hand.Remove(card); 
    }
}