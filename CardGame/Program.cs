using System.Collections.Generic;
using System.Linq;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

namespace CardGame;

public class GameController
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const float TargetWidth = 120f;
    private const int StartX = 350;
    private const int CardSpacing = 45;

    private GameEngine engine;
    private Player human;

    private List<string> gameLog = new List<string>();
    private double lastBotMoveTime;
    private const double BotMoveDelay = 3.0;

    private bool isChoosingSuit;
    private Card? pendingQueen;
    private Rectangle[] suitButtons = new Rectangle[4];

    public void Run()
    {
        Initialize();
        GameLoop();
        Cleanup();
    }

    private void Initialize()
    {
        InitWindow(ScreenWidth, ScreenHeight, "Card Game 101");
        SetTargetFPS(60);
        AssetManager.LoadAssets();

        human = new Player("Ти");
        var bot1 = new Bot("Василь");
        var bot2 = new Bot("Олена");
        var bot3 = new Bot("Петро");

        engine = new GameEngine(new List<IPlayer> { human, bot1, bot2, bot3 });
        engine.StartNewRound();

        AddLog("Гру розпочато!");
        lastBotMoveTime = GetTime();
    }

    private void GameLoop()
    {
        while (!WindowShouldClose())
        {
            Update();
            Draw();
        }
    }

    private void Update()
    {
        if (engine.IsRoundOver)
        {
            HandleGameOverInput();
            return;
        }

        var handSnapshot = engine.Players.ToDictionary(p => p.Name, p => p.Hand.Count);
        int oldPlayerIndex = engine.CurrentPlayerIndex;
        Card oldTopCard = engine.TopCard;
        bool moveHappened = false;

        if (isChoosingSuit)
        {
            moveHappened = HandleSuitSelection();
        }
        else if (engine.Players[engine.CurrentPlayerIndex] == human)
        {
            moveHappened = HandleHumanTurn();
        }
        else
        {
            if (GetTime() - lastBotMoveTime > BotMoveDelay)
            {
                engine.ProcessCurrentTurn();
                moveHappened = true;
            }
        }

        if (moveHappened)
        {
            ProcessTurnResults(handSnapshot, oldPlayerIndex, oldTopCard);
        }
    }

    private void HandleGameOverInput()
    {
        if (!IsMouseButtonPressed(0)) return;

        Vector2 mousePos = GetMousePosition();
        Rectangle exBtn = new Rectangle(650, 500, 160, 50);

        if (CheckCollisionPointRec(mousePos, exBtn))
        {
            CloseWindow();
            return;
        }

        if (engine.IsGameOver)
        {
            Rectangle resBtn = new Rectangle(400, 500, 230, 50);
            if (CheckCollisionPointRec(mousePos, resBtn))
            {
                foreach (var p in engine.Players) p.TotalScore = 0;
                Restart("Новий турнір розпочато!");
            }
        }
        else
        {
            Rectangle contBtn = new Rectangle(460, 500, 170, 50);
            if (CheckCollisionPointRec(mousePos, contBtn))
            {
                Restart("Новий раунд розпочато!");
            }
        }
    }

    private void Restart(string message)
    {
        engine.StartNewRound();
        gameLog.Clear();
        AddLog(message);
        isChoosingSuit = false;
        pendingQueen = null;
        lastBotMoveTime = GetTime();
    }

    private bool HandleSuitSelection()
    {
        if (!IsMouseButtonPressed(0)) return false;

        Vector2 mPos = GetMousePosition();
        bool clickedOnButton = false;

        for (int i = 0; i < 4; i++)
        {
            if (CheckCollisionPointRec(mPos, suitButtons[i]))
            {
                Enums.CardSuit selected = (Enums.CardSuit)i;
                engine.PlayTurn(human, pendingQueen!, selected);
                AddLog($"Ти замовив(-ла): {GetSuitUkr(selected)}.");

                isChoosingSuit = false;
                pendingQueen = null;
                clickedOnButton = true;
                return true;
            }
        }

        if (!clickedOnButton)
        {
            isChoosingSuit = false;
            pendingQueen = null;
        }

        return false;
    }

    private bool HandleHumanTurn()
    {
        bool hasValidMoves = human.Hand.Any(c => engine.IsValidMove(c));

        if (!hasValidMoves && engine.GameDeck.IsEmpty)
        {
            engine.EndRound();
            AddLog("Ходів більше нема! Кінець раунду!");
            return false;
        }

        if (!IsMouseButtonPressed(0)) return false;

        Vector2 mousePos = GetMousePosition();
        float dH = AssetManager.CardBackTexture.Height * (TargetWidth / AssetManager.CardBackTexture.Width);
        Rectangle deckRect = new Rectangle(400, 200, TargetWidth, dH);

        if (CheckCollisionPointRec(mousePos, deckRect) && !engine.GameDeck.IsEmpty)
        {
            if (hasValidMoves) 
            {
                AddLog("У тебе є чим ходити!");
                return false; 
            }

            Card drawn = engine.GameDeck.DrawCard();
            human.DrawCard(drawn);

            if (engine.NineToCover != null)
            {
                if (engine.IsValidMove(drawn)) {
                    AddLog("Витягнута карта підходить! Твій хід.");
                } else {
                    AddLog("Карта не підходить. Тягни ще!");
                }
            }
            else if (engine.IsValidMove(drawn))
            {
                AddLog("Витягнута карта підходить! Твій хід.");
            }
            else
            {
                AddLog("Карта не підходить. Пропуск ходу.");
                engine.PassTurn();
            }
            return true;
        }

        for (int i = human.Hand.Count - 1; i >= 0; i--)
        {
            Card c = human.Hand[i];
            string key = $"{c.Suit}_{c.Rank}";
            if (!AssetManager.CardTextures.ContainsKey(key)) continue;

            Texture2D tex = AssetManager.CardTextures[key];
            float cH = tex.Height * (TargetWidth / tex.Width);
            Rectangle cardRect = new Rectangle(StartX + (i * CardSpacing), 450, TargetWidth, cH);

            if (CheckCollisionPointRec(mousePos, cardRect))
            {
                if (engine.IsValidMove(c))
                {
                    if (c.Rank == Enums.CardRank.Queen)
                    {
                        isChoosingSuit = true;
                        pendingQueen = c;
                    }
                    else
                    {
                        engine.PlayTurn(human, c);
                        return true;
                    }
                }
                break;
            }
        }
        return false;
    }

    private void ProcessTurnResults(Dictionary<string, int> handSnapshot, int oldPlayerIndex, Card oldTopCard)
    {
        if (engine.TopCard != oldTopCard)
        {
            AddLog(
                $"{engine.Players[oldPlayerIndex].Name} викинув(-ла) {GetRankUkr(engine.TopCard.Rank)} {GetSuitUkr(engine.TopCard.Suit)}.");
        }

        foreach (var p in engine.Players)
        {
            int diff = p.Hand.Count - handSnapshot[p.Name];
            if (diff > 0) AddLog($"{(p == human ? "Ти" : p.Name)} потіг {diff} карту(-и).");
        }

        int expectedNext = (oldPlayerIndex + 1) % engine.Players.Count;
        
        if (engine.CurrentPlayerIndex != expectedNext && 
            engine.CurrentPlayerIndex != oldPlayerIndex && 
            !engine.IsRoundOver)
        {
            AddLog($"{engine.Players[expectedNext].Name} пропускає хід!");
        }

        lastBotMoveTime = GetTime();
    }

    private void Draw()
    {
        BeginDrawing();
        ClearBackground(new Color(35, 100, 50, 255));

        DrawTable();
        DrawPlayers();
        DrawUI();

        if (isChoosingSuit && !engine.IsRoundOver) DrawSuitSelectionMenu();
        if (engine.IsRoundOver) DrawGameOverScreen();

        EndDrawing();
    }

    private void DrawTable()
    {
        float backScale = TargetWidth / AssetManager.CardBackTexture.Width;
        float deckHeight = AssetManager.CardBackTexture.Height * backScale;

        if (!engine.GameDeck.IsEmpty)
        {
            DrawTextureEx(AssetManager.CardBackTexture, new Vector2(400, 200), 0f, backScale, Color.White);
            DrawTextUkr($"В колоді: {engine.GameDeck.Count}", 405, 170, 20, Color.Gold);
        }
        else
        {
            DrawRectangleLinesEx(new Rectangle(400, 200, TargetWidth, deckHeight), 3, Color.Gray);
            DrawTextUkr("ПУСТО", 425, 270, 22, Color.Gray);
        }

        if (engine.TopCard != null &&
            AssetManager.CardTextures.ContainsKey($"{engine.TopCard.Suit}_{engine.TopCard.Rank}"))
        {
            Texture2D tTex = AssetManager.CardTextures[$"{engine.TopCard.Suit}_{engine.TopCard.Rank}"];
            DrawTextureEx(tTex, new Vector2(600, 200), 0f, TargetWidth / tTex.Width, Color.White);
        }
    }

    private void DrawPlayers()
    {
        for (int i = 0; i < human.Hand.Count; i++)
        {
            string key = $"{human.Hand[i].Suit}_{human.Hand[i].Rank}";
            if (AssetManager.CardTextures.ContainsKey(key))
            {
                DrawTextureEx(AssetManager.CardTextures[key], new Vector2(StartX + (i * CardSpacing), 450), 0f, TargetWidth / AssetManager.CardTextures[key].Width, Color.White);
            }
        }

        float botScale = 80f / AssetManager.CardBackTexture.Width;
        float bH = AssetManager.CardBackTexture.Height * botScale;

        int b1Y = 360 - (engine.Players[1].Hand.Count * 30) / 2;
        DrawTextUkr($"{engine.Players[1].Name}: {engine.Players[1].Hand.Count}", 30, b1Y - 30, 22, Color.RayWhite);
        for (int i = 0; i < engine.Players[1].Hand.Count; i++)
            DrawTextureEx(AssetManager.CardBackTexture, new Vector2(30 + bH, b1Y + (i * 30)), 90f, botScale, Color.White);

        int b2X = 640 - (engine.Players[2].Hand.Count * 40) / 2;
        int text2X = b2X + (engine.Players[2].Hand.Count * 40) / 2 - 45;
        DrawTextUkr($"{engine.Players[2].Name}: {engine.Players[2].Hand.Count}", text2X, 120, 22, Color.RayWhite);
        for (int i = 0; i < engine.Players[2].Hand.Count; i++)
            DrawTextureEx(AssetManager.CardBackTexture, new Vector2(b2X + (i * 40), -20), 0f, botScale, Color.White);

        int b3Y = 360 - (engine.Players[3].Hand.Count * 30) / 2;
        DrawTextUkr($"{engine.Players[3].Name}: {engine.Players[3].Hand.Count}", 1140, b3Y - 30, 22, Color.RayWhite);
        for (int i = 0; i < engine.Players[3].Hand.Count; i++)
            DrawTextureEx(AssetManager.CardBackTexture, new Vector2(1250 - bH, b3Y + (i * 30) + 80f), 270f, botScale, Color.White);
    }

    private void DrawUI()
    {
        DrawRectangle(10, 10, 320, 180, new Color(0, 0, 0, 100));
        DrawTextUkr("ЛОГ ПОДІЙ:", 20, 20, 20, Color.Gold);
        for (int i = 0; i < gameLog.Count; i++)
            DrawTextUkr(gameLog[i], 20, 50 + (i * 22), 16, Color.LightGray);

        int infoX = 960;
        DrawRectangle(infoX - 10, 10, 320, 130, new Color(0, 0, 0, 100));
        DrawTextUkr("СТАТУС ГРИ:", infoX, 20, 20, Color.Gold);
        DrawTextUkr($"МАСТЬ: {GetSuitUkrNominative(engine.ActiveSuit)}", infoX, 50, 20, Color.Yellow);

        if (!engine.IsRoundOver)
        {
            DrawTextUkr($"ХІД: {engine.Players[engine.CurrentPlayerIndex].Name}", infoX, 80, 20, Color.Orange);
            if (engine.NineToCover != null)
                DrawTextUkr("ПЕРЕКРИЙ ДЕВ'ЯТКУ!", infoX, 105, 20, Color.Red);
        }
    }

    private void DrawSuitSelectionMenu()
    {
        DrawRectangle(0, 0, 1280, 720, new Color(0, 0, 0, 180));
        DrawTextUkr("ОБЕРИ НОВУ МАСТЬ:", 480, 300, 32, Color.Gold);
        string[] sNames = { "Черва", "Бубна", "Хреста", "Піка" };
        Color[] sColors = { Color.Red, Color.Red, Color.Black, Color.Black };

        for (int i = 0; i < 4; i++)
        {
            suitButtons[i] = new Rectangle(340 + (i * 150), 350, 130, 60);
            DrawRectangleRec(suitButtons[i], Color.RayWhite);
            DrawTextUkr(sNames[i], (int)suitButtons[i].X + 25, (int)suitButtons[i].Y + 20, 20, sColors[i]);
        }
    }

    private void DrawGameOverScreen()
    {
        DrawRectangle(0, 0, 1280, 720, new Color(0, 0, 0, 220));

        if (engine.IsGameOver)
        {
            DrawTextUkr("ТУРНІР ЗАВЕРШЕНО!", 420, 80, 50, Color.Red);
            var winners = engine.Players.Where(p => p.TotalScore <= 101).Select(p => p.Name).ToList();
            string winnersText = winners.Count > 0 ? $"ПЕРЕМОЖЦІ: {string.Join(", ", winners)}" : "ПЕРЕМОЖЦІВ НЕМАЄ";
            DrawTextUkr(winnersText, 350, 150, 40, Color.Gold);
        }
        else
        {
            DrawTextUkr("РАУНД ЗАВЕРШЕНО!", 460, 100, 50, Color.Gold);
        }

        int y = 240;
        foreach (var p in engine.Players)
        {
            if (engine.ResettedPlayers.Contains(p))
            {
                DrawTextUkr($"{p.Name} : 0 очок (Згоріло 101!)", 520, y, 32, Color.Green);
            }
            else
            {
                DrawTextUkr($"{p.Name} : {p.TotalScore} очок", 520, y, 32, p.TotalScore > 101 ? Color.Red : Color.SkyBlue);
            }
            y += 45;
        }

        Rectangle exBtn = new Rectangle(650, 500, 160, 50);
        DrawRectangleRec(exBtn, Color.Maroon);
        DrawTextUkr("ВИЙТИ", (int)exBtn.X + 40, (int)exBtn.Y + 15, 22, Color.White);

        if (!engine.IsGameOver)
        {
            Rectangle contBtn = new Rectangle(460, 500, 170, 50);
            DrawRectangleRec(contBtn, Color.DarkGreen);
            DrawTextUkr("ПРОДОВЖИТИ", (int)contBtn.X + 15, (int)contBtn.Y + 15, 22, Color.White);
        }
        else
        {
            Rectangle resBtn = new Rectangle(400, 500, 230, 50);
            DrawRectangleRec(resBtn, Color.DarkBlue);
            DrawTextUkr("НОВИЙ ТУРНІР", (int)resBtn.X + 30, (int)resBtn.Y + 15, 22, Color.White);
        }
    }

    private void Cleanup()
    {
        AssetManager.UnloadAssets();
        CloseWindow();
    }

    private void AddLog(string message)
    {
        gameLog.Add(message);
        if (gameLog.Count > 6) gameLog.RemoveAt(0);
    }

    private void DrawTextUkr(string text, float x, float y, float fontSize, Color color)
    {
        DrawTextEx(AssetManager.GameFont, text, new Vector2(x, y), fontSize, 1, color);
    }

    private string GetRankUkr(Enums.CardRank rank) => rank switch
    {
        Enums.CardRank.Six => "Шістку", Enums.CardRank.Seven => "Сімку", Enums.CardRank.Eight => "Вісімку",
        Enums.CardRank.Nine => "Дев'ятку", Enums.CardRank.Ten => "Десятку", Enums.CardRank.Jack => "Валета",
        Enums.CardRank.Queen => "Даму", Enums.CardRank.King => "Короля", Enums.CardRank.Ace => "Туза",
        _ => rank.ToString()
    };

    private string GetSuitUkr(Enums.CardSuit suit) => suit switch
    {
        Enums.CardSuit.Hearts => "Черву", Enums.CardSuit.Diamonds => "Бубну",
        Enums.CardSuit.Clubs => "Хресту", Enums.CardSuit.Spades => "Піку", _ => suit.ToString()
    };

    private string GetSuitUkrNominative(Enums.CardSuit suit) => suit switch
    {
        Enums.CardSuit.Hearts => "Черва",
        Enums.CardSuit.Diamonds => "Бубна",
        Enums.CardSuit.Clubs => "Хреста",
        Enums.CardSuit.Spades => "Піка",
        _ => suit.ToString()
    };
}

class Program
{
    static void Main(string[] args)
    {
        GameController game = new GameController();
        game.Run();
    }
}