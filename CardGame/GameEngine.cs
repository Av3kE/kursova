using System;
using System.Collections.Generic;
using System.Linq;
using static CardGame.Enums;

namespace CardGame;

public class GameEngine
{
    public List<IPlayer> Players { get; private set; }
    public Deck GameDeck { get; private set; }
    public List<Card> DiscardPile { get; private set; }

    public bool IsRoundOver { get; private set; }

    public bool IsGameOver
    {
        get { return Players.Any(p => p.TotalScore > 101); }
    }

    public int CurrentPlayerIndex { get; private set; }

    public Card TopCard => DiscardPile.LastOrDefault();
    public CardSuit ActiveSuit { get; private set; }

    public Card? NineToCover { get; private set; }

    public List<IPlayer> ResettedPlayers { get; private set; } = new List<IPlayer>();

    public GameEngine(List<IPlayer> players)
    {
        if (players.Count < 3)
        {
            throw new ArgumentException("Мінімум 3 гравці для гри!");
        }

        Players = players;
        DiscardPile = new List<Card>();
    }

    public void StartNewRound()
    {
        IsRoundOver = false;
        NineToCover = null;
        ResettedPlayers.Clear();
        GameDeck = new Deck();
        DiscardPile.Clear();

        foreach (var player in Players)
        {
            player.ClearHand();
        }

        for (int i = 0; i < Players.Count; i++)
        {
            int cardsToDraw = (i == Players.Count - 1) ? 4 : 5;
            for (int j = 0; j < cardsToDraw; j++)
            {
                Players[i].DrawCard(GameDeck.DrawCard());
            }
        }

        Card firstTableCard = GameDeck.DrawCard();
        DiscardPile.Add(firstTableCard);
        ActiveSuit = firstTableCard.Suit;

        CurrentPlayerIndex = 0;
    }

    public bool IsValidMove(Card cardToPlay)
    {
        if (cardToPlay.Rank == CardRank.Queen) return true;

        if (NineToCover != null)
        {
            return cardToPlay.Rank == CardRank.Nine || cardToPlay.Suit == ActiveSuit;
        }

        if (cardToPlay.Rank == CardRank.King && cardToPlay.Suit == CardSuit.Spades)
        {
            return TopCard.Rank == CardRank.King || ActiveSuit == CardSuit.Spades;
        }

        if (cardToPlay.Rank == CardRank.Ace)
        {
            return ActiveSuit == cardToPlay.Suit || TopCard.Rank == CardRank.Ace;
        }

        return cardToPlay.Suit == ActiveSuit || cardToPlay.Rank == TopCard.Rank;
    }

    public bool CanPlayCard(Card card)
    {
        return IsValidMove(card);
    }

    public void PlayTurn(IPlayer player, Card cardToPlay, CardSuit? chosenSuitByQueen = null)
    {
        if (!IsValidMove(cardToPlay))
        {
            throw new InvalidOperationException("Цей хід порушує правила гри!");
        }

        player.PlayCard(cardToPlay);
        DiscardPile.Add(cardToPlay);

        if (cardToPlay.Rank == CardRank.Queen && chosenSuitByQueen.HasValue)
        {
            ActiveSuit = chosenSuitByQueen.Value;
        }
        else
        {
            ActiveSuit = cardToPlay.Suit;
        }

        if (player.HasEmptyHand)
        {
            EndRound(player, cardToPlay);
            return;
        }

        if (cardToPlay.Rank == CardRank.Nine)
        {
            NineToCover = cardToPlay;
            return;
        }
        else
        {
            NineToCover = null;
        }

        ApplyCardEffects(cardToPlay);
    }

    public void ProcessCurrentTurn()
    {
        IPlayer currentPlayer = Players[CurrentPlayerIndex];
        currentPlayer.Action(this);
    }

    public void PassTurn()
    {
        MoveToNextPlayer(skipCount: 0);
    }

    private void ApplyCardEffects(Card playedCard)
    {
        if (playedCard.Rank == CardRank.Ace)
        {
            MoveToNextPlayer(skipCount: 1);
            return;
        }

        int nextPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
        IPlayer nextPlayer = Players[nextPlayerIndex];

        if (playedCard.Rank == CardRank.King && playedCard.Suit == CardSuit.Spades)
        {
            DrawCardsForPlayer(nextPlayer, 4);
            MoveToNextPlayer(skipCount: 1);
            return;
        }

        if (playedCard.Rank == CardRank.Seven)
        {
            DrawCardsForPlayer(nextPlayer, 2);
            MoveToNextPlayer(skipCount: 1);
            return;
        }

        if (playedCard.Rank == CardRank.Six)
        {
            DrawCardsForPlayer(nextPlayer, 1);
            MoveToNextPlayer(skipCount: 1);
            return;
        }

        MoveToNextPlayer(skipCount: 0);
    }

    public void DrawCardsForPlayer(IPlayer player, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (GameDeck.IsEmpty)
            {
                EndRound();
                return;
            }

            player.DrawCard(GameDeck.DrawCard());
        }
    }

    private void MoveToNextPlayer(int skipCount = 0)
    {
        CurrentPlayerIndex = (CurrentPlayerIndex + 1 + skipCount) % Players.Count;
    }

    public void EndRound(IPlayer roundWinner = null, Card lastPlayedCard = null)
    {
        IsRoundOver = true;
        NineToCover = null;

        foreach (var player in Players)
        {
            if (player == roundWinner) continue;
            player.TotalScore += player.CalculateHandScore();
        }

        if (roundWinner != null && lastPlayedCard != null && lastPlayedCard.Rank == CardRank.Queen)
        {
            if (lastPlayedCard.Suit == CardSuit.Spades)
            {
                roundWinner.TotalScore -= 40;
            }
            else
            {
                roundWinner.TotalScore -= 20;
            }
        }

        foreach (var player in Players)
        {
            if (player.TotalScore == 101)
            {
                player.TotalScore = 0;
                ResettedPlayers.Add(player);
            }
        }
    }
}