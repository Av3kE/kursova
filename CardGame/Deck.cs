using System;
using System.Collections.Generic;
using static CardGame.Enums;

namespace CardGame;

public class Deck
{
    private List<Card> _cards;
    
    private static readonly Random Random = new Random();

    public int Count => _cards.Count;
    public bool IsEmpty => _cards.Count == 0;

    public Deck()
    {
        GenerateDeck();
        Shuffle();
    }

    private void GenerateDeck()
    {
        _cards = new List<Card>();

        foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
        {
            foreach (CardRank rank in Enum.GetValues(typeof(CardRank)))
            {
                _cards.Add(new Card(suit, rank));
            }
        }
    }

    public void Shuffle()
    {
        int n = _cards.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Next(n + 1);
            (_cards[k], _cards[n]) = (_cards[n], _cards[k]);
        }
    }

    public Card DrawCard()
    {
        if (IsEmpty)
        {
            throw new InvalidOperationException("Колода порожня! Неможливо взяти карту.");
        }

        int lastIndex = _cards.Count - 1;
        Card cardToDraw = _cards[lastIndex];
        
        _cards.RemoveAt(lastIndex);
        
        return cardToDraw;
    }

    public void AddCards(IEnumerable<Card> cardsToAdd)
    {
        _cards.AddRange(cardsToAdd);
        Shuffle();
    }
}