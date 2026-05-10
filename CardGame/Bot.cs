using System.Linq;
using static CardGame.Enums;

namespace CardGame;

public class Bot : Player
{
    public Bot(string name) : base(name) { }

    public override void Action(GameEngine engine)
    {
        var validCards = Hand.Where(card => engine.IsValidMove(card)).ToList();

        if (!validCards.Any())
        {
            if (engine.GameDeck.IsEmpty) 
            { 
                engine.EndRound(); 
                return; 
            }

            Card drawnCard = engine.GameDeck.DrawCard();
            this.DrawCard(drawnCard);

            if (engine.NineToCover != null) return; 

            if (engine.IsValidMove(drawnCard))
            {
                CardSuit? drawnChosenSuit = (drawnCard.Rank == CardRank.Queen) ? ChooseBestSuit() : null;
                engine.PlayTurn(this, drawnCard, drawnChosenSuit);
            }
            else
            {
                engine.PassTurn();
            }
            return;
        }

        var safeCards = validCards.Where(card =>
        {
            if (card.Rank != CardRank.Nine) return true;
            return Hand.Any(c => c != card && (c.Suit == card.Suit || c.Rank == CardRank.Nine)) || Hand.Count == 1;
        }).ToList();

        Card cardToPlay = (safeCards.Any() ? safeCards : validCards)
            .OrderByDescending(c => c.Points)
            .First();

        CardSuit? chosenSuit = null;
        if (cardToPlay.Rank == CardRank.Queen)
        {
            chosenSuit = ChooseBestSuit();
        }

        engine.PlayTurn(this, cardToPlay, chosenSuit);
    }

    private CardSuit ChooseBestSuit()
    {
        var suitGroups = Hand.Where(c => c.Rank != CardRank.Queen)
            .GroupBy(c => c.Suit)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();
        return suitGroups != null ? suitGroups.Key : CardSuit.Spades;
    }
}