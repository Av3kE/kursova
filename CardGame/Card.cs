using static CardGame.Enums;

namespace CardGame;

public class Card
{
    public CardSuit Suit { get; }
    public CardRank Rank { get; }

    public int Points
    {
        get
        {
            return Rank switch
            {
                CardRank.Ace => 11,
                CardRank.Ten => 10,
                CardRank.Nine => 0,
                CardRank.Eight => 8,
                CardRank.Seven => 7,
                CardRank.Six => 6,
                CardRank.King => 4,
                CardRank.Queen => 3,
                CardRank.Jack => 2,
                _ => 0
            };
        }
    }
    
    public bool IsActionCard => Rank == CardRank.Ace || 
                                Rank == CardRank.Queen || 
                                (Rank == CardRank.King && Suit == CardSuit.Spades) ||
                                Rank == CardRank.Nine || 
                                Rank == CardRank.Seven || 
                                Rank == CardRank.Six;

    public Card(CardSuit suit, CardRank rank)
    {
        Suit = suit;
        Rank = rank;
    }
}