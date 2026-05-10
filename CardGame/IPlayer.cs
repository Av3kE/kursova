using System.Collections.Generic;

namespace CardGame;

public interface IPlayer
{
    string Name { get; }
    List<Card> Hand { get; }
    int TotalScore { get; set; }
    bool HasEmptyHand { get; }

    void Action(GameEngine engine);

    void DrawCard(Card card);
    void ClearHand();
    int CalculateHandScore();

    bool PlayCard(Card card);
}