using UnityEngine;
using LSFunctions;

namespace CreativePlayers.Functions
{
    public static class PlayerHelpers
    {
        public static Color GetColor(int playerIndex, int col, float alpha, string hex)
        {
            if (col < 4)
                return LSColors.fadeColor(GameManager.inst.LiveTheme.playerColors[col], alpha);
            if (col == 4)
                return LSColors.fadeColor(GameManager.inst.LiveTheme.guiColor, alpha);
            if (col > 4 && col < 23)
                return LSColors.fadeColor(GameManager.inst.LiveTheme.objectColors[col - 5], alpha);
            if (col == 23)
                return LSColors.fadeColor(GameManager.inst.LiveTheme.playerColors[playerIndex % 4], alpha);
            if (col == 24)
            {
                return LSColors.fadeColor(LSColors.HexToColor(hex), alpha);
            }

            return LSColors.pink500;
        }

        public static Color GetColor(int playerIndex, int col, string hex)
        {
            if (col < 4)
                return GameManager.inst.LiveTheme.playerColors[col];
            if (col == 4)
                return GameManager.inst.LiveTheme.guiColor;
            if (col > 4 && col < 23)
                return GameManager.inst.LiveTheme.objectColors[col - 5];
            if (col == 23)
                return GameManager.inst.LiveTheme.playerColors[playerIndex % 4];
            if (col == 24)
            {
                return LSColors.HexToColor(hex);
            }

            return LSColors.pink500;
        }

    }
}
