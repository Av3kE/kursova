using System;
using System.Collections.Generic;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace CardGame;

public static class AssetManager
{
    public static Dictionary<string, Texture2D> CardTextures { get; private set; }
    public static Texture2D CardBackTexture { get; private set; }

    public static Font GameFont { get; private set; }

    public static void LoadAssets()
    {
        CardTextures = new Dictionary<string, Texture2D>();

        CardBackTexture = LoadTexture("Assets/Card_Back.png");
        List<int> codepoints = new List<int>();
        for (int i = 32; i < 127; i++) codepoints.Add(i); 
        for (int i = 1024; i < 1119; i++) codepoints.Add(i);
        for (int i = 1168; i < 1172; i++) codepoints.Add(i);

        GameFont = LoadFontEx("Assets/font.ttf", 32, codepoints.ToArray(), codepoints.Count);
        foreach (Enums.CardSuit suit in Enum.GetValues(typeof(Enums.CardSuit)))
        {
            foreach (Enums.CardRank rank in Enum.GetValues(typeof(Enums.CardRank)))
            {
                string rankName = ((int)rank <= 10) ? ((int)rank).ToString() : rank.ToString();

                string fileName = $"Assets/{suit}_{rankName}.png";

                string dictionaryKey = $"{suit}_{rank}";

                CardTextures[dictionaryKey] = LoadTexture(fileName);
            }
        }
    }

    public static void UnloadAssets()
    {
        UnloadTexture(CardBackTexture);
        foreach (var texture in CardTextures.Values)
        {
            UnloadTexture(texture);
        }

        UnloadFont(GameFont);
    }
}